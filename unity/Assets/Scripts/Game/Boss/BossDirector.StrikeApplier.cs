using Dovus.App.Boss;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using Dovus.Game.DevTools;
using Dovus.Game.Feel;
using Dovus.Game.Team;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Boss
{
    public sealed partial class BossDirector
    {
        sealed class BossStrikeApplier
        {
            readonly BossDirector _d;

            public BossStrikeApplier(BossDirector director) => _d = director;

            public void ResolveStrike()
            {
                if (_d._vitals != null && _d._vitals.IsDown)
                    return;
                if (_d._attack == null || _d._resolver == null)
                    return;

                if (_d._attack.Kind == BossAttackKind.Volley)
                {
                    FireVolley();
                    return;
                }

                if (_d._attack.Kind == BossAttackKind.WebField)
                    return;

                bool? blindMiss = null;
                bool Blind()
                {
                    blindMiss ??= _d._bossStatus != null
                        && BossStatusMath.Misses(_d._bossStatus.Board, (float)_d._rng.NextDouble());
                    return blindMiss.Value;
                }

                ResolveOtherFriendlies(Blind);

                Vector3 strikeOrigin = StrikeVolumeOrigin();
                float dist = 0f;
                float angleDeg = 0f;
                if (_d._player != null)
                {
                    Vector3 forward = _d.transform.forward;
                    forward.y = 0f;
                    BossStrikeResolver.ComputeStrikeMetrics(
                        strikeOrigin.x,
                        strikeOrigin.z,
                        _d._player.position.x,
                        _d._player.position.z,
                        forward.x,
                        forward.z,
                        out dist,
                        out angleDeg);
                }

                bool stealthed = _d.PlayerStealthed;
                bool inVolume = BossStrikeResolver.PlayerVolumeHits(
                    _d._attack,
                    stealthed,
                    dist,
                    angleDeg,
                    _d._team != null ? _d._team.Hub.BossStrikeScale : 1f,
                    Blind());
                if (inVolume
                    && _d._player != null
                    && _d._player.GetComponent<PlayerDodgeController>() is PlayerDodgeController rig
                    && rig.IsSkillInvulnerable)
                    inVolume = false;

                int? press = BossStrikeResolver.SanitizeDodgePress(
                    _d._dodge?.PressTimeMs,
                    _d._dodge,
                    _d.TelegraphStartMs);

                var input = BossStrikeResolver.BuildExchangeInput(
                    _d.TelegraphStartMs,
                    _d._brain != null && _d._brain.StrikeWorldMs > 0
                        ? _d._brain.StrikeWorldMs
                        : _d._attack.StrikeTimeMs(_d.TelegraphStartMs),
                    press,
                    inVolume);

                ExchangeResult result = _d._resolver.Resolve(input);
                _d._feel?.OnExchange(result);

                if (result.Outcome == ExchangeOutcome.Hit)
                {
                    float raw = BossStatusMath.OutgoingDamage(
                        _d._attack.Damage,
                        _d._bossStatus != null ? _d._bossStatus.Board : null);
                    bool landed;
                    ActorStatusHost _playerStatus = _d._playerStatus;
                    if (_playerStatus != null)
                    {
                        _playerStatus.ApplyDamage(raw);
                        landed = _playerStatus.LastAppliedDamage > 0f;
                    }
                    else
                    {
                        _d._vitals?.ApplyDamage(Mathf.CeilToInt(raw));
                        landed = raw > 0f;
                    }

                    if (landed)
                        _d._engine?.Abort();

                    if (_d._attack.Kind == BossAttackKind.FireCone && _d._playerStatus != null && _d._combat != null)
                        ApplyFireConeMechanics(_d._playerStatus.Board);
                }
            }

            public void OnVolleyProjectilePlayerHit()
            {
                if (!_d._volleyOnHit.IsValid || _d._playerStatus == null || _d._combat == null)
                    return;
                if (!StatusKindUtil.TryParse(_d._volleyOnHit.Id, out StatusKind kind) || kind == StatusKind.None)
                    return;
                BossMechanicStatusMap.ApplyKind(
                    _d._playerStatus.Board,
                    kind,
                    _d._combat.Status,
                    _d._volleyOnHit.DurationSec);
            }

            void ApplyFireConeMechanics(StatusBoard board)
            {
                if (board == null || _d._combat == null)
                    return;
                IReadOnlyList<string> mechs = _d._activeAttackEntry?.Mechanics;
                BossMechanicStatusMap.ApplyFireCone(board, _d._combat.Status, mechs);
            }

            void FireVolley()
            {
                if (_d._projectiles == null || _d._reactor == null)
                    return;
                _d._volleyOnHit = _d._activeAttackEntry != null && _d._activeAttackEntry.OnHitStatus.IsValid
                    ? _d._activeAttackEntry.OnHitStatus
                    : default;
                Vector3 origin = _d._reactor.Home;
                Transform aim = _d.AimTarget();
                Vector3 dir = aim != null ? aim.position - origin : _d.transform.forward;
                dir.y = 0f;
                if (_d.IsReversed && aim != null)
                    dir = -dir;
                if (dir.sqrMagnitude < 0.0001f)
                    dir = _d.transform.forward;
                dir.y = 0f;
                dir.Normalize();

                bool blind = _d._bossStatus != null && BossStatusMath.BlindMissChance(_d._bossStatus.Board) > 0f;
                float spread = VolleyPattern.EffectiveSpreadDeg(_d._attack.VolleySpreadDeg, blind);
                VolleyLayout layout = VolleyPattern.ComputeLayout(_d._attack.VolleyCount, spread);
                float raw = BossStatusMath.OutgoingDamage(
                    _d._attack.Damage,
                    _d._bossStatus != null ? _d._bossStatus.Board : null);
                Vector3 from = origin + dir * (_d._reactor.BodyRadiusM + _d._attack.VolleyRadiusM);
                for (int i = 0; i < layout.Count; i++)
                {
                    Vector3 d = Quaternion.AngleAxis(VolleyPattern.YawDegAt(layout, i), Vector3.up) * dir;
                    _d._projectiles.Spawn(
                        from,
                        d * _d._attack.VolleySpeedMps,
                        _d._attack.VolleyRadiusM,
                        raw,
                        _d._attack.VolleyLifeSec,
                        _d._targetId);
                }

                DebugConfig.DevLog(
                    $"[Boss] Zehir Tükürüğü {layout.Count} mermi yelpaze {spread:0}°"
                    + (blind ? " (kör)" : "")
                    + $" hedef={_d.CurrentTargetKind}");
            }

            void ResolveOtherFriendlies(System.Func<bool> blind)
            {
                if (_d._targets == null || _d._reactor == null)
                    return;
                Vector3 strikeOrigin = StrikeVolumeOrigin();
                Vector3 forward = _d.transform.forward;
                forward.y = 0f;
                var entries = _d._targets.Entries;
                for (int i = entries.Count - 1; i >= 0; i--)
                {
                    HostileTargetsHost.Entry e = entries[i];
                    if (e.Kind == TargetKind.Player || !e.IsAlive)
                        continue;
                    BossStrikeResolver.ComputeStrikeMetrics(
                        strikeOrigin.x,
                        strikeOrigin.z,
                        e.Transform.position.x,
                        e.Transform.position.z,
                        forward.x,
                        forward.z,
                        out float dist,
                        out float angleDeg);
                    if (!BossStrikeResolver.AllyVolumeHits(
                            _d._attack,
                            e.IsStealthed,
                            dist,
                            angleDeg,
                            _d._team != null ? _d._team.Hub.BossStrikeScale : 1f,
                            blind()))
                        continue;

                    if (e.Kind == TargetKind.Decoy)
                    {
                        e.Kill?.Invoke();
                        continue;
                    }

                    float raw = BossStatusMath.OutgoingDamage(
                        _d._attack.Damage,
                        _d._bossStatus != null ? _d._bossStatus.Board : null);
                    e.Damage?.Invoke(raw);
                    AllyDummyController ally = _d._attack.Kind == BossAttackKind.FireCone
                        ? e.Transform.GetComponent<AllyDummyController>()
                        : null;
                    if (ally != null && _d._combat != null && ally.Board != null && !ally.IsDown)
                        ApplyFireConeMechanics(ally.Board);
                }
            }

            Vector3 StrikeVolumeOrigin()
            {
                if (_d._attack != null
                    && _d._attack.Kind == BossAttackKind.Pounce
                    && _d._reactor != null)
                    return new Vector3(_d._attack.LandingX, _d._reactor.Home.y, _d._attack.LandingZ);
                return _d._reactor != null ? _d._reactor.Home : _d.transform.position;
            }
        }
    }
}
