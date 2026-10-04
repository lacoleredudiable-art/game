using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Status;

namespace Dovus.App.Boss
{
    public static class BossDirectorRules
    {
        public static bool IsEnraged(float hp, float maxHp) =>
            maxHp > 0f && (hp / maxHp) <= 0.5f;

        public static float CurrentPhaseSpeed(StatusBoard board, BossAttackMotion motion)
        {
            if (board == null)
                return 1f;
            return BossAttackControl.Evaluate(board, motion).PhaseSpeed;
        }

        public static bool AttackKindAllowed(StatusBoard board, BossAttackKind kind)
        {
            if (board == null)
                return true;
            return BossAttackControl.Gate(
                board,
                BossAttackControl.MotionOf(kind),
                kind,
                staggered: false).CanStart;
        }

        public static bool ShouldSnapBossHomeToOrigin(
            float homeX,
            float homeZ,
            float spawnX,
            float spawnZ,
            float safeRadiusM)
        {
            float dx = homeX - spawnX;
            float dz = homeZ - spawnZ;
            return dx * dx + dz * dz < safeRadiusM * safeRadiusM;
        }

        public static float PounceAirSec(float configuredAirSec) =>
            configuredAirSec < BossDefaults.MinAirborneSec ? BossDefaults.MinAirborneSec : configuredAirSec;
    }
}
