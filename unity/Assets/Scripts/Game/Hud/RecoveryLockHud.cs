using Dovus.Core.Grammar;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Hud
{
    /// <summary>
    /// Â§5 toparlanma kilidi â€” kalan sÃ¼re eriyen gÃ¶sterge. Kesme becerisinin (dÃ¼z vuruÅŸ /
    /// yeni fiil / dodge) Ã¶dÃ¼lÃ¼ buradan okunur; debug metnindeki "kilit: X ms" kalÄ±cÄ± HUD'a
    /// taÅŸÄ±ndÄ± (T11.1). Renk mevcut oyuncu paletinden (camgÃ¶beÄŸi/mor).
    /// </summary>
    public sealed class RecoveryLockHud : MonoBehaviour
    {
        SentenceEngine _engine;
        SentenceTuning _sentence;
        PrototypeTuning _tuning;
        RectTransform _root;
        RectTransform _bg;
        Image _fill;
        Image _bgImg;

        float _armedRecoveryMs;
        bool _visible;
        // Kesme anÄ±nda bar aynÄ± karede kapanmasÄ±n diye kÄ±sa tutuÅŸ (Ã¶lÃ§eklenmemiÅŸ).
        // Spec'te yok â€” uydurma; docs/durum.md T11.1 sapmasÄ±.
        float _cutHoldUntilUnscaled = -1f;
        const float CutHoldSec = 0.14f;

        // T10 canlÄ± paneli vitals Ã¶lÃ§Ã¼lerini oynatabilir; aynÄ± dp alanlarÄ±nÄ± paylaÅŸÄ±yoruz.
        float _appliedWidthDp = -1f;
        float _appliedHeightDp = -1f;
        float _appliedMarginDp = -1f;
        float _appliedVitalsStackDp = -1f;
        int _vitalsBarCount = 3;

        public void Configure(
            SentenceEngine engine,
            CombatTuning combat,
            PrototypeTuning tuning,
            Transform canvasRoot,
            int vitalsBarCount = 3)
        {
            _engine = engine;
            _sentence = combat != null ? combat.Sentence : new SentenceTuning();
            _tuning = tuning;
            _vitalsBarCount = Mathf.Max(1, vitalsBarCount);

            var go = new GameObject("RecoveryLockHud");
            go.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                go.layer = canvasRoot.gameObject.layer;

            _root = go.AddComponent<RectTransform>();
            _root.anchorMin = new Vector2(0.02f, 1f);
            _root.anchorMax = new Vector2(0.02f, 1f);
            _root.pivot = new Vector2(0f, 1f);

            _fill = CreateBar(go.transform, out _bg, out _bgImg);
            ApplyTuningLayout();
            SetVisible(false);
        }

        static Image CreateBar(Transform parent, out RectTransform bgRect, out Image bgImg)
        {
            var bg = new GameObject("LockBg");
            bg.transform.SetParent(parent, false);
            bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.pivot = new Vector2(0f, 1f);
            bgImg = bg.AddComponent<Image>();
            bgImg.raycastTarget = false;

            var fillGo = new GameObject("LockFill");
            fillGo.transform.SetParent(bg.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.raycastTarget = false;
            return fillImg;
        }

        VitalsHud _vitalsHud;

        public void BindVitalsHud(VitalsHud vitalsHud) => _vitalsHud = vitalsHud;

        void ApplyTuningLayout()
        {
            float w = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsBarWidthDp);
            float h = HexagonLayoutScreen.DpToPixels(_tuning.Hud.RecoveryLockHeightDp);
            float gap = HexagonLayoutScreen.DpToPixels(_tuning.Hud.RecoveryLockGapDp);
            float left = HexagonLayoutScreen.SafeLeftInsetPx()
                + HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsMarginDp);

            float topY;
            if (_vitalsHud != null)
                topY = _vitalsHud.PlayerStackBottomCanvasY - gap
                    - HexagonLayoutScreen.DpToPixels(_tuning.Hud.StatusIconSizeDp + _tuning.Hud.StatusIconGapDp);
            else
            {
                int rows = _vitalsBarCount;
                float vitalsStackDp =
                    _tuning.Hud.VitalsBarHeightDp * rows
                    + _tuning.Hud.VitalsBarSpacingDp * Mathf.Max(0, rows - 1)
                    + _tuning.Hud.RecoveryLockGapDp;
                topY = -(HexagonLayoutScreen.SafeTopInsetPx()
                    + HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsMarginDp + vitalsStackDp));
            }

            _root.anchoredPosition = new Vector2(left, topY);
            _root.sizeDelta = new Vector2(w, h);
            _bg.anchoredPosition = Vector2.zero;
            _bg.sizeDelta = new Vector2(w, h);
            _appliedWidthDp = _tuning.Hud.VitalsBarWidthDp;
            _appliedHeightDp = _tuning.Hud.RecoveryLockHeightDp;
            _appliedMarginDp = _tuning.Hud.VitalsMarginDp;
        }

        void LateUpdate()
        {
            if (_tuning == null || _engine == null || _fill == null)
                return;

            ApplyTuningLayout();

            // Â§10: camgÃ¶beÄŸi dolgu, mor zemin â€” boss tehdit paleti yok.
            _fill.color = _tuning.Visuals.InkCyan;
            Color bg = _tuning.Visuals.InkPurple;
            bg.a = 0.35f;
            _bgImg.color = bg;

            SentenceState s = _engine.State;
            if (s.Phase == SentencePhase.Recovering && s.RemainingRecoveryMs > 0)
            {
                _cutHoldUntilUnscaled = -1f;
                if (_armedRecoveryMs <= 0)
                    _armedRecoveryMs = ArmMs(s);

                float denom = Mathf.Max(1f, (float)_armedRecoveryMs);
                _fill.fillAmount = Mathf.Clamp01((float)s.RemainingRecoveryMs / denom);
                SetVisible(true);
            }
            else
            {
                // Kesildi (Â§5): kalan anÄ±nda 0 â€” bar bir an boÅŸ gÃ¶rÃ¼nÃ¼r, sonra kapanÄ±r.
                // DoÄŸal erime zaten fillAmountâ‰ˆ0 ile geldiyse flaÅŸ gerekmez.
                if (_visible && _fill.fillAmount > 0.05f && _cutHoldUntilUnscaled < 0f)
                {
                    _fill.fillAmount = 0f;
                    _cutHoldUntilUnscaled = Time.unscaledTime + CutHoldSec;
                }

                _armedRecoveryMs = 0;
                if (_cutHoldUntilUnscaled >= 0f && Time.unscaledTime < _cutHoldUntilUnscaled)
                {
                    _fill.fillAmount = 0f;
                    SetVisible(true);
                }
                else
                {
                    _cutHoldUntilUnscaled = -1f;
                    SetVisible(false);
                }
            }
        }

        float ArmMs(SentenceState s)
        {
            if (s.LastClosing.HasValue)
                return (float)(_sentence.StepForDots(s.LastClosing.Value.DotCount).RecoverySec * 1000.0);

            // LastClosing yoksa (olmamalÄ±) kalanÄ± tavan kabul et â€” sÄ±fÄ±r bÃ¶lme yok.
            return Mathf.Max(1f, (float)s.RemainingRecoveryMs);
        }

        void SetVisible(bool on)
        {
            if (on == _visible)
                return;

            _visible = on;
            // AlfasÄ± 0 Graphic yine overdraw Ã¼retir (T8.1) â€” kapalÄ±yken Image'lar kapanÄ±r.
            _fill.enabled = on;
            _bgImg.enabled = on;
        }
    }
}
