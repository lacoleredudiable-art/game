using Dovus.App.Casting;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Casting;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class ExecutorKindHitboxDimsTests
{
    [Test]
    public void MeleeHitboxUsesReachAndRadius()
    {
        var spec = new VerbHitboxSpec("sphere", 3f, 1f, false, 0f, false, string.Empty);
        var size = new HitboxSize("sphere", 0.8f, 2.5f, 0f);
        float radius = 0f;
        float range = 0f;
        float duration = 1f;
        int spawn = 1;

        ExecutorKindHitboxDims.Apply(
            SkillExecutorKind.MeleeHitbox,
            spec,
            size,
            dashDurationSec: 0f,
            reflectDurationSec: 0f,
            minionDurationSec: 0f,
            lifetimeAdd: 0f,
            slotLifeSec: 0f,
            engineMinionCount: 1,
            ref radius,
            ref range,
            ref duration,
            ref spawn);

        Assert.That(range, Is.EqualTo(2.5f).Within(0.001f));
        Assert.That(radius, Is.EqualTo(0.8f).Within(0.001f));
    }

    [Test]
    public void FieldAuraAddsSlotLifeToDuration()
    {
        var spec = new VerbHitboxSpec("sphere", 2f, 0f, true, 0f, false, string.Empty);
        var size = new HitboxSize("sphere", 1.2f, 1.2f, 0f);
        float radius = 0f;
        float range = 0f;
        float duration = 1f;
        int spawn = 1;

        ExecutorKindHitboxDims.Apply(
            SkillExecutorKind.FieldAura,
            spec,
            size,
            dashDurationSec: 0f,
            reflectDurationSec: 0f,
            minionDurationSec: 0f,
            lifetimeAdd: 0f,
            slotLifeSec: 0.35f,
            engineMinionCount: 1,
            ref radius,
            ref range,
            ref duration,
            ref spawn);

        Assert.That(duration, Is.EqualTo(1.35f).Within(0.001f));
        Assert.That(radius, Is.EqualTo(1.2f).Within(0.001f));
    }

    [Test]
    public void SummonSetsMinionCountFromEngine()
    {
        var spec = new VerbHitboxSpec("sphere", 1f, 1f, false, 0f, false, string.Empty);
        var size = new HitboxSize("sphere", 0.5f, 1f, 0f);
        float radius = 0f;
        float range = 0f;
        float duration = 0f;
        int spawn = 1;

        ExecutorKindHitboxDims.Apply(
            SkillExecutorKind.Summon,
            spec,
            size,
            dashDurationSec: 0f,
            reflectDurationSec: 0f,
            minionDurationSec: 5f,
            lifetimeAdd: 0.25f,
            slotLifeSec: 0f,
            engineMinionCount: 3,
            ref radius,
            ref range,
            ref duration,
            ref spawn);

        Assert.That(spawn, Is.EqualTo(3));
        Assert.That(duration, Is.EqualTo(5.25f).Within(0.001f));
    }
}
