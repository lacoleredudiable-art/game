using System;

namespace Dovus.Core.Grammar
{
    /// <summary>
    /// scaling_economy.mobility_resolution — length × verb.cast_mobility.
    /// motion ailesi length.mobility yok sayar.
    /// </summary>
    public static class SkillMobility
    {
        public const string FreeMove = "free_move";
        public const string SlowedMove = "slowed_move";
        public const string Rooted = "rooted";

        /// <summary>
        /// final_mobility = most_restrictive(length.mobility, verb.cast_mobility)
        /// except motion family → yalnızca verb.cast_mobility.
        /// </summary>
        public static string Resolve(in SkillResolution skill)
        {
            if (skill.IsEmpty)
                return FreeMove;

            string verbMob = Normalize(skill.CastMobility);
            if (string.Equals(skill.VerbFamily, "motion", StringComparison.Ordinal))
                return verbMob;

            string lenMob = Normalize(skill.LengthMobility);
            return MostRestrictive(lenMob, verbMob);
        }

        public static string MostRestrictive(string a, string b)
        {
            int ra = Rank(a);
            int rb = Rank(b);
            return ra >= rb ? Normalize(a) : Normalize(b);
        }

        public static int Rank(string mobility) => Normalize(mobility) switch
        {
            Rooted => 2,
            SlowedMove => 1,
            _ => 0
        };

        static string Normalize(string mobility)
        {
            if (string.IsNullOrEmpty(mobility))
                return FreeMove;
            return mobility switch
            {
                Rooted => Rooted,
                SlowedMove => SlowedMove,
                FreeMove => FreeMove,
                _ => FreeMove
            };
        }

        /// <summary>LengthCastMult × sıfat cast_time_mult (yoksa 1).</summary>
        public static float CastTimeMult(in SkillResolution skill)
        {
            float m = skill.LengthCastMult > 0f ? skill.LengthCastMult : 1f;
            if (!skill.EngineModifiers.IsNull && skill.EngineModifiers.Has("cast_time_mult"))
            {
                float adj = skill.EngineModifiers["cast_time_mult"].AsFloat(1f);
                if (adj > 0f)
                    m *= adj;
            }
            return m;
        }

        /// <summary>base_resource_cost × length.resource_cost_mult.</summary>
        public static float ResourceCost(in SkillResolution skill)
        {
            float baseCost = skill.BaseResourceCost;
            if (baseCost <= 0f)
                return 0f;
            float len = skill.LengthResourceCostMult > 0f ? skill.LengthResourceCostMult : 1f;
            return baseCost * len;
        }
    }
}
