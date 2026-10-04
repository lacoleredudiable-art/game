using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using System;

namespace Dovus.App.Casting
{
    /// <summary>Executor alan süresi / tick — prezentasyon hitbox + engine + status fallback.</summary>
    public static class FieldTimingResolver
    {
        public static void Resolve(
            in SkillResolution skill,
            float catalogLifetimeSec,
            float catalogTickSec,
            float executorFieldTickSec,
            float bangDurationSec,
            float weaponDurationMult,
            float regenSec,
            float shieldSec,
            float rootSec,
            float hasteSec,
            float slowSec,
            out float durationSec,
            out float tickSec,
            out float perTickShare)
        {
            durationSec = 0f;
            tickSec = catalogTickSec > 0f ? catalogTickSec : executorFieldTickSec;
            perTickShare = 1f;
            if (catalogLifetimeSec > 0f)
                durationSec = catalogLifetimeSec;

            var engine = skill.Engine;
            float tickRateMult = Math.Max(CastingDefaults.MinTick01f, engine.TickRateMult(1f));
            tickSec /= tickRateMult;
            if (durationSec <= 0f && !engine.IsNull)
            {
                durationSec = Math.Max(
                    engine.ChannelSec(0f),
                    Math.Max(
                        engine.CcDurationSec(0f),
                        Math.Max(
                            engine.BuffDurationSec(0f),
                            engine.TempoDurationSec(0f))));
            }

            if (durationSec <= 0f)
            {
                durationSec = skill.VerbId switch
                {
                    "2" => regenSec,
                    "4" => shieldSec,
                    "6" => rootSec,
                    "8" => hasteSec,
                    "12" => slowSec,
                    _ => bangDurationSec
                };
            }

            durationSec = Math.Max(bangDurationSec, durationSec);
            float shareTick = Clamp(tickSec, CastingDefaults.MinTick01f, Math.Max(CastingDefaults.MinTick01f, durationSec));
            perTickShare = SustainedField.PerTickShare(durationSec, shareTick);
            durationSec *= weaponDurationMult > 0f ? weaponDurationMult : 1f;
            tickSec = Clamp(tickSec, CastingDefaults.MinTick01f, durationSec);
        }

        static float Clamp(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;
    }
}
