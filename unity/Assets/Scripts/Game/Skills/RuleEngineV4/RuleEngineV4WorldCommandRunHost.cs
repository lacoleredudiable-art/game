using System;
using System.Collections;
using System.Collections.Generic;
using Dovus.Core.Motion;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using Dovus.Game.Actors;
using Dovus.Game.Skills;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>OnSure bekler; hareket MotionTemplateRunner üzerinden.</summary>
    public sealed class RuleEngineV4WorldCommandRunHost
    {
        readonly ManifestationDirector _director;
        readonly RuleEngineV4WorldSession _session;
        Coroutine _active;

        public RuleEngineV4WorldCommandRunHost(ManifestationDirector director, RuleEngineV4WorldSession session)
        {
            _director = director;
            _session = session;
        }

        public bool IsRunning => _active != null;

        public void Start(CommandPlan plan, Transform focus, System.Action<float> onComplete)
        {
            if (_active != null)
                _director.StopCoroutine(_active);
            _active = _director.StartCoroutine(CoRun(plan, focus, onComplete));
        }

        IEnumerator CoRun(CommandPlan plan, Transform focus, System.Action<float> onComplete)
        {
            float dealt = 0;
            Transform caster = _director.MechanicsPlayer;
            OnSureCommand timing = null;
            Transform missileHit = null;
            var delivery = new RuleEngineV4DeliveryState();

            foreach (PhysicsCommand cmd in plan.Commands)
            {
                if (RuleEngineV4CommandAccess.TryOnSure(cmd, out OnSureCommand onSure))
                {
                    timing = onSure;
                    if (onSure.ChargeSec > 0)
                        yield return CoWaitWorld(onSure.ChargeSec);
                    if (onSure.PrefireSec > 0)
                        yield return CoWaitWorld(onSure.PrefireSec);
                    delivery.Refresh(_director, plan, focus);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryMenzile(cmd, out float menzil))
                {
                    yield return CoMenzile(caster, focus, menzil);
                    delivery.Refresh(_director, plan, focus);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryDash(cmd, out KendiniTasiCommand dash))
                {
                    yield return CoDash(caster, focus, dash);
                    delivery.Refresh(_director, plan, focus);
                    continue;
                }

                if (cmd is MermiFirlatCommand missile)
                {
                    yield return CoMissile(caster, focus, missile, hit =>
                    {
                        missileHit = hit;
                        delivery.MissileHit = hit;
                    });
                    continue;
                }

                if (cmd is AlanAcCommand area)
                {
                    delivery.SetAreaHits(RuleEngineV4PhysicsQueries.CollectAreaHits(
                        caster, area.RadiusM, area.MaxTargets));
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryPush(cmd, out float pushM))
                {
                    double nowMs = WorldMs();
                    float scaled = _session.ApplyDiminishNonDamage(focus, pushM, nowMs);
                    ApplyPush(caster, focus, scaled);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryPoise(cmd, out float poise))
                    RuleEngineV4WorldHost.ApplyPoise(_director, focus, poise);

                if (RuleEngineV4CommandAccess.TryGuard(cmd, out float guardSec, out float blockRatio))
                {
                    if (caster != null)
                    {
                        double ms = WorldMs();
                        _session.ArmGuard(caster, guardSec, blockRatio, ms);
                    }
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryBounce(cmd, out SekCommand bounce))
                {
                    dealt += ApplyBounceChain(caster, focus, bounce, plan, delivery);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryStructure(cmd, out float lifeSec))
                {
                    if (_session.Structures.TryPlace(0))
                        SpawnStructure(caster, lifeSec);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryDamage(cmd, out float dmg, out float dmgMult))
                {
                    foreach (Transform victim in delivery.ResolveDamageTargets(focus))
                    {
                        if (!delivery.HitAllowed(victim, focus))
                            continue;
                        float scale = timing != null ? timing.DamageScale : 1;
                        dealt += RuleEngineV4WorldHost.ApplyDamage(
                            _director, victim, dmg * dmgMult * scale);
                    }
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryHeal(cmd, out float heal, out float healMult))
                {
                    Transform healFocus = focus == null ? caster : focus;
                    if (delivery.HitAllowed(healFocus, focus) || healFocus == caster)
                        RuleEngineV4WorldHost.ApplyHeal(_director, healFocus, heal * healMult);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryMark(cmd, out float markLife))
                    _director._castPort?.RuleEngineV4Bridge.PlaceMark(focus, markLife);
            }

            if (timing != null && timing.RecoverySec > 0)
                yield return CoWaitWorld(timing.RecoverySec);

            _active = null;
            onComplete?.Invoke(dealt);
        }

        IEnumerator CoWaitWorld(float sec)
        {
            if (sec <= 0)
                yield break;
            double end = WorldMs() + sec * Units.SecToMs;
            while (WorldMs() < end)
                yield return null;
        }

        IEnumerator CoMenzile(Transform caster, Transform focus, float edgeReachM)
        {
            if (caster == null || focus == null)
                yield break;
            float casterR = RuleEngineV4WorldPhysicsUtil.BodyRadius(caster);
            float targetR = RuleEngineV4WorldPhysicsUtil.BodyRadius(focus);
            Vector3 flat = focus.position - caster.position;
            flat.y = 0;
            float centerDist = flat.magnitude;
            float meters = CastApproach.Meters(centerDist, casterR, targetR, edgeReachM, 0);
            if (meters <= MotionDefaults.MinDashM)
                yield break;
            yield return CoLinearMove(caster, meters, RuleEngineV4WorldPhysicsDefaults.ApproachSpeedMps, "kapan", focus);
        }

        IEnumerator CoDash(Transform caster, Transform focus, KendiniTasiCommand dash)
        {
            if (caster == null)
                yield break;
            float maxM = dash.MaxDistanceM;
            Vector3 dir = caster.forward;
            if (focus != null)
            {
                dir = focus.position - caster.position;
                dir.y = 0;
            }
            if (dir.sqrMagnitude < PositionOwnershipDefaults.MinDistM * PositionOwnershipDefaults.MinDistM)
                dir = caster.forward;
            dir.Normalize();
            float meters = maxM;
            if (focus != null)
            {
                float dist = RuleEngineV4WorldPhysicsUtil.EdgeDistance(caster.position, focus);
                meters = Mathf.Min(maxM, Mathf.Max(0, dist));
            }
            float speed = RuleEngineV4WorldPhysicsDefaults.DashSpeedMps;
            yield return CoLinearMove(caster, meters, speed, "dash", null, dir);
        }

        IEnumerator CoLinearMove(
            Transform caster,
            float meters,
            float speedMps,
            string motionKind,
            Transform trackTarget,
            Vector3? fixedDir = null)
        {
            MotionTemplateBodyHost body = caster.GetComponent<MotionTemplateBodyHost>();
            KinematicMotorController motor = caster.GetComponent<KinematicMotorController>();
            float radius = RuleEngineV4WorldPhysicsUtil.BodyRadius(caster);
            Vector3 start = caster.position;
            Vector3 dir = fixedDir ?? (trackTarget != null ? trackTarget.position - start : caster.forward);
            dir.y = 0;
            if (dir.sqrMagnitude < PositionOwnershipDefaults.MinDistM * PositionOwnershipDefaults.MinDistM)
                dir = Vector3.forward;
            dir.Normalize();
            meters = RuleEngineV4WorldPhysicsUtil.ClearDistance(start, dir, meters, radius, caster);
            if (meters <= 0)
                yield break;
            Vector3 end = start + dir * meters;
            MotionTemplate template = RuleEngineV4MotionBridge.BuildLinearMove(motionKind, motionKind, meters, speedMps);
            Func<MotionTarget> targetFn = () =>
            {
                if (trackTarget != null)
                {
                    Vector3 p = trackTarget.position;
                    float r = RuleEngineV4WorldPhysicsUtil.BodyRadius(trackTarget);
                    return new MotionTarget(true, p.x, p.z, r);
                }
                return new MotionTarget(true, end.x, end.z);
            };
            if (body != null)
            {
                yield return RuleEngineV4MotionBridge.CoRun(body, motor, template, targetFn, radius);
                yield break;
            }
            float incoming = motor != null ? motor.Velocity.magnitude : 0;
            float eff = RuleEngineV4MotionHandoff.EffectiveSpeedMps(speedMps, incoming);
            float t = 0;
            float duration = Mathf.Max(CastApproach.MinSec, meters / Mathf.Max(PositionOwnershipDefaults.MinDistM, eff));
            Vector3 delta = dir * meters;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                Vector3 next = start + delta * u;
                next = RuleEngineV4WorldPhysicsUtil.MoveWithWalls(start, next, radius, caster);
                caster.position = next;
                yield return null;
            }
        }

        IEnumerator CoMissile(
            Transform caster,
            Transform intended,
            MermiFirlatCommand missile,
            System.Action<Transform> onHit)
        {
            if (caster == null)
            {
                onHit(null);
                yield break;
            }
            Vector3 origin = caster.position + Vector3.up * RuleEngineV4PhysicsDefaults.ProjectileOriginHeightM;
            Vector3 dir = intended != null ? intended.position - origin : caster.forward;
            dir.y = 0;
            if (dir.sqrMagnitude < 0.0001f)
                dir = caster.forward;
            dir.Normalize();
            float traveled = 0;
            float step = missile.SpeedMps * Time.deltaTime;
            Transform hit = null;
            while (traveled < missile.RangeM && hit == null)
            {
                float seg = Mathf.Min(step, missile.RangeM - traveled);
                if (RuleEngineV4PhysicsQueries.SphereCastFirstTarget(
                        origin, dir, seg, RuleEngineV4PhysicsDefaults.ProjectileRadiusM, out Transform first))
                    hit = first;
                else
                {
                    traveled += seg;
                    origin += dir * seg;
                }
                yield return null;
            }
            onHit(hit);
        }

        void ApplyPush(Transform caster, Transform pushed, float distanceM)
        {
            if (caster == null || pushed == null || distanceM <= 0)
                return;
            RuleEngineV4WeightTier src = RuleEngineV4WorldPhysicsUtil.Weight(caster);
            RuleEngineV4WeightTier dst = RuleEngineV4WorldPhysicsUtil.Weight(pushed);
            if (!RuleEngineV4WeightRules.CanDisplace(src, dst))
            {
                if (RuleEngineV4WeightRules.IsImmovable(dst))
                    RuleEngineV4WorldHost.ApplyPoise(_director, pushed, distanceM);
                return;
            }
            Vector3 dir = pushed.position - caster.position;
            dir.y = 0;
            if (dir.sqrMagnitude < PositionOwnershipDefaults.MinDistM * PositionOwnershipDefaults.MinDistM)
                dir = caster.forward;
            dir.Normalize();
            float r = RuleEngineV4WorldPhysicsUtil.BodyRadius(pushed);
            Vector3 to = pushed.position + dir * distanceM;
            to = RuleEngineV4WorldPhysicsUtil.MoveWithWalls(pushed.position, to, r, pushed);
            RuleEngineV4PositionWriter.Commit(pushed, to);
        }

        float ApplyBounceChain(
            Transform caster,
            Transform first,
            SekCommand bounce,
            CommandPlan plan,
            RuleEngineV4DeliveryState delivery)
        {
            float dealt = 0;
            Transform current = delivery.MissileHit != null ? delivery.MissileHit : first;
            var visited = new HashSet<Transform>();
            for (int i = 0; i < bounce.BounceCount + 1; i++)
            {
                if (current == null || visited.Contains(current))
                    break;
                visited.Add(current);
                foreach (PhysicsCommand cmd in plan.Commands)
                {
                    if (!RuleEngineV4CommandAccess.TryDamage(cmd, out float dmg, out float mult))
                        continue;
                    dealt += RuleEngineV4WorldHost.ApplyDamage(_director, current, dmg * mult * bounce.BounceMult);
                }
                current = RuleEngineV4PhysicsQueries.FindBounceTarget(
                    current.position, caster.forward, bounce.SearchRadiusM, visited);
            }
            return dealt;
        }

        void SpawnStructure(Transform caster, float lifeSec)
        {
            if (caster == null)
                return;
            Vector3 pos = caster.position + caster.forward * RuleEngineV4UnitySceneDefaults.StructureForwardOffsetM;
            pos.y = RuleEngineV4UnitySceneDefaults.StructureGroundYM;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "RuleEngineV4Structure";
            wall.transform.position = pos;
            float r = RuleEngineV4WorldPhysicsRuntime.Active.BodyRadiusStructureM;
            wall.transform.localScale = new Vector3(
                r * RuleEngineV4UnitySceneDefaults.StructureScaleDepthMult,
                r,
                r * RuleEngineV4UnitySceneDefaults.StructureScaleHalfMult);
            var col = wall.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = false;
            var body = wall.AddComponent<RuleEngineV4PhysicsBodyHost>();
            body.WeightTier = RuleEngineV4WeightTier.Anchored;
            body.BodyRadiusM = r;
            _director.StartCoroutine(CoExpireStructure(wall, lifeSec));
        }

        IEnumerator CoExpireStructure(GameObject wall, float lifeSec)
        {
            yield return CoWaitWorld(lifeSec);
            if (wall != null)
                UnityEngine.Object.Destroy(wall);
            _session.Structures.Release(0);
        }

        double WorldMs() =>
            _director.MechanicsClock != null ? _director.MechanicsClock.Director.WorldTimeMs : 0d;
    }
}
