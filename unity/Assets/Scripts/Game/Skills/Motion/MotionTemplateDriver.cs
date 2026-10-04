using Dovus.Core;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Shared;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Diagnostics;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Vfx;
using System;
using System.Collections.Generic;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Skills.Motion
{
    public sealed class MotionTemplateDriver
    {
        readonly IMotionTemplateDriverHost _host;
        MotionTemplateCatalog _catalog;
        bool _emiciContactPull;
        bool _templateOwnsPosition;
        readonly SkillCastLease _castLease = new();
        SkillResolution _templateSkill;
        PendingClosing _templatePending;
        float _templateChain;
        bool _templateStatusSent;
        Transform _templateAim;
        float _templateStartCenter;
        bool _recoilInTemplate;
        int _templateSlotCastId;
        GameObject _fuse;

        public MotionTemplateDriver(IMotionTemplateDriverHost host) => _host = host;

        public bool TemplateOwnsPosition => _templateOwnsPosition;
        public SkillResolution TemplateSkill => _templateSkill;
        public float TemplateChain => _templateChain;
        public int TemplateSlotCastId => _templateSlotCastId;
        public PendingClosing TemplatePending => _templatePending;
        public float TemplateStartCenter => _templateStartCenter;
        public bool TemplateStatusSent => _templateStatusSent;
        public void SetTemplateStatusSent(bool value) => _templateStatusSent = value;
        public bool RecoilInTemplate => _recoilInTemplate;

        public void SetRecoilInTemplate(bool value) => _recoilInTemplate = value;

        public MotionTemplateCatalog Catalog => MotionCatalog;

        public bool TryPlayTemplate(SkillId skillId, out MotionTemplate template) =>
            MotionCatalog.TryPlay(skillId, out template);

        public float BossBodyRadius()
        {
            if (_host.Boss == null)
                return MotionTemplateDriverDefaults.MotionReturnHeightM;
            Collider col = _host.Boss.GetComponentInChildren<Collider>();
            if (col == null)
            {
                DesignWarnings.Once(
                    "motion.boss_radius",
                    "Boss gövdesi okunamadı. Vuruş payı yedek 0.6 m.");
                return MotionTemplateDriverDefaults.MotionReturnHeightM;
            }
            return Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
        }

        MotionTemplateCatalog MotionCatalog
        {
            get
            {
                if (_catalog != null && _catalog.Templates.Count > 0)
                    return _catalog;
                TextAsset asset = AssetLoader.Load<TextAsset>("ElementSystem/motion-templates", null);
                if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                    throw new InvalidOperationException("motion-templates.json missing/invalid");

                try
                {
                    _catalog = MotionTemplateCatalog.FromJson(asset.text);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("motion-templates.json missing/invalid", e);
                }
                return _catalog;
            }
        }

        public PositionPlayback PreparePositionPlayback(SkillResolution skill, MotionTemplate template)
        {
            var steps = new List<GrammarPositionStep>();
            MechanicPlan plan = _host.MechanicPlanFor(skill);
            if (plan != null)
            {
                foreach (MechanicEffect effect in plan.Effects)
                {
                    if (PositionOwnership.Kind(effect.Atom, effect.Stat) == PositionStepKind.None)
                        continue;
                    steps.Add(new GrammarPositionStep(effect.Stat, effect.Amount));
                }
            }

            MotionFallbacks fallbacks = MotionCatalog.Fallbacks;
            PositionPlayback playback = PositionOwnership.Prepare(
                template, steps, fallbacks.PhaseSec, fallbacks.StepM);
            MotionTemplate shaped = playback.Template ?? template;
            bool owns = playback.OwnsPosition;

            if (ShouldCloseToTarget(skill) && _host.Player != null && _host.Boss != null)
            {
                float center = _host.FlatDistance(_host.Player.position, _host.Boss.transform.position);
                float meters = MotionCastReach.ApproachMeters(
                    center, _host.PlayerBodyRadiusM(), _host.BossBodyRadius(), _host.MotionHitResolver.JsonEdgeReachM(skill), shaped);
                MotionTemplate closed = CastApproach.Prepend(shaped, meters);
                if (!ReferenceEquals(closed, shaped))
                {
                    shaped = closed;
                    owns = true;
                }
            }

            WeaponCombatProfile profile = _host.EquippedProfile;
            if (profile != null && CannonRecoilMotion.Applies(
                    profile.HitShape, profile.RecoilM, skill.Combat.BaseDamage, skill.Identity.Adjective, shaped))
            {
                MotionTemplate recoiled = CannonRecoilMotion.Append(shaped, profile.RecoilM);
                if (!ReferenceEquals(recoiled, shaped))
                {
                    shaped = recoiled;
                    owns = true;
                    _recoilInTemplate = true;
                }
            }

            float dashM = skill.Engine.DashDistanceM(0f);
            if (dashM > MotionTemplateDriverDefaults.DashDistMinM)
            {
                float range = _host.EquippedWeapon != null && _host.EquippedWeapon.RangeMult > 0f
                    ? _host.EquippedWeapon.RangeMult
                    : 1f;
                float box = skill.Engine.HitboxScaleMult(0f);
                if (box <= MotionTemplateDriverDefaults.HitboxAreaEpsilon)
                    box = 1f;
                MotionTemplate dashed = DashDistance.Apply(shaped, dashM * range * box);
                if (!ReferenceEquals(dashed, shaped))
                {
                    shaped = dashed;
                    owns = true;
                }
            }

            if (plan != null && plan.Body.Homing)
            {
                MotionTemplate homing = HomingDelivery.TrackShots(shaped);
                if (!ReferenceEquals(homing, shaped))
                    shaped = homing;
            }

            if (ReferenceEquals(shaped, playback.Template) && owns == playback.OwnsPosition)
                return playback;
            return new PositionPlayback(owns, shaped, playback.PlaceReturnMark);
        }

        bool ShouldCloseToTarget(in SkillResolution skill)
        {
            if (skill.IsEmpty)
                return false;
            if (TargetingRules.AimMode(skill) != SkillAimMode.Targeted)
                return false;
            return !CardEffectRules.PrefersAlly(skill.Targeting.Mode, skill.Presentation.Action);
        }

        /// <summary>
        /// Katalog ve gövde ilk cast'ten önce hazır olsun. Başarısız okuma kilitlenmez;
        /// sonraki cast yeniden dener.
        /// </summary>
        public void EnsureMotionReady()
        {
            if (_host.Player == null)
                return;
            _host.EnsureMotionBody();
            float arena = _host.Colors != null ? _host.Colors.Arena.ArenaHalfSizeM : MotionTemplateDriverDefaults.ArenaHalfSizeFallbackM;
            float body = _host.PlayerBodyRadiusM();
            _host.MotionBody.Bind(_host.Clock, arena, body > MotionTemplateDriverDefaults.MotionBodyBindMinM ? body : 0.5f);
            _ = MotionCatalog;
        }

        public bool TryBeginMotionTemplate(SkillResolution skill, PendingClosing pending)
        {
            _templateOwnsPosition = false;
            _emiciContactPull = false;
            _recoilInTemplate = false;
            _templateStartCenter = 0f;
            _castLease.ReleasePosition();
            if (skill.IsEmpty || string.IsNullOrEmpty(skill.Identity.Id) || _host.Player == null)
                return false;
            if (!MotionCatalog.TryPlay((SkillId)skill.Identity.Id, out MotionTemplate template))
                return false;

            PositionPlayback playback = PreparePositionPlayback(skill, template);
            _castLease.Arm(playback.OwnsPosition);
            _templateOwnsPosition = _castLease.OwnsPosition;
            template = playback.Template ?? template;

            _host.EnsureMotionBody();

            float arena = _host.Colors != null ? _host.Colors.Arena.ArenaHalfSizeM : MotionTemplateDriverDefaults.ArenaHalfSizeFallbackM;
            _templateSkill = skill;
            _templateSlotCastId = _host.SlotQueryCastId;
            _templatePending = pending;
            _templateChain = _host.ClosingChainBonus;
            _templateStatusSent = false;
            SetTemplateAim(ResolveTemplateAim(skill, template));
            Transform aim = _templateAim;
            float bodyR = _host.PlayerBodyRadiusM();
            if (bodyR < MotionTemplateDriverDefaults.BodyRadiusMinM)
                bodyR = 0.5f;
            _host.MotionBody.Bind(_host.Clock, arena, bodyR);
            float stopGap = MotionCatalog.Fallbacks.StopGapM;
            string weapon = _host.EquippedWeapon != null
                ? (string.IsNullOrEmpty(_host.EquippedWeapon.AnimationsKey) ? _host.EquippedWeapon.Id : _host.EquippedWeapon.AnimationsKey)
                : string.Empty;
            _host.MotionBody.SetAnimContext(MotionCatalog.Anims, weapon, VerbOf(skill.Identity.Id));
            _host.MotionBody.NoteSkill((SkillId)skill.Identity.Id);
            if (_host.Boss != null)
                _templateStartCenter = _host.FlatDistance(_host.Player.position, _host.Boss.transform.position);
            // Emici ilerleyen kalıpta oyuncu hep yerinde; boss'u yalnız çeken plan getirir (4-2 çekmez).
            _emiciContactPull = EmiciApproach.ShouldHoldCaster(skill.Identity.Adjective, template);
            MechanicPlan pullPlan = _host.MechanicPlanFor(skill);
            if (_emiciContactPull && pullPlan != null && pullPlan.Body.Pull)
                _host.PullBossToPlayerContact();
            _host.MotionBody.Play(
                template,
                () =>
                {
                    if (aim == null)
                        return default;
                    Vector3 pos = aim.position;
                    bool bossAim = _host.Boss != null
                        && (aim == _host.Boss.transform || aim.IsChildOf(_host.Boss.transform));
                    bool hold = bossAim
                        && (_emiciContactPull || _host.Boss.PullActive)
                        && !EmiciApproach.TemplatePassesThrough(template);
                    bool obstacle = false;
                    float ox = 0f;
                    float oz = 0f;
                    float orad = 0f;
                    if (_host.Boss != null && _host.Ally != null && aim == _host.Ally.transform)
                    {
                        obstacle = true;
                        Vector3 bossPos = _host.Boss.transform.position;
                        ox = bossPos.x;
                        oz = bossPos.z;
                        orad = _host.BossBodyRadius();
                    }
                    return new MotionTarget(
                        true, pos.x, pos.z, ColliderRadius(aim),
                        hold, obstacle, ox, oz, orad);
                },
                () => _host.Input != null && _host.Input.SkillFingerHeld,
                hit => _host.MotionHitResolver.OnMotionTemplateHit(hit),
                bodyR,
                stopGap);
            DebugConfig.DevLog($"[Motion] {skill.Identity.Id} → {template.Name}");
            return true;
        }

        /// <summary>Düz vuruş: katalogdaki basic_strike lunge; hasar zamanlamasına dokunmaz.</summary>
        public bool TryBeginBasicStrikeStep()
        {
            if (_host.Player == null)
                return false;
            EnsureMotionReady();
            // Kira yalnız dodge'da ya da sonraki kalıp başında sıfırlanır; bitmiş bir kalıbın kirası adımı kesmesin.
            if (_host.MotionBody == null || _host.MotionBody.IsDisplacing)
                return false;

            MotionTemplate template = MotionCatalog.BasicStrike;
            if (template == null || template.Phases.Count == 0)
                return false;

            Transform aim = ResolveMotionEnemy(template);
            if (aim == null || aim == _host.Player)
                return false;

            var steps = new List<GrammarPositionStep>();
            MotionFallbacks fallbacks = MotionCatalog.Fallbacks;
            PositionPlayback playback = PositionOwnership.Prepare(
                template, steps, fallbacks.PhaseSec, fallbacks.StepM);
            template = playback.Template ?? template;
            if (!playback.OwnsPosition)
                return false;

            _castLease.Arm(true);
            _templateOwnsPosition = true;
            _templateSkill = SkillResolution.Empty;
            _templatePending = default;
            _templateChain = 0f;
            _templateStatusSent = false;
            SetTemplateAim(aim);
            _emiciContactPull = false;
            _recoilInTemplate = false;
            if (_host.Boss != null)
                _templateStartCenter = _host.FlatDistance(_host.Player.position, _host.Boss.transform.position);

            float bodyR = _host.PlayerBodyRadiusM();
            if (bodyR < MotionTemplateDriverDefaults.BodyRadiusMinM)
                bodyR = 0.5f;
            float arena = _host.Colors != null ? _host.Colors.Arena.ArenaHalfSizeM : MotionTemplateDriverDefaults.ArenaHalfSizeFallbackM;
            _host.MotionBody.Bind(_host.Clock, arena, bodyR);
            string weapon = _host.EquippedWeapon != null
                ? (string.IsNullOrEmpty(_host.EquippedWeapon.AnimationsKey) ? _host.EquippedWeapon.Id : _host.EquippedWeapon.AnimationsKey)
                : string.Empty;
            _host.MotionBody.SetAnimContext(MotionCatalog.Anims, weapon, 0);
            _host.MotionBody.NoteSkill(default);
            float stopGap = fallbacks.StopGapM;
            _host.MotionBody.Play(
                template,
                () =>
                {
                    if (aim == null)
                        return default;
                    Vector3 pos = aim.position;
                    return new MotionTarget(true, pos.x, pos.z, ColliderRadius(aim));
                },
                () => false,
                _ => { },
                bodyR,
                stopGap);
            DebugConfig.DevLog($"[Motion] basic_strike → {template.Name}");
            return true;
        }

        static int VerbOf(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return 0;
            int dash = skillId.IndexOf('-');
            if (dash <= 0)
                return 0;
            return int.TryParse(skillId.Substring(0, dash), out int verb) ? verb : 0;
        }

        /// <summary>
        /// Atıcı duruş hedefi olmaz. Dost kalıbı işaretli dosta yürür; ışınlanma
        /// gövdeyi öte kenardan geçer. Diğerleri düşmanı kullanır, yoksa bakış.
        /// </summary>
        Transform ResolveTemplateAim(SkillResolution skill, MotionTemplate template)
        {
            bool ally = _host.Ally != null && _host.Ally.transform != _host.Player;
            MotionDeliveryAim.Kind kind = MotionDeliveryAim.Choose(
                template.Aim,
                skill.Targeting.Mode,
                skill.Presentation.Action,
                MotionDeliveryAim.MovesTowardMarked(template),
                ally);
            if (kind == MotionDeliveryAim.Kind.Ally && ally && !MotionDeliveryAim.SwapsPastBody(template))
                return _host.Ally.transform;
            if (MotionAim.IsEnemy(template.Aim) || kind == MotionDeliveryAim.Kind.None
                || MotionDeliveryAim.SwapsPastBody(template))
            {
                Transform enemy = ResolveMotionEnemy(template);
                if (enemy != null && enemy != _host.Player)
                    return enemy;
            }
            return _host.Boss != null && _host.Boss.transform != _host.Player ? _host.Boss.transform : null;
        }

        Transform ResolveMotionEnemy(MotionTemplate template)
        {
            float gate = MotionCastReach.GateRangeM(
                MotionCastReach.EdgeReachM(template),
                _host.PlayerBodyRadiusM());
            if (_host.Targeting != null && _host.Player != null)
            {
                TargetableHost selected = _host.Targeting.Selected;
                if (selected != null && selected.IsAvailable && IsEnemyBody(selected.transform)
                    && selected.DistanceFrom(_host.Player.position) <= gate)
                    return selected.transform;
                if (_host.Targeting.TryResolveBasicEnemy(gate, out Transform auto) && IsEnemyBody(auto))
                    return auto;
            }

            return _host.Boss != null ? _host.Boss.transform : null;
        }

        public bool IsEnemyBody(Transform body)
        {
            if (body == null || body == _host.Player)
                return false;
            if (_host.Ally != null && (body == _host.Ally.transform || body.IsChildOf(_host.Ally.transform)))
                return false;
            return true;
        }

        void SetTemplateAim(Transform aim)
        {
            _templateAim = aim;
            _host.SetTemplateAimForSweep(aim);
        }

        public float ColliderRadius(Transform body)
        {
            if (body == null)
                return 0f;
            if (_host.Boss != null && (body == _host.Boss.transform || body.IsChildOf(_host.Boss.transform)))
                return _host.BossBodyRadius();
            Collider col = body.GetComponentInChildren<Collider>();
            if (col == null)
                return 0.5f;
            return Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
        }

        public void SpawnFuse(in MotionHit hit)
        {
            ClearFuse();
            _fuse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _fuse.name = "Fuse";
            _fuse.transform.SetParent(_host.DirectorTransform, true);
            _fuse.transform.position = new Vector3(hit.OriginX, MotionTemplateDriverDefaults.FuseGroundYM, hit.OriginZ);
            _fuse.transform.localScale = Vector3.one * MotionTemplateDriverDefaults.FuseScaleM;
            Collider col = _fuse.GetComponent<Collider>();
            if (col != null)
                UnityEngine.Object.Destroy(col);
            Renderer renderer = _fuse.GetComponent<Renderer>();
            if (renderer != null)
                SharedTint.Apply(renderer, new Color(1f, 0.42f, 0.08f));
        }

        public void ClearFuse()
        {
            if (_fuse == null)
                return;
            UnityEngine.Object.Destroy(_fuse);
            _fuse = null;
        }

        /// <summary>
        /// Dodge kesmesi: kalıp konumu aynı anda bırakılır, süren kapanış ve gövde durur.
        /// Bekleme geri yazılmaz.
        /// </summary>
        public void CancelActiveSkillForDodge()
        {
            _castLease.CancelForDodge();
            _host.SustainedCast.Clear();
            _templateOwnsPosition = false;
            if (_host.MotionBody != null)
                _host.MotionBody.Stop();
            AbortCastView(_host.BuildingView);
            _host.BuildingView = null;
            for (int i = 0; i < _host.PendingList.Count; i++)
                AbortCastView(_host.PendingList[i].View);
            _host.PendingList.Clear();
            _host.PlayerStatus?.ClearCastMobility();
        }

        static void AbortCastView(LivingEffectView view)
        {
            if (view != null && view.Logic != null)
                view.Logic.Abort();
        }

        public void SpawnMotionHitVisual(in MotionHit hit)
        {
            string shape = hit.Shape == "line" ? "capsule" : hit.Shape;
            if (string.IsNullOrEmpty(shape))
                shape = "capsule";
            Vector3 pos = new Vector3(hit.OriginX, _host.Player != null ? _host.Player.position.y + MotionTemplateDriverDefaults.HitFxHeightAbovePlayerM : MotionTemplateDriverDefaults.HitFxHeightAbovePlayerM, hit.OriginZ);
            Vector3 dir = new Vector3(hit.DirX, 0f, hit.DirZ);
            GameObject fx = _host.HitboxVfx?.Create(
                "motion-" + _templateSkill.Identity.Id,
                shape,
                _host.SelectedElementPaint?.ColorHex ?? "#f2d48a",
                pos,
                dir,
                Mathf.Max(MotionTemplateDriverDefaults.HitFxMinRadiusM, hit.RadiusM),
                Mathf.Max(hit.RadiusM, hit.LengthM),
                _host.DirectorTransform);
            if (fx != null)
                _host.DestroyUnityObject(fx, MotionTemplateDriverDefaults.HitFxLifetimeSec);
        }
    
    }
}
