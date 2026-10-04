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
}
