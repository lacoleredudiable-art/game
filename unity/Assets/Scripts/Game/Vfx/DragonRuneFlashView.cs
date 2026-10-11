using System.Collections.Generic;
using Dovus.Core.Presentation;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Skills.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §1.1: çizilen rün 0,2 s'de Ejderha Dili harfine oturur (fiil→sıfat),
    /// 0,3 s parlar. Harfler ortak efekt atlasının harf bölgesinden (8×3, 0 = sol üst), kameraya
    /// bağlı dünya dörtgeni olarak ortak VFX_Kor batch'inde çizilir (<see cref="EjderVfxBatch"/>).
    /// Hücre rün → kod adı → atlas sırası eşlemesinden gelir; adı olmayan harf gizlenir.
    /// </summary>
    public sealed class DragonRuneFlashView : MonoBehaviour, IEjderVfxQuadSource
    {
        readonly HashSet<string> _warnedMissingGlyph = new();
        RuleEngineV4GlyphMap _glyphMap;
        float _age;
        float _snap;
        float _glow;
        bool _playing;
        bool _registered;
        bool _verbOn;
        bool _adjOn;
        int _verbCell;
        int _adjCell;
        Color _verbTint;
        Color _adjTint;
        float _alpha;
        Vector2 _verbFrom;
        Vector2 _adjFrom;
        Vector2 _verbTo;
        Vector2 _adjTo;
        Vector2 _verbPos;
        Vector2 _adjPos;

        public bool VerbVisible => _playing && _verbOn;
        public bool AdjectiveVisible => _playing && _adjOn;
        public int PlayCount { get; private set; }
        public int VerbCell => _verbCell;
        public int AdjectiveCell => _adjCell;
        public RuleEngineV4UvRect VerbUv => RuleVfxSettings.Slice.Atlas.GlyphUv(_verbCell);
        public RuleEngineV4UvRect AdjectiveUv => RuleVfxSettings.Slice.Atlas.GlyphUv(_adjCell);
        /// <summary>Ortak batch materyali (siluet, harf, halka aynı).</summary>
        public Material LetterMaterial => EjderVfxBatch.Instance.SharedMaterial;

        /// <summary>Test / araç: kural-motoru-v4 + Ejder_Glif_Sirasi yerine verilen eşleme.</summary>
        public RuleEngineV4GlyphMap GlyphMap
        {
            get => _glyphMap ??= LoadGlyphMap();
            set => _glyphMap = value;
        }

        public void Play(int verbRuneId, int adjectiveRuneId, Color coreColor)
        {
            EnsureRegistered();
            PlayCount++;
            _snap = VfxPlanDefaults.RuneSnapSec;
            _glow = VfxPlanDefaults.RuneGlowSec;
            _age = 0f;
            _alpha = 1f;
            _playing = true;

            float size = RuleVfxDefaults.RuneLetterSizePx;
            float gap = RuleVfxDefaults.RuneLetterGapPx;
            _verbTo = new Vector2(-size * 0.5f - gap * 0.5f, RuleVfxArtDefaults.RuneAnchorY);
            _adjTo = new Vector2(size * 0.5f + gap * 0.5f, RuleVfxArtDefaults.RuneAnchorY);
            _verbFrom = _verbTo + new Vector2(-RuleVfxArtDefaults.RuneFromOffsetX, RuleVfxArtDefaults.RuneFromOffsetY);
            _adjFrom = _adjTo + new Vector2(RuleVfxArtDefaults.RuneFromOffsetX, -RuleVfxArtDefaults.RuneAdjFromY);

            RuleEngineV4GlyphMap map = GlyphMap;
            _verbOn = map.TryVerbCell(verbRuneId, out int verbCell);
            _adjOn = map.TryAdjectiveCell(adjectiveRuneId, out int adjCell);
            if (_verbOn)
            {
                _verbCell = Mathf.Clamp(verbCell, 0, RuleVfxArtDefaults.GlyphCellMaxIndex);
                _verbTint = Tint(RuneHue(verbRuneId), coreColor);
            }
            else
                WarnMissingGlyph(RuleEngineV4GlyphDefaults.VerbPrefix, verbRuneId);
            if (_adjOn)
            {
                _adjCell = Mathf.Clamp(adjCell, 0, RuleVfxArtDefaults.GlyphCellMaxIndex);
                _adjTint = Tint(RuneHue(adjectiveRuneId), coreColor);
            }
            else
                WarnMissingGlyph(RuleEngineV4GlyphDefaults.AdjectivePrefix, adjectiveRuneId);
            Layout(0f);
        }

        void WarnMissingGlyph(string prefix, int runeId)
        {
            if (!_warnedMissingGlyph.Add(prefix + runeId))
                return;
            Debug.LogWarning($"[DragonRuneFlash] {prefix}{runeId}: rune_glyphs / {RuleVfxDefaults.GlyphOrderResource} içinde glif yok; harf gizli.");
        }

        static RuleEngineV4GlyphMap LoadGlyphMap()
        {
            TextAsset json = Resources.Load<TextAsset>(RuleEngineV4CatalogAccess.JsonResource);
            TextAsset order = Resources.Load<TextAsset>(RuleVfxDefaults.GlyphOrderResource);
            return RuleEngineV4GlyphMap.FromJson(json != null ? json.text : null, order != null ? order.text : null);
        }

        void Update()
        {
            if (!_playing)
                return;
            _age += Time.unscaledDeltaTime;
            float snapU = Mathf.Clamp01(_age / _snap);
            Layout(snapU);

            float glowAge = _age - _snap;
            _alpha = glowAge <= 0f ? 1f : 1f - Mathf.Clamp01(glowAge / _glow);

            if (_age >= _snap + _glow)
                _playing = false;
        }

        void Layout(float snapU)
        {
            float u = snapU * snapU * (RuleVfxArtDefaults.SmoothstepThree - RuleVfxArtDefaults.SmoothstepTwo * snapU);
            _verbPos = Vector2.Lerp(_verbFrom, _verbTo, u);
            _adjPos = Vector2.Lerp(
                _adjFrom,
                _adjTo,
                Mathf.Clamp01((snapU - RuleVfxArtDefaults.RuneAdjDelay) / RuleVfxArtDefaults.RuneAdjDelayInv));
        }

        public void EmitQuads(EjderVfxBatch batch)
        {
            if (!_playing)
                return;
            Camera cam = Camera.main;
            if (cam == null)
                return;
            RuleEngineV4VfxAtlas atlas = batch.Layout;
            float depth = Mathf.Max(RuleVfxDefaults.RuneQuadDepthM, cam.nearClipPlane * 2f);
            float worldPerRefPx = WorldPerScreenPx(cam, depth) * cam.pixelWidth / RuleVfxDefaults.RuneCanvasReferenceWidthPx;
            Transform t = cam.transform;
            Vector3 origin = t.position + t.forward * depth;
            float half = RuleVfxDefaults.RuneLetterSizePx * 0.5f * worldPerRefPx;
            Vector3 halfRight = t.right * half;
            Vector3 halfUp = t.up * half;
            if (_verbOn)
                EmitLetter(batch, origin, t, _verbPos, worldPerRefPx, halfRight, halfUp, atlas.GlyphUv(_verbCell), _verbTint);
            if (_adjOn)
                EmitLetter(batch, origin, t, _adjPos, worldPerRefPx, halfRight, halfUp, atlas.GlyphUv(_adjCell), _adjTint);
        }

        void EmitLetter(
            EjderVfxBatch batch,
            Vector3 origin,
            Transform cam,
            Vector2 refPx,
            float worldPerRefPx,
            Vector3 halfRight,
            Vector3 halfUp,
            in RuleEngineV4UvRect uv,
            Color tint)
        {
            Vector3 center = origin + cam.right * (refPx.x * worldPerRefPx) + cam.up * (refPx.y * worldPerRefPx);
            tint.a = _alpha;
            batch.AddQuad(center, halfRight, halfUp, uv, tint, 0f);
        }

        /// <summary>Kameradan <paramref name="depth"/> uzaklıkta bir ekran pikselinin dünya boyu.</summary>
        static float WorldPerScreenPx(Camera cam, float depth)
        {
            float h = cam.orthographic
                ? cam.orthographicSize * 2f
                : depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f;
            return h / Mathf.Max(1, cam.pixelHeight);
        }

        static Color Tint(float hue, Color core)
        {
            Color tint = Color.HSVToRGB(hue, RuleVfxArtDefaults.RuneHueSat, 1f) * core;
            tint.a = 1f;
            return tint;
        }

        void EnsureRegistered()
        {
            if (_registered)
                return;
            _registered = true;
            EjderVfxBatch.Register(this);
        }

        /// <summary>Ton rün kimliğinden (hücreden değil): atlas sırası değişse de görünen renk aynı kalır.</summary>
        static float RuneHue(int runeId) =>
            Mathf.Clamp(runeId - 1, 0, RuleVfxArtDefaults.GlyphHueMod - 1) / (float)RuleVfxArtDefaults.GlyphHueMod;

        void OnDestroy()
        {
            if (_registered)
                EjderVfxBatch.Unregister(this);
        }
    }
}
