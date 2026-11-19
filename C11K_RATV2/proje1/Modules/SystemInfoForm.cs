// Dosya: Modules/SystemInfoForm.cs
//using App.Service.Modules;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class SystemInfoForm : ModuleForm, IRemoteModule
    {
        TextBox _txt;

        public SystemInfoForm() : base("Sistem Bilgisi")
        {
            Size = new Size(640, 480);
            _txt = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Theme.Current.Title,
                ForeColor = Theme.Current.Text,
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.None
            };
            Body.Controls.Add(_txt);
        }

        public void Initialize(ClientSession s, Action<string> send)
        {
            Title.Text = "Sistem Bilgisi - " + s.UserPc;
            Shown += (e, a) => { if (send != null) send("SYSINFO"); };
        }

        public void UpdateInfo(string info)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { UpdateInfo(info); }); } catch { } return; }
            _txt.Text = info;
        }
    }
}