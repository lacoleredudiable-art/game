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
            // Sağ (ya da sol) kenarda, üst debug HUD'ın (0.82-0.98) ve beşgenin (merkez
            // y≈0.40) arasında dikey bant — telegrafı kapatmayacak kadar dar (§10).
            _root.anchorMin = right ? new Vector2(0.58f, 0.56f) : new Vector2(0.02f, 0.56f);
            _root.anchorMax = right ? new Vector2(0.98f, 0.80f) : new Vector2(0.42f, 0.80f);
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
            _root.pivot = new Vector2(right ? 1f : 0f, 0.5f);

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

            TextAnchor anchor = right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            _main = CreateText(go.transform, "Main", feel.ReadoutSizePx, anchor, new Vector2(0f, 0.42f), new Vector2(1f, 1f));
            _outline = _main.gameObject.AddComponent<Outline>();
            _outline.effectDistance = new Vector2(feel.ReadoutGlow * 0.05f, -feel.ReadoutGlow * 0.05f);

            _sub = CreateText(go.transform, "Sub", feel.ReadoutSizePx * 0.32f, anchor, new Vector2(0f, 0.20f), new Vector2(1f, 0.42f));
            _tally = CreateText(go.transform, "Tally", feel.ReadoutSizePx * 0.24f, anchor, new Vector2(0f, 0f), new Vector2(1f, 0.20f));

            HideAll();
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
                Show();
            }
            else if (result.Outcome == ExchangeOutcome.Hit)
            {
                _streak = 0;
                // §10: kırmızı-turuncu yalnızca boss tehdidi. Vurulma sebebi nötr renkte.
                _accent = _tuning.PentagonDotColor;
                _main.text = result.HitReasonText ?? "vuruldun";
                _sub.text = string.Empty;
                Show();
            }

            // Safe (menzil dışı) bu büyük yazıyı tetiklemez; SentenceDebugHud zaten not ediyor.
        }

        void Show() => _shownAtUnscaled = Time.unscaledTime;

        void HideAll()
        {
            if (_main != null) _main.color = Color.clear;
            if (_sub != null) _sub.color = Color.clear;
            if (_tally != null) _tally.color = Color.clear;
            if (_glow != null) _glow.color = Color.clear;
            if (_outline != null) _outline.effectColor = Color.clear;
        }

        void LateUpdate()
        {
            if (_feel == null)
                return;

            float t = Time.unscaledTime - _shownAtUnscaled;
            float holdSec = _feel.ReadoutHoldMs / 1000f;
            float fadeSec = Mathf.Max(0.001f, _feel.ReadoutFadeMs / 1000f);

            if (_shownAtUnscaled < 0f || t > holdSec + fadeSec)
            {
                HideAll();
            }
            else
            {
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
                _tally.color = Color.clear;
                return;
            }

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

        static Text CreateText(Transform parent, string name, float fontSize, TextAnchor anchor, Vector2 anchorMin, Vector2 anchorMax)
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
            text.fontSize = Mathf.Max(1, Mathf.RoundToInt(fontSize));
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
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
