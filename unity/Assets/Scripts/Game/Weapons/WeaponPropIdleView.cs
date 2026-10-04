using UnityEngine;

namespace Dovus.Game.Weapons
{
    /// <summary>Sunum: off-hand küre vb. için yavaş bob/döndürme (unscaled time).</summary>
    public sealed class WeaponPropIdleView : MonoBehaviour
    {
        [SerializeField] float _bobAmplitudeM = 0.02f;
        [SerializeField] float _bobHz = 0.35f;
        [SerializeField] float _rotateDegPerSec = 18f;
        [SerializeField] Vector3 _bobAxisLocal = Vector3.up;
        [SerializeField] Vector3 _rotateAxisLocal = Vector3.up;

        Vector3 _baseLocalPos;

        void OnEnable()
        {
            _baseLocalPos = transform.localPosition;
        }

        void Update()
        {
            float t = Time.unscaledTime;
            float bob = Mathf.Sin(t * Mathf.PI * 2f * _bobHz) * _bobAmplitudeM;
            transform.localPosition = _baseLocalPos + _bobAxisLocal.normalized * bob;
            transform.localRotation *= Quaternion.AngleAxis(_rotateDegPerSec * Time.unscaledDeltaTime, _rotateAxisLocal);
        }
    }
}
