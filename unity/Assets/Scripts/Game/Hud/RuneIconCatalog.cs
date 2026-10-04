using Dovus.Core.Equipment;
using System;
using System.Collections.Generic;
using Dovus.Game.Assets;
using UnityEngine;
namespace Dovus.Game.Hud
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
}
