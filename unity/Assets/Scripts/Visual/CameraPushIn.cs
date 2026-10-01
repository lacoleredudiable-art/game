using UnityEngine;

namespace Dovus.Visual
{
    /// <summary>
    /// Deneme sahnesi: yavaş, alçak kamera yaklaşması. Başlangıçtan bitişe yumuşak eğriyle gider,
    /// hep <see cref="lookAt"/> noktasına bakar, çok hafif el kamerası salınımı ekler.
    /// </summary>
    public sealed class CameraPushIn : MonoBehaviour
    {
        public Vector3 startPosition = new Vector3(1.1f, 1.35f, -8f);
        public Vector3 endPosition = new Vector3(0.55f, 1.05f, -3.9f);
        public Vector3 lookAt = new Vector3(-0.6f, 9.5f, 60f);
        [Tooltip("Yaklaşma süresi (sn).")]
        public float duration = 24f;
        public float startDelay = 0.5f;
        [Tooltip("Bitince baştan başla.")]
        public bool loop = true;
        public float holdAtEnd = 4f;
        [Tooltip("El kamerası salınımı (metre).")]
        public float swayMeters = 0.03f;

        float _start;

        void OnEnable() { Restart(); }

        public void Restart()
        {
            _start = Time.time;
            Apply(0f);
        }

        void LateUpdate()
        {
            float t = (Time.time - _start - startDelay) / Mathf.Max(0.01f, duration);
            if (loop && t > 1f + holdAtEnd / Mathf.Max(0.01f, duration)) { Restart(); return; }
            Apply(Mathf.Clamp01(t));
        }

        void Apply(float t)
        {
            float e = t * t * (3f - 2f * t);
            Vector3 p = Vector3.Lerp(startPosition, endPosition, e);
            if (swayMeters > 0f)
            {
                float s = Time.time * 0.23f;
                p += new Vector3(Mathf.PerlinNoise(s, 0.1f) - 0.5f, Mathf.PerlinNoise(0.7f, s) - 0.5f, 0f) * (2f * swayMeters);
            }
            transform.position = p;
            transform.rotation = Quaternion.LookRotation(lookAt - p, Vector3.up);
        }
    }
}
