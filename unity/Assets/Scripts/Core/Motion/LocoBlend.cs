using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Motion
{
    /// <summary>Kalıp hızının bacak blend'i. İleri/strafe bakışa göredir.</summary>
    public readonly struct LocoBlend
    {
        public LocoBlend(float speedMps, float speed01, float forward, float strafe)
        {
            SpeedMps = speedMps;
            Speed01 = speed01;
            Forward = forward;
            Strafe = strafe;
        }

        public float SpeedMps { get; }
        public float Speed01 { get; }
        /// <summary>1 ileri, -1 geri.</summary>
        public float Forward { get; }
        /// <summary>1 sağa, -1 sola.</summary>
        public float Strafe { get; }

        public static LocoBlend FromVelocity(
            float velX, float velZ, float faceX, float faceZ, float refMps)
        {
            float speed = MathF.Sqrt(velX * velX + velZ * velZ);
            float reference = refMps > MotionAnimDefaults.MinSpeedMps ? refMps : MotionAnimDefaults.DefaultRefMoveMps;
            float flen = MathF.Sqrt(faceX * faceX + faceZ * faceZ);
            if (flen < 0.0001f)
            {
                faceX = 0f;
                faceZ = 1f;
            }
            else
            {
                faceX /= flen;
                faceZ /= flen;
            }

            if (speed < MotionAnimDefaults.MinSpeedMps)
                return new LocoBlend(0f, 0f, 0f, 0f);

            float forward = (velX * faceX + velZ * faceZ) / speed;
            float strafe = (velX * faceZ - velZ * faceX) / speed;
            float speed01 = Math.Clamp(speed / reference, 0f, 1f);
            return new LocoBlend(
                speed,
                speed01,
                Math.Clamp(forward, -1f, 1f),
                Math.Clamp(strafe, -1f, 1f));
        }

        /// <summary>
        /// Koşu klibi en çok 2× oynar. Gövde bunu aşarsa döngü hızlanmaz;
        /// <see cref="DashFastKey"/> tutulur (gerçek dash klibi animasyon paketiyle gelir).
        /// </summary>
        public const float TemplatePlaybackCap = 2f;

        public const string DashFastKey = "dash_fast";

        public static float MatchPlayback(float worldMps, float clipRunMps)
        {
            if (clipRunMps <= MotionAnimDefaults.MinSpeedMps || worldMps <= clipRunMps)
                return 1f;
            float need = worldMps / clipRunMps;
            return MathF.Min(need, TemplatePlaybackCap);
        }

        public static bool NeedsDashPose(float worldMps, float clipRunMps)
        {
            float clip = clipRunMps > MotionAnimDefaults.MinSpeedMps ? clipRunMps : MotionAnimDefaults.DefaultClipRunMps;
            return worldMps > clip * TemplatePlaybackCap + MotionAnimDefaults.PlaybackSpeedEpsilon;
        }

        /// <summary>Koşu anahtarı 2×'i aşan gövde hızında dash pozuna döner. Saldırı anahtarı kalır.</summary>
        public static string PresentationKey(string phaseKey, float worldMps, float clipRunMps)
        {
            if (NeedsDashPose(worldMps, clipRunMps) && MotionAnimTable.IsLocomotionKey(phaseKey))
                return DashFastKey;
            return phaseKey ?? string.Empty;
        }
    }
}
