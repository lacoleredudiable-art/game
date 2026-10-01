using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game
{
    /// <summary>
    /// PostFX Volume üzerinde A/B/C ön ayarını uygular; C için yansıma probu.
    /// </summary>
    public sealed class LookPresetController : MonoBehaviour
    {
        Volume _volume;
        Light _sun;
        ReflectionProbe _probe;
        bool _probeRenderPending;

        public void Initialize(Volume volume, Light sun)
        {
            _volume = volume;
            _sun = sun;
            LookPresets.Bind(this, volume, sun);

            char preset = 'A';
#if UNITY_EDITOR || DOVUS_DEBUG
            string saved = PlayerPrefs.GetString(LookPresets.PlayerPrefsKey, "A");
            if (!string.IsNullOrEmpty(saved))
                preset = char.ToUpperInvariant(saved[0]);
#endif
            LookPresets.Apply(preset);
        }

        void Update()
        {
#if UNITY_EDITOR || DOVUS_DEBUG
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.f2Key.wasPressedThisFrame)
            {
                char next = LookPresets.Active switch
                {
                    'A' => 'B',
                    'B' => 'C',
                    _ => 'A',
                };
                LookPresets.Apply(next);
            }
#endif
            if (_probeRenderPending && _probe != null)
            {
                _probe.RenderProbe();
                _probeRenderPending = false;
            }
        }

        void OnDestroy() => LookPresets.RestoreOnExit();

        internal void ApplyPresetExtras(char preset)
        {
            if (preset == 'C')
                EnsureProbe();
            else
                DisableProbe();
        }

        internal void RestoreExtras() => DisableProbe();

        public void ForceProbeRender()
        {
            if (_probe == null)
                return;
            _probe.RenderProbe();
            _probeRenderPending = false;
        }

        void EnsureProbe()
        {
            if (_probe != null)
            {
                _probe.enabled = true;
                return;
            }

            var go = new GameObject("LookReflectionProbe");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, 6f, 0f);
            _probe = go.AddComponent<ReflectionProbe>();
            _probe.mode = ReflectionProbeMode.Realtime;
            _probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            _probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            _probe.resolution = 128;
            _probe.size = new Vector3(80f, 30f, 80f);
            _probe.intensity = 0.65f;
            _probeRenderPending = true;
        }

        void DisableProbe()
        {
            if (_probe != null)
                _probe.enabled = false;
        }
    }
}
