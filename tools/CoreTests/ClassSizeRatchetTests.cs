using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class ClassSizeRatchetTests
{
    static string ScriptsRoot =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts"));

    // spec'te yok: mevcut toplamlar (yalnız aşağı ratchet; değer = gerçek toplam)
    static readonly Dictionary<string, int> Allowlist = new()
    {
        ["RuleEngineV4SentenceHost"] = 2409,
        ["HexagonView"] = 1093,
        ["BossDirector"] = 1112,
        ["MotionTemplateRunner"] = 1034,
        ["PortalSystem"] = 979,
        ["MixamoAnimatorBind"] = 843,
        ["BuildSelectHud"] = 836,
        ["TuningPanelHud"] = 809,
        ["VitalsHud"] = 753,
        ["TeamComboSystem"] = 721,
        ["TeamComboHost"] = 706,
        ["ActorView"] = 658,
        ["LivingEffectView"] = 671,
        ["StatusBoard"] = 632,
    };

    static Dictionary<string, int> MeasureTotals()
    {
        var totals = new Dictionary<string, int>();
        foreach (string layer in new[] { "Core", "App", "Game" })
        {
            string dir = Path.Combine(ScriptsRoot, layer);
            if (!Directory.Exists(dir))
                continue;
            foreach (string path in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(path);
                int lines = File.ReadAllLines(path).Length;
                int partialIdx = text.IndexOf("partial class ");
                if (partialIdx >= 0)
                {
                    int nameStart = partialIdx + "partial class ".Length;
                    int nameEnd = text.IndexOfAny(new[] { ' ', ':', '<', '\r', '\n' }, nameStart);
                    if (nameEnd < 0)
                        continue;
                    string typeName = text.Substring(nameStart, nameEnd - nameStart).Trim();
                    totals.TryGetValue(typeName, out int cur);
                    totals[typeName] = cur + lines;
                }
                else
                {
                    string typeName = Path.GetFileNameWithoutExtension(path);
                    totals.TryGetValue(typeName, out int cur);
                    totals[typeName] = cur + lines;
                }
            }
        }

        return totals;
    }

    [Test]
    public void Core_App_Game_class_line_totals_at_most_600_unless_allowlisted()
    {
        var totals = MeasureTotals();
        foreach (var kv in totals)
        {
            int cap = Allowlist.TryGetValue(kv.Key, out int allowed) ? allowed : 600;
            Assert.That(
                kv.Value,
                Is.LessThanOrEqualTo(cap),
                () => $"{kv.Key} total {kv.Value} lines (cap {cap})");
        }
    }

    [Test]
    public void Allowlist_entries_match_measured_totals_exactly()
    {
        var totals = MeasureTotals();
        foreach (var kv in Allowlist)
        {
            Assert.That(
                totals.TryGetValue(kv.Key, out int measured),
                Is.True,
                () => $"allowlisted type missing: {kv.Key}");
            Assert.That(
                measured,
                Is.EqualTo(kv.Value),
                () => $"{kv.Key} allowlist {kv.Value} but measured {measured} (ratchet: güncelle)");
        }
    }
}
