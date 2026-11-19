// Dosya: UI/DarkGroup.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App.Service
{
    public class DarkGroup : BufferedPanel
    {
        public string Caption = "";

        public DarkGroup()
        {
            BackColor = Theme.Current.Main;
            Font = Ui.F(7.5f, FontStyle.Bold);
        }

        int S(int v) { return (int)Math.Round(v * DeviceDpi / 96.0); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(S(4), S(10), Math.Max(1, Width - S(9)), Math.Max(1, Height - S(15)));

            Ui.Shadow(g, rect, S(7), 3);

            using (var path = Ui.Round(rect, S(7)))
            {
                using (var fill = new LinearGradientBrush(rect, Theme.Current.CardTop, Theme.Current.CardBottom, 90f)) g.FillPath(fill, path);
                using (var p = new Pen(Theme.Current.Border)) g.DrawPath(p, path);
            }

            if (!string.IsNullOrEmpty(Caption))
            {
                Size sz = TextRenderer.MeasureText(Caption, Font);
                using (var b = new SolidBrush(Theme.Current.Card)) g.FillRectangle(b, S(13), S(3), sz.Width + S(8), S(13));
                using (var b = new SolidBrush(Theme.Current.Accent)) g.DrawString(Caption, Font, b, S(16), S(1));
            }
            base.OnPaint(e);
        }
    }
}