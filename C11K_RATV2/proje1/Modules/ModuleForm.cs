// Dosya: Modules/ModuleForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class ModuleForm : DarkForm
    {
        public readonly Panel Body;

        public ModuleForm(string title)
        {
            BuildChrome(title);
            Padding = new Padding(0, S(TitleHeight), 0, 0);
            Size = new Size(880, 600);
            MinimumSize = new Size(520, 380);
            MaximizeBox = true;
            ShowInTaskbar = true;

            Body = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Theme.Current.Main, Tag = "side" };
            Controls.Add(Body);
            BringChromeToFront();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyTheme();
        }

        public override void ApplyTheme()
        {
            base.ApplyTheme();
            if (Body != null) Body.BackColor = Theme.Current.Main;
        }
    }
}