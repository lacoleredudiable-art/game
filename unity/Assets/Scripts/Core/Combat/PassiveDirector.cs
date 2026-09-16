using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// docs/element-sistemi.json "passives": trigger_combo eşleşince DurationSec kadar
    /// aktif kalır. ActiveModeDirector'dan farkı: cooldown yok; birden fazla pasif aynı anda
    /// aktif olabilir (tek "Active" yerine liste). Efektlerin dünyaya uygulanması Game
    /// katmanı işi — bu sınıf yalnızca durum makinesi.
    /// Kombo tablosu DEĞİL: her pasif JSON'dan gelir.
    /// </summary>
    public sealed class PassiveDirector
    {
        readonly List<PassiveNode> _passives;
        readonly List<ActivePassive> _active = new();

        public PassiveDirector(IReadOnlyList<PassiveNode> passives)
        {
            _passives = new List<PassiveNode>(passives ?? Array.Empty<PassiveNode>());
        }

        public IReadOnlyList<ActivePassive> Active => _active;

        public int ActiveCount => _active.Count;

        public bool IsActive(string passiveId)
        {
            if (string.IsNullOrEmpty(passiveId)) return false;
            for (int i = 0; i < _active.Count; i++)
            {
                if (string.Equals(_active[i].Node.Id, passiveId, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public float RemainingSec(string passiveId, double worldMs)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                ActivePassive a = _active[i];
                if (!string.Equals(a.Node.Id, passiveId, StringComparison.Ordinal))
                    continue;
                double elapsed = (worldMs - a.SinceMs) / 1000.0;
                return Math.Max(0f, a.Node.DurationSec - (float)elapsed);
            }
            return 0f;
        }

        /// <summary>
        /// Dot dizisi (1..6) bir pasifin trigger_combo'suyla birebir eşleşirse onu aktif eder.
        /// Aynı pasif zaten açıksa süreyi yeniler. Eşleşme yoksa null.
        /// </summary>
        public PassiveNode? TryTrigger(IReadOnlyList<int> dots, double worldMs)
        {
            if (dots == null || dots.Count == 0)
                return null;

            for (int i = 0; i < _passives.Count; i++)
            {
                PassiveNode p = _passives[i];
                if (!ComboEquals(p.TriggerCombo, dots))
                    continue;

                for (int j = 0; j < _active.Count; j++)
                {
                    if (!string.Equals(_active[j].Node.Id, p.Id, StringComparison.Ordinal))
                        continue;
                    _active[j] = new ActivePassive(p, worldMs);
                    return p;
                }

                _active.Add(new ActivePassive(p, worldMs));
                return p;
            }
            return null;
        }

        /// <summary>Süresi dolan pasifleri düşürür; en az biri düştüyse true.</summary>
        public bool Tick(double worldMs)
        {
            bool any = false;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActivePassive a = _active[i];
                if ((worldMs - a.SinceMs) / 1000.0 < a.Node.DurationSec)
                    continue;
                _active.RemoveAt(i);
                any = true;
            }
            return any;
        }

        // --- Aktif pasiflerin birleşik çarpanları / ekleri (aktif yoksa nötr) ---
        public float DamageMult => ProductEffect("damage_mult", 1f);
        public float DamageTakenMult => ProductEffect("damage_taken_mult", 1f);
        public float HealMult => ProductEffect("heal_mult", 1f);
        public float CritChanceAdd => SumEffect("crit_chance_add");
        public float LifestealAdd => SumEffect("lifesteal_add");
        public float ArmorAdd => SumEffect("armor_add");
        public float ReflectRatioAdd => SumEffect("reflect_ratio_add");
        public float DashCooldownMult => ProductEffect("dash_cooldown_mult", 1f);
        public float RevealRadiusMult => ProductEffect("reveal_radius_mult", 1f);

        float ProductEffect(string key, float identity)
        {
            float v = identity;
            for (int i = 0; i < _active.Count; i++)
                v *= _active[i].Node.GetEffect(key, identity);
            return v;
        }

        float SumEffect(string key)
        {
            float v = 0f;
            for (int i = 0; i < _active.Count; i++)
                v += _active[i].Node.GetEffect(key, 0f);
            return v;
        }

        static bool ComboEquals(int[] combo, IReadOnlyList<int> dots)
        {
            if (combo == null || combo.Length != dots.Count)
                return false;
            for (int i = 0; i < combo.Length; i++)
            {
                if (combo[i] != dots[i])
                    return false;
            }
            return true;
        }
    }

    public readonly struct ActivePassive
    {
        public ActivePassive(PassiveNode node, double sinceMs)
        {
            Node = node;
            SinceMs = sinceMs;
        }

        public PassiveNode Node { get; }
        public double SinceMs { get; }
    }
}
