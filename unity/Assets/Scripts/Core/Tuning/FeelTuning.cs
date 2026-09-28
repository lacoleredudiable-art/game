namespace Dovus.Core.Tuning
{
    /// <summary>
    /// His katmanı ve tepki yazısı — dovus-sistemi.md §8 "Başlangıç sayıları".
    /// Bu değerler tahmindir; asıl ayar telefonda oyun içi panelden yapılacak.
    /// </summary>
    [System.Serializable]
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

        // Bossa isabet: art arda tick/çoklu vuruş hitstop'u üst üste yığmasın. Önerilen (durum.md).
        public int BossHitHitstopMinGapMs = 140;
        public float BossHitShakePx = 4f;
        // Gövde beyaz parlaması (MaterialPropertyBlock). Önerilen (durum.md).
        public int HitFlashMs = 90;
        public float HitFlashStrength = 0.85f;

        // Kapanış kamera vuruşu — SkillFeel.CameraKick'teki gömülü değerler aynen taşındı.
        public float SkillKickStrike = 3.2f;
        public float SkillShakeStrikePx = 14f;
        public float SkillKickDisrupt = 1.4f;
        public float SkillShakeDisruptPx = 22f;
        public float SkillKickControl = 0.8f;
        public float SkillShakeControlPx = 6f;
        public float SkillKickZone = 2.4f;
        public float SkillShakeZonePx = 18f;
        public float SkillKickMotion = 2.0f;
        public float SkillShakeMotionPx = 10f;
        public float SkillKickDefault = 1.6f;
        public float SkillShakeDefaultPx = 12f;
        public float SkillKickRollDeg = 1.2f;
        public float SkillKickDecay = 8f;

        // Tepki yazısı — dovus-sistemi.md §6 gösterim
        public float ReadoutSizePx = 96f;
        public float ReadoutGlow = 34f;
        public int ReadoutHoldMs = 900;
        public int ReadoutFadeMs = 500;
        public float ReadoutPunchScale = 1.45f;

        /// <summary>T10: panel "Sıfırla"/JSON yükleme — bu sınıfın TÜM alanları panelde
        /// (kamera + yazı grupları) kapsanıyor, yani tam kopya güvenli.</summary>
        public void CopyFrom(FeelTuning other)
        {
            HitstopPerfectMs = other.HitstopPerfectMs;
            HitstopPlayerHitMs = other.HitstopPlayerHitMs;
            HitstopBossHitMs = other.HitstopBossHitMs;
            ImpactFrameMs = other.ImpactFrameMs;
            PostHitSilenceMs = other.PostHitSilenceMs;

            CameraPerfectZoomKick = other.CameraPerfectZoomKick;
            CameraDodgeZoomKick = other.CameraDodgeZoomKick;
            CameraRollDeg = other.CameraRollDeg;
            ShakePerfectPx = other.ShakePerfectPx;
            ShakeHitPx = other.ShakeHitPx;
            ShakeDecay = other.ShakeDecay;

            AfterimageCount = other.AfterimageCount;
            AfterimageLifeMs = other.AfterimageLifeMs;

            BossHitHitstopMinGapMs = other.BossHitHitstopMinGapMs;
            BossHitShakePx = other.BossHitShakePx;
            HitFlashMs = other.HitFlashMs;
            HitFlashStrength = other.HitFlashStrength;

            SkillKickStrike = other.SkillKickStrike;
            SkillShakeStrikePx = other.SkillShakeStrikePx;
            SkillKickDisrupt = other.SkillKickDisrupt;
            SkillShakeDisruptPx = other.SkillShakeDisruptPx;
            SkillKickControl = other.SkillKickControl;
            SkillShakeControlPx = other.SkillShakeControlPx;
            SkillKickZone = other.SkillKickZone;
            SkillShakeZonePx = other.SkillShakeZonePx;
            SkillKickMotion = other.SkillKickMotion;
            SkillShakeMotionPx = other.SkillShakeMotionPx;
            SkillKickDefault = other.SkillKickDefault;
            SkillShakeDefaultPx = other.SkillShakeDefaultPx;
            SkillKickRollDeg = other.SkillKickRollDeg;
            SkillKickDecay = other.SkillKickDecay;

            ReadoutSizePx = other.ReadoutSizePx;
            ReadoutGlow = other.ReadoutGlow;
            ReadoutHoldMs = other.ReadoutHoldMs;
            ReadoutFadeMs = other.ReadoutFadeMs;
            ReadoutPunchScale = other.ReadoutPunchScale;
        }

        public void ResetToDefaults() => CopyFrom(new FeelTuning());
    }
}
