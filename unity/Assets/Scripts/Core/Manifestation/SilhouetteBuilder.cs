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
            // element-sistemi çekirdek fiilleri
            Rune.Ates => new EffectSilhouette(focus: 0.82f, pierce: 0.7f, spread: 0f, lift: 0f),      // Ateş saldırı
            Rune.Su => new EffectSilhouette(focus: 0.25f, pierce: 0f, spread: 0.35f, lift: 0.05f),   // Su heal
            Rune.Hava => new EffectSilhouette(focus: 0.15f, pierce: 0.2f, spread: 0.45f, lift: 0f),  // Hava hareket
            Rune.Toprak => new EffectSilhouette(focus: 0.35f, pierce: 0f, spread: 0f, lift: 0.25f),    // Toprak savunma
            Rune.Aydinlik => new EffectSilhouette(focus: 0.88f, pierce: 0.4f, spread: 0f, lift: 0f), // Aydınlık arındırma
            Rune.Karanlik => new EffectSilhouette(focus: 0.2f, pierce: 0f, spread: 0.5f, lift: 0f),    // Karanlık gizlilik
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
                case Rune.Ates:
                    // Ateş — yoğunlaştırma
                    f += tuning.FocusPerIgne;
                    p += tuning.PiercePerIgne;
                    break;
                case Rune.Su:
                    // Su — yayma
                    s += tuning.SpreadPerSuru;
                    if (f < tuning.SuruFocusReduceThreshold)
                        f -= tuning.SuruFocusReduceAmount;
                    break;
                case Rune.Hava:
                    // Hava — taşıma
                    p += tuning.PiercePerIgne * 0.4f;
                    s += tuning.SpreadPerSuru * 0.5f;
                    break;
                case Rune.Toprak:
                    // Toprak — sabitleme
                    s *= tuning.KabukSpreadMultiplier;
                    f += tuning.KabukFocusAdd;
                    l += tuning.LiftPerSarsinti;
                    break;
                case Rune.Aydinlik:
                    // Aydınlık — saflaştırma / odak
                    f += tuning.FocusPerIgne;
                    p += tuning.PiercePerIgne * 0.35f;
                    break;
                case Rune.Karanlik:
                    // Karanlık — örtme
                    s += tuning.SpreadPerSuru * 0.75f;
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
