using System;
using Dovus.Core.Shared;
using Dovus.App.Boss;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class BossAttackSelectorTests
{
    const int MaxKindStreak = 2;
    const int MaxVariantStreak = 2;

    static readonly BossAttackKind[] Phase1Custom =
        { BossAttackKind.Slam, BossAttackKind.Volley, BossAttackKind.Pounce };

    static readonly BossAttackKind[] Phase2Custom =
        { BossAttackKind.Slam, BossAttackKind.FireCone, BossAttackKind.Volley, BossAttackKind.WebField };

    [Test]
    public void TwoHundredSteps_MatchesInlineReference()
    {
        const int steps = 200;
        const int seed = 0x2_6a_01;

        var refKinds = new BossAttackKind[steps];
        var refVariants = new SlamVariant?[steps];
        var selKinds = new BossAttackKind[steps];
        var selVariants = new SlamVariant?[steps];

        RunReference(seed, steps, refKinds, refVariants);
        RunSelector(seed, steps, selKinds, selVariants);

        for (int i = 0; i < steps; i++)
        {
            Assert.That(selKinds[i], Is.EqualTo(refKinds[i]), $"kind step {i}");
            Assert.That(selVariants[i], Is.EqualTo(refVariants[i]), $"variant step {i}");
        }
    }

    [Test]
    public void NoEligible_LeavesStateAndRngUntouched()
    {
        const int seed = 991;
        var rngRef = new Random(seed);
        var rngSel = new Random(seed);

        BossAttackKind? lastKind = null;
        int kindStreak = 0;
        SlamVariant? lastVar = null;
        int varStreak = 0;

        bool refOk = ReferenceTrySelect(
            enraged: false,
            ReadOnlySpan<BossAttackKind>.Empty,
            ReadOnlySpan<BossAttackKind>.Empty,
            _ => false,
            MaxKindStreak,
            MaxVariantStreak,
            rngRef,
            ref lastKind,
            ref kindStreak,
            ref lastVar,
            ref varStreak,
            out _);

        var selector = new BossAttackSelector();
        selector.TrySelect(
            false,
            ReadOnlySpan<BossAttackKind>.Empty,
            ReadOnlySpan<BossAttackKind>.Empty,
            _ => false,
            MaxKindStreak,
            MaxVariantStreak,
            rngSel,
            out _);

        Assert.That(refOk, Is.False);
        Assert.That(selector.LastAttackKind, Is.Null);
        Assert.That(selector.AttackKindStreak, Is.EqualTo(0));
        Assert.That(selector.LastVariant, Is.Null);
        Assert.That(selector.VariantStreak, Is.EqualTo(0));
        AssertNextRngValueEqual(rngRef, rngSel);
    }

    [Test]
    public void NonSlam_DoesNotConsumeVariantRng()
    {
        for (int seed = 0; seed < 40; seed++)
        {
            var rngKindOnly = new Random(seed);
            var rngSelector = new Random(seed);

            BossAttackKind? lastKind = null;
            int kindStreak = 0;
            SlamVariant? lastVar = null;
            int varStreak = 0;

            bool eligible(BossAttackKind k) => k == BossAttackKind.FireCone;

            bool ok = ReferenceTrySelect(
                enraged: true,
                Phase1Custom,
                Phase2Custom,
                eligible,
                MaxKindStreak,
                MaxVariantStreak,
                rngKindOnly,
                ref lastKind,
                ref kindStreak,
                ref lastVar,
                ref varStreak,
                out var refChoice);

            var selector = new BossAttackSelector();
            bool selOk = selector.TrySelect(
                true,
                Phase1Custom,
                Phase2Custom,
                eligible,
                MaxKindStreak,
                MaxVariantStreak,
                rngSelector,
                out var selChoice);

            Assert.That(selOk, Is.EqualTo(ok));
            if (!ok)
                continue;

            Assert.That(selChoice.Kind, Is.EqualTo(BossAttackKind.FireCone));
            Assert.That(selChoice.Variant, Is.Null);
            Assert.That(refChoice.Variant, Is.Null);
            AssertNextRngValueEqual(rngKindOnly, rngSelector);
        }
    }

    static void RunReference(int seed, int steps, BossAttackKind[] kinds, SlamVariant?[] variants)
    {
        var rng = new Random(seed);
        BossAttackKind? lastKind = null;
        int kindStreak = 0;
        SlamVariant? lastVar = null;
        int varStreak = 0;

        for (int i = 0; i < steps; i++)
        {
            bool enraged = (i & 4) != 0;
            ReadOnlySpan<BossAttackKind> phase1 = (i % 7) == 0
                ? ReadOnlySpan<BossAttackKind>.Empty
                : Phase1Custom;
            ReadOnlySpan<BossAttackKind> phase2 = (i % 11) == 0
                ? ReadOnlySpan<BossAttackKind>.Empty
                : Phase2Custom;

            bool eligible(BossAttackKind k)
            {
                if ((i % 5) == 0 && k == BossAttackKind.Volley)
                    return false;
                if ((i % 9) == 0 && k == BossAttackKind.Pounce)
                    return false;
                return true;
            }

            bool ok = ReferenceTrySelect(
                enraged,
                phase1,
                phase2,
                eligible,
                MaxKindStreak,
                MaxVariantStreak,
                rng,
                ref lastKind,
                ref kindStreak,
                ref lastVar,
                ref varStreak,
                out var choice);

            Assert.That(ok, Is.True, $"reference failed at step {i}");
            kinds[i] = choice.Kind;
            variants[i] = choice.Variant;
        }
    }

    static void RunSelector(int seed, int steps, BossAttackKind[] kinds, SlamVariant?[] variants)
    {
        var rng = new Random(seed);
        var selector = new BossAttackSelector();

        for (int i = 0; i < steps; i++)
        {
            bool enraged = (i & 4) != 0;
            ReadOnlySpan<BossAttackKind> phase1 = (i % 7) == 0
                ? ReadOnlySpan<BossAttackKind>.Empty
                : Phase1Custom;
            ReadOnlySpan<BossAttackKind> phase2 = (i % 11) == 0
                ? ReadOnlySpan<BossAttackKind>.Empty
                : Phase2Custom;

            bool eligible(BossAttackKind k)
            {
                if ((i % 5) == 0 && k == BossAttackKind.Volley)
                    return false;
                if ((i % 9) == 0 && k == BossAttackKind.Pounce)
                    return false;
                return true;
            }

            bool ok = selector.TrySelect(
                enraged,
                phase1,
                phase2,
                eligible,
                MaxKindStreak,
                MaxVariantStreak,
                rng,
                out var choice);

            Assert.That(ok, Is.True, $"selector failed at step {i}");
            kinds[i] = choice.Kind;
            variants[i] = choice.Variant;
        }
    }

    static bool ReferenceTrySelect(
        bool enraged,
        ReadOnlySpan<BossAttackKind> phase1Kinds,
        ReadOnlySpan<BossAttackKind> phase2Kinds,
        Func<BossAttackKind, bool> eligible,
        int maxSameKindStreak,
        int maxSameVariantStreak,
        Random rng,
        ref BossAttackKind? lastAttackKind,
        ref int attackKindStreak,
        ref SlamVariant? lastVariant,
        ref int variantStreak,
        out BossAttackChoice choice)
    {
        choice = default;

        ReadOnlySpan<BossAttackKind> phaseKinds = enraged
            ? (phase2Kinds.Length > 0
                ? phase2Kinds
                : BossAttackKindPicker.AllowedFor(true))
            : (phase1Kinds.Length > 0
                ? phase1Kinds
                : BossAttackKindPicker.AllowedFor(false));

        Span<BossAttackKind> allowed = stackalloc BossAttackKind[phaseKinds.Length];
        int n = 0;
        for (int i = 0; i < phaseKinds.Length; i++)
        {
            BossAttackKind k = phaseKinds[i];
            if (eligible(k))
                allowed[n++] = k;
        }

        if (n == 0)
            return false;

        BossAttackKind kind = BossAttackKindPicker.Pick(
            lastAttackKind,
            attackKindStreak,
            maxSameKindStreak,
            rng,
            allowed.Slice(0, n));
        attackKindStreak = BossAttackKindPicker.NextStreak(lastAttackKind, attackKindStreak, kind);
        lastAttackKind = kind;

        if (kind == BossAttackKind.Slam)
        {
            SlamVariant picked = SlamVariantPicker.Pick(
                lastVariant,
                variantStreak,
                maxSameVariantStreak,
                rng);
            variantStreak = SlamVariantPicker.NextStreak(lastVariant, variantStreak, picked);
            lastVariant = picked;
            choice = new BossAttackChoice(kind, picked);
        }
        else
            choice = new BossAttackChoice(kind, null);

        return true;
    }

    static void AssertNextRngValueEqual(Random a, Random b) =>
        Assert.That(a.Next(), Is.EqualTo(b.Next()));
}
