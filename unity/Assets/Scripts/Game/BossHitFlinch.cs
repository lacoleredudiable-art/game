using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>İsabet yönünde kısa yerel itme — simülasyon konumu değil, görsel kök.</summary>
    public sealed class BossHitFlinch : MonoBehaviour
    {
        FeelTuning _feel;
        Transform _visualRoot;
        Vector3 _baseLocal;
        Vector3 _kickLocal;
        float _untilUnscaled;

        public void Bind(FeelTuning feel, Transform visualRoot = null)
        {
            _feel = feel;
            _visualRoot = visualRoot != null ? visualRoot : transform;
            _baseLocal = _visualRoot.localPosition;
        }

        public void KickFromWorldPoint(Vector3 worldHit, Vector3 bossCenter)
        {
            if (_feel == null || _feel.BossFlinchMs <= 0 || _feel.BossFlinchOffsetM <= 0f)
                return;

            Vector3 dir = worldHit - bossCenter;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f)
                dir = Vector3.forward;
            dir.Normalize();
            Vector3 local = _visualRoot.parent != null
                ? _visualRoot.parent.InverseTransformDirection(dir)
                : dir;
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
