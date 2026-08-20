using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// T5 prototip kabuğu ayarları. Spec'te yürüme hızı yok; varsayılanlar durum.md'de kayıtlı.
    /// </summary>
    [System.Serializable]
    public sealed class PrototypeTuning
    {
        [Header("Arena")]
        public float ArenaHalfSizeM = 12f;

        [Header("Oyuncu")]
        public float WalkSpeedMps = 4.5f;

        [Header("Sanal çubuk")]
        public float JoystickMaxRadiusDp = 72f;
        public float JoystickDeadZone = 0.12f;

        [Header("Kamera")]
        public float FollowSmoothTimeSec = 0.18f;
        public float LookAheadM = 1.4f;
        public Vector3 CameraOffset = new Vector3(0f, 7f, -6f);
    }
}
