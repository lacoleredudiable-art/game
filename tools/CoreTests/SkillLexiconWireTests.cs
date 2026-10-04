using System.IO;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class SkillLexiconWireTests
{
    [Test]
    public void All144Combos_WireEnumsRecognizeJsonValues()
    {
        SkillMotor motor = SkillResolutionSnapshotUtil.LoadMotor();
        for (int verb = 1; verb <= 12; verb++)
        for (int adj = 1; adj <= 12; adj++)
        {
            SkillResolution skill = motor.Resolve(new[] { verb, adj });
            string label = $"{verb}-{adj}";
            AssertKnown(HitboxWire.Parse(skill.Presentation.Hitbox), label, "hitbox");
            AssertKnown(CastMobilityWire.Parse(skill.Targeting.CastMobility), label, "cast_mobility");
            AssertKnown(TargetModeWire.Parse(skill.Targeting.Mode), label, "target_mode");
            AssertKnown(VerbFamilyWire.Parse(skill.Presentation.VerbFamily), label, "verb_family");
            AssertKnown(SkillActionWire.Parse(skill.Presentation.Action), label, "action");
            AssertKnown(LengthMobilityWire.Parse(skill.Length.Mobility), label, "length_mobility");
            AssertKnown(LengthRoleWire.Parse(skill.Length.Role), label, "length_role");
        }
    }

    static void AssertKnown(HitboxWire wire, string label, string field) =>
        Assert.That(wire.Value, Is.Not.EqualTo(HitboxWire.Kind.Unknown), $"{label} {field} '{wire}'");

    static void AssertKnown(CastMobilityWire wire, string label, string field) =>
        Assert.That(wire.Value, Is.Not.EqualTo(CastMobilityWire.Kind.Unknown), $"{label} {field} '{wire}'");

    static void AssertKnown(TargetModeWire wire, string label, string field) =>
        Assert.That(wire.Value, Is.Not.EqualTo(TargetModeWire.Kind.Unknown), $"{label} {field} '{wire}'");

    static void AssertKnown(VerbFamilyWire wire, string label, string field) =>
        Assert.That(wire.Value, Is.Not.EqualTo(VerbFamilyWire.Kind.Unknown), $"{label} {field} '{wire}'");

    static void AssertKnown(SkillActionWire wire, string label, string field) =>
        Assert.That(wire.Value, Is.Not.EqualTo(SkillActionWire.Kind.Unknown), $"{label} {field} '{wire}'");

    static void AssertKnown(LengthMobilityWire wire, string label, string field) =>
        Assert.That(wire.Value, Is.Not.EqualTo(LengthMobilityWire.Kind.Unknown), $"{label} {field} '{wire}'");

    static void AssertKnown(LengthRoleWire wire, string label, string field) =>
        Assert.That(wire.Value, Is.Not.EqualTo(LengthRoleWire.Kind.Unknown), $"{label} {field} '{wire}'");
}
