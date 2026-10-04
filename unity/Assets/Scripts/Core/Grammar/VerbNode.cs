using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Element;
using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    public readonly struct VerbNode
    {
        public VerbNode(
            string id, string name, string family, string action,
            float baseDamage, float basePoise, string hitbox, string castMobility, string[] mechanics,
            string animationType, string targetMode, float baseCooldownSec, float baseResourceCost,
            IReadOnlyDictionary<string, string> targetBehaviors, JsonValue special, JsonValue zoneEffect,
            JsonValue raw,
            bool critEligible = false, string elementOrigin = "", string damageType = "")
        {
            Id = id; Name = name; Family = family; Action = action;
            BaseDamage = baseDamage; BasePoise = basePoise; Hitbox = hitbox;
            CastMobility = castMobility; Mechanics = mechanics;
            AnimationType = animationType; TargetMode = targetMode;
            BaseCooldownSec = baseCooldownSec; BaseResourceCost = baseResourceCost;
            TargetBehaviors = targetBehaviors; Special = special; ZoneEffect = zoneEffect;
            Raw = raw;
            CritEligible = critEligible;
            ElementOrigin = elementOrigin ?? string.Empty;
            DamageType = damageType ?? string.Empty;
        }

        public string Id { get; }
        public string Name { get; }
        public string Family { get; }
        public string Action { get; }
        public float BaseDamage { get; }
        public float BasePoise { get; }
        public string Hitbox { get; }
        public string CastMobility { get; }
        public string[] Mechanics { get; }
        public string AnimationType { get; }
        public string TargetMode { get; }
        public float BaseCooldownSec { get; }
        /// <summary>docs/element-sistemi.json "base_resource_cost" — bazı fiillerde henüz yok (bkz. docs/durum.md).</summary>
        public float BaseResourceCost { get; }
        /// <summary>selective fiiller için self/enemy/ally davranış metni.</summary>
        public IReadOnlyDictionary<string, string> TargetBehaviors { get; }
        /// <summary>Fiile özel serbest-form veri (heal_value, shield_amount, vb.). Ham JSON, tip yok.</summary>
        public JsonValue Special { get; }
        public JsonValue ZoneEffect { get; }
        public JsonValue Raw { get; }
        /// <summary>docs/element-sistemi.json verbs[].crit_eligible</summary>
        public bool CritEligible { get; }
        /// <summary>docs/element-sistemi.json verbs[].element_origin</summary>
        public string ElementOrigin { get; }
        /// <summary>docs/element-sistemi.json verbs[].engine_base_stats.damage_type</summary>
        public string DamageType { get; }
    }
}
