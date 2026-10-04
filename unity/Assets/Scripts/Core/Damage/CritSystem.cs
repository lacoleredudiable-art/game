using System;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Damage
{
    /// <summary>
    /// O7: element-sistemi.json "crit_system" (base_crit_chance, crit_multiplier, max_crit_chance) ve
    /// formulas.crit ("random() &lt; crit_base + bonus → hasar × 2.0"). Bonus silah/sıfat/rün eklemeleri.
    /// </summary>
    public readonly struct CritSystem
    {
        public const float DefaultBaseChance = 0.05f;
        public const float DefaultMultiplier = 2f;
        public const float DefaultMaxChance = 0.75f;

        public CritSystem(float baseChance, float multiplier, float maxChance)
        {
            BaseChance = Math.Max(0f, baseChance);
            Multiplier = multiplier > 0f ? multiplier : DefaultMultiplier;
            MaxChance = Math.Min(1f, Math.Max(0f, maxChance));
        }

        public float BaseChance { get; }
        public float Multiplier { get; }
        public float MaxChance { get; }

        public static CritSystem Default => new CritSystem(DefaultBaseChance, DefaultMultiplier, DefaultMaxChance);

        /// <summary>Taban + bonus, [0, tavan] aralığına kırpılmış.</summary>
        public float ChanceWith(float bonus)
        {
            float c = BaseChance + Math.Max(0f, bonus);
            return Math.Min(MaxChance, Math.Max(0f, c));
        }

        public static CritSystem FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Default;
            return FromDocument(ElementSystemDocument.Parse(json));
        }

        public static CritSystem FromDocument(ElementSystemDocument doc) =>
            FromJson(doc.Root);

        public static CritSystem FromJson(JsonValue root)
        {
            if (root == null || root.IsNull)
                return Default;
            JsonValue c = root["crit_system"];
            if (c == null || c.IsNull)
                return Default;
            return new CritSystem(
                c["base_crit_chance"].AsFloat(DefaultBaseChance),
                c["crit_multiplier"].AsFloat(DefaultMultiplier),
                c["max_crit_chance"].AsFloat(DefaultMaxChance));
        }
    }
}
