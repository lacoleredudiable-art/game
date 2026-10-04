using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Portal;

namespace Dovus.App.Boss
{
    public static class BossStrikeResolver
    {
        public static void ComputeStrikeMetrics(
            float originX,
            float originZ,
            float targetX,
            float targetZ,
            float forwardX,
            float forwardZ,
            out float horizontalDist,
            out float signedAngleDeg)
        {
            float dx = targetX - originX;
            float dz = targetZ - originZ;
            horizontalDist = BossStrikeMath.HorizontalDistance(dx, dz);
            signedAngleDeg = 0f;
            if (horizontalDist > 0.01f)
                signedAngleDeg = BossStrikeMath.FlatSignedAngleDeg(forwardX, forwardZ, dx, dz);
        }

        public static bool PlayerVolumeHits(
            BossAttack attack,
            bool stealthed,
            float horizontalDist,
            float signedAngleDeg,
            float bossStrikeScale,
            bool blindMiss)
        {
            if (attack == null)
                return false;
            bool inVolume = attack.IsInEffectVolume(
                BossStrikeShape.DistanceForVolume(horizontalDist, bossStrikeScale),
                signedAngleDeg,
                attack.ArcHalfAngleDeg);
            if (!BossStatusMath.VolumeHits(stealthed, inVolume, attack.ArcHalfAngleDeg))
                return false;
            if (blindMiss)
                return false;
            return true;
        }

        public static bool AllyVolumeHits(
            BossAttack attack,
            bool stealthed,
            float horizontalDist,
            float signedAngleDeg,
            float bossStrikeScale,
            bool blindMiss)
        {
            if (attack == null)
                return false;
            bool inVolume = attack.IsInEffectVolume(
                BossStrikeShape.DistanceForVolume(horizontalDist, bossStrikeScale),
                signedAngleDeg,
                attack.ArcHalfAngleDeg);
            if (!BossStatusMath.VolumeHits(stealthed, inVolume, attack.ArcHalfAngleDeg))
                return false;
            if (blindMiss)
                return false;
            return true;
        }

        public static int? SanitizeDodgePress(int? pressMs, DodgeState dodge, int telegraphStartMs)
        {
            if (pressMs.HasValue && dodge != null && dodge.IframeEndMs(pressMs.Value) < telegraphStartMs)
                return null;
            return pressMs;
        }

        public static ExchangeInput BuildExchangeInput(
            int telegraphStartMs,
            int strikeTimeMs,
            int? dodgePressMs,
            bool inEffectVolume) =>
            new ExchangeInput
            {
                TelegraphStartMs = telegraphStartMs,
                StrikeTimeMs = strikeTimeMs,
                DodgePressMs = dodgePressMs,
                InEffectVolume = inEffectVolume
            };
    }
}
