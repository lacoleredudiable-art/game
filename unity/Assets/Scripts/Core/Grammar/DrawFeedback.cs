using System.Collections.Generic;
using System.Text;
using Dovus.Core.Element;
using Dovus.Core.Grammar;

namespace Dovus.Core.Grammar
{
    /// <summary>
    /// Çizim geri bildirimi (denetim B ek): tanınan çizimde rün adları, tanınmayanda "şekil tanınmadı".
    /// Saf kurallar (Unity'siz); HexagonInput/InkTrail/HexagonView yalnız gösterir.
    /// </summary>
    public static class DrawFeedback
    {
        public const string Unrecognized = "şekil tanınmadı";
        public const string ClosedRune = "kapalı rün";
        public const string TooShort = "çok kısa";
        public const string OnCooldown = "cooldown'da";

        /// <summary>spec'te yok — varsayılan</summary>
        public const float TooShortDp = 24f;

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

        public enum DenialKind
        {
            None,
            Cooldown,
            Other
        }

        public enum StrokeOutcome
        {
            None,
            TooShort,
            Unrecognized,
            Cooldown
        }

        /// <summary>
        /// Çizim parmağı kalktı. Hiç nokta kabul edilmediyse ve başka bir red yazısı (mana, soğuma,
        /// kapalı rün, hedef) gösterilmediyse "şekil tanınmadı". İptal (panel/kilit) sessizdir.
        /// </summary>
        public static StrokeOutcome OnStrokeEnd(bool wasDrawing, int acceptedDots, bool denialShown, bool cancelled)
        {
            return OnStrokeEnd(wasDrawing, acceptedDots,
                denialShown ? DenialKind.Other : DenialKind.None,
                cancelled, float.MaxValue);
        }

        public static StrokeOutcome OnStrokeEnd(bool wasDrawing, int acceptedDots, DenialKind denial,
            bool cancelled, float strokeLengthDp)
        {
            if (!wasDrawing || cancelled || acceptedDots > 0)
                return StrokeOutcome.None;
            if (denial == DenialKind.Cooldown)
                return StrokeOutcome.Cooldown;
            if (denial == DenialKind.Other)
                return StrokeOutcome.None;
            if (strokeLengthDp < TooShortDp)
                return StrokeOutcome.TooShort;
            return StrokeOutcome.Unrecognized;
        }

        public static string CaptionFor(StrokeOutcome o)
        {
            return o switch
            {
                StrokeOutcome.TooShort => TooShort,
                StrokeOutcome.Unrecognized => Unrecognized,
                StrokeOutcome.Cooldown => OnCooldown,
                _ => string.Empty
            };
        }

        /// <summary>"Saldırı → Patlama" — cümlenin rün adları sırayla.</summary>
        public static string RuneChain(IReadOnlyList<SentenceWord> words)
        {
            if (words == null || words.Count == 0)
                return string.Empty;
            var sb = new StringBuilder(GrammarDefaults.DrawCaptionBufferCapacity);
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
            float fadeStart = CaptionSec * GrammarDefaults.CaptionFadeStartRatio;
            return elapsedSec <= fadeStart ? 1f : 1f - (elapsedSec - fadeStart) / (CaptionSec - fadeStart);
        }
    }
}
