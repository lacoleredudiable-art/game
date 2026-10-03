using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game.Boss
{
    /// <summary>İsabet yönünde kısa göğüs ofseti — ayak/kök simülasyonu değil, yalnız üst gövde.</summary>
    public sealed class BossHitFlinch : MonoBehaviour
    {
        FeelTuning _feel;
        Transform _visualRoot;
        Vector3 _baseLocal;
        Vector3 _kickLocal;
        float _untilUnscaled;

        public void Bind(FeelTuning feel, Animator animator)
        {
            _feel = feel;
            _visualRoot = null;
            if (animator != null)
            {
                if (animator.isHuman)
                {
                    Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest)
                        ?? animator.GetBoneTransform(HumanBodyBones.UpperChest)
                        ?? animator.GetBoneTransform(HumanBodyBones.Spine);
                    if (chest != null)
                        _visualRoot = chest;
                }

                if (_visualRoot == null && animator.transform != transform)
                    _visualRoot = animator.transform;
            }

            if (_visualRoot == null)
                _visualRoot = transform;
            _baseLocal = _visualRoot.localPosition;
        }

        public void KickFromWorldPoint(Vector3 worldHit, Vector3 bossCenter)
        {
            if (_feel == null || _feel.BossFlinchMs <= 0 || _feel.BossFlinchOffsetM <= 0f)
                return;

            _baseLocal = _visualRoot.localPosition;

            Vector3 dir = worldHit - bossCenter;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f)
                dir = Vector3.forward;
            dir.Normalize();
            Vector3 local = _visualRoot.parent != null
                ? _visualRoot.parent.InverseTransformDirection(dir)
                : dir;
            local.y = 0f;
            _kickLocal = local * _feel.BossFlinchOffsetM;
            _untilUnscaled = Time.unscaledTime + _feel.BossFlinchMs / 1000f;
        }

        void LateUpdate()
        {
            if (_visualRoot == null || _feel == null)
                return;

            float left = _untilUnscaled - Time.unscaledTime;
            if (left <= 0f)
            {
                _visualRoot.localPosition = _baseLocal;
                return;
            }

            float t = 1f - left / Mathf.Max(0.001f, _feel.BossFlinchMs / 1000f);
            float k = (1f - t) * (1f - t);
            _visualRoot.localPosition = _baseLocal + _kickLocal * k;
        }

        void OnDisable()
        {
            if (_visualRoot != null)
                _visualRoot.localPosition = _baseLocal;
        }
    }
}
