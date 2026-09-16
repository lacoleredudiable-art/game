namespace Dovus.Core.Tuning
{
    /// <summary>
    /// Skill hareket / işaret / portal. Spec sayı vermiyor — his varsayılanları;
    /// değişiklik docs/durum.md'ye yazılır.
    /// </summary>
    [System.Serializable]
    public sealed class SkillMotionTuning
    {
        public float ShortBlinkDistanceM = 4.0f;
        public float ForwardDashDistanceM = 3.5f;
        public float DashDurationSec = 0.18f;
        public float BlinkDurationSec = 0.08f;

        /// <summary>Boss bu mesafedeyse Pus → Zenitsu geçiş (enemy behavior).</summary>
        public float ZenitsuEngageRangeM = 9.0f;
        public float ZenitsuBehindOffsetM = 2.4f;
        public float ZenitsuDurationSec = 0.14f;
        public int ZenitsuIframeMs = 220;

        /// <summary>BaseDamage=0 teleport fiilinde Zenitsu kesisi: commit × bu.</summary>
        public float ZenitsuSlashCommitMult = 1.0f;

        public float MarkLifetimeSec = 12f;
        public float BridgeMaxDistanceM = 16f;
        public float BridgeDurationSec = 6f;
        public float PortalEnterRadiusM = 1.35f;
        public float PortalTraverseCooldownSec = 0.6f;
        public int MaxMarks = 4;
        public int MaxBridges = 2;

        public float ArenaHalfSizeM = 11f;

        public void CopyFrom(SkillMotionTuning other)
        {
            if (other == null) return;
            ShortBlinkDistanceM = other.ShortBlinkDistanceM;
            ForwardDashDistanceM = other.ForwardDashDistanceM;
            DashDurationSec = other.DashDurationSec;
            BlinkDurationSec = other.BlinkDurationSec;
            ZenitsuEngageRangeM = other.ZenitsuEngageRangeM;
            ZenitsuBehindOffsetM = other.ZenitsuBehindOffsetM;
            ZenitsuDurationSec = other.ZenitsuDurationSec;
            ZenitsuIframeMs = other.ZenitsuIframeMs;
            ZenitsuSlashCommitMult = other.ZenitsuSlashCommitMult;
            MarkLifetimeSec = other.MarkLifetimeSec;
            BridgeMaxDistanceM = other.BridgeMaxDistanceM;
            BridgeDurationSec = other.BridgeDurationSec;
            PortalEnterRadiusM = other.PortalEnterRadiusM;
            PortalTraverseCooldownSec = other.PortalTraverseCooldownSec;
            MaxMarks = other.MaxMarks;
            MaxBridges = other.MaxBridges;
            ArenaHalfSizeM = other.ArenaHalfSizeM;
        }

        public void ResetToDefaults() => CopyFrom(new SkillMotionTuning());
    }
}
