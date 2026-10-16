namespace Dovus.Core.Trial
{
    /// <summary>O-17.3 sonuç kartı verisi: süre, atılan skill, en çok hasar veren kombo, deneme sayısı.</summary>
    public readonly struct TrialResult
    {
        public TrialResult(bool won, int attempt, double fightSec, int skillCount, string? topCombo, float topComboDamage, float bossHp01)
        {
            Won = won;
            Attempt = attempt;
            FightSec = fightSec;
            SkillCount = skillCount;
            TopCombo = topCombo;
            TopComboDamage = topComboDamage;
            BossHp01 = bossHp01;
        }

        public bool Won { get; }
        public int Attempt { get; }
        public double FightSec { get; }
        public int SkillCount { get; }
        public string? TopCombo { get; }
        public float TopComboDamage { get; }

        /// <summary>Denemenin bittiği andaki boss canı oranı (ölüm ekranında büyük yazılacak; 13 §5 [Ö]).</summary>
        public float BossHp01 { get; }
    }
}
