using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    static partial class SliceGrammarFromV4
    {
        static string SkillEffectText(string verbKey, string adjKey, float kontrolSec)
        {
            if (verbKey == "kontrol")
            {
                if (adjKey == "yogun")
                    return kontrolSec.ToString("0.0", CultureInfo.InvariantCulture) + " sn stun";
                return kontrolSec.ToString("0.0", CultureInfo.InvariantCulture) + " sn root";
            }

            if (verbKey == "sifa")
                return "Şifa";
            if (verbKey == "zarar")
                return "Hasar";
            if (verbKey == "hareket")
                return "Dash";
            if (verbKey == "koruma")
                return "Koruma";
            if (verbKey == "arindirma")
                return "Arındırma";
            return verbKey;
        }

        static JsonValue CcRow(int rank, string cc, float durationSec) =>
            JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["rank"] = JsonValue.Number(rank),
                ["cc"] = JsonValue.Of(cc),
                ["duration_sec"] = JsonValue.Number(durationSec),
            });

        readonly struct VerbMeta
        {
            public string DisplayName { get; }
            public string Noun { get; }
            public string Family { get; }
            public string Category { get; }
            public string TargetMode { get; }
            public string BaseEffect { get; }
            public string Action { get; }
            public string Hitbox { get; }
            public string HitboxShape { get; }
            public string HitboxSizeLabel { get; }
            public float PassiveDurationSec { get; }

            VerbMeta(
                string displayName, string noun, string family, string category, string targetMode,
                string baseEffect, string action, string hitbox, string hitboxShape, string hitboxSizeLabel,
                float passiveDurationSec)
            {
                DisplayName = displayName;
                Noun = noun;
                Family = family;
                Category = category;
                TargetMode = targetMode;
                BaseEffect = baseEffect;
                Action = action;
                Hitbox = hitbox;
                HitboxShape = hitboxShape;
                HitboxSizeLabel = hitboxSizeLabel;
                PassiveDurationSec = passiveDurationSec;
            }

            public static VerbMeta For(string key) => key switch
            {
                "zarar" => new VerbMeta("Zarar", "Vuruş", "strike", "hasar", "enemy_only",
                    "Hasar fiili", "damage", "projectile", "capsule", "1.5m × 0.5m", 5f),
                "sifa" => new VerbMeta("Şifa", "Şifa", "mend", "destek", "self_or_ally",
                    "Şifa fiili", "heal", "self_aura", "sphere", "2m yarıçap", 7f),
                "hareket" => new VerbMeta("Hareket", "Adım", "motion", "hareket", "self_only",
                    "Hareket fiili", "dash", "dash_line", "line", "3m × 0.8m", 5f),
                "koruma" => new VerbMeta("Koruma", "Kalkan", "guard", "savunma", "self_only",
                    "Koruma fiili", "shield", "self", "sphere", "1.5m yarıçap", 7f),
                "kontrol" => new VerbMeta("Kontrol", "Bağ", "control", "savunma", "enemy_only",
                    "Kontrol fiili", "cc", "target", "cone", "4m × 60°", 4f),
                "arindirma" => new VerbMeta("Arındırma", "Arınma", "purge", "destek", "self_or_ally",
                    "Arındırma fiili", "cleanse", "self_or_ally", "sphere", "2.5m yarıçap", 5f),
                _ => new VerbMeta(key, key, "strike", "hasar", "enemy_only",
                    key, "damage", "projectile", "capsule", "1m", 5f),
            };
        }

        readonly struct AdjMeta
        {
            public string DisplayName { get; }
            public string Prefix { get; }

            public AdjMeta(string displayName, string prefix)
            {
                DisplayName = displayName;
                Prefix = prefix;
            }

            public static AdjMeta For(string key) => key switch
            {
                "yogun" => new AdjMeta("Yoğun", "Yoğun"),
                "odakli" => new AdjMeta("Odaklı", "Odaklı"),
                "sicrayan" => new AdjMeta("Sıçrayan", "Sıçrayan"),
                "sabit" => new AdjMeta("Sabit", "Sabit"),
                "gudumlu" => new AdjMeta("Güdümlü", "Güdümlü"),
                "isaretli" => new AdjMeta("İşaretli", "İşaretli"),
                _ => new AdjMeta(key, key),
            };
        }
    }
}
