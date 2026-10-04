using System;
using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Passives
{
    /// <summary>
    /// Sıçrama. Menzil global_rules.ally_skill_range_m ile aynı 6 m.
    /// Başka düşman yoksa ek vuruşlar kaynağa (boss) iner, adet bounce_targets.
    /// </summary>
    public static class PassiveBounce
    {
        public const float RangeM = 6f;

        public static List<PassiveBounceHit> Plan(
            float sourceDamage,
            int bounceCount,
            float damageMult,
            int sourceTargetId,
            IReadOnlyList<PassiveBounceCandidate> candidates)
        {
            var hits = new List<PassiveBounceHit>();
            if (bounceCount <= 0 || sourceDamage <= 0f || damageMult <= 0f)
                return hits;

            float damage = sourceDamage * damageMult;
            var nearest = new List<PassiveBounceCandidate>();
            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    PassiveBounceCandidate candidate = candidates[i];
                    if (candidate.Id == sourceTargetId)
                        continue;
                    if (candidate.DistanceM < 0f || candidate.DistanceM > RangeM)
                        continue;
                    nearest.Add(candidate);
                }
            }

            nearest.Sort((a, b) =>
            {
                int byDistance = a.DistanceM.CompareTo(b.DistanceM);
                return byDistance != 0 ? byDistance : a.Id.CompareTo(b.Id);
            });

            if (nearest.Count == 0)
            {
                for (int i = 0; i < bounceCount; i++)
                    hits.Add(new PassiveBounceHit(sourceTargetId, damage));
                return hits;
            }

            int count = bounceCount < nearest.Count ? bounceCount : nearest.Count;
            for (int i = 0; i < count; i++)
                hits.Add(new PassiveBounceHit(nearest[i].Id, damage));
            return hits;
        }
    }
}
