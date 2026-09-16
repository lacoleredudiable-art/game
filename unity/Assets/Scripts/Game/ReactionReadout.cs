using Dovus.Core.Combat;
using Dovus.Core.Tuning;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// §6 gösterim: kenarda büyük, parlak (katmanlı glow) tepki yazısı — "0.45 sn" + derece +
    /// mesaj. Seri sayacı ve en iyi tepki kaydı da burada. Vurulunca sebep yazısı ("erken
    /// bastın"/"geç kaldın") aynı yolla gösterilir ama oyuncu rengiyle DEĞİL — §10 kırmızı-
    /// turuncu yasak olduğu için nötr `PentagonDotColor` kullanılır.
    ///
    /// Animasyon ÖLÇEKLENMEMİŞ saatle (Time.unscaledTime) çalışır: dünya yavaşken bile
    /// keskin görünmeli (§6). Punto/glow/bekleme/sönme `FeelTuning.Readout*`'tan (T1'de spec'e
    /// göre kondu); giriş vuruşunun sönme süresi spec'te yok — `PrototypeTuning.ReadoutPunchInSec`
    /// uydurma alan, gerekçesi durum.md T9 sapmalarında.
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

        // Punto/glow/kenar kurulumda bir kez okunursa T10'un canlı slider'ı ekranda hiçbir şeyi
        // değiştirmez (kabul kriteri 6 "ayarlanabilir" + T10 "yeniden başlatma gerektirmez").
        // Uygulanan değer saklanıp her karede karşılaştırılıyor: değişmediyse tek bir float
        // karşılaştırması, değiştiyse layout yeniden yazılıyor.
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
            bool right = tuning.ReadoutAnchorRight;

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
        /// Kenar/punto/glow ayarlarını uygular. Değişmeyen kare için maliyeti üç karşılaştırma;
        /// T10 paneli değeri oynattığında yerleşim aynı karede yeniden yazılır.
        /// </summary>
        void ApplyTuningLayout()
        {
            bool right = _tuning.ReadoutAnchorRight;
            if (right != _appliedAnchorRight)
            {
                _appliedAnchorRight = right;
                // Sağ (ya da sol) kenarda, üst debug HUD'ın (0.82-0.98) ve beşgenin (merkez
                // y≈0.40) arasında dikey bant — telegrafı kapatmayacak kadar dar (§10).
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
        /// Punto bir TAVAN: yazı bandına sığıyorsa tam bu boyda çizilir, sığmıyorsa oranla
        /// küçülür. Sabit puntoyla "0,45 sn MÜKEMMEL" dar bir ekranda bandını taşıp dünyayı
        /// (ve bossu) örtüyordu — kabul kriteri "yazı bossun telegrafını kapatmıyor" (§10).
        /// Spec'in `FeelTuning.ReadoutSizePx` sayısı değişmedi, yalnızca üst sınır olarak
        /// okunuyor. Unity'nin kendi `resizeTextForBestFit`'i kullanılmadı: kurulum karesinde
        /// bandın genişliği daha 0 olduğu için puntoyu 96'dan 14'e düşürüp orada bırakıyordu.
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

        /// <summary>CombatFeel.OnExchange her sonucu buraya iletir.</summary>
        public void NoteExchange(ExchangeResult result)
        {
            if (_feel == null)
                return;

            if (result.Outcome == ExchangeOutcome.Dodged)
            {
                _streak++;
                float reactionSec = Mathf.Max(0f, result.ReactionMs) / 1000f;
                if (_bestReactionSec < 0f || reactionSec < _bestReactionSec)
                    _bestReactionSec = reactionSec;

                _accent = GradeColor(result.Grade);
                _main.text = $"{reactionSec:0.00} sn  {GradeLabel(result.Grade)}";
                _sub.text = GradeMessage(result.Grade);
                _needsFit = true;
                Show();
            }
            else if (result.Outcome == ExchangeOutcome.Hit)
            {
                _streak = 0;
                // §10: kırmızı-turuncu yalnızca boss tehdidi. Vurulma sebebi nötr renkte.
                _accent = _tuning.PentagonDotColor;
                _main.text = result.HitReasonText ?? "vuruldun";
                _sub.text = string.Empty;
                _needsFit = true;
                Show();
            }

            // Safe (menzil dışı) bu büyük yazıyı tetiklemez; SentenceDebugHud zaten not ediyor.
        }

        /// <summary>Kapanış bang'inde skill adı — "farklı iş" havasının yazı katmanı.</summary>
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
        /// Bağlama 3: yetersiz mana — §10 kırmızı-turuncu yasak; nötr PentagonDotColor.
        /// </summary>
        public void NoteDenied(string title, string detail = null)
        {
            if (_feel == null || string.IsNullOrEmpty(title))
                return;

            _accent = _tuning != null
                ? _tuning.PentagonDotColor
                : new Color(0.55f, 0.62f, 0.72f, 0.85f);
            _main.text = title;
            _sub.text = detail ?? string.Empty;
            _needsFit = true;
            Show();
        }

        void Show() => _shownAtUnscaled = Time.unscaledTime;

        /// <summary>
        /// Alfası 0 olan bir Graphic yine de geometri üretip harmanlanır (T8.1 denetimi 12:
        /// "Color.clear ile kapatmak overdraw'ı kapatmaz"). Yazı ekranda yokken bant, glow ve
        /// kontur tamamen kapanır — mobilde boşta duran tam ekran harman yok.
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
                float punchT = Mathf.Clamp01(t / Mathf.Max(0.001f, _tuning.ReadoutPunchInSec));
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
                _tally.text = $"seri ×{_streak} · en iyi {_bestReactionSec:0.00} sn";
            else if (_streak > 1)
                _tally.text = $"seri ×{_streak}";
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

        static string GradeLabel(DodgeGrade? g) => g switch
        {
            DodgeGrade.Mukemmel => "MÜKEMMEL",
            DodgeGrade.Harika => "HARİKA",
            DodgeGrade.Temiz => "TEMİZ",
            DodgeGrade.Siyirdi => "SIYIRDI",
            _ => "SIYIRMA"
        };

        static string GradeMessage(DodgeGrade? g) => g switch
        {
            DodgeGrade.Mukemmel => "tepki süren mükemmel",
            DodgeGrade.Harika => "neredeyse kusursuz",
            DodgeGrade.Temiz => "iyi okudun",
            DodgeGrade.Siyirdi => "biraz erken bastın",
            _ => string.Empty
        };

        /// <summary>§10: oyuncu efekti camgöbeği/mor. Derece iyileştikçe camgöbeğine yaklaşır.</summary>
        Color GradeColor(DodgeGrade? g)
        {
            float t = g switch
            {
                DodgeGrade.Mukemmel => 1f,
                DodgeGrade.Harika => 0.66f,
                DodgeGrade.Temiz => 0.33f,
                _ => 0f
            };
            return Color.Lerp(_tuning.InkPurple, _tuning.InkCyan, t);
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontStyle = FontStyle.Bold;
            // Tek satır: sarma yerine punto küçülür (FitTexts).
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.color = Color.clear;
            return text;
        }

        /// <summary>Merkezden kenara sönen yumuşak ışık — "katmanlı glow"un arka planı.</summary>
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
