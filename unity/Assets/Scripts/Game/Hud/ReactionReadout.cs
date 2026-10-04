using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Tuning;
using Dovus.Game.Config;
using Dovus.Game.Feel;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Hud
{
    /// <summary>
    /// Â§6 gÃ¶sterim: kenarda bÃ¼yÃ¼k, parlak (katmanlÄ± glow) tepki yazÄ±sÄ± â€” "0.45 sn" + derece +
    /// mesaj. Seri sayacÄ± ve en iyi tepki kaydÄ± da burada. Vurulunca sebep yazÄ±sÄ± ("erken
    /// bastÄ±n"/"geÃ§ kaldÄ±n") aynÄ± yolla gÃ¶sterilir ama oyuncu rengiyle DEÄÄ°L â€” Â§10 kÄ±rmÄ±zÄ±-
    /// vurulma nedeni iÃ§in nÃ¶tr `HexagonDotColor` kullanÄ±lÄ±r.
    ///
    /// Animasyon Ã–LÃ‡EKLENMEMÄ°Å saatle (Time.unscaledTime) Ã§alÄ±ÅŸÄ±r: dÃ¼nya yavaÅŸken bile
    /// keskin gÃ¶rÃ¼nmeli (Â§6). Punto/glow/bekleme/sÃ¶nme `FeelTuning.Readout*`'tan (T1'de spec'e
    /// gÃ¶re kondu); giriÅŸ vuruÅŸunun sÃ¶nme sÃ¼resi spec'te yok â€” `PrototypeTuning.ReadoutPunchInSec`
    /// uydurma alan, gerekÃ§esi durum.md T9 sapmalarÄ±nda.
    /// </summary>
    public sealed class ReactionReadout : MonoBehaviour
    {
        RectTransform _root;
        Text _main;
        Text _sub;
        Text _tally;
        Image _glow;
        Outline _outline;

        FeelTuning _feel;
        PrototypeTuning _tuning;

        float _shownAtUnscaled = -999f;
        Color _accent = Color.white;
        int _streak;
        float _bestReactionSec = -1f;

        // Punto/glow/kenar kurulumda bir kez okunursa T10'un canlÄ± slider'Ä± ekranda hiÃ§bir ÅŸeyi
        // deÄŸiÅŸtirmez (kabul kriteri 6 "ayarlanabilir" + T10 "yeniden baÅŸlatma gerektirmez").
        // Uygulanan deÄŸer saklanÄ±p her karede karÅŸÄ±laÅŸtÄ±rÄ±lÄ±yor: deÄŸiÅŸmediyse tek bir float
        // karÅŸÄ±laÅŸtÄ±rmasÄ±, deÄŸiÅŸtiyse layout yeniden yazÄ±lÄ±yor.
        bool _appliedAnchorRight;
        float _appliedSizePx = -1f;
        float _appliedGlow = -1f;
        float _fittedBandWidth = -1f;
        bool _needsFit = true;
        bool _graphicsVisible = true;

        public void Configure(FeelTuning feel, PrototypeTuning tuning, Transform canvasRoot)
        {
            _feel = feel;
            _tuning = tuning;
            bool right = tuning.Hud.ReadoutAnchorRight;

            var go = new GameObject("ReactionReadout");
            go.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                go.layer = canvasRoot.gameObject.layer;

            _root = go.AddComponent<RectTransform>();
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(go.transform, false);
            var glowRect = glowGo.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;
            _glow = glowGo.AddComponent<Image>();
            _glow.sprite = CreateGlowSprite();
            _glow.color = Color.clear;
            _glow.raycastTarget = false;

            _main = CreateText(go.transform, "Main", new Vector2(0f, 0.42f), new Vector2(1f, 1f));
            _outline = _main.gameObject.AddComponent<Outline>();

            _sub = CreateText(go.transform, "Sub", new Vector2(0f, 0.20f), new Vector2(1f, 0.42f));
            _tally = CreateText(go.transform, "Tally", new Vector2(0f, 0f), new Vector2(1f, 0.20f));

            _appliedAnchorRight = !right;
            ApplyTuningLayout();
            HideAll();
        }

        /// <summary>
        /// Kenar/punto/glow ayarlarÄ±nÄ± uygular. DeÄŸiÅŸmeyen kare iÃ§in maliyeti Ã¼Ã§ karÅŸÄ±laÅŸtÄ±rma;
        /// T10 paneli deÄŸeri oynattÄ±ÄŸÄ±nda yerleÅŸim aynÄ± karede yeniden yazÄ±lÄ±r.
        /// </summary>
        void ApplyTuningLayout()
        {
            bool right = _tuning.Hud.ReadoutAnchorRight;
            if (right != _appliedAnchorRight)
            {
                _appliedAnchorRight = right;
                // SaÄŸ (ya da sol) kenarda, Ã¼st debug HUD'Ä±n (0.82-0.98) ve altÄ±genin (merkez
                // yâ‰ˆ0.40) arasÄ±nda dikey bant â€” telegrafÄ± kapatmayacak kadar dar (Â§10).
                _root.anchorMin = right ? new Vector2(0.58f, 0.56f) : new Vector2(0.02f, 0.56f);
                _root.anchorMax = right ? new Vector2(0.98f, 0.80f) : new Vector2(0.42f, 0.80f);
                _root.offsetMin = Vector2.zero;
                _root.offsetMax = Vector2.zero;
                _root.pivot = new Vector2(right ? 1f : 0f, 0.5f);

                TextAnchor anchor = right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
                _main.alignment = anchor;
                _sub.alignment = anchor;
                _tally.alignment = anchor;
                _needsFit = true;
            }

            if (!Mathf.Approximately(_feel.ReadoutSizePx, _appliedSizePx))
            {
                _appliedSizePx = _feel.ReadoutSizePx;
                _needsFit = true;
            }

            if (!Mathf.Approximately(_feel.ReadoutGlow, _appliedGlow))
            {
                _appliedGlow = _feel.ReadoutGlow;
                _outline.effectDistance = new Vector2(_appliedGlow * 0.05f, -_appliedGlow * 0.05f);
            }

            if (_needsFit || !Mathf.Approximately(_root.rect.width, _fittedBandWidth))
                FitTexts();
        }

        /// <summary>
        /// Punto bir TAVAN: yazÄ± bandÄ±na sÄ±ÄŸÄ±yorsa tam bu boyda Ã§izilir, sÄ±ÄŸmÄ±yorsa oranla
        /// kÃ¼Ã§Ã¼lÃ¼r. Sabit puntoyla "0,45 sn MÃœKEMMEL" dar bir ekranda bandÄ±nÄ± taÅŸÄ±p dÃ¼nyayÄ±
        /// (ve bossu) Ã¶rtÃ¼yordu â€” kabul kriteri "yazÄ± bossun telegrafÄ±nÄ± kapatmÄ±yor" (Â§10).
        /// Spec'in `FeelTuning.ReadoutSizePx` sayÄ±sÄ± deÄŸiÅŸmedi, yalnÄ±zca Ã¼st sÄ±nÄ±r olarak
        /// okunuyor. Unity'nin kendi `resizeTextForBestFit`'i kullanÄ±lmadÄ±: kurulum karesinde
        /// bandÄ±n geniÅŸliÄŸi daha 0 olduÄŸu iÃ§in puntoyu 96'dan 14'e dÃ¼ÅŸÃ¼rÃ¼p orada bÄ±rakÄ±yordu.
        /// </summary>
        void FitTexts()
        {
            float band = _root.rect.width;
            _fittedBandWidth = band;
            _needsFit = false;

            FitOne(_main, _appliedSizePx, band);
            FitOne(_sub, _appliedSizePx * 0.32f, band);
            FitOne(_tally, _appliedSizePx * 0.24f, band);
        }

        static void FitOne(Text text, float basePx, float bandWidth)
        {
            int max = Mathf.Max(1, Mathf.RoundToInt(basePx));
            text.fontSize = max;
            if (bandWidth <= 1f || string.IsNullOrEmpty(text.text))
                return;

            float preferred = text.preferredWidth;
            if (preferred > bandWidth)
                text.fontSize = Mathf.Max(1, Mathf.FloorToInt(max * bandWidth / preferred));
        }

        /// <summary>CombatFeel artÄ±k dodge/vurulma bÃ¼yÃ¼k yazÄ±sÄ±nÄ± tetiklemez (feel-2).</summary>
        public void NoteExchange(ExchangeResult result)
        {
            if (_feel == null)
                return;

            // YalnÄ±z skill/kapanÄ±ÅŸ/deny yazÄ±larÄ± kalÄ±r â€” dodge derecesi ve vurulma sebebi gÃ¶sterilmez.
            if (result.Outcome == ExchangeOutcome.Dodged || result.Outcome == ExchangeOutcome.Hit)
                return;
        }

        /// <summary>KapanÄ±ÅŸ bang'inde skill adÄ± â€” "farklÄ± iÅŸ" havasÄ±nÄ±n yazÄ± katmanÄ±.</summary>
        public void NoteSkill(string title, string detail, Color accent)
        {
            if (_feel == null || string.IsNullOrEmpty(title))
                return;

            _accent = accent;
            _main.text = title;
            _sub.text = detail ?? string.Empty;
            _needsFit = true;
            Show();
        }

        /// <summary>
        /// BaÄŸlama 3: yetersiz mana iÃ§in nÃ¶tr HexagonDotColor.
        /// </summary>
        public void NoteDenied(string title, string detail = null)
        {
            if (_feel == null || string.IsNullOrEmpty(title))
                return;

            _accent = _tuning != null
                ? _tuning.Visuals.HexagonDotColor
                : new Color(0.55f, 0.62f, 0.72f, 0.85f);
            _main.text = title;
            _sub.text = detail ?? string.Empty;
            _needsFit = true;
            Show();
        }

        void Show() => _shownAtUnscaled = Time.unscaledTime;

        /// <summary>
        /// AlfasÄ± 0 olan bir Graphic yine de geometri Ã¼retip harmanlanÄ±r (T8.1 denetimi 12:
        /// "Color.clear ile kapatmak overdraw'Ä± kapatmaz"). YazÄ± ekranda yokken bant, glow ve
        /// kontur tamamen kapanÄ±r â€” mobilde boÅŸta duran tam ekran harman yok.
        /// </summary>
        void HideAll()
        {
            if (!_graphicsVisible)
                return;

            _graphicsVisible = false;
            _main.enabled = false;
            _sub.enabled = false;
            _glow.enabled = false;
            _outline.enabled = false;
            _root.localScale = Vector3.one;
        }

        void ShowGraphics()
        {
            if (_graphicsVisible)
                return;

            _graphicsVisible = true;
            _main.enabled = true;
            _sub.enabled = true;
            _glow.enabled = true;
            _outline.enabled = true;
        }

        void LateUpdate()
        {
            if (_feel == null)
                return;

            ApplyTuningLayout();

            float t = Time.unscaledTime - _shownAtUnscaled;
            float holdSec = _feel.ReadoutHoldMs / 1000f;
            float fadeSec = Mathf.Max(0.001f, _feel.ReadoutFadeMs / 1000f);

            if (_shownAtUnscaled < 0f || t > holdSec + fadeSec)
            {
                HideAll();
            }
            else
            {
                ShowGraphics();
                float alpha = t <= holdSec ? 1f : Mathf.Clamp01(1f - (t - holdSec) / fadeSec);
                float punchT = Mathf.Clamp01(t / Mathf.Max(0.001f, _tuning.Hud.ReadoutPunchInSec));
                float scale = Mathf.Lerp(_feel.ReadoutPunchScale, 1f, punchT);
                _root.localScale = Vector3.one * scale;

                Color main = _accent;
                main.a = alpha;
                _main.color = main;
                _sub.color = new Color(1f, 1f, 1f, 0.85f * alpha);

                Color oc = _accent;
                oc.a = alpha * 0.6f;
                _outline.effectColor = oc;

                Color glowColor = _accent;
                glowColor.a = alpha * 0.5f;
                _glow.color = glowColor;
            }

            UpdateTally();
        }

        void UpdateTally()
        {
            if (_streak > 1 && _bestReactionSec >= 0f)
                _tally.text = $"seri Ã—{_streak} Â· en iyi {_bestReactionSec:0.00} sn";
            else if (_streak > 1)
                _tally.text = $"seri Ã—{_streak}";
            else if (_bestReactionSec >= 0f)
                _tally.text = $"en iyi tepki: {_bestReactionSec:0.00} sn";
            else
            {
                _tally.enabled = false;
                return;
            }

            _tally.enabled = true;
            _tally.color = new Color(1f, 1f, 1f, 0.55f);
        }

        static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = go.AddComponent<Text>();
            text.font = HudTheme.LegacyFont;
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontStyle = FontStyle.Bold;
            // Tek satÄ±r: sarma yerine punto kÃ¼Ã§Ã¼lÃ¼r (FitTexts).
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.color = Color.clear;
            return text;
        }

        /// <summary>Merkezden kenara sÃ¶nen yumuÅŸak Ä±ÅŸÄ±k â€” "katmanlÄ± glow"un arka planÄ±.</summary>
        static Sprite CreateGlowSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float a = 1f - Mathf.SmoothStep(0f, 1f, r);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }
    }
}
