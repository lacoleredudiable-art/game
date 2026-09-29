#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Dovus.Game.EditorTools
{
    /// <summary>Tek cast: fiil + sıfat, boss'a merkez mesafesi, silah.</summary>
    public sealed class PlaySweepCase
    {
        public int Verb;
        public int Adj;
        public float StartDistM = 3f;
        public string Weapon = "Kılıç";
        public string Label = "";
        /// <summary>Kare kare iz detay dosyasına yazılır.</summary>
        public bool Trace;
        /// <summary>Kalıp başladıktan BossShiftAtSec sonra boss X ekseninde bu kadar kayar.</summary>
        public float BossShiftX;
        public float BossShiftAtSec = 0.2f;
        public float BossShiftDurSec = 0.3f;
        /// <summary>Kalıp zamanı (sn). Editör o anda duraklar; ekran görüntüsü için.</summary>
        public float[] PauseAtSec;
        /// <summary>Kalıp StickAtSec'e gelince çubuk bu yöne basılır, kalıp bitince bırakılır.</summary>
        public Vector2 Stick;
        public float StickAtSec = 0.5f;

        public string Id => Verb + "-" + Adj;
    }

    public sealed class PlaySweepResult
    {
        public PlaySweepCase Case;
        public string Name = "";
        public string Template = "";
        public string Weapon = "";
        public bool Cast;
        public bool Hit;
        public bool Position;
        public bool NotInside;
        public bool OneSystem;
        public bool NoErrors;
        public bool OnTime;
        public bool NoTeleport;
        public string ExpectedPos = "";
        public string ActualPos = "";
        public float Damage;
        public float MinDist;
        public float Contact;
        public float TemplateSec;
        public float ExpectedSec;
        public float TotalSec;
        public string Effects = "";
        public string Legs = "";
        public readonly List<string> Notes = new();

        public bool Pass => Cast && Hit && Position && NotInside && OneSystem && NoErrors && OnTime && NoTeleport;
    }

    /// <summary>
    /// Play Mode kombo taraması. Boss'u durdurur, oyuncuyu boss'a verilen mesafeye koyar,
    /// her komboyu TryDebugCastSkill ile atar ve kare sonunda (PostLateUpdate) ölçer.
    /// Skill koduna dokunmaz; yalnız ölçer ve raporlar.
    /// Çıktı: docs/play-sweep/&lt;etiket&gt;.csv ve &lt;etiket&gt;-detay.txt.
    /// </summary>
    [InitializeOnLoad]
    public static class PlaySweep
    {
        const string PendingKey = "Dovus.PlaySweep.Pending";
        const string ScenePath = "Assets/Scenes/Prototype.unity";
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // Ölçüm eşikleri — araç ayarı, oyun hissi değil.
        const float SettleSec = 0.35f;
        const float IdleHoldSec = 0.25f;
        const float TailSec = 0.6f;
        const float TimeoutExtraSec = 4f;
        const float TemplateTolSec = 0.2f;
        const float TotalTolSec = 0.6f;
        const float StartTolSec = 0.15f;
        const float InsideTolM = 0.25f;
        const float InPlaceM = 0.35f;
        const float SimMatchM = 0.75f;
        const float TemplateMoveM = 0.05f;
        const float OtherMoveM = 0.15f;
        const float TeleportM = 0.6f;
        const float LegMoveMps = 1.5f;

        static readonly int[][] RuneGroups =
        {
            new[] { 1, 2, 3 }, new[] { 4, 5, 6 }, new[] { 7, 8, 9 }, new[] { 10, 11, 12 },
        };

        public static bool Running { get; private set; }
        public static string Status { get; private set; } = "boşta";
        public static string LastSummary { get; private set; } = "";
        public static string PausedAt { get; private set; } = "";
        public static readonly List<PlaySweepResult> Results = new();

        static List<PlaySweepCase> _cases;
        static string _label = "";
        static string _secondWeapon = "";
        static int _index;
        static Stage _stage;
        static double _stageMs;
        static double _castMs;
        static int _waitFrames;
        static readonly List<Frame> _frames = new();
        static readonly List<string> _logs = new();
        static readonly List<Vector3> _hitOrigins = new();
        static readonly List<float> _hitTimes = new();
        static readonly HashSet<int> _seenVfx = new();
        static readonly StringBuilder _detail = new();
        static readonly Dictionary<int, string> _stateNames = new();
        static int _pauseIndex;
        static Vector3 _bossHome;
        static bool _bossShifted;
        static Snapshot _pre;
        static CaseInfo _info;
        /// <summary>ManifestationDirector._pending[0].BangAtWorldMs, cast anına göre sn; castMult dahil.</summary>
        static float _scheduledBang = -1f;

        // Sahne referansları
        static ManifestationDirector _md;
        static HexagonInput _input;
        static Transform _player;
        static Transform _boss;
        static BossVitals _bossVitals;
        static BossDirector _bossDirector;
        static ActorStatus _playerStatus;
        static ActorStatus _bossStatus;
        static PlayerVitals _playerVitals;
        static AllyDummy _ally;
        static MotionTemplateBody _body;
        static SkillMotionDriver _driver;
        static DodgeMotion _dodge;
        static Animator _animator;
        static GameClock _clock;
        static SkillMotor _skills;

        enum Stage { WaitScene, Idle, Setup, Settle, Record, Done }

        struct Frame
        {
            public float T;
            public Vector3 P;
            public Vector3 B;
            public Vector3 Runner;
            public bool Playing;
            public bool Performing;
            public bool Busy;
            public bool Bang;
            public bool Driver;
            public bool Dodge;
            public string Phase;
            public string Base;
            public string Upper;
            public float BaseNorm;
            public float Speed;
            public float Playback;
            public float Yaw;
            public float BossHp;
            public int PlayerHp;
            public int AllyHp;
            public string BossKinds;
            public string PlayerKinds;
            public float BossMove;
            public float BossAction;
            public float PlayerMove;
            public float PlayerAction;
            public float Shield;
            public int Zones;
        }

        struct Snapshot
        {
            public float BossHp;
            public int PlayerHp;
            public int AllyHp;
            public Vector3 P;
            public Vector3 B;
            public int RootCount;
        }

        sealed class CaseInfo
        {
            public string Name = "";
            public MotionTemplate Template;
            public float ExpectedSec;
            public float RecoverySec;
            public float BossR;
            public float PlayerR;
            public bool PassThrough;
            public bool DamageSkill;
            public bool HasHitPhase;
            public string ExpectedCat = "";
            public Vector3 SimFinal;
            public bool HasSim;
        }

        static PlaySweep()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Dovus/Play Sweep/144 kombo - Kılıç")]
        static void MenuSword() => Launch("kilic");

        [MenuItem("Dovus/Play Sweep/144 kombo - Kılıç + Asa")]
        static void MenuSwordStaff() => Launch("kilic+asa");

        [MenuItem("Dovus/Play Sweep/Durdur")]
        static void MenuStop() => Stop("menü");

        /// <summary>Menü girişi. Play kapalıysa sahneyi açar, Play'e girer, sonra başlar.</summary>
        public static void Launch(string preset)
        {
            if (EditorApplication.isPlaying)
            {
                StartPreset(preset);
                return;
            }

            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                EditorSceneManager.OpenScene(ScenePath);
            }

            SessionState.SetString(PendingKey, preset);
            EditorApplication.isPlaying = true;
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                string pending = SessionState.GetString(PendingKey, "");
                if (!string.IsNullOrEmpty(pending))
                {
                    SessionState.EraseString(PendingKey);
                    StartPreset(pending);
                }
            }
            else if (change == PlayModeStateChange.ExitingPlayMode && Running)
            {
                Stop("Play kapandı");
            }
        }

        public static void StartPreset(string preset)
        {
            string second = preset == "kilic+asa" ? "Asa" : "";
            Start(AllCombos("Kılıç", second), preset, second);
        }

        /// <summary>144 kombo, rün grubu çiftlerine göre sıralı (az build değişimi).</summary>
        public static List<PlaySweepCase> AllCombos(string weapon, string secondWeapon = "", float startDistM = 3f)
        {
            var list = new List<PlaySweepCase>();
            foreach (string w in new[] { weapon, secondWeapon })
            {
                if (string.IsNullOrEmpty(w))
                    continue;
                for (int v = 1; v <= 12; v++)
                for (int a = 1; a <= 12; a++)
                    list.Add(new PlaySweepCase { Verb = v, Adj = a, Weapon = w, StartDistM = startDistM });
            }

            return list
                .OrderBy(c => c.Weapon == weapon ? 0 : 1)
                .ThenBy(c => BuildKey(c.Verb, c.Adj))
                .ThenBy(c => c.Verb)
                .ThenBy(c => c.Adj)
                .ToList();
        }

        public static void Start(List<PlaySweepCase> cases, string label, string secondWeapon = "")
        {
            if (!EditorApplication.isPlaying)
            {
                Status = "Play kapalı";
                return;
            }

            _cases = cases ?? new List<PlaySweepCase>();
            _label = string.IsNullOrEmpty(label) ? "sweep" : label;
            _secondWeapon = secondWeapon ?? "";
            _index = 0;
            _stage = Stage.WaitScene;
            _waitFrames = 0;
            _detail.Clear();
            Results.Clear();
            LastSummary = "";
            PausedAt = "";
            Running = true;
            Status = "sahne bekleniyor";
            Application.runInBackground = true;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            InstallLoop(true);
        }

        public static void Stop(string why)
        {
            if (!Running)
                return;
            Running = false;
            InstallLoop(false);
            Application.logMessageReceived -= OnLog;
            if (_bossDirector != null)
                _bossDirector.enabled = true;
            Status = "durdu: " + why;
            if (Results.Count > 0)
                WriteOutputs();
        }

        // ---------------------------------------------------------------- döngü

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

        static double NowMs => _clock != null ? _clock.Director.WorldTimeMs : Time.timeAsDouble * 1000.0;

        static void Tick()
        {
            if (!Running || !Application.isPlaying)
                return;
            try
            {
                switch (_stage)
                {
                    case Stage.WaitScene:
                        if (++_waitFrames < 30 || !BindScene())
                            return;
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
            _input = F<HexagonInput>(_md, "_input");
            _player = F<Transform>(_md, "_player");
            var bossReactor = F<Component>(_md, "_boss");
            if (_input == null || _player == null || bossReactor == null)
                return false;
            _boss = bossReactor.transform;
            _bossVitals = F<BossVitals>(_md, "_bossVitals");
            _bossDirector = F<BossDirector>(_md, "_bossDirector");
            _playerStatus = F<ActorStatus>(_md, "_playerStatus");
            _bossStatus = F<ActorStatus>(_md, "_bossStatus");
            _ally = F<AllyDummy>(_md, "_ally");
            _clock = F<GameClock>(_md, "_clock");
            _skills = F<SkillMotor>(_md, "_skills");
            _playerVitals = _player.GetComponent<PlayerVitals>();
            _dodge = _player.GetComponent<DodgeMotion>();
            var visual = _player.GetComponent<ActorVisual>();
            _animator = visual != null ? F<Animator>(visual, "_animator") : null;
            CollectStateNames();
            return _skills != null && _bossVitals != null;
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
            InstallLoop(false);
            Application.logMessageReceived -= OnLog;
            if (_bossDirector != null)
                _bossDirector.enabled = true;
            WriteOutputs();
            Status = "bitti: " + LastSummary;
            Debug.Log("[PlaySweep] " + LastSummary);
        }

        // ---------------------------------------------------------------- aşamalar

        static void TickIdle()
        {
            if (_bossDirector != null)
                _bossDirector.enabled = false;
            _body ??= _player.GetComponent<MotionTemplateBody>();
            _driver ??= _player.GetComponent<SkillMotionDriver>();
            bool busy = Performing() || (_body != null && _body.IsDisplacing);
            var zones = _md.ZoneDirector;
            bool zonesLeft = zones != null && zones.ActiveZones.Count > 0;
            bool worldLeft = MechanicLeftovers() > 0;
            double waited = NowMs - _stageMs;
            if ((busy || zonesLeft || worldLeft) && waited < 6000)
                return;
            if (busy)
                ForceClean();
            if (zonesLeft)
            {
                foreach (var z in zones.ActiveZones.ToList())
                    zones.Remove(z.Id);
            }
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
            PlacePlayer(c.StartDistM);
            _stage = Stage.Settle;
            _stageMs = NowMs;
        }

        static void TickSettle()
        {
            if (NowMs - _stageMs < SettleSec * 1000.0)
                return;
            PlaySweepCase c = _cases[_index];
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
            _pre = Snap();
            bool ok = _input.TryDebugCastSkill(c.Verb, c.Adj);
            _castMs = NowMs;
            if (!ok)
            {
                var r = NewResult(c);
                r.Notes.Add("cast reddedildi (menzil/hedef kapısı ya da kilit)");
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
            ApplyStick(c, templateT, f.Playing);
            if (c.PauseAtSec != null && _pauseIndex < c.PauseAtSec.Length && templateT >= c.PauseAtSec[_pauseIndex])
            {
                PausedAt = $"{c.Id} kalıp t={templateT:F2} (istenen {c.PauseAtSec[_pauseIndex]:F2})";
                _pauseIndex++;
                EditorApplication.isPaused = true;
            }

            bool started = _frames.Any(x => x.Playing);
            bool idle = !f.Busy;
            float minRecord = _info.RecoverySec + _info.ExpectedSec + TailSec;
            float timeout = _info.RecoverySec + _info.ExpectedSec + TimeoutExtraSec;
            if (f.T >= timeout)
            {
                FinishCase(true);
                return;
            }
            if (!started && f.T < _info.RecoverySec + 1.0f)
                return;
            if (idle && f.T >= minRecord && IdleFor() >= IdleHoldSec)
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

        static void ApplyStick(PlaySweepCase c, float templateT, bool playing)
        {
            if (c.Stick.sqrMagnitude < 0.0001f)
                return;
            var move = _player.GetComponent<MoveInput>();
            if (move == null)
                return;
            move.SetScriptedDirection(playing && templateT >= c.StickAtSec ? c.Stick : (Vector2?)null);
        }

        static void FinishCase(bool timedOut)
        {
            PlaySweepCase c = _cases[_index];
            _player.GetComponent<MoveInput>()?.SetScriptedDirection(null);
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

        // ---------------------------------------------------------------- kurulum

        static int BuildKey(int v, int a)
        {
            int gv = (v - 1) / 3;
            int ga = (a - 1) / 3;
            if (gv == ga)
                ga = (gv + 1) % 4;
            int lo = Math.Min(gv, ga);
            int hi = Math.Max(gv, ga);
            return lo * 4 + hi;
        }

        static bool EnsureLoadout(int v, int a, out string error)
        {
            error = "";
            RuneLoadout current = _input.Engine?.Loadout;
            if (current != null && current.RuneIds.Contains(v) && current.RuneIds.Contains(a))
                return true;

            int key = BuildKey(v, a);
            var ids = new List<int>(RuneGroups[key / 4]);
            ids.AddRange(RuneGroups[key % 4]);

            var screen = UnityEngine.Object.FindAnyObjectByType<BuildSelectScreen>(FindObjectsInactive.Include);
            if (screen == null)
            {
                error = "BuildSelectScreen yok";
                return false;
            }
            screen.Open();
            var selected = F<List<int>>(screen, "_selected");
            selected.Clear();
            selected.AddRange(ids);
            F<List<int>>(screen, "_passiveSelected")?.Clear();
            var weapons = F<List<EquipmentItem>>(screen, "_weapons");
            EquipmentItem primary = FindWeapon(_cases[_index].Weapon);
            if (weapons != null && primary != null)
            {
                weapons.Clear();
                weapons.Add(primary);
                EquipmentItem second = FindWeapon(_secondWeapon);
                if (second == null || second.Id == primary.Id)
                    second = _md.AvailableWeapons.FirstOrDefault(w => w.Id != primary.Id);
                if (second != null)
                    weapons.Add(second);
            }
            Call(screen, "ApplyAndStart");
            if (BuildSelectScreen.IsOpen)
                Call(screen, "Close");
            current = _input.Engine?.Loadout;
            if (current == null || !current.RuneIds.Contains(v) || !current.RuneIds.Contains(a))
            {
                error = "build uygulanmadı [" + string.Join(",", ids) + "]";
                return false;
            }
            return true;
        }

        static EquipmentItem FindWeapon(string name)
        {
            if (string.IsNullOrEmpty(name) || _md == null)
                return null;
            foreach (EquipmentItem w in _md.AvailableWeapons)
            {
                if (string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)
                    || (w.Name != null && w.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
                    return w;
            }
            return null;
        }

        static void EnsureWeapon(string name)
        {
            EquipmentItem w = FindWeapon(name);
            if (w == null)
                return;
            if (_md.EquippedWeapon != null && _md.EquippedWeapon.Id == w.Id)
                return;
            EquipmentItem second = FindWeapon(_secondWeapon);
            if (second == null || second.Id == w.Id)
                second = _md.AvailableWeapons.FirstOrDefault(x => x.Id != w.Id);
            _md.SetWeaponLoadout(w, second);
        }

        static void ForceClean()
        {
            _body?.Stop();
            _player.GetComponent<ActorVisual>()?.EndMotionAnim();
            _input.Engine?.Abort();
            (F<object>(_md, "_pending") as IList)?.Clear();
            _logs.Add("önceki cast 6 sn'de bitmedi, zorla temizlendi");
        }

        static readonly string[] MechanicWorldLists = { "_mechanicLinks", "_mechanicVolumes", "_guardTriggers", "_mechanicBodies" };

        /// <summary>Bağ/hacim/tuzak: status board temizlense de sonraki casta Root/Slow ve boss çekişi taşır.</summary>
        static int MechanicLeftovers()
        {
            int n = 0;
            foreach (string name in MechanicWorldLists)
                n += (F<object>(_md, name) as IList)?.Count ?? 0;
            return n;
        }

        static void ClearMechanicWorld()
        {
            foreach (string name in MechanicWorldLists)
            {
                if (!(F<object>(_md, name) is IList list))
                    continue;
                foreach (object item in list)
                {
                    if (F<object>(item, "View") is GameObject view && view != null)
                        UnityEngine.Object.Destroy(view);
                    if (F<object>(item, "Line") is LineRenderer line && line != null)
                        UnityEngine.Object.Destroy(line.gameObject);
                }
                list.Clear();
            }
        }

        static void ResetActors()
        {
            if (_bossDirector != null)
                _bossDirector.enabled = false;
            _bossStatus?.Board.Clear();
            _playerStatus?.Board.Clear();
            _ally?.Board?.Clear();
            if (_bossVitals.IsDown || _bossVitals.Hp < _bossVitals.MaxHp * 0.6f)
                _bossVitals.Revive();
            if (_playerVitals != null)
                S(_playerVitals, "_hp", Math.Max(1, _playerVitals.MaxHp / 2));
            if (_ally != null)
                S(_ally, "_hp", Math.Max(1, _ally.MaxHp / 2));

            var resource = _player.GetComponent<PlayerResource>();
            object tracker = resource != null ? F<object>(resource, "_tracker") : null;
            if (tracker != null)
                S(tracker, "_mana", F<float>(tracker, "_maxMana"));
            var cooldown = _player.GetComponent<PlayerCooldown>();
            if (cooldown != null)
                cooldown.Bind(cooldown.GlobalCooldownSec > 0f ? cooldown.GlobalCooldownSec : 0.3f, 1);

            F<ChainDirector>(_md, "_chainDirector")?.Reset();
            S(_md, "_closingChainBonus", 1f);
            S(_md, "_pendingChainBonus", 1f);
            _input.Dodge?.Reset();
        }

        static void PlacePlayer(float dist)
        {
            Vector3 b = _boss.position;
            _player.position = new Vector3(b.x, _player.position.y, b.z - dist);
            _player.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            Physics.SyncTransforms();
            var targeting = F<PlayerTargeting>(_md, "_targeting");
            var target = _boss.GetComponentInChildren<Targetable>();
            if (targeting != null && target != null && targeting.Selected != target)
                Call(targeting, "Select", target);
        }

        static CaseInfo Describe(PlaySweepCase c)
        {
            var info = new CaseInfo();
            SkillResolution skill = _skills.Resolve(new[] { c.Verb, c.Adj });
            info.Name = skill.DisplayName;
            info.DamageSkill = skill.BaseDamage > 0f || skill.BaseHeal > 0f;
            info.BossR = Call<float>(_md, "BossBodyRadius");
            float pr = Call<float>(_md, "PlayerBodyRadiusM");
            info.PlayerR = pr < 0.05f ? 0.5f : pr;
            var combat = F<Dovus.Core.Tuning.CombatTuning>(_md, "_combat");
            info.RecoverySec = combat != null ? combat.Sentence.StepForDots(2).RecoverySec : 0.26f;

            var catalog = P<MotionTemplateCatalog>(_md, "MotionCatalog");
            if (catalog == null || !catalog.TryPlay(skill.SkillId, out MotionTemplate template))
                return info;
            object playback = Call(_md, "PreparePositionPlayback", skill, template);
            info.Template = (playback as PositionPlayback?)?.Template ?? template;
            foreach (MotionPhase p in info.Template.Phases)
            {
                info.ExpectedSec += p.DurationSec;
                if (p.OvershootM > 0f)
                    info.PassThrough = true;
                if (p.Hit != null)
                    info.HasHitPhase = true;
            }

            Vector3 b = _boss.position;
            Vector3 s = new(b.x, _player.position.y, b.z - c.StartDistM);
            var runner = new MotionTemplateRunner();
            float stopGap = catalog.Fallbacks.StopGapM;
            runner.Begin(info.Template, s.x, s.y, s.z, 0f, 1f, info.PlayerR, stopGap);
            float maxExc = 0f;
            for (int i = 0; i < 1200 && !runner.Finished; i++)
            {
                runner.Tick(1f / 60f, new MotionTarget(true, b.x, b.z, info.BossR), new MotionStick(false, 0f, 0f));
                maxExc = Mathf.Max(maxExc, Flat(new Vector3(runner.X, 0f, runner.Z) - s).magnitude);
            }
            info.SimFinal = new Vector3(runner.X, s.y, runner.Z);
            info.HasSim = true;
            info.ExpectedCat = DesignCategory(info.Template) ?? Category(s, info.SimFinal, b, maxExc);
            return info;
        }

        static string DesignCategory(MotionTemplate t)
        {
            bool behind = false;
            bool back = false;
            foreach (MotionPhase p in t.Phases)
            {
                if (p.Motion == "blink" || p.Land == "behind" || p.OvershootM > 0f)
                    behind = true;
                if (p.Motion == "return")
                    back = true;
            }
            if (back)
                return "başlangıç";
            return behind ? "arka" : null;
        }

        static string Category(Vector3 start, Vector3 final, Vector3 boss, float maxExcursion)
        {
            float moved = Flat(final - start).magnitude;
            if (maxExcursion > 0.6f && moved < 0.5f)
                return "başlangıç";
            if (moved < InPlaceM)
                return "yerinde";
            Vector3 toStart = Flat(start - boss).normalized;
            Vector3 rel = Flat(final - boss);
            float along = Vector3.Dot(rel, toStart);
            float lateral = Mathf.Abs(toStart.x * rel.z - toStart.z * rel.x);
            if (along < -0.2f)
                return "arka";
            float startDist = Flat(start - boss).magnitude;
            if (rel.magnitude > startDist + 0.3f)
                return "geri";
            if (lateral > 0.6f && lateral > Mathf.Abs(startDist - along))
                return "yan";
            return "ön";
        }

        // ---------------------------------------------------------------- ölçüm

        static bool Performing() => P<bool>(_md, "PerformingAttack");

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
            var f = new Frame
            {
                T = (float)((NowMs - _castMs) / 1000.0),
                P = _player.position,
                B = _boss.position,
                Playing = _body != null && _body.IsDisplacing,
                Performing = Performing(),
                Driver = _driver != null && _driver.IsDisplacing,
                Dodge = _dodge != null && _dodge.IsDisplacing,
                Yaw = _player.eulerAngles.y,
                BossHp = _bossVitals.Hp,
                PlayerHp = _playerVitals != null ? _playerVitals.Hp : 0,
                AllyHp = _ally != null ? _ally.Hp : 0,
                Zones = _md.ZoneDirector != null ? _md.ZoneDirector.ActiveZones.Count : 0,
            };
            SentencePhase sentence = _input.Engine != null ? _input.Engine.State.Phase : SentencePhase.Idle;
            bool drawing = sentence == SentencePhase.Building || sentence == SentencePhase.Recovering;
            var pendingList = F<object>(_md, "_pending") as IList;
            int pending = pendingList?.Count ?? 0;
            if (_scheduledBang < 0f && pending > 0)
                _scheduledBang = (float)((F<double>(pendingList[0], "BangAtWorldMs") - _castMs) / 1000.0);
            f.Bang = !drawing;
            f.Busy = f.Playing || f.Driver || drawing || pending > 0;

            object runner = _body != null ? F<object>(_body, "_runner") : null;
            if (runner is MotionTemplateRunner mr)
            {
                f.Runner = new Vector3(mr.X, mr.Y, mr.Z);
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
            return f;
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
            if (!Running || _stage != Stage.Record)
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

        // ---------------------------------------------------------------- değerlendirme

        static PlaySweepResult NewResult(PlaySweepCase c)
        {
            var r = new PlaySweepResult { Case = c, Weapon = c.Weapon };
            try
            {
                r.Name = _skills.Resolve(new[] { c.Verb, c.Adj }).DisplayName;
            }
            catch
            {
                r.Name = "";
            }
            return r;
        }

        static PlaySweepResult Evaluate(PlaySweepCase c, bool timedOut)
        {
            PlaySweepResult r = NewResult(c);
            r.Cast = true;
            r.Name = _info.Name;
            r.Template = _info.Template != null ? _info.Template.Id : "";
            r.Weapon = _md.EquippedWeapon != null ? _md.EquippedWeapon.Name : c.Weapon;
            r.ExpectedSec = _info.ExpectedSec;
            r.Contact = _info.BossR + _info.PlayerR;
            if (_frames.Count == 0)
            {
                r.Notes.Add("kare yok");
                return r;
            }

            Frame last = _frames[_frames.Count - 1];
            int i0 = _frames.FindIndex(x => x.Playing);
            int i1 = _frames.FindLastIndex(x => x.Playing);
            if (timedOut)
                r.Notes.Add($"zaman aşımı {last.T:F1} sn");

            // 1) isabet ya da amaçlanan etki
            r.Damage = _pre.BossHp - _frames.Min(x => x.BossHp);
            var effects = new List<string>();
            var preBoss = new HashSet<string>();
            var bossKinds = new HashSet<string>();
            var playerKinds = new HashSet<string>();
            foreach (Frame f in _frames)
            {
                foreach (string k in (f.BossKinds ?? "").Split('+'))
                    if (k.Length > 0) bossKinds.Add(k);
                foreach (string k in (f.PlayerKinds ?? "").Split('+'))
                    if (k.Length > 0) playerKinds.Add(k);
            }
            if (bossKinds.Count > 0)
                effects.Add("boss:" + string.Join("+", bossKinds));
            if (playerKinds.Count > 0)
                effects.Add("oyuncu:" + string.Join("+", playerKinds));
            float bossSlow = _frames.Min(x => Mathf.Min(x.BossMove, x.BossAction));
            if (bossSlow < 0.999f)
                effects.Add($"boss hız x{bossSlow:F2}");
            float haste = _frames.Max(x => Mathf.Max(x.PlayerMove, x.PlayerAction));
            if (haste > 1.001f)
                effects.Add($"oyuncu hız x{haste:F2}");
            float shield = _frames.Max(x => x.Shield);
            if (shield > 0.01f)
                effects.Add($"kalkan {shield:F0}");
            int heal = _frames.Max(x => x.PlayerHp) - _pre.PlayerHp;
            if (heal > 0)
                effects.Add($"oyuncu +{heal} can");
            int allyHeal = _frames.Max(x => x.AllyHp) - _pre.AllyHp;
            if (allyHeal > 0)
                effects.Add($"dost +{allyHeal} can");
            int zones = _frames.Max(x => x.Zones);
            if (zones > 0)
                effects.Add($"alan {zones}");
            int roots = _player.gameObject.scene.rootCount - _pre.RootCount;
            r.Effects = string.Join(", ", effects);
            r.Hit = r.Damage > 0.01f || effects.Count > 0;
            if (!r.Hit && !_info.DamageSkill && (!_info.HasHitPhase || _hitOrigins.Count > 0))
            {
                r.Hit = true;
                r.Notes.Add("hasarsız skill (base_damage 0): etki hareketin kendisi");
            }
            else if (!r.Hit)
            {
                r.Notes.Add("hasar yok, etki görülmedi" + (roots > 0 ? $" (+{roots} sahne nesnesi)" : ""));
            }
            if (_hitOrigins.Count > 0)
            {
                var parts = new List<string>();
                for (int i = 0; i < _hitOrigins.Count && i < 6; i++)
                {
                    Frame at = FrameAt(_hitTimes[i]);
                    parts.Add($"{_hitTimes[i]:F2}s@boss{Flat(_hitOrigins[i] - at.B).magnitude:F2}m/oyuncu{Flat(_hitOrigins[i] - at.P).magnitude:F2}m");
                }
                r.Notes.Add($"vuruş {_hitOrigins.Count}: " + string.Join(" ", parts));
            }

            // 2) konum — kalıp bittiği karede, boss'un o anki yerine göre
            Vector3 start = _pre.P;
            int endIdx = i1 >= 0 && i1 + 1 < _frames.Count ? i1 + 1 : _frames.Count - 1;
            Frame end = _frames[endIdx];
            float maxExc = _frames.Take(endIdx + 1).Max(x => Flat(x.P - start).magnitude);
            r.ActualPos = Category(start, end.P, end.B, maxExc);
            r.ExpectedPos = string.IsNullOrEmpty(_info.ExpectedCat) ? "?" : _info.ExpectedCat;
            float simErr = _info.HasSim ? Flat(end.P - _info.SimFinal).magnitude : 0f;
            bool steered = c.Stick.sqrMagnitude > 0.0001f;
            bool bossMoved = Flat(end.B - _pre.B).magnitude > 0.1f;
            r.Position = steered
                         || (r.ActualPos == r.ExpectedPos && (!_info.HasSim || simErr <= SimMatchM || bossMoved));
            r.Notes.Add($"kalıp sonu: merkeze {Flat(end.P - end.B).magnitude:F2} m, başlangıçtan {Flat(end.P - start).magnitude:F2} m, " +
                        $"kalıp simülasyonundan {simErr:F2} m" + (bossMoved ? $", boss {Flat(end.B - _pre.B).magnitude:F2} m kaydı" : "") +
                        (steered ? ", çubukla yönlendirildi" : ""));

            // 3) boss gövdesine girmedi
            r.MinDist = float.MaxValue;
            float minRest = float.MaxValue;
            float minAt = 0f;
            for (int i = 0; i < _frames.Count; i++)
            {
                Frame f = _frames[i];
                float d = Flat(f.P - f.B).magnitude;
                if (d < r.MinDist)
                {
                    r.MinDist = d;
                    minAt = f.T;
                }
                float v = i > 0 ? Flat(f.P - _frames[i - 1].P).magnitude / Mathf.Max(0.001f, f.T - _frames[i - 1].T) : 0f;
                if (v < 1f)
                    minRest = Mathf.Min(minRest, d);
            }
            float limit = r.Contact - InsideTolM;
            r.NotInside = _info.PassThrough ? minRest >= limit : r.MinDist >= limit;
            if (r.MinDist < limit)
                r.Notes.Add($"gövdeye girdi: merkeze {r.MinDist:F2} m (t={minAt:F2}, temas {r.Contact:F2})" + (_info.PassThrough ? " geçiş tasarım gereği" : ""));

            // 4) tek sistem
            float templateMove = 0f;
            float otherMove = 0f;
            float maxJump = 0f;
            float jumpAt = 0f;
            int driverFrames = 0;
            int dodgeFrames = 0;
            for (int i = 1; i < _frames.Count; i++)
            {
                Frame a = _frames[i - 1];
                Frame b = _frames[i];
                Vector3 actual = Flat(b.P - a.P);
                Vector3 tpl = Vector3.zero;
                if (b.Playing && !a.Playing)
                    tpl = Flat(b.Runner - a.P);
                else if (a.Playing)
                    tpl = Flat(b.Runner - a.Runner);
                templateMove += tpl.magnitude;
                otherMove += (actual - tpl).magnitude;
                if (b.Driver) driverFrames++;
                if (b.Dodge) dodgeFrames++;
                if (actual.magnitude > maxJump)
                {
                    maxJump = actual.magnitude;
                    jumpAt = b.T;
                }
            }
            int systems = (templateMove > TemplateMoveM ? 1 : 0) + (otherMove > OtherMoveM ? 1 : 0)
                          + (driverFrames > 0 ? 1 : 0) + (dodgeFrames > 0 ? 1 : 0);
            r.OneSystem = systems <= 1;
            if (!r.OneSystem || otherMove > OtherMoveM)
                r.Notes.Add($"yer değiştiren: kalıp {templateMove:F2} m, başka {otherMove:F2} m, SkillMotionDriver {driverFrames} kare, dodge {dodgeFrames} kare");
            // 7) ışınlanma / titreme yok (blink fazı tasarım gereği sıçrar)
            int teleports = 0;
            int reversals = 0;
            for (int i = 1; i < _frames.Count; i++)
            {
                Vector3 step = Flat(_frames[i].P - _frames[i - 1].P);
                if (step.magnitude > TeleportM && !IsBlinkPhase(_frames[i].Phase))
                    teleports++;
                if (i >= 2)
                {
                    Vector3 prev = Flat(_frames[i - 1].P - _frames[i - 2].P);
                    if (step.magnitude > 0.2f && prev.magnitude > 0.2f && Vector3.Dot(step.normalized, prev.normalized) < -0.5f)
                        reversals++;
                }
            }
            r.NoTeleport = teleports == 0 && reversals < 3;
            string phaseAtJump = FrameAt(jumpAt).Phase;
            if (maxJump > TeleportM)
                r.Notes.Add($"tek karede {maxJump:F2} m sıçrama (t={jumpAt:F2}{(string.IsNullOrEmpty(phaseAtJump) ? "" : ", faz " + phaseAtJump)}" +
                            (IsBlinkPhase(phaseAtJump) ? ", blink tasarım gereği" : "") + ")");
            if (reversals >= 3)
                r.Notes.Add($"titreme: {reversals} karede ≥0,2 m ileri-geri");
            float bossJump = 0f;
            float bossJumpAt = 0f;
            for (int i = 1; i < _frames.Count; i++)
            {
                float step = Flat(_frames[i].B - _frames[i - 1].B).magnitude;
                if (step > bossJump)
                {
                    bossJump = step;
                    bossJumpAt = _frames[i].T;
                }
            }
            if (bossJump > TeleportM)
            {
                r.NoTeleport = false;
                r.Notes.Add($"boss tek karede {bossJump:F2} m sıçradı (t={bossJumpAt:F2})");
            }

            // 5) hata yok
            var errors = _logs.Where(l => l.StartsWith("ERROR") || l.StartsWith("SWEEP-EXC")).ToList();
            r.NoErrors = errors.Count == 0;
            if (errors.Count > 0)
                r.Notes.Add("hata: " + string.Join(" | ", errors.Take(2)));
            var warns = _logs.Where(l => l.StartsWith("WARN")).Distinct().Take(2).ToList();
            if (warns.Count > 0)
                r.Notes.Add("uyarı: " + string.Join(" | ", warns.Select(w => w.Substring(5))));

            // 6) süre
            if (i0 < 0)
            {
                r.OnTime = false;
                r.Notes.Add("kalıp hiç oynamadı");
            }
            else
            {
                float tStart = _frames[i0].T;
                float tEnd = i1 + 1 < _frames.Count ? _frames[i1 + 1].T : _frames[i1].T;
                r.TemplateSec = tEnd - tStart;
                int bangIdx = _frames.FindIndex(x => x.Bang);
                float bang = _scheduledBang >= 0f ? _scheduledBang : bangIdx >= 0 ? _frames[bangIdx].T : tStart;
                float idleAt = last.T;
                for (int i = Math.Max(i1, 0); i < _frames.Count; i++)
                {
                    if (!_frames[i].Busy)
                    {
                        idleAt = _frames[i].T;
                        break;
                    }
                }
                r.TotalSec = idleAt;
                float totalLimit = bang + _info.ExpectedSec + TotalTolSec;
                bool startOk = tStart - bang <= StartTolSec;
                bool templateOk = r.TemplateSec <= _info.ExpectedSec + TemplateTolSec;
                bool totalOk = r.TotalSec <= totalLimit;
                r.OnTime = startOk && templateOk && totalOk && !timedOut;
                if (bang > _info.RecoverySec + StartTolSec)
                    r.Notes.Add($"toparlanma {bang:F2} sn (tablo {_info.RecoverySec:F2})");
                if (!startOk)
                    r.Notes.Add($"kalıp bang'den {tStart - bang:F2} sn sonra başladı");
                if (!templateOk)
                    r.Notes.Add($"kalıp {r.TemplateSec:F2} sn (beklenen {_info.ExpectedSec:F2})");
                if (!totalOk)
                    r.Notes.Add($"cast {r.TotalSec:F2} sn'de bitti (beklenen ≤ {totalLimit:F2})");
            }

            r.Legs = LegSummary();
            return r;
        }

        static bool IsBlinkPhase(string phase)
        {
            if (string.IsNullOrEmpty(phase) || _info?.Template == null)
                return false;
            foreach (MotionPhase p in _info.Template.Phases)
            {
                if (p.Name == phase)
                    return p.Motion == "blink";
            }
            return false;
        }

        static Frame FrameAt(float t)
        {
            Frame best = _frames[0];
            foreach (Frame f in _frames)
            {
                if (f.T > t)
                    break;
                best = f;
            }
            return best;
        }

        /// <summary>Oyuncu ≥1,5 m/s giderken bacak klibinin oynadığı kare oranı.</summary>
        static string LegSummary()
        {
            int moving = 0;
            int legs = 0;
            float ratio = 0f;
            for (int i = 1; i < _frames.Count; i++)
            {
                Frame a = _frames[i - 1];
                Frame b = _frames[i];
                if (!b.Playing)
                    continue;
                float dt = Mathf.Max(0.001f, b.T - a.T);
                float v = Flat(b.P - a.P).magnitude / dt;
                if (v < LegMoveMps)
                    continue;
                moving++;
                bool loco = b.Base == "Locomotion" && b.Speed > 0.05f;
                if (loco)
                {
                    legs++;
                    float scale = _animator != null ? _animator.transform.lossyScale.y : 1f;
                    ratio += b.Speed * Mathf.Abs(b.Playback) * scale / v;
                }
            }
            if (moving == 0)
                return "";
            return legs > 0
                ? $"{legs}/{moving} kare koşu, ayak/gövde hız oranı {ratio / legs:F2}"
                : $"0/{moving} kare koşu";
        }

        // ---------------------------------------------------------------- çıktı

        static string OutDir()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "docs", "play-sweep"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        static void WriteOutputs()
        {
            int pass = Results.Count(r => r.Pass);
            var byWeapon = Results.GroupBy(r => r.Case.Weapon)
                .Select(g => $"{g.Key}: {g.Count(r => r.Pass)}/{g.Count()}");
            LastSummary = $"{pass}/{Results.Count} geçti ({string.Join(", ", byWeapon)})";

            var csv = new StringBuilder();
            csv.AppendLine("kombo,isim,silah,kalip,cast,isabet,konum,govdeye_girmedi,tek_sistem,hata_yok,sure,sicrama_yok,gecti,"
                           + "beklenen_konum,gercek_konum,hasar,etki,min_merkez_m,temas_m,kalip_sn,beklenen_sn,toplam_sn,bacak,notlar");
            foreach (PlaySweepResult r in Results)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    r.Case.Id, Q(r.Name), Q(r.Weapon), Q(r.Template), B(r.Cast), B(r.Hit), B(r.Position),
                    B(r.NotInside), B(r.OneSystem), B(r.NoErrors), B(r.OnTime), B(r.NoTeleport), B(r.Pass),
                    Q(r.ExpectedPos), Q(r.ActualPos), N(r.Damage), Q(r.Effects),
                    N(r.MinDist == float.MaxValue ? 0f : r.MinDist), N(r.Contact), N(r.TemplateSec),
                    N(r.ExpectedSec), N(r.TotalSec), Q(r.Legs), Q(string.Join("; ", r.Notes)),
                }));
            }

            string dir = OutDir();
            string safe = _label.Replace('+', '-');
            File.WriteAllText(Path.Combine(dir, safe + ".csv"), csv.ToString(), new UTF8Encoding(false));

            var detail = new StringBuilder();
            detail.AppendLine("# Play taraması " + _label + " — " + LastSummary);
            foreach (PlaySweepResult r in Results.Where(x => !x.Pass))
            {
                var failed = new List<string>();
                if (!r.Cast) failed.Add("cast");
                if (r.Cast && !r.Hit) failed.Add("isabet");
                if (r.Cast && !r.Position) failed.Add($"konum({r.ExpectedPos}→{r.ActualPos})");
                if (r.Cast && !r.NotInside) failed.Add("gövde");
                if (r.Cast && !r.OneSystem) failed.Add("tek-sistem");
                if (r.Cast && !r.NoErrors) failed.Add("hata");
                if (r.Cast && !r.OnTime) failed.Add("süre");
                if (r.Cast && !r.NoTeleport) failed.Add("sıçrama");
                detail.AppendLine($"{r.Case.Id} {r.Name} [{r.Weapon}] KALDI: {string.Join(", ", failed)} — {string.Join("; ", r.Notes)}");
            }
            detail.AppendLine();
            detail.Append(_detail);
            File.WriteAllText(Path.Combine(dir, safe + "-detay.txt"), detail.ToString(), new UTF8Encoding(false));
        }

        static void WriteTrace(PlaySweepCase c, PlaySweepResult r)
        {
            _detail.AppendLine($"=== {c.Label} {c.Id} {r.Name} [{r.Weapon}] başlangıç {c.StartDistM:F1} m, kalıp {r.Template}, " +
                               $"boss r={_info.BossR:F2}, oyuncu r={_info.PlayerR:F2}, temas {r.Contact:F2}" +
                               (Mathf.Abs(c.BossShiftX) > 0.001f ? $", boss kayması {c.BossShiftX:F1} m" : ""));
            _detail.AppendLine($"  sonuç: {(r.Pass ? "GEÇTİ" : "KALDI")} isabet={r.Hit} konum={r.ExpectedPos}→{r.ActualPos} gövde={r.NotInside} " +
                               $"tek={r.OneSystem} hata={r.NoErrors} süre={r.OnTime} sıçrama_yok={r.NoTeleport} hasar={r.Damage:F1} etki=[{r.Effects}] bacak=[{r.Legs}]");
            foreach (string n in r.Notes)
                _detail.AppendLine("  not: " + n);
            foreach (string l in _logs.Distinct().Take(12))
                _detail.AppendLine("  log: " + l);
            Vector3 start = _pre.P;
            Vector3 back = Flat(start - _pre.B).normalized;
            _detail.AppendLine("  t | faz | merkez_m | taraf(+ön/-arka) | yan_m | hız_mps | yaw | taban_state(norm) | üst_state | Speed | Playback | bossHP | oyuncuHP");
            for (int i = 0; i < _frames.Count; i++)
            {
                Frame f = _frames[i];
                Vector3 rel = Flat(f.P - f.B);
                float v = i > 0 ? Flat(f.P - _frames[i - 1].P).magnitude / Mathf.Max(0.001f, f.T - _frames[i - 1].T) : 0f;
                float side = rel.magnitude > 0.001f ? Vector3.Dot(rel.normalized, back) : 0f;
                float lateral = back.x * rel.z - back.z * rel.x;
                _detail.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "  {0:F3} | {1} | {2:F2} | {3:F2} | {4:F2} | {5:F1} | {6:F0} | {7}({8:F2}) | {9} | {10:F2} | {11:F2} | {12:F1} | {13}{14}",
                    f.T, f.Playing ? f.Phase : "-", rel.magnitude, side, lateral, v, f.Yaw, f.Base, f.BaseNorm,
                    f.Upper, f.Speed, f.Playback, f.BossHp, f.PlayerHp,
                    HitMark(i)));
            }
            _detail.AppendLine();
        }

        static string HitMark(int i)
        {
            float t0 = _frames[i].T;
            float tPrev = i > 0 ? _frames[i - 1].T : -1f;
            var marks = new List<string>();
            for (int k = 0; k < _hitTimes.Count; k++)
            {
                if (_hitTimes[k] > tPrev && _hitTimes[k] <= t0)
                    marks.Add($"VURUŞ@boss{Flat(_hitOrigins[k] - _frames[i].B).magnitude:F2}m");
            }
            return marks.Count > 0 ? " | " + string.Join(" ", marks) : "";
        }

        static string Q(string s) => "\"" + (s ?? "").Replace("\"", "'") + "\"";
        static string B(bool b) => b ? "1" : "0";
        static string N(float f) => f.ToString("F2", CultureInfo.InvariantCulture);

        // ---------------------------------------------------------------- yardımcılar

        static void CollectStateNames()
        {
            _stateNames.Clear();
            if (_animator == null)
                return;
            RuntimeAnimatorController rc = _animator.runtimeAnimatorController;
            if (rc is AnimatorOverrideController oc)
                rc = oc.runtimeAnimatorController;
            if (rc is not UnityEditor.Animations.AnimatorController ac)
                return;
            foreach (var layer in ac.layers)
                AddStates(layer.stateMachine);
        }

        static void AddStates(UnityEditor.Animations.AnimatorStateMachine sm)
        {
            foreach (var s in sm.states)
                _stateNames[Animator.StringToHash(s.state.name)] = s.state.name;
            foreach (var child in sm.stateMachines)
                AddStates(child.stateMachine);
        }

        static string StateName(int hash) => _stateNames.TryGetValue(hash, out string n) ? n : hash.ToString();

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        static T F<T>(object o, string name)
        {
            object v = F(o, name);
            return v is T t ? t : default;
        }

        static object F(object o, string name)
        {
            for (Type t = o?.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, BF | BindingFlags.DeclaredOnly);
                if (f != null)
                    return f.GetValue(o);
            }
            return null;
        }

        static T P<T>(object o, string name)
        {
            for (Type t = o?.GetType(); t != null; t = t.BaseType)
            {
                PropertyInfo p = t.GetProperty(name, BF | BindingFlags.DeclaredOnly);
                if (p != null)
                    return p.GetValue(o) is T v ? v : default;
            }
            return default;
        }

        static void S(object o, string name, object value)
        {
            for (Type t = o?.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, BF | BindingFlags.DeclaredOnly);
                if (f != null)
                {
                    f.SetValue(o, value);
                    return;
                }
            }
        }

        static object Call(object o, string name, params object[] args)
        {
            for (Type t = o.GetType(); t != null; t = t.BaseType)
            {
                foreach (MethodInfo m in t.GetMethods(BF | BindingFlags.DeclaredOnly))
                {
                    if (m.Name == name && m.GetParameters().Length == args.Length)
                        return m.Invoke(o, args);
                }
            }
            throw new MissingMethodException(o.GetType().Name, name);
        }

        static T Call<T>(object o, string name, params object[] args) =>
            Call(o, name, args) is T v ? v : default;
    }
}
#endif
