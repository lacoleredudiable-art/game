using Dovus.Core.Grammar;
using Dovus.Core.Portal;
using Dovus.Core.Team;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoreTests;

[TestFixture]
public class SkillMechanicTagTests
{
    static string JsonPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        }
        Assert.That(File.Exists(path), Is.True);
        return path;
    }

    static SkillMotor LoadMotor() => SkillMotor.FromJson(File.ReadAllText(JsonPath()));

    [Test]
    public void JsonPortalOps_MatchLegacyBothWays()
    {
        Dictionary<string, PortalOp> fromJson = PortalOpTable.FromMotor(LoadMotor());
        Assert.That(fromJson, Is.EquivalentTo(PortalOpTable.Legacy));
        foreach (KeyValuePair<string, PortalOp> kv in PortalOpTable.Legacy)
            Assert.That(fromJson[kv.Key], Is.EqualTo(kv.Value));
    }

    [Test]
    public void JsonTeamOps_MatchLegacyBothWays()
    {
        Dictionary<string, TeamOp> fromJson = TeamOpTable.FromMotor(LoadMotor());
        Assert.That(fromJson, Is.EquivalentTo(TeamOpTable.Legacy));
        foreach (KeyValuePair<string, TeamOp> kv in TeamOpTable.Legacy)
            Assert.That(fromJson[kv.Key], Is.EqualTo(kv.Value));
    }

    [Test]
    public void UnknownSkillId_ResolvesToNone()
    {
        var portal = new Dictionary<string, PortalOp> { ["9-9"] = PortalOp.Swap };
        var team = new Dictionary<string, TeamOp> { ["9-9"] = TeamOp.Mine };
        Assert.That(PortalOpTable.Resolve("1-1", portal), Is.EqualTo(PortalOp.None));
        Assert.That(TeamOpTable.Resolve("1-1", team), Is.EqualTo(TeamOp.None));
        Assert.That(PortalSystem.IsPortalSkill("1-1"), Is.False);
        Assert.That(TeamComboSystem.IsTeamSkill("1-1"), Is.False);
    }

    [Test]
    public void TeamMarkerOps_ReturnNonePulse()
    {
        var team = new TeamComboSystem(TeamOpTable.FromMotor(LoadMotor()));
        Disc boss = new Disc(true, 0f, 4f, 0.85f, PortalSystem.ClearGapM);
        FakeAlly ally = new FakeAlly { Id = 1 };
        Assert.That(team.Cast("3-10", ally, null, new[] { ally }, boss), Is.EqualTo(TeamPulse.None));
        Assert.That(team.Cast("10-10", ally, null, new[] { ally }, boss), Is.EqualTo(TeamPulse.None));
    }

    [Test]
    public void PortalCast_JsonTable_MatchesLegacyTable()
    {
        SkillMotor motor = LoadMotor();
        PortalSystem legacy = new PortalSystem();
        PortalSystem tagged = new PortalSystem(PortalOpTable.FromMotor(motor));
        Disc boss = new Disc(true, 0f, 4f, 0.85f, PortalSystem.ClearGapM);
        Body caster = new Body(1, 0f, 0f, 0f, 0.5f, false, false);
        Body ally = new Body(2, 0f, 4f, 0f, 0.5f, false, false);

        foreach (string id in PortalOpTable.Legacy.Keys.OrderBy(x => x))
        {
            legacy.Clear();
            tagged.Clear();
            legacy.Cast(id, caster, ally, null, boss);
            tagged.Cast(id, caster, ally, null, boss);
            Assert.That(tagged.Doors.Count, Is.EqualTo(legacy.Doors.Count), id);
            Assert.That(tagged.Drain().Count, Is.EqualTo(legacy.Drain().Count), id);
            Assert.That(tagged.HasAnchor, Is.EqualTo(legacy.HasAnchor), id);
        }
    }

    [Test]
    public void TeamCast_JsonTable_MatchesLegacyTable()
    {
        SkillMotor motor = LoadMotor();
        TeamComboSystem legacy = new TeamComboSystem();
        TeamComboSystem tagged = new TeamComboSystem(TeamOpTable.FromMotor(motor));
        Disc boss = new Disc(true, 0f, 4f, 0.85f, PortalSystem.ClearGapM);
        FakeAlly caster = new FakeAlly { Id = 1, X = 0f, Z = 0f };
        FakeAlly target = new FakeAlly { Id = 2, X = 0f, Z = 4f };
        IAllyPlayer[] allies = { caster, target };

        foreach (KeyValuePair<string, TeamOp> kv in TeamOpTable.Legacy)
        {
            if (kv.Value == TeamOp.Marker)
                continue;
            legacy.Clear();
            tagged.Clear();
            TeamPulse a = legacy.Cast(kv.Key, caster, target, allies, boss);
            TeamPulse b = tagged.Cast(kv.Key, caster, target, allies, boss);
            Assert.That(b.Stunned, Is.EqualTo(a.Stunned), kv.Key);
            Assert.That(b.StunSec, Is.EqualTo(a.StunSec).Within(1e-4f), kv.Key);
            Assert.That(b.BossIncomingMult, Is.EqualTo(a.BossIncomingMult).Within(1e-4f), kv.Key);
            Assert.That(b.MineMult, Is.EqualTo(a.MineMult).Within(1e-4f), kv.Key);
            Assert.That(b.CopiedSkill, Is.EqualTo(a.CopiedSkill), kv.Key);
        }
    }

    sealed class FakeAlly : IAllyPlayer
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Radius { get; set; } = 0.5f;
        public float HpRatio => 1f;
        public string LastSkillId { get; set; } = string.Empty;
        public bool TemplateOwnsPosition { get; set; }
    }
}
