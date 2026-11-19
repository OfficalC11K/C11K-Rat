// Dosya: Forms/DarkForm.cs
//using App.Service.Panels;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App.Service
{
    public class DarkForm : Form
    {
        public const int TitleHeight = 34;

        const int WM_NCHITTEST = 0x84;
        const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14,
                  HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

        public readonly ChromeButton BtnClose = new ChromeButton(ChromeKind.Close);
        public readonly ChromeButton BtnMin = new ChromeButton(ChromeKind.Min);
        public readonly ChromeButton BtnMax = new ChromeButton(ChromeKind.Max);
        public readonly Label Title = new Label();

        bool _drag, _built;
        Point _cur, _form;
        Rectangle _restore;
        int _resize = 6;

        static DarkForm() { Native.EnableDpi(); }

        public DarkForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            BackColor = Theme.Current.Main;
            StartPosition = FormStartPosition.CenterScreen;
            Font = Ui.F(9f);
            KeyPreview = true;
            ShowInTaskbar = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected int S(int v) { return (int)Math.Round(v * DeviceDpi / 96.0); }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (e == null || e.Graphics == null) return;
            using (var b = new SolidBrush(BackColor)) e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected void BuildChrome(string title)
        {
            if (_built) return;
            _built = true;

            Title.Text = title;
            Title.AutoSize = false;
            Title.Height = S(TitleHeight);
            Title.TextAlign = ContentAlignment.MiddleLeft;
            Title.Padding = new Padding(S(14), 0, 0, 0);
            Title.Font = Ui.F(9f, FontStyle.Bold);
            Title.ForeColor = Theme.Current.Text;
            Title.BackColor = Color.Transparent;
            Title.AutoEllipsis = true;

            StyleChrome(BtnClose, "✕");
            StyleChrome(BtnMax, "▢");
            StyleChrome(BtnMin, "—");

            BtnClose.Click += (s, e) => Close();
            BtnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;
            BtnMax.Click += (s, e) => ToggleMax();

            Controls.Add(BtnClose);
            Controls.Add(BtnMax);
            Controls.Add(BtnMin);
            Controls.Add(Title);

            MouseDown += DragStart;
            MouseMove += DragMove;
            MouseUp += (s, e) => _drag = false;
            Title.MouseDown += DragStart;
            Title.MouseMove += DragMove;
            Title.MouseUp += (s, e) => _drag = false;
            Title.DoubleClick += (s, e) => ToggleMax();

            Resize += (s, e) => LayoutChrome();
            LayoutChrome();
            Paint += PaintChrome;
        }

        protected void BringChromeToFront()
        {
            Title.BringToFront();
            BtnMin.BringToFront();
            BtnMax.BringToFront();
            BtnClose.BringToFront();
        }

        void StyleChrome(ChromeButton b, string text)
        {
            b.Text = text;
            b.Font = Ui.F(8.5f, FontStyle.Bold);
            b.Size = new Size(S(34), S(TitleHeight));
            b.TabStop = false;
            b.BackColor = Color.Transparent;
        }

        void LayoutChrome()
        {
            int w = S(34);
            BtnMax.Visible = MaximizeBox;
            BtnMin.Visible = MinimizeBox;

            int x = Width;
            x -= w; BtnClose.Location = new Point(x, 0);
            if (MinimizeBox) { x -= w; BtnMin.Location = new Point(x, 0); }
            if (MaximizeBox) { x -= w; BtnMax.Location = new Point(x, 0); }

            Title.Width = Math.Max(0, x - S(6));
            Title.Location = new Point(0, 0);
        }

        protected void ToggleMax()
        {
            if (!MaximizeBox || !BtnMax.Visible) return;
            if (WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
                if (_restore.Width > 0) Bounds = _restore;
            }
            else
            {
                _restore = Bounds;
                MaximizedBounds = Screen.FromControl(this).WorkingArea;
                WindowState = FormWindowState.Maximized;
            }
            if (IsHandleCreated) Native.Rounded(Handle, WindowState != FormWindowState.Maximized);
        }

        void DragStart(object s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (WindowState == FormWindowState.Maximized)
            {
                double rx = (double)e.X / Math.Max(1, Width);
                WindowState = FormWindowState.Normal;
                if (_restore.Width > 0) Bounds = _restore;
                Location = new Point(Cursor.Position.X - (int)(Width * rx), Cursor.Position.Y - S(TitleHeight) / 2);
                if (IsHandleCreated) Native.Rounded(Handle, true);
            }
            _drag = true;
            _cur = Cursor.Position;
            _form = Location;
        }

        void DragMove(object s, MouseEventArgs e)
        {
            if (!_drag) return;
            Point d = Point.Subtract(Cursor.Position, new Size(_cur));
            Location = Point.Add(_form, new Size(d));
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal && _built)
            {
                long lp = m.LParam.ToInt64();
                int sx = unchecked((short)(lp & 0xFFFF));
                int sy = unchecked((short)((lp >> 16) & 0xFFFF));
                Point p = PointToClient(new Point(sx, sy));

                bool l = p.X <= _resize, r = p.X >= ClientSize.Width - _resize;
                bool t = p.Y <= _resize, b = p.Y >= ClientSize.Height - _resize;

                if (t && l) { m.Result = (IntPtr)HTTOPLEFT; return; }
                if (t && r) { m.Result = (IntPtr)HTTOPRIGHT; return; }
                if (b && l) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
                if (b && r) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }
                if (l) { m.Result = (IntPtr)HTLEFT; return; }
                if (r) { m.Result = (IntPtr)HTRIGHT; return; }
                if (t) { m.Result = (IntPtr)HTTOP; return; }
                if (b) { m.Result = (IntPtr)HTBOTTOM; return; }
            }
            base.WndProc(ref m);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.DarkBar(Handle, !Theme.Current.IsLight);
            Native.Rounded(Handle, WindowState != FormWindowState.Maximized);
            _resize = S(6);
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            if (IsHandleCreated) Native.Rounded(Handle, WindowState != FormWindowState.Maximized);
        }

        void PaintChrome(object s, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int th = S(TitleHeight);
            using (var b = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(2, Width), th), Theme.Current.Title, Ui.Mix(Theme.Current.Title, Theme.Current.Accent, 0.18), 0f))
                g.FillRectangle(b, 0, 0, Width, th);

            using (var p = new Pen(Theme.Current.Border)) g.DrawLine(p, 0, th, Width, th);
            using (var p = new Pen(Color.FromArgb(60, Theme.Current.Glow))) g.DrawLine(p, 0, th + 1, Width, th + 1);
        }

        public virtual void ApplyTheme()
        {
            BackColor = Theme.Current.Main;
            Title.ForeColor = Theme.Current.Text;
            LayoutChrome();
            Recolor(this);
            BringChromeToFront();
            Invalidate(true);
        }

        public static void Recolor(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is PillButton pb) { pb.Palette(Theme.Current.Side, Theme.Current.Text); continue; }
                if (c is Button b && (b.Tag as string) != "accent") { b.BackColor = Theme.Current.Side; b.ForeColor = Theme.Current.Text; }
                else if (c is Label l) l.ForeColor = Theme.Current.Text;
                else if (c is CheckBox ch) ch.ForeColor = Theme.Current.Text;
                else if (c is RadioButton rb) rb.ForeColor = Theme.Current.Text;
                else if (c is RichTextBox rtb) { rtb.BackColor = Theme.Current.Title; rtb.ForeColor = Theme.Current.Text; }
                else if (c is TextBox tb) { tb.BackColor = Theme.Current.InputBack; tb.ForeColor = Theme.Current.InputFore; }
                else if (c is ComboBox cb) { cb.BackColor = Theme.Current.InputBack; cb.ForeColor = Theme.Current.InputFore; }
                else if (c is CardPanel cp) { cp.TopColor = Theme.Current.CardTop; cp.BottomColor = Theme.Current.CardBottom; cp.BackColor = Theme.Current.Side; cp.Invalidate(); }
                else if (c is DarkGroup dg) { dg.BackColor = Theme.Current.Main; dg.Invalidate(); }
                else if (c is ClientGrid cg) cg.Invalidate();
                else if (c is RailButton rbtn) rbtn.Invalidate();
                else if (c is ListView lv)
                {
                    lv.BackColor = Theme.Current.Main;
                    lv.ForeColor = Theme.Current.Text;
                    if (lv.IsHandleCreated) Native.SetWindowTheme(lv.Handle, Theme.Current.IsLight ? "Explorer" : "DarkMode_Explorer", null);
                }
                else if (c is DataGridView dgv)
                {
                    dgv.BackgroundColor = Theme.Current.Main;
                    dgv.DefaultCellStyle.BackColor = Theme.Current.Main;
                    dgv.DefaultCellStyle.ForeColor = Theme.Current.Text;
                    dgv.DefaultCellStyle.SelectionBackColor = Theme.Current.Accent;
                    dgv.ColumnHeadersDefaultCellStyle.BackColor = Theme.Current.Side;
                    dgv.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Current.Text;
                }
                else if (c is Panel pnl)
                {
                    pnl.BackColor = (pnl.Tag as string) == "side" ? Theme.Current.Side : Theme.Current.Main;
                    pnl.Invalidate();
                }
                if (c.HasChildren) Recolor(c);
            }
        }
    }
}