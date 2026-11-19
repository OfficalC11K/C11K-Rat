// Dosya: Modules/ScriptForm.cs
//using App.Service.Modules;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace App.Service
{
    public class ScriptForm : ModuleForm, IRemoteModule
    {
        TextBox _code, _out;
        ComboBox _kind;
        Action<string> _send;

        public ScriptForm() : base("Script")
        {
            Size = new Size(920, 660);

            var top = new BufferedPanel { Height = S(44), Dock = DockStyle.Top, BackColor = Theme.Current.Side, Padding = new Padding(S(8)), Tag = "side" };
            _kind = new ComboBox { Dock = DockStyle.Left, Width = S(136), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, Font = Ui.F(8.5f) };
            _kind.Items.AddRange(new object[] { "PowerShell", "Batch" });
            _kind.SelectedIndex = 0;

            var run = new PillButton { Dock = DockStyle.Right, Width = S(112), Text = "Çalıştır", Radius = 7 };
            run.Palette(Theme.Current.Ok, Color.White);
            run.Click += (s, e) =>
            {
                if (_send == null) return;
                _out.Clear();
                string kind = _kind.SelectedIndex == 0 ? "PS" : "BAT";
                _send("SCRIPT|" + kind + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(_code.Text)));
            };

            top.Controls.Add(_kind);
            top.Controls.Add(run);

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterWidth = S(6) };
            split.Panel1.BackColor = Theme.Current.Title;
            split.Panel2.BackColor = Color.FromArgb(6, 10, 8);

            _code = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                AcceptsTab = true,
                BackColor = Theme.Current.Title,
                ForeColor = Theme.Current.Text,
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.None
            };

            _out = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                BackColor = Color.FromArgb(6, 10, 8),
                ForeColor = Color.FromArgb(94, 240, 132),
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None
            };

            split.Panel1.Controls.Add(_code);
            split.Panel2.Controls.Add(_out);

            Body.Controls.Add(split);
            Body.Controls.Add(top);
        }

        public void Initialize(ClientSession s, Action<string> send)
        {
            _send = send;
            Title.Text = "Script - " + s.UserPc;
        }

        public void SetOutput(string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { SetOutput(text); }); } catch { } return; }
            _out.Text = text;
        }

        public void SetCode(string code)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { SetCode(code); }); } catch { } return; }
            _code.Text = code;
        }
    }
}