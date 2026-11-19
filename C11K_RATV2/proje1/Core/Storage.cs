// Dosya: Core/Storage.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace App.Service
{
    public static class Storage
    {
        static string Dir
        {
            get
            {
                string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "C11" + "K");
                Directory.CreateDirectory(d);
                return d;
            }
        }

        public static HashSet<string> Load(string file)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string p = Path.Combine(Dir, file);
                if (!File.Exists(p)) return set;
                foreach (string l in File.ReadAllLines(p))
                    if (!string.IsNullOrWhiteSpace(l)) set.Add(l.Trim());
            }
            catch { }
            return set;
        }

        public static void Save(string file, IEnumerable<string> lines)
        {
            try
            {
                string p = Path.Combine(Dir, file);
                string t = p + ".tmp";
                File.WriteAllLines(t, lines.ToArray());
                if (File.Exists(p)) File.Replace(t, p, null); else File.Move(t, p);
            }
            catch { }
        }

        public static string Read(string file)
        {
            try { string p = Path.Combine(Dir, file); return File.Exists(p) ? File.ReadAllText(p) : ""; } catch { return ""; }
        }

        public static void Write(string file, string text)
        {
            try { File.WriteAllText(Path.Combine(Dir, file), text ?? ""); } catch { }
        }
    }
}