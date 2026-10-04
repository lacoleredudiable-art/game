using System;
using System.Collections.Generic;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;

namespace Dovus.Core.Manifestation
{
    /// <summary>
    /// Cümle kelimelerinden silüet üretir. Kombo tablosu yok — her sıfat bir ekseni iter.
    /// Katlama yolu: <see cref="FromSkill"/> (JSON silhouette_axis).
    /// </summary>
    public static class SilhouetteBuilder
    {
        /// <summary>
        /// SkillMotor katlama sonucu — fiil ailesi seed + çözülmüş sıfat ekseni.
        /// Ara rünleri sıfat saymaz (fold: 1-2 bileşik, 3. kök sıfat).
        /// </summary>
        public static EffectSilhouette FromSkill(
            in SkillResolution skill,
            ManifestationTuning? tuning = null)
        {
            tuning ??= new ManifestationTuning();
            if (skill.IsEmpty)
                return default;

            EffectSilhouette s = VerbFamilySeed(skill.VerbFamily, skill.Hitbox);
            s = ApplySilhouetteAxis(s, skill.SilhouetteAxis, tuning);
            return s.Clamped();
        }

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

        /// <summary>JSON adjectives.silhouette_axis → float eksenleri.</summary>
        public static EffectSilhouette ApplySilhouetteAxis(
            EffectSilhouette current,
            string axis,
            ManifestationTuning tuning)
        {
            if (string.IsNullOrEmpty(axis) || axis.Equals("none", StringComparison.Ordinal))
                return current;

            float f = current.Focus;
            float p = current.Pierce;
            float s = current.Spread;
            float l = current.Lift;

            switch (axis)
            {
                case "focus":
                    f += tuning.FocusPerIgne;
                    p += tuning.PiercePerIgne * 0.5f;
                    break;
                case "pierce":
                    p += tuning.PiercePerIgne;
                    f += tuning.FocusPerIgne * SilhouetteBuilderDefaults.FocusWeightMid;
                    s *= SilhouetteBuilderDefaults.PierceWeightHigh;
                    break;
                case "wave":
                case "cloud":
                    s += tuning.SpreadPerSuru;
                    if (f < tuning.SuruFocusReduceThreshold)
                        f -= tuning.SuruFocusReduceAmount;
                    f *= SilhouetteBuilderDefaults.FocusScaleMult;
                    break;
                case "trail":
                    p += tuning.PiercePerIgne * SilhouetteBuilderDefaults.PierceSpreadScale;
                    s += tuning.SpreadPerSuru * SilhouetteBuilderDefaults.SpreadScaleBase;
                    break;
                case "lift":
                    l += tuning.LiftPerSarsinti;
                    break;
                case "ring":
                    f = MathF.Min(f, SilhouetteBuilderDefaults.SpreadWeightQuarter);
                    s += tuning.SpreadPerSuru * SilhouetteBuilderDefaults.SpreadSuruMidMult;
                    break;
                case "hollow":
                    f *= 0.5f;
                    s += tuning.SpreadPerSuru * SilhouetteBuilderDefaults.PierceSpreadScale;
                    break;
                case "cone":
                    f = SilhouetteBuilderDefaults.SpreadScaleBase + f * SilhouetteBuilderDefaults.FocusLerpWeight;
                    s += tuning.SpreadPerSuru * SilhouetteBuilderDefaults.SpreadSuruHighMult;
                    break;
                default:
                    break;
            }

            return new EffectSilhouette(f, p, s, l);
        }

        static EffectSilhouette VerbFamilySeed(string family, string hitbox)
        {
            if (hitbox is "projectile" or "beam" or "chain_projectile" or "raycast")
                return new EffectSilhouette(focus: SilhouetteBuilderDefaults.FocusWeightStrike, pierce: SilhouetteBuilderDefaults.PierceWeightHigh, spread: 0f, lift: 0f);
            if (hitbox is "static_cloud" or "ground_circle" or "ground_ring" or "ground_surface")
                return new EffectSilhouette(focus: SilhouetteBuilderDefaults.FocusWeightMidLow, pierce: 0f, spread: 0.5f, lift: 0f);
            if (hitbox is "cone" or "radial_burst")
                return new EffectSilhouette(focus: SilhouetteBuilderDefaults.FocusWeightMid, pierce: SilhouetteBuilderDefaults.FocusWeightLow, spread: SilhouetteBuilderDefaults.SpreadSuruHighMult, lift: 0f);
            if (hitbox is "wall" or "ground_line")
                return new EffectSilhouette(focus: SilhouetteBuilderDefaults.PierceSpreadScale, pierce: 0f, spread: SilhouetteBuilderDefaults.SpreadWeightDefault, lift: SilhouetteBuilderDefaults.FocusWeightMidLow);
            if (hitbox is "self" or "self_aura" or "target_ally")
                return new EffectSilhouette(focus: 0.5f, pierce: 0f, spread: SilhouetteBuilderDefaults.SpreadWeightQuarter, lift: SilhouetteBuilderDefaults.LiftWeightLow);

            return family switch
            {
                "strike" => new EffectSilhouette(SilhouetteBuilderDefaults.FocusWeightStrike, SilhouetteBuilderDefaults.PierceWeightHigh, 0f, 0f),
                "mend" or "purge" or "guard" => new EffectSilhouette(SilhouetteBuilderDefaults.SpreadWeightQuarter, 0f, SilhouetteBuilderDefaults.FocusWeightMid, SilhouetteBuilderDefaults.LiftWeightLow),
                "motion" => new EffectSilhouette(SilhouetteBuilderDefaults.FocusWeightLow, SilhouetteBuilderDefaults.FocusWeightMidLow, SilhouetteBuilderDefaults.SpreadScaleBase, 0f),
                "zone" or "control" => new EffectSilhouette(SilhouetteBuilderDefaults.FocusWeightMid, 0f, 0f, SilhouetteBuilderDefaults.SpreadWeightQuarter),
                "disrupt" => new EffectSilhouette(SilhouetteBuilderDefaults.FocusWeightMidLow, 0f, 0.5f, 0f),
                _ => new EffectSilhouette(0.5f, SilhouetteBuilderDefaults.SpreadWeightQuarter, SilhouetteBuilderDefaults.FocusWeightMidLow, 0f)
            };
        }

        public static EffectSilhouette VerbSeed(Rune verb) => verb switch
        {
            // element-sistemi çekirdek fiilleri
            Rune.Ates => new EffectSilhouette(focus: SilhouetteBuilderDefaults.FocusWeightStrike, pierce: SilhouetteBuilderDefaults.PierceWeightHigh, spread: 0f, lift: 0f),      // Ateş saldırı
            Rune.Su => new EffectSilhouette(focus: SilhouetteBuilderDefaults.SpreadWeightQuarter, pierce: 0f, spread: SilhouetteBuilderDefaults.FocusWeightMid, lift: SilhouetteBuilderDefaults.LiftWeightLow),   // Su heal
            Rune.Hava => new EffectSilhouette(focus: SilhouetteBuilderDefaults.FocusWeightLow, pierce: SilhouetteBuilderDefaults.FocusWeightMidLow, spread: SilhouetteBuilderDefaults.SpreadScaleBase, lift: 0f),  // Hava hareket
            Rune.Toprak => new EffectSilhouette(focus: SilhouetteBuilderDefaults.FocusWeightMid, pierce: 0f, spread: 0f, lift: SilhouetteBuilderDefaults.SpreadWeightQuarter),    // Toprak savunma
            Rune.Aydinlik => new EffectSilhouette(focus: SilhouetteBuilderDefaults.FocusWeightLight, pierce: SilhouetteBuilderDefaults.PierceSpreadScale, spread: 0f, lift: 0f), // Aydınlık arındırma
            Rune.Karanlik => new EffectSilhouette(focus: SilhouetteBuilderDefaults.FocusWeightMidLow, pierce: 0f, spread: 0.5f, lift: 0f),    // Karanlık gizlilik
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
                    p += tuning.PiercePerIgne * SilhouetteBuilderDefaults.PierceSpreadScale;
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
                    p += tuning.PiercePerIgne * SilhouetteBuilderDefaults.FocusWeightMid;
                    break;
                case Rune.Karanlik:
                    // Karanlık — örtme
                    s += tuning.SpreadPerSuru * SilhouetteBuilderDefaults.SpreadSuruPeakMult;
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