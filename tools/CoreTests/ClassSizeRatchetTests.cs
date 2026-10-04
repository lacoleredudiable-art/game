using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class ClassSizeRatchetTests
{
    static string ScriptsRoot =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts"));

    // spec'te yok: mevcut partial toplamları (yalnız aşağı ratchet)
    static readonly Dictionary<string, int> Allowlist = new()
    {
        ["ManifestationDirector"] = 4475,
        ["PlaySweep"] = 2445,
        ["HexagonView"] = 1233,
        ["BossDirector"] = 1112,
        ["MotionTemplateRunner"] = 1030,
        ["PortalSystem"] = 971,
        ["MixamoAnimatorBind"] = 843,
        ["BuildSelectHud"] = 825,
        ["TuningPanelHud"] = 819,
        ["VitalsHud"] = 753,
        ["MechanicWorldRuntime"] = 733,
        ["MechanicGrammar"] = 722,
        ["TeamComboSystem"] = 716,
        ["TeamComboHost"] = 696,
        ["ActorView"] = 676,
        ["LivingEffectView"] = 671,
        ["FollowCameraController"] = 660,
        ["StatusBoard"] = 632,
    };

    [Test]
    public void Core_App_Game_class_line_totals_at_most_600_unless_allowlisted()
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
                int partialIdx = text.IndexOf("partial class ");
                if (partialIdx < 0)
                    continue;
                int nameStart = partialIdx + "partial class ".Length;
                int nameEnd = text.IndexOfAny(new[] { ' ', ':', '<', '\r', '\n' }, nameStart);
                if (nameEnd < 0)
                    continue;
                string typeName = text.Substring(nameStart, nameEnd - nameStart).Trim();
                totals.TryGetValue(typeName, out int cur);
                totals[typeName] = cur + File.ReadAllLines(path).Length;
            }
        }

        foreach (var kv in totals)
        {
            int cap = Allowlist.TryGetValue(kv.Key, out int allowed) ? allowed : 600;
            Assert.That(
                kv.Value,
                Is.LessThanOrEqualTo(cap),
                () => $"{kv.Key} total {kv.Value} lines (cap {cap})");
        }
    }
}
