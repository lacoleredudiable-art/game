using System.IO;
using Dovus.Core.Combat;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class V611RuntimeGapTests
{
    string _json = null!;
    SkillMotor _motor = null!;
    VerbExecutionData _hitboxes = null!;
    MobilityCcData _mobility = null!;

    [SetUp]
    public void SetUp()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        _json = File.ReadAllText(path);
        _motor = SkillMotor.FromJson(_json);
        _hitboxes = VerbExecutionData.FromJson(_json);
        _mobility = MobilityCcData.FromJson(_json);
    }

    [Test]
    public void SlotPassive_SameExtends_DifferentStacks_ZeroDurationIsNoOp()
    {
        var director = new SlotPassiveDirector();
        Assert.That(_motor.TryGetAdjective("1", out AdjectiveNode focused), Is.True);
        Assert.That(_motor.TryGetAdjective("2", out AdjectiveNode drain), Is.True);

        Assert.That(director.Activate(1, focused.Name, 5f, focused.EngineModifiers, 0), Is.True);
        Assert.That(director.Activate(1, focused.Name, 5f, focused.EngineModifiers, 1000), Is.True);
        Assert.That(director.ActiveCount, Is.EqualTo(1));
        Assert.That(director.Active[0].RemainingSec(1000), Is.EqualTo(9f).Within(0.001f));

        Assert.That(director.Activate(2, drain.Name, 7f, drain.EngineModifiers, 1000), Is.True);
        Assert.That(director.ActiveCount, Is.EqualTo(2));
        Assert.That(director.DamageMult, Is.EqualTo(1.35f * 0.95f).Within(0.001f));
        Assert.That(director.LifestealAdd, Is.EqualTo(0.3f).Within(0.001f));
        Assert.That(director.Activate(9, "Odaklama", 0f, JsonValue.Null, 1000), Is.False);
    }

    [Test]
    public void VerbOneAndFive_UseBindingFormula()
    {
        Assert.That(_hitboxes.TryGetHitbox(1, out VerbHitboxSpec one), Is.True);
        Assert.That(_hitboxes.TryGetHitbox(5, out VerbHitboxSpec five), Is.True);

        HitboxSize oneFocused = HitboxSizing.Resolve(
            one, _hitboxes.WeaponSizeMult(4), _hitboxes.AdjectiveSizeMult(1));
        Assert.That(oneFocused.Shape, Is.EqualTo("capsule"));
        Assert.That(oneFocused.ReachM, Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(oneFocused.RadiusM, Is.EqualTo(0.1f).Within(0.001f));

        HitboxSize fiveSpread = HitboxSizing.Resolve(
            five, _hitboxes.WeaponSizeMult(4), _hitboxes.AdjectiveSizeMult(5));
        Assert.That(fiveSpread.Shape, Is.EqualTo("sphere"));
        Assert.That(fiveSpread.RadiusM, Is.EqualTo(7.5f).Within(0.001f));
        Assert.That(_hitboxes.VfxKey("Su", 5, 5), Is.EqualTo("VFX_Su_5_5"));

        Assert.That(_hitboxes.TryGetHitbox(6, out VerbHitboxSpec six), Is.True);
        HitboxSize cone = HitboxSizing.Resolve(six, 1f, 1f);
        Assert.That(cone.Shape, Is.EqualTo("cone"));
        Assert.That(cone.RadiusM, Is.EqualTo(4f).Within(0.001f), "60° açı yarıçap sayılmamalı");
        Assert.That(cone.ReachM, Is.EqualTo(4f).Within(0.001f));
        Assert.That(_hitboxes.TryGetElementColor(2, out ElementVfxColor color), Is.True);
        Assert.That(color.Primary, Is.EqualTo("#1a8cff"));
    }

    [Test]
    public void Mobility_CombinesVerbAdjectiveAndWeapon()
    {
        Assert.That(_mobility.ResolveMobility(1, 1, 4), Is.EqualTo(SkillMobility.SlowedMove),
            "free + restrict + neutral lands on the middle tier");
        Assert.That(_mobility.ResolveMobility(3, 3, 1), Is.EqualTo(SkillMobility.FreeMove));
        Assert.That(_mobility.ResolveMobility(1, 4, 2), Is.EqualTo(SkillMobility.Rooted),
            "rooted_zorla cannot be removed by a mobile weapon");
        Assert.That(_mobility.ResolveMobility(12, 2, 5), Is.EqualTo(SkillMobility.Rooted));
    }

    [Test]
    public void CcPriorityStackingAndDurationComeFromBinding()
    {
        var board = new StatusBoard();
        board.ConfigureMobilityCc(_mobility);
        board.Apply(StatusKind.Root, 3000, 1f, "zone");
        board.Apply(StatusKind.Root, 3000, 1f, "zone");
        Assert.That(board.TryGet(StatusKind.Root, out double remaining, out _, out _), Is.True);
        Assert.That(remaining, Is.EqualTo(3000).Within(0.001), "aynı kök kaynağı süreyi uzatmaz, yeniler");
        board.Apply(StatusKind.Root, 1000, 1f, "skill");
        Assert.That(board.TryGet(StatusKind.Root, out remaining, out _, out _), Is.True);
        Assert.That(remaining, Is.EqualTo(3000).Within(0.001), "farklı kaynaklar toplanmaz, en uzun kalır");

        board.Apply(StatusKind.Stun, 2000, 1f);
        Assert.That(board.HasEffective(StatusKind.Stun), Is.True);
        Assert.That(board.HasEffective(StatusKind.Root), Is.False, "stun_root: root gizli");
        Assert.That(_mobility.ResolveCcDurationMs(StatusKind.Stun, 4, 800),
            Is.EqualTo(4000).Within(0.001), "Sabitleme ×2");
    }

    [Test]
    public void InterruptAndPoiseRulesAreCorePure()
    {
        Assert.That(_mobility.CanInterrupt(CastInterruptPhase.Startup, "dodge"), Is.True);
        Assert.That(_mobility.CanInterrupt(CastInterruptPhase.Active, "dodge"), Is.False);
        Assert.That(_mobility.CanInterrupt(CastInterruptPhase.Recovery, "swap"), Is.True);
        Assert.That(_mobility.TryPoiseBreak(26f, _mobility.PoiseThreshold("orta"), out int stunMs), Is.True);
        Assert.That(stunMs, Is.EqualTo(1000));
        Assert.That(_mobility.TryPoiseBreak(25f, _mobility.PoiseThreshold("orta"), out _), Is.False);
    }
}
