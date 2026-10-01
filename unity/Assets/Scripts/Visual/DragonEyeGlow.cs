using UnityEngine;

namespace Dovus.Visual
{
    /// <summary>
    /// Deneme sahnesi: ejderha gözlerinin açılması. Gecikmeden sonra göz kapağı (Y ölçeği) açılır ve
    /// parlaklık 0'dan hedefe çıkar; sonra çok hafif titrer. Parlaklık MaterialPropertyBlock ile
    /// <c>_Intensity</c> özelliğine yazılır (Dovus/Visual/LavaEmissive). Shader yoksa _BaseColor çarpılır.
    /// </summary>
    public sealed class DragonEyeGlow : MonoBehaviour
    {
        public Renderer[] eyes = new Renderer[0];
        [Tooltip("Sahne açıldıktan sonra bekleme (sn).")]
        public float openDelay = 3.5f;
        [Tooltip("Açılma süresi (sn).")]
        public float openDuration = 2.2f;
        [Tooltip("Tam açık parlaklık çarpanı.")]
        public float maxIntensity = 1f;
        [Tooltip("Açıkken titreme genliği (0-1).")]
        public float flicker = 0.08f;
        [Tooltip("Döngü: kapanıp yeniden açılır (0 = kapalı).")]
        public float blinkEverySeconds = 0f;

        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        MaterialPropertyBlock _mpb;
        Vector3[] _eyeScales;
        float _start;

        void OnEnable()
        {
            _mpb ??= new MaterialPropertyBlock();
            _eyeScales = new Vector3[eyes.Length];
            for (int i = 0; i < eyes.Length; i++)
                if (eyes[i] != null) _eyeScales[i] = eyes[i].transform.localScale;
            Restart();
        }

        void OnDisable()
        {
            if (_eyeScales == null) return;
            for (int i = 0; i < eyes.Length && i < _eyeScales.Length; i++)
                if (eyes[i] != null) eyes[i].transform.localScale = _eyeScales[i];
        }

        public void Restart() { _start = Time.time; }

        void Update()
        {
            float t = Time.time - _start;
            if (blinkEverySeconds > 0f && t > openDelay + openDuration + blinkEverySeconds)
            {
                Restart();
                t = 0f;
            }
            float open = Mathf.Clamp01((t - openDelay) / Mathf.Max(0.01f, openDuration));
            open = open * open * (3f - 2f * open);
            float glow = open * maxIntensity;
            if (open >= 1f && flicker > 0f)
                glow *= 1f - flicker * Mathf.PerlinNoise(Time.time * 1.7f, 0.37f);

            for (int i = 0; i < eyes.Length; i++)
            {
                Renderer r = eyes[i];
                if (r == null) continue;
                Vector3 s = _eyeScales[i];
                r.transform.localScale = new Vector3(s.x, s.y * Mathf.Lerp(0.05f, 1f, open), s.z);
                r.enabled = open > 0.001f;
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(IntensityId, glow);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
