using Dovus.Core.Grammar;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Her kaydedilen nokta: kısa titreşim + çalışma anında üretilmiş hece sesi (§9).
    /// Dosya bağımlılığı yok — AudioClip.Create.
    /// </summary>
    public sealed class SyllableFeedback : MonoBehaviour
    {
        AudioSource _source;
        readonly AudioClip[] _clips = new AudioClip[6]; // index 1..5

        static readonly float[] BaseHz =
        {
            0f,
            440f, // hi — İĞNE
            370f, // hu — SÜRÜ
            330f, // ho — KABUK
            294f, // he — ZEHİR
            523f  // ha — SARSINTI
        };

        void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            for (int dot = 1; dot <= 5; dot++)
                _clips[dot] = BuildClip(dot, BaseHz[dot]);
        }

        void OnDestroy()
        {
            for (int i = 0; i < _clips.Length; i++)
            {
                if (_clips[i] != null)
                    Destroy(_clips[i]);
            }
        }

        /// <summary>dotIndex 1..5; sentenceDotsAfter = cümledeki nokta sayısı (perde yükselir).</summary>
        public void PlayForDot(int dot, int sentenceDotsAfter)
        {
            if (dot < 1 || dot > 5)
                return;

            float pitch = 1f + 0.09f * Mathf.Max(0, sentenceDotsAfter - 1);
            _source.pitch = Mathf.Clamp(pitch, 0.85f, 1.55f);
            _source.PlayOneShot(_clips[dot], 0.7f);

            if (Application.isMobilePlatform)
                Handheld.Vibrate();
        }

        public void PlayForRune(Rune rune, int sentenceDotsAfter) =>
            PlayForDot((int)rune, sentenceDotsAfter);

        static AudioClip BuildClip(int dot, float hz)
        {
            const int sampleRate = 22050;
            const float durationSec = 0.09f;
            int samples = Mathf.CeilToInt(sampleRate * durationSec);
            var clip = AudioClip.Create($"syl_{dot}", samples, 1, sampleRate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float env = 1f - t / durationSec;
                env *= env; // hızlı sönüm — hece tıkırtısı
                data[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * env * 0.55f;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
