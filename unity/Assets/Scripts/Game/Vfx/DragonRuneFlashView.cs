using Dovus.Core.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §1.1: çizilen rün 0,2 s'de Ejderha Dili harfine oturur (fiil→sıfat),
    /// 0,3 s parlar. Yer tutucu glif atlası (harf hücreleri).
    /// </summary>
    public sealed class DragonRuneFlashView : MonoBehaviour
    {
        Canvas _canvas;
        RawImage _verb;
        RawImage _adj;
        Texture2D _atlas;
        Material _verbMat;
        Material _adjMat;
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
            _verbTo = new Vector2(-size * 0.5f - gap * 0.5f, 120f);
            _adjTo = new Vector2(size * 0.5f + gap * 0.5f, 120f);
            _verbFrom = _verbTo + new Vector2(-80f, 40f);
            _adjFrom = _adjTo + new Vector2(80f, -30f);

            ApplyCell(_verb, _verbMat, GlyphIndex(verbRuneId), coreColor);
            ApplyCell(_adj, _adjMat, GlyphIndex(adjectiveRuneId) + 12, coreColor);
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
            SetAlpha(_verbMat, glowU);
            SetAlpha(_adjMat, glowU);

            if (_age >= _snap + _glow)
            {
                _playing = false;
                _verb.enabled = false;
                _adj.enabled = false;
            }
        }

        void Layout(float snapU)
        {
            float u = snapU * snapU * (3f - 2f * snapU);
            _verb.rectTransform.anchoredPosition = Vector2.Lerp(_verbFrom, _verbTo, u);
            _adj.rectTransform.anchoredPosition = Vector2.Lerp(
                _adjFrom,
                _adjTo,
                Mathf.Clamp01((snapU - 0.15f) / 0.85f));
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

            _atlas = BuildGlyphAtlas();
            Shader sh = Shader.Find(RuleVfxDefaults.KorShaderName)
                ?? Shader.Find("UI/Default");
            _verbMat = new Material(sh);
            _adjMat = new Material(sh);
            if (_verbMat.HasProperty("_MainTex"))
            {
                _verbMat.SetTexture("_MainTex", _atlas);
                _adjMat.SetTexture("_MainTex", _atlas);
            }

            _verb = MakeLetter("VerbLetter", _verbMat);
            _adj = MakeLetter("AdjLetter", _adjMat);
            _verb.enabled = false;
            _adj.enabled = false;
        }

        RawImage MakeLetter(string name, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(RuleVfxDefaults.RuneLetterSizePx, RuleVfxDefaults.RuneLetterSizePx);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<RawImage>();
            img.texture = _atlas;
            img.material = mat;
            img.raycastTarget = false;
            return img;
        }

        static void ApplyCell(RawImage img, Material mat, int cell, Color core)
        {
            int idx = Mathf.Clamp(cell, 0, 23);
            int col = idx % 8;
            int row = idx / 8;
            float u0 = col / 8f;
            float v0 = 1f - (row + 1) / 3f;
            float u1 = (col + 1) / 8f;
            float v1 = 1f - row / 3f;
            img.uvRect = new Rect(u0, v0, u1 - u0, v1 - v0);
            if (mat.HasProperty("_CoreColor"))
                mat.SetColor("_CoreColor", core);
            if (mat.HasProperty("_Intensity"))
                mat.SetFloat("_Intensity", 2.2f);
            img.color = Color.white;
        }

        static void SetAlpha(Material mat, float a)
        {
            if (mat == null)
                return;
            if (mat.HasProperty("_Intensity"))
                mat.SetFloat("_Intensity", 0.6f + 1.6f * a);
            if (mat.HasProperty("_Color"))
            {
                Color c = mat.GetColor("_Color");
                c.a = a;
                mat.SetColor("_Color", c);
            }
        }

        static int GlyphIndex(int runeId) => Mathf.Clamp(runeId <= 0 ? 0 : runeId - 1, 0, 11);

        static Texture2D BuildGlyphAtlas()
        {
            // 24 harf yer tutucu: 8×3 ızgara.
            const int cell = 64;
            const int cols = 8;
            const int rows = 3;
            var tex = new Texture2D(cell * cols, cell * rows, TextureFormat.RGBA32, false)
            {
                name = "DragonScriptGlyphs_Placeholder",
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[tex.width * tex.height];
            for (int i = 0; i < cols * rows; i++)
            {
                int c = i % cols;
                int r = i / cols;
                int x0 = c * cell;
                int y0 = (rows - 1 - r) * cell;
                for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                {
                    float nx = x / (float)cell * 2f - 1f;
                    float ny = y / (float)cell * 2f - 1f;
                    // Basit ejderha-harf hissi: kırık açısal stroke.
                    float stroke = 0f;
                    stroke = Mathf.Max(stroke, Mathf.Exp(-Mathf.Pow(ny - 0.6f * nx, 2f) * 40f));
                    stroke = Mathf.Max(stroke, Mathf.Exp(-Mathf.Pow(nx + 0.35f, 2f) * 55f) * Mathf.Exp(-ny * ny * 8f));
                    stroke = Mathf.Max(stroke, Mathf.Exp(-Mathf.Pow(ny + 0.45f, 2f) * 50f) * Mathf.Exp(-(nx - 0.2f) * (nx - 0.2f) * 10f));
                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(stroke * 255f), 0, 255);
                    pixels[(y0 + y) * tex.width + (x0 + x)] = new Color32(255, 220, 140, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        void OnDestroy()
        {
            if (_verbMat != null)
                Destroy(_verbMat);
            if (_adjMat != null)
                Destroy(_adjMat);
            if (_atlas != null)
                Destroy(_atlas);
        }
    }
}
