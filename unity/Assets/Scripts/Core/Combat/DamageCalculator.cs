using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// docs/element-sistemi.json formulas.damage + crit_system.
    /// ClosingDamageMath / ManifestationDirector yoluna BAĞLI DEĞİL — paralel sınıf;
    /// birleştirme ayrı karar (docs/durum.md).
    /// </summary>
    public sealed class DamageCalculator
    {
        readonly Random _rng;
        readonly float _baseCritChance;
        readonly float _critMultiplier;
        readonly float _maxCritChance;
        readonly Dictionary<string, float> _adjectiveCritBonus;

        public DamageCalculator(
            int seed,
            float baseCritChance,
            float critMultiplier,
            float maxCritChance,
            IReadOnlyDictionary<string, float> adjectiveCritBonus)
        {
            _rng = new Random(seed);
            _baseCritChance = baseCritChance;
            _critMultiplier = critMultiplier;
            _maxCritChance = maxCritChance;
            _adjectiveCritBonus = new Dictionary<string, float>(StringComparer.Ordinal);
            if (adjectiveCritBonus != null)
            {
                foreach (KeyValuePair<string, float> kv in adjectiveCritBonus)
                    _adjectiveCritBonus[kv.Key] = kv.Value;
            }
        }

        public float BaseCritChance => _baseCritChance;
        public float CritMultiplier => _critMultiplier;
        public float MaxCritChance => _maxCritChance;
        public IReadOnlyDictionary<string, float> AdjectiveCritBonus => _adjectiveCritBonus;

        /// <summary>crit_system + formulas köklerini MiniJson ile okur (sayı uydurma yok).</summary>
        public static DamageCalculator FromElementSystemJson(string json, int seed)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON boş.", nameof(json));

            JsonValue root = MiniJson.Parse(json);
            JsonValue crit = root["crit_system"];
            var bonuses = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, JsonValue> kv in crit["adjective_crit_bonus"].AsObject())
                bonuses[kv.Key] = kv.Value.AsFloat(0f);

            return new DamageCalculator(
                seed,
                crit["base_crit_chance"].AsFloat(0f),
                crit["crit_multiplier"].AsFloat(1f),
                crit["max_crit_chance"].AsFloat(1f),
                bonuses);
        }

        /// <summary>base_crit_chance + adjective_crit_bonus[id], max_crit_chance ile sınırlı.</summary>
        public float CritChanceFor(string adjectiveId)
        {
            float bonus = 0f;
            if (!string.IsNullOrEmpty(adjectiveId) &&
                _adjectiveCritBonus.TryGetValue(adjectiveId, out float b))
                bonus = b;
            return Math.Min(_baseCritChance + bonus, _maxCritChance);
        }

        /// <summary>
        /// formulas.damage: base × adj.damage_mult × length.damage_mult × (1-resistance) × weakness_bonus,
        /// ardından crit_system (CritEligible değilse crit yok).
        /// </summary>
        public DamageHit Compute(
            float baseDamageValue,
            float adjectiveDamageMult,
            float lengthDamageMult,
            float resistance,
            float weaknessBonus,
            string adjectiveId,
            bool critEligible,
            float extraCritChanceAdd = 0f)
        {
            float adj = adjectiveDamageMult > 0f ? adjectiveDamageMult : 1f;
            float len = lengthDamageMult > 0f ? lengthDamageMult : 1f;
            float weak = weaknessBonus > 0f ? weaknessBonus : 1f;
            float resistFactor = 1f - resistance;
            if (resistFactor < 0f)
                resistFactor = 0f;

            float amount = baseDamageValue * adj * len * resistFactor * weak;

            bool wasCrit = false;
            float chance = 0f;
            if (critEligible && amount > 0f)
            {
                chance = Math.Min(CritChanceFor(adjectiveId) + Math.Max(0f, extraCritChanceAdd), _maxCritChance);
                if (chance > 0f && _rng.NextDouble() < chance)
                {
                    amount *= _critMultiplier;
                    wasCrit = true;
                }
            }

            return new DamageHit(amount, wasCrit, chance);
        }

        /// <summary>
        /// ClosingDamageMath sonrası pasif/sıfat crit_chance_add — taban crit_system yok,
        /// yalnız ekstra şans (UseFormulaDamage=false yolu).
        /// </summary>
        public DamageHit ApplyExtraCrit(float amount, float extraCritChanceAdd)
        {
            if (amount <= 0f || extraCritChanceAdd <= 0f)
                return new DamageHit(amount, false, 0f);

            float chance = Math.Min(extraCritChanceAdd, _maxCritChance);
            if (chance > 0f && _rng.NextDouble() < chance)
                return new DamageHit(amount * _critMultiplier, true, chance);
            return new DamageHit(amount, false, chance);
        }

        /// <summary>
        /// SkillResolution alanlarından çözer. LengthDamageMult SkillResolution'da yok
        /// (SkillMotor yalnızca LengthCastMult taşır) — çağıran length.damage_mult verir.
        /// </summary>
        public DamageHit Compute(
            in SkillResolution skill,
            float lengthDamageMult,
            float resistance,
            float weaknessBonus,
            float extraCritChanceAdd = 0f)
        {
            if (skill.IsEmpty || skill.BaseDamage <= 0f)
                return new DamageHit(0f, false, 0f);

            return Compute(
                skill.BaseDamage,
                skill.DamageMult,
                lengthDamageMult,
                resistance,
                weaknessBonus,
                skill.AdjectiveId,
                skill.CritEligible,
                extraCritChanceAdd);
        }
    }

    public readonly struct DamageHit
    {
        public DamageHit(float amount, bool wasCrit, float critChanceUsed)
        {
            Amount = amount;
            WasCrit = wasCrit;
            CritChanceUsed = critChanceUsed;
        }

        public float Amount { get; }
        public bool WasCrit { get; }
        /// <summary>Bu vuruşta kullanılan (clamp'li) crit şansı; CritEligible false ise 0.</summary>
        public float CritChanceUsed { get; }
    }
}
