// Dosya: Forms/PreviewForm.cs
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class PreviewForm : DarkForm
    {
        public PreviewForm(Image img, string title)
        {
            BuildChrome(title);
            Padding = new Padding(0, S(TitleHeight), 0, 0);
            Size = new Size(900, 600);
            MinimumSize = new Size(420, 320);
            MaximizeBox = true;

            var pic = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
            Ui.Buffered(pic);
            pic.Image = img;
            Controls.Add(pic);
            BringChromeToFront();
            FormClosed += (s, e) => { if (pic.Image != null) { pic.Image.Dispose(); pic.Image = null; } };
        }
    }
}