// Dosya: UI/RailButton.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App.Service
{
    public class RailButton : Control
    {
        public string Glyph = "";
        public string Caption = "";
        public bool Active;

        readonly Font _fGlyph = Ui.F(14f);
        readonly Font _fCap = Ui.F(6.5f);

        float _indicator;
        float _hover;
        System.Windows.Forms.Timer _anim;
        Form _tip;

        public RailButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Theme.Current.Side;
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        int S(int v) { return (int)Math.Round(v * DeviceDpi / 96.0); }

        void Kick()
        {
            if (_anim == null)
            {
                _anim = new System.Windows.Forms.Timer { Interval = 16 };
                _anim.Tick += (s, e) =>
                {
                    float target = Active ? 1f : 0f;
                    _indicator += (target - _indicator) * 0.25f;
                    if (Math.Abs(target - _indicator) < 0.02f) { _indicator = target; _anim.Stop(); }
                    Invalidate();
                };
            }
            _anim.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color bg = Ui.Mix(Theme.Current.Side, Theme.Current.Accent, 0.10 + _indicator * 0.22 + _hover * 0.06);
            using (var b = new SolidBrush(bg)) g.FillRectangle(b, ClientRectangle);

            if (_indicator > 0.01f)
            {
                int h = (int)((Height - S(18)) * _indicator);
                using (var b = new SolidBrush(Theme.Current.Accent))
                    g.FillRectangle(b, 0, (Height - h) / 2, S(3), Math.Max(1, h));
            }

            Color fg = _indicator > 0.5f ? Color.White : Theme.Current.Text;

            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
            using (var b = new SolidBrush(fg))
                g.DrawString(Glyph, _fGlyph, b, new RectangleF(0, S(4), Width, S(21)), sf);

            using (var b = new SolidBrush(Ui.Mix(Theme.Current.Muted, Color.White, _indicator)))
                g.DrawString(Caption, _fCap, b, new RectangleF(2, Height - S(19), Width - 4, S(14)), sf);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = 1f;
            Invalidate();
            ShowTip();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = 0f;
            Invalidate();
            HideTip();
        }

        void ShowTip()
        {
            if (string.IsNullOrEmpty(Caption) || Parent == null) return;
            try
            {
                HideTip();
                var f = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    StartPosition = FormStartPosition.Manual,
                    ShowInTaskbar = false,
                    TopMost = true,
                    BackColor = Theme.Current.Card,
                    Size = new Size(TextRenderer.MeasureText(Caption, Ui.F(8.5f)).Width + 20, 26)
                };
                f.Paint += (s, e) =>
                {
                    using (var p = new Pen(Theme.Current.Border)) e.Graphics.DrawRectangle(p, 0, 0, f.Width - 1, f.Height - 1);
                };
                f.Controls.Add(new Label
                {
                    Text = Caption,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Theme.Current.Text,
                    Font = Ui.F(8.5f)
                });

                var screen = PointToScreen(new Point(Width, (Height - f.Height) / 2));
                f.Location = new Point(screen.X + 6, screen.Y);
                f.Show();
                _tip = f;
            }
            catch { }
        }

        void HideTip()
        {
            var t = _tip;
            _tip = null;
            if (t != null) { try { t.Close(); } catch { } }
        }

        public void SetActive(bool on)
        {
            if (Active == on) return;
            Active = on;
            Kick();
        }

        public void ApplyTheme()
        {
            BackColor = Theme.Current.Side;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                HideTip();
                _fGlyph.Dispose();
                _fCap.Dispose();
                if (_anim != null) _anim.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}