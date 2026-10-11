namespace Dovus.Game.Composition.Builders
{
    public static class RuleEngineV4SliceBuilderDefaults
    {
        public const float MinRadiusFloorM = 0.25f;
        public const float MinHeightFloorM = 0.5f;
        public const float Minion1X = -3f;
        public const float Minion1Z = 6f;
        public const float Minion2X = 3f;
        public const float Minion2Z = 7f;
        public const float Minion3Z = 8.5f;
        public const float Ally2SpawnX = 2.5f;
        /// <summary>Bayrak kapalı + SliceScene: eski sabit can (bayrak açıkken scale.light_creature_max_hp).</summary>
        public const int LegacyMinionMaxHp = 200;
    }
}
