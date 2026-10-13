namespace Dovus.Game.Cameras
{
    /// <summary>
    /// Dilim takip kamerasının boss çevresindeki kuralları: dönüş tavanı (O-5.6), gövde kenarına yakınken yaw dondurma
    /// ve 2–5 m geçişi (O-5.7), omuz değişimi aralığı (O-5.9), boss arada kalınca model opaklığı (O-5.11).
    /// </summary>
    public readonly struct CameraSliceRules
    {
        public CameraSliceRules(float turnRateMaxDps, float bossYawFreezeEdgeM, float bossYawFreeEdgeM,
            float shoulderSwitchMinSec, float bossOccludedOpacity)
        {
            TurnRateMaxDps = turnRateMaxDps;
            BossYawFreezeEdgeM = bossYawFreezeEdgeM;
            BossYawFreeEdgeM = bossYawFreeEdgeM;
            ShoulderSwitchMinSec = shoulderSwitchMinSec;
            BossOccludedOpacity = bossOccludedOpacity;
        }

        public float TurnRateMaxDps { get; }
        public float BossYawFreezeEdgeM { get; }
        public float BossYawFreeEdgeM { get; }
        public float ShoulderSwitchMinSec { get; }
        public float BossOccludedOpacity { get; }
    }
}
