using System;
using System.Collections.Generic;

namespace Dovus.Core.Trial
{
    /// <summary>O-17.3 sonuç kartı verisi: süre, atılan skill, en çok hasar veren kombo, deneme sayısı.</summary>
    public readonly struct TrialResult
    {
        public TrialResult(bool won, int attempt, double fightSec, int skillCount, string? topCombo, float topComboDamage, float bossHp01)
        {
            Won = won;
            Attempt = attempt;
            FightSec = fightSec;
            SkillCount = skillCount;
            TopCombo = topCombo;
            TopComboDamage = topComboDamage;
            BossHp01 = bossHp01;
        }

        public bool Won { get; }
        public int Attempt { get; }
        public double FightSec { get; }
        public int SkillCount { get; }
        public string? TopCombo { get; }
        public float TopComboDamage { get; }

        /// <summary>Denemenin bittiği andaki boss canı oranı (ölüm ekranında büyük yazılacak; 13 §5 [Ö]).</summary>
        public float BossHp01 { get; }
    }

    /// <summary>Tek denemenin sayaçları; her denemede sıfırlanır.</summary>
    public sealed class TrialStats
    {
        readonly Dictionary<string, float> _damageByCombo = new(StringComparer.Ordinal);

        public int SkillCount { get; private set; }

        public void NoteSkillCast() => SkillCount++;

        public void NoteDamage(string comboId, float damage)
        {
            if (string.IsNullOrEmpty(comboId) || damage <= 0f)
                return;
            _damageByCombo.TryGetValue(comboId, out float sum);
            _damageByCombo[comboId] = sum + damage;
        }

        public string? TopCombo(out float damage)
        {
            string? best = null;
            damage = 0f;
            foreach (KeyValuePair<string, float> kv in _damageByCombo)
            {
                // eşitlikte alfabetik küçük kazanır: sonuç sözlük sırasına bağlı kalmasın
                if (kv.Value > damage || (kv.Value == damage && best != null && string.CompareOrdinal(kv.Key, best) < 0))
                {
                    best = kv.Key;
                    damage = kv.Value;
                }
            }
            return best;
        }

        public void Reset()
        {
            SkillCount = 0;
            _damageByCombo.Clear();
        }
    }
}
