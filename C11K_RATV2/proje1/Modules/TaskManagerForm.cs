// Dosya: Modules/TaskManagerForm.cs
//using App.Service.Modules;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class TaskManagerForm : ModuleForm, IRemoteModule
    {
        DataGridView _grid;
        System.Windows.Forms.Timer _timer;
        Action<string> _send;

        public TaskManagerForm() : base("Süreçler")
        {
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Theme.Current.Main,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                Font = Ui.F(8.5f)
            };
            Ui.Buffered(_grid);

            _grid.RowTemplate.Height = S(26);
            _grid.GridColor = Theme.Current.Border;
            _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            _grid.ColumnHeadersHeight = S(30);
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            _grid.Columns.Add("PID", "PID");
            _grid.Columns.Add("N", "İşlem");
            _grid.Columns.Add("T", "Pencere");
            _grid.Columns[0].FillWeight = 12;
            _grid.Columns[1].FillWeight = 38;
            _grid.Columns[2].FillWeight = 50;

            _grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                e.CellStyle.BackColor = e.RowIndex % 2 == 1 ? Theme.Current.RowAlt : Theme.Current.Main;
                e.CellStyle.ForeColor = Theme.Current.Text;
                e.CellStyle.SelectionBackColor = Theme.Current.Accent;
                e.CellStyle.SelectionForeColor = Color.White;
            };

            var bottom = new BufferedPanel { Height = S(48), Dock = DockStyle.Bottom, BackColor = Theme.Current.Side, Tag = "side" };

            var r = new PillButton { Text = "Yenile", Location = new Point(S(8), S(9)), Size = new Size(S(100), S(30)), Radius = 6 };
            r.Palette(Theme.Current.Accent, Color.White);
            r.Click += (s, e) => { if (_send != null) _send("PROC|LIST"); };

            var k = new PillButton { Text = "Sonlandır", Location = new Point(S(116), S(9)), Size = new Size(S(120), S(30)), Radius = 6 };
            k.Palette(Theme.Current.Bad, Color.White);
            k.Click += (s, e) =>
            {
                if (_send == null) return;
                if (_grid.SelectedRows.Count == 0) return;
                string pid = Convert.ToString(_grid.SelectedRows[0].Cells[0].Value);
                if (MessageBox.Show("PID " + pid + " sonlandırılsın mı?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _send("PROC|KILL|" + pid);
                    _send("PROC|LIST");
                }
            };

            var st = new PillButton { Text = "Başlat", Location = new Point(S(244), S(9)), Size = new Size(S(100), S(30)), Radius = 6 };
            st.Palette(Theme.Current.Ok, Color.White);
            st.Click += (s, e) =>
            {
                if (_send == null) return;
                string line = Input("Çalıştırılacak dosya / komut:", "notepad.exe");
                if (!string.IsNullOrWhiteSpace(line)) { _send("PROC|START|" + line); _send("PROC|LIST"); }
            };

            bottom.Controls.Add(r);
            bottom.Controls.Add(k);
            bottom.Controls.Add(st);

            Body.Controls.Add(_grid);
            Body.Controls.Add(bottom);

            _timer = new System.Windows.Forms.Timer { Interval = 4000 };
            _timer.Tick += (s, e) => { if (_send != null) _send("PROC|LIST"); };
            Shown += (e, a) => { _timer.Start(); if (_send != null) _send("PROC|LIST"); };
            FormClosed += (s, e) => _timer.Stop();
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

        public void Initialize(ClientSession s, Action<string> send)
        {
            _send = send;
            Title.Text = "Süreçler - " + s.UserPc;
        }

        public void UpdateProcesses(string data)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((MethodInvoker)delegate { UpdateProcesses(data); }); } catch { } return; }
            string keep = _grid.SelectedRows.Count > 0 ? Convert.ToString(_grid.SelectedRows[0].Cells[0].Value) : null;
            _grid.SuspendLayout();
            _grid.Rows.Clear();
            foreach (string row in data.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] p = row.Split('~');
                if (p.Length < 2) continue;
                int i = _grid.Rows.Add(p[0], p[1], p.Length > 2 ? p[2] : "");
                if (keep != null && p[0] == keep) _grid.Rows[i].Selected = true;
            }
            _grid.ResumeLayout();
        }
    }
}