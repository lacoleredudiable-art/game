using Dovus.Core.Input;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Game.Config;
using Dovus.Game.Feel;
using UnityEngine;

namespace Dovus.Game.Casting
{
    /// <summary>
    /// Her kaydedilen nokta: kısa titreşim + çalışma anında üretilmiş hece sesi (§9).
    /// Dosya bağımlılığı yok — AudioClip.Create. Hece adı Core RuneInfo'dan.
    /// </summary>
    public sealed class SyllableFeedback : MonoBehaviour
    {
        GameTuning _tuning;
        AudioSource _source;
        readonly AudioClip[] _clips = new AudioClip[7]; // index 1..6
        AudioClip _denyClip;

        // Frekanslar spec'te yok; hece adı/sırası RuneInfo'dan gelir.
        static readonly float[] BaseHz =
        {
            0f,
            SyllableFeedbackDefaults.DotFireHz, // 1 Ateş
            SyllableFeedbackDefaults.DotLightHz, // 2 Aydınlık
            SyllableFeedbackDefaults.DotLightningHz, // 3 Yıldırım
            SyllableFeedbackDefaults.DotWaterHz, // 4 Su
            SyllableFeedbackDefaults.DotDarkHz, // 5 Karanlık
            SyllableFeedbackDefaults.DotEarthHz  // 6 Toprak
        };

        public void Configure(GameTuning tuning) => _tuning = tuning;

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

        /// <summary>dotIndex 1..6; sentenceDotsAfter = cümledeki nokta sayısı (perde yükselir).</summary>
        public void PlayForDot(int dot, int sentenceDotsAfter, bool playHaptic = false)
        {
            if (dot < 1 || dot > HexagonLayout.DotCount || _clips[dot] == null)
                return;

            float pitch = 1f + SyllableFeedbackDefaults.SentencePitchStep * Mathf.Max(0, sentenceDotsAfter - 1);
            _source.pitch = Mathf.Clamp(pitch, SyllableFeedbackDefaults.PitchClampMin, SyllableFeedbackDefaults.PitchClampMax);
            _source.PlayOneShot(_clips[dot], SyllableFeedbackDefaults.DotOneShotVolume);

            if (!playHaptic)
                return;
            long ms = _tuning != null ? _tuning.Input.DotVibrationMs : SyllableFeedbackDefaults.FallbackDotVibrationMs;
            FeelHaptics.Pulse((int)ms);
        }

        /// <summary>Bağlama 3: yetersiz mana — düşük kısa buzz (hece frekanslarından ayrı).</summary>
        public void PlayDenied()
        {
            if (_source == null)
                return;
            if (_denyClip == null)
                _denyClip = BuildClip("deny", SyllableFeedbackDefaults.DenyHz);
            _source.pitch = SyllableFeedbackDefaults.DenyPitch;
            _source.PlayOneShot(_denyClip, SyllableFeedbackDefaults.DenyOneShotVolume);
            FeelHaptics.Pulse(SyllableFeedbackDefaults.DenyHapticMs);
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
                env *= env; // hızlı sönüm — hece tıkırtısı
                data[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * env * SyllableFeedbackDefaults.ClipSampleAmp;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
