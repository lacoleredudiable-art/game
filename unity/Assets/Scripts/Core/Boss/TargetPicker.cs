using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Boss
{
    /// <summary>
    /// Saf hedef seçimi (Core, Unity yok, rastgelelik parametre). Sıra:
    /// 1) canlı ve dikkat çeken yem (dikkat_ceker) kazanır — birden çoksa listede en sondaki (en yeni);
    /// 2) ölü / gizli adaylar atlanır;
    /// 3) oyuncu ağırlığı 1 − AllyWeight, dost ağırlığı AllyWeight ile ağırlıklı seçim;
    /// 4) geçerli aday yoksa −1 (boss bekler; bugünkü gizlilik davranışı).
    /// Co-op'ta adaylar N oyuncu + yemleri olur, ağırlık tehdit olur; API aynı kalır.
    /// </summary>
    public static class TargetPicker
    {
        public static int Pick(IReadOnlyList<HostileCandidate> candidates, TargetingConfig cfg, double roll01)
        {
            if (candidates == null || candidates.Count == 0)
                return -1;
            cfg ??= new TargetingConfig();

            if (cfg.DecoyPriority)
            {
                for (int i = candidates.Count - 1; i >= 0; i--)
                {
                    HostileCandidate c = candidates[i];
                    if (c.Kind == TargetKind.Decoy && c.Taunting && c.Valid)
                        return c.Id;
                }
            }

            double total = 0;
            for (int i = 0; i < candidates.Count; i++)
                total += WeightOf(candidates[i], cfg);
            if (total <= 0)
                return -1;

            double r = Math.Max(0, Math.Min(BossDefaults.RollClampMax, roll01)) * total;
            int lastValid = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                double w = WeightOf(candidates[i], cfg);
                if (w <= 0)
                    continue;
                lastValid = candidates[i].Id;
                if (r < w)
                    return candidates[i].Id;
                r -= w;
            }
            return lastValid;
        }

        /// <summary>
        /// Mevcut hedef bırakılmalı mı: aday listede yok, ölü/gizli, ya da dikkat çeken
        /// yeni bir yem var ve hedef o değil.
        /// </summary>
        public static bool ShouldRetarget(IReadOnlyList<HostileCandidate> candidates, int currentId, TargetingConfig cfg)
        {
            if (candidates == null || candidates.Count == 0)
                return currentId >= 0;
            cfg ??= new TargetingConfig();
            bool found = false;
            bool currentIsTauntingDecoy = false;
            bool anyTaunting = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                HostileCandidate c = candidates[i];
                bool taunting = cfg.DecoyPriority && c.Kind == TargetKind.Decoy && c.Taunting && c.Valid;
                anyTaunting |= taunting;
                if (c.Id != currentId)
                    continue;
                found = c.Valid;
                currentIsTauntingDecoy = taunting;
            }
            if (!found)
                return true;
            return anyTaunting && !currentIsTauntingDecoy;
        }

        static double WeightOf(HostileCandidate c, TargetingConfig cfg)
        {
            if (!c.Valid)
                return 0;
            return c.Kind switch
            {
                TargetKind.Player => Math.Max(0f, 1f - cfg.AllyWeight),
                TargetKind.Ally => Math.Max(0f, cfg.AllyWeight),
                _ => 0
            };
        }
    }
}
