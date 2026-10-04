using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

/// <summary>PLAN 2B.12 — sonek / eski sürüm adları kaynak kapısı.</summary>
[TestFixture]
public sealed class NamingConventionTests
{
    static readonly Regex LegacyNamePattern = new(
        @"V611|Weapons10|PortalBorderTeam|PrototypeBootstrap|PrototypeTuning",
        RegexOptions.Compiled);

    static string ScriptsRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts"));

    static string GameRoot() => Path.Combine(ScriptsRoot(), "Game");

    static IEnumerable<string> GameCsFiles()
    {
        foreach (string file in Directory.EnumerateFiles(GameRoot(), "*.cs", SearchOption.AllDirectories))
            yield return file;
    }

    static string StripMigrationAllowlist(string text)
    {
        var lines = text.Split('\n');
        var kept = new List<string>(lines.Length);
        foreach (string line in lines)
        {
            if (line.Contains("FormerlySerializedAs", StringComparison.Ordinal))
                continue;
            if (line.Contains("MovedFrom", StringComparison.Ordinal))
                continue;
            if (line.Contains("migration", StringComparison.OrdinalIgnoreCase)
                && LegacyNamePattern.IsMatch(line))
                continue;
            kept.Add(line);
        }

        return string.Join('\n', kept);
    }

    [Test]
    public void Scripts_DoNotUse_LegacyVersionOrPrototypeTypeNames()
    {
        var hits = new List<string>();
        foreach (string file in Directory.EnumerateFiles(ScriptsRoot(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(ScriptsRoot(), file).Replace('\\', '/');
            string text = StripMigrationAllowlist(File.ReadAllText(file));
            if (LegacyNamePattern.IsMatch(text))
                hits.Add(rel);
        }

        Assert.That(hits, Is.Empty, () => "Legacy ad geçen dosyalar: " + string.Join(", ", hits));
    }

    [Test]
    public void Game_MonoBehaviour_PrimaryClassNameMatchesFileName()
    {
        var mismatches = new List<string>();
        var topLevel = new Regex(
            @"^\s{4}(?:public|internal)\s+(?:sealed\s+|abstract\s+)?class\s+(\w+)\s*:\s*MonoBehaviour",
            RegexOptions.Multiline | RegexOptions.Compiled);

        foreach (string file in GameCsFiles())
        {
            string rel = Path.GetRelativePath(GameRoot(), file).Replace('\\', '/');
            string stem = Path.GetFileNameWithoutExtension(file);
            string text = File.ReadAllText(file);
            var names = new List<string>();
            foreach (Match m in topLevel.Matches(text))
                names.Add(m.Groups[1].Value);

            if (names.Count == 0)
                continue;

            if (names.Any(n => string.Equals(n, stem, StringComparison.Ordinal)))
                continue;

            mismatches.Add($"{rel}: {string.Join(", ", names)}");
        }

        Assert.That(mismatches, Is.Empty, () => string.Join("; ", mismatches));
    }

    [Test]
    public void Game_MonoBehaviours_UseStandardSuffix()
    {
        var anyMb = new Regex(@"\bclass\s+(\w+)\s*:\s*MonoBehaviour\b", RegexOptions.Compiled);
        string[] suffixes = { "Director", "View", "Hud", "Host", "Controller" };
        var bad = new List<string>();
        int count = 0;
        foreach (string file in GameCsFiles())
        {
            string rel = Path.GetRelativePath(GameRoot(), file).Replace('\\', '/');
            foreach (Match m in anyMb.Matches(File.ReadAllText(file)))
            {
                count++;
                string name = m.Groups[1].Value;
                if (!suffixes.Any(s => name.EndsWith(s, StringComparison.Ordinal)))
                    bad.Add($"{rel}: {name}");
            }
        }

        Assert.That(count, Is.GreaterThanOrEqualTo(79), "Game MonoBehaviour taraması boş/eksik");
        Assert.That(bad, Is.Empty, () => "Sonek standardı dışı (docs/naming.md): " + string.Join("; ", bad));
    }
}
