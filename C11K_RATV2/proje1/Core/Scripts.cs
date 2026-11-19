// Dosya: Core/Scripts.cs
using System;
using System.IO;

namespace App.Service
{
    public static class Scripts
    {
        public static string Collect()
        {
            try
            {
                string b = AppDomain.CurrentDomain.BaseDirectory;
                foreach (var root in new[] { b, Path.Combine(b, ".."), Path.Combine(b, "..\\..") })
                {
                    string p = Path.Combine(root, "scripts\\collect.ps1");
                    if (File.Exists(p)) return File.ReadAllText(p);
                }
            }
            catch { }
            return null;
        }
    }
}