using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dovus.Core.Boss;
using Dovus.Core.Damage;
using Dovus.Core.Grammar;
using Dovus.Core.Shared;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

/// <summary>PLAN 2B.27 — doküman yolları, OYUN.md üretimi, silinen belgeler.</summary>
[TestFixture]
public class DocsTruthTests
{
    static string RepoRoot()
    {
        string dir = TestContext.CurrentContext.TestDirectory;
        for (int i = 0; i < 8; i++)
        {
            string candidate = Path.GetFullPath(Path.Combine(dir, "..", "..", "..", "..", ".."));
            if (File.Exists(Path.Combine(candidate, "AGENTS.md")))
                return candidate;
            candidate = Path.GetFullPath(Path.Combine(dir, "..", "..", "..", ".."));
            if (File.Exists(Path.Combine(candidate, "AGENTS.md")))
                return candidate;
            dir = Path.GetDirectoryName(dir)!;
        }

        throw new InvalidOperationException("repo kökü bulunamadı");
    }

    static IEnumerable<string> TrackedMarkdownPaths(string root)
    {
        yield return Path.Combine(root, "README.md");
        yield return Path.Combine(root, "AGENTS.md");
        foreach (string rel in new[]
                 {
                     "docs/OYUN.md",
                     "docs/PLAN.md",
                     "docs/MAP.md",
                     "docs/ARCHITECTURE.md",
                     "docs/unity-notlari.md",
                     "docs/agent-task-template.md",
                     "docs/asset-references.md",
                     "docs/asset-yedegi.md",
                     "docs/design/aglarin-kralicesi.md",
                     "docs/AGENTS.md",
                     "tools/AGENTS.md",
                     "tools/capture/README.md",
                     "unity/Assets/Scripts/Core/AGENTS.md",
                     "unity/Assets/Scripts/App/AGENTS.md",
                     "unity/Assets/Scripts/Game/AGENTS.md",
                     "unity/Assets/Scripts/Game/Editor/AGENTS.md",
                 })
            yield return Path.Combine(root, rel);
    }

    static readonly HashSet<string> PathAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "archive/capture-scripts",
        "docs/play-sweep/pr35-final-4x.csv",
        "docs/play-sweep/headless-baseline.sha256",
        "docs/play-sweep/known-play-diffs.txt",
    };

    [Test]
    public void TrackedMarkdown_PathReferences_ResolveToExistingFiles()
    {
        string root = RepoRoot();
        var missing = new List<string>();
        var reBacktick = new Regex(@"`((?:unity|tools|docs)/[^`\s]+)`", RegexOptions.CultureInvariant);
        var reLink = new Regex(@"\((docs/[^)\s]+\.(?:md|json))\)", RegexOptions.CultureInvariant);

        foreach (string mdPath in TrackedMarkdownPaths(root))
        {
            if (!File.Exists(mdPath))
            {
                missing.Add($"izlenen dosya yok: {Path.GetRelativePath(root, mdPath)}");
                continue;
            }

            string text = File.ReadAllText(mdPath);
            foreach (Match m in reBacktick.Matches(text))
            {
                CheckRef(root, m.Groups[1].Value, missing);
            }

            foreach (Match m in reLink.Matches(text))
            {
                CheckRef(root, m.Groups[1].Value, missing);
            }
        }

        Assert.That(missing, Is.Empty, string.Join(Environment.NewLine, missing));
    }

    static void CheckRef(string root, string rel, List<string> missing)
    {
        rel = rel.Replace('\\', '/');
        if (rel.Contains('<') || rel.Contains('>'))
            return;
        if (rel.StartsWith("docs/concept", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("docs/archive", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("unity/Assets/Synty", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("unity/Assets/Art/Mixamo", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("unity/Assets/JMO Assets", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("unity/Assets/UnityTechnologies", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("tools/vendor", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("unity/Temp/", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("unity/Library/", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("unity/obj/", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("unity/Logs/", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("tools/verify-out/", StringComparison.OrdinalIgnoreCase))
            return;
        if (rel.Contains("...", StringComparison.Ordinal))
            return;
        if (PathAllowlist.Any(a => rel.Equals(a, StringComparison.OrdinalIgnoreCase)
                                    || rel.StartsWith(a, StringComparison.OrdinalIgnoreCase)))
            return;
        if (rel.EndsWith("/*", StringComparison.Ordinal) || rel.Contains('*'))
            return;
        string full = Path.GetFullPath(Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(full) && !Directory.Exists(full))
            missing.Add(rel);
    }

    [Test]
    public void OyunMd_GeneratedSection_MatchesCodeAndJson()
    {
        string root = RepoRoot();
        string oyunPath = Path.Combine(root, "docs", "OYUN.md");
        Assert.That(File.Exists(oyunPath), Is.True);
        string text = File.ReadAllText(oyunPath);
        const string begin = "<!-- gen:begin -->";
        const string end = "<!-- gen:end -->";
        int i0 = text.IndexOf(begin, StringComparison.Ordinal);
        int i1 = text.IndexOf(end, StringComparison.Ordinal);
        Assert.That(i0, Is.GreaterThanOrEqualTo(0));
        Assert.That(i1, Is.GreaterThan(i0));
        string gen = text.Substring(i0 + begin.Length, i1 - i0 - begin.Length);

        string jsonPath = Path.Combine(root, "unity", "Assets", "Resources", "ElementSystem", "element-sistemi.json");
        string json = File.ReadAllText(jsonPath);
        JsonValue rootJson = MiniJson.Parse(json);
        int combo = rootJson["combo_system"]["current_max_length"].AsInt(-1);
        Assert.That(gen, Does.Contain($"**{combo}**"));

        float playerRaw = rootJson["global_rules"]["player_stats"]["max_hp"].AsFloat(100f);
        float bossRaw = rootJson["global_rules"]["boss_stats_default"]["max_hp"].AsFloat(BossDefaults.FallbackBossMaxHp);
        int playerHp = CombatScale.MagnitudeInt(playerRaw);
        int bossHp = CombatScale.MagnitudeInt(bossRaw);
        Assert.That(gen, Does.Contain("400,000"));
        Assert.That(gen, Does.Contain("88,000,000"));
        Assert.That(playerHp, Is.EqualTo(400_000));
        Assert.That(bossHp, Is.EqualTo(88_000_000));

        var dodge = new DodgeTuning();
        Assert.That(gen, Does.Contain(dodge.MaxCharges.ToString()));
        Assert.That(gen, Does.Contain((dodge.ChargeRechargeMs / 1000f).ToString("0.##")));

        var combat = new CombatTuning();
        Assert.That(gen, Does.Contain(combat.EnforceResourceCost.ToString().ToLowerInvariant()));
        Assert.That(gen, Does.Contain(combat.EnforceCooldown.ToString().ToLowerInvariant()));

        Assert.That(gen, Does.Contain("5%"));
        Assert.That(gen, Does.Contain("×**2**"));
        Assert.That(gen, Does.Contain("1,000,000,000"));

        Assert.That(gen, Does.Contain("ActiveBossId"));
        Assert.That(gen, Does.Contain("aglarin_kralicesi"));
        Assert.That(gen, Does.Contain("50"));
    }

    [Test]
    public void OyunMd_GenScript_CheckMode_Passes()
    {
        string root = RepoRoot();
        string py = Path.Combine(root, "tools", "gen-game-overview.py");
        Assert.That(File.Exists(py), Is.True);
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{py}\" --check",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = System.Diagnostics.Process.Start(psi)!;
        p.WaitForExit(120_000);
        Assert.That(p.ExitCode, Is.EqualTo(0), p.StandardError.ReadToEnd() + p.StandardOutput.ReadToEnd());
    }

    [Test]
    public void RemovedDocs_DoNotExist()
    {
        string root = RepoRoot();
        foreach (string rel in new[]
                 {
                     "docs/durum.md",
                     "docs/ARCHITECTURE-PLAN.md",
                     "docs/core-layers.md",
                     "docs/skill-model.md",
                     "docs/naming.md",
                     "docs/glossary.md",
                     "docs/constants-report.md",
                     "docs/ci.md",
                     "docs/migrations/2.1-game-folders.md",
                     "docs/bosses/aglarin-kralicesi.md",
                 })
        {
            Assert.That(File.Exists(Path.Combine(root, rel)), Is.False, rel);
        }
    }
}
