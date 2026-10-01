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
                _probe.backgroundColor = Camera.main != null ? Camera.main.backgroundColor : _probe.backgroundColor;
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
            _probe.intensity = 0.85f;
            // Skybox null (SceneAtmosphere.Apply) + varsayılan ReflectionProbeClearFlags.Skybox =
            // Unity'nin stok mavi fallback'i; zırh gibi parlak/metalik yüzeylere mavi gökyüzü yansıtıyordu
            // (task-look-v2b problem 1, "source" fix). Gerçek sahne grisiyle eşle.
            _probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            _probe.backgroundColor = Camera.main != null ? Camera.main.backgroundColor : new Color(0.56f, 0.6f, 0.66f);
            _probeRenderPending = true;
        }

        void DisableProbe()
        {
            if (_probe != null)
                _probe.enabled = false;
        }
    }
}
