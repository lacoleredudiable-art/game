namespace Dovus.Core.Tuning
{
    /// <summary>Prototip boss (YERE ÇAKMA) — dovus-sistemi.md §11.</summary>
    public class BossTuning
    {
        public int WindupMs = 640;
        public int ActiveMs = 90;
        public int RecoveryMs = 720;
        public float RadiusM = 5.4f;
        public int Damage = 22;
        public int IdleMinMs = 700;
        public int IdleMaxMs = 1500;
        public float ApproachSpeedMps = 2.2f;

        // Ölümden sonra tekrar dövüş — dovus-sistemi.md §11 (özet §4)
        public float RespawnMaxSec = 2.0f;
    }
}
