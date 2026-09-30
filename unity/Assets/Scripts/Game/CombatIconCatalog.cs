using System;
using System.Collections.Generic;
using Dovus.Core.Equipment;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// v6 çift yüzlü rün kimliği → repo içi, telefonda okunur dairesel ikon.
    /// Element ikonlarına düşmez; eksik asset'te çağıran iki harfli glifi kullanır.
    /// </summary>
    public static class RuneIconCatalog
    {
        static readonly string[] Paths =
        {
            null,
            "UI/Runes/rune-01-saldiri",
            "UI/Runes/rune-02-iyilestirme",
            "UI/Runes/rune-03-hareket",
            "UI/Runes/rune-04-savunma",
            "UI/Runes/rune-05-patlama",
            "UI/Runes/rune-06-kontrol",
            "UI/Runes/rune-07-zayiflatma",
            "UI/Runes/rune-08-guclendirme",
            "UI/Runes/rune-09-arindirma",
            "UI/Runes/rune-10-yansima",
            "UI/Runes/rune-11-cagirma",
            "UI/Runes/rune-12-zaman",
        };

        static readonly Sprite[] Cache = new Sprite[Paths.Length];
        static readonly bool[] Loaded = new bool[Paths.Length];

        public static Sprite Get(int runeId)
        {
            if (runeId <= 0 || runeId >= Paths.Length)
                return null;
            if (Loaded[runeId])
                return Cache[runeId];
            Loaded[runeId] = true;
            Cache[runeId] = CombatIconLoader.Load(Paths[runeId]);
            return Cache[runeId];
        }

        public static string AssetPath(int runeId) =>
            runeId > 0 && runeId < Paths.Length ? "Assets/Resources/" + Paths[runeId] + ".png" : string.Empty;
    }

    /// <summary>v6 silah kimliği → savaş swap ve build/loadout ikonu.</summary>
    public static class WeaponIconCatalog
    {
        static readonly string[] Paths =
        {
            null,
            "UI/Weapons/weapon-01-yumruk",
            "UI/Weapons/weapon-02-hancer",
            "UI/Weapons/weapon-03-mizrak",
            "UI/Weapons/weapon-04-kilic",
            "UI/Weapons/weapon-05-balta",
            "UI/Weapons/weapon-06-cekic",
            "UI/Weapons/weapon-07-top",
            "UI/Weapons/weapon-08-asa",
            "UI/Weapons/weapon-09-tilsim",
            "UI/Weapons/weapon-10-kalkan",
        };

        static readonly Dictionary<int, Sprite> Cache = new();
        static readonly HashSet<int> Loaded = new();

        public static Sprite Get(EquipmentItem weapon) =>
            weapon == null ? null : Get(ParseId(weapon.Id, weapon.Name));

        public static Sprite Get(int weaponId)
        {
            if (weaponId <= 0 || weaponId >= Paths.Length)
                return null;
            if (Loaded.Contains(weaponId))
                return Cache.TryGetValue(weaponId, out Sprite cached) ? cached : null;
            Loaded.Add(weaponId);
            Sprite sprite = CombatIconLoader.Load(Paths[weaponId]);
            Cache[weaponId] = sprite;
            return sprite;
        }

        public static int ParseId(string id, string displayName = null)
        {
            if (!string.IsNullOrEmpty(id))
            {
                int colon = id.LastIndexOf(':');
                string suffix = colon >= 0 ? id.Substring(colon + 1) : id;
                if (int.TryParse(suffix, out int parsed) && parsed >= 1 && parsed <= 10)
                    return parsed;
            }

            string folded = Fold(displayName);
            return folded switch
            {
                "yumruk" => 1,
                "yay" => 2,
                "hancer" => 2,
                "buyu kitabi" => 3,
                "kitap" => 3,
                "mizrak" => 3,
                "kilic" => 4,
                "kure" => 5,
                "balta" => 5,
                "cekic" => 6,
                "top" => 7,
                "asa" => 8,
                "tilsim" => 9,
                "kalkan" => 10,
                _ => 0
            };
        }

        public static string AssetPath(int weaponId) =>
            weaponId > 0 && weaponId < Paths.Length ? "Assets/Resources/" + Paths[weaponId] + ".png" : string.Empty;

        static string Fold(string value) => (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace('ı', 'i')
            .Replace('ş', 's')
            .Replace('ç', 'c')
            .Replace('ğ', 'g')
            .Replace('ü', 'u')
            .Replace('ö', 'o');
    }

    static class CombatIconLoader
    {
        public static Sprite Load(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return null;
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
                return null;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
        }
    }
}
