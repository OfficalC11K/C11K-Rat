// Dosya: Modules/StartupManagerForm.cs
//using App.Service.Modules;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class StartupManagerForm : ModuleForm, IRemoteModule
    {
        ListView _list;
        Action<string> _send;

        public StartupManagerForm() : base("Başlangıç")
        {
            Size = new Size(800, 540);

            _list = new BufferedListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, BackColor = Theme.Current.Main, ForeColor = Theme.Current.Text, BorderStyle = BorderStyle.None, Font = Ui.F(8.5f) };
            _list.Columns.Add("Ad", S(220));
            _list.Columns.Add("Yol", S(520));

            var bottom = new BufferedPanel { Height = S(48), Dock = DockStyle.Bottom, BackColor = Theme.Current.Side, Tag = "side" };

            var add = new PillButton { Text = "Ekle", Location = new Point(S(8), S(9)), Size = new Size(S(100), S(30)), Radius = 6 };
            add.Palette(Theme.Current.Accent, Color.White);
            add.Click += (s, e) =>
            {
                if (_send == null) return;
                string name = Input("Başlangıç adı:", "OneDrive");
                if (string.IsNullOrEmpty(name)) return;
                string path = Input("Dosya yolu:", "C:\\Windows\\System32\\calc.exe");
                if (string.IsNullOrEmpty(path)) return;
                _send("STARTUP|ADD|" + name.Replace("|", "/") + "|" + path);
            };

            var del = new PillButton { Text = "Kaldır", Location = new Point(S(116), S(9)), Size = new Size(S(100), S(30)), Radius = 6 };
            del.Palette(Theme.Current.Bad, Color.White);
            del.Click += (s, e) =>
            {
                if (_send == null) return;
                if (_list.SelectedItems.Count == 0) return;
                _send("STARTUP|DEL|" + _list.SelectedItems[0].Text);
            };

            bottom.Controls.Add(add);
            bottom.Controls.Add(del);

            Body.Controls.Add(_list);
            Body.Controls.Add(bottom);
        }

        public void Initialize(ClientSession s, Action<string> send)
        {
            _send = send;
            Title.Text = "Başlangıç - " + s.UserPc;
            Shown += (e, a) => { if (_send != null) _send("STARTUP|LIST"); };
        }

        string Input(string label, string def)
        {
            using (var f = new Form())
            {
                f.Text = "Girdi";
                f.Size = new Size(404, 144);
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.BackColor = Theme.Current.Main;
                f.ForeColor = Theme.Current.Text;
                f.Font = Ui.F(9f);

                var l = new Label { Text = label, Left = 16, Top = 14, AutoSize = true };
                var t = new TextBox { Left = 16, Top = 38, Width = 360, Text = def, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore };
                var ok = new PillButton { Text = "Tamam", Left = 272, Width = 104, Top = 72, DialogResult = DialogResult.OK, Radius = 6 };
                ok.Palette(Theme.Current.Accent, Color.White);

                f.Controls.Add(l);
                f.Controls.Add(t);
                f.Controls.Add(ok);
                f.AcceptButton = ok;

                return f.ShowDialog() == DialogResult.OK ? t.Text : null;
            }
        }

        public void UpdateStartup(string data)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { UpdateStartup(data); }); } catch { } return; }
            if (data == "OK") { if (_send != null) _send("STARTUP|LIST"); return; }
            if (data.StartsWith("ERROR|")) { MessageBox.Show(data.Substring(6), "Hata"); return; }
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (string row in data.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] p = row.Split('~');
                if (p.Length < 2) continue;
                var lvi = new ListViewItem(p[0]);
                lvi.SubItems.Add(p[1]);
                _list.Items.Add(lvi);
            }
            _list.EndUpdate();
        }
    }
}