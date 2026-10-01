using System.Collections.Generic;
using System.Text;
using Dovus.Core.Grammar;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Çizim geri bildirimi (denetim B ek): tanınan çizimde rün adları, tanınmayanda "tanınmadı".
    /// Saf kurallar (Unity'siz); HexagonInput/InkTrail/HexagonView yalnız gösterir.
    /// </summary>
    public static class DrawFeedback
    {
        public const string Unrecognized = "tanınmadı";
        public const string ClosedRune = "kapalı rün";

        /// <summary>Tanınan şekil izinin parlama ömrü çarpanı (InkLingerSec × bu).</summary>
        public const float FlashLifeScale = 1.6f;
        /// <summary>Ömrün bu kesrinde beyaz-altın parlama normal renge iner.</summary>
        public const float FlashFraction = 0.35f;
        /// <summary>Tanınmayan çizginin kırmızı sönme süresi (sn).</summary>
        public const float FailFadeSec = 0.45f;
        /// <summary>Çizgi bittiğinde (tanındı / sürüyor) ham izin hızlı sönme süresi (sn).</summary>
        public const float RawFadeSec = 0.18f;
        /// <summary>Etiketin ekranda kalma süresi (sn); son çeyrekte söner.</summary>
        public const float CaptionSec = 0.9f;

        public enum StrokeOutcome
        {
            None,
            Unrecognized
        }

        /// <summary>
        /// Çizim parmağı kalktı. Hiç nokta kabul edilmediyse ve başka bir red yazısı (mana, soğuma,
        /// kapalı rün, hedef) gösterilmediyse "tanınmadı". İptal (panel/kilit) sessizdir.
        /// </summary>
        public static StrokeOutcome OnStrokeEnd(bool wasDrawing, int acceptedDots, bool denialShown, bool cancelled)
        {
            if (!wasDrawing || cancelled || acceptedDots > 0 || denialShown)
                return StrokeOutcome.None;
            return StrokeOutcome.Unrecognized;
        }

        /// <summary>"Saldırı → Patlama" — cümlenin rün adları sırayla.</summary>
        public static string RuneChain(IReadOnlyList<SentenceWord> words)
        {
            if (words == null || words.Count == 0)
                return string.Empty;
            var sb = new StringBuilder(32);
            for (int i = 0; i < words.Count; i++)
            {
                if (i > 0)
                    sb.Append(" → ");
                sb.Append(RuneInfo.DisplayName(words[i].Rune));
            }
            return sb.ToString();
        }

        /// <summary>Parlama karışımı: 1 (tam beyaz-altın) → 0 (normal mürekkep), u = ömür oranı.</summary>
        public static float FlashMix(float u)
        {
            if (u <= 0f)
                return 1f;
            if (u >= FlashFraction)
                return 0f;
            return 1f - u / FlashFraction;
        }

        /// <summary>Etiket alfası: ilk ¾ tam, son ¼ söner.</summary>
        public static float CaptionAlpha(float elapsedSec)
        {
            if (elapsedSec < 0f || elapsedSec >= CaptionSec)
                return 0f;
            float fadeStart = CaptionSec * 0.75f;
            return elapsedSec <= fadeStart ? 1f : 1f - (elapsedSec - fadeStart) / (CaptionSec - fadeStart);
        }
    }
}
