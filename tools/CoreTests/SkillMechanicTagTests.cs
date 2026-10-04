using Dovus.Core.Grammar;
using Dovus.Core.Shared;
using Dovus.Core.Portal;
using Dovus.Core.Team;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoreTests;

[TestFixture]
public partial class SkillMechanicTagTests
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

    internal static SkillMotor LoadMotorPublic() => LoadMotor();

    [Test]
    public void JsonPortalOps_MatchExpectedBothWays()
    {
        Dictionary<string, PortalOp> fromJson = PortalOpTable.FromMotor(LoadMotor());
        Assert.That(fromJson, Is.EquivalentTo(ExpectedPortalOps));
        foreach (KeyValuePair<string, PortalOp> kv in ExpectedPortalOps)
            Assert.That(fromJson[kv.Key], Is.EqualTo(kv.Value));
    }

    [Test]
    public void JsonTeamOps_MatchExpectedBothWays()
    {
        Dictionary<string, TeamOp> fromJson = TeamOpTable.FromMotor(LoadMotor());
        Assert.That(fromJson, Is.EquivalentTo(ExpectedTeamOps));
        foreach (KeyValuePair<string, TeamOp> kv in ExpectedTeamOps)
            Assert.That(fromJson[kv.Key], Is.EqualTo(kv.Value));
    }

    [Test]
    public void UnknownSkillId_ResolvesToNone()
    {
        var portalOps = new Dictionary<string, PortalOp> { ["9-9"] = PortalOp.Swap };
        var teamOps = new Dictionary<string, TeamOp> { ["9-9"] = TeamOp.Mine };
        Assert.That(PortalOpTable.Resolve((SkillId)"1-1", portalOps), Is.EqualTo(PortalOp.None));
        Assert.That(TeamOpTable.Resolve((SkillId)"1-1", teamOps), Is.EqualTo(TeamOp.None));
        Assert.That(new PortalSystem().IsPortalSkill((SkillId)"1-1"), Is.False);
        Assert.That(new TeamComboSystem().IsTeamSkill((SkillId)"1-1"), Is.False);
    }

    [Test]
    public void TeamMarkerOps_ReturnNonePulse()
    {
        var team = new TeamComboSystem(TeamOpTable.FromMotor(LoadMotor()));
        Disc boss = new Disc(true, 0f, 4f, 0.85f, PortalSystem.ClearGapM);
        FakeAlly ally = new FakeAlly { Id = 1 };
        Assert.That(team.Cast((SkillId)"3-10", ally, null, new[] { ally }, boss), Is.EqualTo(TeamPulse.None));
        Assert.That(team.Cast((SkillId)"10-10", ally, null, new[] { ally }, boss), Is.EqualTo(TeamPulse.None));
    }

    [Test]
    public void PortalCast_JsonTable_MatchesLegacyTable()
    {
        SkillMotor motor = LoadMotor();
        PortalSystem baseline = new PortalSystem(ExpectedPortalOps);
        PortalSystem tagged = new PortalSystem(PortalOpTable.FromMotor(motor));
        Disc boss = new Disc(true, 0f, 4f, 0.85f, PortalSystem.ClearGapM);
        Body caster = new Body(1, 0f, 0f, 0f, 0.5f, false, false);
        Body ally = new Body(2, 0f, 4f, 0f, 0.5f, false, false);

        foreach (string id in ExpectedPortalOps.Keys.OrderBy(x => x))
        {
            baseline.Clear();
            tagged.Clear();
            baseline.Cast((SkillId)id, caster, ally, null, boss);
            tagged.Cast((SkillId)id, caster, ally, null, boss);
            Assert.That(tagged.Doors.Count, Is.EqualTo(baseline.Doors.Count), id);
            Assert.That(tagged.Drain().Count, Is.EqualTo(baseline.Drain().Count), id);
            Assert.That(tagged.HasAnchor, Is.EqualTo(baseline.HasAnchor), id);
        }
    }

    [Test]
    public void TeamCast_JsonTable_MatchesExpectedTable()
    {
        SkillMotor motor = LoadMotor();
        TeamComboSystem baseline = new TeamComboSystem(ExpectedTeamOps);
        TeamComboSystem tagged = new TeamComboSystem(TeamOpTable.FromMotor(motor));
        Disc boss = new Disc(true, 0f, 4f, 0.85f, PortalSystem.ClearGapM);
        FakeAlly caster = new FakeAlly { Id = 1, X = 0f, Z = 0f };
        FakeAlly target = new FakeAlly { Id = 2, X = 0f, Z = 4f };
        IAllyPlayer[] allies = { caster, target };

        foreach (KeyValuePair<string, TeamOp> kv in ExpectedTeamOps)
        {
            if (kv.Value == TeamOp.Marker)
                continue;
            baseline.Clear();
            tagged.Clear();
            TeamPulse a = baseline.Cast((SkillId)kv.Key, caster, target, allies, boss);
            TeamPulse b = tagged.Cast((SkillId)kv.Key, caster, target, allies, boss);
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
