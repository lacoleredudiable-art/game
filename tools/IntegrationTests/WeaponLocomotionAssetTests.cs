using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace IntegrationTests;

/// <summary>
/// Envanter B2 (O-2.19, O-3.7): dilim silahlarının k3 yürüme seti repodaki override dosyalarından gelir.
/// WeaponVisualRegistry → k3_&lt;silah&gt;.overrideController (temel k3.controller) → Loko_Kilic_Idle/Run yerine
/// Loko_&lt;Silah&gt;_Idle/Run. Dosyalar gitignored değil: APK ile editör aynı seti oynar.
/// </summary>
[TestFixture]
public class WeaponLocomotionAssetTests
{
    const string RegistryRel = "Resources/Animation/WeaponVisualRegistry.asset";
    const string K3Dir = "Art/Characters/k3";
    const string K3ControllerRel = K3Dir + "/k3.controller";
    const string BaseIdle = "Loko_Kilic_Idle";
    const string BaseRun = "Loko_Kilic_Run";

    static readonly (string WeaponKey, string ClipSet)[] SliceWeapons = { ("kilic", "Kilic"), ("yay", "Yay") };

    static readonly Regex EntryKey = new(@"^\s*-\s*WeaponKey:\s*(\S+)\s*$", RegexOptions.Compiled);
    static readonly Regex OverrideRef = new(@"^\s*Override:\s*\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-f]{32})", RegexOptions.Compiled);
    static readonly Regex ControllerRef = new(@"^\s*m_Controller:\s*\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-f]{32})", RegexOptions.Compiled);
    static readonly Regex ClipRef = new(@"\{fileID:\s*(-?\d+)(?:,\s*guid:\s*([0-9a-f]{32}))?", RegexOptions.Compiled);
    static readonly Regex MetaGuid = new(@"^guid:\s*([0-9a-f]{32})\s*$", RegexOptions.Compiled);
    static readonly Regex ClipName = new(@"^\s*-?\s*name:\s*(\S+)\s*$", RegexOptions.Compiled);
    static readonly Regex InternalId = new(@"^\s*internalID:\s*(-?\d+)\s*$", RegexOptions.Compiled);

    [Test]
    public void O_2_19_DilimSilahlari_K3YurumeSetiRepodakiOverridedan()
    {
        Dictionary<string, string> entries = RegistryWeaponLocomotion();
        Dictionary<string, string> guidToPath = GuidIndex(Path.Combine(RepoPaths.UnityAssets, K3Dir));
        Dictionary<(string Guid, string FileId), string> clipNames = K3ClipNames();
        string k3Guid = ReadMetaGuid(Path.Combine(RepoPaths.UnityAssets, K3ControllerRel));

        foreach ((string weaponKey, string set) in SliceWeapons)
        {
            Assert.That(entries.TryGetValue(weaponKey, out string guid), Is.True,
                $"{RegistryRel}: '{weaponKey}' silah yürüme kaydı yok (Dovus/k3/Silah yürüme setleri)");
            Assert.That(guidToPath.TryGetValue(guid, out string path), Is.True,
                $"'{weaponKey}' override'ı ({guid}) {K3Dir} altında yok");
            string rel = Path.GetRelativePath(RepoPaths.Root, path).Replace('\\', '/');
            Assert.That(IsGitIgnored(rel), Is.False, $"{rel} gitignored: APK başka makinede yürüme setini kaybeder");

            string[] lines = File.ReadAllLines(path);
            string controller = lines.Select(l => ControllerRef.Match(l)).Where(m => m.Success).Select(m => m.Groups[1].Value).FirstOrDefault();
            Assert.That(controller, Is.EqualTo(k3Guid), $"{rel}: temel controller k3.controller değil");

            Dictionary<string, string> swaps = OverrideClips(lines, clipNames);
            Assert.That(swaps.GetValueOrDefault(BaseIdle), Is.EqualTo($"Loko_{set}_Idle"), $"{rel}: bekleme klibi");
            Assert.That(swaps.GetValueOrDefault(BaseRun), Is.EqualTo($"Loko_{set}_Run"), $"{rel}: koşu klibi");
        }
    }

    static Dictionary<string, string> RegistryWeaponLocomotion()
    {
        string[] lines = File.ReadAllLines(Path.Combine(RepoPaths.UnityAssets, RegistryRel));
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        int start = Array.FindIndex(lines, l => l.TrimStart().StartsWith("_weaponLocomotion:", StringComparison.Ordinal));
        if (start < 0)
            return map;
        string key = null;
        for (int i = start + 1; i < lines.Length && lines[i].StartsWith("  ", StringComparison.Ordinal); i++)
        {
            Match k = EntryKey.Match(lines[i]);
            if (k.Success)
                key = k.Groups[1].Value;
            Match o = OverrideRef.Match(lines[i]);
            if (o.Success && key != null)
                map[key] = o.Groups[1].Value;
        }
        return map;
    }

    /// <summary>Özgün klip adı → override klibin adı (override boşsa özgün klip kalır).</summary>
    static Dictionary<string, string> OverrideClips(string[] lines, Dictionary<(string, string), string> clipNames)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 0; i + 1 < lines.Length; i++)
        {
            if (!lines[i].Contains("m_OriginalClip:", StringComparison.Ordinal))
                continue;
            string original = NameOf(ClipRef.Match(lines[i]), clipNames);
            Match o = ClipRef.Match(lines[i + 1]);
            string replacement = o.Success && o.Groups[1].Value != "0" ? NameOf(o, clipNames) : original;
            if (original != null)
                map[original] = replacement;
        }
        return map;
    }

    static string NameOf(Match m, Dictionary<(string, string), string> clipNames) =>
        m.Success && clipNames.TryGetValue((m.Groups[2].Value, m.Groups[1].Value), out string n) ? n : null;

    /// <summary>k3 klip FBX'lerinin .meta'sındaki clipAnimations: (guid, internalID) → klip adı.</summary>
    static Dictionary<(string, string), string> K3ClipNames()
    {
        var map = new Dictionary<(string, string), string>();
        foreach (string meta in Directory.EnumerateFiles(Path.Combine(RepoPaths.UnityAssets, K3Dir, "Klipler"), "*.fbx.meta"))
        {
            string guid = ReadMetaGuid(meta.Substring(0, meta.Length - ".meta".Length));
            string name = null;
            foreach (string line in File.ReadLines(meta))
            {
                Match n = ClipName.Match(line);
                if (n.Success)
                    name = n.Groups[1].Value;
                Match id = InternalId.Match(line);
                if (id.Success && name != null)
                    map[(guid, id.Groups[1].Value)] = name;
            }
        }
        return map;
    }

    static Dictionary<string, string> GuidIndex(string dir)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string meta in Directory.EnumerateFiles(dir, "*.meta", SearchOption.AllDirectories))
        {
            string asset = meta.Substring(0, meta.Length - ".meta".Length);
            map[ReadMetaGuid(asset)] = asset;
        }
        return map;
    }

    static string ReadMetaGuid(string assetPath) =>
        File.ReadLines(assetPath + ".meta").Select(l => MetaGuid.Match(l)).Where(m => m.Success)
            .Select(m => m.Groups[1].Value).FirstOrDefault() ?? string.Empty;

    static bool IsGitIgnored(string repoRelativePath)
    {
        var psi = new ProcessStartInfo("git", $"check-ignore -q \"{repoRelativePath}\"")
        {
            WorkingDirectory = RepoPaths.Root,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        using Process p = Process.Start(psi);
        p.WaitForExit();
        return p.ExitCode == 0;
    }
}
