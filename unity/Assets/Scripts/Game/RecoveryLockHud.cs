using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// §5 toparlanma kilidi — kalan süre eriyen gösterge. Kesme becerisinin (düz vuruş /
    /// yeni fiil / dodge) ödülü buradan okunur; debug metnindeki "kilit: X ms" kalıcı HUD'a
    /// taşındı (T11.1). Renk §10 oyuncu paleti (camgöbeği/mor); kırmızı-turuncu yok.
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
        // Kesme anında bar aynı karede kapanmasın diye kısa tutuş (ölçeklenmemiş).
        // Spec'te yok — uydurma; docs/durum.md T11.1 sapması.
        float _cutHoldUntilUnscaled = -1f;
        const float CutHoldSec = 0.14f;

        // T10 canlı paneli vitals ölçülerini oynatabilir; aynı dp alanlarını paylaşıyoruz.
        float _appliedWidthDp = -1f;
        float _appliedHeightDp = -1f;
        float _appliedMarginDp = -1f;
        float _appliedVitalsStackDp = -1f;

        public void Configure(
            SentenceEngine engine,
            CombatTuning combat,
            PrototypeTuning tuning,
            Transform canvasRoot)
        {
            _engine = engine;
            _sentence = combat != null ? combat.Sentence : new SentenceTuning();
            _tuning = tuning;

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

        void ApplyTuningLayout()
        {
            // Can barlarının hemen altında: 2× yükseklik + aralık + kendi boşluğu.
            // Ölçüler VitalsHud ile aynı veri — yeni his sayısı uydurulmadı (yalnızca yerleşim).
            float vitalsStackDp =
                _tuning.VitalsBarHeightDp * 2f + _tuning.VitalsBarSpacingDp + _tuning.RecoveryLockGapDp;

            bool changed =
                !Mathf.Approximately(_tuning.VitalsBarWidthDp, _appliedWidthDp) ||
                !Mathf.Approximately(_tuning.RecoveryLockHeightDp, _appliedHeightDp) ||
                !Mathf.Approximately(_tuning.VitalsMarginDp, _appliedMarginDp) ||
                !Mathf.Approximately(vitalsStackDp, _appliedVitalsStackDp);

            if (!changed)
                return;

            _appliedWidthDp = _tuning.VitalsBarWidthDp;
            _appliedHeightDp = _tuning.RecoveryLockHeightDp;
            _appliedMarginDp = _tuning.VitalsMarginDp;
            _appliedVitalsStackDp = vitalsStackDp;

            float w = PentagonLayoutScreen.DpToPixels(_appliedWidthDp);
            float h = PentagonLayoutScreen.DpToPixels(_appliedHeightDp);
            float top = PentagonLayoutScreen.DpToPixels(_appliedMarginDp + vitalsStackDp);

            _root.anchoredPosition = new Vector2(0f, -top);
            _root.sizeDelta = new Vector2(w, h);
            _bg.anchoredPosition = Vector2.zero;
            _bg.sizeDelta = new Vector2(w, h);
        }

        void LateUpdate()
        {
            if (_tuning == null || _engine == null || _fill == null)
                return;

            ApplyTuningLayout();

            // §10: camgöbeği dolgu, mor zemin — boss tehdit paleti yok.
            _fill.color = _tuning.InkCyan;
            Color bg = _tuning.InkPurple;
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
                // Kesildi (§5): kalan anında 0 — bar bir an boş görünür, sonra kapanır.
                // Doğal erime zaten fillAmount≈0 ile geldiyse flaş gerekmez.
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

            // LastClosing yoksa (olmamalı) kalanı tavan kabul et — sıfır bölme yok.
            return Mathf.Max(1f, (float)s.RemainingRecoveryMs);
        }

        void SetVisible(bool on)
        {
            if (on == _visible)
                return;

            _visible = on;
            // Alfası 0 Graphic yine overdraw üretir (T8.1) — kapalıyken Image'lar kapanır.
            _fill.enabled = on;
            _bgImg.enabled = on;
        }
    }
}
