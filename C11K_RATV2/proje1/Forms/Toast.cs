// Dosya: Forms/Toast.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class Toast : Form
    {
        static readonly List<Toast> Open = new List<Toast>();

        public Toast(string title, string text, Color accent)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            Size = new Size(320, 72);
            BackColor = Theme.Current.Card;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            Opacity = 0;
            Font = Ui.F(9f);

            var bar = new Panel { Dock = DockStyle.Left, Width = 3, BackColor = accent };
            var t1 = new Label { Text = title, Left = 14, Top = 10, Width = 286, Height = 17, Font = Ui.F(8.5f, FontStyle.Bold), ForeColor = Theme.Current.Text, BackColor = Color.Transparent, AutoEllipsis = true };
            var t2 = new Label { Text = text, Left = 14, Top = 31, Width = 290, Height = 30, ForeColor = Theme.Current.Muted, BackColor = Color.Transparent, AutoEllipsis = true };

            Controls.Add(bar);
            Controls.Add(t1);
            Controls.Add(t2);

            Paint += (s, e) =>
            {
                using (var p = new Pen(Ui.Mix(Theme.Current.Card, accent, 0.5))) e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            };

            Click += (s, e) => Close();
            foreach (Control c in Controls) c.Click += (s, e) => Close();

            int slot;
            lock (Open) { slot = Open.Count; Open.Add(this); }

            var area = Screen.PrimaryScreen.WorkingArea;
            int y = area.Bottom - Height - 14 - slot * (Height + 6);
            Location = new Point(area.Right - Width - 14, Math.Max(area.Top + 8, y));

            var fade = new System.Windows.Forms.Timer { Interval = 18 };
            int step = 0;
            fade.Tick += (s, e) => { step++; Opacity = Math.Min(1, step / 12.0); if (step >= 12) fade.Stop(); };
            Shown += (s, e) => fade.Start();

            var life = new System.Windows.Forms.Timer { Interval = 4200 };
            life.Tick += (s, e) => { life.Stop(); Close(); };
            Shown += (s, e) => life.Start();

            FormClosed += (s, e) => { lock (Open) Open.Remove(this); };
        }
    }
}