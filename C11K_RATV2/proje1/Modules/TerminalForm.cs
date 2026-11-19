// Dosya: Modules/TerminalForm.cs
//using App.Service.Modules;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class TerminalForm : ModuleForm, IRemoteModule
    {
        TextBox _out, _in;
        Action<string> _send;

        public TerminalForm() : base("Terminal")
        {
            _out = new TextBox
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

            _in = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 14, 12),
                ForeColor = Color.FromArgb(94, 240, 132),
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.None
            };

            var bottom = new BufferedPanel { Height = S(36), Dock = DockStyle.Bottom, Padding = new Padding(S(8)), BackColor = Theme.Current.Side, Tag = "side" };
            bottom.Controls.Add(_in);

            Body.Controls.Add(_out);
            Body.Controls.Add(bottom);

            _in.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                string cmd = _in.Text;
                _in.Clear();
                Append("> " + cmd);
                if (_send != null) _send("SHELL|" + cmd);
            };
        }

        public void Initialize(ClientSession s, Action<string> send)
        {
            _send = send;
            Title.Text = "Terminal - " + s.UserPc;
        }

        public void Append(string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { Append(text); }); } catch { } return; }
            _out.AppendText(text.Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n");
            _out.SelectionStart = _out.Text.Length;
            _out.ScrollToCaret();
        }
    }
}