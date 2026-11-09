## 🏗️ Mimari

C11K üç ayrı bileşenden oluşur ve hepsi **tek bir C# dosyasında** (`Form1.cs`) yaşar. Builder, server ve bot aynı süreçte çalışır; client ise builder tarafından **runtime'da derlenen** ayrı bir `.exe`'dir.

```
┌──────────────────────────────────────────────────────────────────────┐
│                        C11K SERVER (WinForms)                        │
│                                                                      │
│  ┌────────────┐   ┌────────────┐   ┌────────────┐   ┌────────────┐   │
│  │  Builder   │   │  Session   │   │  Telegram  │   │   Module   │   │
│  │  Panel     │   │  Manager   │   │    Bot     │   │  Registry  │   │
│  └─────┬──────┘   └─────┬──────┘   └─────┬──────┘   └─────┬──────┘   │
│        │                │                │                │          │
│        │ CSharpCode     │ TcpListener    │ HTTPS          │ Form     │
│        │ Provider       │ + Protocol     │ (Bot API)      │ cache    │
│        ▼                ▼                ▼                ▼          │
│  ┌──────────────────────────────────────────────────────────────┐    │
│  │              Protocol (frame + crypto katmanı)               │    │
│  └──────────────────────────┬───────────────────────────────────┘    │
└─────────────────────────────┼────────────────────────────────────────┘
                              │
              TCP · AES-256-CBC + HMAC-SHA256
              PBKDF2(40k) · oturum tuzu · 96 MB frame
                              │
┌─────────────────────────────┼────────────────────────────────────────┐
│                    C11K CLIENT (derlenmiş .exe)                      │
│                                                                      │
│  ┌────────────┐   ┌────────────┐   ┌────────────┐   ┌────────────┐   │
│  │  Connect   │   │  Dispatch  │   │  Modules   │   │  Persist   │   │
│  │  Loop      │   │  (cmd      │   │  (RD/KL/   │   │  (Reg/     │   │
│  │  + SOCKS5  │   │   parser)  │   │   Clip/..) │   │   Task/..) │   │
│  └─────┬──────┘   └─────┬──────┘   └─────┬──────┘   └─────┬──────┘   │
│        └────────────────┴────────────────┴────────────────┘          │
│                              │                                       │
│              Win32 API (user32/gdi32/avicap32/winmm)                 │
│              DPAPI + BCrypt (şifre kurtarma)                         │
│              sqlite3 (Chromium Login Data)                           │
└──────────────────────────────────────────────────────────────────────┘
                              │
                              │ HTTPS (Bot API)
                              ▼
                    ┌──────────────────┐
                    │  Operatör        │
                    │  (Telefon / PC)  │
                    └──────────────────┘
```

---

### 🧩 Bileşenler ve Sorumlulukları

| Bileşen | Sınıf / Dosya | Görevi | Kullandığı Teknoloji |
|---|---|---|---|
| **UI çatısı** | `DarkForm`, `ChromeButton`, `Theme` | Özel çizilmiş pencere, 19 tema, DWM dark title bar, rounded corners | `user32`, `dwmapi`, `uxtheme` P/Invoke |
| **Builder** | `BuilderPanel`, `Builder`, `BuildConfig` | Kullanıcıdan ayar topla, `PayloadTemplate.Source` içine enjekte et, derle | `Microsoft.CSharp.CSharpCodeProvider` (`csc` in-process) |
| **Payload** | `PayloadTemplate.Source` | Client'in tam kaynak kodu — placeholder (`%%HOST%%`, `%%AESKEY%%` …) ile gelir | C# string template |
| **Session Manager** | `SessionManager` | `TcpListener` (dual-mode), oturum sözlüğü, heartbeat sweep | `System.Net.Sockets` |
| **Protokol** | `Protocol`, `SessionCrypto` | Frame okuma/yazma, AES + HMAC, salt üretimi | `System.Security.Cryptography` |
| **Telegram Bot** | `TgBot`, `Json` (mini parser), `Notify` | Long polling, inline butonlar, dosya indirme/yükleme | `HttpWebRequest`, `WebClient` |
| **Modül kaydı** | `ModuleRegistry`, `IRemoteModule` | Aynı istemci için aynı pencereyi tekrar açma, tema senkronu | Generic `ConcurrentDictionary` |
| **İstemci tarafı** | `GeneratedClient.Program` (template) | Bağlantı döngüsü, komut dispatch, tüm casusluk modülleri | Win32 P/Invoke, WMI, DPAPI, BCrypt |
| **Depolama** | `Storage` | Favoriler, engelliler, bilinen istemciler, Telegram token | `%AppData%\C11K\` düz metin |
| **Görselleştirme** | `ClientGrid`, `InfoRow`, `Bubble`, `Toast` | Özel çizilmiş istemci ızgarası, sohbet balonları, bildirimler | GDI+ (`System.Drawing`) |

---

### 🔄 Veri Akışı — Adım Adım

**1. Builder → Client (derleme anı)**

```
BuilderPanel.Collect()  →  BuildConfig
        │
        ▼
Builder.Build(cfg, outPath)
        │  %%HOST%%, %%AESKEY%%, %%PORT%% … placeholder'ları değiştirir
        │  obfuscation açıksa → D("base64") çağrısına çevirir
        ▼
CSharpCodeProvider.CompileAssemblyFromSource()
        │
        ├── UseLoader=false → doğrudan OneDrive.exe
        └── UseLoader=true  → önce DLL derle, base64'e çevir, stub'a göm, stub'ı derle
        │
        ▼
(opsiyonel) upx.exe --best
```

**2. Client → Server (bağlantı kurulumu)**

```
Client                              Server
  │                                   │
  │──── TCP connect ─────────────────►│
  │──── HELLO|sid|tag|user@pc|... ───►│  (düz metin)
  │                                   │  SessionManager.Handle()
  │◄─── SALT|<base64-16byte> ─────────│  (düz metin, sunucu üretir)
  │                                   │
  │  key = AESKEY + "|" + SALT        │  key = AESKEY + "|" + SALT
  │  PBKDF2(40k) → enc(32) + mac(32)  │  PBKDF2(40k) → enc(32) + mac(32)
  │                                   │
  │◄═══ AES-256-CBC + HMAC ══════════►│  (artık her şey şifreli)
  │                                   │
```

**3. Komut gönderme (server → client)**

```
UI (QuickAction)  →  SessionManager.Send(sid, "SHOT")
        │
        ▼
Protocol.Write(stream, "SHOT", key)
        │  aes.Encrypt("SHOT") → IV + cipher + MAC
        │  5-byte header: [len][flag=1]
        ▼
Client Recv() → Decrypt → Dispatch("SHOT") → ShotOnce()
        │
        ▼
Send("SHOT|1920|1080|<base64 jpeg>")
        │
        ▼
Server MessageIn() → OnShot() → thumbnail üret → ClientGrid.UpdateRow()
```

**4. Telegram botu (paralel kanal)**

```
Operatör ──/shot──► Telegram API ──getUpdates──► TgBot.Poll()
                                                       │
                                                       ▼
                                              Handle() → Cmd("/shot")
                                                       │
                                                       ▼
                                              _mgr.Send(sid, "SHOT")
                                                       │
                                        (client cevabı gelince)
                                                       │
                                                       ▼
                                              TgBot.Feed(sid, msg)
                                                       │
                                                       ▼
                                              Photo(chat, b64, caption)
                                                       │
                                                       ▼
                                              Operatör ◄── sendPhoto
```

---

### 🧰 Kullanılan Teknolojiler ve Nedenleri

**Dil & Framework**
- **C# / .NET Framework 4.7.2+** — tek dosyada WinForms, P/Invoke ve runtime derleme bir arada
- **WinForms** — özel çizim (`OnPaint`) ile tema sistemi, harici UI kütüphanesi yok
- **GDI+** (`System.Drawing`) — tüm özel kontroller, ekran görüntüsü işleme, JPEG encode

**Ağ & Protokol**
- **`System.Net.Sockets.TcpListener` / `TcpClient`** — dual-mode IPv6/IPv4, `NoDelay`, `KeepAlive`
- **`System.Security.Cryptography`** — `Aes`, `HMACSHA256`, `Rfc2898DeriveBytes` (PBKDF2)
- **SOCKS5** — elle yazılmış RFC 1928 implementasyonu (kullanıcı/şifre auth dahil)

**Runtime Derleme**
- **`Microsoft.CSharp.CSharpCodeProvider`** — in-process `csc`, harici SDK gerektirmez
- **`CompilerParameters`** — `/target:winexe`, `/optimize+`, `/target:library` (loader için)
- **Loader stub** — payload'ı base64 olarak gömer, `Assembly.Load(byte[])` ile bellekte açar

**Windows Entegrasyonu (P/Invoke)**
| DLL | Kullanım |
|---|---|
| `user32.dll` | `SendMessage`, `GetAsyncKeyState`, `ToUnicodeEx`, `LockWorkStation`, `SetCursorPos`, `mouse_event`, `keybd_event`, `SystemParametersInfo` |
| `gdi32.dll` | `BitBlt`, `CreateDC`, `DeleteDC` — ekran yakalama |
| `dwmapi.dll` | `DwmSetWindowAttribute` — dark title bar, rounded corners |
| `uxtheme.dll` | `SetWindowTheme` — ListView dark mode |
| `avicap32.dll` | `capCreateCaptureWindowA` — webcam |
| `winmm.dll` | `mciSendString` — ses kaydı, CD tepsi |
| `crypt32.dll` | `CryptUnprotectData` — DPAPI çözme |
| `bcrypt.dll` | `BCryptDecrypt` — AES-GCM (Chromium v10 şifreleri) |
| `winsqlite3.dll` | `sqlite3_*` — Chromium `Login Data` okuma |

**Sistem Bilgisi**
- **`System.Management` (WMI)** — `Win32_OperatingSystem` (RAM), `Win32_PnPEntity` (kamera), `root\SecurityCenter2` (AV)
- **`Microsoft.Win32.Registry`** — kalıcılık, anti-VM tespiti, startup yönetimi
- **`Process` / `ProcessStartInfo`** — shell, script, güç komutları

**Telegram**
- **`HttpWebRequest` / `WebClient`** — `getUpdates` long polling, `sendMessage`, `sendPhoto`, `sendDocument`, `getFile`
- **`multipart/form-data`** — elle yazılmış boundary encoder
- **Mini JSON parser** — `System.Text.Json` bağımlılığı yok, ~200 satır elle yazılmış

**Şifre Kurtarma**
- **DPAPI** (`CryptUnprotectData`) — Chromium `encrypted_key` çözme
- **BCrypt AES-GCM** — Chromium v10+ şifreleri
- **SQLite P/Invoke** — `Login Data` dosyasını kopyalayıp okuma (kilitli dosya sorunu için)
- **`netsh wlan`** — WiFi profilleri

**Görsel / UI**
- **`LinearGradientBrush`, `GraphicsPath`** — kartlar, butonlar, balonlar
- **`Control.SetStyle`** — `DoubleBuffered`, `UserPaint`, `AllPaintingInWmPaint` (flicker yok)
- **`TextRenderer` / `Graphics.DrawString`** — ClearType rendering

**Opsiyonel**
- **UPX** — `--best` sıkıştırma (harici `upx.exe`)

---

### 📐 Protokol Detayı

**Frame yapısı:**
```
┌────────┬────────┬────────┬────────┬────────┬─────────────────────┐
│ len[0] │ len[1] │ len[2] │ len[3] │ flag   │ body[len]           │
│ (LE32 toplam uzunluk)          │ 0/1    │                     │
└────────┴────────┴────────┴────────┴────────┴─────────────────────┘

flag=0  →  body = UTF-8 düz metin
flag=1  →  body = IV[16] + cipher[..] + HMAC[32]
```

**Şifreli gövde:**
```
IV (16 byte, rastgele)
Cipher (AES-256-CBC, PKCS7, plaintext UTF-8)
HMAC-SHA256(IV || Cipher)  →  32 byte
```

**Doğrulama:** HMAC sabit zamanlı karşılaştırma (`diff |= mac[i] ^ body[...]`) — timing attack'a dirençli.

**Sınırlar:**
- Maksimum frame: `96 * 1024 * 1024` (96 MB)
- PBKDF2 iterasyon: `40000`
- Heartbeat: min 10 sn, varsayılan 60 sn
- Sessizlik timeout: 150 sn → oturum düşer

---

### 🗂️ Dosya Yapısı (tek dosya içinde)

```
Form1.cs
├── Native              → P/Invoke imzaları
├── Ui                  → font, renk, rounded path, shadow yardımcıları
├── BufferedPanel       → flicker'sız Panel
├── BufferedListView    → double-buffered ListView
├── PillButton          → animasyonlu buton
├── CardPanel           → gradient + shadow kart
├── InfoRow             → ikon + caption + değer satırı
├── SearchBox           → placeholder'lı arama kutusu
├── Bubble              → sohbet balonu
├── Storage             → %AppData%\C11K\ dosya I/O
├── Notify              → Telegram bildirim
├── Json                → mini JSON parser
├── TgBot               → Telegram bot (poll, cmd, callback)
├── Theme               → 19 tema tanımı
├── ChromeButton        → kapat/küçült/büyüt
├── DarkForm            → özel title bar + resize
├── DarkGroup           → başlıklı kart
├── BuildConfig         → builder ayarları
├── ClientSession       → oturum modeli
├── SessionCrypto       → AES + HMAC sarmalayıcı
├── Protocol            → frame oku/yaz
├── PayloadTemplate     → client kaynak kodu (string)
├── Builder             → derleme + loader + UPX
├── SessionManager      → TcpListener + oturum yönetimi
├── IRemoteModule       → modül arayüzü
├── ModuleRegistry      → açık pencerelerin kaydı
├── ModuleForm          → modül tabanı
├── CamForm / DesktopViewerForm / RemoteShellForm / ...
├── ChatForm / ConsoleForm / InfoTextForm / ScriptForm / ...
├── FileManagerForm / TaskManagerForm / SystemInfoForm / ...
├── RegistryEditorForm / StartupManagerForm / KeyloggerForm / ...
├── ClipboardForm / PasswordForm / MessageBoxForm
├── Toast               → bildirim penceresi
├── RailButton          → sol ray düğmesi
├── ClientGrid          → istemci ızgarası
├── PreviewForm         → büyük önizleme
├── ClientsPanel        → istemci paneli + drawer
├── LogsPanel           → renkli log
├── DarkRenderer / DarkTable
├── BuilderPanel        → 5 sekmeli builder
├── SettingsPanel       → tema + port + Telegram
└── Form1               → ana pencere, hepsini bağlar
```

---

### ⚖️ Tasarım Kararları

| Karar | Neden |
|---|---|
| **Tek dosya** | Taşıması kolay, builder kendi kaynağını derleyebilir, DLL cehennemi yok |
| **Runtime `csc`** | Kullanıcıdan Visual Studio istemez, tek exe ile client üretir |
| **String template** | `%%HOST%%` gibi placeholder'lar okunabilir, obfuscation opsiyonel |
| **Mini JSON** | `System.Text.Json` .NET Framework 4.7.2'de yok, Newtonsoft bağımlılığı istemedik |
| **PBKDF2 + SALT** | Aynı AES anahtarı farklı oturumlarda farklı session key üretir |
| **Encrypt-then-MAC** | Padding oracle'a dirençli, HMAC önce doğrulanır |
| **Sabit zamanlı HMAC** | Timing attack engeli |
| **Dual-mode TCP** | IPv6 ve IPv4 aynı portta, fallback gerekmez |
| **ModuleRegistry** | Aynı istemci için ikinci pencere açılmaz, tema değişince hepsi güncellenir |
| **Loader stub** | Statik analizde payload görünmez, `Assembly.Load` ile bellekte açılır |
| **Telegram + UI paralel** | Operatör masaüstünde değilken de tam kontrol |

---

<div align="center">

**C11K v2.0.0** · Mimari dokümanı

*Proje aktif geliştirme aşamasında — halka açık sürüm yakında.* 🚧

</div>
