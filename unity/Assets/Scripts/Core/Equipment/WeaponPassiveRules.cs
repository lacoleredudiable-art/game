using System;

namespace Dovus.Core.Equipment
{
    /// <summary>
    /// Uyumlu fiilde pasif çalışır. Şifa (2), kalkan (4) ve güç (8) destek fiilleridir.
    /// </summary>
    public static class WeaponPassiveRules
    {
        public static bool IsSupportVerb(int verbId) => verbId is 2 or 4 or 8;

        public static WeaponPassiveMods Evaluate(WeaponCombatProfile profile, in WeaponPassiveQuery query)
        {
            if (profile == null)
                return WeaponPassiveMods.Identity;
            WeaponPassiveSpec spec = profile.Passive;
            // Kutsal etki kuşanılıyken fiil uyumuna bakmaz: şifa, kalkan ve buff büyüklüğü.
            if (!query.PassiveEnabled)
            {
                if (spec.Kind == WeaponPassiveKind.KutsalEtki)
                    return HolyMods(spec.PowerMult);
                return WeaponPassiveMods.Identity;
            }
            switch (spec.Kind)
            {
                case WeaponPassiveKind.SirtVurusu:
                    if (!query.Harmful || query.BehindAngleDeg > spec.ArcDeg * 0.5f)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(spec.DamageMult, 1f, 1f, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case WeaponPassiveKind.GenisYay:
                    return new WeaponPassiveMods(
                        1f, 1f, 1f, 0f, false, 0f,
                        MeleeArc.FriendlyVerb(query.VerbId), spec.ArcDeg,
                        false, false, 0f, 0f, false, 1f, 1f);
                case WeaponPassiveKind.YereCakma:
                    if (!query.Harmful)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(1f, 1f, 1f, 0f, true, spec.StunSec, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case WeaponPassiveKind.KarsiSaldiri:
                    if (!query.BlockedRecently)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(spec.DamageMult, 1f, 1f, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case WeaponPassiveKind.KosuAtisi:
                    if (query.SinceMovedSec > spec.MoveWindowSec)
                        return WeaponPassiveMods.Identity;
                    float add = Math.Max(0f, spec.CritChance - spec.BaseCrit);
                    return new WeaponPassiveMods(1f, 1f, 1f, add, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case WeaponPassiveKind.SabitNisan:
                    if (query.SinceMovedSec < spec.StillSec)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(spec.DamageMult, 1f, 1f, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case WeaponPassiveKind.UzunBuyu:
                    if (!query.Sustained)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(1f, 1f, spec.DurationMult, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case WeaponPassiveKind.KutsalEtki:
                    return HolyMods(spec.PowerMult);
                case WeaponPassiveKind.DoluSayfa:
                    if (spec.EveryNth <= 0 || query.ChainIndex <= 0 || query.ChainIndex % spec.EveryNth != 0)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(spec.DamageMult, 1f, 1f, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case WeaponPassiveKind.CaprazAtes:
                    if (query.OrbAngleDeg < spec.AngleDeg)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(spec.DamageMult, 1f, 1f, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                default:
                    return WeaponPassiveMods.Identity;
            }
        }

        static WeaponPassiveMods HolyMods(float power) =>
            new(1f, power, 1f, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);

        /// <summary>
        /// Tılsım kuşanılıyken şifa, kalkan ve buff büyüklüğü. Fiil uyumu aranmaz.
        /// Hasar çarpanının yerine geçmez; o ayrı kalır.
        /// </summary>
        public static float HolyMagnitude(WeaponCombatProfile profile)
        {
            if (profile != null && profile.Passive.Kind == WeaponPassiveKind.KutsalEtki && profile.Passive.PowerMult > 0f)
                return profile.Passive.PowerMult;
            return 1f;
        }

        /// <summary>Kutsal büyüklük. Değilse çağıranın verdiği silah hasar çarpanı.</summary>
        public static float SupportPower(WeaponCombatProfile profile, bool passiveEnabled, int verbId, float weaponDamageMult)
        {
            if (HolyMagnitude(profile) > 1f)
                return HolyMagnitude(profile);
            return weaponDamageMult > 0f ? weaponDamageMult : 1f;
        }

        /// <summary>Oyuncunun ürettiği şifa, kalkan veya buff miktarı.</summary>
        public static float ScaleFriendlyMagnitude(float magnitude, float holyMult)
        {
            if (magnitude <= 0f || holyMult <= 0f)
                return magnitude;
            return magnitude * holyMult;
        }

        /// <summary>Poise = taban × skill × silah × swap. Swap yoksa bonus 1.</summary>
        public static float OutgoingPoise(float basePoise, float skillMult, float weaponMult, float bonusMult)
        {
            if (basePoise <= 0f)
                return 0f;
            float skill = skillMult > 0f ? skillMult : 1f;
            float weapon = weaponMult > 0f ? weaponMult : 1f;
            float bonus = bonusMult > 0f ? bonusMult : 1f;
            return basePoise * skill * weapon * bonus;
        }

        /// <summary>Sersem beklemesi yalnız sersem gerçekten oturunca başlar.</summary>
        public static bool CommitStunOnLand(bool ready, bool alreadyHadStun, bool applied) =>
            ready && !alreadyHadStun && applied;

        public static bool AngleInArc(float deltaDeg, float arcDeg)
        {
            if (arcDeg <= 0f)
                return false;
            float half = arcDeg * 0.5f;
            float d = Math.Abs(deltaDeg) % 360f;
            if (d > 180f)
                d = 360f - d;
            return d <= half;
        }
    }
}
