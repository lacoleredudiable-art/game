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
        readonly RuleEngineV4CombatContext _combat;
        readonly RuleEngineV4CastHost _cast;
        CompletedSentence _held;
        double _heldSinceRealMs;
        bool _releasing;
        HexagonInputController _hookedInput;
        RuleEngineV4TargetPickRingView _ring;

        public RuleEngineV4HeldCast(RuleEngineV4CombatContext combat, RuleEngineV4CastHost cast)
        {
            _combat = combat;
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
            HexagonInputController input = _combat.Input;
            if (_releasing
                || sentence == null
                || sentence.Phase == SentencePhase.Aborted
                || !sentence.Closing.HasValue
                || sentence.Words == null
                || sentence.Words.Count != RuleEngineV4CastContext.VerbAdjectiveComboLen
                || input == null
                || !input.SkillFingerHeld)
                return false;
            SkillResolution skill = _combat.ResolveSkillWords?.Invoke(sentence.Words) ?? SkillResolution.Empty;
            if (!_cast.OwnsSkill(skill))
                return false;
            if (_held != null)
                Resolve(RuleEngineV4HeldRelease.LaunchDefault, null, default);
            HookInput(input);
            EnsureRing();
            _held = sentence;
            _heldSinceRealMs = HexagonPointerHits.NowRealMs();
            LastRelease = null;
            RuleDrivenVfxSink.FlashRunes(Actor(), skill, _combat.EquippedWeapon);
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
            HexagonInputController input = _combat.Input;
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
                    _combat.SkillAim?.ClearArmedTarget();
                    _combat.ClosingQueue?.OnSentenceCompleted(
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
                _combat.ClosingQueue?.OnSentenceCompleted(sentence);
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
            PlayerTargetingController targeting = _combat.Targeting;
            if (targeting == null || !targeting.TryPickWorld(screen, out target, out RaycastHit solid))
                return false;
            if (target != null)
                return true;
            RuleEngineV4MarkOnlySurfaceHost markSurface = solid.collider != null
                ? solid.collider.GetComponentInParent<RuleEngineV4MarkOnlySurfaceHost>()
                : null;
            if (markSurface == null)
                return false;
            Transform player = _combat.Player;
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
            _ring.transform.SetParent(_combat.HostTransform, true);
        }

        double HeldSec() => (HexagonPointerHits.NowRealMs() - _heldSinceRealMs) / Dovus.Core.Shared.Units.SecToMs;

        Transform Actor() => _combat.Visual != null ? _combat.Visual.transform : null;
    }
}
