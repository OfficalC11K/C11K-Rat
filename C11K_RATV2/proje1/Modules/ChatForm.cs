// Dosya: Modules/ChatForm.cs
//using App.Service.Modules;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace App.Service
{
    public class ChatForm : ModuleForm, IRemoteModule
    {
        FlowLayoutPanel _flow;
        TextBox _in;
        Label _head;
        Action<string> _send;
        string _me = "Operatör";
        string _peer = "İstemci";
        bool _closed;

        public ChatForm() : base("Sohbet")
        {
            Size = new Size(620, 680);
            MinimumSize = new Size(440, 440);

            _head = new Label
            {
                Dock = DockStyle.Top,
                Height = S(36),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(S(14), 0, 0, 0),
                BackColor = Theme.Current.Title,
                ForeColor = Theme.Current.Ok,
                Font = Ui.F(8.5f, FontStyle.Bold),
                Text = "Bağlanıyor…"
            };

            _flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Theme.Current.Title,
                Padding = new Padding(8)
            };

            _in = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, Font = Ui.F(9f) };

            var bar = new BufferedPanel { Dock = DockStyle.Bottom, Height = S(48), BackColor = Theme.Current.Side, Padding = new Padding(S(8)), Tag = "side" };
            var btnSend = new PillButton { Dock = DockStyle.Right, Width = S(92), Text = "Gönder", Margin = new Padding(8, 0, 0, 0), Radius = 7 };
            btnSend.Palette(Theme.Current.Accent, Color.White);
            btnSend.Click += (s, e) => Push();

            bar.Controls.Add(_in);
            bar.Controls.Add(btnSend);

            Body.Controls.Add(_flow);
            Body.Controls.Add(bar);
            Body.Controls.Add(_head);

            _in.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Push(); } };
            FormClosing += (s, e) => CloseRemote();
        }

        public void Initialize(ClientSession s, Action<string> send)
        {
            _send = send;
            _peer = Ui.Alias(s.UserPc);
            Title.Text = "Sohbet - " + _peer;
            _head.Text = _peer + "  •  " + s.Ip + "   —   pencereyi kapatınca istemcideki sohbet de kapanır";
            Shown += (e, a) => { if (_send != null) _send("CHAT|OPEN|" + B64(_me)); };
        }

        static string B64(string s) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(s ?? "")); }

        public void SetOperator(string name)
        {
            _me = string.IsNullOrWhiteSpace(name) ? "Operatör" : name.Trim();
            Title.Text = "Sohbet - " + _peer + " (" + _me + ")";
            Add("— " + _me + " olarak bağlandınız —", false, true);
        }

        void CloseRemote()
        {
            if (_closed) return;
            _closed = true;
            try { if (_send != null) _send("CHAT|CLOSE"); } catch { }
        }

        void Push()
        {
            string m = _in.Text.Trim();
            if (m.Length == 0 || _send == null) return;
            _in.Clear();
            Add("Ben: " + m, true, false);
            _send("CHAT|MSG|" + B64(m));
        }

        public void AppendPeer(string text) { Add(_peer + ": " + text, false, false); }

        void Add(string text, bool mine, bool system)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { Add(text, mine, system); }); } catch { } return; }

            int max = Math.Max(180, _flow.ClientSize.Width - 26);

            if (system)
            {
                var lbl = new Label
                {
                    Text = text,
                    AutoSize = false,
                    Width = max,
                    Height = 20,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Theme.Current.Muted,
                    Font = Ui.F(7.5f),
                    Margin = new Padding(0, 4, 0, 8)
                };
                _flow.Controls.Add(lbl);
            }
            else
            {
                var host = new BufferedPanel { Width = max, BackColor = Color.Transparent, Margin = new Padding(0, 2, 0, 2) };
                var b = new Bubble(text, mine);
                b.Measure(max);
                b.Left = mine ? Math.Max(0, max - b.Width) : 0;
                b.Top = 0;
                host.Height = b.Height;
                host.Controls.Add(b);
                _flow.Controls.Add(host);
            }

            try { _flow.ScrollControlIntoView(_flow.Controls[_flow.Controls.Count - 1]); } catch { }
        }
    }
}