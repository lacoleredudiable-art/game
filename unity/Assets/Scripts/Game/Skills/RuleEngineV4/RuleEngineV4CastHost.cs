using System.Collections.Generic;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Diagnostics;
using Dovus.Game.Skills;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public sealed class RuleEngineV4CastHost
    {
        readonly ManifestationDirector _director;
        readonly RuleEngineV4TeamMarkRegistry _marks = new();
        readonly List<RuleEngineV4TargetCandidate> _candidates = new();
        readonly RuleEngineV4WorldSession _session = new();
        readonly RuleEngineV4PhysicsServices _physics = new();
        readonly RuleEngineV4CatalogAccess _catalogAccess = new();
        readonly RuleEngineV4DeliveryEvaluator _deliveryEvaluator;
        readonly RuleEngineV4WorldCommandRunHost _runner;
        float _lastDealt;

        public RuleEngineV4CastHost(ManifestationDirector director)
        {
            _director = director;
            _director.RuleEngineV4Session = _session;
            _deliveryEvaluator = new RuleEngineV4DeliveryEvaluator(_physics);
            _runner = new RuleEngineV4WorldCommandRunHost(director, _session, _physics, _deliveryEvaluator);
        }

        public RuleEngineV4CatalogAccess CatalogAccess => _catalogAccess;

        public RuleEngineV4WorldSession Session => _session;

        public float LastDealt => _lastDealt;

        public bool IsRunning => _runner.IsRunning;

        internal RuleEngineV4WorldCommandRunHost Runner => _runner;

        public bool TryLaunch(
            PendingClosing ctx,
            SkillResolution skill,
            in SkillMotionPlan motion,
            out float dealt)
        {
            dealt = 0;
            if (skill.IsEmpty
                || !int.TryParse(skill.Identity.Verb.Value, out int verb)
                || !int.TryParse(skill.Identity.Adjective.Value, out int adj))
                return false;
            return TryLaunchCombo(verb, adj, out dealt);
        }

        internal bool TryLaunchCombo(int verb, int adj, out float dealt)
        {
            dealt = 0;
            _lastDealt = 0;
            if (!RuleEngineV4Feature.Enabled)
                return false;
            if (!RuleEngineV4Slice.IsSliceCombo(verb, adj))
                return false;

            int weaponId = _director.EquippedWeaponNumber();
            if (!RuleEngineV4Slice.IsSliceWeapon(weaponId))
                return false;

            RuleEngineV4Planner planner = _catalogAccess.Planner;
            RuleEngineV4Catalog data = _catalogAccess.Catalog;
            RuleEngineV4TargetResolution resolution = RuleEngineV4TargetResolver.Resolve(
                GetVerb(data, verb),
                GetAdj(data, adj));

            CollectSceneCandidates();
            bool manual = _director._targeting != null
                && _director._targeting.Selected != null
                && _director._targeting.Selected.IsAvailable;
            int? manualId = _director._targeting?.Selected != null
                ? _director._targeting.Selected.TargetKey
                : null;
            float weaponRange = EffectiveWeaponRange(data, verb, weaponId);
            RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
                resolution,
                adj,
                verb,
                weaponRange,
                data.Globals.FriendlyRangeCapM,
                data.Globals.IsaretUseRangeM,
                manual,
                manualId,
                _candidates);
            bool hasMark = RuleEngineV4AutoTargetPicker.AnyUsableMark(_candidates, data.Globals.IsaretUseRangeM);
            RuleEngineV4CastContext castCtx = RuleEngineV4AutoTargetPicker.ToCastContext(pick, adj, hasMark);
            CommandPlan plan = planner.Plan(verb, adj, weaponId, castCtx);
            Transform focus = ResolveTargetTransform(pick);
            RuleEngineV4SurfaceAim surfaceAim = default;
            if (TryAimSurfaceMark(plan, pick, out surfaceAim))
            {
                plan = planner.Plan(verb, adj, weaponId, InReachContext(hasMark));
                focus = null;
            }
            else if (TryUseSurfaceMark(planner, verb, adj, weaponId, resolution, pick, data, out CommandPlan surfacePlan, out Transform anchor))
            {
                plan = surfacePlan;
                focus = anchor;
            }
            LastFocus = focus;
            LastSurfaceAim = surfaceAim;
            if (!plan.IsValid)
            {
                DebugConfig.DevLog($"[RuleEngineV4] boş plan {verb}-{adj} w{weaponId}");
                return true;
            }

            RuleEngineV4SkillAnimBridge.PlayForPlan(_director, plan, data.SkillAnim);
            _runner.Start(plan, focus, d =>
            {
                _lastDealt = d;
                _director._castPort?.NotifyRuleEngineV4Dealt(d);
            }, surfaceAim);
            return true;
        }

        internal Transform LastFocus { get; private set; }

        internal RuleEngineV4SurfaceAim LastSurfaceAim { get; private set; }

        internal RuleEngineV4TeamMarkRegistry Marks => _marks;

        static RuleEngineV4CastContext InReachContext(bool hasMark) =>
            new(hasValidTarget: true, targetInWeaponRange: true, manualTargetSelected: false, hasUsableMark: hasMark);

        /// <summary>İşaret koyan plan: nişan ışını MarkOnly yüzeye çarpıyor ve canlı hedeften önce geliyorsa.</summary>
        bool TryAimSurfaceMark(CommandPlan plan, in RuleEngineV4TargetPick pick, out RuleEngineV4SurfaceAim aim)
        {
            aim = default;
            float placeM = RuleEngineV4MarkOnlyRules.MarkPlaceRangeM(plan);
            if (placeM <= 0 || !_physics.TryAimMarkOnlySurface(_director.MechanicsPlayer, placeM, out aim))
                return false;
            bool living = TryLivingDistance(pick, out float livingM);
            if (RuleEngineV4MarkOnlyRules.SurfaceBeatsLiving(aim.DistanceM, living && livingM <= placeM, livingM))
                return true;
            aim = default;
            return false;
        }

        /// <summary>İşarete giden plan: yüzey işareti canlı işaretten yakınsa yalnız taşıyan, hasar/şifasız plan onu kullanır.</summary>
        bool TryUseSurfaceMark(
            RuleEngineV4Planner planner,
            int verb,
            int adj,
            int weaponId,
            RuleEngineV4TargetResolution resolution,
            in RuleEngineV4TargetPick pick,
            RuleEngineV4Catalog data,
            out CommandPlan plan,
            out Transform anchor)
        {
            plan = null;
            anchor = null;
            Transform player = _director.MechanicsPlayer;
            if (player == null
                || !RuleEngineV4MarkOnlyRules.TargetsMark(resolution)
                || !_marks.TryNearestSurfaceMark(player.position, data.Globals.IsaretUseRangeM, WorldMs(), out anchor, out float anchorM))
                return false;
            if (TryLivingDistance(pick, out float livingM) && livingM <= anchorM)
                return false;
            plan = planner.Plan(verb, adj, weaponId, InReachContext(hasMark: true));
            if (RuleEngineV4MarkOnlyRules.MayUseSurfaceMark(plan))
                return true;
            plan = null;
            anchor = null;
            return false;
        }

        bool TryLivingDistance(in RuleEngineV4TargetPick pick, out float distanceM)
        {
            distanceM = float.MaxValue;
            if (!pick.HasTarget || pick.UseSelf)
                return false;
            for (int i = 0; i < _candidates.Count; i++)
            {
                if (_candidates[i].Id != pick.TargetId)
                    continue;
                distanceM = _candidates[i].DistanceM;
                return true;
            }
            return false;
        }

        double WorldMs() => _director.MechanicsClock != null ? _director.MechanicsClock.Director.WorldTimeMs : 0;

        void CollectSceneCandidates()
        {
            Transform player = _director.MechanicsPlayer;
            double worldMs = _director.MechanicsClock != null ? _director.MechanicsClock.Director.WorldTimeMs : 0;
            IReadOnlyList<TargetableHost> live = _director._liveTargetables != null
                ? _director._liveTargetables.Live
                : System.Array.Empty<TargetableHost>();
            RuleEngineV4SceneTargets.Collect(
                player != null ? player.position : Vector3.zero,
                0,
                live,
                _marks,
                worldMs,
                _candidates);
        }

        Transform ResolveTargetTransform(in RuleEngineV4TargetPick pick)
        {
            if (pick.UseSelf)
                return _director.MechanicsPlayer;
            IReadOnlyList<TargetableHost> live = _director._liveTargetables != null
                ? _director._liveTargetables.Live
                : System.Array.Empty<TargetableHost>();
            if (RuleEngineV4SceneTargets.TryResolveTransform(live, pick.TargetId, out Transform t))
                return t;
            return null;
        }

        static RuleEngineV4Verb GetVerb(RuleEngineV4Catalog c, int id) =>
            c.TryGetVerb(id, out RuleEngineV4Verb v) ? v : new RuleEngineV4Verb { Id = id };

        static RuleEngineV4Adjective GetAdj(RuleEngineV4Catalog c, int id) =>
            c.TryGetAdjective(id, out RuleEngineV4Adjective a) ? a : new RuleEngineV4Adjective { Id = id };

        float EffectiveWeaponRange(RuleEngineV4Catalog data, int verbId, int weaponId)
        {
            if (!data.TryGetVerb(verbId, out RuleEngineV4Verb verb)
                || !data.TryGetWeapon(weaponId, out RuleEngineV4Weapon weapon))
                return 0;
            float range = weapon.RangeM(data.Scale.RangeReferenceM);
            if (!verb.Hostile && verb.Id != 3)
                range = Mathf.Min(range, data.Globals.FriendlyRangeCapM);
            return range;
        }

        public void PlaceMark(Transform marked, float lifeSec)
        {
            double ms = _director.MechanicsClock != null ? _director.MechanicsClock.Director.WorldTimeMs : 0;
            _marks.Place(marked, lifeSec, ms);
        }

        public void PlaceSurfaceMark(in RuleEngineV4SurfaceAim aim, float lifeSec) =>
            _marks.PlaceOnSurface(aim, OwnerColor, lifeSec, WorldMs());

        /// <summary>İşareti koyan oyuncunun takım rengi.</summary>
        internal Color OwnerColor => _director._colors != null ? _director._colors.Visuals.PlayerColor : Color.white;
    }
}
