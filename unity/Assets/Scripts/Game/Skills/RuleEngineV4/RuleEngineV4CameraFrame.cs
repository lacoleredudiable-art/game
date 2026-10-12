using Dovus.Core.RuleEngineV4;
using Dovus.Game.Cameras;
using Dovus.Game.Config.Sections;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>
    /// O-5.1–O-5.4: dilimde takip kamerasının temel kadrajının tek kaynağı, kural-motoru-v4.json <c>slice_camera</c>.
    /// Sahnedeki ve <see cref="CameraSettings"/>'teki uzaklık, eğim, bakış yüksekliği ve FOV okunmaz; JSON'da olmayan
    /// alan [V4-FALLBACK] ile yazılır.
    /// </summary>
    public static class RuleEngineV4CameraFrame
    {
        public static CameraBaseFrame Resolve(RuleEngineV4SliceCamera json, CameraSettings legacy)
        {
            return new CameraBaseFrame(
                Pick(json.HasDistanceM, json.DistanceM, "distance_m", legacy.CameraDistanceM),
                Pick(json.HasPitchDeg, json.PitchDeg, "pitch_deg", legacy.CameraDefaultPitchDeg),
                Pick(json.HasLookHeightM, json.LookHeightM, "look_height_m", legacy.CameraLookHeightM),
                Pick(json.HasFovDeg, json.FovDeg, "fov_deg", legacy.CameraFovDeg));
        }

        static float Pick(bool has, float value, string key, float legacy)
        {
            if (has)
                return value;
            Debug.LogError(
                $"[V4-FALLBACK] RuleEngineV4CameraFrame: kural-motoru-v4.json slice_camera.{key} yok; " +
                $"CameraSettings değeri {legacy:0.00} kullanıldı");
            return legacy;
        }
    }
}
