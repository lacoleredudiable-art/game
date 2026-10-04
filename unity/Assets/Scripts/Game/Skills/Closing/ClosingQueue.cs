using Dovus.App.Casting;
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
using Dovus.Game.Actors;
using Dovus.Game.Composition;
using Dovus.Game.Vfx;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Closing
{
    public sealed class ClosingQueue
    {
        readonly IClosingQueueHost _host;
        readonly List<PendingClosing> _pending;

        public List<PendingClosing> Pending => _pending;
        public int PendingCount => _pending.Count;

        public ClosingQueue(IClosingQueueHost host, List<PendingClosing> pending)
        {
            _host = host;
            _pending = pending;
        }

        public void OnSentenceCompleted(CompletedSentence sentence)
        {
            _host.EnsureSkillServices();
            _host.SentenceBridge.OnCompleted(sentence);
        }

        public void TickCastHold(double worldMs, ActorView visual, bool channelHeld, bool guardHeld)
        {
            if (visual == null)
                return;
            visual.SetHoldFlags(channelHeld, guardHeld);
        }

        public void TickPendingClosings(double worldMs)
        {
            int basicDot = _host.Colors != null ? _host.Colors.Input.BasicStrikeDot : 1;
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingClosing p = _pending[i];
                bool basic = IsPendingBasic(p, basicDot);
                if (p.View == null || p.View.Logic == null)
                {
                    if (BasicStrikePayoff.KeepUntilBang(basic, worldMs, p.BangAtWorldMs))
                        continue;
                    if (BasicStrikePayoff.PayWithoutView(basic, worldMs, p.BangAtWorldMs))
                        FireClosing(p);
                    _pending.RemoveAt(i);
                    continue;
                }

                if (p.View.Logic.Phase is LivingEffectPhase.Fading or LivingEffectPhase.Dead)
                {
                    if (BasicStrikePayoff.KeepUntilBang(basic, worldMs, p.BangAtWorldMs))
                        continue;
                    if (BasicStrikePayoff.PayWithoutView(basic, worldMs, p.BangAtWorldMs))
                        FireClosing(p);
                    _pending.RemoveAt(i);
                    continue;
                }

                if (worldMs < p.BangAtWorldMs)
                    continue;

                FireClosing(p);
                _pending.RemoveAt(i);
            }
        }

        public bool IsPendingBasic(PendingClosing p)
        {
            int basicDot = _host.Colors != null ? _host.Colors.Input.BasicStrikeDot : 1;
            return IsPendingBasic(p, basicDot);
        }

        static bool IsPendingBasic(PendingClosing p, int basicDot)
        {
            bool viewBasic = p.View != null && p.View.IsBasicStrike;
            return PendingBasicStrikeRules.IsPendingBasic(
                p.IsBasicStrike,
                viewBasic,
                p.Words,
                basicDot);
        }

        public void FireClosing(PendingClosing p)
        {
            LivingEffect logic = p.View != null ? p.View.Logic : null;
            logic?.FireClosingBang();
            _host.StampScar(p.View, p.Closing);

            bool basic = IsPendingBasic(p);
            if (basic)
            {
                _host.EnsureCastPort();
                _host.RunBasicClosing(p);
                return;
            }

            if (logic == null)
                return;

            _host.EnsureCastPort();
            _host.RunSkillClosing(p, logic);
        }

        public void SpawnClosingImpact(PendingClosing p)
        {
            if (p.View == null || p.View.Logic == null)
                return;

            LivingEffect logic = p.View.Logic;
            Vector3 tip = new Vector3(logic.TipX, ClosingDefaults.ClosingTipGroundYM, logic.TipZ);
            Vector3 origin = new Vector3(logic.OriginX, ClosingDefaults.ClosingOriginGroundYM, logic.OriginZ);
            string element = _host.SelectedElementPaint?.Name
                ?? (p.Words != null && p.Words.Count > 0
                    ? RuneInfo.LegacySerializationName(p.Words[0].Rune)
                    : "Ates");

            string impactStyle = "burst_soft";
            string trailStyle = string.Empty;
            if (!p.IsBasicStrike && _host.Skills != null && p.Words != null)
            {
                SkillResolution skill = ResolveSkillWords(p.Words);
                _host.EnsurePresentationCatalog();
                var catalog = _host.PresentationCatalog;
                if (catalog != null && !skill.IsEmpty)
                {
                    LivingEffectPlan plan = SkillWorldPlanner.Build(
                        skill, catalog,
                        _host.Combat != null ? _host.Combat.Manifestation : new ManifestationTuning());
                    if (!string.IsNullOrEmpty(plan.TrajectoryId)
                        && catalog.TryGetTrajectory(plan.TrajectoryId, out TrajectoryNode traj))
                    {
                        trailStyle = traj.GetString("vfx_trail_type", string.Empty);
                        if (plan.TravelKind == LivingTravelKind.ExpandingRadial
                            || plan.TrajectoryId is "expanding_wave" or "radial_burst")
                            impactStyle = "pulse";
                        else if (plan.TrajectoryId is "raycast" or "instant_hit")
                            impactStyle = "pierce_hit";
                    }
                }
            }

            if (!string.IsNullOrEmpty(trailStyle))
            {
                GameObject trail = PlaceholderFactory.CreateTrail(
                    trailStyle, element, origin, tip, _host.DirectorTransform);
                if (trail != null)
                    _host.DestroyUnityObjectAfter(trail, 1.0f);
            }

            GameObject fx = PlaceholderFactory.CreateImpact(impactStyle, element, tip, _host.DirectorTransform);
            if (fx != null)
                _host.DestroyUnityObjectAfter(fx, ClosingDefaults.ImpactFxLifetimeSec);
        }

        public SkillResolution ResolveSkillWords(IReadOnlyList<SentenceWord> words)
        {
            if (_host.Skills == null || words == null || words.Count == 0)
                return SkillResolution.Empty;
            if (_host.Skills.IsV61 && words.Count == 2 && _host.SkillFactory != null)
            {
                int elementId = _host.SelectedElementPaint?.Id ?? 0;
                try
                {
                    _host.LastFactorySkill = _host.SkillFactory.CreateFromWords(
                        words, _host.EquippedWeapon, elementId);
                    return _host.LastFactorySkill.Resolution;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SkillFactory] pair çözülemedi: {e.Message}");
                    return SkillResolution.Empty;
                }
            }
            return _host.Skills.ResolveWords(words);
        }

        public SkillResolution ResolvePendingSkill(PendingClosing p)
        {
            if (_host.Skills == null || p.Words == null || p.Words.Count == 0)
                return SkillResolution.Empty;
            return ResolveSkillWords(p.Words);
        }

#if UNITY_EDITOR
        public void ForceTickClosings(double worldMs) => TickPendingClosings(worldMs);

        public void ForceFirePendingClosings(double now)
        {
            for (int i = 0; i < _pending.Count; i++)
            {
                PendingClosing p = _pending[i];
                p.BangAtWorldMs = now;
                _pending[i] = p;
            }
            TickPendingClosings(now);
        }
#endif
    }
}
