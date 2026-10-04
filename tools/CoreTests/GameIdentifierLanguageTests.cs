using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

/// <summary>PLAN 2B.13b — Game kod tanımlayıcıları İngilizce (A24).</summary>
[TestFixture]
public sealed class GameIdentifierLanguageTests
{
    static readonly Regex TurkishCharInIdentifier = new(
        @"\b[A-Za-z_]*[ığüşöçİĞÜŞÖÇ][A-Za-z0-9_]*\b",
        RegexOptions.Compiled);

    static readonly HashSet<string> AllowedTurkishWordIdentifiers = new(StringComparer.Ordinal)
    {
        // Serileştirilmiş / tuning alanları (bilinçli veri sözcüğü — docs/ARCHITECTURE.md sözlük).
        "PoseZehir",
        // TuningPreset üyeleri: spec dışı hazır set adları; JSON anahtarı değil.
        "Agir",
        "Cevik",
        "Anime",
    };

    static readonly string[] BannedTurkishWordRoots =
    {
        "Kalkan", "Yay", "Asa", "Kilic", "Mizrak", "HavaPin", "ToprakKnock", "ToprakShake",
        "GenisYay", "KarsiSaldiri", "KosuAtisi", "CaprazAtes", "Surekli", "Yakin",
        "ManifestationDirectorDefaults",
        "Cekic", "Tilsim", "Kitap", "Kure",
    };

    /// <summary>feature/grip-calibration merge edilince adlandırılacak.</summary>
    static readonly HashSet<string> BannedRootPathExemptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Weapons/WeaponGripView.cs",
        "Weapons/WeaponGripViewDefaults.cs",
    };

    static string GameRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts", "Game"));

    static string StripStringsAndComments(string line)
    {
        int sq = line.IndexOf('\'');
        if (sq >= 0)
            line = line.Substring(0, sq);
        int dq = line.IndexOf('"');
        if (dq >= 0)
            line = line.Substring(0, dq);
        int cm = line.IndexOf("//", StringComparison.Ordinal);
        if (cm >= 0)
            line = line.Substring(0, cm);
        return line;
    }

    [Test]
    public void Game_identifiers_have_no_Turkish_characters_outside_strings_and_comments()
    {
        var hits = new List<string>();
        foreach (string file in Directory.EnumerateFiles(GameRoot(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(GameRoot(), file).Replace('\\', '/');
            foreach (string raw in File.ReadLines(file))
            {
                string line = StripStringsAndComments(raw);
                foreach (Match m in TurkishCharInIdentifier.Matches(line))
                {
                    if (line.Contains("FormerlySerializedAs", StringComparison.Ordinal))
                        continue;
                    hits.Add($"{rel}: {m.Value}");
                }
            }
        }

        Assert.That(hits, Is.Empty, string.Join("\n", hits));
    }

    [Test]
    public void Game_identifiers_avoid_listed_Turkish_word_roots()
    {
        var hits = new List<string>();
        foreach (string file in Directory.EnumerateFiles(GameRoot(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(GameRoot(), file).Replace('\\', '/');
            if (BannedRootPathExemptions.Contains(rel))
                continue;
            foreach (string raw in File.ReadLines(file))
            {
                string line = StripStringsAndComments(raw);
                if (line.Contains("FormerlySerializedAs", StringComparison.Ordinal))
                    continue;
                foreach (string root in BannedTurkishWordRoots)
                {
                    if (line.Contains(root, StringComparison.Ordinal))
                    {
                        foreach (Match id in Regex.Matches(line, @"\b[A-Za-z_][A-Za-z0-9_]*\b"))
                        {
                            string token = id.Value;
                            if (token.Contains(root, StringComparison.Ordinal) && !AllowedTurkishWordIdentifiers.Contains(token))
                                hits.Add($"{rel}: {token}");
                        }
                    }
                }
            }
        }

        Assert.That(hits.Distinct().ToList(), Is.Empty, string.Join("\n", hits.Distinct()));
    }
}
