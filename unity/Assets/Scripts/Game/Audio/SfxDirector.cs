using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Audio
{
    /// <summary>
    /// Tek noktadan ses: <see cref="SfxLibrary"/> olayını küçük bir 2D kaynak havuzunda çalar.
    /// Klip yoksa sessiz geçer (oyun sessiz ama çalışır).
    /// </summary>
    public sealed class SfxDirector : MonoBehaviour
    {
        const int Voices = 10;

        readonly List<AudioSource> _voices = new();
        readonly Dictionary<string, float> _lastPlayed = new();
        readonly Dictionary<string, int> _lastClip = new();
        int _next;
        SfxLibrary _sfx;

        /// <summary>Test/doğrulama: son çalınan olay ve oturumda çalınmış olay kimlikleri.</summary>
        public string LastPlayed { get; private set; }
        public int PlayCount { get; private set; }
        public HashSet<string> PlayedIds { get; } = new();

        public void Bind(SfxLibrary sfx) => _sfx = sfx;

        void Awake()
        {
            for (int i = 0; i < Voices; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _voices.Add(src);
            }
        }

        public void Play(string id, float volumeScale = 1f)
        {
            if (_sfx == null)
                return;
            PlayInternal(id, volumeScale);
        }

        void PlayInternal(string id, float volumeScale)
        {
            SfxLibrary lib = _sfx;
            if (!lib.TryGet(id, out SfxLibrary.Entry e))
                return;

            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(id, out float last) && now - last < e.MinGapSec)
                return;
            _lastPlayed[id] = now;

            int idx = Random.Range(0, e.Clips.Length);
            if (e.Clips.Length > 1 && _lastClip.TryGetValue(id, out int prev) && prev == idx)
                idx = (idx + 1) % e.Clips.Length;
            _lastClip[id] = idx;

            AudioSource src = _voices[_next];
            _next = (_next + 1) % _voices.Count;
            src.pitch = Random.Range(Mathf.Min(e.PitchMin, e.PitchMax), Mathf.Max(e.PitchMin, e.PitchMax));
            src.PlayOneShot(e.Clips[idx], Mathf.Clamp01(e.Volume * volumeScale * lib.MasterVolume));
            LastPlayed = id;
            PlayCount++;
            PlayedIds.Add(id);
        }
    }
}
