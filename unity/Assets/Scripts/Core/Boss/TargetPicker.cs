using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Boss
{
    /// <summary>Boss'un (ve sonra küçük canavarların) hedef alabileceği dost türleri.</summary>
    public enum TargetKind
    {
        Player,
        Ally,
        Decoy
    }

    /// <summary>Bir dost aday. Konum düzlem (x, z); seçim konuma bakmaz, co-op tehdidi için saklanır.</summary>
    public readonly struct HostileCandidate
    {
        public readonly int Id;
        public readonly TargetKind Kind;
        public readonly float X;
        public readonly float Z;
        public readonly bool Alive;
        public readonly bool Stealthed;
        public readonly bool Taunting;

        public HostileCandidate(int id, TargetKind kind, float x, float z, bool alive, bool stealthed, bool taunting)
        {
            Id = id;
            Kind = kind;
            X = x;
            Z = z;
            Alive = alive;
            Stealthed = stealthed;
            Taunting = taunting;
        }

        public bool Valid => Alive && !Stealthed;
    }

    /// <summary>
    /// Hedefleme tasarım sayıları — <c>Resources/Bosses/karadul.json</c> "targeting" bloğu.
    /// Varsayılanlar 2026-10-01 kullanıcı kararı (test boss'u): oyuncu %70 / dost %30,
    /// dost boss vuruşunun yarısını alır, ölürse 8 sn sonra yarım canla kalkar, yem her zaman önce.
    /// </summary>
    public sealed class TargetingConfig
    {
        public float AllyWeight = 0.3f;
        public float AllyDamageMult = 0.5f;
        public float AllyReviveSec = 8f;
        public float AllyReviveRatio = 0.5f;
        public bool DecoyPriority = true;

        /// <summary>karadul.json kökünden okur; blok ya da alan yoksa varsayılan kalır.</summary>
        public static TargetingConfig FromJson(JsonValue root)
        {
            var cfg = new TargetingConfig();
            if (root == null || !root.Has("targeting"))
                return cfg;
            JsonValue t = root["targeting"];
            cfg.AllyWeight = Clamp01(t["ally_weight"].AsFloat(cfg.AllyWeight));
            cfg.AllyDamageMult = Math.Max(0f, t["ally_damage_mult"].AsFloat(cfg.AllyDamageMult));
            cfg.AllyReviveSec = Math.Max(0f, t["ally_revive_sec"].AsFloat(cfg.AllyReviveSec));
            cfg.AllyReviveRatio = Clamp01(t["ally_revive_ratio"].AsFloat(cfg.AllyReviveRatio));
            cfg.DecoyPriority = t["decoy_priority"].AsBool(cfg.DecoyPriority);
            return cfg;
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }

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

            double r = Math.Max(0, Math.Min(0.999999999, roll01)) * total;
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

    /// <summary>Dost kuklanın boss hasarı ve yeniden kalkışı (saf; zaman parametre, dünya saati).</summary>
    public static class AllyLifeRules
    {
        /// <summary>Boss ham hasarı × ally_damage_mult (DamagePipeline'a girmeden önce).</summary>
        public static float AllyRawDamage(float raw, TargetingConfig cfg) =>
            raw <= 0f ? 0f : raw * Math.Max(0f, (cfg ?? new TargetingConfig()).AllyDamageMult);

        /// <summary>Düştüğü andan ally_revive_sec geçti mi. downAtMs &lt; 0: düşmedi.</summary>
        public static bool ReviveDue(double downAtMs, double nowMs, TargetingConfig cfg)
        {
            if (downAtMs < 0)
                return false;
            double sec = Math.Max(0f, (cfg ?? new TargetingConfig()).AllyReviveSec);
            return nowMs - downAtMs >= sec * 1000.0;
        }

        /// <summary>Kalkış canı: max × ally_revive_ratio, en az 1.</summary>
        public static int ReviveHp(int maxHp, TargetingConfig cfg)
        {
            int max = Math.Max(1, maxHp);
            float ratio = (cfg ?? new TargetingConfig()).AllyReviveRatio;
            int hp = (int)Math.Round(max * ratio, MidpointRounding.AwayFromZero);
            return Math.Max(1, Math.Min(max, hp));
        }
    }
}
