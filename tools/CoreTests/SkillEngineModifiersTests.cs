using Dovus.Core.Data;
using Dovus.Core.Grammar;
using NUnit.Framework;
using System.IO;

using Dovus.Core.Shared;
namespace CoreTests;

[TestFixture]
public class SkillEngineModifiersTests
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

    [Test]
    public void All144Skills_EngineViewMatchesRawEngineModifiers()
    {
        SkillMotor motor = SkillMotor.FromJson(File.ReadAllText(JsonPath()));
        int count = 0;
        for (int verb = 1; verb <= 12; verb++)
        for (int adjective = 1; adjective <= 12; adjective++)
        {
            SkillResolution skill = motor.Resolve(new[] { verb, adjective });
            string label = $"{verb}-{adjective}";
            Assert.That(skill.IsEmpty, Is.False, label);

            JsonValue raw = skill.EngineModifiers;
            SkillEngineModifiers view = skill.Engine;

            Assert.That(view.IsNull, Is.EqualTo(raw.IsNull), label);
            Assert.That(view.HasElementMult, Is.EqualTo(raw.Has("element_mult")), label);
            Assert.That(view.HasCritChanceAdd, Is.EqualTo(raw.Has("crit_chance_add")), label);
            Assert.That(view.HasLifesteal, Is.EqualTo(raw.Has("lifesteal")), label);
            Assert.That(view.HasDuplicateDamageMult, Is.EqualTo(raw.Has("duplicate_damage_mult")), label);
            Assert.That(view.HasBounceDamageMult, Is.EqualTo(raw.Has("bounce_damage_mult")), label);

            foreach (float fb in new[] { 0f, 1f, 3f, 4f })
            {
                Assert.That(view.Aoe(false), Is.EqualTo(raw["aoe"].AsBool(false)), label);
                Assert.That(view.ArmorAdd(fb), Is.EqualTo(raw["armor_add"].AsFloat(fb)), label);
                Assert.That(view.ApplyLifesteal(fb), Is.EqualTo(raw["apply_lifesteal"].AsFloat(fb)), label);
                Assert.That(view.BuffArmor(fb), Is.EqualTo(raw["buff_armor"].AsFloat(fb)), label);
                Assert.That(view.BuffDamage(fb), Is.EqualTo(raw["buff_damage"].AsFloat(fb)), label);
                Assert.That(view.BuffDurationSec(fb), Is.EqualTo(raw["buff_duration_sec"].AsFloat(fb)), label);
                Assert.That(view.CcDurationSec(fb), Is.EqualTo(raw["cc_duration_sec"].AsFloat(fb)), label);
                Assert.That(view.ChannelSec(fb), Is.EqualTo(raw["channel_sec"].AsFloat(fb)), label);
                Assert.That(view.DebuffArmor(fb), Is.EqualTo(raw["debuff_armor"].AsFloat(fb)), label);
                Assert.That(view.DebuffDurationSec(fb), Is.EqualTo(raw["debuff_duration_sec"].AsFloat(fb)), label);
                Assert.That(view.DashDistanceM(fb), Is.EqualTo(raw["dash_distance_m"].AsFloat(fb)), label);
                Assert.That(view.DuplicateDamageMult(fb), Is.EqualTo(raw["duplicate_damage_mult"].AsFloat(fb)), label);
                Assert.That(view.DuplicateDelaySec(fb), Is.EqualTo(raw["duplicate_delay_sec"].AsFloat(fb)), label);
                Assert.That(view.ElementMult(fb), Is.EqualTo(raw["element_mult"].AsFloat(fb)), label);
                Assert.That(view.HitboxScaleMult(fb), Is.EqualTo(raw["hitbox_scale_mult"].AsFloat(fb)), label);
                Assert.That(view.LifetimeAdd(fb), Is.EqualTo(raw["lifetime_add"].AsFloat(fb)), label);
                Assert.That(view.Lifesteal(fb), Is.EqualTo(raw["lifesteal"].AsFloat(fb)), label);
                Assert.That(view.MinionDurationSec(fb), Is.EqualTo(raw["minion_duration_sec"].AsFloat(fb)), label);
                Assert.That(view.ReflectDurationSec(fb), Is.EqualTo(raw["reflect_duration_sec"].AsFloat(fb)), label);
                Assert.That(view.ReflectRatio(fb), Is.EqualTo(raw["reflect_ratio"].AsFloat(fb)), label);
                Assert.That(view.SelfDamageBuff(fb), Is.EqualTo(raw["self_damage_buff"].AsFloat(fb)), label);
                Assert.That(view.ShieldAbsorb(fb), Is.EqualTo(raw["shield_absorb"].AsFloat(fb)), label);
                Assert.That(view.TempoDurationSec(fb), Is.EqualTo(raw["tempo_duration_sec"].AsFloat(fb)), label);
                Assert.That(view.TickRateMult(fb), Is.EqualTo(raw["tick_rate_mult"].AsFloat(fb)), label);
            }

            foreach (int fb in new[] { 0, 1, 3 })
            {
                Assert.That(view.BounceTargets(fb), Is.EqualTo(raw["bounce_targets"].AsInt(fb)), label);
                Assert.That(view.CleanseCount(fb), Is.EqualTo(raw["cleanse_count"].AsInt(fb)), label);
                Assert.That(view.MaxTargets(fb), Is.EqualTo(raw["max_targets"].AsInt(fb)), label);
                Assert.That(view.MinionCount(fb), Is.EqualTo(raw["minion_count"].AsInt(fb)), label);
            }

            Assert.That(view.DuplicateCast(false), Is.EqualTo(raw["duplicate_cast"].AsBool(false)), label);
            Assert.That(view.IgnoreArmor(false), Is.EqualTo(raw["ignore_armor"].AsBool(false)), label);
            Assert.That(view.CritChanceAdd(0f), Is.EqualTo(raw["crit_chance_add"].AsFloat(0f)), label);
            Assert.That(view.BounceDamageMult(1f), Is.EqualTo(raw["bounce_damage_mult"].AsFloat(1f)), label);

            foreach (string fb in new[] { "", "fallback" })
            {
                Assert.That(view.TrajectoryOverride(fb), Is.EqualTo(raw["trajectory_override"].AsString(fb)), label);
                Assert.That(view.HitboxOverride(fb), Is.EqualTo(raw["hitbox_override"].AsString(fb)), label);
            }

            count++;
        }

        Assert.That(count, Is.EqualTo(144));
    }
}
