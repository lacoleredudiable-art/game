using Dovus.Core;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Composition;
using Dovus.Game.DevTools;
using Dovus.Game.Team;
using Dovus.Game.Vfx;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dovus.Game.Skills.Mechanics
{
    public sealed partial class MechanicWorldRuntime
    {
        readonly IMechanicWorldHost _host;
        VolumePayloadApplier _payload;
        JsonEffectRuntime _json;
        readonly List<MechanicWorldBody> _bodies = new();
        public readonly List<MechanicVolume> Volumes = new();
        public readonly List<MechanicLink> Links = new();
        readonly List<GuardTrigger> _guardTriggers = new();
        readonly GuardTriggerDelivery.Once _guardOnce = new();
        int _nextGuardId;
        readonly TimedHistory<Vector3> _bossMechanicHistory = new(5000, 50);
        HostileTargets _hostileTargets;

        static readonly StatusKind[] PositiveStatuses =
        {
            StatusKind.Shield,
            StatusKind.Haste,
            StatusKind.DamageReduction,
            StatusKind.Regen,
            StatusKind.Stealth,
            StatusKind.Stasis
        };
        static readonly StatusKind[] SlowOnly = { StatusKind.Slow };

        public MechanicWorldRuntime(IMechanicWorldHost host) => _host = host;

        internal void Wire(VolumePayloadApplier payload, JsonEffectRuntime json)
        {
            _payload = payload;
            _json = json;
        }

        public void BindHostileTargets(HostileTargets targets) => _hostileTargets = targets;

        public void PullBossToPlayerContact()
        {
            if (_host.Boss == null || _host.Player == null)
                return;
            if (!ForcedDisplacement.Allows(_host.BossStatus != null ? _host.BossStatus.Board : null))
                return;
            float playerR = Mathf.Max(0.5f, _host.PlayerBodyRadiusM());
            _host.Boss.PullToContact(_host.Player.position, playerR, _host.Boss.BodyRadiusM);
        }

        public void BeginMechanicWorld(MechanicPlan plan, Vector3 aimDir, Vector3 landedAt, double worldMs)
        {
            if (plan == null || _host.Player == null)
                return;

            MechanicWorldProfile profile = MechanicWorldProfile.From(plan);
            Vector3 center = ResolveMechanicCenter(plan, aimDir, landedAt);
            double lifeMs = Math.Max(100, plan.Body.LifeSec * 1000.0);

            if (profile.BlocksMovement)
                SpawnMechanicWall(plan, center, aimDir, worldMs + lifeMs);
            if (profile.Decoy)
                SpawnMechanicDecoy(plan, worldMs);
            if (profile.TempoField || profile.Cloud || profile.Vortex || profile.CleanseField
                || profile.Reflector || profile.ProjectileBarrier || profile.Continuous || profile.Payload)
                SpawnMechanicVolume(plan, profile, center, worldMs);
            if (profile.Link)
                SpawnMechanicLink(plan, worldMs);
            if (profile.ProjectileBarrier)
                _host.BeginProjectileErase(plan, aimDir, center);

            foreach (MechanicEffect e in plan.Effects)
            {
                if (!e.Has("koruyucu_tetik"))
                    continue;
                SpawnGuardTrigger(e, worldMs, plan.Compatible);
            }
        }

        Vector3 ResolveMechanicCenter(MechanicPlan plan, Vector3 aimDir, Vector3 landedAt)
        {
            if (plan.Body.BornAt == "sende")
                return _host.Player.position;
            if (plan.Body.BornAt == "dokunus" && _host.Ally != null)
                return _host.Ally.transform.position;
            landedAt.y = _host.Player.position.y;
            if ((landedAt - _host.Player.position).sqrMagnitude > 0.01f)
                return _host.ClampToArena(landedAt);
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 0.0001f)
                aimDir = _host.Player.forward;
            return _host.ClampToArena(_host.Player.position + aimDir.normalized * (float)plan.Body.ReachM);
        }

        void SpawnMechanicWall(MechanicPlan plan, Vector3 center, Vector3 aimDir, double untilMs)
        {
            float thickness = Mathf.Max(0.05f, (float)plan.Body.SizeM);
            float length = plan.Body.Path == "isin"
                ? Mathf.Max(thickness, (float)plan.Body.ReachM)
                : Mathf.Max(thickness, (float)plan.Body.SizeM * 2f);
            float height = Mathf.Max(thickness, (float)plan.Body.SizeM * 2f);

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = plan.Body.Path == "isin" ? "MechanicFence" : "MechanicWall";
            wall.transform.SetParent(_host.DirectorTransform, true);
            center.y = height * 0.5f;
            wall.transform.position = center;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude > 0.0001f)
                wall.transform.rotation = Quaternion.LookRotation(aimDir.normalized, Vector3.up);
            wall.transform.localScale = new Vector3(length, height, thickness);
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
                SharedTint.Apply(renderer, new Color(0.35f, 0.7f, 0.95f, 0.8f));
            _bodies.Add(new MechanicWorldBody { View = wall, UntilMs = untilMs });
            DebugConfig.DevLog($"[MechanicWorld] collider {wall.name} size={length:0.#}Ã—{height:0.#} life={(untilMs - (_host.Clock?.Director.WorldTimeMs ?? 0)) / 1000.0:0.#}sn");
        }

        void SpawnMechanicDecoy(MechanicPlan plan, double worldMs)
        {
            MechanicEffect decoy = plan.Find("yem_kopya");
            if (decoy == null)
                return;
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _host.DestroyUnityObject(body.GetComponent<Collider>());
            body.name = "MechanicPlayerDecoy";
            body.transform.SetParent(_host.DirectorTransform, true);
            body.transform.position = _host.Player.position;
            body.transform.rotation = _host.Player.rotation;
            float size = Mathf.Max(0.05f, (float)plan.Body.SizeM);
            body.transform.localScale = new Vector3(size, size * 1.8f, size);
            Renderer renderer = body.GetComponent<Renderer>();
            if (renderer != null)
                SharedTint.Apply(renderer, new Color(0.2f, 0.9f, 1f, 0.45f));
            _bodies.Add(new MechanicWorldBody
            {
                View = body,
                UntilMs = worldMs + Math.Max(100, decoy.DurationSec * 1000.0)
            });
            // dikkat_ceker: yem yaÅŸadÄ±ÄŸÄ± sÃ¼rece (decoy_life_sec) boss'un hedefi; boss vuruÅŸu onu yok eder.
            if (decoy.Has("dikkat_ceker") && _hostileTargets != null)
            {
                _hostileTargets.Register(
                    body.transform,
                    TargetKind.Decoy,
                    Mathf.Max(0.05f, size * 0.5f),
                    alive: () => body != null,
                    taunting: () => true,
                    kill: () =>
                    {
                        if (body != null)
                            _host.DestroyUnityObject(body);
                    });
                DebugConfig.DevLog($"[Mechanic] yem dikkat Ã§ekiyor: {plan.SkillId}/{plan.WeaponName} {decoy.DurationSec:0.#}sn");
            }
        }

        void SpawnMechanicVolume(
            MechanicPlan plan,
            MechanicWorldProfile profile,
            Vector3 center,
            double worldMs)
        {
            double durationSec = Math.Max(
                plan.Body.LifeSec,
                plan.Effects.Where(e =>
                        e.Has("zaman_alani") || e.Has("bulut_ici") || e.Stat == "yansit"
                        || e.Has("arinma_alani") || e.Has("aura"))
                    .Select(e => e.DurationSec)
                    .DefaultIfEmpty(0)
                    .Max());
            if (durationSec <= 0)
                durationSec = 0.1;
            if (profile.Payload)
                durationSec = Math.Max(durationSec, _host.JsonParam("payload_min_life_sec", 3.0));
            durationSec += _host.SlotPassives?.LifetimeAddSecFor(_host.SlotQueryCastId) ?? 0f;

            if (profile.Reflector)
            {
                MechanicEffect reflect = plan.Effects.FirstOrDefault(e => e.Stat == "yansit"
                    && (e.Target is "dost" or "alan" || e.Has("ayna_yuzey")));
                if (reflect != null && reflect.Target == "dost" && _host.Ally != null)
                    center = _host.Ally.transform.position;
            }

            float radius = Mathf.Max(0.05f, (float)plan.Body.SizeM);
            GameObject disk = PlaceholderFactory.CreateZoneDisk(
                _host.SelectedElementPaint?.Name ?? string.Empty,
                center,
                radius,
                _host.DirectorTransform,
                alpha: _host.Combat != null ? _host.Combat.Manifestation.ExecutorFieldDiskAlpha : 0.6f);
            if (disk != null)
                disk.name = profile.Reflector ? "MechanicReflector" : profile.Cloud ? "MechanicCloud" : "MechanicField";

            double tickRate = _host.MechanicEngine?.Rules.AdjNum(plan.Adjective, "tick_rate_mult", 1) ?? 1;
            float baseTick = _host.Combat != null ? _host.Combat.Manifestation.ExecutorFieldTickSec : 1f;
            Volumes.Add(new MechanicVolume
            {
                View = disk,
                Plan = plan,
                Profile = profile,
                Center = center,
                RadiusM = radius,
                UntilMs = worldMs + durationSec * 1000.0,
                NextTickMs = worldMs,
                TickMs = Math.Max(10, baseTick * 1000.0 / Math.Max(0.01, tickRate)),
                StartMs = worldMs,
                Skill = _host.JsonCastSkill,
                Closing = _host.JsonCastClosing,
                ArmAtMs = worldMs + JsonEffectRules.TrapArmSec(plan.Body, _host.JsonRules) * 1000.0,
                Erase = profile.ProjectileBarrier ? _host.ProjectileEraseSpec(plan) : default,
                NextEraseMs = worldMs
            });
        }

        void SpawnMechanicLink(MechanicPlan plan, double worldMs)
        {
            GameObject go = new GameObject("MechanicTether");
            go.transform.SetParent(_host.DirectorTransform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = line.endWidth = Mathf.Max(0.02f, (float)plan.Body.SizeM * 0.1f);
            line.sharedMaterial = SharedTint.ForShader("Sprites/Default");
            line.startColor = new Color(0.3f, 0.9f, 1f, 0.85f);
            line.endColor = new Color(0.9f, 0.35f, 1f, 0.85f);
            double linkTickRate = _host.MechanicEngine?.Rules.AdjNum(plan.Adjective, "tick_rate_mult", 1) ?? 1;
            double linkTickMs = Math.Max(50, (_host.Combat != null ? _host.Combat.Manifestation.ExecutorFieldTickSec : 1f) * 1000.0 / Math.Max(0.01, linkTickRate));
            Links.Add(new MechanicLink
            {
                Line = line,
                Plan = plan,
                Target = plan.Effects.Any(e => e.Target == "dusman")
                    ? (_host.Boss != null ? _host.Boss.transform : null)
                    : (_host.Ally != null ? _host.Ally.transform : _host.Player),
                UntilMs = worldMs + Math.Max(100, plan.Body.LifeSec * 1000.0),
                Skill = _host.JsonCastSkill,
                Closing = _host.JsonCastClosing,
                FlowTickMs = linkTickMs,
                NextFlowMs = worldMs + linkTickMs
            });
        }

        void SpawnGuardTrigger(MechanicEffect effect, double worldMs, bool planCompatible)
        {
            double windowSec = _host.MechanicEngine?.Rules.Param("guard_window_sec") ?? 0;
            if (windowSec <= 0)
                return;
            Vector3 at = _host.Ally != null ? _host.Ally.transform.position : _host.Player.position;
            GameObject view = PlaceholderFactory.CreateZoneDisk(
                _host.SelectedElementPaint?.Name ?? string.Empty,
                at,
                Mathf.Max(0.05f, _host.LastMechanicPlan != null ? (float)_host.LastMechanicPlan.Body.SizeM : 1f),
                _host.DirectorTransform,
                alpha: _host.Combat != null ? _host.Combat.Manifestation.ExecutorFieldDiskAlpha : 0.6f);
            if (view != null)
                view.name = "MechanicGuardTrigger";
            bool talisman = _host.EquippedProfile != null && _host.EquippedProfile.Passive.Kind == WeaponPassiveKind.KutsalEtki;
            _guardTriggers.Add(new GuardTrigger
            {
                Id = ++_nextGuardId,
                Effect = effect,
                View = view,
                UntilMs = worldMs + windowSec * 1000.0,
                NeedsHoly = talisman && !planCompatible
            });
        }

        public void Tick(double worldMs)
        {
            if (_host.Boss != null && _host.Boss.PullActive && _host.Player != null)
                _host.Boss.UpdatePullContact(_host.Player.position, Mathf.Max(0.5f, _host.PlayerBodyRadiusM()), _host.Boss.BodyRadiusM);

            if (_host.Boss != null)
                _bossMechanicHistory.Record(worldMs, _host.Boss.Home);

            for (int i = _bodies.Count - 1; i >= 0; i--)
            {
                if (worldMs < _bodies[i].UntilMs && _bodies[i].View != null)
                    continue;
                if (_bodies[i].View != null)
                    _host.DestroyUnityObject(_bodies[i].View);
                _bodies.RemoveAt(i);
            }

            for (int i = Volumes.Count - 1; i >= 0; i--)
            {
                MechanicVolume volume = Volumes[i];
                if (worldMs >= volume.UntilMs || volume.View == null)
                {
                    if (volume.View != null)
                        _host.DestroyUnityObject(volume.View);
                    Volumes.RemoveAt(i);
                    continue;
                }
                if (worldMs >= volume.NextTickMs)
                {
                    TickMechanicVolume(volume);
                    volume.NextTickMs = worldMs + volume.TickMs;
                }
            }

            TickMechanicLinks(worldMs);
            TickGuardTriggers(worldMs);
        }

        public float RedirectMechanicDamage(float incoming)
        {
            if (PortalBorderTeamHooks.TryMiss(PortalBorderTeamHooks.PlayerActorId))
                return 0f;
            if (_host.Clock == null || incoming <= 0)
                return incoming;
            if (_json.TryParry(incoming))
                return 0f;
            double now = _host.Clock.Director.WorldTimeMs;
            float remaining = incoming;
            foreach (MechanicLink link in Links)
            {
                if (now >= link.UntilMs)
                    continue;
                MechanicEffect share = link.Plan.Effects.FirstOrDefault(e => e.Stat == "hasar_paylasimi");
                if (share != null && _host.Ally != null)
                {
                    remaining = BossStatusMath.SplitShare(remaining, (float)share.Amount, out float redirected);
                    if (redirected > 0f)
                        _host.Ally.ApplyDamage(Mathf.CeilToInt(redirected));
                }
                MechanicEffect route = link.Plan.Effects.FirstOrDefault(e => e.Stat == "yonlendir");
                if (route != null && _host.BossVitals != null && !_host.BossVitals.IsDown)
                {
                    remaining = BossStatusMath.SplitShare(remaining, (float)route.Amount, out float redirected);
                    if (redirected > 0f)
                        _host.BossVitals.ApplyDamage(redirected);
                }
            }
            return remaining;
        }

        public void ReflectFromWorldVolumes(float incoming)
        {
            if (_host.Clock == null || _host.Player == null || _host.BossVitals == null || _host.BossVitals.IsDown)
                return;
            double now = _host.Clock.Director.WorldTimeMs;
            foreach (MechanicVolume volume in Volumes)
            {
                if (!volume.Profile.Reflector || now >= volume.UntilMs
                    || _host.FlatDistance(_host.Player.position, volume.Center) > volume.RadiusM)
                    continue;
                MechanicEffect reflect = volume.Plan.Effects.FirstOrDefault(e => e.Stat == "yansit");
                if (reflect == null || reflect.Amount <= 0)
                    continue;
                float ratio = JsonEffectRules.IsRampReflect(volume.Plan)
                    ? JsonEffectRules.RampedRatio((float)reflect.Amount, volume.StartMs, volume.UntilMs, now, _host.JsonParam("ramp_max", 1.5))
                    : (float)reflect.Amount;
                _json.ApplyReflectedDamage(incoming * ratio);
            }
        }

        public void RewindBoss(double seconds, double worldMs, List<string> applied)
        {
            if (_host.Boss == null || seconds <= 0
                || !_bossMechanicHistory.TryGetAtOrBefore(worldMs - seconds * 1000.0, out Vector3 past))
                return;
            // GeÃ§miÅŸ yer oyuncunun ÅŸimdiki gÃ¶vdesine denk gelebilir; boss temas dÄ±ÅŸÄ±nda kalÄ±r.
            if (_host.Player != null)
            {
                float x = past.x;
                float z = past.z;
                ActorSpacing.PushOutside(
                    ref x, ref z, _host.Player.position.x, _host.Player.position.z,
                    Mathf.Max(0.5f, _host.PlayerBodyRadiusM()) + _host.Boss.BodyRadiusM);
                past = _host.ClampToArena(new Vector3(x, past.y, z));
            }
            _host.Boss.SnapHome(past);
            bool cancelled = _host.BossDirector != null && _host.BossDirector.CancelPreparedAttack(worldMs);
            applied.Add(cancelled ? $"geri sarma {seconds:0.#}sn + cast iptal" : $"geri sarma {seconds:0.#}sn");
        }

        public void ApplyStatusTransfer(List<string> applied)
        {
            _host.LastStatusTransferMoved = 0;
            if (_host.BossStatus == null)
                return;
            StatusBoard source = _host.Ally?.Board ?? _host.PlayerStatus?.Board;
            if (source == null)
                return;
            var moved = new List<StatusKind>();
            foreach (StatusKind kind in source.ActiveKinds.ToArray())
            {
                if (!StatusKindUtil.IsDebuff(kind)
                    && !StatusKindUtil.IsHardCc(kind)
                    && !StatusKindUtil.IsSoftCc(kind))
                    continue;
                if (!source.TryGet(kind, out double remainingMs, out float magnitude, out _))
                    continue;
                _host.BossStatus.Board.Apply(kind, remainingMs, magnitude);
                moved.Add(kind);
            }
            source.RemoveKinds(moved);
            _host.LastStatusTransferMoved = moved.Count;
            if (moved.Count > 0)
                applied.Add("durum aktarma " + string.Join(",", moved));
        }

        public void PurgeBossBuffs(List<string> applied)
        {
            if (_host.BossStatus == null)
                return;
            int before = _host.BossStatus.Board.ActiveCount;
            _host.BossStatus.Board.RemoveKinds(PositiveStatuses);
            if (_host.BossStatus.Board.ActiveCount < before)
                applied.Add("buff silme");
        }

        public SkillExecutorRoute ApplyMechanicWorldRoute(MechanicPlan plan, SkillExecutorRoute route)
        {
            if (plan == null || route.Kind is not (SkillExecutorKind.MeleeHitbox or SkillExecutorKind.Projectile))
                return route;
            MechanicWorldProfile profile = MechanicWorldProfile.From(plan);
            if (plan.Body.Anchored || profile.Continuous || profile.Cloud || profile.Vortex || profile.Link)
                return new SkillExecutorRoute(
                    SkillExecutorKind.FieldAura,
                    false,
                    "mechanic_grammar yaÅŸayan alan gÃ¶vdesi");
            return route;
        }

        public int LeftoverCount() =>
            _bodies.Count + Volumes.Count + Links.Count + _guardTriggers.Count;

        public void ClearSweepState()
        {
            foreach (MechanicWorldBody body in _bodies)
            {
                if (body.View != null)
                    _host.DestroyUnityObject(body.View);
            }
            _bodies.Clear();
            foreach (MechanicVolume volume in Volumes)
            {
                if (volume.View != null)
                    _host.DestroyUnityObject(volume.View);
            }
            Volumes.Clear();
            foreach (MechanicLink link in Links)
            {
                if (link.Line != null)
                    _host.DestroyUnityObject(link.Line.gameObject);
            }
            Links.Clear();
            foreach (GuardTrigger guard in _guardTriggers)
            {
                if (guard.View != null)
                    _host.DestroyUnityObject(guard.View);
            }
            _guardTriggers.Clear();
        }
    }
}
