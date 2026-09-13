using System;

namespace Dovus.Core.Elements
{
    public enum ElementResolveKind : byte
    {
        IdentitySkill = 2,
        VerbPlusCoreAdjective = 3,
        VerbPlusCompoundAdjective = 4
    }

    public enum CastMobility : byte
    {
        FreeMove = 0,
        SlowedMove = 1,
        Rooted = 2
    }

    public readonly struct VerbStats
    {
        public VerbStats(
            string id, string name, string family, string action,
            bool dealsBaseDamage, float baseDamage, float basePoise,
            string hitbox, CastMobility castMobility)
        {
            Id = id;
            Name = name;
            Family = family;
            Action = action;
            DealsBaseDamage = dealsBaseDamage;
            BaseDamage = baseDamage;
            BasePoise = basePoise;
            Hitbox = hitbox;
            CastMobility = castMobility;
        }

        public string Id { get; }
        public string Name { get; }
        public string Family { get; }
        public string Action { get; }
        public bool DealsBaseDamage { get; }
        public float BaseDamage { get; }
        public float BasePoise { get; }
        public string Hitbox { get; }
        public CastMobility CastMobility { get; }
        public bool IsMotionFamily =>
            string.Equals(Family, "motion", StringComparison.Ordinal);
    }

    public readonly struct AdjectiveMods
    {
        public AdjectiveMods(
            string id, string name, string category, string silhouetteAxis,
            float damageMult, float poiseDamageMult, float hitboxScaleMult)
        {
            Id = id;
            Name = name;
            Category = category;
            SilhouetteAxis = silhouetteAxis;
            DamageMult = damageMult;
            PoiseDamageMult = poiseDamageMult;
            HitboxScaleMult = hitboxScaleMult;
        }

        public string Id { get; }
        public string Name { get; }
        public string Category { get; }
        public string SilhouetteAxis { get; }
        public float DamageMult { get; }
        public float PoiseDamageMult { get; }
        public float HitboxScaleMult { get; }
    }

    public readonly struct ElementNode
    {
        public ElementNode(
            string id, string name, bool isCore, string group,
            string verbId, string adjectiveId,
            string? skillId, string? skillName, string? skillJob, string? verbUnlocksJob)
        {
            Id = id;
            Name = name;
            IsCore = isCore;
            Group = group;
            VerbId = verbId;
            AdjectiveId = adjectiveId;
            SkillId = skillId;
            SkillName = skillName;
            SkillJob = skillJob;
            VerbUnlocksJob = verbUnlocksJob;
        }

        public string Id { get; }
        public string Name { get; }
        public bool IsCore { get; }
        public string Group { get; }
        public string VerbId { get; }
        public string AdjectiveId { get; }
        public string? SkillId { get; }
        public string? SkillName { get; }
        public string? SkillJob { get; }
        public string? VerbUnlocksJob { get; }
    }

    public readonly struct LengthEconomy
    {
        public LengthEconomy(
            int length, string role,
            float castTimeMult, float resourceCostMult,
            float damageMult, float poiseDamageMult, CastMobility mobility)
        {
            Length = length;
            Role = role;
            CastTimeMult = castTimeMult;
            ResourceCostMult = resourceCostMult;
            DamageMult = damageMult;
            PoiseDamageMult = poiseDamageMult;
            Mobility = mobility;
        }

        public int Length { get; }
        public string Role { get; }
        public float CastTimeMult { get; }
        public float ResourceCostMult { get; }
        public float DamageMult { get; }
        public float PoiseDamageMult { get; }
        public CastMobility Mobility { get; }
    }

    public readonly struct ElementResolveResult
    {
        public ElementResolveResult(
            ElementResolveKind kind, int[] runes, string compoundId,
            string? skillId, string? skillName, string? skillJob, string? verbUnlocksJob,
            VerbStats verb, AdjectiveMods? adjective,
            float finalDamage, float finalPoise, CastMobility finalMobility,
            float castTimeMult, float resourceCostMult, string role)
        {
            Kind = kind;
            Runes = runes;
            CompoundId = compoundId;
            SkillId = skillId;
            SkillName = skillName;
            SkillJob = skillJob;
            VerbUnlocksJob = verbUnlocksJob;
            Verb = verb;
            Adjective = adjective;
            FinalDamage = finalDamage;
            FinalPoise = finalPoise;
            FinalMobility = finalMobility;
            CastTimeMult = castTimeMult;
            ResourceCostMult = resourceCostMult;
            Role = role;
        }

        public ElementResolveKind Kind { get; }
        public int[] Runes { get; }
        public string CompoundId { get; }
        public string? SkillId { get; }
        public string? SkillName { get; }
        public string? SkillJob { get; }
        public string? VerbUnlocksJob { get; }
        public VerbStats Verb { get; }
        public AdjectiveMods? Adjective { get; }
        public float FinalDamage { get; }
        public float FinalPoise { get; }
        public CastMobility FinalMobility { get; }
        public float CastTimeMult { get; }
        public float ResourceCostMult { get; }
        public string Role { get; }
    }
}
