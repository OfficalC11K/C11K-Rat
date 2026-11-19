// Dosya: Core/Ui.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace App.Service
{
    internal static class Ui
    {
        static string _family;

        public static string Family
        {
            get
            {
                if (_family != null) return _family;
                _family = "Segoe UI";
                try
                {
                    foreach (var f in FontFamily.Families)
                        if (string.Equals(f.Name, "Segoe UI Variable Text", StringComparison.OrdinalIgnoreCase)) { _family = f.Name; break; }
                }
                catch { }
                return _family;
            }
        }

        public static Font F(float size, FontStyle style = FontStyle.Regular)
        {
            try { return new Font(Family, size, style); } catch { return new Font("Segoe UI", size, style); }
        }

        public static GraphicsPath Round(Rectangle r, int radius)
        {
            int d = Math.Max(1, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static Color Mix(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        public static void Shadow(Graphics g, Rectangle r, int radius, int depth)
        {
            for (int i = depth; i >= 1; i--)
            {
                var rr = Rectangle.Inflate(r, i, i);
                rr.Offset(0, i / 2);
                using (var p = Round(rr, radius + i))
                using (var b = new SolidBrush(Color.FromArgb(6 + (depth - i) * 3, 0, 0, 0)))
                    g.FillPath(b, p);
            }
        }

        public static void Buffered(Control c)
        {
            if (c == null) return;
            try
            {
                var p = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                if (p != null) p.SetValue(c, true, null);
            }
            catch { }
        }

        public static string Ago(DateTime utc)
        {
            var d = DateTime.UtcNow - utc;
            if (d.TotalDays >= 1) return (int)d.TotalDays + "g " + d.Hours + "s";
            if (d.TotalHours >= 1) return (int)d.TotalHours + "s " + d.Minutes + "dk";
            if (d.TotalMinutes >= 1) return (int)d.TotalMinutes + "dk";
            return Math.Max(0, (int)d.TotalSeconds) + "sn";
        }

        public static string Short(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, Math.Max(0, max - 1)) + "…";
        }

        public static Rectangle Fit(Rectangle box, int w, int h)
        {
            if (w <= 0 || h <= 0) return box;
            double s = Math.Min((double)box.Width / w, (double)box.Height / h);
            int nw = Math.Max(1, (int)(w * s));
            int nh = Math.Max(1, (int)(h * s));
            return new Rectangle(box.X + (box.Width - nw) / 2, box.Y + (box.Height - nh) / 2, nw, nh);
        }

        public static string Alias(string pc)
        {
            if (string.IsNullOrEmpty(pc)) return "istemci";
            string b = Convert.ToBase64String(Encoding.UTF8.GetBytes(pc)).TrimEnd('=');
            if (b.Length <= 8) return b;
            return b.Substring(0, 4) + b.Substring(b.Length - 4, 4);
        }

        public static string Html(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        public static Color Tint(string key, Color a, Color b)
        {
            int h = 0;
            if (key != null) foreach (char c in key) h = unchecked(h * 31 + c);
            return Mix(a, b, ((h & 0x7FFFFFFF) % 100) / 100.0);
        }
    }
}