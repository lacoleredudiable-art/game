namespace Dovus.Core.Trial
{
    /// <summary>Oyun dünyasının her karede döngüye verdiği anlık durum.</summary>
    public readonly struct TrialTick
    {
        public TrialTick(double nowMs, float playerDistanceToBossM, bool playerDamagedBoss, bool playerDead, bool bossDead, float bossHp01)
        {
            NowMs = nowMs;
            PlayerDistanceToBossM = playerDistanceToBossM;
            PlayerDamagedBoss = playerDamagedBoss;
            PlayerDead = playerDead;
            BossDead = bossDead;
            BossHp01 = bossHp01;
        }

        public double NowMs { get; }
        public float PlayerDistanceToBossM { get; }

        /// <summary>Bu karede oyuncu boss'a vurdu (uyku bozar, O-17.1).</summary>
        public bool PlayerDamagedBoss { get; }
        public bool PlayerDead { get; }
        public bool BossDead { get; }
        public float BossHp01 { get; }
    }
}
