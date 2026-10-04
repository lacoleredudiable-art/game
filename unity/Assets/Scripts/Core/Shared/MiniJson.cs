using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Dovus.Core.Shared
{
    public static class MiniJson
    {
        public static JsonValue Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return JsonValue.Null;
            int i = 0;
            JsonValue v = ParseValue(json, ref i);
            return v;
        }

        static JsonValue ParseValue(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            if (i >= s.Length) return JsonValue.Null;
            char c = s[i];
            switch (c)
            {
                case '{': return ParseObject(s, ref i);
                case '[': return ParseArray(s, ref i);
                case '"': return JsonValue.Of(ParseString(s, ref i));
                case 't':
                    i += 4; // true
                    return JsonValue.Of(true);
                case 'f':
                    i += 5; // false
                    return JsonValue.Of(false);
                case 'n':
                    i += 4; // null
                    return JsonValue.Null;
                default:
                    return JsonValue.Of(ParseNumber(s, ref i));
            }
        }

        static JsonValue ParseObject(string s, ref int i)
        {
            var dict = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            i++; // '{'
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == '}')
            {
                i++;
                return JsonValue.Of(dict);
            }
            while (i < s.Length)
            {
                SkipWhitespace(s, ref i);
                string key = ParseString(s, ref i);
                SkipWhitespace(s, ref i);
                if (i < s.Length && s[i] == ':') i++;
                JsonValue val = ParseValue(s, ref i);
                dict[key] = val;
                SkipWhitespace(s, ref i);
                if (i < s.Length && s[i] == ',')
                {
                    i++;
                    continue;
                }
                if (i < s.Length && s[i] == '}')
                {
                    i++;
                    break;
                }
                break;
            }
            return JsonValue.Of(dict);
        }

        static JsonValue ParseArray(string s, ref int i)
        {
            var list = new List<JsonValue>();
            i++; // '['
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == ']')
            {
                i++;
                return JsonValue.Of(list);
            }
            while (i < s.Length)
            {
                JsonValue val = ParseValue(s, ref i);
                list.Add(val);
                SkipWhitespace(s, ref i);
                if (i < s.Length && s[i] == ',')
                {
                    i++;
                    continue;
                }
                if (i < s.Length && s[i] == ']')
                {
                    i++;
                    break;
                }
                break;
            }
            return JsonValue.Of(list);
        }

        static string ParseString(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            if (i >= s.Length || s[i] != '"') return string.Empty;
            i++; // opening quote
            var sb = new StringBuilder();
            while (i < s.Length && s[i] != '"')
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char next = s[i + 1];
                    switch (next)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            if (i + 5 < s.Length)
                            {
                                string hex = s.Substring(i + 2, 4);
                                if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                                    sb.Append((char)code);
                                i += 4;
                            }
                            break;
                        default: sb.Append(next); break;
                    }
                    i += 2;
                    continue;
                }
                sb.Append(c);
                i++;
            }
            if (i < s.Length) i++; // closing quote
            return sb.ToString();
        }

        static double ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] is '-' or '+' or '.' or 'e' or 'E'))
                i++;
            string raw = s.Substring(start, i - start);
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0d;
        }

        static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && s[i] is ' ' or '\t' or '\r' or '\n')
                i++;
        }
    }
}
