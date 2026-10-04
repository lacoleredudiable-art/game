using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Casting
{
    /// <summary>JSON skill sayılarından canlı tuning alanlarına yazım (depo arayüzü dışında).</summary>
    public static class SkillNumberTuningApplier
    {
        public static void ApplyBasicStrikeRange(ISkillRepository repo, ManifestationTuning tuning)
        {
            if (repo == null || tuning == null)
                return;
            tuning.BasicStrikeRangeM = repo.RangeM(1);
            tuning.BasicStrikeRadiusM = repo.RadiusM(1);
        }

        public static void ApplyCcDurations(SkillNumberCatalog catalog, StatusTuning tuning)
        {
            if (catalog == null || tuning == null)
                return;
            void Set(StatusKind kind, System.Action<int> write)
            {
                if (catalog.TryGetCcDurationMs(kind, out int ms) && ms > 0)
                    write(ms);
            }

            Set(StatusKind.Stun, v => tuning.StunMs = v);
            Set(StatusKind.Root, v => tuning.RootMs = v);
            Set(StatusKind.Silence, v => tuning.SilenceMs = v);
            Set(StatusKind.Slow, v => tuning.SlowMs = v);
            Set(StatusKind.Blind, v => tuning.BlindMs = v);
            Set(StatusKind.Disarm, v => tuning.DisarmMs = v);
            Set(StatusKind.Taunt, v => tuning.TauntMs = v);
        }
    }
}
