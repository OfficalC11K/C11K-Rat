// Dosya: Forms/MessageBoxForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class MessageBoxForm : Form
    {
        TextBox _t1, _t2;
        ComboBox _cb;

        public string MessageTitle, MessageText, SelectedIcon;

        public MessageBoxForm()
        {
            Text = "Mesaj Kutusu";
            Size = new Size(430, 330);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Current.Main;
            ForeColor = Theme.Current.Text;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = Ui.F(9f);

            var l1 = new Label { Text = "Başlık:", Left = 16, Top = 14, AutoSize = true };
            _t1 = new TextBox { Left = 16, Top = 38, Width = 380, Text = "Sistem Uyarısı", BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, BorderStyle = BorderStyle.FixedSingle };
            var l2 = new Label { Text = "Mesaj:", Left = 16, Top = 68, AutoSize = true };
            _t2 = new TextBox { Left = 16, Top = 90, Width = 380, Height = 96, Multiline = true, Text = "Bu bir test mesajıdır.", BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, BorderStyle = BorderStyle.FixedSingle };
            var l3 = new Label { Text = "İkon:", Left = 16, Top = 206, AutoSize = true };
            _cb = new ComboBox { Left = 60, Top = 203, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore };
            _cb.Items.AddRange(new object[] { "Information", "Warning", "Error", "Question" });
            _cb.SelectedIndex = 0;

            var ok = new PillButton { Text = "Gönder", Left = 292, Width = 104, Top = 202, DialogResult = DialogResult.OK, Radius = 6 };
            ok.Palette(Theme.Current.Accent, Color.White);

            Controls.Add(l1);
            Controls.Add(_t1);
            Controls.Add(l2);
            Controls.Add(_t2);
            Controls.Add(l3);
            Controls.Add(_cb);
            Controls.Add(ok);
            AcceptButton = ok;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            MessageTitle = string.IsNullOrWhiteSpace(_t1.Text) ? "Sistem Uyarısı" : _t1.Text;
            MessageText = _t2.Text ?? "";
            SelectedIcon = Convert.ToString(_cb.SelectedItem);
            base.OnFormClosing(e);
        }
    }
}