using System;
using Dovus.Core;
using Dovus.Core.Grammar;
using Dovus.Core.Shared;

namespace Dovus.Core.Status
{
    public readonly struct TempoCast
    {
        public TempoCast(
            bool enemySlow,
            bool selfHaste,
            bool allyHaste,
            double durationMs,
            float slowStrength,
            float hasteStrength,
            string sourceId)
        {
            EnemySlow = enemySlow;
            SelfHaste = selfHaste;
            AllyHaste = allyHaste;
            DurationMs = durationMs;
            SlowStrength = slowStrength;
            HasteStrength = hasteStrength;
            SourceId = string.IsNullOrEmpty(sourceId) ? "tempo" : sourceId;
        }

        public bool EnemySlow { get; }
        public bool SelfHaste { get; }
        public bool AllyHaste { get; }
        public double DurationMs { get; }
        public float SlowStrength { get; }
        public float HasteStrength { get; }
        public string SourceId { get; }

        public static TempoCast From(in SkillResolution skill)
        {
            SkillEngineModifiers engine = skill.Engine;
            float durationSec = engine.ReadFloat("tempo_duration_sec");
            float enemySlow = engine.ReadFloat("enemy_slow");
            float selfHaste = engine.ReadFloat("self_haste");
            float damageBuff = engine.ReadFloat("self_damage_buff");
            string text = skill.Identity.SkillJob ?? string.Empty;
            bool haste = CardEffectRules.WantsSelfHaste(text);
            TempoSyncRules.Read(durationSec, enemySlow > 0f ? enemySlow : 0f, out double ms, out float slowStrength);
            float hasteMag = haste
                ? CardEffectRules.HasteMagnitude(text, selfHaste, enemySlow, damageBuff)
                : 1f;
            string skillId = string.IsNullOrEmpty(skill.Identity.Id) ? "zaman" : skill.Identity.Id;
            return new TempoCast(
                !haste && enemySlow > 0f,
                haste,
                haste && CardEffectRules.SharesHasteWithAlly(text),
                ms,
                slowStrength,
                hasteMag,
                "tempo:" + skillId);
        }

        public void Apply(StatusBoard? self, StatusBoard? enemy, StatusBoard? ally, float hasteMult = 1f)
        {
            float haste = hasteMult > 0f ? HasteStrength * hasteMult : HasteStrength;
            if (EnemySlow && enemy != null && DurationMs > 0 && SlowStrength > 0f && SlowStrength < 1f)
                enemy.Apply(StatusKind.Slow, DurationMs, SlowStrength, SourceId);
            if (SelfHaste && self != null && DurationMs > 0 && haste > 1f)
                self.Apply(StatusKind.Haste, DurationMs, haste, SourceId);
            if (AllyHaste && ally != null && DurationMs > 0 && haste > 1f)
                ally.Apply(StatusKind.Haste, DurationMs, haste, SourceId);
        }
    }
}
