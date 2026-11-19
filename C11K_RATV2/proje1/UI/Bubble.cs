// Dosya: UI/Bubble.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App.Service
{
    public class Bubble : Control
    {
        public bool Mine;
        string _text = "";

        public Bubble(string text, bool mine)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            _text = text ?? "";
            Mine = mine;
            Font = Ui.F(9f);
            BackColor = Color.Transparent;
            Margin = new Padding(6, 3, 6, 3);
        }

        public int Measure(int max)
        {
            using (var g = CreateGraphics())
            {
                var sz = TextRenderer.MeasureText(_text, Font, new Size(max - 28, 4000), TextFormatFlags.WordBreak);
                Height = Math.Max(30, sz.Height + 16);
                Width = Math.Min(max - 12, sz.Width + 26);
            }
            return Height;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));

            Color top = Mine ? Theme.Current.SelTop : Theme.Current.CardTop;
            Color bot = Mine ? Theme.Current.SelBottom : Theme.Current.CardBottom;

            using (var p = Ui.Round(r, 9))
            {
                using (var b = new LinearGradientBrush(r, top, bot, 90f)) g.FillPath(b, p);
                using (var pen = new Pen(Mine ? Theme.Current.Accent : Theme.Current.Border)) g.DrawPath(pen, p);
            }

            using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.NoWrap })
            using (var b = new SolidBrush(Mine ? Color.White : Theme.Current.Text))
                g.DrawString(_text, Font, b, new RectangleF(9, 7, Width - 20, Height - 12), sf);
        }
    }
}