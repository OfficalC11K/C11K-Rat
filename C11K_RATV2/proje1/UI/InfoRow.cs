// Dosya: UI/InfoRow.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App.Service
{
    public class InfoRow : Control
    {
        public string Glyph = "•";
        public string Caption = "";
        string _value = "—";

        static int _rowH;

        public static int RowHeight
        {
            get
            {
                if (_rowH > 0) return _rowH;
                using (var f = Ui.F(8f)) _rowH = f.Height + 7;
                return _rowH;
            }
        }

        public InfoRow()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            Font = Ui.F(8f);
            Height = RowHeight;
            BackColor = Color.Transparent;
        }

        public string Value { get { return _value; } set { _value = string.IsNullOrEmpty(value) ? "—" : value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int th = Font.Height;
            int y = Math.Max(0, (Height - th) / 2);

            using (var b = new SolidBrush(Theme.Current.Accent))
                g.DrawString(Glyph, Font, b, 2, y);

            int capW = Math.Max(48, (Width - 24) / 2);

            using (var b = new SolidBrush(Theme.Current.Muted))
            using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                g.DrawString(Caption, Font, b, new RectangleF(18, y, capW, th), sf);

            using (var b = new SolidBrush(Theme.Current.Text))
            using (var sf = new StringFormat { Alignment = StringAlignment.Far, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                g.DrawString(_value, Font, b, new RectangleF(18 + capW + 6, y, Math.Max(1, Width - 24 - capW - 6), th), sf);
        }
    }
}