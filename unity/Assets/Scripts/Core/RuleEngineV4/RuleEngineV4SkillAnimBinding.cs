namespace Dovus.Core.RuleEngineV4
{
    public readonly struct RuleEngineV4SkillAnimBinding
    {
        public RuleEngineV4SkillAnimBinding(
            string clip,
            string fallbackAnimatorState,
            RuleEngineV4SkillAnimEvent events,
            int entrySilhouetteCell = RuleEngineV4SkillAnimDefaults.NoEntrySilhouetteCell,
            float entrySilhouetteLifeSec = 0f,
            float entrySilhouetteFadeInSec = 0f,
            float entrySilhouetteFadeOutSec = 0f)
        {
            Clip = clip ?? string.Empty;
            FallbackAnimatorState = fallbackAnimatorState ?? string.Empty;
            Events = events;
            EntrySilhouetteCell = entrySilhouetteCell;
            EntrySilhouetteLifeSec = entrySilhouetteLifeSec;
            EntrySilhouetteFadeInSec = entrySilhouetteFadeInSec;
            EntrySilhouetteFadeOutSec = entrySilhouetteFadeOutSec;
        }

        public string Clip { get; }
        public string FallbackAnimatorState { get; }
        public RuleEngineV4SkillAnimEvent Events { get; }

        /// <summary>State girişinde bir kez doğan silüet atlas hücresi (A..H → 0..7); -1 = yok.</summary>
        public int EntrySilhouetteCell { get; }
        public float EntrySilhouetteLifeSec { get; }
        /// <summary>entry_silhouette fade_in_sec; 0 = kartın varsayılan belirmesi.</summary>
        public float EntrySilhouetteFadeInSec { get; }
        /// <summary>entry_silhouette fade_out_sec; 0 = kartın varsayılan sönmesi.</summary>
        public float EntrySilhouetteFadeOutSec { get; }

        public bool HasEntrySilhouette => EntrySilhouetteCell >= 0;

        public bool IsEmpty => string.IsNullOrEmpty(Clip) && string.IsNullOrEmpty(FallbackAnimatorState);

        public static RuleEngineV4SkillAnimBinding Empty => new(string.Empty, string.Empty, RuleEngineV4SkillAnimEvent.None);
    }
}
