using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Tuning;
using Dovus.Game.Audio;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using Dovus.Game.Feel;
using Dovus.Game.Hud;
using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// Oyuncu dodge hakları, i-frame sorgusu ve mükemmel sıyırma.
    /// Yavaşlama yalnız bu oyuncunun animatörü ve yerel kameradadır;
    /// Time.timeScale değişmez (çok oyunculu).
    /// </summary>
    public sealed class PlayerDodgeController : MonoBehaviour
    {
        GameClockHost _clock;
        HexagonInputController _input;
        FollowCameraController _camera;
        ReactionReadoutHud _readout;
        ActorView _visual;
        NextHitBuff _nextHit = new NextHitBuff();
        int _perfectPressMs = int.MinValue;
        float _feelUntilUnscaled = -1f;
        float _savedAnimSpeed = 1f;
        bool _animSlowed;
        bool _feelHooked;
        SfxDirector _sfx;

        public NextHitBuff NextHit => _nextHit;

        public void BindSfx(SfxDirector sfx) => _sfx = sfx;

        /// <summary>
        /// Skill hareketi / çağırma anı dokunulmazlığı (F1): 3-7 dash 400 ms, ışınlanma 220 ms,
        /// 11-10 çağırma anı... Stasis değil — oyuncu donmaz, yalnız pencere içindeki vuruş yutulur.
        /// </summary>
        public SkillIframeWindow SkillIframe { get; } = new SkillIframeWindow();

        public void OpenSkillIframe(int durationMs)
        {
            if (durationMs > 0)
                SkillIframe.Open(WorldMs(), durationMs);
        }

        public bool IsSkillInvulnerable => SkillIframe.IsActive(WorldMs());

        /// <summary>Dodge ya da skill i-frame'i açık mı (mermi bunu okur; mükemmel sıyırma ödülü vermez).</summary>
        public static bool IsInvulnerableNow(Component host)
        {
            PlayerDodgeController rig = host != null ? host.GetComponent<PlayerDodgeController>() : null;
            return rig != null && (rig.IsInvulnerable || rig.IsSkillInvulnerable);
        }

        public void Bind(
            GameClockHost clock,
            HexagonInputController input,
            FollowCameraController camera,
            ReactionReadoutHud readout,
            CombatFeelDirector feel)
        {
            _clock = clock;
            _input = input;
            _camera = camera;
            _readout = readout;
            if (feel != null && !_feelHooked)
            {
                feel.Exchanged += OnExchange;
                _feelHooked = true;
            }
        }

        /// <summary>Hasar borusu bunu okur. true ise bu karede vuruş yutulur.</summary>
        public bool IsInvulnerable
        {
            get
            {
                DodgeState dodge = _input != null ? _input.Dodge : null;
                return dodge != null && dodge.IsInvulnerable(WorldMs());
            }
        }

        /// <summary>
        /// Gelen vuruşun tek sorusu. dodgeable false ise i-frame yutmaz.
        /// true dönerse hasar yazılmamalı; mükemmel pencereyse ödül burada bir kez verilir.
        /// </summary>
        public static bool BlocksIncoming(Component host, bool dodgeable = true)
        {
            if (host == null || !dodgeable)
                return false;
            PlayerDodgeController rig = host.GetComponent<PlayerDodgeController>();
            if (rig == null)
                return false;
            // Skill i-frame'i ödülsüz yutar; dodge penceresi mükemmel sıyırmayı burada değerlendirir.
            return rig.TryAbsorbHit() || rig.IsSkillInvulnerable;
        }

        /// <summary>Hasar borusu oyuncunun sıradaki vuruşunda bir kez çarpar. İkinci çağrı 1.</summary>
        public static float ConsumeNextHit(Component player)
        {
            if (player == null)
                return 1f;
            PlayerDodgeController rig = player.GetComponent<PlayerDodgeController>();
            return rig != null ? rig._nextHit.Consume(rig.WorldMs()) : 1f;
        }

        public bool TryAbsorbHit()
        {
            if (!IsInvulnerable)
                return false;
            ConsiderPerfect(WorldMs());
            return true;
        }

        void OnExchange(ExchangeResult result)
        {
            if (result.Outcome != ExchangeOutcome.Dodged)
                return;
            DodgeState dodge = _input != null ? _input.Dodge : null;
            if (dodge == null || !dodge.PressTimeMs.HasValue)
                return;
            int press = dodge.PressTimeMs.Value;
            int strike = press + result.GapMs;
            if (!InPerfect(press, strike))
                return;
            // O2: okumadaki derece zaten "PERFECT" yazıyor (aynı pencere) — ikinci yazı yok.
            TriggerPerfect(press, announce: false);
        }

        void ConsiderPerfect(int strikeMs)
        {
            DodgeState dodge = _input != null ? _input.Dodge : null;
            if (dodge == null || !dodge.PressTimeMs.HasValue)
                return;
            int press = dodge.PressTimeMs.Value;
            if (!InPerfect(press, strikeMs))
                return;
            // Mermi/skill emişi derece üretmez: tek yazı burada.
            TriggerPerfect(press, announce: true);
        }

        bool InPerfect(int pressMs, int strikeMs)
        {
            DodgeTuning tuning = Tuning;
            return PerfectDodgeRule.InWindow(
                pressMs,
                strikeMs,
                tuning.IframeStartMs,
                tuning.IframeMs,
                tuning.PerfectWindowMs);
        }

        void TriggerPerfect(int pressMs, bool announce)
        {
            if (_perfectPressMs == pressMs)
                return;
            _perfectPressMs = pressMs;

            DodgeTuning tuning = Tuning;
            DodgeChargeBank bank = _input != null ? _input.Charges : null;
            if (bank != null)
            {
                bank.RechargeMult = _input.Dodge != null ? _input.Dodge.CooldownMult : 1f;
                bank.Refund(tuning.PerfectChargeRefund);
            }

            _nextHit.Arm(tuning.PerfectNextHitMult, WorldMs(), tuning.PerfectNextHitWindowMs);
            PlayLocalFeel(tuning.PerfectFeelSec);
            // O2: ayrı OnGUI "PERFECT" etiketi kaldırıldı (çift yazı + her kare yeni GUIStyle).
            _sfx?.Play(SfxLibrary.PerfectDodge);
        }

        void PlayLocalFeel(float seconds)
        {
            float dur = Mathf.Max(PlayerDodgeControllerDefaults.MinDodgeDurationSec, seconds);
            _feelUntilUnscaled = Time.unscaledTime + dur;
            _camera?.Punch(PlayerDodgeControllerDefaults.CameraPunchAmplitude, PlayerDodgeControllerDefaults.CameraPunchFrequency, PlayerDodgeControllerDefaults.CameraPunchDurationSec, PlayerDodgeControllerDefaults.CameraPunchDecay);
            if (_visual == null)
                _visual = GetComponent<ActorView>();
            Animator anim = _visual != null ? _visual.Animator : null;
            if (anim == null)
                return;
            if (!_animSlowed)
                _savedAnimSpeed = anim.speed <= PlayerDodgeControllerDefaults.MinAnimSpeed ? 1f : anim.speed;
            anim.speed = PlayerDodgeControllerDefaults.DodgeAnimSpeed;
            _animSlowed = true;
        }

        void Update()
        {
            if (!_animSlowed)
                return;
            if (Time.unscaledTime < _feelUntilUnscaled)
                return;
            Animator anim = _visual != null ? _visual.Animator : null;
            if (anim != null)
                anim.speed = _savedAnimSpeed;
            _animSlowed = false;
        }

        void OnDisable()
        {
            if (!_animSlowed)
                return;
            Animator anim = _visual != null ? _visual.Animator : null;
            if (anim != null)
                anim.speed = _savedAnimSpeed;
            _animSlowed = false;
        }

        int WorldMs() => _clock != null ? (int)_clock.Director.WorldTimeMs : 0;

        DodgeTuning Tuning =>
            _input != null && _input.Combat != null && _input.Combat.Dodge != null
                ? _input.Combat.Dodge
                : new DodgeTuning();
    }
}
