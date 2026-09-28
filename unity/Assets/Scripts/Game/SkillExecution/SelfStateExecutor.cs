using Dovus.Core.Execution;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Kendine süreli durum (Yansıma): durum cast'te verilir, executor süre boyunca
    /// oyuncuyu izleyen alanı (fiil_hitbox.10 sphere, süreli) görünür tutar.
    /// </summary>
    public sealed class SelfStateExecutor : SkillExecutor
    {
        float _ageSec;
        GameObject _disk;

        public override SkillExecutorKind Kind => SkillExecutorKind.SelfState;

        public override void Execute(in SkillExecutionContext context)
        {
            base.Execute(context);
            _ageSec = 0f;
            Apply(1f);
            HitboxVfxRegistry.Create(
                context.VfxKey,
                context.HitboxShape,
                context.VfxColorHex,
                OwnerPosition(),
                context.Direction,
                context.RadiusM,
                context.RangeM,
                transform);

            _disk = PlaceholderFactory.CreateZoneDisk(
                context.ColorKey,
                OwnerPosition(),
                context.RadiusM,
                transform,
                alpha: context.Tuning.ExecutorFieldDiskAlpha);
            if (_disk != null)
                _disk.name = $"SelfState_{context.Skill.SkillId}";
            GameObject flash = PlaceholderFactory.CreateImpact("reflect", context.ColorKey, OwnerPosition() + Vector3.up, transform);
            if (flash != null)
                Destroy(flash, 0.4f);
        }

        void Update()
        {
            if (!HasContext)
                return;

            _ageSec += WorldDeltaSec;
            if (_disk != null)
            {
                Vector3 p = OwnerPosition();
                _disk.transform.position = new Vector3(p.x, _disk.transform.position.y, p.z);
            }

            if (_ageSec >= Context.DurationSec)
                Finish();
        }

        Vector3 OwnerPosition() => Context.Owner != null ? Context.Owner.position : Context.Origin;
    }
}
