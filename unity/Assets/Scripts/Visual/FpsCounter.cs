using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dovus.Visual
{
    /// <summary>
    /// Deneme sahnesi: FPS / ms sayacı + teşhis satırı (gerçek render çözünürlüğü, render scale,
    /// MSAA, kalite seviyesi). Düğmeyle kenar yumuşatma modu değiştirilir (MSAA / SMAA / FXAA / kapalı)
    /// ki cihazda tırtıklığın ne kadarının aliasing'den geldiği görülsün. IMGUI kullanır (yeni Input
    /// System ile de çalışır).
    /// </summary>
    public sealed class FpsCounter : MonoBehaviour
    {
        public Camera targetCamera;
        public CameraPushIn pushIn;
        [Tooltip("Ölçüm penceresi (sn).")]
        public float sampleWindow = 0.5f;
        [Tooltip("Ölçüm için kare tavanı. 30 FPS hedefinin payını görmek için 60.")]
        public int targetFrameRate = 60;

        static readonly string[] AaNames = { "MSAA (asset)", "SMAA", "FXAA", "AA off" };
        int _aaMode;
        float _acc, _worst;
        int _frames;
        string _line1 = "", _line2 = "";
        GUIStyle _style, _button;

        void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            if (targetCamera == null) targetCamera = Camera.main;
            ApplyAa();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _acc += dt; _frames++;
            if (dt > _worst) _worst = dt;
            if (_acc < sampleWindow) return;
            float fps = _frames / _acc;
            _line1 = $"{fps:0.0} FPS  {1000f * _acc / _frames:0.0} ms  (worst {1000f * _worst:0} ms)";
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            int msaa = urp != null ? urp.msaaSampleCount : QualitySettings.antiAliasing;
            float scale = urp != null ? urp.renderScale : 1f;
            int q = QualitySettings.GetQualityLevel();
            string qName = q >= 0 && q < QualitySettings.names.Length ? QualitySettings.names[q] : q.ToString();
            int rw = targetCamera != null ? targetCamera.pixelWidth : Screen.width;
            int rh = targetCamera != null ? targetCamera.pixelHeight : Screen.height;
            _line2 = $"{rw}x{rh} x{scale:0.##}  MSAA {msaa}x  AA: {AaNames[_aaMode]}  Q: {qName}  skin {QualitySettings.skinWeights}";
            _acc = 0f; _frames = 0; _worst = 0f;
        }

        void ApplyAa()
        {
            if (targetCamera == null) return;
            var data = targetCamera.GetComponent<UniversalAdditionalCameraData>();
            targetCamera.allowMSAA = _aaMode == 0;
            if (data == null) return;
            switch (_aaMode)
            {
                case 1:
                    data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    data.antialiasingQuality = AntialiasingQuality.Medium;
                    break;
                case 2:
                    data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                    break;
                default:
                    data.antialiasing = AntialiasingMode.None;
                    break;
            }
        }

        void OnGUI()
        {
            float k = Mathf.Max(1f, Screen.height / 1080f);
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 22 };
                _style.normal.textColor = new Color(0.92f, 0.93f, 0.94f, 1f);
                _button = new GUIStyle(GUI.skin.button) { fontSize = 20 };
            }
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(k, k, 1f));
            GUI.color = new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(new Rect(8, 8, 820, 62), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(16, 10, 820, 30), _line1, _style);
            GUI.Label(new Rect(16, 38, 820, 30), _line2, _style);
            if (GUI.Button(new Rect(8, 76, 200, 44), "AA: " + AaNames[_aaMode], _button))
            {
                _aaMode = (_aaMode + 1) % AaNames.Length;
                ApplyAa();
            }
            if (pushIn != null && GUI.Button(new Rect(216, 76, 200, 44), "Kamera bastan", _button))
                pushIn.Restart();
            GUI.matrix = old;
        }
    }
}
