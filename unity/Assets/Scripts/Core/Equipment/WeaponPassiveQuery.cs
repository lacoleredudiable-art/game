using System;

namespace Dovus.Core.Equipment
{
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
}
