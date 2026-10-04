using Dovus.Core.Equipment;
using System;
using System.Collections.Generic;
using Dovus.Game.Assets;
using UnityEngine;
namespace Dovus.Game.Hud
{
    public static class WeaponIconCatalog
        {
            static readonly string[] Paths =
            {
                null,
                "UI/Weapons/weapon-01-yumruk",
                "UI/Weapons/weapon-02-yay",
                "UI/Weapons/weapon-03-kitap",
                "UI/Weapons/weapon-04-kilic",
                "UI/Weapons/weapon-05-kure",
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
    }
