using System;
using Dovus.Core.Grammar;

namespace Dovus.Core.Combat
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

        public float HpFor(bool hard) => hard ? BossMaxHpHard : BossMaxHp;
        public float ArmorFor(bool hard) => hard ? ArmorHard : Armor;

        public static BossCombatProfile FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON boş.", nameof(json));
            JsonValue root = MiniJson.Parse(json);
            JsonValue player = root["global_rules"]["player_stats"];
            JsonValue boss = root["global_rules"]["boss_stats_default"];
            float playerHp = player["max_hp"].AsFloat(100f);
            float bossHp = boss["max_hp"].AsFloat(22000f);
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
