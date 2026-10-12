using Dovus.Game.Config.Sections;

namespace Dovus.Game.Cameras
{
    /// <summary>Takip kamerasının temel kadrajı: uzaklık (O-5.1), eğim (O-5.2), bakış yüksekliği (O-5.3), FOV (O-5.4).</summary>
    public readonly struct CameraBaseFrame
    {
        public CameraBaseFrame(float distanceM, float pitchDeg, float lookHeightM, float fovDeg)
        {
            DistanceM = distanceM;
            PitchDeg = pitchDeg;
            LookHeightM = lookHeightM;
            FovDeg = fovDeg;
        }

        public float DistanceM { get; }
        public float PitchDeg { get; }
        public float LookHeightM { get; }
        public float FovDeg { get; }

        /// <summary>Dilim dışı (eski) yol: değerler canlı ayardan.</summary>
        public static CameraBaseFrame From(CameraSettings s) =>
            new(s.CameraDistanceM, s.CameraDefaultPitchDeg, s.CameraLookHeightM, s.CameraFovDeg);
    }
}
