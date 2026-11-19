// Dosya: Panels/ClientGrid.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace App.Service
{
    public class ClientGrid : Control
    {
        public sealed class Row
        {
            public ClientSession S;
            public bool Offline;
            public bool Fav;
            public string Key;
        }

        struct Cols
        {
            public int Dot, Avatar, Thumb, Text, TextW, AvX, UpX, RttX, AdmX, Right, W;
            public bool ShowAv, ShowUp, ShowRtt;
        }

        readonly List<Row> _rows = new List<Row>();
        readonly VScrollBar _bar = new VScrollBar();
        readonly Font _fName = Ui.F(9.5f, FontStyle.Bold);
        readonly Font _fSub = Ui.F(8f);
        readonly Font _fHead = Ui.F(7f, FontStyle.Bold);
        readonly Font _fStar = Ui.F(8.5f);
        readonly Font _fAvatar = Ui.F(9f, FontStyle.Bold);

        int _hover = -1;
        int _hoverPrev = -1;
        float _hoverMix;
        System.Windows.Forms.Timer _hoverAnim;
        string _sel;

        public int SortColumn = 0;
        public bool SortDesc = false;

        public event Action SelectionChanged;
        public event Action Activated;
        public event Action RightClicked;

        public ClientGrid()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            BackColor = Theme.Current.Main;
            TabStop = true;

            _bar.Dock = DockStyle.Right;
            _bar.SmallChange = 1;
            _bar.Scroll += (s, e) => Invalidate();
            Controls.Add(_bar);

            _hoverAnim = new System.Windows.Forms.Timer { Interval = 15 };
            _hoverAnim.Tick += (s, e) =>
            {
                _hoverMix += 0.22f;
                if (_hoverMix >= 1f)
                {
                    _hoverMix = 1f;
                    _hoverPrev = -1;
                    _hoverAnim.Stop();
                }
                InvalidateRow(_hover);
                if (_hoverPrev >= 0) InvalidateRow(_hoverPrev);
            };
        }

        int S(int v) { return (int)Math.Round(v * DeviceDpi / 96.0); }
        int RowH { get { return S(58); } }
        int HeadH { get { return S(26); } }
        int Offset { get { return _bar.Value; } }
        int UsableWidth { get { return Math.Max(1, Width - (_bar.Visible ? _bar.Width : 0)); } }

        public int Count { get { return _rows.Count; } }
        public bool HasSelection { get { return Selected != null; } }

        public int SelectedIndex
        {
            get
            {
                if (_sel == null) return -1;
                for (int i = 0; i < _rows.Count; i++)
                    if (_rows[i].Key == _sel) return i;
                return -1;
            }
        }

        public ClientSession Selected
        {
            get
            {
                int i = SelectedIndex;
                return i < 0 ? null : _rows[i].S;
            }
        }

        public void SetRows(List<ClientSession> list, Func<ClientSession, bool> isFav)
        {
            string keep = _sel;
            _rows.Clear();
            foreach (var s in list)
                _rows.Add(new Row { S = s, Offline = !s.Online, Fav = isFav(s), Key = s.Online ? s.Sid : "off:" + s.UserPc });

            if (keep != null && !_rows.Any(r => r.Key == keep)) _sel = null;
            UpdateScroll();
            Invalidate();
        }

        public void SelectKey(string key)
        {
            int old = SelectedIndex;
            _sel = key;
            int i = SelectedIndex;
            if (i >= 0) EnsureVisible(i);
            if (old >= 0) InvalidateRow(old);
            if (i >= 0) InvalidateRow(i);
            Raise();
        }

        public void ClearSelection()
        {
            int old = SelectedIndex;
            _sel = null;
            if (old >= 0) InvalidateRow(old);
            Raise();
        }

        public void UpdateRow(string sid)
        {
            if (sid == null) return;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].S == null || _rows[i].S.Sid != sid) continue;
                InvalidateRow(i);
                return;
            }
        }

        public void InvalidateRow(int index)
        {
            if (index < 0 || index >= _rows.Count) return;
            int y = HeadH + index * RowH - Offset;
            if (y > Height || y + RowH < HeadH) return;
            Invalidate(new Rectangle(0, y, Width, RowH));
        }

        void Raise() { if (SelectionChanged != null) SelectionChanged(); }

        void UpdateScroll()
        {
            int content = _rows.Count * RowH;
            int view = Math.Max(1, Height - HeadH);
            _bar.Maximum = Math.Max(0, content - 1);
            _bar.LargeChange = view;
            bool vis = content > view;
            if (_bar.Visible != vis) _bar.Visible = vis;
            _bar.Value = Clamp(_bar.Value, 0, Math.Max(0, content - view));
            Invalidate();
        }

        static int Clamp(int v, int min, int max) { return v < min ? min : (v > max ? max : v); }

        void EnsureVisible(int index)
        {
            int y = index * RowH;
            int view = Math.Max(1, Height - HeadH);
            if (y < Offset) _bar.Value = y;
            else if (y + RowH > Offset + view) _bar.Value = Math.Max(0, y + RowH - view);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScroll();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int max = Math.Max(0, _bar.Maximum - _bar.LargeChange + 1);
            _bar.Value = Clamp(_bar.Value - (e.Delta / 4), 0, max);
            Invalidate();
        }

        int HitTest(int y)
        {
            int rel = y - HeadH + Offset;
            if (rel < 0) return -1;
            int i = rel / RowH;
            return i < _rows.Count ? i : -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int i = HitTest(e.Y);
            if (i != _hover)
            {
                _hoverPrev = _hover;
                _hover = i;
                _hoverMix = 0f;
                _hoverAnim.Stop();
                _hoverAnim.Start();
            }
            Cursor = i >= 0 ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverPrev = _hover;
            _hover = -1;
            _hoverMix = 0f;
            _hoverAnim.Stop();
            _hoverAnim.Start();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            int i = HitTest(e.Y);
            if (i < 0) return;

            int old = SelectedIndex;
            _sel = _rows[i].Key;
            if (old >= 0) InvalidateRow(old);
            InvalidateRow(i);
            Raise();

            if (e.Button == MouseButtons.Right)
            {
                if (RightClicked != null) RightClicked();
            }
            else if (e.Button == MouseButtons.Left && e.Clicks >= 2)
            {
                if (Activated != null) Activated();
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Enter) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_rows.Count == 0) return;

            int i = SelectedIndex;
            if (e.KeyCode == Keys.Down) { i = Math.Min(_rows.Count - 1, i + 1); SelectKey(_rows[i].Key); }
            else if (e.KeyCode == Keys.Up) { i = Math.Max(0, i - 1); SelectKey(_rows[i].Key); }
            else if (e.KeyCode == Keys.Enter && Activated != null) Activated();
        }

        Cols Calc()
        {
            int w = UsableWidth;
            int dot = S(14);
            int avatar = dot + S(24);
            int thumb = avatar + S(28);
            int text = thumb + S(70);
            int right = Math.Max(text + S(70), w - S(12));

            var c = new Cols { Dot = dot, Avatar = avatar, Thumb = thumb, Text = text, W = w };
            c.ShowAv = w >= S(640);
            c.ShowUp = w >= S(560);
            c.ShowRtt = w >= S(480);

            int r = right;
            if (c.ShowAv) { r -= S(70); c.AvX = r; }
            if (c.ShowUp) { r -= S(66); c.UpX = r; }
            if (c.ShowRtt) { r -= S(56); c.RttX = r; }
            r -= S(56);
            c.AdmX = r;
            c.Right = r - S(8);
            c.TextW = Math.Max(S(70), c.Right - text);
            return c;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Current.Main);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (Width <= 0 || Height <= 0) return;

            var c = Calc();
            DrawHeader(g, c);

            g.SetClip(new Rectangle(0, HeadH, c.W, Math.Max(0, Height - HeadH)));

            for (int i = 0; i < _rows.Count; i++)
            {
                int y = HeadH + i * RowH - Offset;
                if (y > Height || y + RowH < HeadH) continue;
                DrawRow(g, c, i, y);
            }

            g.ResetClip();

            if (_rows.Count == 0)
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (var b = new SolidBrush(Theme.Current.Muted))
                    g.DrawString("Bağlı istemci yok.\r\nBuilder'dan üret, sonra Ayarlar'dan dinlemeyi başlat.\r\n\r\nİstemci bağlanınca otomatik ekran önizlemesi alınır.", _fSub, b,
                        new RectangleF(0, HeadH, c.W, Math.Max(1, Height - HeadH)), sf);
            }

            using (var p = new Pen(Theme.Current.Border))
                g.DrawLine(p, 0, HeadH - 1, c.W, HeadH - 1);
        }

        void Head(Graphics g, string text, Rectangle r, int column, TextFormatFlags extra)
        {
            bool on = SortColumn == column;
            using (var b = new SolidBrush(on ? Theme.Current.Accent : Theme.Current.Muted))
                TextRenderer.DrawText(g, text, _fHead, r, b.Color, TextFormatFlags.VerticalCenter | extra);
            if (on)
            {
                string arrow = SortDesc ? "▼" : "▲";
                var ar = new Rectangle(r.X + (extra.HasFlag(TextFormatFlags.Right) ? r.Width - S(11) : 0), r.Y, S(11), r.Height);
                using (var b = new SolidBrush(Theme.Current.Accent))
                    TextRenderer.DrawText(g, arrow, _fHead, ar, b.Color, TextFormatFlags.VerticalCenter | extra);
            }
        }

        void DrawHeader(Graphics g, Cols c)
        {
            using (var b = new SolidBrush(Theme.Current.Title)) g.FillRectangle(b, 0, 0, c.W, HeadH);
            Head(g, "ÖNİZLEME", new Rectangle(c.Thumb, 0, S(70), HeadH), 0, TextFormatFlags.Left);
            Head(g, "KULLANICI  /  AĞ", new Rectangle(c.Text, 0, c.TextW, HeadH), 1, TextFormatFlags.Left);
            Head(g, "YETKİ", new Rectangle(c.AdmX, 0, S(52), HeadH), 4, TextFormatFlags.Right);
            if (c.ShowRtt) Head(g, "RTT", new Rectangle(c.RttX, 0, S(52), HeadH), 3, TextFormatFlags.Right);
            if (c.ShowUp) Head(g, "SÜRE", new Rectangle(c.UpX, 0, S(62), HeadH), 5, TextFormatFlags.Right);
            if (c.ShowAv) Head(g, "AV", new Rectangle(c.AvX, 0, S(66), HeadH), 2, TextFormatFlags.Right);
        }

        void DrawRow(Graphics g, Cols c, int index, int y)
        {
            var row = _rows[index];
            var s = row.S;
            bool sel = row.Key == _sel;

            float hovA = index == _hover ? _hoverMix : (index == _hoverPrev ? 1f - _hoverMix : 0f);
            if (_hoverPrev < 0 && index == _hover) hovA = 1f;

            using (var b = new SolidBrush(index % 2 == 1 ? Theme.Current.RowAlt : Theme.Current.Main))
                g.FillRectangle(b, 0, y, c.W, RowH);

            if (hovA > 0.01f && !sel)
                using (var b = new SolidBrush(Color.FromArgb((int)(hovA * 22), Theme.Current.Accent)))
                    g.FillRectangle(b, 0, y, c.W, RowH);

            if (sel)
            {
                var box = new Rectangle(S(4), y + S(4), Math.Max(1, c.W - S(9)), RowH - S(9));
                using (var path = Ui.Round(box, S(7)))
                {
                    using (var b = new LinearGradientBrush(box, Theme.Current.SelTop, Theme.Current.SelBottom, 90f)) g.FillPath(b, path);
                    using (var p = new Pen(Theme.Current.Accent)) g.DrawPath(p, path);
                }
            }

            Color fg = sel ? Color.White : (row.Offline ? Theme.Current.Muted : Theme.Current.Text);
            Color sub = sel ? Color.FromArgb(232, 238, 250) : Theme.Current.Muted;

            if (!row.Offline)
            {
                Color dot = s.IsAdmin ? Theme.Current.Ok : Theme.Current.Accent;
                using (var b = new SolidBrush(Color.FromArgb(50, dot)))
                    g.FillEllipse(b, c.Dot - S(3), y + RowH / 2 - S(9), S(18), S(18));
                using (var p = new Pen(Color.FromArgb(120, dot), 1f))
                    g.DrawEllipse(p, c.Dot, y + RowH / 2 - S(6), S(12), S(12));
                using (var b = new SolidBrush(dot))
                    g.FillEllipse(b, c.Dot + S(4), y + RowH / 2 - S(2), S(4), S(4));
            }
            else
            {
                using (var b = new SolidBrush(Theme.Current.Muted))
                    g.FillEllipse(b, c.Dot + S(4), y + RowH / 2 - S(2), S(4), S(4));
            }

            var av = new Rectangle(c.Avatar, y + (RowH - S(24)) / 2, S(24), S(24));
            using (var b = new SolidBrush(Ui.Tint(s.UserPc, Theme.Current.Accent, Theme.Current.Card))) g.FillEllipse(b, av);
            using (var p = new Pen(Theme.Current.Border)) g.DrawEllipse(p, av);
            string initial = string.IsNullOrEmpty(s.UserPc) ? "?" : s.UserPc.Substring(0, 1).ToUpperInvariant();
            using (var b = new SolidBrush(Color.White))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(initial, _fAvatar, b, av, sf);

            var thumb = new Rectangle(c.Thumb, y + (RowH - S(34)) / 2, S(66), S(34));
            using (var path = Ui.Round(thumb, S(5)))
            {
                using (var b = new SolidBrush(Theme.Current.Title)) g.FillPath(b, path);
                if (s.Thumb != null)
                {
                    var old = g.Clip;
                    g.SetClip(path);
                    g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    g.DrawImage(s.Thumb, Ui.Fit(thumb, s.Thumb.Width, s.Thumb.Height));
                    g.Clip = old;
                }
                using (var p = new Pen(Theme.Current.Border)) g.DrawPath(p, path);
            }

            int nameW = row.Fav ? Math.Max(S(40), c.TextW - S(16)) : c.TextW;
            TextRenderer.DrawText(g, Ui.Short(s.UserPc, 40), _fName, new Rectangle(c.Text, y + S(9), nameW, S(18)), fg, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            if (row.Fav)
                TextRenderer.DrawText(g, "★", _fStar, new Point(c.Text + nameW + S(2), y + S(8)), sel ? Color.White : Theme.Current.Warn);

            string line = (s.Ip ?? "") + (string.IsNullOrEmpty(s.Tag) ? "" : "  •  " + s.Tag) + (string.IsNullOrEmpty(s.Os) ? "" : "  •  " + Ui.Short(s.Os, 22));
            TextRenderer.DrawText(g, line, _fSub, new Rectangle(c.Text, y + S(30), c.TextW, S(16)), sub, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(g, s.IsAdmin ? "admin" : "user", _fSub, new Rectangle(c.AdmX, y, S(52), RowH), fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);

            if (c.ShowRtt)
            {
                string rtt = row.Offline ? "—" : (s.Rtt < 0 ? "…" : s.Rtt + "ms");
                Color rttColor = sel ? Color.White : (!row.Offline && s.Rtt >= 0 && s.Rtt < 90 ? Theme.Current.Ok : (!row.Offline && s.Rtt > 260 ? Theme.Current.Bad : sub));
                TextRenderer.DrawText(g, rtt, _fSub, new Rectangle(c.RttX, y, S(52), RowH), rttColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
            }

            if (c.ShowUp)
            {
                string up = row.Offline ? Ui.Ago(s.LastSeen) : (s.UptimeSec > 0 ? Ui.Ago(DateTime.UtcNow.AddSeconds(-s.UptimeSec)) : "—");
                TextRenderer.DrawText(g, up, _fSub, new Rectangle(c.UpX, y, S(62), RowH), sub, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
            }

            if (c.ShowAv)
                TextRenderer.DrawText(g, Ui.Short(s.Av, 9), _fSub, new Rectangle(c.AvX, y, S(66), RowH), sub, TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.EndEllipsis);
        }

        public void ApplyTheme()
        {
            BackColor = Theme.Current.Main;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fName.Dispose();
                _fSub.Dispose();
                _fHead.Dispose();
                _fStar.Dispose();
                _fAvatar.Dispose();
                _bar.Dispose();
                if (_hoverAnim != null) _hoverAnim.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}