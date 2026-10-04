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
using Dovus.Game.Platform;
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

namespace Dovus.Game.Editor.Sweep
{
    public static partial class PlaySweep
    {
            struct SweepTick { }

            static void InstallLoop(bool on)
            {
                PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
                for (int i = 0; i < loop.subSystemList.Length; i++)
                {
                    if (loop.subSystemList[i].type != typeof(PostLateUpdate))
                        continue;
                    PlayerLoopSystem post = loop.subSystemList[i];
                    var list = new List<PlayerLoopSystem>(post.subSystemList ?? Array.Empty<PlayerLoopSystem>());
                    list.RemoveAll(s => s.type == typeof(SweepTick));
                    if (on)
                        list.Add(new PlayerLoopSystem { type = typeof(SweepTick), updateDelegate = Tick });
                    post.subSystemList = list.ToArray();
                    loop.subSystemList[i] = post;
                }
                PlayerLoop.SetPlayerLoop(loop);
            }

            static bool LoopInstalled()
            {
                foreach (PlayerLoopSystem sys in PlayerLoop.GetCurrentPlayerLoop().subSystemList)
                {
                    if (sys.type == typeof(PostLateUpdate) && sys.subSystemList != null)
                        return sys.subSystemList.Any(s => s.type == typeof(SweepTick));
                }
                return false;
            }

            /// <summary>Play açılışında paketler PlayerLoop'u sıfırlayabiliyor; tick kaybolursa geri takılır.</summary>
            static void LoopWatchdog()
            {
                if (Running && EditorApplication.isPlaying && !LoopInstalled())
                    InstallLoop(true);
            }

            static double NowMs => _clock != null ? _clock.Director.WorldTimeMs : Time.timeAsDouble * 1000.0;

            /// <summary>
            /// timeScale ve GameClockHost aynı çarpan. fixedDeltaTime 1× adımında kalır;
            /// maximumDeltaTime dünya saniyesi olduğu için çarpanla büyür.
            /// </summary>
            static void ApplyPace()
            {
                if (!_paceSaved)
                {
                    _savedTimeScale = Time.timeScale;
                    _savedFixed = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;
                    _savedMaxDelta = Time.maximumDeltaTime > 0f ? Time.maximumDeltaTime : 1f / 3f;
                    _paceSaved = true;
                }

                Time.timeScale = _speed;
                Time.fixedDeltaTime = SweepPace.FixedStepSec(_savedFixed);
                Time.maximumDeltaTime = SweepPace.MaxDeltaSec(_savedMaxDelta, _speed);
                if (_clock != null)
                    _clock.SimulationScale = _speed;
            }

            static void ClearPace()
            {
                if (_clock != null)
                    _clock.SimulationScale = 1f;
                if (!_paceSaved)
                    return;
                Time.timeScale = _savedTimeScale;
                Time.fixedDeltaTime = _savedFixed;
                Time.maximumDeltaTime = _savedMaxDelta;
                _paceSaved = false;
            }

            static void Tick()
            {
                if (!Running || !Application.isPlaying)
                    return;
                ApplyPace();
                try
                {
                    switch (_stage)
                    {
                        case Stage.WaitScene:
                            if (++_waitFrames < 30 || !BindScene())
                                return;
                            if (_expandWeapons && _cases.Count == 0)
                            {
                                _cases = AllWeaponCases();
                                _expandWeapons = false;
                                if (_cases.Count == 0)
                                {
                                    Stop("silah kataloğu boş");
                                    return;
                                }
                            }
                            if (!_bossStartSet)
                            {
                                _bossStart = _boss.position;
                                _bossStartSet = true;
                                if (_ally != null)
                                {
                                    _allyStart = _ally.transform.position;
                                    _allyStartSet = true;
                                }
                            }
                            NextCase();
                            break;
                        case Stage.Idle:
                            TickIdle();
                            break;
                        case Stage.Setup:
                            TickSetup();
                            break;
                        case Stage.Settle:
                            TickSettle();
                            break;
                        case Stage.Record:
                            TickRecord();
                            break;
                    }
                }
                catch (Exception e)
                {
                    Exception inner = e is TargetInvocationException t && t.InnerException != null ? t.InnerException : e;
                    _logs.Add("SWEEP-EXC " + inner.GetType().Name + ": " + inner.Message);
                    Debug.LogWarning("[PlaySweep] araç hatası: " + inner);
                    try
                    {
                        if (_stage == Stage.Record)
                        {
                            FinishCase(true);
                        }
                        else if (_stage == Stage.Setup || _stage == Stage.Settle)
                        {
                            PlaySweepResult r = NewResult(_cases[_index]);
                            r.Notes.Add("araç hatası: " + inner.Message);
                            Results.Add(r);
                            _index++;
                            NextCase();
                        }
                        else
                        {
                            Stop("araç hatası");
                        }
                    }
                    catch (Exception again)
                    {
                        Debug.LogWarning("[PlaySweep] durduruldu: " + again);
                        Stop("araç hatası");
                    }
                }
            }

            static bool BindScene()
            {
                _md = UnityEngine.Object.FindAnyObjectByType<ManifestationDirector>();
                if (_md == null)
                    return false;
                _input = _md.SweepInput;
                _player = _md.SweepPlayer;
                var bossReactor = _md.SweepBoss;
                if (_input == null || _player == null || bossReactor == null)
                    return false;
                _boss = bossReactor.transform;
                _bossReactor = bossReactor;
                _bossVitals = _md.SweepBossVitals;
                _bossDirector = _md.SweepBossDirector;
                _playerStatus = _md.SweepPlayerStatus;
                _bossStatus = _md.SweepBossStatus;
                _ally = _md.SweepAlly;
                _clock = _md.SweepClock;
                _skills = _md.SweepSkills;
                _projectiles = _md.SweepProjectiles;
                BindSweepAccess(_md.SweepTeamAccess);
                _playerVitals = _player.GetComponent<PlayerVitalsHost>();
                if (_playerVitals != null)
                    _playerVitals.SuppressDown = true;
                _dodge = _player.GetComponent<DodgeMotionController>();
                var visual = _player.GetComponent<ActorView>();
                _animator = visual != null ? visual.Animator : null;
                RefreshBody();
                CollectStateNames();
                return _skills != null && _bossVitals != null;
            }

            /// <summary>
            /// Gövde bileşenleri geç eklenebilir (build ekranı açıkken Idle atlanır, gövde bang'de doğabilir).
            /// Boş ya da yok edilmiş referans her çağrıda yeniden aranır.
            /// </summary>
            static void RefreshBody()
            {
                if (_player == null)
                    return;
                if (_body == null)
                    _body = _player.GetComponent<MotionTemplateBodyHost>();
            }

            static void NextCase()
            {
                if (_index >= _cases.Count)
                {
                    Finish();
                    return;
                }
                _stage = Stage.Idle;
                _stageMs = NowMs;
                Status = $"{_index + 1}/{_cases.Count} {_cases[_index].Id} {_cases[_index].Weapon}";
            }

            static void Finish()
            {
                Running = false;
                _expandWeapons = false;
                InstallLoop(false);
                ClearPace();
                Application.logMessageReceived -= OnLog;
                if (_bossDirector != null)
                    _bossDirector.enabled = true;
                if (_playerVitals != null)
                    _playerVitals.SuppressDown = false;
                _projectiles?.ClearAll();
                WriteOutputs();
                Status = "bitti: " + LastSummary;
                Debug.Log("[PlaySweep] " + LastSummary);
            }

            // ---------------------------------------------------------------- aşamalar

            static void TickIdle()
            {
                if (_bossDirector != null)
                    _bossDirector.enabled = false;
                if (BuildSelectHud.IsOpen)
                {
                    // Açık build ekranı saati durdurur; bekleme dünya saatine bakar, hiç bitmez.
                    _stage = Stage.Setup;
                    _stageMs = NowMs;
                    return;
                }
                RefreshBody();
                bool busy = Performing() || (_body != null && _body.IsDisplacing) || BossPulling();
                bool worldLeft = MechanicLeftovers() > 0;
                double waited = NowMs - _stageMs;
                if ((busy || worldLeft) && waited < 6000)
                    return;
                if (busy)
                    ForceClean();
                if (worldLeft)
                    ClearMechanicWorld();
                _stage = Stage.Setup;
                _stageMs = NowMs;
            }

            static void TickSetup()
            {
                PlaySweepCase c = _cases[_index];
                if (!EnsureLoadout(c.Verb, c.Adj, out string buildError))
                {
                    var r = NewResult(c);
                    r.Notes.Add("build reddedildi: " + buildError);
                    Results.Add(r);
                    _index++;
                    NextCase();
                    return;
                }
                EnsureWeapon(c.Weapon);
                ResetActors();
                PrepareGuardFixture(c);
                ResetBossPosition();
                ResetSweepActors();
                PlacePlayer(c.StartDistM);
                _stage = Stage.Settle;
                _stageMs = NowMs;
            }

            static void TickSettle()
            {
                if (NowMs - _stageMs < SettleSec * 1000.0)
                    return;
                // Önceki Emici çekmesi sürerken boss oyuncunun eski yerine kayar; yeni cast o kaymayı devralır.
                if (BossPulling() && NowMs - _stageMs < 3000.0)
                    return;
                PlaySweepCase c = _cases[_index];
                ResetSweepActors();
                PlacePlayer(c.StartDistM);
                _info = Describe(c);
                _frames.Clear();
                _scheduledBang = -1f;
                _logs.Clear();
                _hitOrigins.Clear();
                _hitTimes.Clear();
                _seenVfx.Clear();
                foreach (Transform child in _md.transform)
                    _seenVfx.Add(child.GetInstanceID());
                _pauseIndex = 0;
                _bossShifted = false;
                _bossHome = _boss.position;
                _playerShiftDone = 0f;
                _playerShiftDir = Vector3.zero;
                PrepareProjectileFixture();
                _pre = Snap();
                RefreshBody();
                bool ok = _input.TryDebugCastSkill(c.Verb, c.Adj);
                _castMs = NowMs;
                if (!ok)
                {
                    var r = NewResult(c);
                    r.Notes.Add("cast reddedildi: " + RejectReason(c));
                    Results.Add(r);
                    _index++;
                    NextCase();
                    return;
                }
                _stage = Stage.Record;
                _stageMs = NowMs;
            }

            static void TickRecord()
            {
                PlaySweepCase c = _cases[_index];
                Frame f = Sample();
                _frames.Add(f);
                ScanHitVfx(f.T);
                float templateT = TemplateTime();
                ApplyBossShift(c, templateT);
                ApplyPlayerShift(c, templateT);
                ApplyStick(c, templateT, f.Playing);
                if (c.PauseAtSec != null && _pauseIndex < c.PauseAtSec.Length && templateT >= c.PauseAtSec[_pauseIndex])
                {
                    PausedAt = $"{c.Id} kalıp t={templateT:F2} (istenen {c.PauseAtSec[_pauseIndex]:F2})";
                    _pauseIndex++;
                    EditorApplication.isPaused = true;
                }

                bool started = _frames.Any(x => x.Playing);
                // Geri gönderilen mermi boss'a varana dek vaka bitmez (geri_gonder: boss canı düşmeli).
                bool idle = !f.Busy && f.ReflectedAlive == 0;
                float delivered = Mathf.Max(_info.ExpectedSec, _info.DeliveryDelaySec);
                float minRecord = _info.RecoverySec + delivered + TailSec;
                float timeout = _info.RecoverySec + delivered + TimeoutExtraSec;
                if (f.T >= timeout)
                {
                    FinishCase(true);
                    return;
                }
                if (!started && f.T < _info.RecoverySec + 1.0f)
                    return;
                if (idle && f.T >= minRecord && IdleFor() >= IdleHoldSec
                    && TemplateEndedFor() >= Grounding.SettleAfterSec)
                    FinishCase(false);
            }

            static float IdleFor()
            {
                float end = _frames[_frames.Count - 1].T;
                for (int i = _frames.Count - 1; i >= 0; i--)
                {
                    if (_frames[i].Busy)
                        return end - _frames[i].T;
                }
                return end;
            }

            static float TemplateTime()
            {
                int first = _frames.FindIndex(x => x.Playing);
                return first < 0 ? -1f : _frames[_frames.Count - 1].T - _frames[first].T;
            }

            static void ApplyBossShift(PlaySweepCase c, float templateT)
            {
                if (Mathf.Abs(c.BossShiftX) < 0.001f || templateT < c.BossShiftAtSec)
                    return;
                float k = Mathf.Clamp01((templateT - c.BossShiftAtSec) / Mathf.Max(0.01f, c.BossShiftDurSec));
                Vector3 p = _bossHome + new Vector3(c.BossShiftX * k, 0f, 0f);
                _boss.position = new Vector3(p.x, _boss.position.y, p.z);
                _bossShifted = true;
            }

            /// <summary>Kaymanın yalnız bu karelik payı eklenir; kalıp oyuncuyu yeni yerinden sürdürür.</summary>
            static void ApplyPlayerShift(PlaySweepCase c, float templateT)
            {
                if (Mathf.Abs(c.PlayerShiftM) < 0.001f || templateT < c.PlayerShiftAtSec)
                    return;
                if (_playerShiftDir.sqrMagnitude < 0.0001f)
                {
                    Vector3 d = _boss.position - _player.position;
                    d.y = 0f;
                    _playerShiftDir = d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
                }
                float k = Mathf.Clamp01((templateT - c.PlayerShiftAtSec) / Mathf.Max(0.01f, c.PlayerShiftDurSec));
                float want = c.PlayerShiftM * k;
                float step = want - _playerShiftDone;
                if (Mathf.Abs(step) < 0.0001f)
                    return;
                _player.position += _playerShiftDir * step;
                _playerShiftDone = want;
            }

            static void ApplyStick(PlaySweepCase c, float templateT, bool playing)
            {
                if (c.Stick.sqrMagnitude < 0.0001f)
                    return;
                var move = _player.GetComponent<MoveInputController>();
                if (move == null)
                    return;
                move.SetScriptedDirection(playing && templateT >= c.StickAtSec ? c.Stick : (Vector2?)null);
            }

            static void FinishCase(bool timedOut)
            {
                PlaySweepCase c = _cases[_index];
                _player.GetComponent<MoveInputController>()?.SetScriptedDirection(null);
                PlaySweepResult r = Evaluate(c, timedOut);
                Results.Add(r);
                if (c.Trace)
                    WriteTrace(c, r);
                if (_bossShifted)
                {
                    _boss.position = new Vector3(_bossHome.x, _boss.position.y, _bossHome.z);
                    Physics.SyncTransforms();
                }
                _index++;
                NextCase();
            }

    }

}
#endif
