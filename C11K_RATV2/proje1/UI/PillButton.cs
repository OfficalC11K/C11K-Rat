// Dosya: UI/PillButton.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App.Service
{
    public class PillButton : Button
    {
        Color _back = Theme.Current.Card;
        Color _fore = Theme.Current.Text;
        float _hover;
        float _angle;
        System.Windows.Forms.Timer _anim;
        bool _spinning;

        public int Radius = 6;

        public PillButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = Ui.F(8f);
            BackColor = Theme.Current.Card;
            ForeColor = Theme.Current.Text;
            _back = BackColor;
            _fore = ForeColor;
        }

        public void Palette(Color back, Color fore)
        {
            _back = back; _fore = fore;
            BackColor = back; ForeColor = fore;
            Invalidate();
        }

        public bool Spinning
        {
            get { return _spinning; }
            set
            {
                _spinning = value;
                if (value) { _anim = _anim ?? NewAnim(); _anim.Start(); }
                else if (_anim != null) _anim.Stop();
                Invalidate();
            }
        }

        System.Windows.Forms.Timer NewAnim()
        {
            var t = new System.Windows.Forms.Timer { Interval = 16 };
            t.Tick += (s, e) => { _angle = (_angle + 26f) % 360f; Invalidate(); };
            return t;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = 1f; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = 0f; Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color bg = Enabled ? Ui.Mix(_back, Color.White, _hover * 0.18) : Ui.Mix(_back, Theme.Current.Main, 0.55);
            var r = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));

            using (var p = Ui.Round(r, Radius))
            {
                using (var b = new SolidBrush(bg)) g.FillPath(b, p);
                using (var pen = new Pen(Ui.Mix(bg, Theme.Current.Border, 0.6))) g.DrawPath(pen, p);
            }

            int textRight = Width;
            if (_spinning)
            {
                textRight = Width - 18;
                using (var pen = new Pen(Ui.Mix(_fore, bg, 0.15), 2f))
                {
                    pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, Width - 16, (Height - 11) / 2, 11, 11, _angle, 260);
                }
            }

            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            using (var b = new SolidBrush(Enabled ? _fore : Ui.Mix(_fore, bg, 0.5)))
                g.DrawString(Text, Font, b, new RectangleF(0, 0, textRight, Height), sf);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _anim != null) { _anim.Dispose(); _anim = null; }
            base.Dispose(disposing);
        }
    }
}