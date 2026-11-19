// Dosya: Net/Protocol.cs
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace App.Service
{
    public static class Protocol
    {
        public const int MaxFrame = 96 * 1024 * 1024;
        const int Iter = 40000;
        const string WsGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        static readonly string Salt3 = Encoding.UTF8.GetString(Convert.FromBase64String("QzExSyRTYWx0JHYz"));

        static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();
        static readonly ConcurrentDictionary<string, SessionCrypto> Cache = new ConcurrentDictionary<string, SessionCrypto>();

        public static void Fill(byte[] buf)
        {
            lock (Rng) Rng.GetBytes(buf);
        }

        public static string NewSalt()
        {
            var s = new byte[16];
            Fill(s);
            return Convert.ToBase64String(s);
        }

        public static SessionCrypto Crypto(string pass)
        {
            if (string.IsNullOrEmpty(pass)) return null;
            SessionCrypto c;
            if (Cache.TryGetValue(pass, out c)) return c;

            using (var d = new Rfc2898DeriveBytes(pass, Encoding.UTF8.GetBytes(Salt3), Iter))
                c = new SessionCrypto(d.GetBytes(64));

            if (Cache.Count > 64) Cache.Clear();
            Cache[pass] = c;
            return c;
        }

        public static void Write(Stream s, string msg, string key, bool ws)
        {
            var c = Crypto(key);
            if (c == null) return;

            byte[] body = c.Encrypt(msg);

            var frame = new byte[5 + body.Length];
            frame[0] = (byte)(body.Length & 0xFF);
            frame[1] = (byte)((body.Length >> 8) & 0xFF);
            frame[2] = (byte)((body.Length >> 16) & 0xFF);
            frame[3] = (byte)((body.Length >> 24) & 0xFF);
            frame[4] = 1;
            Buffer.BlockCopy(body, 0, frame, 5, body.Length);

            if (ws) WsSend(s, frame);
            else { s.Write(frame, 0, frame.Length); s.Flush(); }
        }

        public static void WriteRaw(Stream s, string msg, bool ws)
        {
            byte[] body = Encoding.UTF8.GetBytes(msg);

            var frame = new byte[5 + body.Length];
            frame[0] = (byte)(body.Length & 0xFF);
            frame[1] = (byte)((body.Length >> 8) & 0xFF);
            frame[2] = (byte)((body.Length >> 16) & 0xFF);
            frame[3] = (byte)((body.Length >> 24) & 0xFF);
            frame[4] = 0;
            Buffer.BlockCopy(body, 0, frame, 5, body.Length);

            if (ws) WsSend(s, frame);
            else { s.Write(frame, 0, frame.Length); s.Flush(); }
        }

        public static string Read(Stream s, string key, bool ws)
        {
            try
            {
                byte[] frame = ws ? WsRecv(s) : RawFrame(s);
                if (frame == null || frame.Length < 5) return null;

                int len = BitConverter.ToInt32(frame, 0);
                if (len < 0 || len > MaxFrame || len != frame.Length - 5) return null;

                byte[] body = new byte[len];
                if (len > 0) Buffer.BlockCopy(frame, 5, body, 0, len);

                if (frame[4] == 1)
                {
                    var c = Crypto(key);
                    if (c == null) return null;
                    return c.Decrypt(body);
                }

                if (!string.IsNullOrEmpty(key)) return null;
                return Encoding.UTF8.GetString(body);
            }
            catch { return null; }
        }

        static byte[] Exact(Stream s, int n)
        {
            byte[] b = new byte[n];
            int r = 0;
            while (r < n)
            {
                int x = s.Read(b, r, n - r);
                if (x <= 0) return null;
                r += x;
            }
            return b;
        }

        static byte[] RawFrame(Stream s)
        {
            byte[] h = Exact(s, 5);
            if (h == null) return null;
            int len = BitConverter.ToInt32(h, 0);
            if (len < 0 || len > MaxFrame) return null;
            byte[] body = len > 0 ? Exact(s, len) : new byte[0];
            if (body == null) return null;
            byte[] f = new byte[5 + len];
            Buffer.BlockCopy(h, 0, f, 0, 5);
            if (len > 0) Buffer.BlockCopy(body, 0, f, 5, len);
            return f;
        }

        static void WsPong(Stream s, byte[] data)
        {
            try
            {
                byte[] b = new byte[2 + data.Length];
                b[0] = 0x8A;
                b[1] = (byte)(data.Length & 0x7F);
                Buffer.BlockCopy(data, 0, b, 2, data.Length);
                s.Write(b, 0, b.Length);
                s.Flush();
            }
            catch { }
        }

        static void WsSend(Stream s, byte[] data)
        {
            int n = data.Length;
            byte[] head;
            if (n < 126) { head = new byte[2]; head[1] = (byte)n; }
            else if (n <= 0xFFFF) { head = new byte[4]; head[1] = (byte)126; head[2] = (byte)(n >> 8); head[3] = (byte)n; }
            else { head = new byte[10]; head[1] = (byte)127; for (int i = 0; i < 8; i++) head[9 - i] = (byte)((long)n >> (8 * i)); }
            head[0] = 0x82;

            byte[] buf = new byte[head.Length + n];
            Buffer.BlockCopy(head, 0, buf, 0, head.Length);
            Buffer.BlockCopy(data, 0, buf, head.Length, n);

            s.Write(buf, 0, buf.Length);
            s.Flush();
        }

        static byte[] WsRecv(Stream s)
        {
            var ms = new MemoryStream();
            while (true)
            {
                byte[] h = Exact(s, 2);
                if (h == null) return null;

                int op = h[0] & 0x0F;
                bool fin = (h[0] & 0x80) != 0;
                bool masked = (h[1] & 0x80) != 0;
                long len = h[1] & 0x7F;

                if (len == 126) { byte[] b = Exact(s, 2); if (b == null) return null; len = (b[0] << 8) | b[1]; }
                else if (len == 127) { byte[] b = Exact(s, 8); if (b == null) return null; len = 0; for (int i = 0; i < 8; i++) len = (len << 8) | b[i]; }
                if (len < 0 || len > MaxFrame) return null;

                byte[] mask = masked ? Exact(s, 4) : null;
                if (masked && mask == null) return null;

                byte[] data = len > 0 ? Exact(s, (int)len) : new byte[0];
                if (data == null) return null;
                if (mask != null) for (int i = 0; i < data.Length; i++) data[i] ^= mask[i & 3];

                if (op == 9) { WsPong(s, data); continue; }
                if (op == 10) continue;
                if (op == 8) return null;

                ms.Write(data, 0, data.Length);
                if (fin) return ms.ToArray();
            }
        }

        public static bool ServerWsHandshake(Stream s)
        {
            try
            {
                var sb = new StringBuilder();
                byte[] one = new byte[1];
                while (sb.Length < 8192)
                {
                    int r = s.Read(one, 0, 1);
                    if (r <= 0) return false;
                    sb.Append((char)one[0]);
                    if (sb.Length >= 4 && sb[sb.Length - 4] == '\r' && sb[sb.Length - 3] == '\n' && sb[sb.Length - 2] == '\r' && sb[sb.Length - 1] == '\n') break;
                }

                string head = sb.ToString();
                if (!head.StartsWith("GET ", StringComparison.OrdinalIgnoreCase)) return false;

                int i = head.IndexOf("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase);
                if (i < 0) return false;
                i += 18;
                int e = head.IndexOf("\r\n", i);
                if (e < 0) return false;
                string key = head.Substring(i, e - i).Trim();

                string acc;
                using (var sha = SHA1.Create())
                    acc = Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(key + WsGuid)));

                string resp =
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Sec-WebSocket-Accept: " + acc + "\r\n\r\n";

                byte[] b = Encoding.ASCII.GetBytes(resp);
                s.Write(b, 0, b.Length);
                s.Flush();
                return true;
            }
            catch { return false; }
        }
    }
}