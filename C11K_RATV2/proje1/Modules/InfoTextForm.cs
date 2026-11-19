// Dosya: Modules/InfoTextForm.cs
//using App.Service.Modules;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class InfoTextForm : ModuleForm, IRemoteModule
    {
        TextBox _txt;

        public InfoTextForm() : base("Bilgi")
        {
            Size = new Size(740, 560);
            _txt = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                BackColor = Theme.Current.Title,
                ForeColor = Theme.Current.Text,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None
            };
            Body.Controls.Add(_txt);
        }

        public void Initialize(ClientSession s, Action<string> send) { Title.Text = "Bilgi - " + s.UserPc; }
        public void SetTitle(string t) { Title.Text = t; }

        public void Set(string t)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { Set(t); }); } catch { } return; }
            _txt.Text = t;
        }
    }
}