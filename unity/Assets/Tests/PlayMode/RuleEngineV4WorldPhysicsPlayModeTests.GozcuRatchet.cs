using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Gözcü cırcırı: duman testindeki Gözcü satırları tür başına tools/gozcu-esik.txt tavanıyla karşılaştırılır.
    /// "engel" türü tavanı aşarsa kırmızı; "uyari" türü (saate bağlı oynayan) yalnız uyarır.
    /// Sonuç gozcu çıktısına cirlik.txt olarak yazılır (Gözcü tür tablosu; PR açıklamasına yapıştırılır).
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const string GozcuThresholdFileName = "gozcu-esik.txt";
        const string GozcuRatchetReportName = "cirlik.txt";
        const string GateBlock = "engel";
        const string GateWarn = "uyari";
        const string ReportRed = "KIRMIZI";
        const string ReportWarn = "UYARI";
        const string ReportInfo = "BILGI";
        const string ReportOk = "TAMAM";

        readonly struct GozcuThreshold
        {
            public readonly int Max;
            public readonly bool Blocks;

            public GozcuThreshold(int max, bool blocks)
            {
                Max = max;
                Blocks = blocks;
            }
        }

        /// <summary>Raporu yazar; engelleyen ihlal satırlarını döndürür.</summary>
        static List<string> ApplyGozcuRatchet(IReadOnlyList<string> gozcuLines, string outDir)
        {
            Dictionary<string, GozcuThreshold> thresholds = LoadGozcuThresholds(GozcuThresholdPath());
            Dictionary<string, int> counts = gozcuLines
                .Select(GozcuRuleOf)
                .GroupBy(r => r)
                .ToDictionary(g => g.Key, g => g.Count());

            var report = new List<string> { $"toplam {gozcuLines.Count} Gözcü satırı" };
            var red = new List<string>();
            foreach (string rule in thresholds.Keys.Union(counts.Keys).OrderBy(r => r, StringComparer.Ordinal))
            {
                counts.TryGetValue(rule, out int n);
                GozcuThreshold t = thresholds.TryGetValue(rule, out GozcuThreshold known)
                    ? known
                    : new GozcuThreshold(0, blocks: true);
                string gate = t.Blocks ? GateBlock : GateWarn;
                string line;
                if (n > t.Max)
                {
                    line = $"{(t.Blocks ? ReportRed : ReportWarn)} {rule} {n} > eşik {t.Max} ({gate})";
                    if (t.Blocks)
                        red.Add(line);
                }
                else if (n < t.Max)
                {
                    line = t.Blocks
                        ? $"{ReportWarn} {rule} {n} < eşik {t.Max} ({gate}): tools/{GozcuThresholdFileName} içinde eşiği {n} yap"
                        : $"{ReportInfo} {rule} {n} < eşik {t.Max} ({gate})";
                }
                else
                {
                    line = $"{ReportOk} {rule} {n} = eşik {t.Max} ({gate})";
                }
                report.Add(line);
                if (line.StartsWith(ReportWarn, StringComparison.Ordinal))
                    Debug.LogWarning("[gozcu-cirlik] " + line);
            }

            File.WriteAllText(Path.Combine(outDir, GozcuRatchetReportName), string.Join("\n", report) + "\n");
            TestContext.WriteLine(string.Join("\n", report));
            return red;
        }

        /// <summary>"t=.. [etiket] [GOZCU] &lt;kural&gt; &lt;değer&gt; kare=n" satırından kural.</summary>
        static string GozcuRuleOf(string line)
        {
            int tag = line.IndexOf(GozcuReporter.Tag, StringComparison.Ordinal);
            Assert.That(tag, Is.GreaterThanOrEqualTo(0), "Gözcü etiketi yok: " + line);
            string rest = line.Substring(tag + GozcuReporter.Tag.Length).TrimStart();
            int space = rest.IndexOf(' ');
            return space < 0 ? rest : rest.Substring(0, space);
        }

        static Dictionary<string, GozcuThreshold> LoadGozcuThresholds(string path)
        {
            Assert.That(File.Exists(path), Is.True, "Gözcü eşik dosyası yok: " + path);
            var map = new Dictionary<string, GozcuThreshold>(StringComparer.Ordinal);
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;
                string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                Assert.That(parts.Length, Is.EqualTo(3), $"{GozcuThresholdFileName}: '<tür> <eşik> <engel|uyari>' bekleniyor: {raw}");
                Assert.That(int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int max), Is.True,
                    $"{GozcuThresholdFileName}: eşik sayı değil: {raw}");
                Assert.That(parts[2] == GateBlock || parts[2] == GateWarn, Is.True,
                    $"{GozcuThresholdFileName}: kip '{GateBlock}' ya da '{GateWarn}' olmalı: {raw}");
                Assert.That(map.ContainsKey(parts[0]), Is.False, $"{GozcuThresholdFileName}: tür iki kez: {parts[0]}");
                map[parts[0]] = new GozcuThreshold(max, parts[2] == GateBlock);
            }
            return map;
        }

        static string GozcuThresholdPath() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "tools", GozcuThresholdFileName));
    }
}
