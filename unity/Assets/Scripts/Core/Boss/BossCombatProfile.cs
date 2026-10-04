using System;
using Dovus.Core.Data;
using Dovus.Core.Grammar;
using Dovus.Core.Damage;

using Dovus.Core.Shared;
namespace Dovus.Core.Boss
{
    /// <summary>
    /// Oyuncu / boss canı ve boss zırhı JSON'dan. Can ölçeklenir, zırh puan olarak kalır.
    /// Sert mod anahtarı yoksa normal değer kullanılır (150-250M hedefi sonra max_hp ayarı).
    /// </summary>
    public readonly struct BossCombatProfile
    {
        public BossCombatProfile(
            float playerMaxHp,
            float bossMaxHp,
            float bossMaxHpHard,
            float armor,
            float armorHard)
        {
            PlayerMaxHp = playerMaxHp;
            BossMaxHp = bossMaxHp;
            BossMaxHpHard = bossMaxHpHard;
            Armor = armor;
            ArmorHard = armorHard;
        }

        public float PlayerMaxHp { get; }
        public float BossMaxHp { get; }
        public float BossMaxHpHard { get; }
        public float Armor { get; }
        public float ArmorHard { get; }

        public static BossCombatProfile FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON boş.", nameof(json));
            return FromDocument(ElementSystemDocument.Parse(json));
        }

        public static BossCombatProfile FromDocument(ElementSystemDocument doc) =>
            FromJsonRoot(doc.Root);

        public static BossCombatProfile FromJsonRoot(JsonValue root)
        {
            JsonValue player = root["global_rules"]["player_stats"];
            JsonValue boss = root["global_rules"]["boss_stats_default"];
            float playerHp = player["max_hp"].AsFloat(100f);
            float bossHp = boss["max_hp"].AsFloat(BossDefaults.Lit22000f);
            float bossHard = boss.Has("max_hp_hard") ? boss["max_hp_hard"].AsFloat(bossHp) : bossHp;
            float armor = boss["armor"].AsFloat(0f);
            float armorHard = boss.Has("armor_hard") ? boss["armor_hard"].AsFloat(armor) : armor;
            return new BossCombatProfile(
                CombatScale.Magnitude(playerHp),
                CombatScale.Magnitude(bossHp),
                CombatScale.Magnitude(bossHard),
                armor,
                armorHard);
        }
    }
}
