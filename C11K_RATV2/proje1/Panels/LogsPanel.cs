// Dosya: Panels/LogsPanel.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace App.Service
{
    public class LogsPanel : BufferedPanel
    {
        public RichTextBox Box { get; private set; }
        PillButton _clear, _copy, _save, _ads;
        Panel _bar;
        int _writes;

        struct Line { public DateTime W; public string M; public Color C; }
        readonly List<Line> _queue = new List<Line>();
        System.Windows.Forms.Timer _flush;

        public LogsPanel()
        {
            BackColor = Theme.Current.Main;

            _bar = new BufferedPanel { Dock = DockStyle.Bottom, Height = 40, BackColor = Theme.Current.Side, Tag = "side" };
            _clear = new PillButton { Text = "Temizle", Location = new Point(10, 7), Size = new Size(96, 26), Radius = 6 };
            _clear.Palette(Theme.Current.Accent, Color.White);
            _clear.Click += (s, e) => { Box.Clear(); };
            _bar.Controls.Add(_clear);

            _copy = new PillButton { Text = "Kopyala", Location = new Point(114, 7), Size = new Size(96, 26), Radius = 6 };
            _copy.Palette(Theme.Current.Card, Theme.Current.Text);
            _copy.Click += (s, e) => { try { if (Box.TextLength > 0) Clipboard.SetText(Box.Text); } catch { } };
            _bar.Controls.Add(_copy);

            _save = new PillButton { Text = "Kaydet", Location = new Point(218, 7), Size = new Size(96, 26), Radius = 6 };
            _save.Palette(Theme.Current.Card, Theme.Current.Text);
            _save.Click += (s, e) =>
            {
                using (var sfd = new SaveFileDialog { FileName = "c11k_log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt", Filter = "Metin|*.txt" })
                {
                    if (sfd.ShowDialog() != DialogResult.OK) return;
                    try { File.WriteAllText(sfd.FileName, Box.Text); } catch { }
                }
            };
            _bar.Controls.Add(_save);

            _ads = new PillButton { Text = "ADS Oku", Location = new Point(322, 7), Size = new Size(96, 26), Radius = 6 };
            _ads.Palette(Theme.Current.Card, Theme.Current.Text);
            _ads.Click += (s, e) =>
            {
                string t = Ads.Read();
                if (string.IsNullOrEmpty(t)) { MessageBox.Show("ADS log bulunamadı veya boş.\n" + Ads.AdsPath, "ADS"); return; }
                try
                {
                    Box.SelectionStart = Box.TextLength;
                    Box.SelectionColor = Theme.Current.Accent;
                    Box.AppendText("\n===== ADS: C11K_LOGS.txt =====\n");
                    Box.SelectionColor = Theme.Current.Text;
                    Box.AppendText(t + "\n===== /ADS =====\n");
                    Box.SelectionStart = Box.TextLength;
                    Box.ScrollToCaret();
                }
                catch { }
            };
            _bar.Controls.Add(_ads);

            Box = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(6, 10, 8),
                ForeColor = Color.FromArgb(170, 220, 180),
                Font = new Font("Consolas", 8.5f),
                DetectUrls = false,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            Ui.Buffered(Box);

            Controls.Add(Box);
            Controls.Add(_bar);

            _flush = new System.Windows.Forms.Timer { Interval = 90 };
            _flush.Tick += (s, e) => Flush();
            _flush.Start();
        }

        public void Append(DateTime when, string msg, Color color)
        {
            lock (_queue)
            {
                if (_queue.Count > 4000) _queue.Clear();
                _queue.Add(new Line { W = when, M = msg, C = color });
            }
        }

        void Flush()
        {
            Line[] batch;
            lock (_queue)
            {
                if (_queue.Count == 0) return;
                batch = _queue.ToArray();
                _queue.Clear();
            }

            try
            {
                Box.SelectionStart = Box.TextLength;
                foreach (var l in batch)
                {
                    Box.SelectionColor = Theme.Current.Muted;
                    Box.AppendText(l.W.ToString("HH:mm:ss") + "  ");
                    Box.SelectionColor = l.C;
                    Box.AppendText(l.M + "\n");
                }
                Box.SelectionStart = Box.TextLength;
                Box.ScrollToCaret();
            }
            catch { }

            _writes += batch.Length;
            if (_writes >= 400) { _writes = 0; Trim(); }
        }

        void Trim()
        {
            if (Box.TextLength < 160000) return;
            try
            {
                int lines = Box.Lines.Length;
                if (lines < 1200) return;
                int cut = Box.GetFirstCharIndexFromLine(lines - 900);
                if (cut <= 0) return;
                Box.Select(0, cut);
                Box.SelectedText = "";
            }
            catch { }
        }

        public void ApplyTheme()
        {
            BackColor = Theme.Current.Main;
            _bar.BackColor = Theme.Current.Side;
            Box.BackColor = Theme.Current.IsLight ? Theme.Current.Title : Color.FromArgb(6, 10, 8);
            Box.ForeColor = Theme.Current.IsLight ? Theme.Current.Text : Color.FromArgb(170, 220, 180);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _flush != null) { _flush.Stop(); _flush.Dispose(); }
            base.Dispose(disposing);
        }
    }
}