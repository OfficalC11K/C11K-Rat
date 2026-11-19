// Dosya: UI/SearchBox.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App.Service
{
    public class SearchBox : BufferedPanel
    {
        readonly TextBox _box;
        bool _focus;

        public const string Placeholder = "Ara…";

        public SearchBox()
        {
            Height = 22;
            BackColor = Theme.Current.InputBack;
            Padding = new Padding(6, 3, 6, 3);
            Cursor = Cursors.IBeam;

            _box = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                BackColor = Theme.Current.InputBack,
                ForeColor = Theme.Current.InputFore,
                Font = Ui.F(8.5f),
                Text = Placeholder
            };
            _box.GotFocus += (s, e) => { _focus = true; if (_box.Text == Placeholder) _box.Text = ""; Invalidate(); };
            _box.LostFocus += (s, e) => { _focus = false; if (_box.TextLength == 0) _box.Text = Placeholder; Invalidate(); };
            _box.TextChanged += (s, e) => { if (TextChanged != null) TextChanged(this, EventArgs.Empty); Invalidate(); };
            Controls.Add(_box);
        }

        public new event EventHandler TextChanged;

        public override string Text
        {
            get { return _box.Text; }
            set { _box.Text = string.IsNullOrEmpty(value) && !_box.Focused ? Placeholder : value; }
        }

        public int TextLength { get { return _box.TextLength; } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));

            using (var p = Ui.Round(r, 4))
            using (var b = new SolidBrush(_focus ? Ui.Mix(Theme.Current.InputBack, Theme.Current.Accent, 0.10) : Theme.Current.InputBack))
                g.FillPath(b, p);

            using (var p = Ui.Round(r, 4))
            using (var pen = new Pen(_focus ? Theme.Current.Accent : Theme.Current.Border))
                g.DrawPath(pen, p);

            if (_focus)
                using (var b = new SolidBrush(Theme.Current.Accent))
                    g.FillRectangle(b, 5, Height - 2, Math.Max(1, Width - 10), 1);

            base.OnPaint(e);
        }
    }
}