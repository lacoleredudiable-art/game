using Dovus.Core.Combat;
using Dovus.Core.Tuning;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Sıyırma/vurulma hissi: hitstop, yavaş çekim, impact frame, vinyet, kamera yumruğu.
    /// Ekran katmanı Overlay değil — Overlay kamera üzerinde Screen Space Camera (§10).
    /// </summary>
    public sealed class CombatFeel : MonoBehaviour
    {
        CombatTuning _combat;
        GameClock _clock;
        FollowCamera _follow;
        Camera _worldCam;
        AudioLowPassFilter _lowpass;
        SentenceEngineBridge _hud;

        Canvas _canvas;
        Image _impact;
        Image _vignette;
        Image _threatFlash;
        float _impactUntil;
        float _vignetteUntil;
        float _threatUntil;
        float _baseCutoff = 22000f;

        public ExchangeResult? LastExchange { get; private set; }

        public void Bind(
            GameClock clock,
            FollowCamera follow,
            CombatTuning combat,
            Camera overlayCam,
            SentenceDebugHud hud)
        {
            _clock = clock;
            _follow = follow;
            _combat = combat;
            _hud = new SentenceEngineBridge(hud);
            _worldCam = Camera.main;
            if (_worldCam != null)
            {
                _lowpass = _worldCam.GetComponent<AudioLowPassFilter>();
                if (_lowpass == null)
                    _lowpass = _worldCam.gameObject.AddComponent<AudioLowPassFilter>();
                _lowpass.cutoffFrequency = _baseCutoff;
            }

            BuildCanvas(overlayCam);
        }

        public void ShowThreat(float progress01)
        {
            if (_threatFlash == null)
                return;

            float p = Mathf.Clamp01(progress01);
            float pulse = 0.15f + 0.55f * p + 0.15f * Mathf.Sin(Time.unscaledTime * (4f + 10f * p));
            Color c = Color.Lerp(
                new Color(1f, 0.604f, 0.235f, 0f),
                new Color(1f, 0.302f, 0.141f, pulse * 0.35f),
                p);
            _threatFlash.color = c;
            _threatUntil = Time.unscaledTime + 0.05f;
        }

        public void ClearThreat()
        {
            if (_threatFlash != null)
                _threatFlash.color = Color.clear;
        }

        public void OnExchange(ExchangeResult result)
        {
            LastExchange = result;
            FeelTuning feel = _combat.Feel;
            SlowmoTuning slowmo = _combat.Slowmo;

            if (result.Outcome == ExchangeOutcome.Dodged)
            {
                _clock.Director.TriggerHitstop(feel.HitstopPerfectMs);
                if (result.Grade.HasValue && result.Grade.Value <= slowmo.SlowmoMinGrade)
                    _clock.Director.TriggerSlowmo();

                float kick = result.Grade == DodgeGrade.Mukemmel
                    ? feel.CameraPerfectZoomKick
                    : feel.CameraDodgeZoomKick;
                _follow?.Punch(kick, feel.CameraRollDeg, feel.ShakePerfectPx, feel.ShakeDecay);
                FlashImpact(feel.ImpactFrameMs);
                _hud.NoteExchange(result);
                return;
            }

            if (result.Outcome == ExchangeOutcome.Hit)
            {
                _clock.Director.TriggerHitstop(feel.HitstopPlayerHitMs);
                _follow?.Punch(feel.CameraDodgeZoomKick, feel.CameraRollDeg, feel.ShakeHitPx, feel.ShakeDecay);
                ShowVignette(0.85f);
                _hud.NoteExchange(result);
            }
        }

        void LateUpdate()
        {
            float now = Time.unscaledTime;
            if (_impact != null)
            {
                float t = _impactUntil - now;
                _impact.color = t > 0f
                    ? new Color(1f, 1f, 1f, Mathf.Clamp01(t / 0.04f))
                    : Color.clear;
            }

            if (_vignette != null)
            {
                float t = _vignetteUntil - now;
                _vignette.color = t > 0f
                    ? new Color(0.7f, 0.05f, 0.02f, 0.55f * Mathf.Clamp01(t / 0.45f))
                    : Color.clear;
            }

            if (_threatFlash != null && now > _threatUntil)
                _threatFlash.color = Color.clear;

            if (_lowpass != null && _clock != null)
            {
                _lowpass.cutoffFrequency = _clock.Director.IsSlowmoActive
                    ? _combat.Slowmo.AudioLowpassHz
                    : _baseCutoff;
            }
        }

        void FlashImpact(int ms)
        {
            _impactUntil = Time.unscaledTime + ms / 1000f;
        }

        void ShowVignette(float holdSec)
        {
            _vignetteUntil = Time.unscaledTime + holdSec;
        }

        void BuildCanvas(Camera overlayCam)
        {
            var go = new GameObject("FeelCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = overlayCam != null
                ? RenderMode.ScreenSpaceCamera
                : RenderMode.ScreenSpaceOverlay;
            _canvas.worldCamera = overlayCam;
            _canvas.planeDistance = 0.8f;
            _canvas.sortingOrder = 200;
            if (overlayCam != null)
            {
                int layer = 0;
                int mask = overlayCam.cullingMask;
                for (int i = 0; i < 32; i++)
                {
                    if ((mask & (1 << i)) != 0)
                    {
                        layer = i;
                        break;
                    }
                }

                go.layer = layer;
            }

            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            go.AddComponent<GraphicRaycaster>();

            _impact = CreateFull(go.transform, "Impact", Color.clear);
            _vignette = CreateFull(go.transform, "Vignette", Color.clear);
            _threatFlash = CreateFull(go.transform, "ThreatFlash", Color.clear);

            if (overlayCam != null)
                SetLayerRecursively(go, go.layer);
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
        }

        static Image CreateFull(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        readonly struct SentenceEngineBridge
        {
            readonly SentenceDebugHud _hud;

            public SentenceEngineBridge(SentenceDebugHud hud) => _hud = hud;

            public void NoteExchange(ExchangeResult result) => _hud?.NoteExchange(result);
        }
    }
}
