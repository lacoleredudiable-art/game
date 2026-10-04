using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Element;
using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    public readonly struct SkillResolution
    {
        public static SkillResolution Empty { get; } = default;

        public SkillResolution(
            string elementId, string elementName, string displayName, string skillId, string skillJob,
            string verbId, string verbName, string verbFamily, string action,
            float baseDamage, float basePoise, string hitbox, string castMobility, string[] mechanics,
            string adjectiveId, string adjectiveName, string silhouetteAxis,
            float damageMult, float hitboxScaleMult, float poiseDamageMult,
            int length, string lengthRole, float lengthCastMult, string lengthMobility,
            string flavorElement,
            string animationType = "", string targetMode = "", float baseCooldownSec = 0f,
            float baseResourceCost = 0f, IReadOnlyDictionary<string, string>? targetBehaviors = null,
            JsonValue? special = null, JsonValue? zoneEffect = null, JsonValue? engineModifiers = null,
            bool critEligible = false, string elementOrigin = "", string damageType = "",
            float lengthResourceCostMult = 1f, bool isComplete = true, float baseHeal = 0f,
            string passiveDescription = "", string proseFeel = "", string proseVisual = "")
        {
            ElementId = elementId ?? string.Empty;
            ElementName = elementName ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            SkillId = skillId ?? string.Empty;
            SkillJob = skillJob ?? string.Empty;
            VerbId = verbId ?? string.Empty;
            VerbName = verbName ?? string.Empty;
            VerbFamily = verbFamily ?? string.Empty;
            Action = action ?? string.Empty;
            BaseDamage = baseDamage;
            BasePoise = basePoise;
            Hitbox = hitbox ?? string.Empty;
            CastMobility = castMobility ?? string.Empty;
            Mechanics = mechanics ?? Array.Empty<string>();
            AdjectiveId = adjectiveId ?? string.Empty;
            AdjectiveName = adjectiveName ?? string.Empty;
            SilhouetteAxis = silhouetteAxis ?? string.Empty;
            DamageMult = damageMult;
            HitboxScaleMult = hitboxScaleMult;
            PoiseDamageMult = poiseDamageMult;
            Length = length;
            LengthRole = lengthRole ?? string.Empty;
            LengthCastMult = lengthCastMult;
            LengthMobility = lengthMobility ?? string.Empty;
            LengthResourceCostMult = lengthResourceCostMult > 0f ? lengthResourceCostMult : 1f;
            FlavorElement = flavorElement ?? string.Empty;
            AnimationType = animationType ?? string.Empty;
            TargetMode = targetMode ?? string.Empty;
            BaseCooldownSec = baseCooldownSec;
            BaseResourceCost = baseResourceCost;
            TargetBehaviors = targetBehaviors ?? EmptyBehaviors;
            Special = special ?? JsonValue.Null;
            ZoneEffect = zoneEffect ?? JsonValue.Null;
            EngineModifiers = engineModifiers ?? JsonValue.Null;
            CritEligible = critEligible;
            ElementOrigin = elementOrigin ?? string.Empty;
            DamageType = damageType ?? string.Empty;
            IsComplete = isComplete;
            BaseHeal = baseHeal;
            PassiveDescription = passiveDescription ?? string.Empty;
            ProseFeel = proseFeel ?? string.Empty;
            ProseVisual = proseVisual ?? string.Empty;
        }

        static readonly IReadOnlyDictionary<string, string> EmptyBehaviors =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public string ElementId { get; }
        public string ElementName { get; }
        public string DisplayName { get; }
        public string Name => DisplayName;
        public string SkillId { get; }
        public string SkillJob { get; }
        public string VerbId { get; }
        public string VerbName { get; }
        public string Verb => VerbName;
        public string VerbFamily { get; }
        public string Action { get; }
        public float BaseDamage { get; }
        public float BasePoise { get; }
        public string Hitbox { get; }
        public string CastMobility { get; }
        public string[] Mechanics { get; }
        public string AdjectiveId { get; }
        public string AdjectiveName { get; }
        public string Adjective => AdjectiveName;
        public string SilhouetteAxis { get; }
        public float DamageMult { get; }
        public float HitboxScaleMult { get; }
        public float PoiseDamageMult { get; }
        public int Length { get; }
        public string LengthRole { get; }
        public float LengthCastMult { get; }
        public string LengthMobility { get; }
        /// <summary>scaling_economy.lengths[N].resource_cost_mult (anti-ladder dışı maliyet).</summary>
        public float LengthResourceCostMult { get; }
        public string FlavorElement { get; }
        public bool IsEmpty => Length <= 0 || string.IsNullOrEmpty(DisplayName);

        // --- v4.2.2 / motor_parse_extension adım 1 ---
        public string AnimationType { get; }
        public string TargetMode { get; }
        public float BaseCooldownSec { get; }
        /// <summary>0 dönebilir: bazı fiillerde henüz tanımlı değil (bkz. docs/durum.md "Bilinen açıklar").</summary>
        public float BaseResourceCost { get; }
        public IReadOnlyDictionary<string, string> TargetBehaviors { get; }
        public JsonValue Special { get; }
        public JsonValue ZoneEffect { get; }
        public JsonValue EngineModifiers { get; }
        public SkillEngineModifiers Engine => new SkillEngineModifiers(EngineModifiers);
        public SkillId TypedSkillId => new SkillId(SkillId);
        public ElementId TypedElement => new ElementId(ElementId);
        public bool CritEligible { get; }
        public string ElementOrigin { get; }
        public string DamageType { get; }
        /// <summary>v6: false yalnız tek-rün fiil önizlemesinde; gerçek skill 2 ründür.</summary>
        public bool IsComplete { get; }
        public float BaseHeal { get; }
        public string PassiveDescription { get; }
        public string ProseFeel { get; }
        public string ProseVisual { get; }
    }
}
