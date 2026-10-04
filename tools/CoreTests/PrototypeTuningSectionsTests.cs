using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

/// <summary>PLAN 2B.5b — PrototypeTuning bölüm göçü (kaynak + şema; Game derlemesi gerekmez).</summary>
[TestFixture]
public class PrototypeTuningSectionsTests
{
    static string Root() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string GameConfig() => Path.Combine(Root(), "unity", "Assets", "Scripts", "Game", "Config");

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
    public void LegacyFieldNames_MatchSectionFields()
    {
        var legacy = CollectLegacyFieldNames(Read("PrototypeTuning.Legacy.cs"));
        var fromCopy = CollectCopyLegacyRhsNames(Read("PrototypeTuning.Sections.cs"));
        Assert.That(legacy.Count, Is.EqualTo(247), "legacy düz alan sayısı");
        Assert.That(fromCopy.SetEquals(legacy), Is.True, "CopyLegacyFlatFieldsToSections legacy kümesini kapsamalı");
    }

    [Test]
    public void CopyLegacyFlatFieldsToSections_CoversEveryLegacyField()
    {
        var legacy = CollectLegacyFieldNames(Read("PrototypeTuning.Legacy.cs"));
        var copy = Read("PrototypeTuning.Sections.cs");
        int start = copy.IndexOf("void CopyLegacyFlatFieldsToSections()", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThan(0));
        int open = copy.IndexOf('{', start);
        int depth = 0;
        int close = open;
        for (int i = open; i < copy.Length; i++)
        {
            if (copy[i] == '{') depth++;
            else if (copy[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    close = i;
                    break;
                }
            }
        }

        var block = copy.Substring(open, close - open + 1);
        var assigned = new HashSet<string>();
        foreach (Match m in Regex.Matches(block, @"=\s*(\w+)\s*;"))
            assigned.Add(m.Groups[1].Value);
        foreach (string name in legacy)
            Assert.That(assigned, Does.Contain(name), $"CopyLegacyFlatFieldsToSections eksik: {name}");
    }

    static HashSet<string> CollectLegacyFieldNames(string legacySource)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(legacySource, @"private\s+(?:[\w<>,\.\s\[\]]+?)\s+(\w+)\s*="))
            names.Add(m.Groups[1].Value);
        return names;
    }

    static HashSet<string> CollectCopyLegacyRhsNames(string sectionsSource)
    {
        int start = sectionsSource.IndexOf("void CopyLegacyFlatFieldsToSections()", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThan(0));
        int open = sectionsSource.IndexOf('{', start);
        int depth = 0;
        int close = open;
        for (int i = open; i < sectionsSource.Length; i++)
        {
            if (sectionsSource[i] == '{') depth++;
            else if (sectionsSource[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    close = i;
                    break;
                }
            }
        }

        var block = sectionsSource.Substring(open, close - open + 1);
        return new HashSet<string>(
            Regex.Matches(block, @"=\s*(\w+)\s*;").Select(m => m.Groups[1].Value),
            StringComparer.Ordinal);
    }
}
