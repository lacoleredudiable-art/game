using Dovus.Core.Boss;
using Dovus.Core.Element;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Casting.Input;
using Dovus.Game.Platform;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.Diagnostics;
using Dovus.Game.Feel;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Dovus.Game.Casting
{
    /// <summary>
    /// Sağ yarı altıgen çizim girdisi + merkez tap (düz vuruş / erken kapanış) + altıgenin
    /// dışındaki dodge düğmesi (§2). Yalnızca Core motoruna bildirir.
    /// </summary>
    public sealed partial class HexagonInputController : MonoBehaviour
    {
        readonly HexagonInputSession _session = new HexagonInputSession();
        CastFeedback _feedback;
        CastGate _gate;
        StrokeCaster _stroke;
        DodgeTrigger _dodgeTrigger;
        PointerRouter _pointer;
        bool _eventsHooked;

        /// <summary>İkinci rün hâlâ basılıyken yükleme fazı bekler.</summary>
        public bool SkillFingerHeld => _session.Mode == FingerMode.Drawing;

        /// <summary>Q tuşu ve dokunmatik silah düğmesi: silah değiştir (K3: Küre'de de).</summary>
        public event System.Action WeaponSwapRequested;

        /// <summary>R tuşu ve silah düğmesinde uzun basma (Küre, JSON orb.hold_sec): küreyi yollar ya da çağırır.</summary>
        public event System.Action OrbCommandRequested;

        /// <summary>
        /// K3: silah düğmesinin uzun basma komutu eşiği (sn). 0 = yok → düğme basınca değiştirir.
        /// Küre kuşanılıyken JSON orb.hold_sec (0,4).
        /// </summary>
        public System.Func<float> SwapHoldCommandSec
        {
            get => _session.SwapHoldCommandSec;
            set => _session.SwapHoldCommandSec = value;
        }

        /// <summary>Dodge kabul edildi. Süren skill kesilir, kalıp konumu hemen bırakılır.</summary>
        public event System.Action SkillCancelledByDodge;

        public GameTuning Tuning
        {
            get
            {
                _session.Tuning ??= new GameTuning();
                return _session.Tuning;
            }
            set => _session.Tuning = value;
        }

        public CombatTuning Combat
        {
            get
            {
                _session.Combat ??= new CombatTuning();
                return _session.Combat;
            }
            set => _session.Combat = value;
        }

        public SentenceEngine Engine => _session.Engine;
        public DodgeState Dodge => _session.Dodge;
        public DodgeChargeBank Charges => _session.Charges;

        /// <summary>Motor bir dokunuşu kabul etti (0 = merkez düz vuruş). Yalnız UI juice için.</summary>
        public event System.Action<int> DotAccepted;

        /// <summary>Çizim geri bildirimi yazısı (metin, tanındı mı) — HexagonView altıgenin üstünde gösterir.</summary>
        public event System.Action<string, bool> DrawCaption;

        /// <summary>16 Eylül: CameraOrbitController'un "bu parmak zaten çiziyor/dodge'a ait" kontrolü için.</summary>
        public int? ClaimedFingerId => _session.FingerId;
        public int? ClaimedDodgeFingerId => _session.DodgeFingerId;

        /// <summary>Ölü oyuncu yazamaz ve dodge atamaz (T8.1).</summary>
        public void BindVitals(PlayerVitalsHost vitals) => _session.Vitals = vitals;

        public void BindStatus(ActorStatusHost status) => _session.Status = status;

        /// <summary>state_machine.player_states — çizim/dodge kapısı.</summary>
        public void BindPlayerStates(PlayerStateMachine states, System.Func<bool> isCasting = null)
        {
            _session.PlayerStates = states;
            _session.IsCasting = isCasting;
        }

        /// <summary>
        /// İkinci rün motora verilmeden hedef/menzil kontrolü. false ise cümle ilk ründe
        /// kalır; mana ve cooldown kapanışta harcanmadığı için reddedilen cast ücretsizdir.
        /// </summary>
        public void BindSkillTargetGate(System.Func<SkillResolution, bool> gate) =>
            _session.SkillTargetGate = gate;

        /// <summary>Bağlama 3: EnforceResourceCost kapısı + yetersiz mana readout.</summary>
        public void BindResource(PlayerResourceHost resource, ReactionReadoutHud readout, SkillMotor skills = null)
        {
            _session.Resource = resource;
            _session.Readout = readout;
            _session.Skills = skills;
        }

        /// <summary>Bağlama 4: EnforceCooldown kapısı + soğuma readout (dodge'a dokunmaz).</summary>
        public void BindCooldown(PlayerCooldownHost cooldown, ReactionReadoutHud readout = null, SkillMotor skills = null)
        {
            _session.Cooldown = cooldown;
            if (readout != null)
                _session.Readout = readout;
            if (skills != null)
                _session.Skills = skills;
        }

        public bool TrySetLoadout(RuneLoadout loadout)
        {
            EnsureRuntime();
            if (_session.Engine == null || !_session.Engine.TrySetLoadout(loadout))
                return false;
            _session.CenterStrikeArmed = true;
            SyncPlayerStateFromWorld();
            return true;
        }

        /// <summary>
        /// v6 ladder smoke path: aynı SentenceEngine event zincirinden iki rün cast eder.
        /// ManifestationDirector normal cast gibi alır; ayrı hasar/skill yolu yoktur.
        /// </summary>
        public bool TryDebugCastSkill(int verbRuneId, int adjectiveRuneId)
        {
            EnsureRuntime();
            if (_session.Engine == null || _session.InputLocked)
                return false;

            int verbSlot = FindSlot(_session.Engine.Loadout, verbRuneId);
            int adjectiveSlot = FindSlot(_session.Engine.Loadout, adjectiveRuneId);
            if (verbSlot <= 0 || adjectiveSlot <= 0)
                return false;

            if (_session.Engine.State.Phase == SentencePhase.Building)
                _session.Engine.Abort();
            double worldMs = _session.Clock != null ? _session.Clock.Director.WorldTimeMs : 0;
            _session.Engine.OnDotTouched(verbSlot, worldMs);
            DotAccepted?.Invoke(verbSlot);
            _session.Syllable?.PlayForDot(verbSlot, 1);
            SkillResolution prospective = _session.Skills.Resolve(new[] { verbRuneId, adjectiveRuneId });
            if (_session.SkillTargetGate != null && !_session.SkillTargetGate(prospective))
            {
                _session.Engine.Abort();
                _stroke.FlushInkBreak();
                return false;
            }
            _session.Engine.OnDotTouched(adjectiveSlot, worldMs);
            DotAccepted?.Invoke(adjectiveSlot);
            _session.Syllable?.PlayForDot(adjectiveSlot, 2);
            _stroke.FlushInkBreak();
            DebugConfig.DevLog($"[ElementSystem] smoke cast accepted: {verbRuneId}-{adjectiveRuneId}");
            return true;
        }

        static int FindSlot(RuneLoadout loadout, int runeId)
        {
            if (loadout == null)
                return 0;
            for (int slot = 1; slot <= RuneLoadout.SlotCount; slot++)
                if (loadout.RuneIdAtSlot(slot) == runeId)
                    return slot;
            return 0;
        }

        public void ConfigureDebugAndFeel(IDebugPanelInputState debugPanel, FeelHapticsRuntime haptics)
        {
            _session.DebugPanel = debugPanel;
            _session.Haptics = haptics;
        }

        public void Bind(
            GameClockHost clock,
            InkTrailView ink,
            SyllableFeedbackView syllable,
            ISentenceDebugSink debugHud,
            SkillMotor skills = null,
            RuneLoadout loadout = null)
        {
            _session.Clock = clock;
            _session.Ink = ink;
            _session.Syllable = syllable;
            _session.DebugHud = debugHud;
            _session.Skills = skills ?? _session.Skills;
            _session.Combat ??= new CombatTuning();
            if (_session.Skills != null && _session.Skills.MaxComboLength > 0)
                _session.Combat.Sentence.MaxSentenceDots = _session.Skills.MaxComboLength;
            if (_session.SentenceHooked && _session.Engine != null)
            {
                _session.Engine.SentenceCompleted -= OnSentenceCompleted;
                _session.SentenceHooked = false;
            }

            _session.Engine = new SentenceEngine(
                _session.Combat.Sentence,
                loadout ?? _session.Skills?.DefaultLoadout ?? RuneLoadout.Sequential);
            _session.Dodge = new DodgeState(_session.Combat.Dodge);
            _session.Charges = new DodgeChargeBank(_session.Combat.Dodge);
            _session.Engine.SentenceCompleted += OnSentenceCompleted;
            _session.SentenceHooked = true;
        }

        void OnEnable()
        {
            EnsureServices();
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerMove += OnFingerMove;
            Touch.onFingerUp += OnFingerUp;
            _eventsHooked = true;
        }

        void OnDisable()
        {
            if (_eventsHooked)
            {
                Touch.onFingerDown -= OnFingerDown;
                Touch.onFingerMove -= OnFingerMove;
                Touch.onFingerUp -= OnFingerUp;
                _eventsHooked = false;
            }

            EnhancedTouchSupport.Disable();
            _pointer?.CancelOnDisable();
        }

        void OnDestroy()
        {
            if (_session.SentenceHooked && _session.Engine != null)
            {
                _session.Engine.SentenceCompleted -= OnSentenceCompleted;
                _session.SentenceHooked = false;
            }
        }

        void OnSentenceCompleted(CompletedSentence sentence) => _stroke?.OnSentenceCompleted(sentence);

        void Update()
        {
            EnsureRuntime();
            _dodgeTrigger.TickCharges();
            SyncPlayerStateFromWorld();

            if (_session.Engine != null && _session.Clock != null)
                _session.Engine.Tick(_session.Clock.WorldDeltaMs);

            _stroke.FlushInkBreak();

            if (_session.InputLocked || _pointer.PanelBlocking)
            {
                if (_session.FingerId.HasValue || _session.MouseHeld)
                    _pointer.CancelAllPointers();
                return;
            }

            _pointer.HandleKeyboardDodge();
            _pointer.HandleMouse();
            _stroke.TickStrokeSettle();
            _pointer.TickSwapHold();
            _stroke.TickDwell();
        }

        void SyncPlayerStateFromWorld()
        {
            if (_session.PlayerStates == null)
                return;

            int worldMs = _session.Clock != null ? (int)_session.Clock.Director.WorldTimeMs : 0;
            bool isDead = _session.Vitals != null && _session.Vitals.IsDown;
            bool isStunned = false;
            bool isRooted = false;
            if (_session.Status != null)
            {
                var board = _session.Status.Board;
                isStunned = board.Has(StatusKind.Stun) || board.Has(StatusKind.Stasis) || board.Has(StatusKind.Fear);
                isRooted = board.Has(StatusKind.Root);
            }

            bool isDodging = _session.Dodge != null && _session.Dodge.IsActive(worldMs);
            bool isCasting = _session.IsCasting != null && _session.IsCasting();
            bool isDrawing = _session.Engine != null && _session.Engine.State.Phase == SentencePhase.Building;
            bool isRecovering = _session.Engine != null && _session.Engine.State.Phase == SentencePhase.Recovering;

            _session.PlayerStates.SyncWorld(
                isDead, isStunned, isDodging, isRooted, isCasting, isDrawing, isRecovering);
        }

        /// <summary>
        /// Bağlama 3–4 / MCP: cümle başlatma kapısı. true = devam, false = reddedildi
        /// (readout + deny sesi zaten gösterildi). Dodge / recovery'ye dokunmaz.
        /// </summary>
        public bool TryAllowSentenceStart(int verbDot)
        {
            EnsureServices();
            return _gate.TryAllowSentenceStart(verbDot);
        }

        /// <summary>Hedef seçimi, altıgen/dodge/swap tap'lerini dünya tap'i saymasın.</summary>
        public bool IsCombatControlAt(Vector2 pos)
        {
            EnsureServices();
            return _pointer.IsCombatControlAt(pos);
        }

        /// <summary>Sol yarı sanal çubuktur; oradaki dokunuş hedef seçmez.</summary>
        public bool IsStickHalf(Vector2 pos)
        {
            EnsureServices();
            return _pointer.IsStickHalf(pos);
        }

        void OnFingerDown(Finger finger) => _pointer?.OnFingerDown(finger);
        void OnFingerMove(Finger finger) => _pointer?.OnFingerMove(finger);
        void OnFingerUp(Finger finger) => _pointer?.OnFingerUp(finger);

        void EnsureServices()
        {
            if (_pointer != null)
                return;

            _session.RaiseDotAccepted = dot => DotAccepted?.Invoke(dot);
            _session.RaiseDrawCaption = (text, ok) => DrawCaption?.Invoke(text, ok);
            _session.RaiseWeaponSwapRequested = () => WeaponSwapRequested?.Invoke();
            _session.RaiseOrbCommandRequested = () => OrbCommandRequested?.Invoke();
            _session.RaiseSkillCancelledByDodge = () => SkillCancelledByDodge?.Invoke();

            _feedback = new CastFeedback(_session);
            _gate = new CastGate(_session, _feedback);
            _stroke = new StrokeCaster(_session, _gate, _feedback);
            _dodgeTrigger = new DodgeTrigger(_session, () => _stroke.FlushInkBreak(), SyncPlayerStateFromWorld);
            _pointer = new PointerRouter(_session, _stroke, _dodgeTrigger, SyncPlayerStateFromWorld);
        }

        void EnsureRuntime()
        {
            EnsureServices();
            _session.Tuning ??= new GameTuning();
            _session.Combat ??= new CombatTuning();
            if (_session.Dodge == null)
                _session.Dodge = new DodgeState(_session.Combat.Dodge);
            if (_session.Charges == null)
                _session.Charges = new DodgeChargeBank(_session.Combat.Dodge);

            if (_session.Engine == null)
            {
                _gate.EnsureSkills();
                if (_session.Skills != null && _session.Skills.MaxComboLength > 0)
                    _session.Combat.Sentence.MaxSentenceDots = _session.Skills.MaxComboLength;
                _session.Engine = new SentenceEngine(
                    _session.Combat.Sentence,
                    _session.Skills?.DefaultLoadout ?? RuneLoadout.Sequential);
                _session.Engine.SentenceCompleted += OnSentenceCompleted;
                _session.SentenceHooked = true;
            }

            if (_session.Status == null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                    _session.Status = player.GetComponent<ActorStatusHost>();
            }
            if (_session.Resource == null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                    _session.Resource = player.GetComponent<PlayerResourceHost>();
            }
            if (_session.Cooldown == null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                    _session.Cooldown = player.GetComponent<PlayerCooldownHost>();
            }
        }
    }
}
