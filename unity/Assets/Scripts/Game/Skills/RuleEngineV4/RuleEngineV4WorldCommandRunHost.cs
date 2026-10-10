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

        public void Start(CommandPlan plan, Transform target, System.Action<float> onComplete)
        {
            if (_active != null)
                _director.StopCoroutine(_active);
            _active = _director.StartCoroutine(CoRun(plan, target, onComplete));
        }

        IEnumerator CoRun(CommandPlan plan, Transform target, System.Action<float> onComplete)
        {
            float dealt = 0f;
            Transform caster = _director.MechanicsPlayer;
            OnSureCommand timing = null;
            Transform missileHit = null;
            var delivery = new RuleEngineV4DeliveryState();

            foreach (PhysicsCommand cmd in plan.Commands)
            {
                if (RuleEngineV4CommandAccess.TryOnSure(cmd, out OnSureCommand onSure))
                {
                    timing = onSure;
                    if (onSure.ChargeSec > 0f)
                        yield return CoWaitWorld(onSure.ChargeSec);
                    if (onSure.PrefireSec > 0f)
                        yield return CoWaitWorld(onSure.PrefireSec);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryMenzile(cmd, out float menzil))
                {
                    yield return CoMenzile(caster, target, menzil);
                    delivery.Refresh(_director, plan, target);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryDash(cmd, out KendiniTasiCommand dash))
                {
                    yield return CoDash(caster, target, dash);
                    delivery.Refresh(_director, plan, target);
                    continue;
                }

                if (cmd is MermiFirlatCommand missile)
                {
                    yield return CoMissile(caster, target, missile, hit =>
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
                    float scaled = _session.ApplyDiminishNonDamage(target, pushM);
                    ApplyPush(caster, target, scaled);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryPoise(cmd, out float poise))
                    RuleEngineV4WorldHost.ApplyPoise(_director, target, poise);

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
                    dealt += ApplyBounceChain(caster, target, bounce, plan, delivery);
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
                    foreach (Transform victim in delivery.ResolveDamageTargets(target))
                    {
                        if (!delivery.HitAllowed(victim, target))
                            continue;
                        float scale = timing != null ? timing.DamageScale : 1f;
                        dealt += RuleEngineV4WorldHost.ApplyDamage(
                            _director, victim, dmg * dmgMult * scale);
                    }
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryHeal(cmd, out float heal, out float healMult))
                {
                    Transform healTarget = target == null ? caster : target;
                    if (delivery.HitAllowed(healTarget, target) || healTarget == caster)
                        RuleEngineV4WorldHost.ApplyHeal(_director, healTarget, heal * healMult);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryMark(cmd, out float markLife))
                    _director._castPort?.RuleEngineV4Bridge.PlaceMark(target, markLife);
            }

            if (timing != null && timing.RecoverySec > 0f)
                yield return CoWaitWorld(timing.RecoverySec);

            _active = null;
            onComplete?.Invoke(dealt);
        }

        IEnumerator CoWaitWorld(float sec)
        {
            if (sec <= 0f)
                yield break;
            double end = WorldMs() + sec * Units.SecToMs;
            while (WorldMs() < end)
                yield return null;
        }

        IEnumerator CoMenzile(Transform caster, Transform target, float edgeReachM)
        {
            if (caster == null || target == null)
                yield break;
            float casterR = RuleEngineV4WorldPhysicsUtil.BodyRadius(caster);
            float targetR = RuleEngineV4WorldPhysicsUtil.BodyRadius(target);
            Vector3 flat = target.position - caster.position;
            flat.y = 0f;
            float centerDist = flat.magnitude;
            float meters = CastApproach.Meters(centerDist, casterR, targetR, edgeReachM, 0f);
            if (meters <= MotionDefaults.MinDashM)
                yield break;
            yield return CoLinearMove(caster, meters, RuleEngineV4WorldPhysicsDefaults.ApproachSpeedMps, "kapan", target);
        }

        IEnumerator CoDash(Transform caster, Transform target, KendiniTasiCommand dash)
        {
            if (caster == null)
                yield break;
            float maxM = dash.MaxDistanceM;
            Vector3 dir = caster.forward;
            if (target != null)
            {
                dir = target.position - caster.position;
                dir.y = 0f;
            }
            if (dir.sqrMagnitude < 0.0001f)
                dir = caster.forward;
            dir.Normalize();
            float meters = maxM;
            if (target != null)
            {
                float dist = RuleEngineV4WorldPhysicsUtil.EdgeDistance(caster.position, target);
                meters = Mathf.Min(maxM, Mathf.Max(0f, dist));
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
            MotionTemplate template = RuleEngineV4MotionBridge.BuildLinearMove(motionKind, motionKind, meters, speedMps);
            Func<MotionTarget> targetFn = () =>
            {
                if (trackTarget != null)
                {
                    Vector3 p = trackTarget.position;
                    float r = RuleEngineV4WorldPhysicsUtil.BodyRadius(trackTarget);
                    return new MotionTarget(true, p.x, p.z, r);
                }
                Vector3 fwd = fixedDir ?? caster.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.0001f)
                    fwd = Vector3.forward;
                Vector3 end = caster.position + fwd.normalized * meters;
                return new MotionTarget(true, end.x, end.z);
            };
            if (body != null)
            {
                yield return RuleEngineV4MotionBridge.CoRun(body, motor, template, targetFn, radius);
                yield break;
            }
            float incoming = motor != null ? motor.Velocity.magnitude : 0f;
            float eff = RuleEngineV4MotionHandoff.EffectiveSpeedMps(speedMps, incoming);
            float t = 0f;
            float duration = Mathf.Max(CastApproach.MinSec, meters / Mathf.Max(0.01f, eff));
            Vector3 start = caster.position;
            Vector3 delta = (fixedDir ?? (trackTarget != null
                ? (trackTarget.position - start)
                : caster.forward)).normalized * meters;
            delta.y = 0f;
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
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                dir = caster.forward;
            dir.Normalize();
            float traveled = 0f;
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
            onHit(hit ?? intended);
        }

        void ApplyPush(Transform caster, Transform target, float distanceM)
        {
            if (caster == null || target == null || distanceM <= 0f)
                return;
            RuleEngineV4WeightTier src = RuleEngineV4WorldPhysicsUtil.Weight(caster);
            RuleEngineV4WeightTier dst = RuleEngineV4WorldPhysicsUtil.Weight(target);
            if (!RuleEngineV4WeightRules.CanDisplace(src, dst))
            {
                if (RuleEngineV4WeightRules.IsImmovable(dst))
                    RuleEngineV4WorldHost.ApplyPoise(_director, target, distanceM);
                return;
            }
            Vector3 dir = target.position - caster.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                dir = caster.forward;
            dir.Normalize();
            float r = RuleEngineV4WorldPhysicsUtil.BodyRadius(target);
            Vector3 to = target.position + dir * distanceM;
            to = RuleEngineV4WorldPhysicsUtil.MoveWithWalls(target.position, to, r, target);
            target.position = to;
        }

        float ApplyBounceChain(
            Transform caster,
            Transform first,
            SekCommand bounce,
            CommandPlan plan,
            RuleEngineV4DeliveryState delivery)
        {
            float dealt = 0f;
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
            Vector3 pos = caster.position + caster.forward * 1.5f;
            pos.y = 0.25f;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "RuleEngineV4Structure";
            wall.transform.position = pos;
            wall.transform.localScale = new Vector3(1.5f, 0.5f, 0.35f);
            var col = wall.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = false;
            UnityEngine.Object.Destroy(wall, lifeSec);
        }

        double WorldMs() =>
            _director.MechanicsClock != null ? _director.MechanicsClock.Director.WorldTimeMs : 0d;
    }
}
