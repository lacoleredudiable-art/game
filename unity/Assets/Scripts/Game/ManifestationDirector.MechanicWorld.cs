using System;
using System.Collections.Generic;
using System.Linq;
using Dovus.Core;
using Dovus.Core.Combat;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Status;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// mechanic_grammar'ın dünyada yaşayan adaptörü. Etiket okumaz; gövde ve atom profilini
    /// collider, aktör, alan, bağ, yansıtıcı ve geri-sarma davranışlarına çevirir.
    /// </summary>
    public sealed partial class ManifestationDirector
    {
        sealed class MechanicWorldBody
        {
            public GameObject View;
            public double UntilMs;
        }

        sealed class MechanicVolume
        {
            public GameObject View;
            public MechanicPlan Plan;
            public MechanicWorldProfile Profile;
            public Vector3 Center;
            public float RadiusM;
            public double UntilMs;
            public double NextTickMs;
            public double TickMs;
        }

        sealed class MechanicLink
        {
            public LineRenderer Line;
            public MechanicPlan Plan;
            public Transform Target;
            public double UntilMs;
        }

        sealed class GuardTrigger
        {
            public MechanicEffect Effect;
            public GameObject View;
            public double UntilMs;
        }

        readonly List<MechanicWorldBody> _mechanicBodies = new();
        readonly List<MechanicVolume> _mechanicVolumes = new();
        readonly List<MechanicLink> _mechanicLinks = new();
        readonly List<GuardTrigger> _guardTriggers = new();
        readonly TimedHistory<Vector3> _bossMechanicHistory = new(5000, 50);

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

        void PullBossToPlayerContact()
        {
            if (_boss == null || _player == null)
                return;
            if (!ForcedDisplacement.Allows(_bossStatus != null ? _bossStatus.Board : null))
                return;
            float playerR = Mathf.Max(0.5f, PlayerBodyRadiusM());
            _boss.PullToContact(_player.position, playerR, _boss.BodyRadiusM);
        }

        void BeginMechanicWorld(MechanicPlan plan, Vector3 aimDir, Vector3 landedAt, double worldMs)
        {
            if (plan == null || _player == null)
                return;

            MechanicWorldProfile profile = MechanicWorldProfile.From(plan);
            Vector3 center = ResolveMechanicCenter(plan, aimDir, landedAt);
            double lifeMs = Math.Max(100, plan.Body.LifeSec * 1000.0);

            if (profile.BlocksMovement)
                SpawnMechanicWall(plan, center, aimDir, worldMs + lifeMs);
            if (profile.Decoy)
                SpawnMechanicDecoy(plan, worldMs);
            if (profile.TempoField || profile.Cloud || profile.Vortex || profile.CleanseField
                || profile.Reflector || profile.ProjectileBarrier || profile.Continuous)
                SpawnMechanicVolume(plan, profile, center, worldMs);
            if (profile.Link)
                SpawnMechanicLink(plan, worldMs);

            foreach (MechanicEffect e in plan.Effects)
            {
                if (!e.Has("koruyucu_tetik"))
                    continue;
                SpawnGuardTrigger(e, worldMs);
            }
        }

        Vector3 ResolveMechanicCenter(MechanicPlan plan, Vector3 aimDir, Vector3 landedAt)
        {
            if (plan.Body.BornAt == "sende")
                return _player.position;
            if (plan.Body.BornAt == "dokunus" && _ally != null)
                return _ally.transform.position;
            landedAt.y = _player.position.y;
            if ((landedAt - _player.position).sqrMagnitude > 0.01f)
                return ClampToArena(landedAt);
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 0.0001f)
                aimDir = _player.forward;
            return ClampToArena(_player.position + aimDir.normalized * (float)plan.Body.ReachM);
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
            wall.transform.SetParent(transform, true);
            center.y = height * 0.5f;
            wall.transform.position = center;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude > 0.0001f)
                wall.transform.rotation = Quaternion.LookRotation(aimDir.normalized, Vector3.up);
            wall.transform.localScale = new Vector3(length, height, thickness);
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.35f, 0.7f, 0.95f, 0.8f);
            _mechanicBodies.Add(new MechanicWorldBody { View = wall, UntilMs = untilMs });
            Debug.Log($"[MechanicWorld] collider {wall.name} size={length:0.#}×{height:0.#} life={(untilMs - (_clock?.Director.WorldTimeMs ?? 0)) / 1000.0:0.#}sn");
        }

        void SpawnMechanicDecoy(MechanicPlan plan, double worldMs)
        {
            MechanicEffect decoy = plan.Find("yem_kopya");
            if (decoy == null)
                return;
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.name = "MechanicPlayerDecoy";
            body.transform.SetParent(transform, true);
            body.transform.position = _player.position;
            body.transform.rotation = _player.rotation;
            float size = Mathf.Max(0.05f, (float)plan.Body.SizeM);
            body.transform.localScale = new Vector3(size, size * 1.8f, size);
            Renderer renderer = body.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.2f, 0.9f, 1f, 0.45f);
            _mechanicBodies.Add(new MechanicWorldBody
            {
                View = body,
                UntilMs = worldMs + Math.Max(100, decoy.DurationSec * 1000.0)
            });
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

            if (profile.Reflector)
            {
                MechanicEffect reflect = plan.Effects.First(e => e.Stat == "yansit" && e.Target is "dost" or "alan");
                if (reflect.Target == "dost" && _ally != null)
                    center = _ally.transform.position;
            }

            float radius = Mathf.Max(0.05f, (float)plan.Body.SizeM);
            GameObject disk = PlaceholderFactory.CreateZoneDisk(
                SelectedElementPaint?.Name ?? string.Empty,
                center,
                radius,
                transform,
                alpha: _combat != null ? _combat.Manifestation.ExecutorFieldDiskAlpha : 0.6f);
            if (disk != null)
                disk.name = profile.Reflector ? "MechanicReflector" : profile.Cloud ? "MechanicCloud" : "MechanicField";

            double tickRate = MechanicEngine?.Rules.AdjNum(plan.Adjective, "tick_rate_mult", 1) ?? 1;
            float baseTick = _combat != null ? _combat.Manifestation.ExecutorFieldTickSec : 1f;
            _mechanicVolumes.Add(new MechanicVolume
            {
                View = disk,
                Plan = plan,
                Profile = profile,
                Center = center,
                RadiusM = radius,
                UntilMs = worldMs + durationSec * 1000.0,
                NextTickMs = worldMs,
                TickMs = Math.Max(10, baseTick * 1000.0 / Math.Max(0.01, tickRate))
            });
        }

        void SpawnMechanicLink(MechanicPlan plan, double worldMs)
        {
            GameObject go = new GameObject("MechanicTether");
            go.transform.SetParent(transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = line.endWidth = Mathf.Max(0.02f, (float)plan.Body.SizeM * 0.1f);
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(0.3f, 0.9f, 1f, 0.85f);
            line.endColor = new Color(0.9f, 0.35f, 1f, 0.85f);
            _mechanicLinks.Add(new MechanicLink
            {
                Line = line,
                Plan = plan,
                Target = plan.Effects.Any(e => e.Target == "dusman")
                    ? (_boss != null ? _boss.transform : null)
                    : (_ally != null ? _ally.transform : _player),
                UntilMs = worldMs + Math.Max(100, plan.Body.LifeSec * 1000.0)
            });
        }

        void SpawnGuardTrigger(MechanicEffect effect, double worldMs)
        {
            double windowSec = MechanicEngine?.Rules.Param("guard_window_sec") ?? 0;
            if (windowSec <= 0)
                return;
            Vector3 at = _ally != null ? _ally.transform.position : _player.position;
            GameObject view = PlaceholderFactory.CreateZoneDisk(
                SelectedElementPaint?.Name ?? string.Empty,
                at,
                Mathf.Max(0.05f, LastMechanicPlan != null ? (float)LastMechanicPlan.Body.SizeM : 1f),
                transform,
                alpha: _combat != null ? _combat.Manifestation.ExecutorFieldDiskAlpha : 0.6f);
            if (view != null)
                view.name = "MechanicGuardTrigger";
            _guardTriggers.Add(new GuardTrigger
            {
                Effect = effect,
                View = view,
                UntilMs = worldMs + windowSec * 1000.0
            });
        }

        void TickMechanicWorld(double worldMs)
        {
            if (_boss != null)
                _bossMechanicHistory.Record(worldMs, _boss.Home);

            for (int i = _mechanicBodies.Count - 1; i >= 0; i--)
            {
                if (worldMs < _mechanicBodies[i].UntilMs && _mechanicBodies[i].View != null)
                    continue;
                if (_mechanicBodies[i].View != null)
                    Destroy(_mechanicBodies[i].View);
                _mechanicBodies.RemoveAt(i);
            }

            for (int i = _mechanicVolumes.Count - 1; i >= 0; i--)
            {
                MechanicVolume volume = _mechanicVolumes[i];
                if (worldMs >= volume.UntilMs || volume.View == null)
                {
                    if (volume.View != null)
                        Destroy(volume.View);
                    _mechanicVolumes.RemoveAt(i);
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

        void TickMechanicVolume(MechanicVolume volume)
        {
            bool bossInside = _boss != null && FlatDistance(_boss.transform.position, volume.Center) <= volume.RadiusM;
            bool playerInside = _player != null && FlatDistance(_player.position, volume.Center) <= volume.RadiusM;
            bool allyInside = _ally != null && FlatDistance(_ally.transform.position, volume.Center) <= volume.RadiusM;
            double refreshMs = volume.TickMs * 2.1;
            string volumeId = volume.Plan != null && !string.IsNullOrEmpty(volume.Plan.SkillId)
                ? volume.Plan.SkillId
                : "volume";

            MechanicEffect flickerTempo = volume.Plan.Effects.FirstOrDefault(
                e => e.Stat == "tempo" && e.Has("titrer") && e.Target == "dusman");
            if (flickerTempo != null && bossInside)
            {
                double flickerSec = MechanicEngine?.Rules.Param("flicker_sec") ?? 0;
                bool on = flickerSec <= 0
                    || ((long)(volume.NextTickMs / (flickerSec * 1000.0)) & 1) == 0;
                if (on && flickerTempo.Amount < 1)
                    _bossStatus?.Board.Apply(StatusKind.Slow, refreshMs, (float)flickerTempo.Amount, "flicker:" + volumeId);
                else
                    _bossStatus?.Board.RemoveKinds(SlowOnly);
            }

            if (volume.Profile.TempoField)
            {
                foreach (MechanicEffect e in volume.Plan.Effects.Where(e => e.Stat == "tempo" && e.Has("zaman_alani")))
                {
                    if (e.Target == "dusman" && bossInside && e.Amount < 1)
                        _bossStatus?.Board.Apply(StatusKind.Slow, refreshMs, (float)e.Amount, "field:" + volumeId);
                    if ((e.Target == "dost" || e.Target == "kendin") && e.Amount > 1)
                    {
                        if (playerInside)
                            _playerStatus?.Board.Apply(StatusKind.Haste, refreshMs, (float)e.Amount, "field:" + volumeId);
                        if (allyInside)
                        {
                            _ally.EnsureStatusBoard();
                            _ally.Board.Apply(StatusKind.Haste, refreshMs, (float)e.Amount, "field:" + volumeId);
                        }
                    }
                }
            }

            if (volume.Profile.Cloud)
            {
                if (playerInside)
                    _playerStatus?.Board.Apply(StatusKind.Stealth, refreshMs, 1f);
                if (allyInside)
                {
                    _ally.EnsureStatusBoard();
                    _ally.Board.Apply(StatusKind.Stealth, refreshMs, 1f);
                }
                if (bossInside)
                {
                    MechanicEffect blind = volume.Plan.Effects.FirstOrDefault(e => e.Stat == "kor");
                    _bossStatus?.Board.Apply(StatusKind.Blind, refreshMs, (float)Math.Max(0, blind?.Amount ?? 0));
                }
            }

            if (volume.Profile.Vortex && _boss != null && _player != null
                && ForcedDisplacement.Allows(_bossStatus != null ? _bossStatus.Board : null))
                PullBossToPlayerContact();
            if (volume.Profile.Continuous && bossInside)
            {
                if (volume.Plan.Effects.Any(e => e.Has("akinti")))
                    _bossStatus?.ApplyKnockbackFrom(_player != null ? _player.position : volume.Center);
                MechanicEffect mire = volume.Plan.Effects.FirstOrDefault(e => e.Has("bataklik"));
                if (mire != null)
                {
                    if (mire.Amount <= 0)
                        _bossStatus?.Board.Apply(
                            StatusKind.Root, refreshMs, 1f, "mire:" + (volume.Plan != null ? volume.Plan.SkillId : "volume"));
                    else if (mire.Amount < 1)
                        _bossStatus?.Board.Apply(StatusKind.Slow, refreshMs, (float)mire.Amount, "mire:" + volumeId);
                }
            }
            if (volume.Profile.CleanseField)
            {
                if (playerInside)
                    _playerStatus?.Board.CleanseHostile();
                if (allyInside)
                {
                    _ally.EnsureStatusBoard();
                    _ally.Board.CleanseHostile();
                }
            }
        }

        void TickMechanicLinks(double worldMs)
        {
            for (int i = _mechanicLinks.Count - 1; i >= 0; i--)
            {
                MechanicLink link = _mechanicLinks[i];
                if (worldMs >= link.UntilMs || link.Line == null || _player == null || link.Target == null)
                {
                    if (link.Line != null)
                        Destroy(link.Line.gameObject);
                    _mechanicLinks.RemoveAt(i);
                    continue;
                }
                link.Line.SetPosition(0, _player.position + Vector3.up);
                link.Line.SetPosition(1, link.Target.position + Vector3.up);
                if (_boss != null && link.Target == _boss.transform)
                {
                    float distance = FlatDistance(_player.position, _boss.Home);
                    float maxLength = Mathf.Max(0f, (float)link.Plan.Body.ReachM);
                    if (maxLength > 0f && distance > maxLength
                        && ForcedDisplacement.Allows(_bossStatus != null ? _bossStatus.Board : null))
                        _boss.MoveHomeToward(_player.position, distance - maxLength);
                }
                string linkId = link.Plan != null && !string.IsNullOrEmpty(link.Plan.SkillId)
                    ? link.Plan.SkillId
                    : "link";
                foreach (MechanicEffect e in link.Plan.Effects)
                {
                    if (e.Stat == "durum_sil" && e.Has("bag_bagisiklik"))
                    {
                        _playerStatus?.Board.CleanseHostile();
                        if (_ally != null)
                        {
                            _ally.EnsureStatusBoard();
                            _ally.Board.CleanseHostile();
                        }
                        continue;
                    }
                    if (e.Target != "dusman" || e.Atom != "hiz")
                        continue;
                    if (e.Stat == "tempo" && e.Has("senkron"))
                    {
                        SkillResolution linked = SkillFromPlan(link.Plan);
                        if (CardEffectRules.WantsSelfHaste(linked.SkillJob))
                            continue;
                        TempoSyncRules.Read(e.DurationSec, (float)e.Amount, out double syncMs, out float syncStrength);
                        if (_playerStatus != null && _playerStatus.EffectiveBlocksMovement)
                        {
                            _bossStatus?.Board.Apply(
                                StatusKind.Root, syncMs, 1f,
                                "link-tempo:" + (link.Plan != null ? link.Plan.SkillId : "link"));
                        }
                        else if (_playerStatus != null && _playerStatus.EffectiveMoveSpeedMult < 1f)
                            _bossStatus?.Board.Apply(StatusKind.Slow, syncMs, syncStrength, "link:" + linkId);
                        continue;
                    }
                    SkillResolution linkedLock = SkillFromPlan(link.Plan);
                    if (CardEffectRules.WantsSelfHaste(linkedLock.SkillJob)
                        && !CardEffectRules.Names(linkedLock.SkillJob, "root"))
                        continue;
                    double refresh = Math.Max(100, e.DurationSec * 1000.0);
                    if (e.Amount <= 0)
                        _bossStatus?.Board.Apply(
                            StatusKind.Root, refresh, 1f,
                            "link:" + (link.Plan != null ? link.Plan.SkillId : "link"));
                    else if (e.Amount < 1)
                        _bossStatus?.Board.Apply(StatusKind.Slow, refresh, (float)e.Amount, "link:" + linkId);
                }
            }
        }

        SkillResolution SkillFromPlan(MechanicPlan plan)
        {
            if (_skills == null || plan == null || plan.Verb <= 0 || plan.Adjective <= 0)
                return SkillResolution.Empty;
            return _skills.Resolve(new[] { plan.Verb, plan.Adjective });
        }

        void TickGuardTriggers(double worldMs)
        {
            double threshold = MechanicEngine?.Rules.Param("guard_threshold") ?? 0;
            PlayerVitals playerVitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
            for (int i = _guardTriggers.Count - 1; i >= 0; i--)
            {
                GuardTrigger guard = _guardTriggers[i];
                bool expired = worldMs >= guard.UntilMs;
                bool allyLow = _ally != null && _ally.Ratio <= threshold;
                bool playerLow = playerVitals != null && playerVitals.MaxHp > 0
                    && (float)playerVitals.Hp / playerVitals.MaxHp <= threshold;
                if (!expired && !allyLow && !playerLow)
                    continue;

                if (!expired)
                {
                    int amount = Mathf.Max(0, Mathf.RoundToInt((float)Math.Abs(guard.Effect.Amount)));
                    if (guard.Effect.Stat == "can")
                    {
                        if (allyLow)
                            _ally?.ApplyHeal(amount);
                        else
                            playerVitals?.ApplyHeal(amount);
                    }
                    else if (guard.Effect.Stat == "kalkan")
                    {
                        StatusBoard board = allyLow ? _ally?.Board : _playerStatus?.Board;
                        board?.Apply(StatusKind.Shield, Math.Max(100, guard.Effect.DurationSec * 1000.0), amount);
                    }
                    else if (guard.Effect.Stat == "hasar_buff" && _clock != null)
                    {
                        _selfDamageBuff = Mathf.Max(_selfDamageBuff, (float)Math.Abs(guard.Effect.Amount));
                        _selfDamageBuffUntilMs = Math.Max(
                            _selfDamageBuffUntilMs,
                            _clock.Director.WorldTimeMs + Math.Max(100, guard.Effect.DurationSec * 1000.0));
                    }
                }
                if (guard.View != null)
                    Destroy(guard.View);
                _guardTriggers.RemoveAt(i);
            }
        }

        float RedirectMechanicDamage(float incoming)
        {
            if (_clock == null || incoming <= 0)
                return incoming;
            double now = _clock.Director.WorldTimeMs;
            float remaining = incoming;
            foreach (MechanicLink link in _mechanicLinks)
            {
                if (now >= link.UntilMs)
                    continue;
                MechanicEffect share = link.Plan.Effects.FirstOrDefault(e => e.Stat == "hasar_paylasimi");
                if (share != null && _ally != null)
                {
                    float ratio = Mathf.Clamp01((float)share.Amount);
                    float redirected = remaining * ratio;
                    _ally.ApplyDamage(Mathf.CeilToInt(redirected));
                    remaining -= redirected;
                }
                MechanicEffect route = link.Plan.Effects.FirstOrDefault(e => e.Stat == "yonlendir");
                if (route != null && _bossVitals != null && !_bossVitals.IsDown)
                {
                    float ratio = Mathf.Clamp01((float)route.Amount);
                    float redirected = remaining * ratio;
                    _bossVitals.ApplyDamage(redirected);
                    remaining -= redirected;
                }
            }
            return remaining;
        }

        void ReflectFromWorldVolumes(float incoming)
        {
            if (_clock == null || _player == null || _bossVitals == null || _bossVitals.IsDown)
                return;
            double now = _clock.Director.WorldTimeMs;
            foreach (MechanicVolume volume in _mechanicVolumes)
            {
                if (!volume.Profile.Reflector || now >= volume.UntilMs
                    || FlatDistance(_player.position, volume.Center) > volume.RadiusM)
                    continue;
                MechanicEffect reflect = volume.Plan.Effects.FirstOrDefault(e => e.Stat == "yansit");
                if (reflect != null && reflect.Amount > 0)
                    _bossVitals.ApplyDamage(incoming * (float)reflect.Amount);
            }
        }

        void RewindBoss(double seconds, double worldMs, List<string> applied)
        {
            if (_boss == null || seconds <= 0
                || !_bossMechanicHistory.TryGetAtOrBefore(worldMs - seconds * 1000.0, out Vector3 past))
                return;
            _boss.Home = past;
            bool cancelled = _bossDirector != null && _bossDirector.CancelPreparedAttack(worldMs);
            applied.Add(cancelled ? $"geri sarma {seconds:0.#}sn + cast iptal" : $"geri sarma {seconds:0.#}sn");
        }

        void ApplyStatusTransfer(List<string> applied)
        {
            if (_bossStatus == null)
                return;
            StatusBoard source = _ally?.Board ?? _playerStatus?.Board;
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
                _bossStatus.Board.Apply(kind, remainingMs, magnitude);
                moved.Add(kind);
            }
            source.RemoveKinds(moved);
            if (moved.Count > 0)
                applied.Add("durum aktarma " + string.Join(",", moved));
        }

        void PurgeBossBuffs(List<string> applied)
        {
            if (_bossStatus == null)
                return;
            int before = _bossStatus.Board.ActiveCount;
            _bossStatus.Board.RemoveKinds(PositiveStatuses);
            if (_bossStatus.Board.ActiveCount < before)
                applied.Add("buff silme");
        }

        bool HasSelfReflect(MechanicPlan plan) =>
            plan != null && plan.Effects.Any(e => e.Stat == "yansit" && e.Target == "kendin");

        SkillExecutorRoute ApplyMechanicWorldRoute(MechanicPlan plan, SkillExecutorRoute route)
        {
            if (plan == null || route.Kind is not (SkillExecutorKind.MeleeHitbox or SkillExecutorKind.Projectile))
                return route;
            MechanicWorldProfile profile = MechanicWorldProfile.From(plan);
            if (plan.Body.Anchored || profile.Continuous || profile.Cloud || profile.Vortex || profile.Link)
                return new SkillExecutorRoute(
                    SkillExecutorKind.FieldAura,
                    false,
                    "mechanic_grammar yaşayan alan gövdesi");
            return route;
        }
    }
}
