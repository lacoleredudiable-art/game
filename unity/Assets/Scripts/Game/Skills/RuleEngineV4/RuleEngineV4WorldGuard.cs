using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Skills;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Koruma (DurusAc): yalnız oyuncu, dost ile saldırgan arasında araya girme.</summary>
    public static class RuleEngineV4WorldGuard
    {
        public static bool ApplyBossDamageToAlly(
            ManifestationDirector director,
            Transform ally,
            float raw,
            System.Func<float, bool> applyToAlly)
        {
            if (!RuleEngineV4Feature.Enabled || director == null || ally == null || raw <= 0f)
                return applyToAlly(raw);

            RuleEngineV4WorldSession session = director.RuleEngineV4Session;
            Transform player = director.MechanicsPlayer;
            double ms = director.MechanicsClock != null ? director.MechanicsClock.Director.WorldTimeMs : 0;
            if (session == null || player == null || !session.GuardActive(ms, player))
                return applyToAlly(raw);

            Transform boss = director.MechanicsBoss != null ? director.MechanicsBoss.transform : null;
            if (boss == null || !IsIntercepting(player, boss.position, ally.position))
                return applyToAlly(raw);

            float block = session.GuardBlockRatio;
            float toAlly = raw * (1f - block);
            float toPlayer = raw * block;
            if (toAlly > 0f)
                applyToAlly(toAlly);
            if (toPlayer > 0f)
                director.CachedPlayerVitals()?.ApplyDamage(Mathf.RoundToInt(toPlayer));
            return true;
        }

        static bool IsIntercepting(Transform guard, Vector3 from, Vector3 to)
        {
            Vector3 g = guard.position;
            g.y = 0f;
            from.y = 0f;
            to.y = 0f;
            Vector3 seg = to - from;
            float len = seg.magnitude;
            if (len < 0.01f)
                return false;
            Vector3 dir = seg / len;
            float t = Vector3.Dot(g - from, dir);
            if (t < 0f || t > len)
                return false;
            Vector3 closest = from + dir * t;
            float lateral = Vector3.Distance(closest, g);
            return lateral <= RuleEngineV4WorldPhysicsDefaults.ProtectionInterceptWidthM;
        }
    }
}
