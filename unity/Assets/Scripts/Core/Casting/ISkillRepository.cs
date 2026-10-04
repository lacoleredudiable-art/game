using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Casting
{
    /// <summary>element-sistemi.json skill sayıları deposu (fiil tabanı, global kurallar).</summary>
    public interface ISkillRepository
    {
        float VerbDamageReference { get; }
        float GlobalCooldownSec { get; }
        int MaxConcurrentCasts { get; }
        float MaxMana { get; }
        float ManaRegenPerSec { get; }
        float ManaRegenDelaySec { get; }
        double RootImmunityMs { get; }
        float AllySkillRangeM { get; }

        bool TryGetVerb(
            int verbId,
            out float damage,
            out float cooldownSec,
            out float manaCost,
            out float durationSec,
            out float rangeM,
            out float radiusM);

        float RadiusM(int verbId);
        float RangeM(int verbId);
        void ApplyBasicStrikeRange(ManifestationTuning tuning);
        void ApplyCcDurations(StatusTuning tuning);
    }
}
