#if UNITY_EDITOR
using Dovus.App.Sweep;
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
            const string PendingKey = "Dovus.PlaySweep.Pending";
            const string SpeedKey = "Dovus.PlaySweep.Speed";
            const string SpeedSetKey = "Dovus.PlaySweep.SpeedSet";
            const string Speed4Path = "Dovus/Play Sweep/Hız/4x (varsayılan)";
            const string Speed1Path = "Dovus/Play Sweep/Hız/1x (ayıklama)";
            const string ScenePath = "Assets/Scenes/Prototype.unity";
            const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    
            // Ölçüm eşikleri — araç ayarı, oyun hissi değil.
            // Cast arası bekleme SweepPace: iki fizik adımı + bir referans kare.
            // Gözlem kuyruğu oyun süresi; hız duvar saatini kısaltır, bu süreyi değil.
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
            const float LegMoveMps = 1.5f;
    
            public static bool Running { get; private set; }
            public static string Status { get; private set; } = "boşta";
            public static string LastSummary { get; private set; } = "";
            public static string PausedAt { get; private set; } = "";
            /// <summary>Takılmayı ayıklamak için: aşama ve aşamada geçen dünya süresi.</summary>
            public static string StageInfo => Running ? $"{_stage} {(NowMs - _stageMs) / 1000.0:F1} sn" : "-";
            public static readonly List<PlaySweepResult> Results = new();
    
            static List<PlaySweepCase> _cases;
            static float _speed = SweepPace.DefaultSpeed;
            static bool _expandWeapons;
            static bool _paceSaved;
            static float _savedTimeScale = 1f;
            static float _savedFixed = 0.02f;
            static float _savedMaxDelta = 0.333f;
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
            static Vector3 _bossStart;
            static bool _bossStartSet;
            static Vector3 _allyStart;
            static bool _allyStartSet;
            static bool _bossShifted;
            static float _playerShiftDone;
            static Vector3 _playerShiftDir;
            static Snapshot _pre;
            static CaseInfo _info;
            /// <summary>ManifestationDirector._pending[0].BangAtWorldMs, cast anına göre sn; castMult dahil.</summary>
            static float _scheduledBang = -1f;
    
            // Sahne referansları
            static ManifestationDirector _md;
            static HexagonInput _input;
            static Transform _player;
            static Transform _boss;
            static BossReactor _bossReactor;
            static BossVitals _bossVitals;
            static BossDirector _bossDirector;
            static ActorStatus _playerStatus;
            static ActorStatus _bossStatus;
            static PlayerVitals _playerVitals;
            static AllyDummy _ally;
            static MotionTemplateBody _body;
            static DodgeMotion _dodge;
            static Animator _animator;
            static GameClock _clock;
            static SkillMotor _skills;
            static HostileProjectileHost _projectiles;
    
            enum Stage { WaitScene, Idle, Setup, Settle, Record, Done }
    
            struct Frame
            {
                public float T;
                public float Dt;
                public Vector3 P;
                public Vector3 B;
                public Vector3 Runner;
                public bool Playing;
                public bool RunnerDone;
                public bool Performing;
                public bool Busy;
                public bool Bang;
                public bool Dodge;
                public bool Teleport;
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
                public float Feet;
                public float FootGround;
                public float BossFeet;
                public float BossFootGround;
                public float AllyFeet;
                public float AllyFootGround;
                public bool Airborne;
                public bool BossReversed;
                public bool DecoyAggro;
                public int ProjectilesAlive;
                public int ReflectedAlive;
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
                public bool ExpectsReverse;
                public bool ExpectsDecoyAggro;
                /// <summary>mermi_sil ailesi: düzenek mermisi ve etki kontrolleri (plan verisinden).</summary>
                public bool ExpectsErase;
                public bool ExpectsAbsorb;
                public bool ExpectsReflect;
                public bool ExpectsLinkErase;
                public string ExpectedCat = "";
                public int Adj;
                public Vector3 SimFinal;
                public bool HasSim;
                public Vector3 SimStart;
                public float StopGap;
                public string SimAim = "boss";
                public bool AimCaptured;
                /// <summary>Verinin bildirdiği teslim gecikmesi (mark_delay_sec / rise_delay_sec), kalıp başından.</summary>
                public float DeliveryDelaySec;
            }
    
            static PlaySweep()
            {
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                EditorApplication.update += LoopWatchdog;
            }
    
            [MenuItem("Dovus/Play Sweep/144 kombo - Yumruk")]
            static void MenuFist() => Launch("yumruk");
    
            [MenuItem("Dovus/Play Sweep/144 kombo - Kılıç")]
            static void MenuSword() => Launch("kilic");
    
            [MenuItem("Dovus/Play Sweep/144 kombo - Çekiç")]
            static void MenuHammer() => Launch("cekic");
    
            [MenuItem("Dovus/Play Sweep/144 kombo - Kalkan")]
            static void MenuShield() => Launch("kalkan");
    
            [MenuItem("Dovus/Play Sweep/144 kombo - Yay")]
            static void MenuBow() => Launch("yay");
    
            [MenuItem("Dovus/Play Sweep/144 kombo - Top")]
            static void MenuCannon() => Launch("top");
    
            [MenuItem("Dovus/Play Sweep/144 kombo - Asa")]
            static void MenuStaff() => Launch("asa");
    
            [MenuItem("Dovus/Play Sweep/144 kombo - Tılsım")]
            static void MenuTalisman() => Launch("tilsim");
    
            [MenuItem("Dovus/Play Sweep/144 kombo - Büyü Kitabı")]
            static void MenuBook() => Launch("kitap");
    
            [MenuItem("Dovus/Play Sweep/144 kombo - Küre")]
            static void MenuOrb() => Launch("kure");
    
            [MenuItem("Dovus/Play Sweep/144 kombo - Kılıç + Asa")]
            static void MenuSwordStaff() => Launch("kilic+asa");
    
            [MenuItem("Dovus/Play Sweep/1440 kombo - Tüm silahlar", false, 15)]
            static void MenuAllWeapons() => Launch("tum");
    
            [MenuItem(Speed4Path, false, 30)]
            static void ChooseSpeed4() => StoreSpeed(SweepPace.DefaultSpeed);
    
            [MenuItem(Speed4Path, true)]
            static bool ValidateSpeed4()
            {
                Menu.SetChecked(Speed4Path, ChosenSpeed() >= 1.5f);
                return !Running;
            }
    
            [MenuItem(Speed1Path, false, 31)]
            static void ChooseSpeed1() => StoreSpeed(SweepPace.DebugSpeed);
    
            [MenuItem(Speed1Path, true)]
            static bool ValidateSpeed1()
            {
                Menu.SetChecked(Speed1Path, ChosenSpeed() < 1.5f);
                return !Running;
            }
    
            [MenuItem("Dovus/Play Sweep/Durdur")]
            static void MenuStop() => Stop("menü");
    
            static float ChosenSpeed()
            {
                if (!SessionState.GetBool(SpeedSetKey, false))
                    return SweepPace.DefaultSpeed;
                return SweepPace.ClampSpeed(SessionState.GetFloat(SpeedKey, SweepPace.DefaultSpeed));
            }
    
            static void StoreSpeed(float speed)
            {
                SessionState.SetBool(SpeedSetKey, true);
                SessionState.SetFloat(SpeedKey, SweepPace.ClampSpeed(speed));
            }
    
            static float SettleSec => SweepPace.SettleWaitSec(_savedFixed);
            static float IdleHoldSec => SweepPace.IdleHoldSec;
    
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
                    _bossStartSet = false;
                    _allyStartSet = false;
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
                _expandWeapons = preset == "tum";
                if (_expandWeapons)
                {
                    Start(new List<PlaySweepCase>(), preset);
                    return;
                }
                if (SweepMotion.IsAllWeapons(preset))
                {
                    var all = new List<PlaySweepCase>();
                    foreach (string name in SweepWeaponNames)
                        all.AddRange(AllCombos(name));
                    Start(all, preset);
                    return;
                }
                if (preset == "kilic+asa")
                {
                    Start(AllCombos("Kılıç", "Asa"), preset, "Asa");
                    return;
                }
                Start(AllCombos(PresetWeapon(preset)), preset);
            }
    
            static readonly string[] SweepWeaponNames = SweepMotion.WeaponMenuNames;
    
            static string PresetWeapon(string preset) => preset switch
            {
                "yumruk" => "Yumruk",
                "cekic" => "Çekiç",
                "kalkan" => "Kalkan",
                "yay" => "Yay",
                "top" => "Top",
                "asa" => "Asa",
                "tilsim" => "Tılsım",
                "kitap" => "Büyü Kitabı",
                "kure" => "Küre",
                _ => "Kılıç"
            };
    
            /// <summary>144 kombo, rün grubu çiftlerine göre sıralı (az build değişimi).</summary>
            public static List<PlaySweepCase> AllCombos(string weapon, string secondWeapon = "", float startDistM = 3f)
            {
                var list = new List<PlaySweepCase>();
                foreach (SweepComboEntry e in SweepComboCatalog.OrderedCombos(weapon, secondWeapon, startDistM))
                    list.Add(new PlaySweepCase { Verb = e.Verb, Adj = e.Adj, Weapon = e.Weapon, StartDistM = e.StartDistM });
                return list;
            }
    
            /// <summary>Katalogdaki her silah × 144 kombo. Sahne bağlandıktan sonra doldurulur.</summary>
            static List<PlaySweepCase> AllWeaponCases()
            {
                var list = new List<PlaySweepCase>();
                if (_md == null)
                    return list;
                foreach (EquipmentItem w in _md.AvailableWeapons)
                {
                    if (w == null || string.IsNullOrEmpty(w.Name))
                        continue;
                    list.AddRange(AllCombos(w.Name));
                }
                return list;
            }
    
            public static void Start(List<PlaySweepCase> cases, string label, string secondWeapon = "")
            {
                if (!EditorApplication.isPlaying)
                {
                    Status = "Play kapalı";
                    return;
                }
    
                _cases = cases ?? new List<PlaySweepCase>();
                _speed = ChosenSpeed();
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
                ApplyPace();
                Application.logMessageReceived -= OnLog;
                Application.logMessageReceived += OnLog;
                InstallLoop(true);
            }
    
            public static void Stop(string why)
            {
                if (!Running)
                    return;
                Running = false;
                _expandWeapons = false;
                InstallLoop(false);
                ClearPace();
                Application.logMessageReceived -= OnLog;
                if (_bossDirector != null)
                    _bossDirector.enabled = true;
                if (_playerVitals != null)
                    _playerVitals.SuppressDown = false;
                Status = "durdu: " + why;
                if (Results.Count > 0)
                    WriteOutputs();
            }
    }
}
#endif
