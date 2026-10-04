using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

/// <summary>PLAN 2B.28 — oturum hasar zarı ile IRng sarmalayıcısı ayrı ad alanlarında kalır.</summary>
[TestFixture]
public sealed class CombatRngNamingTests
{
    static string ScriptsRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts"));

    [Test]
    public void Scripts_define_CombatRng_only_in_Damage_session_rng()
    {
        var hits = new List<string>();
        var pattern = new Regex(@"\b(?:sealed\s+)?class\s+CombatRng\b", RegexOptions.Compiled);
        foreach (string file in Directory.EnumerateFiles(ScriptsRoot(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(ScriptsRoot(), file).Replace('\\', '/');
            foreach (string line in File.ReadLines(file))
            {
                if (pattern.IsMatch(line))
                    hits.Add(rel);
            }
        }

        Assert.That(hits, Is.EquivalentTo(new[] { "Core/Damage/CombatRng.cs" }));
    }

    [Test]
    public void Shared_layer_uses_SeededRng_not_CombatRng()
    {
        string sharedDir = Path.Combine(ScriptsRoot(), "Core", "Shared");
        foreach (string file in Directory.EnumerateFiles(sharedDir, "*.cs"))
        {
            string text = File.ReadAllText(file);
            Assert.That(text, Does.Not.Contain("class CombatRng"));
        }
        Assert.That(File.Exists(Path.Combine(sharedDir, "SeededRng.cs")), Is.True);
    }
}
