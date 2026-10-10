using System;

namespace Dovus.Core.RuleEngineV4
{
    public sealed class RuleEngineV4Globals
    {
        public float PrefireCapSec { get; init; }
        public float FriendlyRangeCapM { get; init; }
        public bool ClipInnerMeasuresToRange { get; init; }
        public float YogunChargeSec { get; init; }
        public float YogunPowerMult { get; init; }
        public float YogunDurationMult { get; init; }
        public float YayilanRadiusM { get; init; }
        public int YayilanTargetCap { get; init; }
        public float YayilanPowerMult { get; init; }
        public int SicrayanBounceCount { get; init; }
        public float SicrayanBounceMult { get; init; }
        public float SicrayanSearchM { get; init; }
        public float GudumluLockCapSec { get; init; }
        public float GudumluBlockSec { get; init; }
        public float SabitStructureLifeSec { get; init; }
        public float RitmReferenceTotalSec { get; init; }
        public float KontrolDurationSec { get; init; }
        public float KorumaYogunBlockSec { get; init; }
        public float KorumaDefaultBlockSec { get; init; }
        public float TetikliWaitSec { get; init; }
        public float IsaretUseRangeM { get; init; }
        public float IsaretLifeSec { get; init; }
        public int IsaretMaxPerPlayer { get; init; }
        public float ZamanLookbackSec { get; init; }
        public float KuvvetPushM { get; init; }
        public float YansitmaRatio { get; init; }
        public float YansitmaDurationSec { get; init; }
        public float YansitmaYogunDurationSec { get; init; }
        public float ConjureLifeSec { get; init; }
        public float ConjureYogunLifeSec { get; init; }
    }
}
