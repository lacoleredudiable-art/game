using Dovus.App.Casting;
using Dovus.Core.Combat;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Presentation;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Data;
using UnityEngine;

namespace Dovus.Game.Skills.Launch
{
    public sealed class HitboxSizingApplier
    {
        readonly ISkillExecutorLaunchHost _host;

        public HitboxSizingApplier(ISkillExecutorLaunchHost host) => _host = host;

        public void ApplyVerbHitboxSizing(
            SkillExecutorKind kind,
            in SkillResolution skill,
            ManifestationTuning tuning,
            float rangeMult,
            bool burst,
            ref float radius,
            ref float range,
            ref float durationSec,
            ref int spawnCount)
        {
            if (!_host.TryVerbHitbox(skill, out VerbHitboxSpec spec))
                return;
            int.TryParse(skill.AdjectiveId, out int adjectiveId);
            int weaponId = _host.EquippedWeaponNumber();
            float weaponScale = _host.VerbData?.WeaponSizeMult(weaponId, rangeMult) ?? rangeMult;
            var engine = skill.Engine;
            float tableScale = _host.VerbData?.AdjectiveSizeMult(adjectiveId) ?? 1f;
            float adjectiveScale = HitboxSizing.AdjectiveScale(tableScale, engine.HitboxScaleMult(0f));
            adjectiveScale *= _host.SlotPassives?.HitboxSizeMultFor(_host.SlotQueryCastId) ?? 1f;
            HitboxSize size = HitboxSizing.Resolve(spec, weaponScale, adjectiveScale);
            float lifetimeAdd = Mathf.Max(0f, engine.LifetimeAdd(0f));
            float slotLife = _host.SlotPassives?.LifetimeAddSecFor(_host.SlotQueryCastId) ?? 0f;
            float dashSec = _host.Combat != null ? _host.Combat.SkillMotion.DashDurationSec : 0f;
            float stateSec = engine.ReflectDurationSec(0f);
            float minionSec = engine.MinionDurationSec(0f);
            int minionCount = engine.MinionCount(1);

            ExecutorKindHitboxDims.Apply(
                kind,
                spec,
                size,
                dashSec,
                stateSec,
                minionSec,
                lifetimeAdd,
                slotLife,
                minionCount,
                ref radius,
                ref range,
                ref durationSec,
                ref spawnCount);
        }

        public void ResolveFieldTiming(
            in SkillResolution skill,
            in LivingEffectPlan plan,
            ManifestationTuning tuning,
            out float durationSec,
            out float tickSec,
            out float perTickShare)
        {
            float catalogLifetime = 0f;
            float catalogTick = 0f;
            if (_host.PresentationCatalog != null
                && _host.PresentationCatalog.TryGetHitbox(plan.HitboxId, out HitboxNode hitbox))
            {
                catalogLifetime = hitbox.GetFloat("lifetime_sec_default", 0f);
                catalogTick = hitbox.GetFloat("tick_interval_sec", 0f);
            }

            StatusTuning status = _host.Combat != null ? _host.Combat.Status : new StatusTuning();
            float weaponMult = WeaponDurationMult(skill);
            FieldTimingResolver.Resolve(
                skill,
                catalogLifetime,
                catalogTick,
                tuning.ExecutorFieldTickSec,
                tuning.BangDurationSec,
                weaponMult,
                status.RegenMs / 1000f,
                status.ShieldMs / 1000f,
                status.RootMs / 1000f,
                status.HasteMs / 1000f,
                status.SlowMs / 1000f,
                out durationSec,
                out tickSec,
                out perTickShare);
        }

        float WeaponDurationMult(in SkillResolution skill) =>
            _host is IWeaponDurationMultHost weaponHost
                ? weaponHost.WeaponDurationMult(skill)
                : 1f;
    }

    public interface IWeaponDurationMultHost
    {
        float WeaponDurationMult(in SkillResolution skill);
    }
}
