// Dosya: Panels/ClientsPanel.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App.Service
{
    public class ClientsPanel : BufferedPanel
    {
        public const string Ph = SearchBox.Placeholder;

        public ClientGrid Grid { get; private set; }
        public ContextMenuStrip Menu { get; private set; }
        public SearchBox Filter { get; private set; }
        public ComboBox TagFilter { get; private set; }
        public ComboBox SortBox { get; private set; }
        public PillButton RefreshBtn { get; private set; }
        public CheckBox OnlyOnline { get; private set; }

        public Panel Drawer { get; private set; }
        public PictureBox Preview { get; private set; }
        public Label DTitle, DState, DSub;
        public Label ChipOnline, ChipAdmin, ChipAes;

        public Label LblState, LblCount, LblCrypto, LblAdmin;
        Panel _status;
        SplitContainer _split;
        TableLayoutPanel _root;
        Panel _toolbar;

        readonly List<InfoRow> _infoVals = new List<InfoRow>();
        string _fTitle = "-", _fState = "", _fSub = "";
        bool _fOnline;
        string[] _fInfo = new string[9];
        Image _fThumb;
        bool _previewHover;

        public event Action<string> Quick;
        public event Action FilterChanged;
        public event Action RefreshAll;

        public ClientsPanel()
        {
            BackColor = Theme.Current.Main;

            _root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.Current.Main };
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

            _toolbar = BuildToolbar();
            _root.Controls.Add(_toolbar, 0, 0);

            _split = new SplitContainer { Dock = DockStyle.Fill, SplitterWidth = 8, BackColor = Theme.Current.Main, FixedPanel = FixedPanel.Panel2 };
            _split.Panel1.BackColor = Theme.Current.Main;
            _split.Panel2.BackColor = Theme.Current.Main;
            _split.HandleCreated += (s, e) => ApplySplit();
            _split.SizeChanged += (s, e) => ApplySplit();

            Grid = new ClientGrid { Dock = DockStyle.Fill };
            _split.Panel1.Controls.Add(Grid);

            Drawer = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Theme.Current.Main };
            _split.Panel2.Controls.Add(Drawer);
            BuildDrawerContent();

            _root.Controls.Add(_split, 0, 1);
            _split.Panel2Collapsed = true;

            _status = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Current.Side, Tag = "side" };
            LblState = new Label { Text = "Dinlenmiyor", Dock = DockStyle.Left, Width = 176, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0), ForeColor = Theme.Current.Text, AutoEllipsis = true };
            LblAdmin = new Label { Dock = DockStyle.Right, Width = 80, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Current.Muted, Text = "Admin 0" };
            LblCrypto = new Label { Dock = DockStyle.Right, Width = 152, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Current.Muted, Text = "Şifreleme kapalı" };
            LblCount = new Label { Dock = DockStyle.Right, Width = 88, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Current.Text, Text = "Bağlı 0" };
            _status.Controls.Add(LblState);
            _status.Controls.Add(Sep());
            _status.Controls.Add(LblCount);
            _status.Controls.Add(Sep());
            _status.Controls.Add(LblAdmin);
            _status.Controls.Add(Sep());
            _status.Controls.Add(LblCrypto);
            _root.Controls.Add(_status, 0, 2);

            Controls.Add(_root);

            Menu = new ContextMenuStrip { ShowImageMargin = false };
            Menu.Renderer = new DarkRenderer();
            Menu.BackColor = Theme.Current.Card;
            Menu.ForeColor = Theme.Current.Text;

            Grid.SelectionChanged += UpdateDrawer;
            Grid.RightClicked += () =>
            {
                if (!Grid.HasSelection) return;
                Menu.Show(Grid, Grid.PointToClient(Cursor.Position));
            };
            Grid.Activated += () => { if (Quick != null) Quick("rd"); };
        }

        Panel Sep() { return new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Theme.Current.Border }; }

        void ApplySplit()
        {
            if (_split == null || _split.IsDisposed) return;
            int w = _split.Width;
            if (w < 420) return;
            try
            {
                _split.Panel2MinSize = Math.Min(272, w / 3);
                int want = Math.Max(200, w - 304);
                if (Math.Abs(_split.SplitterDistance - want) > 40) _split.SplitterDistance = want;
            }
            catch { }
        }

        Panel BuildToolbar()
        {
            var host = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Theme.Current.Main };

            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.Current.Main };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var left = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(16, 11, 0, 0)
            };

            Filter = new SearchBox { Width = 160, Height = 22, Margin = new Padding(0, 0, 8, 0) };
            Filter.TextChanged += (s, e) => { if (FilterChanged != null) FilterChanged(); };
            left.Controls.Add(Filter);

            TagFilter = new ComboBox { Width = 120, Margin = new Padding(0, 0, 8, 0), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, Font = Ui.F(8.5f) };
            TagFilter.Items.Add("Tüm tag'ler");
            TagFilter.SelectedIndex = 0;
            TagFilter.SelectedIndexChanged += (s, e) => { if (FilterChanged != null) FilterChanged(); };
            left.Controls.Add(TagFilter);

            SortBox = new ComboBox { Width = 120, Margin = new Padding(0, 0, 8, 0), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, Font = Ui.F(8.5f) };
            SortBox.Items.AddRange(new object[] { "Ada göre ↑", "Ada göre ↓", "IP'ye göre", "RTT'ye göre", "Yetkiye göre", "Duruma göre" });
            SortBox.SelectedIndex = 0;
            SortBox.SelectedIndexChanged += (s, e) => { if (FilterChanged != null) FilterChanged(); };
            left.Controls.Add(SortBox);

            OnlyOnline = new CheckBox { Text = "Sadece açık", AutoSize = true, Margin = new Padding(0, 3, 8, 0), ForeColor = Theme.Current.Text, BackColor = Color.Transparent, Font = Ui.F(8.5f) };
            OnlyOnline.CheckedChanged += (s, e) => { if (FilterChanged != null) FilterChanged(); };
            left.Controls.Add(OnlyOnline);

            RefreshBtn = new PillButton { Text = "⟳", Width = 30, Height = 22, Margin = new Padding(0), Font = Ui.F(8.5f) };
            RefreshBtn.Palette(Theme.Current.Card, Theme.Current.Text);
            RefreshBtn.Click += (s, e) => { if (RefreshAll != null) RefreshAll(); };
            left.Controls.Add(RefreshBtn);

            grid.Controls.Add(left, 0, 0);

            var chips = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 12, 16, 0)
            };

            ChipAes = Chip("AES kapalı", Theme.Current.Muted);
            ChipAdmin = Chip("Admin 0", Theme.Current.Accent);
            ChipOnline = Chip("Çevrimiçi 0", Theme.Current.Ok);

            chips.Controls.Add(ChipAes);
            chips.Controls.Add(ChipAdmin);
            chips.Controls.Add(ChipOnline);
            grid.Controls.Add(chips, 1, 0);

            host.Controls.Add(grid);
            return host;
        }

        void RebuildToolbar()
        {
            string keepTag = TagFilter != null ? Convert.ToString(TagFilter.SelectedItem) : null;
            int keepSort = SortBox != null ? SortBox.SelectedIndex : 0;
            bool only = OnlyOnline != null && OnlyOnline.Checked;
            string keepText = Filter != null ? Filter.Text : Ph;

            var old = _toolbar;
            _toolbar = BuildToolbar();
            if (old != null) { _root.Controls.Remove(old); old.Dispose(); }
            _root.Controls.Add(_toolbar, 0, 0);
            _toolbar.BringToFront();

            if (keepTag != null && TagFilter.Items.Contains(keepTag)) TagFilter.SelectedItem = keepTag;
            if (keepSort >= 0) SortBox.SelectedIndex = keepSort;
            OnlyOnline.Checked = only;
            Filter.Text = keepText;
        }

        Label Chip(string text, Color color)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Padding = new Padding(8, 4, 8, 4),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = color,
                BackColor = Theme.Current.Card,
                Font = Ui.F(7.5f, FontStyle.Bold),
                Margin = new Padding(4, 0, 0, 0)
            };
        }

        void BuildDrawerContent()
        {
            if (Drawer == null) return;

            Drawer.SuspendLayout();
            var kids = new Control[Drawer.Controls.Count];
            Drawer.Controls.CopyTo(kids, 0);
            Drawer.Controls.Clear();
            foreach (var c in kids) { try { c.Dispose(); } catch { } }
            _infoVals.Clear();

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Theme.Current.Main,
                Padding = new Padding(10, 10, 4, 10)
            };

            int W = Drawer.ClientSize.Width > 80 ? Drawer.ClientSize.Width - 24 : 250;
            if (W < 140) W = 250;

            DTitle = new Label { Text = "-", Width = W, Height = 24, Font = Ui.F(10.5f, FontStyle.Bold), ForeColor = Theme.Current.Text, Margin = new Padding(4, 0, 0, 2), AutoEllipsis = true };
            DState = new Label { Text = "", Width = W, Height = 16, Font = Ui.F(8f), ForeColor = Theme.Current.Muted, Margin = new Padding(4, 0, 0, 0) };
            DSub = new Label { Text = "", Width = W, Height = 16, Font = Ui.F(7.5f), ForeColor = Theme.Current.Muted, Margin = new Padding(4, 0, 0, 8), AutoEllipsis = true };

            flow.Controls.Add(DTitle);
            flow.Controls.Add(DState);
            flow.Controls.Add(DSub);

            Preview = new PictureBox { Width = W, Height = 124, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black, Margin = new Padding(4, 0, 0, 10) };
            Ui.Buffered(Preview);
            Preview.Paint += (s, e) =>
            {
                using (var p = new Pen(Theme.Current.Border)) e.Graphics.DrawRectangle(p, 0, 0, Preview.Width - 1, Preview.Height - 1);
                if (_previewHover && Preview.Image != null)
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    var box = new Rectangle(Preview.Width - 28, 6, 22, 22);
                    using (var p = Ui.Round(box, 5))
                    using (var b = new SolidBrush(Color.FromArgb(190, 0, 0, 0)))
                        e.Graphics.FillPath(b, p);
                    using (var b = new SolidBrush(Color.White))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        e.Graphics.DrawString("⤢", Ui.F(10f, FontStyle.Bold), b, box, sf);
                }
            };
            Preview.MouseEnter += (s, e) => { _previewHover = true; Preview.Cursor = Cursors.Hand; Preview.Invalidate(); };
            Preview.MouseLeave += (s, e) => { _previewHover = false; Preview.Cursor = Cursors.Default; Preview.Invalidate(); };
            Preview.Click += (s, e) =>
            {
                if (Preview.Image == null) return;
                try { new PreviewForm((Image)Preview.Image.Clone(), "Önizleme - " + _fTitle).Show(); } catch { }
            };
            flow.Controls.Add(Preview);

            flow.Controls.Add(InfoCard(W));
            flow.Controls.Add(ActionCard(W, "HIZLI EYLEM", new[]
            {
                new[] { "Ekran", "shot" }, new[] { "Masaüstü", "rd" },
                new[] { "Terminal", "shell" }, new[] { "Dosyalar", "files" },
                new[] { "Sohbet", "chat" }, new[] { "Webcam", "webcam" },
                new[] { "Input", "keylog" }, new[] { "Pano", "clip" }
            }));
            flow.Controls.Add(ActionCard(W, "MEDYA & MASAÜSTÜ", new[]
            {
                new[] { "Ses +", "volup" }, new[] { "Ses −", "voldown" },
                new[] { "Sessiz", "mute" }, new[] { "Monitör Kapat", "monoff" },
                new[] { "Kilitle", "lock" }, new[] { "CD Tepsi", "cd" },
                new[] { "Görev Çubuğu", "taskbar" }, new[] { "Bildirim", "toast" }
            }));
            flow.Controls.Add(ActionCard(W, "SİSTEM ARAÇLARI", new[]
            {
                new[] { "Süreçler", "task" }, new[] { "Kayıt Defteri", "reg" },
                new[] { "Başlangıç", "startup" }, new[] { "Veriler", "pass" },
                new[] { "Sistem Bilgisi", "sysinfo" }, new[] { "Script", "script" }
            }));
            flow.Controls.Add(ActionCard(W, "BİLGİ TOPLAMA", new[]
            {
                new[] { "Sürücüler", "drives" }, new[] { "Ortam", "env" },
                new[] { "Hosts", "hosts" }, new[] { "Yazılımlar", "soft" },
                new[] { "Dosya Ara", "search" }, new[] { "Konsol", "console" }
            }));
            flow.Controls.Add(ActionCard(W, "GÜÇ", new[]
            {
                new[] { "Yeniden Başlat", "restart" }, new[] { "Kapat", "shutdown" },
                new[] { "Oturum Kapat", "logoff" }, new[] { "Beklet", "standby" }
            }));
            flow.Controls.Add(ActionCard(W, "BAKIM", new[]
            {
                new[] { "Yönetici Yap", "elevate" }, new[] { "Yeniden Bağlan", "reconnect" },
                new[] { "Duvar Kağıdı", "wallpaper" }, new[] { "Kaldır", "uninstall" }
            }));

            Drawer.Controls.Add(flow);
            Drawer.ResumeLayout();
            Drawer.Resize -= DrawerResized;
            Drawer.Resize += DrawerResized;
            SizeDrawerContent();
            ApplyFill();
        }

        void DrawerResized(object sender, EventArgs e) { SizeDrawerContent(); }

        void SizeDrawerContent()
        {
            if (Drawer == null || Drawer.Controls.Count == 0) return;
            var flow = Drawer.Controls[0] as FlowLayoutPanel;
            if (flow == null) return;

            int w = flow.ClientSize.Width - flow.Padding.Horizontal;
            if (w < 120) return;

            flow.SuspendLayout();
            foreach (Control c in flow.Controls)
                if (c.Width != w) c.Width = w;
            flow.ResumeLayout();
        }

        Panel InfoCard(int w)
        {
            int rowH = InfoRow.RowHeight;
            var pad = new Padding(10, 8, 10, 8);
            var card = new CardPanel { Width = w, Padding = pad, Margin = new Padding(4, 0, 0, 8) };

            var t = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, BackColor = Color.Transparent };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(t, rowH, "▣", "İşletim Sistemi");
            AddRow(t, rowH, "⚑", "Yetki");
            AddRow(t, rowH, "◈", "Koruma");
            AddRow(t, rowH, "▤", "Bellek");
            AddRow(t, rowH, "◉", "Kamera");
            AddRow(t, rowH, "▭", "Monitör");
            AddRow(t, rowH, "↯", "Gecikme");
            AddRow(t, rowH, "◔", "Çalışma Süresi");
            AddRow(t, rowH, "◷", "Son Görülme");

            card.Controls.Add(t);
            card.Height = pad.Vertical + _infoVals.Count * rowH + 4;
            return card;
        }

        void AddRow(TableLayoutPanel t, int rowH, string glyph, string caption)
        {
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, rowH));
            var r = new InfoRow { Dock = DockStyle.Fill, Glyph = glyph, Caption = caption, Height = rowH };
            _infoVals.Add(r);
            t.Controls.Add(r);
        }

        Panel ActionCard(int w, string caption, string[][] pairs)
        {
            int rows = (pairs.Length + 1) / 2;
            var card = new CardPanel { Width = w, Padding = new Padding(8, 5, 8, 6), Margin = new Padding(4, 0, 0, 8) };

            var cap = new Label { Text = caption, Dock = DockStyle.Top, Height = 16, Font = Ui.F(7f, FontStyle.Bold), ForeColor = Theme.Current.Muted, BackColor = Color.Transparent };

            var t = new TableLayoutPanel { Dock = DockStyle.Top, Height = rows * 30, ColumnCount = 2, BackColor = Color.Transparent };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            for (int i = 0; i < pairs.Length; i += 2)
            {
                t.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
                t.Controls.Add(ActBtn(pairs[i][0], pairs[i][1]));
                if (i + 1 < pairs.Length) t.Controls.Add(ActBtn(pairs[i + 1][0], pairs[i + 1][1]));
            }

            card.Controls.Add(t);
            card.Controls.Add(cap);

            card.Height = 16 + rows * 30 + 11;
            return card;
        }

        PillButton ActBtn(string text, string key)
        {
            var b = new PillButton
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
                Font = Ui.F(7.5f),
                Radius = 5
            };
            b.Palette(Theme.Current.Card, Theme.Current.Text);
            b.Click += (s, e) => { if (Quick != null) Quick(key); };
            return b;
        }

        void UpdateDrawer() { _split.Panel2Collapsed = !Grid.HasSelection; }

        public void FillDrawer(string title, string state, bool online, string sub, string[] info, Image thumb)
        {
            _fTitle = title ?? "-";
            _fState = state ?? "";
            _fSub = sub ?? "";
            _fOnline = online;
            _fInfo = info ?? new string[9];

            _fThumb = thumb;
            ApplyFill();
        }

        public void ClearDrawer()
        {
            _fTitle = "-";
            _fState = "";
            _fSub = "";
            _fOnline = false;
            _fInfo = new string[9];

            if (_fThumb != null) { _fThumb.Dispose(); _fThumb = null; }

            ApplyFill();
            if (Preview != null) Preview.Image = null;
        }

        void ApplyFill()
        {
            if (DTitle != null) DTitle.Text = _fTitle;

            if (DState != null)
            {
                DState.Text = _fState;
                DState.ForeColor = _fOnline ? Theme.Current.Ok : Theme.Current.Muted;
            }

            if (DSub != null) DSub.Text = _fSub;

            if (_fInfo != null && _infoVals.Count > 0)
            {
                for (int i = 0; i < _infoVals.Count && i < _fInfo.Length; i++)
                    _infoVals[i].Value = _fInfo[i];
            }

            if (Preview != null && !ReferenceEquals(Preview.Image, _fThumb))
            {
                var old = Preview.Image;
                Preview.Image = _fThumb;
                if (old != null && !ReferenceEquals(old, _fThumb)) old.Dispose();
            }
        }

        public void ApplyTheme()
        {
            BackColor = Theme.Current.Main;
            Grid.ApplyTheme();

            _root.BackColor = Theme.Current.Main;
            _split.BackColor = Theme.Current.Main;
            _split.Panel1.BackColor = Theme.Current.Main;
            _split.Panel2.BackColor = Theme.Current.Main;

            RebuildToolbar();

            if (Drawer != null)
            {
                Drawer.BackColor = Theme.Current.Main;
                BuildDrawerContent();
            }

            _status.BackColor = Theme.Current.Side;
            LblState.ForeColor = Theme.Current.Text;
            LblCount.ForeColor = Theme.Current.Text;
            foreach (var c in new[] { ChipOnline, ChipAdmin, ChipAes }) if (c != null) c.BackColor = Theme.Current.Card;

            if (Menu != null)
            {
                Menu.BackColor = Theme.Current.Card;
                Menu.ForeColor = Theme.Current.Text;
                Menu.Renderer = new DarkRenderer();
            }

            Invalidate(true);
        }
    }
}