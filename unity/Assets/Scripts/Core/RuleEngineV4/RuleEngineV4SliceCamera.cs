using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// kural-motoru-v4.json <c>slice_camera</c>: dilim takip kamerasının uzaklık (O-5.1), eğim (O-5.2), bakış
    /// yüksekliği (O-5.3) ve FOV (O-5.4) değerlerinin tek kaynağı. Alan yoksa <c>Has*</c> false olur; okuyan taraf
    /// bunu [V4-FALLBACK] ile görünür kılar. Kod varsayılanı yoktur.
    /// </summary>
    public sealed class RuleEngineV4SliceCamera
    {
        public float DistanceM { get; private set; }
        public bool HasDistanceM { get; private set; }
        public float PitchDeg { get; private set; }
        public bool HasPitchDeg { get; private set; }
        public float LookHeightM { get; private set; }
        public bool HasLookHeightM { get; private set; }
        public float FovDeg { get; private set; }
        public bool HasFovDeg { get; private set; }

        public static RuleEngineV4SliceCamera FromJson(JsonValue root)
        {
            var c = new RuleEngineV4SliceCamera();
            JsonValue n = root["slice_camera"];
            if (n.Kind != JsonKind.Object)
                return c;
            c.HasDistanceM = Read(n["distance_m"], out float distance);
            c.DistanceM = distance;
            c.HasPitchDeg = Read(n["pitch_deg"], out float pitch);
            c.PitchDeg = pitch;
            c.HasLookHeightM = Read(n["look_height_m"], out float look);
            c.LookHeightM = look;
            c.HasFovDeg = Read(n["fov_deg"], out float fov);
            c.FovDeg = fov;
            return c;
        }

        static bool Read(JsonValue node, out float value)
        {
            bool has = node.Kind == JsonKind.Number;
            value = has ? node.AsFloat(0f) : 0f;
            return has;
        }
    }
}
