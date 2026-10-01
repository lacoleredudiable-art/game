using UnityEngine;

namespace Dovus.Visual
{
    /// <summary>
    /// Deneme sahnesi: ejderhanın yavaş nefesi. Hedefin ölçeğini ve yüksekliğini sinüsle oynatır,
    /// başı hafifçe kaldırıp indirir. Yalnız görsel; oynanış koduna bağlı değil.
    /// Ejderha değişince (Dungeon Mason) yalnız <see cref="target"/> yeni görsele bağlanır.
    /// </summary>
    public sealed class DragonBreathing : MonoBehaviour
    {
        [Tooltip("Oynatılacak görsel (boşsa bu nesne).")]
        public Transform target;
        [Tooltip("Bir nefesin süresi (sn).")]
        public float period = 5.5f;
        [Tooltip("Ölçek genliği (0.015 = %1.5).")]
        public float scaleAmplitude = 0.015f;
        [Tooltip("Dikey kayma genliği (metre, dünya).")]
        public float heaveMeters = 0.35f;
        [Tooltip("Baş eğimi genliği (derece).")]
        public float pitchDegrees = 0.8f;

        Vector3 _baseScale, _basePos;
        Quaternion _baseRot;
        bool _captured;

        void OnEnable()
        {
            if (target == null) target = transform;
            Capture();
        }

        /// <summary>Görsel değiştiyse dinlenme pozunu yeniden al.</summary>
        public void Capture()
        {
            if (target == null) return;
            _baseScale = target.localScale;
            _basePos = target.localPosition;
            _baseRot = target.localRotation;
            _captured = true;
        }

        void OnDisable()
        {
            if (!_captured || target == null) return;
            target.localScale = _baseScale;
            target.localPosition = _basePos;
            target.localRotation = _baseRot;
        }

        void Update()
        {
            if (!_captured || target == null || period <= 0.01f) return;
            float t = Time.time * (Mathf.PI * 2f / period);
            // Nefes: yavaş al, biraz daha hızlı ver (asimetrik eğri).
            float b = Mathf.Sin(t);
            b = b >= 0f ? Mathf.Pow(b, 0.8f) : -Mathf.Pow(-b, 1.25f);
            target.localScale = _baseScale * (1f + scaleAmplitude * b);
            Vector3 heave = Vector3.up * (heaveMeters * b);
            if (target.parent != null) heave = target.parent.InverseTransformVector(heave);
            target.localPosition = _basePos + heave;
            target.localRotation = _baseRot * Quaternion.Euler(-pitchDegrees * b, 0f, 0f);
        }
    }
}
