// Dosya: Core/Theme.cs
using System.Drawing;

namespace App.Service
{
    public class Theme
    {
        public string Name;
        public Color Main, Side, Title, Text, Accent, Hover, Card, Stripe, Border, Muted, Ok, Warn, Bad;
        public Color CardTop, CardBottom, SelTop, SelBottom, Glow;

        static Theme M(string n, int[] m, int[] s, int[] t, int[] x, int[] a)
        {
            var main = Color.FromArgb(m[0], m[1], m[2]);
            var side = Color.FromArgb(s[0], s[1], s[2]);
            var accent = Color.FromArgb(a[0], a[1], a[2]);
            var text = Color.FromArgb(x[0], x[1], x[2]);
            var card = Ui.Mix(main, side, 0.55);
            return new Theme
            {
                Name = n,
                Main = main,
                Side = side,
                Title = Color.FromArgb(t[0], t[1], t[2]),
                Text = text,
                Accent = accent,
                Hover = Ui.Mix(side, accent, 0.35),
                Card = card,
                CardTop = Ui.Mix(card, Color.White, 0.045),
                CardBottom = Ui.Mix(card, Color.Black, 0.05),
                SelTop = Ui.Mix(main, accent, 0.42),
                SelBottom = Ui.Mix(main, accent, 0.66),
                Glow = Ui.Mix(accent, Color.White, 0.45),
                Stripe = Ui.Mix(main, Color.White, 0.035),
                Border = Ui.Mix(main, accent, 0.28),
                Muted = Ui.Mix(text, main, 0.27),
                Ok = Color.FromArgb(88, 208, 130),
                Warn = Color.FromArgb(230, 176, 88),
                Bad = Color.FromArgb(224, 96, 96)
            };
        }

        public static readonly Theme[] All =
        {
            M("Slate",   new[]{17,19,24},   new[]{23,26,33},   new[]{27,31,39},   new[]{198,204,216}, new[]{86,120,255}),
            M("Beyaz",   new[]{244,245,247},new[]{231,233,237},new[]{236,238,242},new[]{38,42,50},    new[]{32,102,200}),
            M("Kırmızı", new[]{24,15,16},   new[]{33,20,22},   new[]{36,22,24},   new[]{238,214,214}, new[]{224,72,72}),
            M("Pembe",   new[]{26,17,22},   new[]{36,23,30},   new[]{40,25,33},   new[]{240,214,226}, new[]{232,96,160}),
            M("Mavi",    new[]{14,18,30},   new[]{20,25,41},   new[]{23,28,46},   new[]{206,220,246}, new[]{64,132,255}),
            M("Yeşil",   new[]{15,24,19},   new[]{20,32,26},   new[]{23,36,29},   new[]{206,238,214}, new[]{62,196,110}),
            M("Turuncu", new[]{26,20,15},   new[]{36,28,20},   new[]{40,31,22},   new[]{244,224,206}, new[]{240,140,56}),
            M("Mor",     new[]{22,17,28},   new[]{30,23,38},   new[]{34,26,43},   new[]{228,214,244}, new[]{158,102,232}),
            M("Sarı",    new[]{26,25,15},   new[]{36,34,20},   new[]{40,38,22},   new[]{240,238,204}, new[]{214,196,54}),
            M("Turkuaz", new[]{13,24,25},   new[]{18,33,34},   new[]{21,37,38},   new[]{204,240,240}, new[]{52,196,196}),
            M("Lacivert",new[]{14,16,32},   new[]{19,22,43},   new[]{22,25,48},   new[]{206,212,244}, new[]{76,88,224}),
            M("Bordo",   new[]{22,15,18},   new[]{31,20,24},   new[]{35,22,27},   new[]{242,212,220}, new[]{186,54,84}),
            M("Gri",     new[]{26,26,28},   new[]{35,35,38},   new[]{39,39,42},   new[]{212,212,216}, new[]{138,138,146}),
            M("Zümrüt",  new[]{14,24,21},   new[]{19,33,29},   new[]{22,37,32},   new[]{204,240,228}, new[]{46,192,140}),
            M("Altın",   new[]{25,23,15},   new[]{35,32,20},   new[]{39,36,22},   new[]{242,234,206}, new[]{196,168,60}),
            M("Gül",     new[]{25,17,20},   new[]{35,23,27},   new[]{39,26,30},   new[]{242,214,222}, new[]{220,110,140}),
            M("Kahve",   new[]{23,19,16},   new[]{32,26,21},   new[]{36,29,24},   new[]{238,224,212}, new[]{164,112,72}),
            M("Mint",    new[]{14,24,22},   new[]{19,33,30},   new[]{22,37,34},   new[]{206,240,232}, new[]{88,208,180}),
            M("Neon",    new[]{12,12,13},   new[]{18,18,19},   new[]{21,21,22},   new[]{214,242,214}, new[]{78,232,120})
        };

        public static Theme Current = All[0];

        public bool IsLight { get { return (Main.R * 299 + Main.G * 587 + Main.B * 114) / 1000 > 128; } }

        public Color InputBack { get { return IsLight ? Color.White : Ui.Mix(Main, Color.White, 0.05); } }
        public Color InputFore { get { return IsLight ? Color.Black : Color.White; } }
        public Color RowAlt { get { return IsLight ? Ui.Mix(Main, Color.Black, 0.03) : Stripe; } }
    }
}