using Dovus.Core.Input;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Game.Config;
using Dovus.Game.Feel;
using UnityEngine;

namespace Dovus.Game.Casting
{
    /// <summary>
    /// Her kaydedilen nokta: kÄ±sa titreÅŸim + Ã§alÄ±ÅŸma anÄ±nda Ã¼retilmiÅŸ hece sesi (Â§9).
    /// Dosya baÄŸÄ±mlÄ±lÄ±ÄŸÄ± yok â€” AudioClip.Create. Hece adÄ± Core RuneInfo'dan.
    /// </summary>
    public sealed class SyllableFeedback : MonoBehaviour
    {
        PrototypeTuning _tuning;
        AudioSource _source;
        readonly AudioClip[] _clips = new AudioClip[7]; // index 1..6
        AudioClip _denyClip;

        // Frekanslar spec'te yok; hece adÄ±/sÄ±rasÄ± RuneInfo'dan gelir.
        static readonly float[] BaseHz =
        {
            0f,
            440f, // 1 AteÅŸ
            494f, // 2 AydÄ±nlÄ±k
            370f, // 3 YÄ±ldÄ±rÄ±m
            330f, // 4 Su
            294f, // 5 KaranlÄ±k
            262f  // 6 Toprak
        };

        public void Configure(PrototypeTuning tuning) => _tuning = tuning;

        void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            for (int dot = 1; dot <= HexagonLayout.DotCount; dot++)
            {
                if (!RuneInfo.TryFromDot(dot, out Rune rune))
                    continue;
                string syllable = RuneInfo.Syllable(rune);
                _clips[dot] = BuildClip(syllable, BaseHz[dot]);
            }
        }

        void OnDestroy()
        {
            for (int i = 0; i < _clips.Length; i++)
            {
                if (_clips[i] != null)
                    Destroy(_clips[i]);
            }
            if (_denyClip != null)
                Destroy(_denyClip);
        }

        /// <summary>dotIndex 1..6; sentenceDotsAfter = cÃ¼mledeki nokta sayÄ±sÄ± (perde yÃ¼kselir).</summary>
        public void PlayForDot(int dot, int sentenceDotsAfter, bool playHaptic = false)
        {
            if (dot < 1 || dot > HexagonLayout.DotCount || _clips[dot] == null)
                return;

            float pitch = 1f + 0.09f * Mathf.Max(0, sentenceDotsAfter - 1);
            _source.pitch = Mathf.Clamp(pitch, 0.85f, 1.55f);
            _source.PlayOneShot(_clips[dot], 0.7f);

            if (!playHaptic)
                return;
            long ms = _tuning != null ? _tuning.Input.DotVibrationMs : 30L;
            FeelHaptics.Pulse((int)ms);
        }

        /// <summary>BaÄŸlama 3: yetersiz mana â€” dÃ¼ÅŸÃ¼k kÄ±sa buzz (hece frekanslarÄ±ndan ayrÄ±).</summary>
        public void PlayDenied()
        {
            if (_source == null)
                return;
            if (_denyClip == null)
                _denyClip = BuildClip("deny", 120f);
            _source.pitch = 0.85f;
            _source.PlayOneShot(_denyClip, 0.45f);
            FeelHaptics.Pulse(20);
        }

        static AudioClip BuildClip(string syllable, float hz)
        {
            const int sampleRate = 22050;
            const float durationSec = 0.09f;
            int samples = Mathf.CeilToInt(sampleRate * durationSec);
            string name = string.IsNullOrEmpty(syllable) ? "syl" : $"syl_{syllable}";
            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float env = 1f - t / durationSec;
                env *= env; // hÄ±zlÄ± sÃ¶nÃ¼m â€” hece tÄ±kÄ±rtÄ±sÄ±
                data[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * env * 0.55f;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
