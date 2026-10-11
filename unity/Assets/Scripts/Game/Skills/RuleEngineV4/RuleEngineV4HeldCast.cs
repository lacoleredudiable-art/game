using Dovus.Core.Grammar;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using Dovus.Game.Casting.Input;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>
    /// Basılı tutarak hedef seçme: 2 noktalı v4 kombo 2. rünle çözülür ama parmak kalkana dek tutulur.
    /// Harf flaşı hemen; kapanış (poz, hareket kilidi, anim, mana/bekleme) parmak kalkınca.
    /// hold_to_target_sec dolunca parmağın altındaki hedef PlayerTargetingController dünya seçimiyle bulunur.
    /// </summary>
    public sealed class RuleEngineV4HeldCast
    {
        readonly ManifestationDirector _director;
        readonly RuleEngineV4CastHost _cast;
        CompletedSentence _held;
        double _heldSinceRealMs;
        bool _releasing;
        HexagonInputController _hookedInput;
        RuleEngineV4TargetPickRingView _ring;

        public RuleEngineV4HeldCast(ManifestationDirector director, RuleEngineV4CastHost cast)
        {
            _director = director;
            _cast = cast;
        }

        public bool IsHolding => _held != null;

        public bool PickOpen => _held != null
            && RuleEngineV4HoldToTarget.PickOpen(HeldSec(), _cast.CatalogAccess.Catalog.Globals.HoldToTargetSec);

        public RuleEngineV4HeldRelease? LastRelease { get; private set; }

        internal RuleEngineV4TargetPickRingView Ring => _ring;

        /// <summary>Kapanış öncesi: parmak hâlâ basılıysa v4 komboyu tutar (true = kapanış ertelendi).</summary>
        public bool TryHold(CompletedSentence sentence)
        {
            HexagonInputController input = _director._input;
            if (_releasing
                || !RuleEngineV4Feature.Enabled
                || sentence == null
                || sentence.Phase == SentencePhase.Aborted
                || !sentence.Closing.HasValue
                || sentence.Words == null
                || sentence.Words.Count != RuleEngineV4CastContext.VerbAdjectiveComboLen
                || input == null
                || !input.SkillFingerHeld)
                return false;
            SkillResolution skill = _director.ResolveSkillWords(sentence.Words);
            if (!_cast.OwnsSkill(skill))
                return false;
            if (_held != null)
                Resolve(RuleEngineV4HeldRelease.LaunchDefault, null, default);
            HookInput(input);
            EnsureRing();
            _held = sentence;
            _heldSinceRealMs = HexagonPointerHits.NowRealMs();
            LastRelease = null;
            RuleDrivenVfxSink.FlashRunes(Actor(), skill, _director._equippedWeapon);
            return true;
        }

        void OnFingerReleased(SkillFingerRelease release)
        {
            if (_held == null)
                return;
            bool pickOpen = PickOpen;
            TargetableHost target = null;
            RuleEngineV4SurfaceAim surface = default;
            bool picked = pickOpen && !release.Cancelled && !release.OnHexagon
                && TryPickUnder(release.ScreenPos, out target, out surface);
            RuleEngineV4HeldRelease decision = RuleEngineV4HoldToTarget.Decide(
                pickOpen, release.Cancelled, release.OnCenter, release.OnHexagon, picked);
            Resolve(decision, target, surface);
        }

        /// <summary>Dodge / girdi kesildi: tutulan skill çıkmaz.</summary>
        public void Cancel()
        {
            if (_held != null)
                Resolve(RuleEngineV4HeldRelease.Cancel, null, default);
        }

        /// <summary>Her kare (halka görünümünden): hedef seçme açıksa parmağın altındaki hedefe halka.</summary>
        internal void Tick()
        {
            if (_held == null)
            {
                _ring.Hide();
                return;
            }
            HexagonInputController input = _director._input;
            if (input == null || !input.SkillFingerHeld)
            {
                Cancel();
                return;
            }
            Vector2 pos = input.PointerScreenPos;
            if (!PickOpen || OnHexagon(input, pos) || !TryPickUnder(pos, out TargetableHost target, out RuleEngineV4SurfaceAim surface))
            {
                _ring.Hide();
                return;
            }
            if (target != null)
                _ring.ShowOnTarget(target);
            else
                _ring.ShowOnSurface(surface.Point, surface.Normal);
        }

        void Resolve(RuleEngineV4HeldRelease decision, TargetableHost target, in RuleEngineV4SurfaceAim surface)
        {
            CompletedSentence sentence = _held;
            _held = null;
            _ring?.Hide();
            LastRelease = decision;
            _releasing = true;
            try
            {
                if (decision == RuleEngineV4HeldRelease.Cancel)
                {
                    RuleDrivenVfxSink.ClearRunePreflash(Actor());
                    _director._skillAim?.ClearArmedTarget();
                    _director._closingQueue.OnSentenceCompleted(
                        new CompletedSentence(sentence.Verb, sentence.Words, SentencePhase.Aborted, null));
                    return;
                }
                _cast.NextTarget = decision switch
                {
                    RuleEngineV4HeldRelease.LaunchAtPicked when target != null =>
                        new RuleEngineV4HeldTarget(RuleEngineV4HeldTarget.Kind.Target, target),
                    RuleEngineV4HeldRelease.LaunchAtPicked =>
                        new RuleEngineV4HeldTarget(RuleEngineV4HeldTarget.Kind.Surface, surface: surface),
                    RuleEngineV4HeldRelease.LaunchAuto => new RuleEngineV4HeldTarget(RuleEngineV4HeldTarget.Kind.Auto),
                    _ => default,
                };
                _director._closingQueue.OnSentenceCompleted(sentence);
            }
            finally
            {
                _cast.NextTarget = default;
                _releasing = false;
            }
        }

        bool TryPickUnder(Vector2 screen, out TargetableHost target, out RuleEngineV4SurfaceAim surface)
        {
            target = null;
            surface = default;
            PlayerTargetingController targeting = _director._targeting;
            if (targeting == null || !targeting.TryPickWorld(screen, out target, out RaycastHit solid))
                return false;
            if (target != null)
                return true;
            RuleEngineV4MarkOnlySurfaceHost markSurface = solid.collider != null
                ? solid.collider.GetComponentInParent<RuleEngineV4MarkOnlySurfaceHost>()
                : null;
            if (markSurface == null)
                return false;
            Transform player = _director.MechanicsPlayer;
            float distanceM = player != null ? Vector3.Distance(player.position, solid.point) : 0;
            surface = new RuleEngineV4SurfaceAim(markSurface, solid.point, solid.normal, distanceM);
            return true;
        }

        static bool OnHexagon(HexagonInputController input, Vector2 pos) =>
            HexagonPointerHits.HitCenter(pos, input.Tuning) || HexagonPointerHits.HitDot(pos, input.Tuning).HasValue;

        void HookInput(HexagonInputController input)
        {
            if (_hookedInput == input)
                return;
            if (_hookedInput != null)
            {
                _hookedInput.SkillFingerReleased -= OnFingerReleased;
                _hookedInput.SkillCancelledByDodge -= Cancel;
            }
            _hookedInput = input;
            input.SkillFingerReleased += OnFingerReleased;
            input.SkillCancelledByDodge += Cancel;
        }

        void EnsureRing()
        {
            if (_ring != null)
                return;
            EjderVfxBatchHost batch = RuleDrivenVfxSink.Resolve(Actor())?.Batch;
            _ring = RuleEngineV4TargetPickRingView.Create(batch, _cast.OwnerColor, Tick);
            _ring.transform.SetParent(_director.transform, true);
        }

        double HeldSec() => (HexagonPointerHits.NowRealMs() - _heldSinceRealMs) / Dovus.Core.Shared.Units.SecToMs;

        Transform Actor() => _director._visual != null ? _director._visual.transform : null;
    }
}
