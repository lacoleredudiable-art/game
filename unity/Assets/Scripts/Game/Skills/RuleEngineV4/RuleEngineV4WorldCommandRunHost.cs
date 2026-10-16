using System;
using System.Collections;
using System.Collections.Generic;
using Dovus.Core.Motion;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using Dovus.Game.Actors;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>OnSure bekler; hareket MotionTemplateRunner üzerinden.</summary>
    public sealed class RuleEngineV4WorldCommandRunHost
    {
        readonly RuleEngineV4CombatContext _combat;
        readonly RuleEngineV4PhysicsServices _physics;
        readonly RuleEngineV4DeliveryEvaluator _delivery;
        Coroutine _active;

        public RuleEngineV4WorldCommandRunHost(
            RuleEngineV4CombatContext combat,
            RuleEngineV4PhysicsServices physics,
            RuleEngineV4DeliveryEvaluator delivery)
        {
            _combat = combat;
            _physics = physics;
            _delivery = delivery;
        }

        RuleEngineV4WorldSession Session => _combat.Session;

        public bool IsRunning => _active != null;

        public void Start(
            CommandPlan plan,
            Transform focus,
            System.Action<float> onComplete,
            RuleEngineV4SurfaceAim surfaceAim = default,
            System.Action<RuleEngineV4CastOutcome> onOutcome = null)
        {
            if (_active != null)
                _combat.CoroutineHost.StopCoroutine(_active);
            _active = _combat.CoroutineHost.StartCoroutine(CoRun(plan, focus, onComplete, surfaceAim, onOutcome));
        }

        /// <summary>onOutcome: menzil sonuna kadar kimseye değmeyen mermi ıskadır (boşa gitme değil).</summary>
        IEnumerator CoRun(
            CommandPlan plan,
            Transform focus,
            System.Action<float> onComplete,
            RuleEngineV4SurfaceAim surfaceAim,
            System.Action<RuleEngineV4CastOutcome> onOutcome)
        {
            float dealt = 0;
            Transform caster = _combat.Player;
            OnSureCommand timing = null;
            Transform missileHit = null;
            bool missileFired = false;
            var delivery = new RuleEngineV4DeliveryState();

            foreach (PhysicsCommand cmd in plan.Commands)
            {
                if (RuleEngineV4CommandAccess.TryOnSure(cmd, out OnSureCommand onSure))
                {
                    timing = onSure;
                    if (onSure.PrefireSec > 0)
                        yield return CoWaitWorld(onSure.PrefireSec);
                    delivery.Refresh(_combat, plan, focus, _delivery);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryMenzile(cmd, out float menzil))
                {
                    yield return CoMenzile(caster, focus, menzil);
                    delivery.Refresh(_combat, plan, focus, _delivery);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryDash(cmd, out KendiniTasiCommand dash))
                {
                    yield return CoDash(caster, focus, dash);
                    delivery.Refresh(_combat, plan, focus, _delivery);
                    continue;
                }

                if (cmd is MermiFirlatCommand missile)
                {
                    missileFired = true;
                    yield return CoMissile(caster, focus, missile, hit =>
                    {
                        missileHit = hit;
                        delivery.MissileHit = hit;
                    });
                    continue;
                }

                if (cmd is AlanAcCommand area)
                {
                    delivery.SetAreaHits(_physics.CollectAreaHits(
                        caster, area.RadiusM, area.MaxTargets));
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryPush(cmd, out float pushM))
                {
                    double nowMs = WorldMs();
                    float scaled = Session.ApplyDiminishNonDamage(focus, pushM, nowMs);
                    ApplyPush(caster, focus, scaled);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryPoise(cmd, out float poise))
                    RuleEngineV4WorldHost.ApplyPoise(_combat, focus, poise);

                if (RuleEngineV4CommandAccess.TryGuard(cmd, out float guardSec, out float blockRatio))
                {
                    if (caster != null)
                    {
                        double ms = WorldMs();
                        Session.ArmGuard(caster, guardSec, blockRatio, ms);
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
                    if (Session.Structures.TryPlace(0))
                        SpawnStructure(caster, lifeSec);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryDamage(cmd, out float dmg, out float dmgMult))
                {
                    foreach (Transform victim in delivery.ResolveDamageTargets(focus))
                    {
                        if (!delivery.HitAllowed(victim, focus))
                            continue;
                        dealt += RuleEngineV4WorldHost.ApplyDamage(_combat, victim, dmg * dmgMult);
                    }
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryHeal(cmd, out float heal, out float healMult))
                {
                    Transform healFocus = focus == null ? caster : focus;
                    if (delivery.HitAllowed(healFocus, focus) || healFocus == caster)
                        RuleEngineV4WorldHost.ApplyHeal(_combat, healFocus, heal * healMult);
                    continue;
                }

                if (RuleEngineV4CommandAccess.TryMark(cmd, out float markLife))
                {
                    if (surfaceAim.IsValid)
                        _combat.PlaceSurfaceMark?.Invoke(surfaceAim, markLife);
                    else
                        _combat.PlaceMark?.Invoke(focus, markLife);
                }
            }

            onOutcome?.Invoke(missileFired && missileHit == null
                ? RuleEngineV4CastOutcome.Missed
                : RuleEngineV4CastOutcome.Delivered);

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
            float casterR = _physics.BodyRadius(caster);
            float targetR = _physics.BodyRadius(focus);
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
            if (dash.Backstep)
            {
                yield return CoBackstep(caster, focus, dash);
                yield break;
            }
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
                float dist = _physics.EdgeDistance(caster.position, focus);
                meters = Mathf.Min(maxM, Mathf.Max(0, dist));
            }
            float speed = RuleEngineV4WorldPhysicsDefaults.DashSpeedMps;
            yield return CoLinearMove(caster, meters, speed, "dash", null, dir);
        }

        /// <summary>Kısa geri sıçrama: odağın (yoksa bakışın) tersine, sabit mesafe, ip/mermi yok.</summary>
        IEnumerator CoBackstep(Transform caster, Transform focus, KendiniTasiCommand hop)
        {
            Vector3 away = -caster.forward;
            if (focus != null && focus != caster)
            {
                away = caster.position - focus.position;
                away.y = 0;
            }
            if (away.sqrMagnitude < PositionOwnershipDefaults.MinDistM * PositionOwnershipDefaults.MinDistM)
                away = -caster.forward;
            LastBackstepDir = away.normalized;
            yield return CoLinearMove(caster, hop.MaxDistanceM, hop.SpeedMps, "dash", null, LastBackstepDir);
        }

        /// <summary>Son geri sıçramanın yönü (test).</summary>
        internal Vector3 LastBackstepDir { get; private set; }

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
            float radius = _physics.BodyRadius(caster);
            Vector3 start = caster.position;
            Vector3 dir = fixedDir ?? (trackTarget != null ? trackTarget.position - start : caster.forward);
            dir.y = 0;
            if (dir.sqrMagnitude < PositionOwnershipDefaults.MinDistM * PositionOwnershipDefaults.MinDistM)
                dir = Vector3.forward;
            dir.Normalize();
            float requestedM = meters;
            meters = _physics.ClearDistance(start, dir, meters, radius, caster);
            if (meters <= 0)
            {
                if (motionKind == "dash")
                    RuleDrivenVfxSink.NotifyMotionEnded(caster, true);
                yield break;
            }
            Vector3 end = start + dir * meters;
            MotionTemplate template = RuleEngineV4MotionBridge.BuildLinearMove(motionKind, motionKind, meters, speedMps);
            Func<MotionTarget> targetFn = () =>
            {
                if (trackTarget != null)
                {
                    Vector3 p = trackTarget.position;
                    float r = _physics.BodyRadius(trackTarget);
                    return new MotionTarget(true, p.x, p.z, r);
                }
                return new MotionTarget(true, end.x, end.z);
            };
            if (body != null)
            {
                // Sabit varış gövde değil, süpürmenin bulduğu merkez noktası: koşucu ondan ayrıca pay bırakmaz.
                float runnerBodyM = trackTarget != null ? radius : 0;
                float runnerGapM = trackTarget != null ? _physics.EdgeGapM(caster) : 0;
                yield return RuleEngineV4MotionBridge.CoRun(body, motor, template, targetFn, runnerBodyM, runnerGapM);
                if (motionKind == "dash")
                {
                    bool atEdge = requestedM - meters > MotionDefaults.MinDashM
                        || (body.SweepRunner != null && body.SweepRunner.StoppedAtBodyEdge);
                    RuleDrivenVfxSink.NotifyMotionEnded(caster, atEdge);
                }
                yield break;
            }
            float incoming = motor != null ? motor.Velocity.magnitude : 0;
            float eff = RuleEngineV4MotionHandoff.EffectiveSpeedMps(speedMps, incoming);
            float t = 0;
            float duration = Mathf.Max(CastApproach.MinSec, meters / Mathf.Max(PositionOwnershipDefaults.MinDistM, eff));
            Vector3 delta = dir * meters;
            Vector3 last = start;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                Vector3 next = start + delta * u;
                next = _physics.MoveWithWalls(last, next, radius, caster);
                last = caster.position = next;
                yield return null;
            }
            if (motionKind == "dash")
            {
                bool atEdge = requestedM - meters > MotionDefaults.MinDashM
                    || (last - end).sqrMagnitude > PositionOwnershipDefaults.MinDistM * PositionOwnershipDefaults.MinDistM;
                RuleDrivenVfxSink.NotifyMotionEnded(caster, atEdge);
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
            Vector3 origin = caster.position + Vector3.up * RuleEngineV4UnitySceneDefaults.ProjectileOriginHeightM;
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
                if (_physics.SphereCastFirstTarget(
                        origin, dir, seg, RuleEngineV4WorldPhysicsDefaults.ProjectileRadiusM, out Transform first))
                    hit = first;
                else if (_physics.TryProjectileBlocked(
                             origin, dir, seg, RuleEngineV4WorldPhysicsDefaults.ProjectileRadiusM, caster, out _))
                    break;
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
            RuleEngineV4WeightTier src = _physics.Weight(caster);
            RuleEngineV4WeightTier dst = _physics.Weight(pushed);
            if (!RuleEngineV4WeightRules.CanDisplace(src, dst))
            {
                if (RuleEngineV4WeightRules.IsImmovable(dst))
                    RuleEngineV4WorldHost.ApplyPoise(_combat, pushed, distanceM);
                return;
            }
            Vector3 dir = pushed.position - caster.position;
            dir.y = 0;
            if (dir.sqrMagnitude < PositionOwnershipDefaults.MinDistM * PositionOwnershipDefaults.MinDistM)
                dir = caster.forward;
            dir.Normalize();
            float r = _physics.BodyRadius(pushed);
            Vector3 to = pushed.position + dir * distanceM;
            to = _physics.MoveWithWalls(pushed.position, to, r, pushed);
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
                    dealt += RuleEngineV4WorldHost.ApplyDamage(_combat, current, dmg * mult * bounce.BounceMult);
                }
                current = _physics.FindBounceTarget(
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
            _combat.CoroutineHost.StartCoroutine(CoExpireStructure(wall, lifeSec));
        }

        IEnumerator CoExpireStructure(GameObject wall, float lifeSec)
        {
            yield return CoWaitWorld(lifeSec);
            if (wall != null)
                UnityEngine.Object.Destroy(wall);
            Session.Structures.Release(0);
        }

        double WorldMs() => _combat.WorldTimeMs;
    }
}
