using System;

namespace Dovus.Core.RuleEngineV4
{
    public sealed class RuleEngineV4Scale
    {
        public float PlayerMaxHp { get; init; }
        public float AllyDummyMaxHp { get; init; }
        public float LightCreatureMaxHp { get; init; }
        public float BossMaxHp { get; init; }
        public float BaseDamage { get; init; }
        public float BaseHeal { get; init; }
        public float BasePoise { get; init; }
        public float BossPoiseBar { get; init; }
        // Mana/bekleme v4 döküm yolunda RuleEngineV4CastEconomy uygular.
        public float ManaPool { get; init; }
        public float ManaRegenPerSec { get; init; }
        public float ManaCostPerSkill { get; init; }
        public float CooldownSec { get; init; }
        /// <summary>Boşa giden skillde uygulanan bekleme oranı.</summary>
        public float FizzleCooldownFrac { get; init; }
        public float RangeReferenceM { get; init; }
    }
}
