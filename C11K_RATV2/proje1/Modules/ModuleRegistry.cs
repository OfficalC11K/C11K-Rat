// Dosya: Modules/ModuleRegistry.cs
//using App.Service.Modules;
using System;
using System.Collections.Concurrent;
using System.Windows.Forms;

namespace App.Service
{
    public static class ModuleRegistry
    {
        static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Form>> Map = new ConcurrentDictionary<Type, ConcurrentDictionary<string, Form>>();

        public static bool TryGet<T>(string sid, out T form) where T : Form
        {
            form = null;
            ConcurrentDictionary<string, Form> d;
            Form f;
            if (Map.TryGetValue(typeof(T), out d) && d.TryGetValue(sid, out f)) { form = (T)f; return true; }
            return false;
        }

        public static void Add<T>(string sid, T form) where T : Form
        {
            var d = Map.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<string, Form>());
            d[sid] = form;
            form.FormClosed += (s, e) => { d.TryRemove(sid, out _); };
        }

        public static void ApplyThemeAll()
        {
            foreach (var d in Map.Values)
                foreach (var f in d.Values)
                {
                    try
                    {
                        var dark = f as DarkForm;
                        if (dark != null && !dark.IsDisposed) dark.ApplyTheme();
                    }
                    catch { }
                }
        }

        public static void Open<T>(ClientSession s, Action<string> send) where T : Form, IRemoteModule, new()
        {
            Open<T>(s, send, null);
        }

        public static void Open<T>(ClientSession s, Action<string> send, Action<T> configure) where T : Form, IRemoteModule, new()
        {
            if (s == null) return;
            T existing;
            if (TryGet(s.Sid, out existing))
            {
                if (existing.WindowState == FormWindowState.Minimized) existing.WindowState = FormWindowState.Normal;
                existing.Activate();
                return;
            }
            var f = new T();
            f.Initialize(s, send);
            Add(s.Sid, f);
            if (configure != null) configure(f);
            f.Show();
        }
    }
}