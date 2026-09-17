using System;
using System.Collections.Generic;
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
                    f += tuning.FocusPerIgne * 0.35f;
                    s *= 0.7f;
                    break;
                case "wave":
                case "cloud":
                    s += tuning.SpreadPerSuru;
                    if (f < tuning.SuruFocusReduceThreshold)
                        f -= tuning.SuruFocusReduceAmount;
                    f *= 0.65f;
                    break;
                case "trail":
                    p += tuning.PiercePerIgne * 0.4f;
                    s += tuning.SpreadPerSuru * 0.45f;
                    break;
                case "lift":
                    l += tuning.LiftPerSarsinti;
                    break;
                case "ring":
                    f = MathF.Min(f, 0.25f);
                    s += tuning.SpreadPerSuru * 0.6f;
                    break;
                case "hollow":
                    f *= 0.5f;
                    s += tuning.SpreadPerSuru * 0.4f;
                    break;
                case "cone":
                    f = 0.45f + f * 0.3f;
                    s += tuning.SpreadPerSuru * 0.55f;
                    break;
                default:
                    break;
            }

            return new EffectSilhouette(f, p, s, l);
        }

        static EffectSilhouette VerbFamilySeed(string family, string hitbox)
        {
            if (hitbox is "projectile" or "beam" or "chain_projectile" or "raycast")
                return new EffectSilhouette(focus: 0.82f, pierce: 0.7f, spread: 0f, lift: 0f);
            if (hitbox is "static_cloud" or "ground_circle" or "ground_ring" or "ground_surface")
                return new EffectSilhouette(focus: 0.2f, pierce: 0f, spread: 0.5f, lift: 0f);
            if (hitbox is "cone" or "radial_burst")
                return new EffectSilhouette(focus: 0.35f, pierce: 0.15f, spread: 0.55f, lift: 0f);
            if (hitbox is "wall" or "ground_line")
                return new EffectSilhouette(focus: 0.4f, pierce: 0f, spread: 0.1f, lift: 0.2f);
            if (hitbox is "self" or "self_aura" or "target_ally")
                return new EffectSilhouette(focus: 0.5f, pierce: 0f, spread: 0.25f, lift: 0.05f);

            return family switch
            {
                "strike" => new EffectSilhouette(0.82f, 0.7f, 0f, 0f),
                "mend" or "purge" or "guard" => new EffectSilhouette(0.25f, 0f, 0.35f, 0.05f),
                "motion" => new EffectSilhouette(0.15f, 0.2f, 0.45f, 0f),
                "zone" or "control" => new EffectSilhouette(0.35f, 0f, 0f, 0.25f),
                "disrupt" => new EffectSilhouette(0.2f, 0f, 0.5f, 0f),
                _ => new EffectSilhouette(0.5f, 0.25f, 0.2f, 0f)
            };
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
