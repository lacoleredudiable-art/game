using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// T11: kare süresi göstergesi. Ölçüm turunun tek sayısal aracı — his değil bütçe okunur.
    /// Sol-alt köşe: can barları sol-ÜSTTE, tepki yazısı/debug metni sağda, ayar düğmesi sağ-altta.
    /// </summary>
    public sealed class FrameTimeHud : MonoBehaviour
    {
        PrototypeTuning _tuning;
        Text _text;
        readonly StringBuilder _sb = new StringBuilder(64);

        float _windowSec;
        int _windowFrames;
        float _windowSumMs;
        float _windowWorstMs;

        bool _appliedVisible;

        public void Configure(PrototypeTuning tuning, Transform canvasRoot)
        {
            _tuning = tuning;

            var go = new GameObject("FrameTimeHud");
            go.transform.SetParent(canvasRoot, false);
            // Overlay kameranın cullingMask'i yalnızca UI; yeni GameObject Default'ta doğar (T8.1).
            if (canvasRoot != null)
                go.layer = canvasRoot.gameObject.layer;

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.02f, 0.02f);
            rect.anchorMax = new Vector2(0.40f, 0.14f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _text = go.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_text.font == null)
                _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _text.fontSize = 22;
            _text.color = new Color(0.9f, 0.95f, 1f, 0.9f);
            _text.alignment = TextAnchor.LowerLeft;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;

            // Kapalıyken bileşen DEVRE DIŞI: alfa 0 bir Graphic yine de geometri üretip
            // harmanlanır (T8.1 denetimi 12, T9.1 madde 2).
            _appliedVisible = true;
            ApplyVisibility(_tuning != null && _tuning.ShowFrameTimeHud);
        }

        void ApplyVisibility(bool visible)
        {
            if (visible == _appliedVisible)
                return;

            _appliedVisible = visible;
            _text.enabled = visible;
            if (visible)
                ResetWindow();
        }

        void ResetWindow()
        {
            _windowSec = 0f;
            _windowFrames = 0;
            _windowSumMs = 0f;
            _windowWorstMs = 0f;
        }

        void Update()
        {
            if (_text == null || _tuning == null)
                return;

            // T9.1 deseni: panelin canlı yazdığı alan her karede karşılaştırılır, değiştiyse uygulanır.
            ApplyVisibility(_tuning.ShowFrameTimeHud);
            if (!_appliedVisible)
                return;

            // Yavaş çekim/hitstop kare süresini DEĞİŞTİRMEZ, yalnızca dünya zamanını ölçekler:
            // bütçe ölçümü ölçeklenmemiş saatte olmak zorunda (T4).
            float dt = Time.unscaledDeltaTime;
            float ms = dt * 1000f;

            _windowSec += dt;
            _windowFrames++;
            _windowSumMs += ms;
            if (ms > _windowWorstMs)
                _windowWorstMs = ms;

            float sample = Mathf.Max(0.05f, _tuning.FrameTimeSampleSec);
            if (_windowSec < sample || _windowFrames == 0)
                return;

            float avgMs = _windowSumMs / _windowFrames;
            float fps = avgMs > 0.0001f ? 1000f / avgMs : 0f;

            _sb.Clear();
            _sb.Append(avgMs.ToString("0.0")).Append(" ms · ").Append(fps.ToString("0")).Append(" fps");
            _sb.Append("\nen kötü ").Append(_windowWorstMs.ToString("0.0")).Append(" ms");
            _sb.Append(" · hedef ").Append(_tuning.TargetFrameRateHz);
            _text.text = _sb.ToString();

            ResetWindow();
        }
    }
}
