using System;
using System.Collections.Generic;
using Dovus.Core;
using Dovus.Core.Combat;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using UnityEngine;

namespace Dovus.Game
{
    public sealed partial class ManifestationDirector
    {
        MotionTemplateCatalog _motionCatalog;
        bool _templateOwnsPosition;
        readonly SkillCastLease _castLease = new();
        SkillResolution _templateSkill;
        PendingClosing _templatePending;
        float _templateChain;
        bool _templateStatusSent;
        Transform _templateAim;

        MotionTemplateCatalog MotionCatalog
        {
            get
            {
                if (_motionCatalog != null && _motionCatalog.Templates.Count > 0)
                    return _motionCatalog;
                TextAsset asset = Resources.Load<TextAsset>("ElementSystem/motion-templates");
                if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                {
                    DesignWarnings.Once(
                        "motion.catalog",
                        "motion-templates.json yok. Hareket kalıpları kapalı, eski davranış sürüyor.");
                    _motionCatalog = MotionTemplateCatalog.Empty;
                    return _motionCatalog;
                }

                try
                {
                    _motionCatalog = MotionTemplateCatalog.FromJson(asset.text);
                }
                catch (Exception e)
                {
                    DesignWarnings.Once("motion.catalog", "Hareket kalıbı okunamadı: " + e.Message);
                    _motionCatalog = MotionTemplateCatalog.Empty;
                }
                return _motionCatalog;
            }
        }

        PositionPlayback PreparePositionPlayback(SkillResolution skill, MotionTemplate template)
        {
            var steps = new List<GrammarPositionStep>();
            MechanicPlan plan = MechanicPlanFor(skill);
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
            return PositionOwnership.Prepare(template, steps, fallbacks.PhaseSec, fallbacks.StepM);
        }

        /// <summary>
        /// Katalog ve gövde ilk cast'ten önce hazır olsun. Başarısız okuma kilitlenmez;
        /// sonraki cast yeniden dener.
        /// </summary>
        void EnsureMotionReady()
        {
            if (_player == null)
                return;
            if (_motionBody == null)
                _motionBody = _player.GetComponent<MotionTemplateBody>();
            if (_motionBody == null)
                _motionBody = _player.gameObject.AddComponent<MotionTemplateBody>();
            float arena = _colors != null ? _colors.ArenaHalfSizeM : 50f;
            float body = PlayerBodyRadiusM();
            _motionBody.Bind(_clock, arena, body > 0.05f ? body : 0.5f);
            _ = MotionCatalog;
        }

        bool TryBeginMotionTemplate(SkillResolution skill, PendingClosing pending)
        {
            _templateOwnsPosition = false;
            _castLease.ReleasePosition();
            if (skill.IsEmpty || string.IsNullOrEmpty(skill.SkillId) || _player == null)
                return false;
            if (!MotionCatalog.TryPlay(skill.SkillId, out MotionTemplate template))
                return false;

            PositionPlayback playback = PreparePositionPlayback(skill, template);
            _castLease.Arm(playback.OwnsPosition);
            _templateOwnsPosition = _castLease.OwnsPosition;
            template = playback.Template ?? template;

            if (_motionBody == null)
                _motionBody = _player.GetComponent<MotionTemplateBody>();
            if (_motionBody == null)
                _motionBody = _player.gameObject.AddComponent<MotionTemplateBody>();

            float arena = _colors != null ? _colors.ArenaHalfSizeM : 50f;
            _templateSkill = skill;
            _templatePending = pending;
            _templateChain = _closingChainBonus;
            _templateStatusSent = false;
            _templateAim = ResolveTemplateAim(skill, template);
            Transform aim = _templateAim;
            float bodyR = PlayerBodyRadiusM();
            if (bodyR < 0.05f)
                bodyR = 0.5f;
            _motionBody.Bind(_clock, arena, bodyR);
            float stopGap = MotionCatalog.Fallbacks.StopGapM;
            string weapon = _equippedWeapon != null
                ? (string.IsNullOrEmpty(_equippedWeapon.AnimationsKey) ? _equippedWeapon.Id : _equippedWeapon.AnimationsKey)
                : string.Empty;
            _motionBody.SetAnimContext(MotionCatalog.Anims, weapon, VerbOf(skill.SkillId));
            _motionBody.NoteSkill(skill.SkillId);
            _motionBody.Play(
                template,
                () =>
                {
                    if (aim == null)
                        return default;
                    Vector3 pos = aim.position;
                    bool hold = _boss != null && _boss.PullActive
                        && (aim == _boss.transform || aim.IsChildOf(_boss.transform));
                    bool obstacle = false;
                    float ox = 0f;
                    float oz = 0f;
                    float orad = 0f;
                    if (_boss != null && _ally != null && aim == _ally.transform)
                    {
                        obstacle = true;
                        Vector3 bossPos = _boss.transform.position;
                        ox = bossPos.x;
                        oz = bossPos.z;
                        orad = BossBodyRadius();
                    }
                    return new MotionTarget(
                        true, pos.x, pos.z, ColliderRadius(aim),
                        hold, obstacle, ox, oz, orad);
                },
                () => _input != null && _input.SkillFingerHeld,
                OnMotionTemplateHit,
                bodyR,
                stopGap);
            Debug.Log($"[Motion] {skill.SkillId} → {template.Name}");
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

        void OnMotionTemplateHit(MotionHit hit)
        {
            if (hit.Payload == "marker" && hit.Anchor == "plant")
            {
                SpawnFuse(hit);
                return;
            }

            SpawnMotionHitVisual(hit);
            if (hit.Anchor == "plant")
                ClearFuse();
            if (hit.Payload == "none" || _templateSkill.IsEmpty)
                return;

            bool friendly = IsFriendlyFieldVerb(_templateSkill) || IsHealSkill(_templateSkill);
            bool geometry = BossReachedMotionHit(hit);
            bool reached = friendly || geometry;
            if (!friendly && reached)
            {
                ApplyClosingDamage(
                    _templatePending.Closing,
                    _templateSkill,
                    false,
                    0f,
                    hit.Share,
                    _templateChain);
            }

            // Fiil hasarı kapanışta iner. Emici aktarımın eksi canı base_damage 0 iken
            // ayrıca boss'a yazılır; yoksa 1-2 gibi vuruşlar iki kez vurur.
            if (geometry && _templateSkill.BaseDamage <= 0.01f)
                ApplyDrainDamage(hit.Share);

            if (friendly && IsHealSkill(_templateSkill))
            {
                if (DrainNumbers.TryShare(LastMechanicPlan, hit.Share, out _, out float drainHeal) && drainHeal > 0.5f)
                    ApplyClosingHealAmount(_templateSkill, Mathf.RoundToInt(drainHeal), null, 0f);
                else
                {
                    ApplyClosingHeal(
                        _templatePending.Closing,
                        _templateSkill,
                        hit.Share,
                        _templateChain);
                }
            }

            bool selfPulse = hit.Anchor is "self" or "ring";
            if ((reached || selfPulse) && !_templateStatusSent)
            {
                ApplyClosingStatuses(_templatePending, _templateSkill, bossReached: !friendly && reached);
                if (!friendly)
                {
                    ApplyMechanicHitEffects(
                        LastMechanicPlan,
                        new Vector3(hit.OriginX, 0f, hit.OriginZ));
                }
                _templateStatusSent = true;
            }
        }

        void ApplyDrainDamage(float share)
        {
            if (!DrainNumbers.TryShare(LastMechanicPlan, share, out float damage, out _) || damage <= 0.01f)
                return;
            if (_bossStatus != null)
                _bossStatus.ApplyDamage(damage);
            else
                _bossVitals?.ApplyDamage(damage);
        }

        bool BossReachedMotionHit(in MotionHit hit)
        {
            if (_boss == null)
                return false;
            Vector3 boss = _boss.transform.position;
            float extra = BossBodyRadius();
            var origin = new Vector3(hit.OriginX, boss.y, hit.OriginZ);
            bool templateHit;
            if (hit.Anchor is "self" or "ring" or "target" or "behind" or "plant" or "target_side"
                || hit.Shape == "sphere")
            {
                Vector3 flat = boss - origin;
                flat.y = 0f;
                templateHit = flat.magnitude <= hit.RadiusM + extra;
            }
            else
            {
                Vector3 dir = new Vector3(hit.DirX, 0f, hit.DirZ);
                if (dir.sqrMagnitude < 0.0001f)
                    dir = Vector3.forward;
                dir.Normalize();
                Vector3 end = origin + dir * Mathf.Max(hit.LengthM, 0.2f);
                templateHit = DistancePointSegment(boss, origin, end) <= hit.RadiusM + extra;
            }
            if (templateHit)
                return true;
            return JsonEdgeReachesBoss(boss, extra);
        }

        /// <summary>
        /// Göğüs ofseti dikeydir (0,35 m); yatay menzil kenardan kenara JSON boyudur.
        /// Kalıp vuruşu kısa kalsa da fiil hitbox'ı yetiyorsa isabet sayılır.
        /// </summary>
        bool JsonEdgeReachesBoss(Vector3 boss, float bossRadius)
        {
            if (_player == null || _templateSkill.IsEmpty)
                return false;
            float reach = JsonEdgeReachM(_templateSkill);
            if (reach <= 0f)
                return false;
            return MotionCastReach.CenterInReach(
                FlatDistance(_player.position, boss),
                PlayerBodyRadiusM(),
                bossRadius,
                reach);
        }

        float JsonEdgeReachM(in SkillResolution skill)
        {
            if (!TryVerbHitbox(skill, out VerbHitboxSpec spec))
                return 0f;
            int.TryParse(skill.AdjectiveId, out int adjectiveId);
            int weaponId = EquippedWeaponNumber();
            float rangeMult = _equippedWeapon != null ? _equippedWeapon.RangeMult : 1f;
            float weaponScale = _verbData?.WeaponSizeMult(weaponId, rangeMult) ?? rangeMult;
            float table = _verbData?.AdjectiveSizeMult(adjectiveId) ?? 1f;
            float engineScale = skill.EngineModifiers["hitbox_scale_mult"].AsFloat(0f);
            float adjective = HitboxSizing.AdjectiveScale(table, engineScale);
            adjective *= _slotPassives?.HitboxSizeMult ?? 1f;
            return HitboxSizing.Resolve(spec, weaponScale, adjective).ReachM;
        }

        float BossBodyRadius()
        {
            if (_boss == null)
                return 0.6f;
            Collider col = _boss.GetComponentInChildren<Collider>();
            if (col == null)
            {
                DesignWarnings.Once(
                    "motion.boss_radius",
                    "Boss gövdesi okunamadı. Vuruş payı yedek 0.6 m.");
                return 0.6f;
            }
            return Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
        }

        static float DistancePointSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.0001f)
                return Vector3.Distance(point, a);
            float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / len2);
            return Vector3.Distance(point, a + ab * t);
        }

        /// <summary>
        /// Atıcı duruş hedefi olmaz. Dost kalıbı işaretli dosta yürür; ışınlanma
        /// gövdeyi öte kenardan geçer. Diğerleri düşmanı kullanır, yoksa bakış.
        /// </summary>
        Transform ResolveTemplateAim(SkillResolution skill, MotionTemplate template)
        {
            bool ally = _ally != null && _ally.transform != _player;
            MotionDeliveryAim.Kind kind = MotionDeliveryAim.Choose(
                template.Aim,
                skill.TargetMode,
                skill.Action,
                MotionDeliveryAim.MovesTowardMarked(template),
                ally);
            if (kind == MotionDeliveryAim.Kind.Ally && ally && !MotionDeliveryAim.SwapsPastBody(template))
                return _ally.transform;
            if (MotionAim.IsEnemy(template.Aim) || kind == MotionDeliveryAim.Kind.None
                || MotionDeliveryAim.SwapsPastBody(template))
            {
                Transform enemy = ResolveMotionEnemy(template);
                if (enemy != null && enemy != _player)
                    return enemy;
            }
            return _boss != null && _boss.transform != _player ? _boss.transform : null;
        }

        Transform ResolveMotionEnemy(MotionTemplate template)
        {
            float gate = MotionCastReach.GateRangeM(
                MotionCastReach.EdgeReachM(template),
                PlayerBodyRadiusM());
            if (_targeting != null && _player != null)
            {
                Targetable selected = _targeting.Selected;
                if (selected != null && selected.IsAvailable && IsEnemyBody(selected.transform)
                    && selected.DistanceFrom(_player.position) <= gate)
                    return selected.transform;
                if (_targeting.TryResolveBasicEnemy(gate, out Transform auto) && IsEnemyBody(auto))
                    return auto;
            }

            return _boss != null ? _boss.transform : null;
        }

        bool IsEnemyBody(Transform body)
        {
            if (body == null || body == _player)
                return false;
            if (_ally != null && (body == _ally.transform || body.IsChildOf(_ally.transform)))
                return false;
            return true;
        }

        float ColliderRadius(Transform body)
        {
            if (body == null)
                return 0f;
            if (_boss != null && (body == _boss.transform || body.IsChildOf(_boss.transform)))
                return BossBodyRadius();
            Collider col = body.GetComponentInChildren<Collider>();
            if (col == null)
                return 0.5f;
            return Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
        }

        GameObject _fuse;

        void SpawnFuse(in MotionHit hit)
        {
            ClearFuse();
            _fuse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _fuse.name = "Fuse";
            _fuse.transform.SetParent(transform, true);
            _fuse.transform.position = new Vector3(hit.OriginX, 0.18f, hit.OriginZ);
            _fuse.transform.localScale = Vector3.one * 0.28f;
            Collider col = _fuse.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            Renderer renderer = _fuse.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(1f, 0.42f, 0.08f);
        }

        void ClearFuse()
        {
            if (_fuse == null)
                return;
            Destroy(_fuse);
            _fuse = null;
        }

        /// <summary>
        /// Dodge kesmesi: kalıp konumu aynı anda bırakılır, süren kapanış ve gövde durur.
        /// Bekleme geri yazılmaz.
        /// </summary>
        void CancelActiveSkillForDodge()
        {
            _castLease.CancelForDodge();
            _templateOwnsPosition = false;
            if (_motionBody != null)
                _motionBody.Stop();
            if (_motionDriver != null)
                _motionDriver.Stop();
            AbortCastView(_buildingView);
            _buildingView = null;
            for (int i = 0; i < _pending.Count; i++)
                AbortCastView(_pending[i].View);
            _pending.Clear();
            _playerStatus?.ClearCastMobility();
        }

        static void AbortCastView(LivingEffectView view)
        {
            if (view != null && view.Logic != null)
                view.Logic.Abort();
        }

        void SpawnMotionHitVisual(in MotionHit hit)
        {
            string shape = hit.Shape == "line" ? "capsule" : hit.Shape;
            if (string.IsNullOrEmpty(shape))
                shape = "capsule";
            Vector3 pos = new Vector3(hit.OriginX, _player != null ? _player.position.y + 0.9f : 0.9f, hit.OriginZ);
            Vector3 dir = new Vector3(hit.DirX, 0f, hit.DirZ);
            GameObject fx = HitboxVfxRegistry.Create(
                "motion-" + _templateSkill.SkillId,
                shape,
                SelectedElementPaint?.ColorHex ?? "#f2d48a",
                pos,
                dir,
                Mathf.Max(0.15f, hit.RadiusM),
                Mathf.Max(hit.RadiusM, hit.LengthM),
                transform);
            if (fx != null)
                Destroy(fx, 0.35f);
        }
    }
}
