using Dovus.Core.Status;

namespace Dovus.Core.Boss
{
    /// <summary>
    /// Boss CC kapısı. Poise/öncelik <see cref="StatusBoard.HasEffective"/> ile okunur.
    /// Kök yalnız hücum/sıçrama/atış başlatmayı keser. Sersemlik her saldırıyı keser.
    /// </summary>
    public static class BossAttackControl
    {
        public static BossAttackMotion MotionOf(BossAttackKind kind) =>
            kind switch
            {
                BossAttackKind.Slam => BossAttackMotion.Standing,
                BossAttackKind.FireCone => BossAttackMotion.Standing,
                BossAttackKind.Volley => BossAttackMotion.Standing,
                BossAttackKind.WebField => BossAttackMotion.Standing,
                BossAttackKind.Pounce => BossAttackMotion.Leap,
                _ => BossAttackMotion.Standing
            };

        /// <summary>Özel/cast saldırı: Silence keser, Disarm kesmez (FireCone, Volley, WebField).</summary>
        public static bool IsSpecial(BossAttackKind kind) =>
            kind is BossAttackKind.FireCone or BossAttackKind.Volley or BossAttackKind.WebField;

        public static bool IsMovement(BossAttackMotion motion) =>
            motion is BossAttackMotion.Charge or BossAttackMotion.Leap or BossAttackMotion.Dash;

        public static BossAttackGate Evaluate(StatusBoard board, BossAttackMotion motion)
        {
            if (board == null)
                return new BossAttackGate(true, false, 1f);

            bool immune = board.IsAttackLockImmune;
            bool hardLock = !immune && (
                board.HasEffective(StatusKind.Stun)
                || board.Has(StatusKind.Stasis)
                || board.HasEffective(StatusKind.Fear));
            if (hardLock)
                return new BossAttackGate(false, true, 1f);

            if (!immune && board.HasEffective(StatusKind.Root) && IsMovement(motion))
                return new BossAttackGate(false, false, 1f);

            float speed = board.ActionSpeedMult;
            if (speed <= 0f)
                speed = 1f;
            return new BossAttackGate(true, false, speed);
        }

        /// <summary>
        /// Sersemlik her saldırıyı keser. Silence özel/cast (FireCone, Volley) başlatmayı ve
        /// hazırlığı keser. Disarm temel/yakın (Slam) başlatmayı ve hazırlığı keser.
        /// Süre tahtadaki durumdan gelir; bu kapı yalnız o anı okur.
        /// </summary>
        public static BossAttackGate Gate(
            StatusBoard board,
            BossAttackMotion motion,
            BossAttackKind kind,
            bool staggered)
        {
            if (staggered)
                return new BossAttackGate(false, true, 1f);

            BossAttackGate gate = Evaluate(board, motion);
            if (!gate.CanStart || board == null)
                return gate;

            bool special = IsSpecial(kind);
            if (special && board.HasEffective(StatusKind.Silence))
                return new BossAttackGate(false, true, gate.PhaseSpeed);
            if (!special && board.HasDisarm)
                return new BossAttackGate(false, true, gate.PhaseSpeed);
            return gate;
        }
    }
}
