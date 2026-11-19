// Dosya: Core/Json.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace App.Service
{
    internal static class Json
    {
        public static object Parse(string s)
        {
            int i = 0;
            return Val(s ?? "", ref i);
        }

        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object Val(string s, ref int i)
        {
            Ws(s, ref i);
            if (i >= s.Length) return null;
            char c = s[i];

            if (c == '{')
            {
                i++;
                var d = new Dictionary<string, object>();
                Ws(s, ref i);
                if (i < s.Length && s[i] == '}') { i++; return d; }
                while (i < s.Length)
                {
                    Ws(s, ref i);
                    string k = Str(s, ref i);
                    Ws(s, ref i);
                    if (i < s.Length && s[i] == ':') i++;
                    d[k] = Val(s, ref i);
                    Ws(s, ref i);
                    if (i < s.Length && s[i] == ',') { i++; continue; }
                    if (i < s.Length && s[i] == '}') { i++; }
                    break;
                }
                return d;
            }

            if (c == '[')
            {
                i++;
                var l = new List<object>();
                Ws(s, ref i);
                if (i < s.Length && s[i] == ']') { i++; return l; }
                while (i < s.Length)
                {
                    l.Add(Val(s, ref i));
                    Ws(s, ref i);
                    if (i < s.Length && s[i] == ',') { i++; continue; }
                    if (i < s.Length && s[i] == ']') { i++; }
                    break;
                }
                return l;
            }

            if (c == '"') return Str(s, ref i);
            if (i + 4 <= s.Length && s.Substring(i, 4) == "true") { i += 4; return true; }
            if (i + 5 <= s.Length && s.Substring(i, 5) == "false") { i += 5; return false; }
            if (i + 4 <= s.Length && s.Substring(i, 4) == "null") { i += 4; return null; }

            int st = i;
            while (i < s.Length && "-+.0123456789eE".IndexOf(s[i]) >= 0) i++;
            if (i == st) { i++; return null; }
            double d2;
            if (double.TryParse(s.Substring(st, i - st), NumberStyles.Float, CultureInfo.InvariantCulture, out d2)) return d2;
            return null;
        }

        static string Str(string s, ref int i)
        {
            if (i < s.Length && s[i] == '"') i++;
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') break;
                if (c == '\\' && i < s.Length)
                {
                    char e = s[i++];
                    if (e == 'n') sb.Append('\n');
                    else if (e == 't') sb.Append('\t');
                    else if (e == 'r') sb.Append('\r');
                    else if (e == 'b') sb.Append('\b');
                    else if (e == 'f') sb.Append('\f');
                    else if (e == 'u')
                    {
                        if (i + 4 <= s.Length)
                        {
                            int cp;
                            if (int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out cp)) sb.Append((char)cp);
                            i += 4;
                        }
                    }
                    else sb.Append(e);
                    continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static Dictionary<string, object> Obj(object o) { return o as Dictionary<string, object>; }
        public static List<object> Arr(object o) { return o as List<object>; }

        public static Dictionary<string, object> D(Dictionary<string, object> d, string k)
        {
            object v;
            return d != null && d.TryGetValue(k, out v) ? v as Dictionary<string, object> : null;
        }

        public static string S(Dictionary<string, object> d, string k)
        {
            object v;
            if (d == null || !d.TryGetValue(k, out v) || v == null) return null;
            return Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        public static string Id(Dictionary<string, object> d, string k)
        {
            object v;
            if (d == null || !d.TryGetValue(k, out v) || v == null) return null;

            if (v is double)
            {
                double x = (double)v;
                if (x >= long.MinValue && x <= long.MaxValue) return ((long)x).ToString(CultureInfo.InvariantCulture);
            }

            double parsed;
            string raw = Convert.ToString(v, CultureInfo.InvariantCulture);
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                && parsed >= long.MinValue && parsed <= long.MaxValue)
                return ((long)parsed).ToString(CultureInfo.InvariantCulture);

            return raw;
        }

        public static long L(Dictionary<string, object> d, string k)
        {
            object v;
            if (d == null || !d.TryGetValue(k, out v)) return 0;
            double x;
            return double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out x) ? (long)x : 0;
        }
    }
}