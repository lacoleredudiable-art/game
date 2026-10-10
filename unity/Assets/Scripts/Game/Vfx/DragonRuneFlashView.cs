using Dovus.Core.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §1.1: çizilen rün 0,2 s'de Ejderha Dili harfine oturur (fiil→sıfat),
    /// 0,3 s parlar. Yer tutucu glif atlası.
    /// </summary>
    public sealed class DragonRuneFlashView : MonoBehaviour
    {
        Canvas _canvas;
        Image _verb;
        Image _adj;
        Texture2D _atlas;
        Sprite _atlasSprite;
        float _age;
        float _snap;
        float _glow;
        bool _playing;
        Vector2 _verbFrom;
        Vector2 _adjFrom;
        Vector2 _verbTo;
        Vector2 _adjTo;

        public void Play(int verbRuneId, int adjectiveRuneId, Color coreColor)
        {
            EnsureUi();
            _snap = VfxPlanDefaults.RuneSnapSec;
            _glow = VfxPlanDefaults.RuneGlowSec;
            _age = 0f;
            _playing = true;

            float size = RuleVfxDefaults.RuneLetterSizePx;
            float gap = RuleVfxDefaults.RuneLetterGapPx;
            _verbTo = new Vector2(-size * 0.5f - gap * 0.5f, RuleVfxArtDefaults.RuneAnchorY);
            _adjTo = new Vector2(size * 0.5f + gap * 0.5f, RuleVfxArtDefaults.RuneAnchorY);
            _verbFrom = _verbTo + new Vector2(-RuleVfxArtDefaults.RuneFromOffsetX, RuleVfxArtDefaults.RuneFromOffsetY);
            _adjFrom = _adjTo + new Vector2(RuleVfxArtDefaults.RuneFromOffsetX, -RuleVfxArtDefaults.RuneAdjFromY);

            ApplyCell(_verb, GlyphIndex(verbRuneId), coreColor);
            ApplyCell(_adj, GlyphIndex(adjectiveRuneId) + RuleVfxArtDefaults.GlyphHueMod, coreColor);
            _verb.enabled = true;
            _adj.enabled = true;
            Layout(0f);
        }

        void Update()
        {
            if (!_playing)
                return;
            _age += Time.unscaledDeltaTime;
            float snapU = Mathf.Clamp01(_age / _snap);
            Layout(snapU);

            float glowAge = _age - _snap;
            float glowU = glowAge <= 0f ? 1f : 1f - Mathf.Clamp01(glowAge / _glow);
            SetAlpha(_verb, glowU);
            SetAlpha(_adj, glowU);

            if (_age >= _snap + _glow)
            {
                _playing = false;
                _verb.enabled = false;
                _adj.enabled = false;
            }
        }

        void Layout(float snapU)
        {
            float u = snapU * snapU * (RuleVfxArtDefaults.SmoothstepThree - RuleVfxArtDefaults.SmoothstepTwo * snapU);
            _verb.rectTransform.anchoredPosition = Vector2.Lerp(_verbFrom, _verbTo, u);
            _adj.rectTransform.anchoredPosition = Vector2.Lerp(
                _adjFrom,
                _adjTo,
                Mathf.Clamp01((snapU - RuleVfxArtDefaults.RuneAdjDelay) / RuleVfxArtDefaults.RuneAdjDelayInv));
        }

        void EnsureUi()
        {
            if (_canvas != null)
                return;
            var canvasGo = new GameObject("DragonRuneFlashCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 80;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            _atlas = RuleVfxArtDefaults.BuildGlyphAtlas();
            _atlasSprite = Sprite.Create(
                _atlas,
                new Rect(0f, 0f, _atlas.width, _atlas.height),
                new Vector2(0.5f, 0.5f),
                RuleVfxArtDefaults.GlyphPpu);

            _verb = MakeLetter("VerbLetter");
            _adj = MakeLetter("AdjLetter");
            _verb.enabled = false;
            _adj.enabled = false;
        }

        Image MakeLetter(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(RuleVfxDefaults.RuneLetterSizePx, RuleVfxDefaults.RuneLetterSizePx);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = _atlasSprite;
            img.raycastTarget = false;
            return img;
        }

        static void ApplyCell(Image img, int cell, Color core)
        {
            int idx = Mathf.Clamp(cell, 0, RuleVfxArtDefaults.GlyphCellMaxIndex);
            float hue = (idx % RuleVfxArtDefaults.GlyphHueMod) / (float)RuleVfxArtDefaults.GlyphHueMod;
            Color tint = Color.HSVToRGB(hue, RuleVfxArtDefaults.RuneHueSat, 1f) * core;
            tint.a = 1f;
            img.color = tint;
        }

        static void SetAlpha(Image img, float a)
        {
            if (img == null)
                return;
            Color c = img.color;
            c.a = a;
            img.color = c;
        }

        static int GlyphIndex(int runeId) =>
            Mathf.Clamp(runeId <= 0 ? 0 : runeId - 1, 0, RuleVfxArtDefaults.GlyphHueMod - 1);

        void OnDestroy()
        {
            if (_atlasSprite != null)
                Destroy(_atlasSprite);
            if (_atlas != null)
                Destroy(_atlas);
        }
    }
}
