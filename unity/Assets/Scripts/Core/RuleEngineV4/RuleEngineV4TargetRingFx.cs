using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Hedef seçme halkası: gösterilirken <see cref="PulseHz"/> ile hafif nabız (ölçek + parlaklık),
    /// parmak bırakılınca <see cref="ReleaseFadeSec"/> içinde doğrusal söner.
    /// </summary>
    public sealed class RuleEngineV4TargetRingFx
    {
        const float TwoPi = (float)(2.0 * System.Math.PI);

        public float PulseHz { get; private set; } = RuleEngineV4SliceVfxDefaults.TargetRingPulseHz;
        /// <summary>Nabız tepesinde yarıçap artışı (oran).</summary>
        public float PulseScale { get; private set; } = RuleEngineV4SliceVfxDefaults.TargetRingPulseScale;
        /// <summary>Nabız dibinde parlaklık (tepe = 1).</summary>
        public float PulseMinBrightness { get; private set; } = RuleEngineV4SliceVfxDefaults.TargetRingPulseMinBrightness;
        public float ReleaseFadeSec { get; private set; } = RuleEngineV4SliceVfxDefaults.TargetRingReleaseFadeSec;

        public static RuleEngineV4TargetRingFx FromJson(JsonValue n)
        {
            var r = new RuleEngineV4TargetRingFx();
            if (n.Kind != JsonKind.Object)
                return r;
            r.PulseHz = n["pulse_hz"].AsFloat(r.PulseHz);
            r.PulseScale = n["pulse_scale"].AsFloat(r.PulseScale);
            r.PulseMinBrightness = n["pulse_min_brightness"].AsFloat(r.PulseMinBrightness);
            r.ReleaseFadeSec = n["release_fade_sec"].AsFloat(r.ReleaseFadeSec);
            return r;
        }

        /// <summary>Gösterildikten <paramref name="shownSec"/> sonra: 0 (dip) … 1 (tepe), t = 0'da dip.</summary>
        public float PulsePhase(float shownSec) =>
            0.5f - 0.5f * (float)System.Math.Cos(TwoPi * PulseHz * shownSec);

        public void Pulse(float shownSec, out float scale, out float brightness)
        {
            float p = PulsePhase(shownSec);
            scale = 1f + PulseScale * p;
            brightness = PulseMinBrightness + (1f - PulseMinBrightness) * p;
        }

        /// <summary>Bırakıldıktan <paramref name="sinceReleaseSec"/> sonra alfa (1 → 0).</summary>
        public float ReleaseAlpha(float sinceReleaseSec)
        {
            if (ReleaseFadeSec <= 0f)
                return 0f;
            float a = 1f - sinceReleaseSec / ReleaseFadeSec;
            return a < 0f ? 0f : a > 1f ? 1f : a;
        }
    }
}
