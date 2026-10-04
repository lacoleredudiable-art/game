using System;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;

namespace Dovus.App.Boss
{
    public readonly struct BossAttackChoice
    {
        public BossAttackChoice(BossAttackKind kind, SlamVariant? variant)
        {
            Kind = kind;
            Variant = variant;
        }

        public BossAttackKind Kind { get; }
        public SlamVariant? Variant { get; }
    }

    public sealed class BossAttackSelector
    {
        BossAttackKind? _lastAttackKind;
        int _attackKindStreak;
        SlamVariant? _lastVariant;
        int _variantStreak;

        public BossAttackKind? LastAttackKind => _lastAttackKind;
        public int AttackKindStreak => _attackKindStreak;
        public SlamVariant? LastVariant => _lastVariant;
        public int VariantStreak => _variantStreak;

        public bool TrySelect(
            bool enraged,
            ReadOnlySpan<BossAttackKind> phase1Kinds,
            ReadOnlySpan<BossAttackKind> phase2Kinds,
            Func<BossAttackKind, bool> eligible,
            int maxSameKindStreak,
            int maxSameVariantStreak,
            Random rng,
            out BossAttackChoice choice)
        {
            choice = default;
            if (eligible == null)
                throw new ArgumentNullException(nameof(eligible));
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

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
                _lastAttackKind,
                _attackKindStreak,
                maxSameKindStreak,
                rng,
                allowed.Slice(0, n));
            _attackKindStreak = BossAttackKindPicker.NextStreak(_lastAttackKind, _attackKindStreak, kind);
            _lastAttackKind = kind;

            if (kind == BossAttackKind.Slam)
            {
                SlamVariant picked = SlamVariantPicker.Pick(
                    _lastVariant,
                    _variantStreak,
                    maxSameVariantStreak,
                    rng);
                _variantStreak = SlamVariantPicker.NextStreak(_lastVariant, _variantStreak, picked);
                _lastVariant = picked;
                choice = new BossAttackChoice(kind, picked);
            }
            else
                choice = new BossAttackChoice(kind, null);

            return true;
        }
    }
}
