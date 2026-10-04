using System.Collections.Generic;
using Dovus.Core.Actors;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Data;
using Dovus.Game.Team;
using UnityEngine;

namespace Dovus.Game.Skills.Closing
{
    public sealed class ClosingStatusApplier
    {
        readonly IClosingStatusHost _host;
        int _passiveBonusDepth;

        public ClosingStatusApplier(IClosingStatusHost host) => _host = host;

        public void Apply(Transform target, SkillResolution skill, bool bossReached = true)
        {
            if (skill.IsEmpty)
                return;
            ActorStatusHost bossStatus = bossReached ? _host.BossStatus : null;
            bool targetsAlly = _host.Ally != null && target == _host.Ally.transform;
            if (targetsAlly)
                _host.Ally.EnsureStatusBoard();
            StatusBoard friendlyBoard = targetsAlly
                ? _host.Ally.Board
                : _host.PlayerStatus != null ? _host.PlayerStatus.Board : null;
            if (friendlyBoard == null && bossStatus == null)
                return;

            if (_host.Ally != null)
                _host.Ally.EnsureStatusBoard();
            float friendlyScale = _host.WeaponFriendlyScale();
            var result = StatusApplicator.ApplySkill(
                skill,
                friendlyBoard,
                bossStatus != null ? bossStatus.Board : null,
                _host.Combat != null ? _host.Combat.Status : new StatusTuning(),
                _host.MobilityCc,
                friendlyScale,
                cleanseCount: _host.JsonCleanseCount(skill));
            _host.LastFriendlyWasAlly = targetsAlly;
            _host.ShareFriendlyStatuses(skill, friendlyBoard);
            _host.ApplyPurgePower(skill, result.CleansedCount);

            if (string.Equals(skill.Presentation.Action, "tempo", System.StringComparison.Ordinal))
            {
                TempoCast.From(skill).Apply(
                    _host.PlayerStatus != null ? _host.PlayerStatus.Board : friendlyBoard,
                    bossStatus != null ? bossStatus.Board : null,
                    _host.Ally != null ? _host.Ally.Board : null,
                    friendlyScale);
            }

            _host.ApplyArmorShred(skill, bossStatus);
            ApplySlotPassiveOnHit(bossStatus);
            if (bossStatus != null)
                ApplyElementStatusToBoss(bossStatus.Board);

            if (result.Knockback && bossStatus != null && _host.Player != null)
                bossStatus.ApplyKnockbackFrom(_host.Player.position);

            if (result.Pull && bossStatus != null && _host.Player != null)
                bossStatus.ApplyPullToward(_host.Player.position);
        }

        void ApplySlotPassiveOnHit(ActorStatusHost target)
        {
            if (target == null || _host.SlotPassives == null || _host.SlotPassives.ActiveCount == 0)
                return;
            StatusTuning tuning = _host.Combat != null ? _host.Combat.Status : new StatusTuning();
            int castId = _host.SlotQueryCastId;
            float rootSec = _host.SlotPassives.RootSecondsFor(castId);
            if (rootSec > 0f)
                target.Board.Apply(StatusKind.Root, rootSec * SkillsTimeDefaults.SecToMs, 1f, "passive:root");
            float speed = _host.SlotPassives.SlowSpeedFor(castId);
            if (speed < ClosingDefaults.SlowSpeedNearFullThreshold)
                target.Board.Apply(
                    StatusKind.Slow,
                    _host.MobilityCc?.ResolveCcDurationMs(StatusKind.Slow, 0, tuning.SlowMs) ?? tuning.SlowMs,
                    speed,
                    "passive:slow");
            float accuracy = _host.SlotPassives.AccuracyDebuffFor(castId);
            if (accuracy > 0f)
            {
                double blindMs = BossStatusMath.BlindDurationMs(
                    _host.MobilityCc?.ResolveCcDurationMs(StatusKind.Blind, 0, tuning.BlindMs) ?? tuning.BlindMs,
                    _host.SlotPassives.AccuracyLifetimeAddSecFor(castId));
                target.Board.Apply(
                    StatusKind.Blind,
                    blindMs,
                    BossStatusMath.BlindChanceFromAccuracy(accuracy),
                    "passive:blind");
            }
        }

        void ApplyElementStatusToBoss(StatusBoard boss)
        {
            ElementPaintNode? paint = _host.SelectedElementPaint;
            if (!paint.HasValue || boss == null)
                return;
            ElementPaintNode node = paint.Value;
            if (!ElementBossStatusRules.TryForBoss(node.Status, node.StatusEffect, node.StatusDurationSec, out ElementBossStatus apply))
                return;
            boss.Apply(apply.Kind, apply.DurationMs, apply.Magnitude, "element:" + node.Id);
        }

        public void ApplySlotPassiveHitExtras(float dealt)
        {
            if (_passiveBonusDepth > 0 || dealt <= 0f || _host.SlotPassives == null)
                return;
            _passiveBonusDepth++;
            try
            {
                ApplySlotBounce(dealt);
                StartSlotFlow(dealt);
            }
            finally
            {
                _passiveBonusDepth--;
            }
        }

        void ApplySlotBounce(float dealt)
        {
            int castId = _host.SlotQueryCastId;
            int count = _host.SlotPassives.BounceCountFor(castId);
            float mult = _host.SlotPassives.BounceDamageMultFor(castId);
            if (count <= 0 || mult <= 0f)
                return;
            int sourceId = ResolveBossTargetKey();
            var candidates = new List<PassiveBounceCandidate>();
            Vector3 from = _host.Boss != null ? _host.Boss.transform.position : (_host.Player != null ? _host.Player.position : Vector3.zero);
            IReadOnlyList<TargetableHost> bodies = _host.LiveTargetables;
            for (int i = 0; i < bodies.Count; i++)
            {
                TargetableHost body = bodies[i];
                if (body == null || !_host.IsEnemyBody(body.transform))
                    continue;
                if (_host.Boss != null && (body.transform == _host.Boss.transform || body.transform.IsChildOf(_host.Boss.transform)))
                    continue;
                candidates.Add(new PassiveBounceCandidate(
                    body.TargetKey,
                    body.DistanceFrom(from)));
            }

            List<PassiveBounceHit> hits = PassiveBounce.Plan(dealt, count, mult, sourceId, candidates);
            for (int i = 0; i < hits.Count; i++)
                ApplyPassiveBonusHit(hits[i]);
        }

        void ApplyPassiveBonusHit(PassiveBounceHit hit)
        {
            if (hit.Damage <= 0f || _host.BossVitals == null || _host.BossVitals.IsDown)
                return;
            int bossKey = ResolveBossTargetKey();
            bool bossHit = _host.Boss == null || hit.TargetId == 0 || hit.TargetId == bossKey;
            if (!bossHit)
                return;
            _host.BossVitals.ApplyDamage(hit.Damage);
            _host.DamageHud?.ShowDamage(hit.Damage, false, _host.BossHitPoint(), _host.DamageTint(), victimIsBoss: true);
        }

        int ResolveBossTargetKey()
        {
            if (_host.Boss == null)
                return 0;
            TargetableHost targetable = _host.Boss.GetComponent<TargetableHost>();
            return targetable != null
                ? targetable.TargetKey
                : ActorTargetKey.FromActorId(ActorDefaults.BossId);
        }

        void StartSlotFlow(float dealt)
        {
            int castId = _host.SlotQueryCastId;
            float channel = _host.SlotPassives.ChannelSecFor(castId);
            if (channel <= 0f)
                return;
            float rate = _host.SlotPassives.TickRateMultFor(castId);
            float baseTick = _host.Combat != null ? _host.Combat.Manifestation.ExecutorFieldTickSec : 1f;
            float fraction = PassiveFlowMath.DefaultTickFraction;
            if (_host.MechanicEngine != null)
            {
                double fromJson = _host.MechanicEngine.Rules.Param("flow_tick_fraction");
                if (fromJson > 0d)
                    fraction = (float)fromJson;
            }
            if (!PassiveFlowMath.TryPlan(channel, rate, dealt, baseTick, fraction, out PassiveFlowPlan plan))
                return;
            double now = _host.Clock != null ? _host.Clock.Director.WorldTimeMs : 0d;
            _host.PassiveFlows.Start(plan, now);
        }
    }
}
