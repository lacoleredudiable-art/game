using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Motion
{
    public sealed class MotionFallbacks
    {
        public static readonly MotionFallbacks Coded = new(MotionTemplateCatalogDefaults.FallbackPhaseSec, MotionTemplateCatalogDefaults.FallbackStepM, MotionTemplateCatalogDefaults.FallbackHitLengthM, MotionTemplateCatalogDefaults.FallbackHitRadiusM, MotionTemplateCatalogDefaults.FallbackMaxHoldSec, MotionTemplateCatalogDefaults.FallbackWalkMps, MotionTemplateCatalogDefaults.FallbackHeightM, MotionTemplateCatalogDefaults.FallbackGapM, MotionTemplateCatalogDefaults.FallbackStopGapM);

        public MotionFallbacks(
            float phaseSec,
            float stepM,
            float hitLengthM,
            float hitRadiusM,
            float maxHoldSec,
            float walkMps,
            float heightM,
            float gapM,
            float stopGapM)
        {
            PhaseSec = phaseSec;
            StepM = stepM;
            HitLengthM = hitLengthM;
            HitRadiusM = hitRadiusM;
            MaxHoldSec = maxHoldSec;
            WalkMps = walkMps;
            HeightM = heightM;
            GapM = gapM;
            StopGapM = stopGapM;
        }

        public float PhaseSec { get; }
        public float StepM { get; }
        public float HitLengthM { get; }
        public float HitRadiusM { get; }
        public float MaxHoldSec { get; }
        public float WalkMps { get; }
        public float HeightM { get; }
        public float GapM { get; }
        /// <summary>Saldıran kenarı ile hedef kenarı arasındaki duruş payı.</summary>
        public float StopGapM { get; }
    }
}
