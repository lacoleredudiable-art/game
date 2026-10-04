using Dovus.Core.Boss;
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
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.DevTools;
using Dovus.Game.Feel;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Dovus.Game.Casting
{
    /// <summary>
    /// SaÄŸ yarÄ± altÄ±gen Ã§izim girdisi + merkez tap (dÃ¼z vuruÅŸ / erken kapanÄ±ÅŸ) + altÄ±genin
    /// dÄ±ÅŸÄ±ndaki dodge dÃ¼ÄŸmesi (Â§2). YalnÄ±zca Core motoruna bildirir.
    /// </summary>
    public sealed class HexagonInput : MonoBehaviour
    {
        readonly HexagonInputSession _session = new HexagonInputSession();
        CastFeedback _feedback;
        CastGate _gate;
        StrokeCaster _stroke;
        DodgeTrigger _dodgeTrigger;
        PointerRouter _pointer;
        bool _eventsHooked;

        /// <summary>Ä°kinci rÃ¼n hÃ¢lÃ¢ basÄ±lÄ±yken yÃ¼kleme fazÄ± bekler.</summary>
        public bool SkillFingerHeld => _session.Mode == FingerMode.Drawing;

        /// <summary>Q tuÅŸu ve dokunmatik silah dÃ¼ÄŸmesi: silah deÄŸiÅŸtir (K3: KÃ¼re'de de).</summary>
        public event System.Action WeaponSwapRequested;

        /// <summary>R tuÅŸu ve silah dÃ¼ÄŸmesinde uzun basma (KÃ¼re, JSON orb.hold_sec): kÃ¼reyi yollar ya da Ã§aÄŸÄ±rÄ±r.</summary>
        public event System.Action OrbCommandRequested;

        /// <summary>
        /// K3: silah dÃ¼ÄŸmesinin uzun basma komutu eÅŸiÄŸi (sn). 0 = yok â†’ dÃ¼ÄŸme basÄ±nca deÄŸiÅŸtirir.
        /// KÃ¼re kuÅŸanÄ±lÄ±yken JSON orb.hold_sec (0,4).
        /// </summary>
        public System.Func<float> SwapHoldCommandSec
        {
            get => _session.SwapHoldCommandSec;
            set => _session.SwapHoldCommandSec = value;
        }

        /// <summary>Dodge kabul edildi. SÃ¼ren skill kesilir, kalÄ±p konumu hemen bÄ±rakÄ±lÄ±r.</summary>
        public event System.Action SkillCancelledByDodge;

        public PrototypeTuning Tuning
        {
            get
            {
                _session.Tuning ??= new PrototypeTuning();
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

        /// <summary>Motor bir dokunuÅŸu kabul etti (0 = merkez dÃ¼z vuruÅŸ). YalnÄ±z UI juice iÃ§in.</summary>
        public event System.Action<int> DotAccepted;

        /// <summary>Ã‡izim geri bildirimi yazÄ±sÄ± (metin, tanÄ±ndÄ± mÄ±) â€” HexagonView altÄ±genin Ã¼stÃ¼nde gÃ¶sterir.</summary>
        public event System.Action<string, bool> DrawCaption;

        /// <summary>16 EylÃ¼l: CameraOrbitInput'un "bu parmak zaten Ã§iziyor/dodge'a ait" kontrolÃ¼ iÃ§in.</summary>
        public int? ClaimedFingerId => _session.FingerId;
        public int? ClaimedDodgeFingerId => _session.DodgeFingerId;

        /// <summary>Ã–lÃ¼ oyuncu yazamaz ve dodge atamaz (T8.1).</summary>
        public void BindVitals(PlayerVitals vitals) => _session.Vitals = vitals;

        public void BindStatus(ActorStatus status) => _session.Status = status;

        /// <summary>state_machine.player_states â€” Ã§izim/dodge kapÄ±sÄ±.</summary>
        public void BindPlayerStates(PlayerStateMachine states, System.Func<bool> isCasting = null)
        {
            _session.PlayerStates = states;
            _session.IsCasting = isCasting;
        }

        /// <summary>
        /// Ä°kinci rÃ¼n motora verilmeden hedef/menzil kontrolÃ¼. false ise cÃ¼mle ilk rÃ¼nde
        /// kalÄ±r; mana ve cooldown kapanÄ±ÅŸta harcanmadÄ±ÄŸÄ± iÃ§in reddedilen cast Ã¼cretsizdir.
        /// </summary>
        public void BindSkillTargetGate(System.Func<SkillResolution, bool> gate) =>
            _session.SkillTargetGate = gate;

        /// <summary>BaÄŸlama 3: EnforceResourceCost kapÄ±sÄ± + yetersiz mana readout.</summary>
        public void BindResource(PlayerResource resource, ReactionReadout readout, SkillMotor skills = null)
        {
            _session.Resource = resource;
            _session.Readout = readout;
            _session.Skills = skills;
        }

        /// <summary>BaÄŸlama 4: EnforceCooldown kapÄ±sÄ± + soÄŸuma readout (dodge'a dokunmaz).</summary>
        public void BindCooldown(PlayerCooldown cooldown, ReactionReadout readout = null, SkillMotor skills = null)
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
        /// v6 ladder smoke path: aynÄ± SentenceEngine event zincirinden iki rÃ¼n cast eder.
        /// ManifestationDirector normal cast gibi alÄ±r; ayrÄ± hasar/skill yolu yoktur.
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

        public void Bind(
            GameClock clock,
            InkTrail ink,
            SyllableFeedback syllable,
            SentenceDebugHud debugHud,
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
        /// BaÄŸlama 3â€“4 / MCP: cÃ¼mle baÅŸlatma kapÄ±sÄ±. true = devam, false = reddedildi
        /// (readout + deny sesi zaten gÃ¶sterildi). Dodge / recovery'ye dokunmaz.
        /// </summary>
        public bool TryAllowSentenceStart(int verbDot)
        {
            EnsureServices();
            return _gate.TryAllowSentenceStart(verbDot);
        }

        /// <summary>Hedef seÃ§imi, altÄ±gen/dodge/swap tap'lerini dÃ¼nya tap'i saymasÄ±n.</summary>
        public bool IsCombatControlAt(Vector2 pos)
        {
            EnsureServices();
            return _pointer.IsCombatControlAt(pos);
        }

        /// <summary>Sol yarÄ± sanal Ã§ubuktur; oradaki dokunuÅŸ hedef seÃ§mez.</summary>
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
            _session.Tuning ??= new PrototypeTuning();
            _session.Combat ??= new CombatTuning();
            _session.Clock ??= FindAnyObjectByType<GameClock>();

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

            if (_session.Ink == null)
                _session.Ink = FindAnyObjectByType<InkTrail>();
            if (_session.Syllable == null)
                _session.Syllable = FindAnyObjectByType<SyllableFeedback>();
            if (_session.DebugHud == null)
                _session.DebugHud = FindAnyObjectByType<SentenceDebugHud>();
            if (_session.Vitals == null)
                _session.Vitals = FindAnyObjectByType<PlayerVitals>();
            if (_session.Status == null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                    _session.Status = player.GetComponent<ActorStatus>();
            }
            if (_session.Resource == null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                    _session.Resource = player.GetComponent<PlayerResource>();
            }
            if (_session.Cooldown == null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                    _session.Cooldown = player.GetComponent<PlayerCooldown>();
            }
            if (_session.Readout == null)
                _session.Readout = FindAnyObjectByType<ReactionReadout>();
        }
    }
}
