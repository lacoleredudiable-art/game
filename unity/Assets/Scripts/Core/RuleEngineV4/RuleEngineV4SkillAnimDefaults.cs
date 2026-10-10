namespace Dovus.Core.RuleEngineV4
{
    public static class RuleEngineV4SkillAnimDefaults
    {
        public const string FallbackAnimatorState = "CastSweep";

        /// <summary>entry_silhouette yok.</summary>
        public const int NoEntrySilhouetteCell = -1;

        /// <summary>Ejder silüet atlası harf hücreleri: "A".."H" → 0..7 (efekt-motoru §3.4).</summary>
        public const char FirstAtlasCellLetter = 'A';
        public const int SilhouetteAtlasCellCount = 8;
    }
}
