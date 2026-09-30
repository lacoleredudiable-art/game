using System;

namespace Dovus.Core.Equipment
{
    /// <summary>Bir vuruşta silah pasifinin istediği çarpanlar. Sayılar profilin JSON'undan gelir.</summary>
    public readonly struct WeaponPassiveMods
    {
        public WeaponPassiveMods(
            float damageMult,
            float healMult,
            float durationMult,
            float critChanceAdd,
            bool stun,
            float stunSec,
            bool arcAllies,
            float arcDeg,
            bool ignoreArmor,
            bool cleanseOne,
            float shieldGrant,
            float shieldSec,
            bool freeMana,
            float areaMult,
            float poiseMult)
        {
            DamageMult = damageMult > 0f ? damageMult : 1f;
            HealMult = healMult > 0f ? healMult : 1f;
            DurationMult = durationMult > 0f ? durationMult : 1f;
            CritChanceAdd = critChanceAdd > 0f ? critChanceAdd : 0f;
            Stun = stun;
            StunSec = stunSec > 0f ? stunSec : 0f;
            ArcAllies = arcAllies;
            ArcDeg = arcDeg;
            IgnoreArmor = ignoreArmor;
            CleanseOne = cleanseOne;
            ShieldGrant = shieldGrant > 0f ? shieldGrant : 0f;
            ShieldSec = shieldSec > 0f ? shieldSec : 0f;
            FreeMana = freeMana;
            AreaMult = areaMult > 0f ? areaMult : 1f;
            PoiseMult = poiseMult > 0f ? poiseMult : 1f;
        }

        public static WeaponPassiveMods Identity { get; } = new(1f, 1f, 1f, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);

        public float DamageMult { get; }
        public float HealMult { get; }
        public float DurationMult { get; }
        public float CritChanceAdd { get; }
        public bool Stun { get; }
        public float StunSec { get; }
        public bool ArcAllies { get; }
        public float ArcDeg { get; }
        public bool IgnoreArmor { get; }
        public bool CleanseOne { get; }
        public float ShieldGrant { get; }
        public float ShieldSec { get; }
        public bool FreeMana { get; }
        public float AreaMult { get; }
        public float PoiseMult { get; }
    }

    public readonly struct WeaponPassiveQuery
    {
        public WeaponPassiveQuery(
            int verbId,
            bool passiveEnabled,
            bool harmful,
            bool sustained,
            float behindAngleDeg,
            float sinceMovedSec,
            bool blockedRecently,
            int chainIndex,
            float orbAngleDeg,
            bool supportVerb)
        {
            VerbId = verbId;
            PassiveEnabled = passiveEnabled;
            Harmful = harmful;
            Sustained = sustained;
            BehindAngleDeg = behindAngleDeg;
            SinceMovedSec = sinceMovedSec;
            BlockedRecently = blockedRecently;
            ChainIndex = chainIndex;
            OrbAngleDeg = orbAngleDeg;
            SupportVerb = supportVerb;
        }

        public int VerbId { get; }
        public bool PassiveEnabled { get; }
        public bool Harmful { get; }
        public bool Sustained { get; }
        /// <summary>Boss'un baktığı yön ile vuruş yönü. 0 = tam sırt, 180 = tam ön.</summary>
        public float BehindAngleDeg { get; }
        public float SinceMovedSec { get; }
        public bool BlockedRecently { get; }
        /// <summary>Penceredeki skill sırası, 1'den başlar. 0 = sayaç yok.</summary>
        public int ChainIndex { get; }
        /// <summary>Boss'tan bakınca oyuncu ile küre arasındaki açı.</summary>
        public float OrbAngleDeg { get; }
        public bool SupportVerb { get; }
    }

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
                if (spec.Id == "kutsal_etki")
                    return HolyMods(spec.PowerMult);
                return WeaponPassiveMods.Identity;
            }
            switch (spec.Id)
            {
                case "sirt_vurusu":
                    if (!query.Harmful || query.BehindAngleDeg > spec.ArcDeg * 0.5f)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(spec.DamageMult, 1f, 1f, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case "genis_yay":
                    return new WeaponPassiveMods(
                        1f, 1f, 1f, 0f, false, 0f,
                        MeleeArc.FriendlyVerb(query.VerbId), spec.ArcDeg,
                        false, false, 0f, 0f, false, 1f, 1f);
                case "yere_cakma":
                    if (!query.Harmful)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(1f, 1f, 1f, 0f, true, spec.StunSec, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case "karsi_saldiri":
                    if (!query.BlockedRecently)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(spec.DamageMult, 1f, 1f, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case "kosu_atisi":
                    if (query.SinceMovedSec > spec.MoveWindowSec)
                        return WeaponPassiveMods.Identity;
                    float add = Math.Max(0f, spec.CritChance - spec.BaseCrit);
                    return new WeaponPassiveMods(1f, 1f, 1f, add, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case "sabit_nisan":
                    if (query.SinceMovedSec < spec.StillSec)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(spec.DamageMult, 1f, 1f, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case "uzun_buyu":
                    if (!query.Sustained)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(1f, 1f, spec.DurationMult, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case "kutsal_etki":
                    return HolyMods(spec.PowerMult);
                case "dolu_sayfa":
                    if (spec.EveryNth <= 0 || query.ChainIndex <= 0 || query.ChainIndex % spec.EveryNth != 0)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(spec.DamageMult, 1f, 1f, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case "capraz_ates":
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
            if (profile != null && profile.Passive.Id == "kutsal_etki" && profile.Passive.PowerMult > 0f)
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

    /// <summary>Pasifin zamanı olan parçaları: çekiç beklemesi, kalkan penceresi, kitap sayacı, swap bonusu.</summary>
    public sealed class WeaponPassiveState
    {
        double _hammerReadyMs;
        double _counterUntilMs;
        double _chainStartMs = double.NegativeInfinity;
        int _chainCount;
        int _bonusWeaponId;
        double _bonusUntilMs;
        bool _bonusArmed;
        string _bonusId = string.Empty;

        public bool CounterArmed(double nowMs) => nowMs < _counterUntilMs;

        public int ChainCount => _chainCount;

        /// <summary>
        /// Aralık dolduysa sayaç bayat: 0 döner ve birikmiş sırayı siler.
        /// Yeni vuruş beklemeden okunur.
        /// </summary>
        public int EffectiveChain(double nowMs, float gapSec)
        {
            if (nowMs - _chainStartMs > Math.Max(0f, gapSec) * 1000.0)
            {
                _chainCount = 0;
                return 0;
            }
            return _chainCount;
        }

        public bool BonusArmed(double nowMs) => _bonusArmed && nowMs < _bonusUntilMs;

        public void NoteBlock(double nowMs, float windowSec)
        {
            _counterUntilMs = nowMs + Math.Max(0f, windowSec) * 1000.0;
        }

        /// <summary>Penceredeki ilk skill vuruşu bonusu yer. Sonrakiler yemez.</summary>
        public bool TryConsumeBlock(double nowMs)
        {
            if (!(nowMs < _counterUntilMs))
                return false;
            _counterUntilMs = double.NegativeInfinity;
            return true;
        }

        public bool HammerReady(double nowMs) => nowMs >= _hammerReadyMs;

        public void CommitHammer(double nowMs, float icdSec)
        {
            _hammerReadyMs = nowMs + Math.Max(0f, icdSec) * 1000.0;
        }

        /// <summary>Art arda skill. Aralık aşılırsa sayaç 1'den başlar. Dönüş bu vuruşun sırası.</summary>
        public int NoteSkill(double nowMs, float gapSec)
        {
            if (nowMs - _chainStartMs > Math.Max(0f, gapSec) * 1000.0)
                _chainCount = 0;
            _chainCount++;
            _chainStartMs = nowMs;
            return _chainCount;
        }

        public void ArmSwapBonus(int weaponId, string bonusId, double nowMs, float windowSec)
        {
            _bonusWeaponId = weaponId;
            _bonusId = bonusId ?? string.Empty;
            _bonusUntilMs = nowMs + Math.Max(0f, windowSec) * 1000.0;
            _bonusArmed = true;
        }

        public bool PeekBonus(int weaponId, double nowMs, out WeaponSwapBonusSpec spec, WeaponCombatProfile profile)
        {
            spec = WeaponSwapBonusSpec.None;
            if (!_bonusArmed || weaponId != _bonusWeaponId || nowMs >= _bonusUntilMs)
                return false;
            spec = profile != null ? profile.SwapBonus : WeaponSwapBonusSpec.None;
            return spec.Id.Length > 0;
        }

        public bool TryConsumeBonus(int weaponId, double nowMs, out WeaponSwapBonusSpec spec, WeaponCombatProfile profile)
        {
            spec = WeaponSwapBonusSpec.None;
            if (!_bonusArmed || weaponId != _bonusWeaponId || nowMs >= _bonusUntilMs)
            {
                if (_bonusArmed && nowMs >= _bonusUntilMs)
                    _bonusArmed = false;
                return false;
            }
            _bonusArmed = false;
            spec = profile != null ? profile.SwapBonus : WeaponSwapBonusSpec.None;
            return spec.Id.Length > 0;
        }

    }

    /// <summary>
    /// Yakın vuruş yayı. Açı 0 ise yay kapısı yok (eski kapsül kuralı durur).
    /// </summary>
    public static class MeleeArc
    {
        public static bool FriendlyVerb(int verbId) => verbId is 2 or 4 or 8 or 9;

        public static bool InFront(float deltaDeg, float arcDeg) =>
            arcDeg <= 0f || WeaponPassiveRules.AngleInArc(deltaDeg, arcDeg);

        /// <summary>
        /// Menzilde ve yayın içinde. ArcAllies açıksa kilit hedef olmayan dost da girer.
        /// </summary>
        public static bool Hits(
            bool inRange,
            float deltaDeg,
            float arcDeg,
            bool designated,
            bool ally,
            bool arcAllies)
        {
            if (!inRange || !InFront(deltaDeg, arcDeg))
                return false;
            if (arcDeg <= 0f)
                return designated;
            return designated || (arcAllies && ally);
        }

        /// <summary>
        /// Kapsül ıskalasa da kenar menzili + yay içi vurur.
        /// Yayın dışı kapsül teması vurmaz. Yay yoksa eski kural.
        /// </summary>
        public static bool StrikeConnects(bool capsuleHit, bool edgeInReach, float deltaDeg, float arcDeg)
        {
            if (!(capsuleHit || edgeInReach))
                return false;
            return InFront(deltaDeg, arcDeg);
        }
    }

    /// <summary>
    /// Süreli alan. Süre uzayınca dilim küçülmez; aynı payla yeni vuruş eklenir.
    /// </summary>
    public static class SustainedField
    {
        public static int TickCount(float durationSec, float tickSec)
        {
            if (durationSec <= 0f)
                return 1;
            float tick = tickSec > 0.01f ? tickSec : 0.01f;
            return Math.Max(1, (int)Math.Ceiling(durationSec / tick - 1e-4f));
        }

        public static float PerTickShare(float baseDurationSec, float tickSec) =>
            1f / TickCount(baseDurationSec, tickSec);
    }
}
