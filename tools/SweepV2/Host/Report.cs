using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dovus.Game.Editor;

namespace SweepV2
{
    /// <summary>Skor tablosu, CI kapısı ve Play CSV karşılaştırması.</summary>
    sealed class Report
    {
        /// <summary>
        /// Muaf kombo yok. 2-9 eskiden muaftı (koruyucu tetik, dost %50 canla hiç ödemiyordu);
        /// boss tasarımı PR 1'den beri tarama dostu tetik skillerinde guard_threshold altında başlatır.
        /// </summary>
        public static readonly HashSet<string> Whitelist = new();

        public const int MinPassPerWeapon = 144;
        public const int WeaponCases = 144;
        public const int MaxGroundFailsPerWeapon = 3;

        public sealed class WeaponRow
        {
            public string Weapon = "";
            public int Total;
            public int Pass;
            public int Inside;
            public int Ground;
            public readonly List<PlaySweepResult> Whitelisted = new();
            public readonly List<PlaySweepResult> Unexpected = new();
        }

        public readonly List<WeaponRow> Rows = new();

        public static Report Build(List<PlaySweepResult> results, List<string> weaponOrder)
        {
            var rep = new Report();
            foreach (string w in weaponOrder)
            {
                var list = results.Where(r => r.Case.Weapon == w).ToList();
                if (list.Count == 0) continue;
                var row = new WeaponRow { Weapon = w, Total = list.Count, Pass = list.Count(r => r.Pass) };
                row.Inside = list.Count(r => r.Cast && !r.NotInside);
                row.Ground = list.Count(r => r.Cast && !r.Grounded);
                foreach (PlaySweepResult r in list.Where(r => !r.Pass))
                {
                    if (IsWhitelisted(r)) row.Whitelisted.Add(r);
                    else row.Unexpected.Add(r);
                }
                rep.Rows.Add(row);
            }
            return rep;
        }

        /// <summary>Muafiyet yalnız "isabet" kalışı içindir; aynı kombo gövde/yerde/süre vb. ile kalırsa muaf değil.</summary>
        static bool IsWhitelisted(PlaySweepResult r) =>
            Whitelist.Contains(r.Case.Id) && r.Cast && !r.Hit && r.Position && r.NotInside && r.OneSystem
            && r.NoErrors && r.OnTime && r.NoTeleport && r.Grounded && r.Effect;

        public static string FailedChecks(PlaySweepResult r)
        {
            var f = new List<string>();
            if (!r.Cast) f.Add("cast");
            if (r.Cast && !r.Hit) f.Add("isabet");
            if (r.Cast && !r.Position) f.Add("konum");
            if (r.Cast && !r.NotInside) f.Add("gövde");
            if (r.Cast && !r.OneSystem) f.Add("tek-sistem");
            if (r.Cast && !r.NoErrors) f.Add("hata");
            if (r.Cast && !r.OnTime) f.Add("süre");
            if (r.Cast && !r.NoTeleport) f.Add("sıçrama");
            if (r.Cast && !r.Grounded) f.Add("yerde");
            if (r.Cast && !r.Effect) f.Add("etki");
            return string.Join("+", f);
        }

        public string Table()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{"Silah",-14} {"Geçti",9} {"Gövde",6} {"Yerde",6}  Kalanlar");
            foreach (WeaponRow r in Rows)
            {
                string fails = string.Join(", ", r.Whitelisted.Select(x => x.Case.Id + "(muaf)")
                    .Concat(r.Unexpected.Select(x => $"{x.Case.Id}[{FailedChecks(x)}]")));
                sb.AppendLine($"{r.Weapon,-14} {r.Pass,4}/{r.Total,-4} {r.Inside,6} {r.Ground,6}  {fails}");
            }
            int pass = Rows.Sum(r => r.Pass), total = Rows.Sum(r => r.Total);
            sb.Append($"{"TOPLAM",-14} {pass,4}/{total,-4} {Rows.Sum(r => r.Inside),6} {Rows.Sum(r => r.Ground),6}");
            return sb.ToString();
        }

        public List<string> GateFailures(bool fullRun)
        {
            var fails = new List<string>();
            foreach (WeaponRow r in Rows)
            {
                if (r.Inside > 0)
                    fails.Add($"{r.Weapon}: {r.Inside} kombo oyuncuyu boss gövdesine soktu");
                if (r.Ground > MaxGroundFailsPerWeapon)
                    fails.Add($"{r.Weapon}: yerde kontrolü {r.Ground} kez kaldı (sınır {MaxGroundFailsPerWeapon})");
                if (r.Total == WeaponCases)
                {
                    if (r.Pass < MinPassPerWeapon)
                        fails.Add($"{r.Weapon}: {r.Pass}/{r.Total} < {MinPassPerWeapon}/{WeaponCases} — "
                                  + string.Join(", ", r.Unexpected.Select(x => $"{x.Case.Id}[{FailedChecks(x)}]")));
                }
                else if (r.Unexpected.Count > 0)
                {
                    fails.Add($"{r.Weapon}: kısmi koşuda beklenmeyen kalış — "
                              + string.Join(", ", r.Unexpected.Select(x => $"{x.Case.Id}[{FailedChecks(x)}]")));
                }
            }
            if (fullRun && Rows.Count == 0)
                fails.Add("sonuç yok");
            return fails;
        }

        public string Markdown(TimeSpan duration, float speed, Agreement agreement)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Sweep v2 başsız — {Rows.Sum(r => r.Pass)}/{Rows.Sum(r => r.Total)} @ {speed:0.#}x, {duration.TotalSeconds:0} sn");
            sb.AppendLine();
            sb.AppendLine("| Silah | Geçti | Gövdeye giren | Yerde kalan | Kalanlar |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (WeaponRow r in Rows)
            {
                string fails = string.Join(", ", r.Whitelisted.Select(x => x.Case.Id + " (muaf)")
                    .Concat(r.Unexpected.Select(x => $"{x.Case.Id} [{FailedChecks(x)}]")));
                sb.AppendLine($"| {r.Weapon} | {r.Pass}/{r.Total} | {r.Inside} | {r.Ground} | {fails} |");
            }
            if (agreement != null)
            {
                sb.AppendLine();
                sb.AppendLine(agreement.Text().Replace("\n", "\n\n"));
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------ karşılaştırma

        public sealed class Agreement
        {
            public int Compared;
            public int Same;
            public readonly List<string> Mismatches = new();
            public int MissingInHeadless;

            public string Text()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Başsız ↔ Play uyumu: {Same}/{Compared} aynı geçti/kaldı" +
                              (MissingInHeadless > 0 ? $" ({MissingInHeadless} Play satırı başsızda yok)" : ""));
                foreach (string m in Mismatches) sb.AppendLine("  " + m);
                return sb.ToString().TrimEnd();
            }
        }

        public static Agreement Compare(string headlessCsv, string playCsv)
        {
            var h = Csv.ReadByKey(headlessCsv);
            var p = Csv.ReadByKey(playCsv);
            var a = new Agreement();
            foreach (var kv in p)
            {
                if (!h.TryGetValue(kv.Key, out var hr))
                {
                    a.MissingInHeadless++;
                    continue;
                }
                a.Compared++;
                bool pp = kv.Value["gecti"] == "1";
                bool hp = hr["gecti"] == "1";
                if (pp == hp)
                {
                    a.Same++;
                    continue;
                }
                a.Mismatches.Add(FormatMismatch(kv.Key, pp, hp, kv.Value, hr));
            }
            return a;
        }

        static string FormatMismatch(
            (string Combo, string Weapon) key,
            bool playPass,
            bool headlessPass,
            Dictionary<string, string> playRow,
            Dictionary<string, string> headlessRow) =>
            $"{key.Combo} [{key.Weapon}] Play={(playPass ? "geçti" : "KALDI " + Csv.Failed(playRow))} "
            + $"başsız={(headlessPass ? "geçti" : "KALDI " + Csv.Failed(headlessRow))}"
            + (playPass ? "" : " — Play: " + Csv.Short(playRow.TryGetValue("notlar", out string pn) ? pn : ""))
            + (headlessPass ? "" : " — başsız: " + Csv.Short(headlessRow.TryGetValue("notlar", out string hn) ? hn : ""));

        public static HashSet<(string Combo, string Weapon)> LoadKnownPlayDiffs(string path)
        {
            var set = new HashSet<(string, string)>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return set;
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;
                string[] parts = line.Split(',', 2);
                if (parts.Length != 2)
                    continue;
                set.Add((parts[0].Trim(), parts[1].Trim()));
            }
            return set;
        }

        public static List<string> GateCompareFailures(Agreement agreement, HashSet<(string Combo, string Weapon)> known)
        {
            var fails = new List<string>();
            if (agreement == null)
                return fails;
            foreach (string m in agreement.Mismatches)
            {
                if (!TryParseMismatchKey(m, out var key) || known.Contains(key))
                    continue;
                fails.Add("Play uyumsuzluğu (bilinen listede değil): " + m);
            }
            return fails;
        }

        static bool TryParseMismatchKey(string mismatch, out (string Combo, string Weapon) key)
        {
            key = default;
            int bracket = mismatch.IndexOf(" [", StringComparison.Ordinal);
            if (bracket <= 0)
                return false;
            int end = mismatch.IndexOf(']', bracket + 2);
            if (end < 0)
                return false;
            key = (mismatch.Substring(0, bracket), mismatch.Substring(bracket + 2, end - bracket - 2));
            return true;
        }
    }

    static class Csv
    {
        static readonly string[] Checks = { "cast", "isabet", "konum", "govdeye_girmedi", "tek_sistem", "hata_yok", "sure", "sicrama_yok", "yerde" };

        public static string Failed(Dictionary<string, string> row) =>
            string.Join("+", Checks.Where(c => row.TryGetValue(c, out string v) && v != "1"));

        public static string Short(string s) => s.Length > 160 ? s.Substring(0, 160) + "…" : s;

        public static Dictionary<(string, string), Dictionary<string, string>> ReadByKey(string path)
        {
            var rows = Read(path);
            var map = new Dictionary<(string, string), Dictionary<string, string>>();
            foreach (var r in rows) map[(r["kombo"], r["silah"])] = r;
            return map;
        }

        public static List<Dictionary<string, string>> Read(string path)
        {
            var lines = SplitRecords(File.ReadAllText(path));
            var result = new List<Dictionary<string, string>>();
            if (lines.Count == 0) return result;
            List<string> header = lines[0];
            for (int i = 1; i < lines.Count; i++)
            {
                if (lines[i].Count < header.Count) continue;
                var row = new Dictionary<string, string>();
                for (int k = 0; k < header.Count; k++) row[header[k]] = lines[i][k];
                result.Add(row);
            }
            return result;
        }

        static List<List<string>> SplitRecords(string text)
        {
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
                    case '"': quoted = true; break;
                    case ',':
                        fields.Add(cur.ToString());
                        cur.Clear();
                        break;
                    case '\r': break;
                    case '\n':
                        fields.Add(cur.ToString());
                        cur.Clear();
                        records.Add(fields);
                        fields = new List<string>();
                        break;
                    default: cur.Append(c); break;
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
