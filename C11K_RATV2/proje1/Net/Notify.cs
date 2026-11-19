// Dosya: Net/Notify.cs
using System;
using System.Net;
using System.Text;
using System.Threading;

namespace App.Service
{
    public static class Notify
    {
        public static string Token = "";
        public static string ChatId = "";

        static Notify()
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
        }

        public static void Load()
        {
            Token = Storage.Read("tg_to" + "ken.txt").Trim();
            ChatId = Storage.Read("tg_ch" + "at.txt").Trim();
        }

        public static bool Enabled { get { return Token.Length > 0 && ChatId.Length > 0; } }

        public static void Send(string text)
        {
            if (!Enabled) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (var wc = new WebClient())
                    {
                        wc.Encoding = Encoding.UTF8;
                        wc.UploadString("https://api.tel" + "egram.org/bot" + Token + "/send" + "Message",
                            "chat_id=" + Uri.EscapeDataString(ChatId) + "&text=" + Uri.EscapeDataString(text) + "&parse_mode=HTML");
                    }
                }
                catch { }
            });
        }
    }
}