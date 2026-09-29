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
            if (profile == null || !query.PassiveEnabled)
                return WeaponPassiveMods.Identity;
            WeaponPassiveSpec spec = profile.Passive;
            switch (spec.Id)
            {
                case "sirt_vurusu":
                    if (!query.Harmful || query.BehindAngleDeg > spec.ArcDeg * 0.5f)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(spec.DamageMult, 1f, 1f, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
                case "genis_yay":
                    return new WeaponPassiveMods(1f, 1f, 1f, 0f, false, 0f, query.SupportVerb, spec.ArcDeg, false, false, 0f, 0f, false, 1f, 1f);
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
                    if (!query.SupportVerb)
                        return WeaponPassiveMods.Identity;
                    return new WeaponPassiveMods(1f, spec.PowerMult, 1f, 0f, false, 0f, false, 0f, false, false, 0f, 0f, false, 1f, 1f);
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

        /// <summary>Destek fiilinde kutsal etki, silahın düşük güç çarpanının yerine geçer.</summary>
        public static float SupportPower(WeaponCombatProfile profile, bool passiveEnabled, int verbId, float weaponDamageMult)
        {
            if (profile != null && passiveEnabled && profile.Passive.Id == "kutsal_etki" && IsSupportVerb(verbId))
                return profile.Passive.PowerMult;
            return weaponDamageMult > 0f ? weaponDamageMult : 1f;
        }

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

        public bool BonusArmed(double nowMs) => _bonusArmed && nowMs < _bonusUntilMs;

        public string BonusId => _bonusArmed ? _bonusId : string.Empty;

        public void NoteBlock(double nowMs, float windowSec)
        {
            _counterUntilMs = nowMs + Math.Max(0f, windowSec) * 1000.0;
        }

        public bool TryHammerStun(double nowMs, float icdSec)
        {
            if (nowMs < _hammerReadyMs)
                return false;
            _hammerReadyMs = nowMs + Math.Max(0f, icdSec) * 1000.0;
            return true;
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

        public void ResetChain()
        {
            _chainCount = 0;
            _chainStartMs = double.NegativeInfinity;
        }
    }
}
