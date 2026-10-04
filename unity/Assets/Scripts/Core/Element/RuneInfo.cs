using Dovus.Core.Element;
namespace Dovus.Core.Element
{
    public static class RuneInfo
    {
        public static string Syllable(Rune rune) => "r" + ((int)rune).ToString();

        /// <summary>element-sistemi.json v6.1.1 rün adları (Türkçe UI).</summary>
        public static string DisplayName(Rune rune) => rune switch
        {
            Rune.Attack => "Saldırı",
            Rune.Heal => "İyileştirme",
            Rune.Move => "Hareket",
            Rune.Defense => "Savunma",
            Rune.Burst => "Patlama",
            Rune.Control => "Kontrol",
            Rune.Weaken => "Zayıflatma",
            Rune.Empower => "Güçlendirme",
            Rune.Cleanse => "Arındırma",
            Rune.Reflect => "Yansıma",
            Rune.Summon => "Çağırma",
            Rune.Time => "Zaman",
            _ => "?"
        };

        /// <summary>
        /// Eski çift adlı <c>Rune</c> enum'unun Unity (Mono) <c>Enum.ToString()</c> çıktısı — renk anahtarı / GameObject adı
        /// uyumu için. Unity 6000.4 MonoBleedingEdge ile ölçüldü (1..6: Ates, Su, Hava, Savunma, Patlama, Karanlik);
        /// .NET (SweepV2/CoreTests) aynı enum'da v6 adlarını veriyordu — eski davranış platforma bağlıydı, artık sabit.
        /// </summary>
        public static string LegacySerializationName(Rune rune) => rune switch
        {
            Rune.Attack => "Ates",
            Rune.Heal => "Su",
            Rune.Move => "Hava",
            Rune.Defense => "Savunma",
            Rune.Burst => "Patlama",
            Rune.Control => "Karanlik",
            Rune.Weaken => "Zayiflatma",
            Rune.Empower => "Guclendirme",
            Rune.Cleanse => "Arindirma",
            Rune.Reflect => "Yansima",
            Rune.Summon => "Cagirma",
            Rune.Time => "Zaman",
            _ => "?"
        };

        public static bool TryFromId(int id, out Rune rune)
        {
            if (id is >= 1 and <= 12)
            {
                rune = (Rune)id;
                return true;
            }

            rune = default;
            return false;
        }

        /// <summary>Eski/test varsayılanı: slot 1..6 doğrudan ilk altı rüne gider.</summary>
        public static bool TryFromDot(int dot, out Rune rune)
        {
            return RuneLoadout.Sequential.TryResolveSlot(dot, out rune);
        }

        public static bool TryFromDot(int dot, RuneLoadout loadout, out Rune rune)
        {
            return (loadout ?? RuneLoadout.Sequential).TryResolveSlot(dot, out rune);
        }
    }
}
