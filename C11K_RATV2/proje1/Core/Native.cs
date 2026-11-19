// Dosya: Core/Native.cs
using System;
using System.Runtime.InteropServices;

namespace App.Service
{
    internal static class Native
    {
        [DllImport("use" + "r32.dll")] static extern bool SetProcessDPIAware();
        [DllImport("use" + "r32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr ctx);
        [DllImport("dwm" + "api.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int val, int size);
        [DllImport("ux" + "theme.dll", CharSet = CharSet.Unicode)] public static extern int SetWindowTheme(IntPtr h, string a, string b);
        [DllImport("use" + "r32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);

        const int LVM_SETEXTENDEDLISTVIEWSTYLE = 0x1000 + 54;
        const int LVS_EX_DOUBLEBUFFER = 0x00000001;

        public static void EnableDpi()
        {
            try { if (!SetProcessDpiAwarenessContext(new IntPtr(-4))) SetProcessDPIAware(); }
            catch { try { SetProcessDPIAware(); } catch { } }
        }

        public static void DarkBar(IntPtr h, bool on)
        {
            try { int v = on ? 1 : 0; DwmSetWindowAttribute(h, 20, ref v, 4); } catch { }
        }

        public static void Rounded(IntPtr h, bool round)
        {
            try { int v = round ? 2 : 1; DwmSetWindowAttribute(h, 33, ref v, 4); } catch { }
        }

        public static void ListViewDoubleBuffer(IntPtr h)
        {
            try { SendMessage(h, LVM_SETEXTENDEDLISTVIEWSTYLE, (IntPtr)LVS_EX_DOUBLEBUFFER, (IntPtr)LVS_EX_DOUBLEBUFFER); } catch { }
        }
    }
}