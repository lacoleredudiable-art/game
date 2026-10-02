using Dovus.Core.Tuning;

namespace Dovus.Game
{
    /// <summary>Silah arketipine göre sunum hitstop/sarsıntı — yalnız his, hasar değil.</summary>
    public static class FeelWeaponPresentation
    {
        public static int BossHitstopMs(FeelTuning feel, string archetype) => archetype switch
        {
            WeaponArchetypeMap.Hammer => feel.HitstopBossHammerMs,
            WeaponArchetypeMap.SwordShield => feel.HitstopBossSwordMs,
            WeaponArchetypeMap.Gun => feel.HitstopBossHeavyMs,
            WeaponArchetypeMap.Fist or WeaponArchetypeMap.Bow or WeaponArchetypeMap.Caster => feel.HitstopBossLightMs,
            _ => feel.HitstopBossHeavyMs
        };

        public static float BossHitShakePx(FeelTuning feel, string archetype, bool isCrit)
        {
            float px = archetype switch
            {
                WeaponArchetypeMap.Hammer or WeaponArchetypeMap.Gun => feel.ShakeBossHeavyPx,
                WeaponArchetypeMap.SwordShield => feel.ShakeBossMediumPx,
                WeaponArchetypeMap.Fist or WeaponArchetypeMap.Bow or WeaponArchetypeMap.Caster => feel.ShakeBossLightPx,
                _ => feel.ShakeBossMediumPx
            };
            return isCrit ? px * feel.BossHitCritShakeMult : px;
        }
    }
}
