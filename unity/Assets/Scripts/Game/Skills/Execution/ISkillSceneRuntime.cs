using Dovus.Game.Vfx;

namespace Dovus.Game.Skills.Execution
{
    /// <summary>Manifestation / executor yollarına enjekte edilen sahne VFX servisleri.</summary>
    public interface ISkillSceneRuntime
    {
        PlaceholderFactory Placeholders { get; }
        HitboxVfxRegistry HitboxVfx { get; }
        HitImpactFxRuntime HitImpact { get; }
    }
}
