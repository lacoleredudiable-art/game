using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace IntegrationTests;

[TestFixture]
public class ScriptReferenceTests
{
    static readonly string[] AssetExtensions = { ".unity", ".prefab", ".asset", ".controller", ".overrideController" };

    static readonly Regex ScriptRefLine = new(
        @"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([0-9a-f]{32}),\s*type:\s*3\}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Regex EditorClassId = new(
        @"^\s*m_EditorClassIdentifier:\s*(.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Regex GuidInMeta = new(
        @"^guid:\s*([0-9a-f]{32})\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static string BaselinePath =>
        Path.Combine(RepoPaths.Root, "tools", "IntegrationTests", "known-unresolved-script-guids.txt");

    [Test]
    public void ScriptGuids_ResolveOrMatchKnownUnresolvedBaseline()
    {
        var scriptIndex = BuildScriptMetaGuidIndex();
        var unresolved = CollectUnresolvedScriptRefs(scriptIndex);

        var baseline = LoadBaseline();
        var baselineKeys = new HashSet<string>(baseline.Select(b => b.guid), StringComparer.OrdinalIgnoreCase);

        foreach (var row in unresolved)
        {
            if (!baselineKeys.Contains(row.guid))
            {
                Assert.Fail(
                    $"Çözülemeyen script guid taban listede yok: {row.guid} | {row.assetFile} | {row.hint}");
            }
        }

        foreach (var row in baseline)
        {
            if (scriptIndex.ContainsKey(row.guid))
                TestContext.Out.WriteLine(
                    $"[uyarı] Artık çözülen script guid tabanda: {row.guid} ({row.assetFile})");
        }
    }

    [Test]
    public void ResolvedScriptGuids_PointToExistingCsFiles()
    {
        var scriptIndex = BuildScriptMetaGuidIndex();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string rel in GitTrackedAssetPaths())
        {
            string assetPath = Path.Combine(RepoPaths.UnityAssets, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(assetPath))
                continue;

            foreach (var (guid, _) in ParseScriptRefs(File.ReadAllText(assetPath)))
            {
                if (!scriptIndex.TryGetValue(guid, out string metaRel))
                    continue;
                if (!seen.Add(guid))
                    continue;

                if (!metaRel.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                string csPath = Path.Combine(RepoPaths.UnityAssets, metaRel.Replace('/', Path.DirectorySeparatorChar));
                Assert.That(File.Exists(csPath), Is.True, $"Orphan meta: guid {guid} → {metaRel}");
                Assert.That(string.IsNullOrWhiteSpace(Path.GetFileName(csPath)), Is.False, metaRel);
            }
        }
    }

    [Test]
    public void CsMetaGuids_AreUnique()
    {
        var guidToMeta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string assets = RepoPaths.UnityAssets;

        foreach (string meta in Directory.EnumerateFiles(assets, "*.cs.meta", SearchOption.AllDirectories))
        {
            if (meta.Contains($"{Path.DirectorySeparatorChar}Library{Path.DirectorySeparatorChar}")
                || meta.Contains($"{Path.DirectorySeparatorChar}Temp{Path.DirectorySeparatorChar}"))
                continue;

            string guid = ReadMetaGuid(meta);
            if (guid == null)
                continue;

            string rel = Path.GetRelativePath(assets, meta).Replace('\\', '/');
            if (guidToMeta.TryGetValue(guid, out string other))
            {
                Assert.Fail($"Yinelenen script guid {guid}: {other} ve {rel}");
            }

            guidToMeta[guid] = rel;
        }
    }

    [Test, Explicit("Taban listesini yeniden üretmek için")]
    public void GenerateKnownUnresolvedScriptGuidsBaseline()
    {
        var scriptIndex = BuildScriptMetaGuidIndex();
        var unresolved = CollectUnresolvedScriptRefs(scriptIndex);
        var lines = unresolved
            .Select(u => string.IsNullOrWhiteSpace(u.hint) ? $"{u.guid} {u.assetFile}" : $"{u.guid} {u.assetFile} {u.hint}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        File.WriteAllLines(BaselinePath, lines);
        TestContext.Out.WriteLine($"Wrote {lines.Count} lines to {BaselinePath}");
    }

    static List<(string guid, string assetFile, string hint)> CollectUnresolvedScriptRefs(
        Dictionary<string, string> scriptIndex)
    {
        var unresolved = new List<(string guid, string assetFile, string hint)>();

        foreach (string rel in GitTrackedAssetPaths())
        {
            string assetPath = Path.Combine(RepoPaths.UnityAssets, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(assetPath))
                continue;

            string yaml = File.ReadAllText(assetPath);
            foreach (var (guid, editorId) in ParseScriptRefs(yaml))
            {
                if (scriptIndex.ContainsKey(guid))
                    continue;
                string hint = string.IsNullOrWhiteSpace(editorId) ? "" : editorId.Trim();
                unresolved.Add((guid, rel, hint));
            }
        }

        return unresolved;
    }

    static IEnumerable<(string guid, string editorClassId)> ParseScriptRefs(string yaml)
    {
        string[] lines = yaml.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            Match m = ScriptRefLine.Match(lines[i]);
            if (!m.Success)
                continue;

            string guid = m.Groups[1].Value.ToLowerInvariant();
            string editorId = "";
            if (i + 1 < lines.Length)
            {
                Match id = EditorClassId.Match(lines[i + 1]);
                if (id.Success)
                    editorId = id.Groups[1].Value;
            }

            yield return (guid, editorId);
        }
    }

    static Dictionary<string, string> BuildScriptMetaGuidIndex()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string assets = RepoPaths.UnityAssets;

        foreach (string pattern in new[] { "*.cs.meta", "*.dll.meta" })
        {
            foreach (string meta in Directory.EnumerateFiles(assets, pattern, SearchOption.AllDirectories))
            {
                if (meta.Contains($"{Path.DirectorySeparatorChar}Library{Path.DirectorySeparatorChar}")
                    || meta.Contains($"{Path.DirectorySeparatorChar}Temp{Path.DirectorySeparatorChar}"))
                    continue;

                string guid = ReadMetaGuid(meta);
                if (guid == null)
                    continue;

                string assetFile = meta.Substring(0, meta.Length - ".meta".Length);
                string rel = Path.GetRelativePath(assets, assetFile).Replace('\\', '/');
                map[guid] = rel;
            }
        }

        return map;
    }

    static string ReadMetaGuid(string metaPath)
    {
        foreach (string line in File.ReadLines(metaPath))
        {
            Match m = GuidInMeta.Match(line);
            if (m.Success)
                return m.Groups[1].Value.ToLowerInvariant();
        }

        return null;
    }

    static IEnumerable<string> GitTrackedAssetPaths()
    {
        string root = RepoPaths.Root;
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "ls-files unity/Assets/",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var proc = Process.Start(psi);
        Assert.That(proc, Is.Not.Null, "git başlatılamadı");
        string output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();
        Assert.That(proc.ExitCode, Is.EqualTo(0), proc.StandardError.ReadToEnd());

        foreach (string line in output.Split('\n'))
        {
            string rel = line.Trim().Replace('\\', '/');
            if (rel.Length == 0)
                continue;
            if (!rel.StartsWith("unity/Assets/", StringComparison.Ordinal))
                continue;

            string assetsRel = rel.Substring("unity/Assets/".Length);
            if (AssetExtensions.Any(ext => assetsRel.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                yield return assetsRel;
        }
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

            int firstSpace = line.IndexOf(' ');
            if (firstSpace <= 0)
                continue;
            string guid = line.Substring(0, firstSpace).ToLowerInvariant();
            string rest = line.Substring(firstSpace + 1).Trim();
            int secondSpace = rest.IndexOf(' ');
            string asset;
            string hint;
            if (secondSpace < 0)
            {
                asset = rest;
                hint = "";
            }
            else
            {
                asset = rest.Substring(0, secondSpace);
                hint = rest.Substring(secondSpace + 1);
            }

            rows.Add((guid, asset, hint));
        }

        return rows;
    }
}
