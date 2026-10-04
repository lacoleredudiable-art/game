using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dovus.Game.Assets;
using NUnit.Framework;

namespace IntegrationTests;

[TestFixture]
public class AssetReferenceTests
{
    static readonly string[] WatchedAssets =
    {
        "Resources/VfxLibrary.asset",
        "Resources/SfxLibrary.asset",
        "Resources/Animation/WeaponVisualRegistry.asset",
        "Resources/Environment/CombatAmbienceAssets.asset",
    };

    static readonly HashSet<string> BuiltinGuids = new(StringComparer.OrdinalIgnoreCase)
    {
        "0000000000000000e000000000000000",
        "0000000000000000f000000000000000",
        "0000000000000000d000000000000000",
    };

    static readonly Regex GuidInAsset = new(@"guid:\s*([0-9a-f]{32})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex GuidInMeta = new(@"^guid:\s*([0-9a-f]{32})\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static string BaselinePath =>
        Path.Combine(RepoPaths.Root, "tools", "IntegrationTests", "known-missing-asset-guids.txt");

    static string AssetReferencesReportPath => RepoPaths.Docs("asset-references.md");

    [Test]
    public void RuntimeResourcePaths_MatchKnownMissingReport()
    {
        var scan = RuntimeResourcePathScanner.Scan(RepoPaths.UnityAssets);
        Assert.That(File.Exists(AssetReferencesReportPath), Is.True, AssetReferencesReportPath);

        string markdown = File.ReadAllText(AssetReferencesReportPath);
        RuntimeResourcePathScanner.ParseKnownMissingFromReport(
            markdown, out HashSet<string> knownResources, out HashSet<string> knownShaders);

        var actualResources = new HashSet<string>(scan.MissingResources, StringComparer.Ordinal);
        var actualShaders = new HashSet<string>(scan.MissingShaders, StringComparer.Ordinal);

        if (!knownResources.SetEquals(actualResources))
        {
            var extra = actualResources.Except(knownResources).OrderBy(s => s).ToList();
            var stale = knownResources.Except(actualResources).OrderBy(s => s).ToList();
            Assert.Fail(
                "Resources bilinen eksikler raporuyla uyuşmuyor."
                + (extra.Count > 0 ? " Yeni eksik: " + string.Join(", ", extra) : "")
                + (stale.Count > 0 ? " Artık var (raporu güncelle): " + string.Join(", ", stale) : ""));
        }

        if (!knownShaders.SetEquals(actualShaders))
        {
            var extra = actualShaders.Except(knownShaders).OrderBy(s => s).ToList();
            var stale = knownShaders.Except(actualShaders).OrderBy(s => s).ToList();
            Assert.Fail(
                "Shader bilinen eksikler raporuyla uyuşmuyor."
                + (extra.Count > 0 ? " Yeni eksik: " + string.Join(", ", extra) : "")
                + (stale.Count > 0 ? " Artık var (raporu güncelle): " + string.Join(", ", stale) : ""));
        }
    }

    [Test, Explicit("docs/asset-references.md raporunu yeniden üret")]
    public void GenerateAssetReferencesReport()
    {
        var scan = RuntimeResourcePathScanner.Scan(RepoPaths.UnityAssets);
        string markdown = RuntimeResourcePathScanner.FormatReportMarkdown(scan, DateTime.UtcNow);
        File.WriteAllText(AssetReferencesReportPath, markdown);
        TestContext.Out.WriteLine($"Wrote {AssetReferencesReportPath}");
    }

    [Test]
    public void ResourceAssetGuids_ResolveOrMatchKnownMissingBaseline()
    {
        var index = BuildMetaGuidIndex();
        var unresolved = new List<(string guid, string assetFile, string hint)>();

        foreach (string rel in WatchedAssets)
        {
            string assetPath = Path.Combine(RepoPaths.UnityAssets, rel.Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(assetPath), Is.True, assetPath);
            string yaml = File.ReadAllText(assetPath);
            int lineNo = 0;
            foreach (string line in yaml.Split('\n'))
            {
                lineNo++;
                foreach (Match m in GuidInAsset.Matches(line))
                {
                    string guid = m.Groups[1].Value.ToLowerInvariant();
                    if (BuiltinGuids.Contains(guid))
                        continue;
                    if (!index.ContainsKey(guid))
                        unresolved.Add((guid, rel, $"line {lineNo}: {line.Trim()}"));
                }
            }
        }

        var baseline = LoadBaseline();
        var baselineKeys = new HashSet<string>(baseline.Select(b => b.guid), StringComparer.OrdinalIgnoreCase);

        foreach (var row in unresolved)
        {
            if (!baselineKeys.Contains(row.guid))
            {
                Assert.Fail(
                    $"Çözülemeyen guid taban listede yok: {row.guid} | {row.assetFile} | {row.hint}");
            }
        }

        foreach (var row in baseline)
        {
            if (index.ContainsKey(row.guid))
                TestContext.Out.WriteLine(
                    $"[uyarı] Artık çözülen guid tabanda: {row.guid} ({row.assetFile})");
        }
    }

    [Test, Explicit("Taban listesini yeniden üretmek için")]
    public void GenerateKnownMissingAssetGuidsBaseline()
    {
        var index = BuildMetaGuidIndex();
        var unresolved = new List<string>();

        foreach (string rel in WatchedAssets)
        {
            string assetPath = Path.Combine(RepoPaths.UnityAssets, rel.Replace('/', Path.DirectorySeparatorChar));
            string yaml = File.ReadAllText(assetPath);
            int lineNo = 0;
            foreach (string line in yaml.Split('\n'))
            {
                lineNo++;
                foreach (Match m in GuidInAsset.Matches(line))
                {
                    string guid = m.Groups[1].Value.ToLowerInvariant();
                    if (BuiltinGuids.Contains(guid))
                        continue;
                    if (!index.ContainsKey(guid))
                        unresolved.Add($"{guid} {rel} line {lineNo}");
                }
            }
        }

        unresolved.Sort(StringComparer.Ordinal);
        File.WriteAllLines(BaselinePath, unresolved);
        TestContext.Out.WriteLine($"Wrote {unresolved.Count} lines to {BaselinePath}");
    }

    static Dictionary<string, string> BuildMetaGuidIndex()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string assets = RepoPaths.UnityAssets;
        foreach (string meta in Directory.EnumerateFiles(assets, "*.meta", SearchOption.AllDirectories))
        {
            if (meta.Contains($"{Path.DirectorySeparatorChar}Library{Path.DirectorySeparatorChar}")
                || meta.Contains($"{Path.DirectorySeparatorChar}Temp{Path.DirectorySeparatorChar}"))
                continue;

            string guid = null;
            foreach (string line in File.ReadLines(meta))
            {
                Match m = GuidInMeta.Match(line);
                if (m.Success)
                {
                    guid = m.Groups[1].Value.ToLowerInvariant();
                    break;
                }
            }

            if (guid == null)
                continue;

            string assetFile = meta.Substring(0, meta.Length - ".meta".Length);
            string rel = Path.GetRelativePath(assets, assetFile).Replace('\\', '/');
            map[guid] = rel;
        }

        return map;
    }

    static List<(string guid, string assetFile, string hint)> LoadBaseline()
    {
        var rows = new List<(string, string, string)>();
        if (!File.Exists(BaselinePath))
            return rows;

        foreach (string line in File.ReadAllLines(BaselinePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                continue;
            string[] parts = line.Split(' ', 3);
            if (parts.Length < 2)
                continue;
            string guid = parts[0].ToLowerInvariant();
            string asset = parts[1];
            string hint = parts.Length > 2 ? parts[2] : "";
            rows.Add((guid, asset, hint));
        }

        return rows;
    }
}
