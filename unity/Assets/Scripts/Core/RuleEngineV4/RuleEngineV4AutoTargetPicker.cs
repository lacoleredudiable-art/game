using System;
using System.Collections.Generic;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>AutoProfileKey + Odaklı ihtiyaç seçimi (Core, sahne yok).</summary>
    public static class RuleEngineV4AutoTargetPicker
    {
        public static RuleEngineV4TargetPick Pick(
            in RuleEngineV4TargetResolution resolution,
            int adjectiveId,
            int verbId,
            float weaponRangeM,
            float friendlyRangeCapM,
            float markUseRangeM,
            bool manualTargetSelected,
            int? manualTargetId,
            IReadOnlyList<RuleEngineV4TargetCandidate> candidates)
        {
            float range = Math.Max(0f, weaponRangeM);
            if (resolution.Side == RuleEngineV4TargetSide.Friendly && adjectiveId != 9)
                range = Math.Min(range, Math.Max(0f, friendlyRangeCapM));

            if (adjectiveId == 2 || resolution.AutoProfileKey == "odakli_need")
                return PickOdakliNeed(verbId, resolution, range, candidates);

            if (resolution.AutoProfileKey == "isaretli_nearest_mark")
                return PickNearestMark(resolution, markUseRangeM, candidates);

            if (resolution.AutoProfileKey == "isaretli_arindirma_place")
                return PickNearestFriendly(range, candidates, allowSelf: true);

            if (resolution.Side == RuleEngineV4TargetSide.Self && !resolution.RequiresLivingTarget)
                return new RuleEngineV4TargetPick(true, true, 0, true, false);

            if (resolution.Side == RuleEngineV4TargetSide.Hostile
                || resolution.AutoProfileKey is "hostile_nearest" or "hostile_nearest_for_move" or "tetikli_nearest")
                return PickNearestHostile(range, candidates);

            if (resolution.Side == RuleEngineV4TargetSide.Friendly)
                return PickNearestFriendly(range, candidates, allowSelf: true);

            return PickNearestHostile(range, candidates);
        }

        public static RuleEngineV4CastContext ToCastContext(in RuleEngineV4TargetPick pick, int adjectiveId, bool hasUsableMark) =>
            new(
                hasValidTarget: pick.HasTarget,
                targetInWeaponRange: pick.TargetInWeaponRange,
                manualTargetSelected: pick.ManualTargetUsed,
                hasUsableMark: hasUsableMark);

        public static bool AnyUsableMark(IReadOnlyList<RuleEngineV4TargetCandidate> candidates, float markUseRangeM)
        {
            float r = Math.Max(0f, markUseRangeM);
            for (int i = 0; i < candidates.Count; i++)
            {
                RuleEngineV4TargetCandidate c = candidates[i];
                if (c.Available && c.HasTeamMark && c.DistanceM <= r)
                    return true;
            }
            return false;
        }

        static RuleEngineV4TargetPick PickOdakliNeed(
            int verbId,
            in RuleEngineV4TargetResolution resolution,
            float rangeM,
            IReadOnlyList<RuleEngineV4TargetCandidate> candidates)
        {
            RuleEngineV4TargetSide side = RuleEngineV4OdakliRules.PickSide(verbId, resolution.Side);
            if (side == RuleEngineV4TargetSide.Self)
                return new RuleEngineV4TargetPick(true, true, 0, true, false);

            bool found = false;
            RuleEngineV4TargetCandidate best = default;
            for (int i = 0; i < candidates.Count; i++)
            {
                RuleEngineV4TargetCandidate c = candidates[i];
                if (!c.Available || !MatchesSide(c, side))
                    continue;
                if (!found || RuleEngineV4OdakliRules.Beats(verbId, c, best))
                {
                    best = c;
                    found = true;
                }
            }

            if (!found)
                return new RuleEngineV4TargetPick(false, false, 0, false, false);

            bool inRange = best.DistanceM <= rangeM;
            return new RuleEngineV4TargetPick(true, false, best.Id, inRange, false);
        }

        static RuleEngineV4TargetPick PickNearestMark(
            in RuleEngineV4TargetResolution resolution,
            float markRangeM,
            IReadOnlyList<RuleEngineV4TargetCandidate> candidates)
        {
            float best = float.MaxValue;
            int id = 0;
            bool found = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                RuleEngineV4TargetCandidate c = candidates[i];
                if (!c.Available || !c.HasTeamMark || c.DistanceM > markRangeM)
                    continue;
                if (!MatchesSide(c, resolution.Side))
                    continue;
                if (!found || c.DistanceM < best)
                {
                    best = c.DistanceM;
                    id = c.Id;
                    found = true;
                }
            }

            if (!found)
                return new RuleEngineV4TargetPick(false, false, 0, false, false);
            return new RuleEngineV4TargetPick(true, false, id, true, false);
        }

        static RuleEngineV4TargetPick PickNearestHostile(
            float rangeM,
            IReadOnlyList<RuleEngineV4TargetCandidate> candidates)
        {
            float best = float.MaxValue;
            int id = 0;
            bool found = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                RuleEngineV4TargetCandidate c = candidates[i];
                if (!c.Available || !c.IsHostileToCaster)
                    continue;
                if (!found || c.DistanceM < best)
                {
                    best = c.DistanceM;
                    id = c.Id;
                    found = true;
                }
            }

            if (!found)
                return new RuleEngineV4TargetPick(false, false, 0, false, false);
            bool inRange = best <= rangeM;
            return new RuleEngineV4TargetPick(true, false, id, inRange, false);
        }

        static RuleEngineV4TargetPick PickNearestFriendly(
            float rangeM,
            IReadOnlyList<RuleEngineV4TargetCandidate> candidates,
            bool allowSelf)
        {
            float best = float.MaxValue;
            int id = 0;
            bool found = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                RuleEngineV4TargetCandidate c = candidates[i];
                if (!c.Available || c.IsHostileToCaster)
                    continue;
                if (!found || c.DistanceM < best)
                {
                    best = c.DistanceM;
                    id = c.Id;
                    found = true;
                }
            }

            if (!found)
                return allowSelf
                    ? new RuleEngineV4TargetPick(true, true, 0, true, false)
                    : new RuleEngineV4TargetPick(false, false, 0, false, false);

            bool inRange = best <= rangeM;
            return new RuleEngineV4TargetPick(true, false, id, inRange, false);
        }

        static bool MatchesSide(RuleEngineV4TargetCandidate c, RuleEngineV4TargetSide side) =>
            side switch
            {
                RuleEngineV4TargetSide.Hostile => c.IsHostileToCaster,
                RuleEngineV4TargetSide.Friendly => !c.IsHostileToCaster,
                _ => true,
            };
    }
}
