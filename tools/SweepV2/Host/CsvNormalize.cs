using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SweepV2
{
    /// <summary>CI sweep hash: LF, sondaki boşluk yok, notlar sütunu çıkarılmış, UTF-8 BOM'suz.</summary>
    static class CsvNormalize
    {
        const string NotesColumn = "notlar";

        public static string NormalizeFile(string csvPath)
        {
            string raw = File.ReadAllText(csvPath);
            return NormalizeText(raw);
        }

        public static string NormalizeText(string raw)
        {
            var rows = Csv.Read(raw);
            if (rows.Count == 0)
                return string.Empty;

            List<string> header = rows[0];
            int notesIndex = header.FindIndex(h => string.Equals(h, NotesColumn, StringComparison.Ordinal));
            var outHeader = header.Where((_, i) => i != notesIndex).ToList();

            var sb = new StringBuilder();
            AppendRow(sb, outHeader);
            for (int i = 1; i < rows.Count; i++)
            {
                List<string> line = rows[i];
                if (line.Count < header.Count)
                    continue;
                var fields = new List<string>();
                for (int k = 0; k < header.Count; k++)
                {
                    if (k == notesIndex)
                        continue;
                    fields.Add(line[k]);
                }
                AppendRow(sb, fields);
            }

            string text = sb.ToString();
            if (text.Length > 0 && !text.EndsWith("\n", StringComparison.Ordinal))
                text += "\n";
            return text;
        }

        static void AppendRow(StringBuilder sb, List<string> fields)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append(Escape(fields[i]));
            }
            sb.Append('\n');
        }

        static string Escape(string value)
        {
            if (value == null)
                return "";
            bool quote = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            if (!quote)
                return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public static string Sha256Hex(string normalizedUtf8)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(normalizedUtf8);
            byte[] hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        public static List<string> FirstLineDiffs(string expected, string actual, int maxLines = 20)
        {
            var diffs = new List<string>();
            string[] a = SplitLines(expected);
            string[] b = SplitLines(actual);
            int n = Math.Max(a.Length, b.Length);
            for (int i = 0; i < n && diffs.Count < maxLines; i++)
            {
                string left = i < a.Length ? a[i] : "<yok>";
                string right = i < b.Length ? b[i] : "<yok>";
                if (left != right)
                    diffs.Add($"satır {i + 1}: beklenen={TrimForLog(left)} | gerçek={TrimForLog(right)}");
            }
            return diffs;
        }

        static string[] SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<string>();
            return text.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n').Split('\n');
        }

        static string TrimForLog(string s) =>
            s.Length > 200 ? s.Substring(0, 200) + "…" : s;

        static class Csv
        {
            public static List<List<string>> Read(string text)
            {
                text = text.Replace("\r\n", "\n").Replace('\r', '\n');
                var records = new List<List<string>>();
                var fields = new List<string>();
                var cur = new StringBuilder();
                bool quoted = false;
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    if (quoted)
                    {
                        if (c == '"')
                        {
                            if (i + 1 < text.Length && text[i + 1] == '"')
                            {
                                cur.Append('"');
                                i++;
                            }
                            else
                            {
                                quoted = false;
                            }
                        }
                        else
                        {
                            cur.Append(c);
                        }
                        continue;
                    }
                    switch (c)
                    {
                        case '"':
                            quoted = true;
                            break;
                        case ',':
                            fields.Add(cur.ToString());
                            cur.Clear();
                            break;
                        case '\n':
                            fields.Add(cur.ToString());
                            cur.Clear();
                            records.Add(fields);
                            fields = new List<string>();
                            break;
                        default:
                            cur.Append(c);
                            break;
                    }
                }
                if (cur.Length > 0 || fields.Count > 0)
                {
                    fields.Add(cur.ToString());
                    records.Add(fields);
                }
                return records;
            }
        }
    }
}
