// Dosya: Modules/ClipboardForm.cs
//using App.Service.Modules;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace App.Service
{
    public class ClipboardForm : ModuleForm, IRemoteModule
    {
        TextBox _log;
        Action<string> _send;
        bool _started;

        public ClipboardForm() : base("Pano")
        {
            Size = new Size(800, 560);

            _log = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(6, 10, 8),
                ForeColor = Color.FromArgb(94, 240, 132),
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.None
            };

            var bar = new BufferedPanel { Dock = DockStyle.Bottom, Height = S(44), BackColor = Theme.Current.Side, Padding = new Padding(S(6)), Tag = "side" };
            var set = new PillButton { Dock = DockStyle.Right, Width = S(124), Text = "Panoya Yaz", Radius = 6 };
            set.Palette(Theme.Current.Ok, Color.White);
            set.Click += (s, e) =>
            {
                if (_send == null) return;
                string text = Input("Panoya yazılacak metin:", "");
                if (!string.IsNullOrEmpty(text)) _send("CLIPSET|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(text)));
            };

            var get = new PillButton { Dock = DockStyle.Right, Width = S(124), Text = "Şimdi Oku", Radius = 6 };
            get.Palette(Theme.Current.Card, Theme.Current.Text);
            get.Click += (s, e) => { if (_send != null) _send("CLIP|GET"); };

            bar.Controls.Add(set);
            bar.Controls.Add(get);

            Body.Controls.Add(_log);
            Body.Controls.Add(bar);
        }

        public void Initialize(ClientSession s, Action<string> send)
        {
            _send = send;
            Title.Text = "Pano - " + s.UserPc;
            Shown += (e, a) => { if (_started) return; _started = true; if (_send != null) _send("CLIP|START"); };
            FormClosed += (e, a) => { if (_started) { _started = false; try { if (_send != null) _send("CLIP|STOP"); } catch { } } };
        }

        string Input(string label, string def)
        {
            using (var f = new Form())
            {
                f.Text = "Girdi";
                f.Size = new Size(424, 170);
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.BackColor = Theme.Current.Main;
                f.ForeColor = Theme.Current.Text;
                f.Font = Ui.F(9f);

                var l = new Label { Text = label, Left = 16, Top = 14, AutoSize = true };
                var t = new TextBox { Left = 16, Top = 38, Width = 380, Height = 48, Multiline = true, Text = def, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore };
                var ok = new PillButton { Text = "Tamam", Left = 292, Width = 104, Top = 96, DialogResult = DialogResult.OK, Radius = 6 };
                ok.Palette(Theme.Current.Accent, Color.White);

                f.Controls.Add(l);
                f.Controls.Add(t);
                f.Controls.Add(ok);
                f.AcceptButton = ok;

                return f.ShowDialog() == DialogResult.OK ? t.Text : null;
            }
        }

        public void Append(string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { Append(text); }); } catch { } return; }
            _log.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + "\r\n---\r\n");
            _log.SelectionStart = _log.Text.Length;
            _log.ScrollToCaret();
        }
    }
}