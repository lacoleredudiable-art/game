using Dovus.Core.Grammar;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Her kaydedilen nokta: kısa titreşim + çalışma anında üretilmiş hece sesi (§9).
    /// Dosya bağımlılığı yok — AudioClip.Create. Hece adı Core RuneInfo'dan.
    /// </summary>
    public sealed class SyllableFeedback : MonoBehaviour
    {
        PrototypeTuning _tuning;
        AudioSource _source;
        readonly AudioClip[] _clips = new AudioClip[7]; // index 1..6

        // Frekanslar spec'te yok; hece adı/sırası RuneInfo'dan gelir.
        static readonly float[] BaseHz =
        {
            0f,
            440f, // 1 Ateş
            494f, // 2 Aydınlık
            370f, // 3 Yıldırım
            330f, // 4 Su
            294f, // 5 Karanlık
            262f  // 6 Toprak
        };

        public void Configure(PrototypeTuning tuning) => _tuning = tuning;

        void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            for (int dot = 1; dot <= PentagonLayout.DotCount; dot++)
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
        }

        /// <summary>dotIndex 1..6; sentenceDotsAfter = cümledeki nokta sayısı (perde yükselir).</summary>
        public void PlayForDot(int dot, int sentenceDotsAfter)
        {
            if (dot < 1 || dot > PentagonLayout.DotCount || _clips[dot] == null)
                return;

            float pitch = 1f + 0.09f * Mathf.Max(0, sentenceDotsAfter - 1);
            _source.pitch = Mathf.Clamp(pitch, 0.85f, 1.55f);
            _source.PlayOneShot(_clips[dot], 0.7f);

            long ms = _tuning != null ? _tuning.DotVibrationMs : 30L;
            TryShortVibrate(ms);
        }

        public void PlayForRune(Rune rune, int sentenceDotsAfter) =>
            PlayForDot((int)rune, sentenceDotsAfter);

        static void TryShortVibrate(long durationMs)
        {
            if (durationMs <= 0)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator == null)
                    return;

                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                int sdk = version.GetStatic<int>("SDK_INT");
                if (sdk >= 26)
                {
                    using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                    // DEFAULT_AMPLITUDE = -1
                    using var effect = effectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot", durationMs, -1);
                    vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", durationMs);
                }
            }
            catch (System.Exception)
            {
                // Emülatör / izin yok — sessizce atla.
            }
#endif
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
                data[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * env * 0.55f;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
