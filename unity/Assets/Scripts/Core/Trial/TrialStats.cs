using System;
using System.Collections.Generic;

namespace Dovus.Core.Trial
{
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
