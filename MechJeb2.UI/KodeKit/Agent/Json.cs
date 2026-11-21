using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KodeKit.Agent
{
    /// <summary>The little JSON the agent bridge needs: a writer, and a parser to dictionaries, lists, strings, doubles, bools.</summary>
    public sealed class JsonWriter
    {
        readonly StringBuilder sb = new StringBuilder(4096);
        bool first = true;

        public override string ToString() => sb.ToString();

        void Sep()
        {
            if (!first) sb.Append(',');
            first = false;
        }

        public JsonWriter Open(string name = null)
        {
            Key(name);
            sb.Append('{');
            first = true;
            return this;
        }

        public JsonWriter Close()
        {
            sb.Append('}');
            first = false;
            return this;
        }

        public JsonWriter OpenArray(string name = null)
        {
            Key(name);
            sb.Append('[');
            first = true;
            return this;
        }

        public JsonWriter CloseArray()
        {
            sb.Append(']');
            first = false;
            return this;
        }

        void Key(string name)
        {
            Sep();
            if (name == null) return;
            Str(name);
            sb.Append(':');
        }

        public JsonWriter Prop(string name, string value)
        {
            Key(name);
            if (value == null) sb.Append("null");
            else Str(value);
            return this;
        }

        public JsonWriter Prop(string name, bool value)
        {
            Key(name);
            sb.Append(value ? "true" : "false");
            return this;
        }

        public JsonWriter Prop(string name, double value)
        {
            Key(name);
            sb.Append(value.ToString("R", CultureInfo.InvariantCulture));
            return this;
        }

        public JsonWriter Value(string value)
        {
            Sep();
            Str(value);
            return this;
        }

        public JsonWriter Strings(string name, IEnumerable<string> values)
        {
            OpenArray(name);
            foreach (string v in values) Value(v);
            return CloseArray();
        }

        void Str(string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }

    public static class JsonReader
    {
        const int MaxDepth = 32;

        /// <summary>Parses a value; a malformed or too deeply nested text is a FormatException (a client's mistake).</summary>
        public static object Parse(string s)
        {
            int i = 0;
            try
            {
                object v = Value(s, ref i, 0);
                Ws(s, ref i);
                if (i != s.Length) throw new FormatException("JSON: text after the value at " + i);
                return v;
            }
            catch (IndexOutOfRangeException)
            {
                throw new FormatException("JSON: unexpected end");
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new FormatException("JSON: unexpected end");
            }
        }

        static void Ws(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        static object Value(string s, ref int i, int depth)
        {
            if (depth > MaxDepth) throw new FormatException("JSON: nested deeper than " + MaxDepth);
            Ws(s, ref i);
            if (i >= s.Length) throw new FormatException("JSON: unexpected end");
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>();
                i++;
                Ws(s, ref i);
                if (s[i] == '}')
                {
                    i++;
                    return d;
                }
                while (true)
                {
                    Ws(s, ref i);
                    string k = Str(s, ref i);
                    Ws(s, ref i);
                    Expect(s, ref i, ':');
                    d[k] = Value(s, ref i, depth + 1);
                    Ws(s, ref i);
                    if (s[i] == ',')
                    {
                        i++;
                        continue;
                    }
                    Expect(s, ref i, '}');
                    return d;
                }
            }
            if (c == '[')
            {
                var l = new List<object>();
                i++;
                Ws(s, ref i);
                if (s[i] == ']')
                {
                    i++;
                    return l;
                }
                while (true)
                {
                    l.Add(Value(s, ref i, depth + 1));
                    Ws(s, ref i);
                    if (s[i] == ',')
                    {
                        i++;
                        continue;
                    }
                    Expect(s, ref i, ']');
                    return l;
                }
            }
            if (c == '"') return Str(s, ref i);
            if (Word(s, ref i, "true")) return true;
            if (Word(s, ref i, "false")) return false;
            if (Word(s, ref i, "null")) return null;
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (start == i) throw new FormatException("JSON: unexpected '" + c + "' at " + i);
            try
            {
                return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
            }
            catch (Exception e) when (e is OverflowException || e is FormatException)
            {
                throw new FormatException("JSON: a bad number at " + start);
            }
        }

        static bool Word(string s, ref int i, string w)
        {
            if (string.CompareOrdinal(s, i, w, 0, w.Length) != 0) return false;
            i += w.Length;
            return true;
        }

        static void Expect(string s, ref int i, char c)
        {
            if (i >= s.Length || s[i] != c) throw new FormatException("JSON: expected '" + c + "' at " + i);
            i++;
        }

        static string Str(string s, ref int i)
        {
            Expect(s, ref i, '"');
            var sb = new StringBuilder();
            while (s[i] != '"')
            {
                char c = s[i++];
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }
                c = s[i++];
                switch (c)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: sb.Append(c); break;
                }
            }
            i++;
            return sb.ToString();
        }
    }
}
