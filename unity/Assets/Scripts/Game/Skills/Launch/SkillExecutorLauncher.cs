using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Casting;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Mechanic;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Data;
using Dovus.Game.Diagnostics;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Skills;
using Dovus.Game.Skills.Hosts;
using System;
using UnityEngine;

namespace Dovus.Game.Skills.Launch
{
    public sealed class SkillExecutorLauncher
    {
        readonly MdLaunchServicesHost _host;
        readonly HitboxSizingApplier _hitbox;

        public SkillExecutorLauncher(MdLaunchServicesHost host, HitboxSizingApplier hitbox)
        {
            _host = host;
            _hitbox = hitbox;
        }

        public bool TryLaunch(
            SkillExecutorKind kind,
            PendingClosing pending,
            SkillResolution skill,
            in SkillMotionPlan motionPlan,
            float effectMult = 1f,
            LivingEffect capturedLogic = null,
            int slotCastId = -1,
            float? activationDelayOverride = null)
        {
            LivingEffect logic = capturedLogic
                ?? (pending.View != null ? pending.View.Logic : null);
            if (_host.Player == null || logic == null)
                return false;

            _host.EnsurePresentationCatalog();
            ManifestationTuning tuning = _host.Combat != null
                ? _host.Combat.Manifestation
                : new ManifestationTuning();
            LivingEffectPlan plan = SkillWorldPlanner.Build(skill, _host.PresentationCatalog, tuning);

            float rangeMult = _host.EquippedWeapon != null ? _host.EquippedWeapon.RangeMult : 1f;
            bool burst = string.Equals(skill.Identity.Verb, "5", StringComparison.Ordinal);
            float radius = plan.BangRadiusM > 0f ? plan.BangRadiusM : tuning.TravelHitRadiusM;
            if (kind == SkillExecutorKind.MeleeHitbox && !burst)
                radius = tuning.TravelHitRadiusM;
            float range = kind == SkillExecutorKind.MeleeHitbox
                ? tuning.BasicStrikeRangeM * rangeMult
                : Mathf.Max(radius, plan.MaxRangeM * rangeMult);
            float speed = plan.SpeedMps > 0f ? plan.SpeedMps : tuning.NeedleSpeedMps;
            _hitbox.ResolveFieldTiming(skill, plan, tuning, out float durationSec, out float tickSec, out float perTickShare);
            int spawnCount = 1;
            _hitbox.ApplyVerbHitboxSizing(kind, skill, tuning, rangeMult, burst, ref radius, ref range, ref durationSec, ref spawnCount);
            string hitboxShape = _host.TryVerbHitbox(skill, out VerbHitboxSpec visualSpec)
                ? visualSpec.Shape
                : "sphere";
            float hitboxAngleDeg = hitboxShape == "cone" ? visualSpec.SizeB : 0f;
            int elementId = _host.SelectedElementPaint?.Id ?? 1;
            int.TryParse(skill.Identity.Verb, out int verbVfxId);
            int.TryParse(skill.Identity.Adjective, out int adjectiveVfxId);
            string vfxKey = _host.VerbData?.VfxKey(
                _host.SelectedElementPaint?.Name ?? elementId.ToString(),
                verbVfxId,
                adjectiveVfxId) ?? string.Empty;
            string vfxColorHex = _host.SelectedElementPaint?.ColorHex ?? string.Empty;
            if (_host.VerbData != null
                && _host.VerbData.TryGetElementColor(elementId, out ElementVfxColor vfxColor)
                && !string.IsNullOrEmpty(vfxColor.Primary))
                vfxColorHex = vfxColor.Primary;

            Vector3 origin = _host.Player.position;
            Vector3 direction = new(logic.DirX, 0f, logic.DirZ);
            Transform target = pending.Target;
            Transform boss = _host.BossTransform;
            if (target == null && pending.AimMode == SkillAimMode.Targeted && boss != null)
                target = boss;
            if (kind == SkillExecutorKind.Summon && boss != null && !_host.IsEnemyBody(target))
                target = boss;
            if (pending.AimMode == SkillAimMode.Targeted && target != null && target != _host.Player)
            {
                Vector3 toTarget = target.position - origin;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                    direction = toTarget.normalized;
            }
            bool friendly = _host.IsFriendlyFieldVerb(skill) || kind == SkillExecutorKind.SelfState;
            Vector3 fieldCenter = pending.AimMode == SkillAimMode.Targeted && target != null
                ? target.position
                : friendly || hitboxShape == "cone"
                    ? origin
                    : new Vector3(logic.TipX, origin.y, logic.TipZ);
            float slashCommitMult = motionPlan.SlashCommitMult;
            float executorChainBonus = _host.ClosingChainBonus;
            string colorKey = _host.SelectedElementPaint?.Name
                ?? (pending.Words != null && pending.Words.Count > 0
                    ? RuneInfo.LegacySerializationName(pending.Words[0].Rune)
                    : string.Empty);

            MechanicPlan mechanicPlan = _host.MechanicPlanFor(skill);
            MechanicWorldProfile worldProfile = mechanicPlan != null
                ? MechanicWorldProfile.From(mechanicPlan)
                : null;
            if (kind == SkillExecutorKind.Summon && mechanicPlan != null)
            {
                MechanicEffect actorEffect = mechanicPlan.Effects.Find(
                    e => e.Stat is "aktor_yarat" or "klon");
                if (actorEffect != null && actorEffect.Amount > 0)
                    spawnCount = Mathf.Max(spawnCount, Mathf.RoundToInt((float)actorEffect.Amount));
            }
            float activationDelaySec = 0f;
            if (activationDelayOverride.HasValue)
                activationDelaySec = Mathf.Max(0f, activationDelayOverride.Value);
            else
            {
                if (worldProfile != null && worldProfile.RiseDelay && _host.MechanicEngine != null)
                    activationDelaySec = Mathf.Max(
                        activationDelaySec,
                        (float)_host.MechanicEngine.Rules.Param("rise_delay_sec"));
                if (worldProfile != null && worldProfile.DelayedMark && _host.MechanicEngine != null)
                    activationDelaySec = Mathf.Max(
                        activationDelaySec,
                        (float)_host.MechanicEngine.Rules.Param("mark_delay_sec"));
            }
            float tickEffectFraction = worldProfile != null && worldProfile.Continuous && _host.MechanicEngine != null
                ? (float)_host.MechanicEngine.Rules.Param("flow_tick_fraction")
                : 0f;
            if (tickEffectFraction <= 0f)
                tickEffectFraction = perTickShare;
            bool arcAllies = _host.HitMods(skill, false, false).ArcAllies;
            bool statusesApplied = false;
            float accumulatedHealScale = 0f;
            int appliedHealAmount = 0;
            int castId = slotCastId >= 0 ? slotCastId : _host.SlotQueryCastId;
            void ApplyExecutorEffect(float effectFraction)
            {
                if (effectFraction <= 0f)
                    return;
                int prevCast = _host.SlotQueryCastId;
                bool prevRecoil = _host.CasterRecoilSuppressed;
                _host.SlotQueryCastId = castId;
                _host.CasterRecoilSuppressed |= kind == SkillExecutorKind.Summon;
                try
                {
                    if (!friendly)
                        _host.ApplyBossClosing(logic, pending.Closing, skill);
                    float hitDamage = _host.ApplyClosingDamage(
                        pending.Closing,
                        skill,
                        isBasicStrike: false,
                        slashCommitMult,
                        effectFraction * effectMult,
                        executorChainBonus);
                    if (!statusesApplied)
                    {
                        bool bossReached = !friendly
                            || _host.BossWithin(_host.Player != null ? _host.Player.position : origin, radius);
                        if (worldProfile == null || !worldProfile.GuardTrigger)
                        {
                            _host.ApplyClosingStatuses(pending, skill, bossReached);
                            if (bossReached)
                                _host.ApplyMechanicHitEffects(mechanicPlan, fieldCenter);
                        }
                        statusesApplied = true;
                    }
                    if (_host.IsHealSkill(skill) && (worldProfile == null || !worldProfile.GuardTrigger))
                    {
                        accumulatedHealScale = Mathf.Min(1f, accumulatedHealScale + effectFraction);
                        int targetTotal = _host.CalculateClosingHealAmount(
                            pending.Closing,
                            skill,
                            accumulatedHealScale,
                            executorChainBonus);
                        int delta = Mathf.Max(0, targetTotal - appliedHealAmount);
                        if (delta > 0)
                        {
                            Vector3? healCenter = kind == SkillExecutorKind.FieldAura && friendly
                                ? fieldCenter
                                : null;
                            _host.ApplyClosingHealAmount(skill, delta, healCenter, radius, target);
                            appliedHealAmount += delta;
                        }
                    }
                    _host.LastSkillEffectApplied = hitDamage > 0f
                        || _host.IsHealSkill(skill)
                        || skill.Mechanics.Length > 0;
                }
                finally
                {
                    _host.SlotQueryCastId = prevCast;
                    _host.CasterRecoilSuppressed = prevRecoil;
                }
            }

            _host.ApplyWeaponDelivery(
                skill, kind, target,
                ref origin, ref range, ref radius, ref hitboxShape, ref hitboxAngleDeg, ref speed);
            var context = new SkillExecutionContext(
                skill,
                _host.Player,
                target,
                pending.AimMode,
                origin,
                direction,
                tuning.BangDurationSec,
                tuning.ExecutorMeleeWindowOpen01,
                tuning.ExecutorMeleeWindowClose01,
                radius,
                range,
                speed,
                durationSec,
                tickSec,
                burst,
                friendly,
                colorKey,
                hitboxShape,
                hitboxAngleDeg,
                vfxKey,
                vfxColorHex,
                ApplyExecutorEffect,
                _host.Clock,
                tuning,
                fieldCenter,
                applyFlatDamage: kind == SkillExecutorKind.Summon
                    ? raw =>
                    {
                        int prevCast = _host.SlotQueryCastId;
                        _host.SlotQueryCastId = castId;
                        try
                        {
                            float bindingDamage = _host.MechanicEngine != null
                                ? (float)_host.MechanicEngine.Rules.Param("minion_hit_damage")
                                : raw;
                            float dealt = _host.ApplyMinionHit(skill, bindingDamage * effectMult);
                            bool drains = mechanicPlan != null && mechanicPlan.Effects.Exists(
                                e => e.Has("can_emen"));
                            if (drains && dealt > 0.5f && _host.Player != null)
                            {
                                PlayerVitalsHost vitals = _host.CachedPlayerVitals();
                                if (vitals != null)
                                    vitals.ApplyHeal(Mathf.RoundToInt(dealt));
                            }
                        }
                        finally
                        {
                            _host.SlotQueryCastId = prevCast;
                        }
                    }
                    : null,
                spawnCount: spawnCount,
                mechanicPlan: mechanicPlan,
                activationDelaySec: activationDelaySec,
                tickEffectFraction: tickEffectFraction,
                arcAllies: arcAllies,
                placeholders: _host.SceneRuntime?.Placeholders,
                hitboxVfx: _host.SceneRuntime?.HitboxVfx);

            var go = new GameObject($"{kind}_{skill.Identity.Id}");
            go.transform.SetParent(_host.DirectorTransform, false);
            ISkillExecutor executor = kind switch
            {
                SkillExecutorKind.Summon => _host.AddSummonExecutor(go),
                _ => null
            };
            if (executor == null)
            {
                _host.DestroyUnityObject(go);
                return false;
            }

            if (kind == SkillExecutorKind.Summon)
                _host.ApplySpawnIFrame(skill);
            executor.Execute(context);
            DebugConfig.DevLog($"[SkillExecutor] {skill.Identity.Id} → {kind} r={radius:0.##} menzil={range:0.##} süre={durationSec:0.##} x{effectMult:0.##}");
            return true;
        }
    }
}
