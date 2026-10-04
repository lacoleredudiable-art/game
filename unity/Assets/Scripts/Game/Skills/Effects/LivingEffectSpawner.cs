using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Casting;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Presentation;
using Dovus.Core.Tuning;
using Dovus.Game.Feel;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Effects
{
    public sealed class LivingEffectSpawner
    {
        readonly ILivingEffectSpawnerHost _host;
        readonly List<LivingEffectView> _active = new();

        public LivingEffectSpawner(ILivingEffectSpawnerHost host) => _host = host;

        public int ActiveCount => _active.Count;

        public void PulseActor(Rune rune, IReadOnlyList<SentenceWord> words, double worldMs)
        {
            SkillResolution skill = SkillResolution.Empty;
            if (_host.Skills != null && words != null && words.Count > 0)
                skill = _host.ResolveSkillWords(words);
            _host.Aim.FaceAim(skill);
            _host.Pose?.PulseRune(rune, worldMs);
            if (_host.Visual == null)
                return;
            _host.SyncVisualDelivery();

            EffectSilhouette s;
            if (!skill.IsEmpty)
                s = SilhouetteBuilder.FromSkill(skill, _host.Combat?.Manifestation);
            else if (words != null && words.Count > 0)
                s = SilhouetteBuilder.FromWords(words, _host.Combat?.Manifestation);
            else
                s = default;

            if (!skill.IsEmpty && !string.IsNullOrEmpty(skill.AnimationType))
                _host.Visual.PulseAnimationType(skill.AnimationType, s);
            else
                _host.Visual.PulseRune(rune, s);
        }

        public void ApplyWindowCue()
        {
            if (_host.BuildingView == null)
                return;

            var state = _host.Engine.State;
            if (state.Phase != SentencePhase.Building || state.ArmedWindowMs <= 0.5)
            {
                _host.BuildingView.SetWindowCue(1f);
                return;
            }

            _host.BuildingView.SetWindowCue((float)(state.RemainingWindowMs / state.ArmedWindowMs));
        }

        public LivingEffectView SpawnEffect(IReadOnlyList<SentenceWord> words, double worldMs, bool basicStrike = false)
        {
            _ = worldMs;
            Vector3 pos = _host.Player.position;
            Vector3 facing;
            if (basicStrike)
            {
                _host.Aim.DirectionalAttack = false;
                _host.Aim.CaptureBasicFacing();
                facing = _host.Aim.FlatBodyForward();
                if (_host.Aim.CastFacingTarget != null)
                {
                    Vector3 toTarget = _host.Aim.CastFacingTarget.position - pos;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.0001f)
                        facing = toTarget.normalized;
                }

                if (_host.Player != null && facing.sqrMagnitude > 0.0001f)
                    _host.Player.rotation = Quaternion.LookRotation(facing, Vector3.up);
            }
            else if (_host.Aim.DirectionalAttack)
                facing = _host.Aim.ResolveAimFacing(pos);
            else
                facing = _host.Aim.FacingOrBody(
                    _host.Aim.CastFacingTarget != null
                        ? _host.Aim.CastFacingTarget
                        : _host.Aim.AttackLockTarget(),
                    pos);

            ManifestationTuning man = _host.Combat.Manifestation;
            if (basicStrike)
                man = man.WithBasicStrikeProfile();

            var logic = new LivingEffect(
                words[0].Rune,
                pos.x,
                pos.z,
                facing.x,
                facing.z,
                words,
                man);
            if (basicStrike)
                _host.StopBasicCannonAtFirstBody(logic, pos, facing);

            var go = new GameObject(basicStrike ? "LivingEffect_BasicStrike" : "LivingEffect_" + words[0].Rune);
            go.transform.SetParent(_host.DirectorTransform, false);
            var view = go.AddComponent<LivingEffectView>();
            view.Bind(logic, man, _host.Colors, basicStrike);
            if (!basicStrike)
                ApplySkillWorldPlan(logic, words);
            ApplySkillTint(view, words);
            _active.Add(view);
            return view;
        }

        public void ApplySkillWorldPlan(LivingEffect logic, IReadOnlyList<SentenceWord> words)
        {
            if (logic == null || words == null || words.Count == 0 || _host.Skills == null)
                return;

            SkillResolution skill = _host.ResolveSkillWords(words);
            if (skill.IsEmpty)
                return;

            _host.EnsurePresentationCatalog();
            ManifestationTuning man = _host.Combat != null ? _host.Combat.Manifestation : new ManifestationTuning();
            LivingEffectPlan plan = SkillWorldPlanner.Build(skill, _host.PresentationCatalog, man);
            logic.ApplyPlan(plan);
        }

        public void ApplySkillTint(LivingEffectView view, IReadOnlyList<SentenceWord> words)
        {
            if (view == null || words == null || words.Count == 0)
                return;

            SkillFeel.ElementPalette(words, _host.Colors, out Color line, out Color blob);
            view.SetSkillTint(line, blob);
        }

        public void TickEffects(float dtSec, double worldMs)
        {
            Vector3 bossPos = _host.Boss != null ? _host.Boss.transform.position : Vector3.zero;
            var man = _host.Combat.Manifestation;
            bool bossDown = _host.BossVitals != null && _host.BossVitals.IsDown;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                LivingEffectView view = _active[i];
                if (view == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                LivingEffect logic = view.Logic;
                logic.Tick(dtSec);
                view.TickVisual(dtSec);

                if (!bossDown && logic.Phase == LivingEffectPhase.Traveling && _host.Boss != null && !view.TravelHitDone)
                {
                    if (logic.OverlapsBoss(bossPos.x, bossPos.z, man.TravelHitRadiusM))
                    {
                        view.TravelHitDone = true;
                        _host.Boss.React(
                            new Vector3(logic.OriginX, 0f, logic.OriginZ),
                            man.BossKnockbackM * 0.25f,
                            0.08f * logic.Current.Lift,
                            man.BossShakeSec * 0.45f,
                            worldMs);
                    }
                }

                if (!view.Scarred && logic.Verb == Rune.Aydinlik && logic.Current.Focus > 0.7f
                    && logic.Travel > 2.5f)
                {
                    Vector3 mid = new Vector3(
                        logic.OriginX + logic.DirX * logic.TipDistance * 0.5f,
                        0f,
                        logic.OriginZ + logic.DirZ * logic.TipDistance * 0.5f);
                    _host.Scars.Stamp(mid, man.ScarScaleM * 0.5f, ScarKind.Crack,
                        new Vector3(logic.DirX, 0f, logic.DirZ));
                    view.Scarred = true;
                }

                if (!logic.IsAlive)
                {
                    _host.ClearClosingStamp(logic);
                    _host.DestroyUnityObject(view.gameObject);
                    _active.RemoveAt(i);
                }
            }
        }
    }
}
