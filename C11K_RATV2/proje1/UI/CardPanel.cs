// Dosya: UI/CardPanel.cs
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App.Service
{
    public class CardPanel : BufferedPanel
    {
        public int Radius = 8;
        public Color TopColor = Theme.Current.CardTop;
        public Color BottomColor = Theme.Current.CardBottom;

        public CardPanel() { BackColor = Theme.Current.Side; }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var b = new SolidBrush(BackColor)) g.FillRectangle(b, ClientRectangle);

            var r = new Rectangle(5, 4, System.Math.Max(1, Width - 10), System.Math.Max(1, Height - 9));
            Ui.Shadow(g, r, Radius, 3);

            using (var path = Ui.Round(r, Radius))
            {
                using (var br = new LinearGradientBrush(r, TopColor, BottomColor, 90f)) g.FillPath(br, path);
                using (var pen = new Pen(Theme.Current.Border)) g.DrawPath(pen, path);
            }
        }
    }
}