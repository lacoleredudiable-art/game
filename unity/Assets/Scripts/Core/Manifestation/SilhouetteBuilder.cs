using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;

namespace Dovus.Core.Manifestation
{
    /// <summary>
    /// Cümle kelimelerinden silüet üretir. Kombo tablosu yok — her sıfat bir ekseni iter.
    /// </summary>
    public static class SilhouetteBuilder
    {
        public static EffectSilhouette FromWords(
            IReadOnlyList<SentenceWord> words,
            ManifestationTuning? tuning = null)
        {
            tuning ??= new ManifestationTuning();
            if (words == null || words.Count == 0)
                return default;

            EffectSilhouette s = VerbSeed(words[0].Rune);
            s = ApplyIntensity(s, words[0].IntensityStacks, tuning);

            for (int i = 1; i < words.Count; i++)
            {
                s = ApplyAdjective(s, words[i].Rune, tuning);
                s = ApplyIntensity(s, words[i].IntensityStacks, tuning);
            }

            return s.Clamped();
        }

        public static EffectSilhouette VerbSeed(Rune verb) => verb switch
        {
            Rune.Sarsinti => new EffectSilhouette(focus: 0f, pierce: 0f, spread: 0f, lift: 0.25f),
            Rune.Igne => new EffectSilhouette(focus: 0.82f, pierce: 0.7f, spread: 0f, lift: 0f),
            Rune.Suru => new EffectSilhouette(focus: 0.15f, pierce: 0f, spread: 0.55f, lift: 0f),
            // Prototip seti dışı fiiller: nötr tohum (T7 kapsamı değil; gramer yine üretebilir)
            Rune.Kabuk => new EffectSilhouette(focus: 0.3f, pierce: 0f, spread: 0f, lift: 0f),
            Rune.Zehir => new EffectSilhouette(focus: 0.2f, pierce: 0f, spread: 0.25f, lift: 0f),
            _ => default
        };

        public static EffectSilhouette ApplyAdjective(
            EffectSilhouette current,
            Rune adjective,
            ManifestationTuning tuning)
        {
            float f = current.Focus;
            float p = current.Pierce;
            float s = current.Spread;
            float l = current.Lift;

            switch (adjective)
            {
                case Rune.Igne:
                    f += tuning.FocusPerIgne;
                    p += tuning.PiercePerIgne;
                    break;
                case Rune.Suru:
                    s += tuning.SpreadPerSuru;
                    // Halka ise hafif aç; hat ise hat boyunca çoğalt (odak korunur)
                    if (f < tuning.SuruFocusReduceThreshold)
                        f -= tuning.SuruFocusReduceAmount;
                    break;
                case Rune.Sarsinti:
                    l += tuning.LiftPerSarsinti;
                    break;
                case Rune.Kabuk:
                    // Tut/katılaştır — yayılmayı keser, hatı kalınlaştırır (sayı değil silüet)
                    s *= tuning.KabukSpreadMultiplier;
                    f += tuning.KabukFocusAdd;
                    break;
                case Rune.Zehir:
                    s += tuning.ZehirSpreadAdd;
                    break;
            }

            return new EffectSilhouette(f, p, s, l);
        }

        static EffectSilhouette ApplyIntensity(
            EffectSilhouette s,
            int stacks,
            ManifestationTuning tuning)
        {
            if (stacks <= 0)
                return s;

            float boost = 1f + stacks * tuning.DwellStackScale;
            return new EffectSilhouette(
                s.Focus * boost,
                s.Pierce * boost,
                s.Spread * boost,
                s.Lift * boost);
        }
    }
}
