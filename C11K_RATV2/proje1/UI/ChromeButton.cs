// Dosya: UI/ChromeButton.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App.Service
{
    public class ChromeButton : Control
    {
        readonly ChromeKind _kind;
        float _hover;

        public ChromeButton(ChromeKind kind)
        {
            _kind = kind;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = 1f; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = 0f; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_hover > 0.01f)
            {
                Color baseCol = _kind == ChromeKind.Close ? Theme.Current.Bad : Theme.Current.Accent;
                using (var p = Ui.Round(new Rectangle(3, 3, Math.Max(1, Width - 7), Math.Max(1, Height - 7)), 5))
                using (var b = new SolidBrush(Color.FromArgb((int)(_hover * 150), baseCol)))
                    g.FillPath(b, p);
            }

            using (var b = new SolidBrush(_hover > 0.5f ? Color.White : Theme.Current.Text))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(Text, Font, b, new RectangleF(0, 0, Width, Height), sf);
        }
    }
}