using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Game.Actors;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru dikey dilim yönetmeni: VfxPlan dinler, Kılıç ATIL katmanlarını oynatır.
    /// Mekanik / hitbox / zamanlama değiştirmez.
    /// </summary>
    public sealed class RuleDrivenVfxDirector : MonoBehaviour
    {
        MotionTemplateBodyHost _body;
        FeelVfxRuntime _feel;
        KorAwakenView _kor;
        EmberMeshTrailView _meshTrail;
        LightningDashTrailView _lightning;
        DragonSilhouetteCardView _silhouette;
        DragonRuneFlashView _runes;
        Transform _fxRoot;

        VfxPlan _plan;
        bool _active;
        bool _hitDone;
        bool _endDone;
        Vector3 _dashStart;
        Vector3 _lastPos;
        Vector3 _dashDir = Vector3.forward;

        public VfxPlan ActivePlan => _plan;
        public bool IsSliceActive => _active && _plan.IsSwordDashSlice;

        public void Bind(FeelVfxRuntime feel)
        {
            _feel = feel;
            _body = GetComponent<MotionTemplateBodyHost>();
            EnsureChildren();
        }

        public void BeginSkill(in SkillResolution skill, string weaponKey)
        {
            _plan = VfxPlanResolver.Resolve(skill, weaponKey);
            if (_plan.IsEmpty)
                return;

            EnsureChildren();
            _active = true;
            _hitDone = false;
            _endDone = false;
            _dashStart = transform.position;
            _lastPos = _dashStart;
            _dashDir = transform.forward;
            _dashDir.y = 0f;
            if (_dashDir.sqrMagnitude < 0.0001f)
                _dashDir = Vector3.forward;
            _dashDir.Normalize();

            if (_plan.SkillAwaken)
                _kor.Begin(_plan.CoreColor);

            if (_plan.RuneLetterFlash)
            {
                Color c = new Color(
                    _plan.CoreColor.R * RuleVfxArtDefaults.CoreHdrMult,
                    _plan.CoreColor.G * RuleVfxArtDefaults.CoreHdrMult,
                    _plan.CoreColor.B * RuleVfxArtDefaults.CoreHdrMult,
                    1f);
                _runes.Play(_plan.VerbRuneId, _plan.AdjectiveRuneId, c);
            }

            if (_plan.LightningDashTrail)
            {
                _meshTrail.Begin(_plan.CoreColor, _plan.TrailLifeSec, _dashDir);
                Vector3 tip = BladeTip();
                Vector3 guard = BladeGuard();
                _meshTrail.SampleBlade(guard, tip);
                _lightning.Play(
                    _dashStart + Vector3.up * RuleVfxArtDefaults.DashStartLift,
                    tip,
                    _plan.CoreColor,
                    _plan.TrailLifeSec);
            }

            if (_plan.DragonTailArcSilhouette)
            {
                // ATIL state girişi (kod) — yol boyunca kuyruk yayı kartı.
                Vector3 foreshadow = _dashStart + _dashDir * RuleVfxDefaults.DashForeshadowM;
                _silhouette.PlayAlongLine(
                    _dashStart + Vector3.up * RuleVfxArtDefaults.SilhouettePathLift,
                    foreshadow + Vector3.up * RuleVfxArtDefaults.SilhouettePathLift,
                    _plan.CoreColor,
                    _plan.SilhouetteLifeSec,
                    _plan.DragonAtlasCell);
            }

            if (_plan.WingFootSparks)
                EmitFootWingSparks();
        }

        public void NotifyHit(Vector3 hitOrigin, Vector3 hitDir)
        {
            if (!_active || _hitDone || _plan.IsEmpty)
                return;
            _hitDone = true;
            if (hitDir.sqrMagnitude > 0.0001f)
            {
                _dashDir = hitDir;
                _dashDir.y = 0f;
                _dashDir.Normalize();
            }

            Vector3 target = hitOrigin;
            if (_body != null)
            {
                // İsabet görseli hedef tarafında (boss gövdesi yönünde).
                target = hitOrigin + _dashDir * Mathf.Max(
                    RuleVfxArtDefaults.HitForwardMin,
                    hitOrigin == default ? 0.5f : RuleVfxArtDefaults.HitForwardPad);
            }

            if (_plan.SlashArcOnHit || _plan.ZararClawMarksOnHit)
                SwordAtilHitVfx.Spawn(_fxRoot, target, _dashDir, _plan, _feel);

            _kor?.SignalDelivery();
        }

        public void NotifyMotionEnded(bool stoppedAtBodyEdge)
        {
            if (!_active || _endDone)
                return;
            _endDone = true;
            _meshTrail?.StopEmit();
            _kor?.SignalDelivery();

            if (stoppedAtBodyEdge && _plan.EdgeStopEmberSpark)
                EdgeStopEmberSparks.Spawn(transform.position, _plan.CoreColor, _feel);

            _active = false;
        }

        void LateUpdate()
        {
            if (!_active || !_plan.LightningDashTrail)
                return;

            Vector3 pos = transform.position;
            Vector3 delta = pos - _lastPos;
            if (delta.sqrMagnitude > RuleVfxDefaults.PathSampleEpsSq)
            {
                _dashDir = delta;
                _dashDir.y = 0f;
                if (_dashDir.sqrMagnitude > 0.0001f)
                    _dashDir.Normalize();
            }

            Vector3 tip = BladeTip();
            Vector3 guard = BladeGuard();
            _meshTrail.SampleBlade(guard, tip);
            _lightning.ExtendTip(tip);

            // Hareket kanat kıvılcımı ayak altında (seyrek).
            if (_plan.WingFootSparks
                && delta.sqrMagnitude > RuleVfxDefaults.FootSparkMoveEpsSq
                && Time.frameCount % RuleVfxArtDefaults.FootSparkFrameMod == 0)
                EmitFootWingSparks();

            _lastPos = pos;

            if (_body != null && !_body.IsDisplacing)
                NotifyMotionEnded(_body.SweepRunner != null && _body.SweepRunner.StoppedAtBodyEdge);
        }

        void EmitFootWingSparks()
        {
            Vector3 foot = transform.position;
            foot.y = FeelVfxRuntime.GroundY + RuleVfxDefaults.FootSparkLiftM;
            Color tint = new Color(_plan.CoreColor.R, _plan.CoreColor.G, _plan.CoreColor.B, 1f);
            _feel?.HitSpark(foot, tint, crit: false);
            // İki yana kısa kanat kıvılcımı.
            Vector3 side = Vector3.Cross(Vector3.up, _dashDir).normalized * RuleVfxDefaults.FootWingSideM;
            _feel?.HitSpark(foot + side, tint, crit: false);
            _feel?.HitSpark(foot - side, tint, crit: false);
        }

        Vector3 BladeTip()
        {
            return transform.position
                + Vector3.up * RuleVfxDefaults.WeaponTipLocalY
                + _dashDir * RuleVfxDefaults.BladeTipForwardM;
        }

        Vector3 BladeGuard()
        {
            return transform.position
                + Vector3.up * RuleVfxDefaults.WeaponGuardLocalY
                + _dashDir * RuleVfxDefaults.BladeGuardForwardM;
        }

        void EnsureChildren()
        {
            if (_fxRoot == null)
            {
                var go = new GameObject("RuleDrivenVfx");
                go.transform.SetParent(transform, false);
                _fxRoot = go.transform;
            }

            if (_kor == null)
                _kor = _fxRoot.gameObject.AddComponent<KorAwakenView>();
            if (_meshTrail == null)
                _meshTrail = _fxRoot.gameObject.AddComponent<EmberMeshTrailView>();
            if (_lightning == null)
                _lightning = _fxRoot.gameObject.AddComponent<LightningDashTrailView>();
            if (_silhouette == null)
            {
                var silGo = new GameObject("DragonSilhouette");
                silGo.transform.SetParent(_fxRoot, false);
                _silhouette = silGo.AddComponent<DragonSilhouetteCardView>();
            }

            if (_runes == null)
                _runes = _fxRoot.gameObject.AddComponent<DragonRuneFlashView>();
        }
    }
}
