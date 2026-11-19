// Dosya: Panels/SettingsPanel.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class SettingsPanel : BufferedPanel
    {
        public TextBox TxtPort, TxtTgToken, TxtTgChat;
        public PillButton BtnListen, BtnTgTest, BtnBot;
        public Label LblStatus, LblKey, LblBot;
        public event Action Toggle;
        public event Action ThemeChanged;

        readonly Dictionary<Button, Theme> _sw = new Dictionary<Button, Theme>();

        public SettingsPanel()
        {
            BackColor = Theme.Current.Main;
            AutoScroll = true;

            Controls.Add(new Label { Text = "Görünüm", Location = new Point(16, 16), AutoSize = true, Font = Ui.F(9.5f, FontStyle.Bold), ForeColor = Theme.Current.Text, BackColor = Color.Transparent });

            var flow = new FlowLayoutPanel { Location = new Point(16, 48), Size = new Size(700, 214), BackColor = Color.Transparent };
            Controls.Add(flow);

            foreach (var th in Theme.All)
            {
                var b = new Button
                {
                    Text = th.Name,
                    Size = new Size(94, 26),
                    Margin = new Padding(2),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = th.Main,
                    ForeColor = th.Text,
                    Font = Ui.F(7.5f, FontStyle.Bold),
                    Tag = "accent",
                    UseVisualStyleBackColor = false
                };
                b.FlatAppearance.BorderColor = th.Accent;
                var captured = th;
                b.Click += (s, e) => { Theme.Current = captured; if (ThemeChanged != null) ThemeChanged(); };
                flow.Controls.Add(b);
                _sw[b] = th;
            }

            Controls.Add(new Label { Text = "Dinleme", Location = new Point(16, 280), AutoSize = true, Font = Ui.F(9.5f, FontStyle.Bold), ForeColor = Theme.Current.Text, BackColor = Color.Transparent });

            TxtPort = new TextBox { Location = new Point(16, 306), Width = 96, Text = "4444", BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, BorderStyle = BorderStyle.FixedSingle, Font = Ui.F(8.5f) };
            Controls.Add(TxtPort);

            BtnListen = new PillButton { Text = "Dinle", Location = new Point(120, 304), Size = new Size(112, 30), Font = Ui.F(8.5f, FontStyle.Bold), Radius = 7 };
            BtnListen.Palette(Theme.Current.Accent, Color.White);
            BtnListen.Click += (s, e) => { if (Toggle != null) Toggle(); };
            Controls.Add(BtnListen);

            LblStatus = new Label { Text = "Durum: Kapalı", Location = new Point(240, 311), AutoSize = true, ForeColor = Theme.Current.Muted, BackColor = Color.Transparent, Font = Ui.F(8.5f) };
            Controls.Add(LblStatus);

            LblKey = new Label { Text = "Anahtar: -", Location = new Point(16, 340), AutoSize = true, ForeColor = Theme.Current.Muted, BackColor = Color.Transparent, Font = Ui.F(8f) };
            Controls.Add(LblKey);

            Controls.Add(new Label { Text = "Telegram", Location = new Point(16, 376), AutoSize = true, Font = Ui.F(9.5f, FontStyle.Bold), ForeColor = Theme.Current.Text, BackColor = Color.Transparent });

            Controls.Add(new Label { Text = "Bot token:", Location = new Point(16, 404), AutoSize = true, ForeColor = Theme.Current.Text, BackColor = Color.Transparent, Font = Ui.F(8.5f) });
            TxtTgToken = new TextBox { Location = new Point(128, 401), Width = 320, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, BorderStyle = BorderStyle.FixedSingle, Font = Ui.F(8.5f) };
            Controls.Add(TxtTgToken);

            Controls.Add(new Label { Text = "Chat ID:", Location = new Point(16, 432), AutoSize = true, ForeColor = Theme.Current.Text, BackColor = Color.Transparent, Font = Ui.F(8.5f) });
            TxtTgChat = new TextBox { Location = new Point(128, 429), Width = 320, BackColor = Theme.Current.InputBack, ForeColor = Theme.Current.InputFore, BorderStyle = BorderStyle.FixedSingle, Font = Ui.F(8.5f) };
            Controls.Add(TxtTgChat);

            BtnTgTest = new PillButton { Text = "Kaydet ve Test Et", Location = new Point(128, 464), Size = new Size(168, 30), Radius = 7 };
            BtnTgTest.Palette(Theme.Current.Ok, Color.White);
            Controls.Add(BtnTgTest);

            BtnBot = new PillButton { Text = "Botu Başlat", Location = new Point(304, 464), Size = new Size(144, 30), Radius = 7 };
            BtnBot.Palette(Theme.Current.Accent, Color.White);
            Controls.Add(BtnBot);

            LblBot = new Label { Text = "Bot: kapalı", Location = new Point(128, 500), AutoSize = true, ForeColor = Theme.Current.Muted, BackColor = Color.Transparent, Font = Ui.F(8f) };
            Controls.Add(LblBot);

            Controls.Add(new Label
            {
                Text = "Chat ID boş bırakıp bota Telegram'dan /st" + "art yaz, sahip olarak kaydedilir.\nGrup için: /id ile ID'leri al, /allow ile ekle. /allowlist ve /deny de var.\nAES anahtarı Builder > Kimlik sekmesindedir, client ile aynı olmalı.\n\nscripts/collect.ps1 dosyasını uygulama klasöründe tut — /pass ve 'Veriler' butonu onu kullanır.",
                Location = new Point(16, 532),
                Size = new Size(640, 84),
                ForeColor = Theme.Current.Muted,
                BackColor = Color.Transparent,
                Font = Ui.F(8f)
            });
        }

        public void ApplyTheme()
        {
            BackColor = Theme.Current.Main;
            DarkForm.Recolor(this);

            foreach (var kv in _sw)
            {
                kv.Key.BackColor = kv.Value.Main;
                kv.Key.ForeColor = kv.Value.Text;
                kv.Key.FlatAppearance.BorderColor = kv.Value.Accent;
            }

            BtnListen.Palette(Theme.Current.Accent, Color.White);
            BtnBot.Palette(Theme.Current.Accent, Color.White);
            BtnTgTest.Palette(Theme.Current.Ok, Color.White);
        }
    }
}