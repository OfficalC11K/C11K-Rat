// Dosya: Modules/ConsoleForm.cs
//using App.Service.Modules;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class ConsoleForm : ModuleForm, IRemoteModule
    {
        TextBox _out, _in;
        CheckBox _all;
        Action<string> _send;
        Func<string, int> _broadcast;
        readonly List<string> _hist = new List<string>();
        int _hIdx = -1;

        public ConsoleForm() : base("Komut Konsolu")
        {
            Size = new Size(840, 580);

            _out = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(6, 10, 8),
                ForeColor = Color.FromArgb(94, 240, 132),
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None
            };

            var bar = new BufferedPanel { Dock = DockStyle.Bottom, Height = S(40), BackColor = Theme.Current.Side, Padding = new Padding(S(6)), Tag = "side" };
            _in = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(10, 14, 12), ForeColor = Color.FromArgb(94, 240, 132), Font = new Font("Consolas", 9f) };
            _all = new CheckBox { Dock = DockStyle.Right, Width = S(156), Text = "Tüm istemcilere", ForeColor = Theme.Current.Text, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft, Font = Ui.F(8.5f) };

            bar.Controls.Add(_in);
            bar.Controls.Add(_all);

            Body.Controls.Add(_out);
            Body.Controls.Add(bar);

            _in.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Run();
                }
                else if (e.KeyCode == Keys.Up && _hist.Count > 0)
                {
                    e.SuppressKeyPress = true;
                    _hIdx = _hIdx < 0 ? _hist.Count - 1 : Math.Max(0, _hIdx - 1);
                    _in.Text = _hist[_hIdx];
                    _in.SelectionStart = _in.TextLength;
                }
                else if (e.KeyCode == Keys.Down && _hist.Count > 0 && _hIdx >= 0)
                {
                    e.SuppressKeyPress = true;
                    _hIdx = Math.Min(_hist.Count - 1, _hIdx + 1);
                    _in.Text = _hist[_hIdx];
                    _in.SelectionStart = _in.TextLength;
                }
            };

            Write("Komut yazıp Enter'a bas. Örnek: SHOT, SYSINFO, VOL|MUTE, LOCK, DRIVES, TOAST|merhaba");
        }

        public void Initialize(ClientSession s, Action<string> send)
        {
            _send = send;
            Title.Text = "Konsol - " + s.UserPc;
        }

        public void SetBroadcast(Func<string, int> b) { _broadcast = b; }

        void Run()
        {
            string cmd = _in.Text.Trim();
            if (cmd.Length == 0) return;
            _in.Clear();
            _hIdx = -1;
            _hist.Add(cmd);

            if (_all.Checked && _broadcast != null)
            {
                int n = _broadcast(cmd);
                Write("→ tüm istemciler (" + n + "): " + cmd);
                return;
            }

            Write("→ " + cmd);
            if (_send != null) _send(cmd);
        }

        public void Write(string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { Write(text); }); } catch { } return; }
            _out.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + text + "\r\n");
            _out.SelectionStart = _out.Text.Length;
            _out.ScrollToCaret();
        }
    }
}