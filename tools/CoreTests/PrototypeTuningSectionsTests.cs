using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

/// <summary>PLAN 2B.5 — PrototypeTuning bölüm serileştirmesi (kaynak + şema; Game derlemesi gerekmez).</summary>
[TestFixture]
public class PrototypeTuningSectionsTests
{
    static string Root() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string GameConfig() => Path.Combine(Root(), "unity", "Assets", "Scripts", "Game", "Config");

    static string SectionsDir() => Path.Combine(GameConfig(), "Sections");

    static string Read(string rel) => File.ReadAllText(Path.Combine(GameConfig(), rel));

    [Test]
    public void PanelFields_JsonMemberNames_Unchanged()
    {
        const string expected =
            "PlayerMaxHp,DodgeGlideSpeedMps,BossApproachStopPadM,FollowSmoothTimeSec,LookAheadM," +
            "CameraShakePxToM,CameraDistanceM,CameraLookHeightM,CameraDefaultPitchDeg,CameraBossAimHeightM," +
            "CameraLockOnMinDistanceM,CameraLockOnMaxDistanceM,CameraLockOnDistancePerSepM," +
            "CameraLockOnMaxExtraDistanceM,CameraWindupDistanceMul,CameraWindupExtraHeightM," +
            "ReadoutAnchorRight,ReadoutPunchInSec,ShowFrameTimeHud,ShowDamageNumbers";

        var body = Read("PrototypeTuning.cs");
        int start = body.IndexOf("public sealed class PanelFields", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThan(0));
        int end = body.IndexOf("public PanelFields ToPanelFields", start, StringComparison.Ordinal);
        var panelBlock = body.Substring(start, end - start);
        var names = Regex.Matches(panelBlock, @"public\s+(?:bool|int|float)\s+(\w+)\s*;")
            .Select(m => m.Groups[1].Value)
            .ToArray();
        Assert.That(string.Join(",", names), Is.EqualTo(expected));
    }

    [Test]
    public void LegacyFlatPartial_IsRemoved()
    {
        Assert.That(File.Exists(Path.Combine(GameConfig(), "PrototypeTuning.Legacy.cs")), Is.False);
        var sections = Read("PrototypeTuning.Sections.cs");
        Assert.That(sections, Does.Not.Contain("CopyLegacyFlatFieldsToSections"));
        Assert.That(sections, Does.Not.Contain("ISerializationCallbackReceiver"));
        Assert.That(sections, Does.Not.Contain("OnAfterDeserialize"));
    }

    [Test]
    public void SectionTypes_ContainAllFormerLegacyFields()
    {
        var names = CollectSectionFieldNames();
        var legacy = CollectFormerLegacyFieldNames();
        Assert.That(names, Is.SupersetOf(legacy), "bölüm tipleri eski düz alan kümesini kapsamalı");
        Assert.That(names.Count, Is.EqualTo(248), "BossSettings.ActiveBossResourcePath bölümde ek alan");
    }

    [Test]
    public void PrototypeTuning_MainFile_AtMost500Lines()
    {
        int lines = File.ReadAllLines(Path.Combine(GameConfig(), "PrototypeTuning.cs")).Length;
        Assert.That(lines, Is.LessThanOrEqualTo(500));
    }

    static HashSet<string> CollectSectionFieldNames()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in Directory.GetFiles(SectionsDir(), "*.cs"))
        {
            string src = File.ReadAllText(path);
            foreach (Match m in Regex.Matches(src, @"public\s+(?:[\w<>,\.\s\[\]]+?)\s+(\w+)\s*="))
                names.Add(m.Groups[1].Value);
        }

        return names;
    }

    static HashSet<string> CollectFormerLegacyFieldNames()
    {
        string legacyPath = Path.Combine(GameConfig(), "PrototypeTuning.Legacy.cs");
        if (File.Exists(legacyPath))
            return CollectPrivateFieldNames(File.ReadAllText(legacyPath));

        string src = RunGitShow("origin/master:unity/Assets/Scripts/Game/Config/PrototypeTuning.Legacy.cs");
        return CollectPrivateFieldNames(src);
    }

    static HashSet<string> CollectPrivateFieldNames(string source)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(source, @"private\s+(?:[\w<>,\.\s\[\]]+?)\s+(\w+)\s*="))
            names.Add(m.Groups[1].Value);
        return names;
    }

    static string RunGitShow(string revPath)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git", $"show {revPath}")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = Root(),
        };
        using var p = System.Diagnostics.Process.Start(psi);
        string stdout = p!.StandardOutput.ReadToEnd();
        p.WaitForExit();
        Assert.That(p.ExitCode, Is.EqualTo(0));
        return stdout;
    }
}
