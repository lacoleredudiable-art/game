using System;
using Dovus.Core;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using UnityEngine;

namespace Dovus.Game
{
    public sealed partial class ManifestationDirector
    {
        MotionTemplateCatalog _motionCatalog;
        bool _motionCatalogTried;
        SkillResolution _templateSkill;
        PendingClosing _templatePending;
        float _templateChain;
        bool _templateStatusSent;
        Transform _templateAim;

        MotionTemplateCatalog MotionCatalog
        {
            get
            {
                if (_motionCatalogTried)
                    return _motionCatalog;
                _motionCatalogTried = true;
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

        bool TryBeginMotionTemplate(SkillResolution skill, PendingClosing pending)
        {
            if (skill.IsEmpty || string.IsNullOrEmpty(skill.SkillId) || _player == null)
                return false;
            if (!MotionCatalog.TryPlay(skill.SkillId, out MotionTemplate template))
                return false;

            if (_motionBody == null)
                _motionBody = _player.GetComponent<MotionTemplateBody>();
            if (_motionBody == null)
                _motionBody = _player.gameObject.AddComponent<MotionTemplateBody>();

            float arena = _colors != null ? _colors.ArenaHalfSizeM : 50f;
            _motionBody.Bind(_clock, arena, 0.5f);
            _templateSkill = skill;
            _templatePending = pending;
            _templateChain = _closingChainBonus;
            _templateStatusSent = false;
            _templateAim = pending.Target;
            if (_templateAim == null && !IsFriendlyFieldVerb(skill) && _boss != null)
                _templateAim = _boss.transform;

            Transform aim = _templateAim;
            _motionBody.Play(
                template,
                () =>
                {
                    if (aim == null)
                        return default;
                    Vector3 pos = aim.position;
                    return new MotionTarget(true, pos.x, pos.z);
                },
                () => _input != null && _input.SkillFingerHeld,
                OnMotionTemplateHit);
            Debug.Log($"[Motion] {skill.SkillId} → {template.Name}");
            return true;
        }

        void OnMotionTemplateHit(MotionHit hit)
        {
            SpawnMotionHitVisual(hit);
            if (hit.Payload == "none" || _templateSkill.IsEmpty)
                return;

            bool friendly = IsFriendlyFieldVerb(_templateSkill) || IsHealSkill(_templateSkill);
            bool reached = friendly || BossReachedMotionHit(hit);
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

            if (friendly && IsHealSkill(_templateSkill))
            {
                ApplyClosingHeal(
                    _templatePending.Closing,
                    _templateSkill,
                    hit.Share,
                    _templateChain);
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

        bool BossReachedMotionHit(in MotionHit hit)
        {
            if (_boss == null)
                return false;
            Vector3 boss = _boss.transform.position;
            float extra = BossBodyRadius();
            var origin = new Vector3(hit.OriginX, boss.y, hit.OriginZ);
            if (hit.Anchor is "self" or "ring" or "target" or "behind" or hit.Shape == "sphere")
            {
                Vector3 flat = boss - origin;
                flat.y = 0f;
                return flat.magnitude <= hit.RadiusM + extra;
            }

            Vector3 dir = new Vector3(hit.DirX, 0f, hit.DirZ);
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.forward;
            dir.Normalize();
            Vector3 end = origin + dir * Mathf.Max(hit.LengthM, 0.2f);
            return DistancePointSegment(boss, origin, end) <= hit.RadiusM + extra;
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
