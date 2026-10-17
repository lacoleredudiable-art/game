using System;
using System.Collections.Generic;

namespace Dovus.Core.Boss
{
    /// <summary>
    /// Fazlara göre saldırı havuzu (KE-5.2, KE-6.2, KE-7.2). Sayılar <see cref="KulEjderiDefaults"/>'tan;
    /// burada yalnız hangi sayının hangi saldırıya ait olduğu yazılır.
    /// </summary>
    public static class KulEjderiAttackTable
    {
        static readonly KulEjderiAttackSpec[] All = Build();

        public static IReadOnlyList<KulEjderiAttackSpec> Specs => All;

        public static KulEjderiAttackSpec For(KulEjderiAttackId id)
        {
            foreach (KulEjderiAttackSpec spec in All)
            {
                if (spec.Id == id)
                    return spec;
            }

            throw new ArgumentOutOfRangeException(nameof(id), id, "Tabloda olmayan saldırı.");
        }

        /// <summary>Ağırlıkla seçilen normal saldırılar (duyurulu olanlar hariç).</summary>
        public static IReadOnlyList<KulEjderiAttackSpec> NormalPool(KulEjderiPhase phase)
        {
            var list = new List<KulEjderiAttackSpec>();
            foreach (KulEjderiAttackSpec spec in All)
            {
                if (spec.Phase == phase && !spec.Announced)
                    list.Add(spec);
            }

            return list;
        }

        /// <summary>Fazın duyurulu büyük saldırısı (KE-5.7, KE-6.7, KE-7.5).</summary>
        public static KulEjderiAttackSpec AnnouncedFor(KulEjderiPhase phase)
        {
            foreach (KulEjderiAttackSpec spec in All)
            {
                if (spec.Phase == phase && spec.Announced)
                    return spec;
            }

            throw new ArgumentOutOfRangeException(nameof(phase), phase, "Fazın duyurulu saldırısı yok.");
        }

        static KulEjderiAttackSpec[] Build()
        {
            const KulEjderiPhase p1 = KulEjderiPhase.Phase1;
            const KulEjderiPhase p2 = KulEjderiPhase.Phase2;
            const KulEjderiPhase p3 = KulEjderiPhase.Phase3;
            return new[]
            {
                // Faz 1
                new KulEjderiAttackSpec(KulEjderiAttackId.ClawSlam, "KE-5.4", p1,
                    KulEjderiDefaults.ClawSlamWeight, KulEjderiDefaults.ClawSlamTelegraphMs,
                    KulEjderiDefaults.ClawSlamActiveMs, KulEjderiDefaults.ClawSlamRecoveryMs,
                    KulEjderiDefaults.ClawSlamDamagePct, KulEjderiShape.Circle)
                { RadiusM = KulEjderiDefaults.ClawSlamRadiusM, Interruptible = true },
                new KulEjderiAttackSpec(KulEjderiAttackId.HeadSweep, "KE-5.5", p1,
                    KulEjderiDefaults.HeadSweepWeight, KulEjderiDefaults.HeadSweepTelegraphMs,
                    KulEjderiDefaults.HeadSweepActiveMs, KulEjderiDefaults.HeadSweepRecoveryMs,
                    KulEjderiDefaults.HeadSweepDamagePct, KulEjderiShape.Arc)
                {
                    RadiusM = KulEjderiDefaults.HeadSweepRadiusM, AngleDeg = KulEjderiDefaults.HeadSweepArcDeg,
                    KnockbackM = KulEjderiDefaults.HeadSweepKnockbackM, Interruptible = true
                },
                new KulEjderiAttackSpec(KulEjderiAttackId.EmberBreath, "KE-5.6", p1,
                    KulEjderiDefaults.EmberBreathWeight, KulEjderiDefaults.EmberBreathTelegraphMs,
                    KulEjderiDefaults.EmberBreathActiveMs, KulEjderiDefaults.EmberBreathRecoveryMs,
                    KulEjderiDefaults.EmberBreathTickDamagePct, KulEjderiShape.Cone)
                {
                    RadiusM = KulEjderiDefaults.EmberBreathRangeM, AngleDeg = KulEjderiDefaults.EmberBreathConeDeg,
                    TickMs = KulEjderiDefaults.EmberBreathTickMs, Interruptible = true
                },
                new KulEjderiAttackSpec(KulEjderiAttackId.EmberWave, "KE-5.7", p1,
                    0f, KulEjderiDefaults.EmberWaveTelegraphMs,
                    KulEjderiDefaults.EmberWaveActiveMs, KulEjderiDefaults.EmberWaveRecoveryMs,
                    KulEjderiDefaults.EmberWaveDamagePct, KulEjderiShape.Ring)
                { Announced = true },

                // Faz 2
                new KulEjderiAttackSpec(KulEjderiAttackId.ClawSlamHigh, "KE-6.4", p2,
                    KulEjderiDefaults.ClawSlamHighWeight, KulEjderiDefaults.ClawSlamHighTelegraphMs,
                    KulEjderiDefaults.ClawSlamHighActiveMs, KulEjderiDefaults.ClawSlamHighRecoveryMs,
                    KulEjderiDefaults.ClawSlamHighDamagePct, KulEjderiShape.Circle)
                { RadiusM = KulEjderiDefaults.ClawSlamHighRadiusM, Interruptible = true },
                new KulEjderiAttackSpec(KulEjderiAttackId.SkyEmberBreath, "KE-6.5", p2,
                    KulEjderiDefaults.SkyEmberBreathWeight, KulEjderiDefaults.SkyEmberBreathTelegraphMs,
                    KulEjderiDefaults.SkyEmberBreathActiveMs, KulEjderiDefaults.SkyEmberBreathRecoveryMs,
                    KulEjderiDefaults.SkyEmberBreathTickDamagePct, KulEjderiShape.Strip)
                {
                    RadiusM = KulEjderiDefaults.SkyEmberBreathStripWidthM,
                    TickMs = KulEjderiDefaults.SkyEmberBreathTickMs, Interruptible = true, InterruptNeedsRange = true
                },
                new KulEjderiAttackSpec(KulEjderiAttackId.WingSweep, "KE-6.6", p2,
                    KulEjderiDefaults.WingSweepWeight, KulEjderiDefaults.WingSweepTelegraphMs,
                    KulEjderiDefaults.WingSweepActiveMs, KulEjderiDefaults.WingSweepRecoveryMs,
                    KulEjderiDefaults.WingSweepDamagePct, KulEjderiShape.Arena)
                {
                    KnockbackM = KulEjderiDefaults.WingSweepKnockbackM, Interruptible = false,
                    MinRepeatGapMs = KulEjderiDefaults.WingSweepMinGapMs
                },
                new KulEjderiAttackSpec(KulEjderiAttackId.SkyCollapse, "KE-6.7", p2,
                    0f, KulEjderiDefaults.SkyCollapseTelegraphMs,
                    KulEjderiDefaults.SkyCollapseActiveMs, KulEjderiDefaults.SkyCollapseRecoveryMs,
                    KulEjderiDefaults.SkyCollapseDamagePct, KulEjderiShape.Barrage)
                { RadiusM = KulEjderiDefaults.SkyCollapseRadiusM, Announced = true },

                // Faz 3
                new KulEjderiAttackSpec(KulEjderiAttackId.ClawSweep, "KE-7.2", p3,
                    KulEjderiDefaults.ClawSweepWeight, KulEjderiDefaults.ClawSweepTelegraphMs,
                    KulEjderiDefaults.ClawSweepActiveMs, KulEjderiDefaults.ClawSweepRecoveryMs,
                    KulEjderiDefaults.ClawSweepDamagePct, KulEjderiShape.Arc)
                {
                    RadiusM = KulEjderiDefaults.ClawSweepRadiusM, AngleDeg = KulEjderiDefaults.ClawSweepArcDeg,
                    KnockbackM = KulEjderiDefaults.ClawSweepKnockbackM, Interruptible = true
                },
                new KulEjderiAttackSpec(KulEjderiAttackId.EmberRing, "KE-7.3", p3,
                    KulEjderiDefaults.EmberRingWeight, KulEjderiDefaults.EmberRingTelegraphMs,
                    KulEjderiDefaults.EmberRingActiveMs, KulEjderiDefaults.EmberRingRecoveryMs,
                    KulEjderiDefaults.EmberRingDamagePct, KulEjderiShape.Ring)
                { RadiusM = KulEjderiDefaults.EmberRingMaxRadiusM, Interruptible = true },
                new KulEjderiAttackSpec(KulEjderiAttackId.WideEmberBreath, "KE-7.4", p3,
                    KulEjderiDefaults.WideEmberBreathWeight, KulEjderiDefaults.WideEmberBreathTelegraphMs,
                    KulEjderiDefaults.WideEmberBreathActiveMs, KulEjderiDefaults.WideEmberBreathRecoveryMs,
                    KulEjderiDefaults.WideEmberBreathTickDamagePct, KulEjderiShape.Cone)
                {
                    RadiusM = KulEjderiDefaults.WideEmberBreathRangeM, AngleDeg = KulEjderiDefaults.WideEmberBreathConeDeg,
                    TickMs = KulEjderiDefaults.WideEmberBreathTickMs, Interruptible = true
                },
                new KulEjderiAttackSpec(KulEjderiAttackId.CraterHeart, "KE-7.5", p3,
                    0f, KulEjderiDefaults.CraterHeartTelegraphMs,
                    KulEjderiDefaults.CraterHeartActiveMs, KulEjderiDefaults.CraterHeartRecoveryMs,
                    KulEjderiDefaults.CraterHeartDamagePct, KulEjderiShape.Arena)
                { RadiusM = KulEjderiDefaults.CraterHeartArenaRadiusM, Announced = true }
            };
        }
    }
}
