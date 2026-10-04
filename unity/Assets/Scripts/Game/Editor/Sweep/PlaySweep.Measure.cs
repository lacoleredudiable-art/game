#if UNITY_EDITOR
using Dovus.Core.Boss;
using Dovus.Core.Element;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Team;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Dovus.Game.Editor
{
    public static partial class PlaySweep
    {
            // ---------------------------------------------------------------- ölçüm

            static bool Performing() => P<bool>(_md, "PerformingAttack");

            static bool BossPulling() => _bossReactor != null && _bossReactor.PullActive;

            static Snapshot Snap() => new()
            {
                BossHp = _bossVitals.Hp,
                PlayerHp = _playerVitals != null ? _playerVitals.Hp : 0,
                AllyHp = _ally != null ? _ally.Hp : 0,
                P = _player.position,
                B = _boss.position,
                RootCount = _player.gameObject.scene.rootCount,
            };

            static Frame Sample()
            {
                RefreshBody();
                var f = new Frame
                {
                    T = (float)((NowMs - _castMs) / 1000.0),
                    Dt = SweepPace.EffectiveFrameSec(Time.unscaledDeltaTime, _speed),
                    P = _player.position,
                    B = _boss.position,
                    Playing = _body != null && _body.IsDisplacing,
                    Performing = Performing(),
                    Dodge = _dodge != null && _dodge.IsDisplacing,
                    Teleport = PortalBorderTeamHost.Hub.ConsumeIntentionalTeleport(),
                    Yaw = _player.eulerAngles.y,
                    BossHp = _bossVitals.Hp,
                    PlayerHp = _playerVitals != null ? _playerVitals.Hp : 0,
                    AllyHp = _ally != null ? _ally.Hp : 0,
                    BossReversed = _bossDirector != null && _bossDirector.IsReversed,
                    DecoyAggro = _bossDirector != null && _bossDirector.Targets != null
                        && _bossDirector.Targets.DecoyHoldsAggro(),
                    ProjectilesAlive = _projectiles != null ? _projectiles.Sim.AliveCount : 0,
                    ReflectedAlive = ReflectedAlive(),
                };
                SentencePhase sentence = _input.Engine != null ? _input.Engine.State.Phase : SentencePhase.Idle;
                bool drawing = sentence == SentencePhase.Building || sentence == SentencePhase.Recovering;
                var pendingList = F<object>(_md, "_pending") as IList;
                int pending = pendingList?.Count ?? 0;
                if (_scheduledBang < 0f && pending > 0)
                    _scheduledBang = (float)((F<double>(pendingList[0], "BangAtWorldMs") - _castMs) / 1000.0);
                f.Bang = !drawing;
                f.Busy = f.Playing || drawing || pending > 0;

                if (f.Playing && _info != null && _info.Template != null && !_info.AimCaptured)
                {
                    _info.AimCaptured = true;
                    var aim = F<Transform>(_md, "_templateAim");
                    if (aim != _boss.transform)
                        Simulate(_info, aim);
                }

                object runner = _body != null ? F<object>(_body, "_runner") : null;
                if (runner is MotionTemplateRunner mr)
                {
                    f.Runner = new Vector3(mr.X, mr.Y, mr.Z);
                    f.RunnerDone = mr.Finished;
                    int phase = F<int>(mr, "_phase");
                    var tpl = F<MotionTemplate>(mr, "_template");
                    f.Phase = tpl != null && phase >= 0 && phase < tpl.Phases.Count ? tpl.Phases[phase].Name : "";
                }

                if (_animator != null && _animator.isActiveAndEnabled)
                {
                    AnimatorStateInfo st = _animator.GetCurrentAnimatorStateInfo(0);
                    f.Base = StateName(st.shortNameHash);
                    f.BaseNorm = st.normalizedTime;
                    int upper = _animator.GetLayerIndex(ActorVisual.UpperLayerName);
                    if (upper >= 0)
                        f.Upper = StateName(_animator.GetCurrentAnimatorStateInfo(upper).shortNameHash);
                    f.Speed = SafeFloat(ActorVisual.ParamSpeed);
                    f.Playback = SafeFloat(ActorVisual.ParamLocoPlayback);
                }

                if (_bossStatus != null)
                {
                    StatusBoard b = _bossStatus.Board;
                    f.BossKinds = string.Join("+", b.ActiveKinds.Select(k => k.ToString()).OrderBy(x => x));
                    f.BossMove = b.MoveSpeedMult;
                    f.BossAction = b.ActionSpeedMult;
                }
                if (_playerStatus != null)
                {
                    StatusBoard b = _playerStatus.Board;
                    f.PlayerKinds = string.Join("+", b.ActiveKinds.Select(k => k.ToString()).OrderBy(x => x));
                    f.PlayerMove = b.MoveSpeedMult;
                    f.PlayerAction = b.ActionSpeedMult;
                    f.Shield = b.ShieldRemaining;
                }
                f.Airborne = f.Playing && PhaseLeavesGround(f.Phase);
                f.Feet = FeetOrRoot(_player);
                f.FootGround = FootGroundOf(_player);
                f.BossFeet = FeetOrRoot(_boss);
                f.BossFootGround = FootGroundOf(_boss);
                if (_ally != null)
                {
                    f.AllyFeet = FeetOrRoot(_ally.transform);
                    f.AllyFootGround = FootGroundOf(_ally.transform);
                }
                return f;
            }

            static int ReflectedAlive()
            {
                if (_projectiles == null)
                    return 0;
                int n = 0;
                for (int i = 0; i < _projectiles.Sim.MaxAlive; i++)
                {
                    Projectile p = _projectiles.Sim.Slot(i);
                    if (p.Alive && p.Team == 0)
                        n++;
                }
                return n;
            }

            static float FeetOrRoot(Transform actor)
            {
                float y = ActorGrounding.MeasureFeet(actor);
                if (float.IsPositiveInfinity(y) && actor != null)
                    return actor.position.y;
                return y;
            }

            static float FootGroundOf(Transform actor)
            {
                ActorGrounding g = actor != null ? actor.GetComponent<ActorGrounding>() : null;
                if (g != null && g.HasFootGround)
                    return g.FootGroundY;
                return 0f;
            }

            static bool PhaseLeavesGround(string phase)
            {
                if (string.IsNullOrEmpty(phase) || _info?.Template == null)
                    return false;
                foreach (MotionPhase p in _info.Template.Phases)
                {
                    if (p.Name == phase)
                        return p.Airborne;
                }
                return false;
            }

            static float SafeFloat(string name)
            {
                foreach (AnimatorControllerParameter p in _animator.parameters)
                {
                    if (p.name == name && p.type == AnimatorControllerParameterType.Float)
                        return _animator.GetFloat(name);
                }
                return 0f;
            }

            static void ScanHitVfx(float t)
            {
                foreach (Transform child in _md.transform)
                {
                    if (!_seenVfx.Add(child.GetInstanceID()))
                        continue;
                    if (child.name.StartsWith("motion-", StringComparison.Ordinal) || child.name == "Fuse")
                    {
                        _hitOrigins.Add(child.position);
                        _hitTimes.Add(t);
                        if (child.name == "Fuse")
                            _logs.Add($"fitil t={t:F2} boss merkezine {Flat(child.position - _boss.position).magnitude:F2} m");
                    }
                }
            }

            static void OnLog(string msg, string stack, LogType type)
            {
                if (!Running || (_stage != Stage.Record && _stage != Stage.Settle))
                    return;
                string first = msg.Split('\n')[0];
                if (first.StartsWith("[PlaySweep]", StringComparison.Ordinal))
                    return;
                switch (type)
                {
                    case LogType.Error:
                    case LogType.Exception:
                    case LogType.Assert:
                        _logs.Add("ERROR " + first);
                        break;
                    case LogType.Warning:
                        _logs.Add("WARN " + first);
                        break;
                    default:
                        if (first.StartsWith("[Motion]") || first.StartsWith("[Mechanic]")
                            || first.StartsWith("[SkillExecutor]") || first.StartsWith("[Position]"))
                            _logs.Add("LOG " + first);
                        break;
                }
            }

    }

}
#endif
