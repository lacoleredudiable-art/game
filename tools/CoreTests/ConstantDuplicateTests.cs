using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

/// <summary>PLAN 2B.26: sabit kopyaları tek kaynakta topla; motion boss yarıçapı yedek adı düzelt.</summary>
[TestFixture]
public sealed class ConstantDuplicateTests
{
    static string ScriptsRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts"));

    static IEnumerable<string> AllCsFiles() =>
        Directory.EnumerateFiles(ScriptsRoot(), "*.cs", SearchOption.AllDirectories);

    [Test]
    public void SecToMs_IsDefinedOnlyInUnits()
    {
        var hits = new List<string>();
        foreach (string file in AllCsFiles())
        {
            string rel = Path.GetRelativePath(ScriptsRoot(), file).Replace('\\', '/');
            string text = File.ReadAllText(file);
            if (Regex.IsMatch(text, @"\bconst\s+double\s+SecToMs\s*="))
                hits.Add(rel);
        }

        Assert.That(hits, Is.EqualTo(new[] { "Core/Shared/Units.cs" }));
    }

    [Test]
    public void BossAndArenaCombatFallbacks_AreDefinedOnlyInCombatFallbacks()
    {
        var boss = new List<string>();
        var arena = new List<string>();
        foreach (string file in AllCsFiles())
        {
            string rel = Path.GetRelativePath(ScriptsRoot(), file).Replace('\\', '/');
            string text = File.ReadAllText(file);
            if (Regex.IsMatch(text, @"\bconst\s+float\s+BossBodyRadiusFallbackM\s*="))
                boss.Add(rel);
            if (Regex.IsMatch(text, @"\bconst\s+float\s+ArenaHalfSizeFallbackM\s*="))
                arena.Add(rel);
        }

        Assert.That(boss, Is.EqualTo(new[] { "Core/Shared/CombatFallbacks.cs" }));
        Assert.That(arena, Is.EqualTo(new[] { "Core/Shared/CombatFallbacks.cs" }));
    }

    [Test]
    public void MotionReturnHeightM_IsNotUsedAsBossBodyRadiusFallback()
    {
        Assert.That(
            AllCsFiles().SelectMany(f => File.ReadAllLines(f)).Any(l => l.Contains("MotionReturnHeightM")),
            Is.False,
            "MotionReturnHeightM kaldırıldı; motion boss yarıçapı yedek CombatFallbacks.MotionBossBodyRadiusFallbackM.");
    }

    [Test]
    public void BossBodyRadiusMethods_DoNotReturnMotionReturnHeightM()
    {
        var offenders = new List<string>();
        foreach (string file in AllCsFiles())
        {
            string text = File.ReadAllText(file);
            foreach (Match m in Regex.Matches(
                         text,
                         @"(?:public|internal|float)\s+float\s+BossBodyRadius\s*\([^)]*\)\s*\{([^}]*(?:\{[^}]*\}[^}]*)*)\}",
                         RegexOptions.Singleline))
            {
                if (m.Groups[1].Value.Contains("MotionReturnHeightM", StringComparison.Ordinal))
                    offenders.Add(Path.GetRelativePath(ScriptsRoot(), file));
            }
        }

        Assert.That(offenders, Is.Empty);
    }

    [Test]
    public void DuplicatePublicConst_NameAndValue_Count_DoesNotExceedPost2B26Baseline()
    {
        // Ölçüm: 2B.26 sonrası (MinDistM 0.01f ×8, MinRadiusM 0.05f ×7, …).
        var ceilings = new Dictionary<(string Name, string Value), int>(StringTupleComparer.Instance)
        {
            [("MinDistM", "0.01f")] = 8,
            [("MinRadiusM", "0.05f")] = 7,
            [("MinTickSec", "0.01f")] = 5,
            [("SegmentLen2EpsilonSqr", "1e-8f")] = 4,
        };

        var counts = new Dictionary<(string, string), int>(StringTupleComparer.Instance);
        var pat = new Regex(@"public const \w+ (\w+)\s*=\s*([^;]+);", RegexOptions.Compiled);
        foreach (string file in AllCsFiles())
        {
            foreach (Match m in pat.Matches(File.ReadAllText(file)))
            {
                var key = (m.Groups[1].Value, m.Groups[2].Value.Trim());
                counts.TryGetValue(key, out int n);
                counts[key] = n + 1;
            }
        }

        foreach (var ((name, value), max) in ceilings)
        {
            counts.TryGetValue((name, value), out int actual);
            Assert.That(actual, Is.LessThanOrEqualTo(max), $"{name}={value}: {actual} tanım (tavan {max})");
        }
    }

    sealed class StringTupleComparer : IEqualityComparer<(string Name, string Value)>
    {
        public static readonly StringTupleComparer Instance = new();

        public bool Equals((string Name, string Value) x, (string Name, string Value) y) =>
            string.Equals(x.Name, y.Name, StringComparison.Ordinal)
            && string.Equals(x.Value, y.Value, StringComparison.Ordinal);

        public int GetHashCode((string Name, string Value) obj) =>
            HashCode.Combine(obj.Name, obj.Value);
    }
}
