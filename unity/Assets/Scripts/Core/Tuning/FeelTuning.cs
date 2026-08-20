namespace Dovus.Core.Tuning
{
    /// <summary>
    /// His katmanı ve tepki yazısı — dovus-sistemi.md §8 "Başlangıç sayıları".
    /// Bu değerler tahmindir; asıl ayar telefonda oyun içi panelden yapılacak.
    /// </summary>
    public class FeelTuning
    {
        public int HitstopPerfectMs = 90;
        public int HitstopPlayerHitMs = 130;
        public int HitstopBossHitMs = 70;
        public int ImpactFrameMs = 33;
        public int PostHitSilenceMs = 120;

        public float CameraPerfectZoomKick = 0.14f;
        public float CameraDodgeZoomKick = 0.08f;
        public float CameraRollDeg = 1.5f;
        public float ShakePerfectPx = 6f;
        public float ShakeHitPx = 14f;
        public float ShakeDecay = 6f;

        public int AfterimageCount = 7;
        public int AfterimageLifeMs = 320;

        // Tepki yazısı — dovus-sistemi.md §6 gösterim
        public float ReadoutSizePx = 96f;
        public float ReadoutGlow = 34f;
        public int ReadoutHoldMs = 900;
        public int ReadoutFadeMs = 500;
        public float ReadoutPunchScale = 1.45f;
    }
}
