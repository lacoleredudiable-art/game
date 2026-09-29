using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

namespace Dovus.Core.Combat
{
    public enum SkillAimMode
    {
        Targeted,
        Directional,
        GroundAimed
    }

    public enum TargetRelation
    {
        Self,
        Ally,
        Enemy
    }

    public enum TargetFailure
    {
        None,
        NoTarget,
        OutOfRange
    }

    /// <summary>
    /// Unity'den bağımsız hedef adayı. DistanceM, Game katmanında hedef collider'ının en yakın
    /// noktasına ölçülür; böylece büyük boss'lar merkezlerinden dolayı haksızca menzil dışı kalmaz.
    /// </summary>
    public readonly struct TargetCandidate
    {
        public TargetCandidate(int id, TargetRelation relation, float distanceM, bool available = true)
        {
            Id = id;
            Relation = relation;
            DistanceM = Math.Max(0f, distanceM);
            Available = available;
        }

        public int Id { get; }
        public TargetRelation Relation { get; }
        public float DistanceM { get; }
        public bool Available { get; }
    }

    public readonly struct TargetResolution
    {
        public TargetResolution(bool allowed, bool useSelf, int targetId, TargetFailure failure)
        {
            Allowed = allowed;
            UseSelf = useSelf;
            TargetId = targetId;
            Failure = failure;
        }

        public bool Allowed { get; }
        public bool UseSelf { get; }
        public int TargetId { get; }
        public TargetFailure Failure { get; }
    }

    /// <summary>
    /// v6.1.1 target_mode + opsiyonel engine.aim_mode hedefleme politikası.
    /// aim_mode yoksa güvenli varsayılan targeted; yalnız dash fiili yönlü kalır.
    /// </summary>
    public static class TargetingRules
    {
        public static bool IsHoming(SkillAimMode aimMode) =>
            aimMode == SkillAimMode.Targeted;

        public static SkillAimMode AimMode(in SkillResolution skill)
        {
            string explicitMode = skill.EngineModifiers["aim_mode"].AsString(
                skill.EngineModifiers["targeting_mode"].AsString());
            if (!string.IsNullOrEmpty(explicitMode))
            {
                if (explicitMode is "directional" or "skillshot")
                    return SkillAimMode.Directional;
                if (explicitMode is "ground" or "ground_aimed")
                    return SkillAimMode.GroundAimed;
                return SkillAimMode.Targeted;
            }

            return string.Equals(skill.Action, "dash", StringComparison.OrdinalIgnoreCase)
                ? SkillAimMode.Directional
                : SkillAimMode.Targeted;
        }

        public static TargetResolution Resolve(
            string targetMode,
            SkillAimMode aimMode,
            float rangeM,
            int? selectedId,
            IReadOnlyList<TargetCandidate> candidates)
        {
            if (aimMode is SkillAimMode.Directional or SkillAimMode.GroundAimed)
                return AllowedSelf();

            if (string.Equals(targetMode, "self_only", StringComparison.OrdinalIgnoreCase))
                return AllowedSelf();

            bool friendly = string.Equals(targetMode, "self_or_ally", StringComparison.OrdinalIgnoreCase);
            TargetRelation wanted = friendly ? TargetRelation.Ally : TargetRelation.Enemy;
            float range = Math.Max(0f, rangeM);

            if (selectedId.HasValue
                && TryFind(candidates, selectedId.Value, out TargetCandidate selected)
                && selected.Available
                && selected.Relation == wanted)
            {
                return selected.DistanceM <= range
                    ? AllowedTarget(selected.Id)
                    : new TargetResolution(false, false, selected.Id, TargetFailure.OutOfRange);
            }

            // Dost fiilleri otomatik başka dosta atlamaz: seçili geçerli dost yoksa self.
            if (friendly)
                return AllowedSelf();

            TargetCandidate best = default;
            bool found = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                TargetCandidate candidate = candidates[i];
                if (!candidate.Available || candidate.Relation != TargetRelation.Enemy
                    || candidate.DistanceM > range)
                    continue;
                if (!found || candidate.DistanceM < best.DistanceM)
                {
                    best = candidate;
                    found = true;
                }
            }

            return found
                ? AllowedTarget(best.Id)
                : new TargetResolution(false, false, 0, TargetFailure.NoTarget);
        }

        static bool TryFind(
            IReadOnlyList<TargetCandidate> candidates,
            int id,
            out TargetCandidate result)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Id != id)
                    continue;
                result = candidates[i];
                return true;
            }
            result = default;
            return false;
        }

        static TargetResolution AllowedSelf() =>
            new(true, true, 0, TargetFailure.None);

        static TargetResolution AllowedTarget(int id) =>
            new(true, false, id, TargetFailure.None);
    }
}
