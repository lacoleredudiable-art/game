using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Parse edilmiş gramer kökünü SkillMotor.FromJson uyumlu metne çevirir (yalnızca dilim bootstrap).</summary>
    static class GrammarJsonCache
    {
        public static string ToJson(JsonValue root)
        {
            var sb = new StringBuilder(64 * 1024);
            WriteValue(sb, root);
            return sb.ToString();
        }

        static void WriteValue(StringBuilder sb, JsonValue v)
        {
            switch (v.Kind)
            {
                case JsonKind.Null:
                    sb.Append("null");
                    break;
                case JsonKind.Bool:
                    sb.Append(v.AsBool() ? "true" : "false");
                    break;
                case JsonKind.Number:
                    sb.Append(v.AsDouble().ToString(CultureInfo.InvariantCulture));
                    break;
                case JsonKind.String:
                    WriteString(sb, v.AsString());
                    break;
                case JsonKind.Array:
                    sb.Append('[');
                    IReadOnlyList<JsonValue> arr = v.AsArray();
                    for (int i = 0; i < arr.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        WriteValue(sb, arr[i]);
                    }
                    sb.Append(']');
                    break;
                case JsonKind.Object:
                    sb.Append('{');
                    bool first = true;
                    foreach (KeyValuePair<string, JsonValue> kv in v.AsObject())
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        WriteString(sb, kv.Key);
                        sb.Append(':');
                        WriteValue(sb, kv.Value);
                    }
                    sb.Append('}');
                    break;
            }
        }

        static void WriteString(StringBuilder sb, string s)
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
                        if (c < 32)
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
