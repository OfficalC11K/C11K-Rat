// Dosya: Build/LoaderBin.cs
using System;
using System.Collections.Concurrent;

namespace App.Service
{
    public static class LoaderBin
    {
        static byte[] _stage1;
        static byte[] _dll;
        static readonly object _lk = new object();

        public static readonly ConcurrentDictionary<string, string> Status =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static DateTime Stage1At, DllAt;

        public static void SetStage1(byte[] b)
        {
            lock (_lk) { _stage1 = b; Stage1At = DateTime.UtcNow; }
        }

        public static void SetDll(byte[] b)
        {
            lock (_lk) { _dll = b; DllAt = DateTime.UtcNow; }
        }

        public static byte[] GetLatestStage1() { lock (_lk) return _stage1; }
        public static byte[] GetLatestDll() { lock (_lk) return _dll; }

        public static bool HasStage1 { get { return GetLatestStage1() != null; } }
        public static bool HasDll { get { return GetLatestDll() != null; } }
    }
}