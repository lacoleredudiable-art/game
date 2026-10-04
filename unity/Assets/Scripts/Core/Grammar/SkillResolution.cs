using System;
using System.Collections.Generic;
using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    public readonly struct SkillResolution
    {
        public static SkillResolution Empty { get; } = default;

        internal static readonly IReadOnlyDictionary<string, string> EmptyBehaviors =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public SkillResolution(
            SkillResolutionIdentity identity,
            SkillResolutionPresentation presentation,
            SkillResolutionCombat combat,
            SkillResolutionScaling scaling,
            SkillResolutionLength length,
            SkillResolutionCosts costs,
            SkillResolutionTargeting targeting,
            string[] mechanics,
            SkillEngineModifiers engine,
            SkillResolutionEffects effects,
            SkillResolutionProse prose,
            bool isComplete)
        {
            Identity = identity;
            Presentation = presentation;
            Combat = combat;
            Scaling = scaling;
            Length = length;
            Costs = costs;
            Targeting = targeting;
            Mechanics = mechanics ?? Array.Empty<string>();
            Engine = engine;
            Effects = effects;
            Prose = prose;
            IsComplete = isComplete;
        }

        public SkillResolutionIdentity Identity { get; }
        public SkillResolutionPresentation Presentation { get; }
        public SkillResolutionCombat Combat { get; }
        public SkillResolutionScaling Scaling { get; }
        public SkillResolutionLength Length { get; }
        public SkillResolutionCosts Costs { get; }
        public SkillResolutionTargeting Targeting { get; }
        public string[] Mechanics { get; }
        public SkillEngineModifiers Engine { get; }
        public SkillResolutionEffects Effects { get; }
        public SkillResolutionProse Prose { get; }
        public bool IsComplete { get; }

        public bool IsEmpty => Length.RuneCount <= 0 || string.IsNullOrEmpty(Identity.DisplayName);

        public static SkillResolution Build(
            string elementId,
            string elementName,
            string displayName,
            SkillId skillId,
            string skillJob,
            string verbId,
            string verbName,
            string verbFamily,
            string action,
            float baseDamage,
            float basePoise,
            string hitbox,
            string castMobility,
            string[] mechanics,
            string adjectiveId,
            string adjectiveName,
            string silhouetteAxis,
            float damageMult,
            float hitboxScaleMult,
            float poiseDamageMult,
            int length,
            string lengthRole,
            float lengthCastMult,
            string lengthMobility,
            string flavorElement,
            string animationType = "",
            string targetMode = "",
            float baseCooldownSec = 0f,
            float baseResourceCost = 0f,
            IReadOnlyDictionary<string, string>? targetBehaviors = null,
            JsonValue? special = null,
            JsonValue? zoneEffect = null,
            JsonValue? engineModifiers = null,
            bool critEligible = false,
            string elementOrigin = "",
            string damageType = "",
            float lengthResourceCostMult = 1f,
            bool isComplete = true,
            float baseHeal = 0f,
            string passiveDescription = "",
            string proseFeel = "",
            string proseVisual = "")
        {
            var identity = new SkillResolutionIdentity(
                new ElementId(elementId),
                elementName,
                displayName,
                skillId,
                skillJob,
                new RuneId(verbId),
                verbName,
                new RuneId(adjectiveId),
                adjectiveName,
                flavorElement,
                elementOrigin);

            var presentation = new SkillResolutionPresentation(
                VerbFamilyWire.Parse(verbFamily),
                SkillActionWire.Parse(action),
                HitboxWire.Parse(hitbox),
                AnimationTypeWire.Parse(animationType),
                silhouetteAxis);

            var combat = new SkillResolutionCombat(baseDamage, basePoise, baseHeal, critEligible, damageType);
            var scaling = new SkillResolutionScaling(damageMult, hitboxScaleMult, poiseDamageMult);
            var lengthInfo = new SkillResolutionLength(
                length,
                LengthRoleWire.Parse(lengthRole),
                lengthCastMult,
                LengthMobilityWire.Parse(lengthMobility),
                lengthResourceCostMult);
            var costs = new SkillResolutionCosts(baseCooldownSec, baseResourceCost);
            var targeting = new SkillResolutionTargeting(
                TargetModeWire.Parse(targetMode),
                CastMobilityWire.Parse(castMobility),
                targetBehaviors);

            return new SkillResolution(
                identity,
                presentation,
                combat,
                scaling,
                lengthInfo,
                costs,
                targeting,
                mechanics,
                new SkillEngineModifiers(engineModifiers ?? JsonValue.Null),
                new SkillResolutionEffects(special ?? JsonValue.Null, zoneEffect ?? JsonValue.Null),
                new SkillResolutionProse(passiveDescription, proseFeel, proseVisual),
                isComplete);
        }
    }
}
