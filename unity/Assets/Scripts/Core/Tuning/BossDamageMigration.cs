namespace Dovus.Core.Tuning
{
    /// <summary>
    /// Eski tuning.json slam/nefes hasarını 0 yazıyordu (prototip sıfırlaması).
    /// Sürüm 0 kayıtta 0 görürse karadul varsayılanına çeker. Sürüm güncelse
    /// bilinçli 0'a dokunulmaz.
    /// </summary>
    public static class BossDamageMigration
    {
        public const int Version = 1;

        /// <summary>true: kayıt sürümü eskidi, diske yeniden yazılmalı.</summary>
        public static bool Apply(BossTuning boss, int storedVersion)
        {
            if (boss == null || storedVersion >= Version)
                return false;

            BossTuning defaults = new BossTuning();
            if (boss.Damage == 0)
                boss.Damage = defaults.Damage;
            if (boss.FireConeDamage == 0)
                boss.FireConeDamage = defaults.FireConeDamage;
            return true;
        }
    }
}
