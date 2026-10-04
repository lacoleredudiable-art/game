using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Hud
{
    public sealed partial class VitalsHud
    {
        void CreatePhaseNotches(RectTransform bossBg, BossHudData data)
        {
            foreach (var p in data.Phases)
            {
                if (p.UpperFrac <= 0.001f || p.UpperFrac >= VitalsHudDefaults.FillFullThreshold)
                    continue;
                var go = new GameObject("PhaseNotch" + p.Phase);
                go.transform.SetParent(bossBg, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(p.UpperFrac, 0f);
                rt.anchorMax = new Vector2(p.UpperFrac, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(HexagonLayoutScreen.DpToPixels(2f), 0f);
                var img = go.AddComponent<Image>();
                img.color = _theme.PhaseNotchColor;
                img.raycastTarget = false;
            }
        }

        void CreateCastBar(RectTransform bossRoot)
        {
            HudTheme th = _theme;
            var go = new GameObject("BossCastBar");
            go.transform.SetParent(bossRoot, false);
            _castRoot = go.AddComponent<RectTransform>();
            _castRoot.anchorMin = new Vector2(0.5f, 1f);
            _castRoot.anchorMax = new Vector2(0.5f, 1f);
            _castRoot.pivot = new Vector2(0.5f, 1f);
            _castGroup = go.AddComponent<CanvasGroup>();
            _castGroup.alpha = 0f;
            _castGroup.blocksRaycasts = false;
            _castGroup.interactable = false;

            var bg = go.AddComponent<Image>();
            bg.sprite = PillSprite();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.02f, 0.03f, 0.04f, 0.78f);
            bg.raycastTarget = false;

            _castFill = CreateBarLayer(go.transform, "CastFill", PillSprite(), Image.Type.Filled);
            _castFill.color = th.CastBarColor;
            _castLabel = HudTheme.CreateTmp(go.transform, "CastLabel", th.CastLabelDp, Color.white);
            var lrt = _castLabel.rectTransform;
            lrt.anchorMin = new Vector2(0f, 1f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.anchoredPosition = new Vector2(0f, HexagonLayoutScreen.DpToPixels(1f));
            lrt.sizeDelta = new Vector2(0f, HexagonLayoutScreen.DpToPixels(th.CastLabelDp + 4f));
        }

        void CreatePhaseBanner(Transform canvasRoot)
        {
            HudTheme th = _theme;
            var go = new GameObject("PhaseBanner");
            go.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                go.layer = canvasRoot.gameObject.layer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.68f);
            rt.anchorMax = new Vector2(0.5f, 0.68f);
            rt.sizeDelta = new Vector2(HexagonLayoutScreen.DpToPixels(360f), HexagonLayoutScreen.DpToPixels(th.BannerDp * 1.6f));
            _bannerGroup = go.AddComponent<CanvasGroup>();
            _bannerGroup.alpha = 0f;
            _bannerGroup.blocksRaycasts = false;
            _bannerGroup.interactable = false;
            _banner = HudTheme.CreateTmp(go.transform, "BannerText", th.BannerDp, th.BannerColor);
            var brt = _banner.rectTransform;
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
        }

        void TickBossExtras()
        {
            HudTheme th = _theme;
            if (_bannerGroup != null && _bannerGroup.alpha > 0f)
            {
                float age = Time.unscaledTime - _bannerShownAt;
                _bannerGroup.alpha = age <= th.BannerHoldSec
                    ? 1f
                    : 1f - Mathf.Clamp01((age - th.BannerHoldSec) / Mathf.Max(VitalsHudDefaults.BannerFadeMinSec, th.BannerFadeSec));
            }

            if (_castGroup == null)
                return;
            bool casting = _bossDirector != null && _bossDirector.IsWindingUp
                && _bossDirector.CurrentAttackKind.HasValue;
            if (casting)
            {
                var kind = _bossDirector.CurrentAttackKind.Value;
                if (!_castShown || _castKind != kind)
                {
                    _castKind = kind;
                    _castLabel.text = _bossData.AttackName(kind);
                    UiJuice.PunchScale(_castRoot, th.CastPopScale, th.JuiceSec);
                }
                _castShown = true;
                float progress = _bossDirector.WindupProgress01;
                _castFill.fillAmount = progress;
                float urgency = Mathf.InverseLerp(th.CastUrgencyStart01, 1f, progress);
                float pulse = UiJuice.Pulse01(th.CastUrgencyPulseHz) * th.CastUrgencyPulseStrength;
                _castFill.color = Color.Lerp(
                    th.CastBarColor,
                    th.CastUrgentColor,
                    Mathf.Clamp01(urgency + urgency * pulse));
                _castGroup.alpha = 1f;
            }
            else
            {
                _castShown = false;
                _castGroup.alpha = Mathf.MoveTowards(_castGroup.alpha, 0f, Time.unscaledDeltaTime / Mathf.Max(VitalsHudDefaults.CastBannerFadeMinSec, th.BannerFadeSec));
            }
        }

        static Sprite _roundedSprite;
        static Sprite _pillSprite;

        RectTransform CreateGlassPanel(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(go.transform, false);
            var shadowRt = shadowGo.AddComponent<RectTransform>();
            shadowRt.anchorMin = Vector2.zero;
            shadowRt.anchorMax = Vector2.one;
            float shadowPx = HexagonLayoutScreen.DpToPixels(_theme.PanelShadowDp);
            shadowRt.offsetMin = new Vector2(0f, -shadowPx);
            shadowRt.offsetMax = new Vector2(shadowPx, 0f);
            var shadowImg = shadowGo.AddComponent<Image>();
            shadowImg.sprite = RoundedRectSprite();
            shadowImg.type = Image.Type.Sliced;
            shadowImg.color = new Color(0f, 0f, 0f, 0.35f);
            shadowImg.raycastTarget = false;

            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite();
            img.type = Image.Type.Sliced;
            img.color = _theme.PanelSoftColor;
            img.raycastTarget = false;

            var edgeGo = new GameObject("Edge");
            edgeGo.transform.SetParent(go.transform, false);
            var edgeRt = edgeGo.AddComponent<RectTransform>();
            edgeRt.anchorMin = Vector2.zero;
            edgeRt.anchorMax = Vector2.one;
            edgeRt.offsetMin = Vector2.zero;
            edgeRt.offsetMax = Vector2.zero;
            var edgeImg = edgeGo.AddComponent<Image>();
            edgeImg.sprite = RoundedRectSprite();
            edgeImg.type = Image.Type.Sliced;
            edgeImg.color = _theme.PanelEdgeColor;
            edgeImg.raycastTarget = false;
            return rect;
        }

        Image CreateBar(
            Transform parent,
            string name,
            out RectTransform bgRect,
            out Image sheen) => CreateBar(parent, name, out bgRect, out sheen, out _);

        Image CreateBar(
            Transform parent,
            string name,
            out RectTransform bgRect,
            out Image sheen,
            out BarJuice juice)
        {
            Sprite pill = PillSprite();

            var bg = new GameObject(name + "Bg");
            bg.transform.SetParent(parent, false);
            bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.pivot = new Vector2(0f, 1f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = pill;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.02f, 0.03f, 0.04f, 0.78f);
            bgImg.raycastTarget = false;

            Image ghost = CreateBarLayer(bg.transform, name + "Ghost", pill, Image.Type.Filled);

            var fillGo = new GameObject(name + "Fill");
            fillGo.transform.SetParent(bg.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            float barInset = HexagonLayoutScreen.DpToPixels(_theme.BarInsetDp);
            fillRect.offsetMin = new Vector2(barInset, barInset);
            fillRect.offsetMax = new Vector2(-barInset, -barInset);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.sprite = pill;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.raycastTarget = false;

            var sheenGo = new GameObject(name + "Sheen");
            sheenGo.transform.SetParent(fillGo.transform, false);
            var sheenRt = sheenGo.AddComponent<RectTransform>();
            sheenRt.anchorMin = new Vector2(0f, 0.55f);
            sheenRt.anchorMax = new Vector2(1f, 1f);
            sheenRt.offsetMin = Vector2.zero;
            sheenRt.offsetMax = Vector2.zero;
            sheen = sheenGo.AddComponent<Image>();
            sheen.sprite = pill;
            sheen.type = Image.Type.Sliced;
            sheen.color = new Color(1f, 1f, 1f, 0.10f);
            sheen.raycastTarget = false;

            Image flash = CreateBarLayer(bg.transform, name + "Flash", pill, Image.Type.Sliced);
            flash.enabled = false;
            juice = new BarJuice(ghost, flash);
            return fillImg;
        }

        Image CreateBarLayer(Transform bg, string name, Sprite pill, Image.Type type)
        {
            var go = new GameObject(name);
            go.transform.SetParent(bg, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            float barInset = HexagonLayoutScreen.DpToPixels(_theme.BarInsetDp);
            rect.offsetMin = new Vector2(barInset, barInset);
            rect.offsetMax = new Vector2(-barInset, -barInset);
            var img = go.AddComponent<Image>();
            img.sprite = pill;
            img.type = type;
            if (type == Image.Type.Filled)
            {
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            img.raycastTarget = false;
            return img;
        }

        static Sprite RoundedRectSprite()
        {
            if (_roundedSprite != null)
                return _roundedSprite;

            const int size = 64;
            const float radius = 14f;
            _roundedSprite = BuildRounded(size, radius);
            return _roundedSprite;
        }

        static Sprite PillSprite()
        {
            if (_pillSprite != null)
                return _pillSprite;

            // Tam yükseklik yarıçapı → stadium / modern bar.
            const int size = 64;
            _pillSprite = BuildRounded(size, size * 0.5f - 0.5f);
            return _pillSprite;
        }

        static Sprite BuildRounded(int size, float radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x, radius, size - 1 - radius);
                    float cy = Mathf.Clamp(y, radius, size - 1 - radius);
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float a = Mathf.Clamp01(radius - dist + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply(false, true);

            int b = Mathf.Max(1, Mathf.RoundToInt(radius));
            return Sprite.Create(
                tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        Text CreateLabel(RectTransform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            float labelInset = HexagonLayoutScreen.DpToPixels(_theme.BarLabelInsetDp);
            rect.offsetMin = new Vector2(labelInset, 0f);
            rect.offsetMax = new Vector2(-labelInset, 0f);
            var text = go.AddComponent<Text>();
            text.font = HudTheme.LegacyFont;
            text.fontSize = Mathf.Max(11, Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(10.5f)));
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, 1f, 1f, 0.88f);
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            text.text = string.Empty;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        TextMeshProUGUI CreateBossName(Transform parent, BossHudData data)
        {
            HudTheme th = _theme;
            var text = HudTheme.CreateTmp(parent, "BossName", th.BossNameDp, new Color(0.92f, 0.88f, 0.82f, 0.95f));
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(400f, 18f);
            float subPct = th.BossNameDp > 0f ? th.BossSubtitleDp / th.BossNameDp * 100f : VitalsHudDefaults.BossSubtitlePctFallback;
            text.text = string.IsNullOrEmpty(data.Subtitle)
                ? data.Upper(data.Name)
                : data.Upper(data.Name) + "  <size=" + subPct.ToString("0") + "%><alpha=#AA>" + data.Subtitle + "</size>";
            return text;
        }
    }
}
