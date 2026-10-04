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
                    f += tuning.FocusPerIgne * SilhouetteBuilderDefaults.Focus035f;
                    s *= SilhouetteBuilderDefaults.LitN07f;
                    break;
                case "wave":
                case "cloud":
                    s += tuning.SpreadPerSuru;
                    if (f < tuning.SuruFocusReduceThreshold)
                        f -= tuning.SuruFocusReduceAmount;
                    f *= SilhouetteBuilderDefaults.LitN065f;
                    break;
                case "trail":
                    p += tuning.PiercePerIgne * SilhouetteBuilderDefaults.LitN04f;
                    s += tuning.SpreadPerSuru * SilhouetteBuilderDefaults.LitN045f;
                    break;
                case "lift":
                    l += tuning.LiftPerSarsinti;
                    break;
                case "ring":
                    f = MathF.Min(f, SilhouetteBuilderDefaults.LitN025f);
                    s += tuning.SpreadPerSuru * SilhouetteBuilderDefaults.LitN06f;
                    break;
                case "hollow":
                    f *= 0.5f;
                    s += tuning.SpreadPerSuru * SilhouetteBuilderDefaults.LitN04f;
                    break;
                case "cone":
                    f = SilhouetteBuilderDefaults.LitN045f + f * SilhouetteBuilderDefaults.LitN03f;
                    s += tuning.SpreadPerSuru * SilhouetteBuilderDefaults.LitN055f;
                    break;
                default:
                    break;
            }

            return new EffectSilhouette(f, p, s, l);
        }

        static EffectSilhouette VerbFamilySeed(string family, string hitbox)
        {
            if (hitbox is "projectile" or "beam" or "chain_projectile" or "raycast")
                return new EffectSilhouette(focus: SilhouetteBuilderDefaults.Focus082f, pierce: SilhouetteBuilderDefaults.LitN07f, spread: 0f, lift: 0f);
            if (hitbox is "static_cloud" or "ground_circle" or "ground_ring" or "ground_surface")
                return new EffectSilhouette(focus: SilhouetteBuilderDefaults.Focus02f, pierce: 0f, spread: 0.5f, lift: 0f);
            if (hitbox is "cone" or "radial_burst")
                return new EffectSilhouette(focus: SilhouetteBuilderDefaults.Focus035f, pierce: SilhouetteBuilderDefaults.Focus015f, spread: SilhouetteBuilderDefaults.LitN055f, lift: 0f);
            if (hitbox is "wall" or "ground_line")
                return new EffectSilhouette(focus: SilhouetteBuilderDefaults.LitN04f, pierce: 0f, spread: SilhouetteBuilderDefaults.Min10f, lift: SilhouetteBuilderDefaults.Focus02f);
            if (hitbox is "self" or "self_aura" or "target_ally")
                return new EffectSilhouette(focus: 0.5f, pierce: 0f, spread: SilhouetteBuilderDefaults.LitN025f, lift: SilhouetteBuilderDefaults.Min05f);

            return family switch
            {
                "strike" => new EffectSilhouette(SilhouetteBuilderDefaults.Focus082f, SilhouetteBuilderDefaults.LitN07f, 0f, 0f),
                "mend" or "purge" or "guard" => new EffectSilhouette(SilhouetteBuilderDefaults.LitN025f, 0f, SilhouetteBuilderDefaults.Focus035f, SilhouetteBuilderDefaults.Min05f),
                "motion" => new EffectSilhouette(SilhouetteBuilderDefaults.Focus015f, SilhouetteBuilderDefaults.Focus02f, SilhouetteBuilderDefaults.LitN045f, 0f),
                "zone" or "control" => new EffectSilhouette(SilhouetteBuilderDefaults.Focus035f, 0f, 0f, SilhouetteBuilderDefaults.LitN025f),
                "disrupt" => new EffectSilhouette(SilhouetteBuilderDefaults.Focus02f, 0f, 0.5f, 0f),
                _ => new EffectSilhouette(0.5f, SilhouetteBuilderDefaults.LitN025f, SilhouetteBuilderDefaults.Focus02f, 0f)
            };
        }

        public static EffectSilhouette VerbSeed(Rune verb) => verb switch
        {
            // element-sistemi çekirdek fiilleri
            Rune.Ates => new EffectSilhouette(focus: SilhouetteBuilderDefaults.Focus082f, pierce: SilhouetteBuilderDefaults.LitN07f, spread: 0f, lift: 0f),      // Ateş saldırı
            Rune.Su => new EffectSilhouette(focus: SilhouetteBuilderDefaults.LitN025f, pierce: 0f, spread: SilhouetteBuilderDefaults.Focus035f, lift: SilhouetteBuilderDefaults.Min05f),   // Su heal
            Rune.Hava => new EffectSilhouette(focus: SilhouetteBuilderDefaults.Focus015f, pierce: SilhouetteBuilderDefaults.Focus02f, spread: SilhouetteBuilderDefaults.LitN045f, lift: 0f),  // Hava hareket
            Rune.Toprak => new EffectSilhouette(focus: SilhouetteBuilderDefaults.Focus035f, pierce: 0f, spread: 0f, lift: SilhouetteBuilderDefaults.LitN025f),    // Toprak savunma
            Rune.Aydinlik => new EffectSilhouette(focus: SilhouetteBuilderDefaults.Focus088f, pierce: SilhouetteBuilderDefaults.LitN04f, spread: 0f, lift: 0f), // Aydınlık arındırma
            Rune.Karanlik => new EffectSilhouette(focus: SilhouetteBuilderDefaults.Focus02f, pierce: 0f, spread: 0.5f, lift: 0f),    // Karanlık gizlilik
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
                    p += tuning.PiercePerIgne * SilhouetteBuilderDefaults.LitN04f;
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
                    p += tuning.PiercePerIgne * SilhouetteBuilderDefaults.Focus035f;
                    break;
                case Rune.Karanlik:
                    // Karanlık — örtme
                    s += tuning.SpreadPerSuru * SilhouetteBuilderDefaults.LitN075f;
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