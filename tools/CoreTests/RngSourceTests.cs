using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Dovus.Core.Damage;
using Dovus.Core.Shared;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RngSourceTests
{
    static string ScriptsRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts"));

    static readonly Regex ArglessSystemRandom = new(
        @"new\s+(?:System\.)?Random\s*\(\s*\)",
        RegexOptions.Compiled);

    static readonly Regex UnityRandomUse = new(
        @"\bUnityEngine\.Random\b|\bRandom\.(?:Range|value)\b",
        RegexOptions.Compiled);

    static readonly string[] UnityRandomAllowedRelPaths =
    {
        "Game/Hud/DamageNumberHud.cs",
        "Game/Audio/SfxDirector.cs",
        "Game/Platform/UnityRng.cs",
    };

    static IEnumerable<string> RuntimeCsUnder(params string[] roots)
    {
        foreach (string rootName in roots)
        {
            string root = Path.Combine(ScriptsRoot(), rootName);
            if (!Directory.Exists(root))
                continue;
            foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(ScriptsRoot(), path).Replace('\\', '/');
                if (rel.Contains("/Editor/"))
                    continue;
                yield return rel;
            }
        }
    }

    static bool IsUnderVfx(string relUnix) =>
        relUnix.StartsWith("Game/Vfx/", StringComparison.Ordinal);

    static bool UnityRandomAllowed(string relUnix) =>
        IsUnderVfx(relUnix) || System.Array.IndexOf(UnityRandomAllowedRelPaths, relUnix) >= 0;

    [Test]
    public void SourceGate_RuntimeScripts_NoArglessSystemRandom()
    {
        var hits = new List<string>();
        foreach (string rel in RuntimeCsUnder("Core", "App", "Game"))
        {
            foreach (string line in File.ReadLines(Path.Combine(ScriptsRoot(), rel)))
            {
                if (ArglessSystemRandom.IsMatch(line))
                    hits.Add($"{rel}: {line.Trim()}");
            }
        }

        Assert.That(hits, Is.Empty, string.Join("\n", hits));
    }

    [Test]
    public void SourceGate_UnityRandom_OnlyOnCosmeticAllowlist()
    {
        var hits = new List<string>();
        foreach (string rel in RuntimeCsUnder("Game"))
        {
            if (UnityRandomAllowed(rel))
                continue;
            string text = File.ReadAllText(Path.Combine(ScriptsRoot(), rel));
            if (UnityRandomUse.IsMatch(text))
                hits.Add(rel);
        }

        Assert.That(hits, Is.Empty, string.Join("\n", hits));
    }

    [Test]
    public void DamagePipeline_SetFallbackRng_UsesInjectedRngWhenVarianceSeedOpen()
    {
        const int seed = 9001;
        var injected = SeededRng.Seeded(seed);
        try
        {
            DamagePipeline.SetFallbackRng(injected);
            var a = DamagePipeline.Resolve(new DamageQuery
            {
                SkillPower = 50f,
                ApplyVariance = true,
                VarianceSeed = -1
            });
            DamagePipeline.SetFallbackRng(SeededRng.Seeded(seed));
            var b = DamagePipeline.Resolve(new DamageQuery
            {
                SkillPower = 50f,
                ApplyVariance = true,
                VarianceSeed = -1
            });
            Assert.That(b.Amount, Is.EqualTo(a.Amount));
        }
        finally
        {
            DamagePipeline.SetFallbackRng(null);
        }
    }
}
