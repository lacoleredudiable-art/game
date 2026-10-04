using Dovus.Core.Equipment;

namespace Dovus.App.Casting
{
    public readonly struct CastCompatibilityPublished
    {
        public CastCompatibilityPublished(WeaponSkillCompatibility compatibility) =>
            Compatibility = compatibility;

        public WeaponSkillCompatibility Compatibility { get; }
    }
}
