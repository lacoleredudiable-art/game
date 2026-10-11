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
    /// bağlı dünya dörtgeni olarak ortak VFX_Kor batch'inde çizilir (<see cref="EjderVfxBatchHost"/>).
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
        EjderVfxBatchHost _batch;
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
        bool _notCast;
        float _notCastAge;

        public bool VerbVisible => _playing && _verbOn;
        public bool AdjectiveVisible => _playing && _adjOn;
        public int PlayCount { get; private set; }

        /// <summary>"Atılmadı" geri bildirimi oynuyor (kül grisi, sönük harf).</summary>
        public bool NotCastActive => _notCast;
        public int NotCastCount { get; private set; }
        public RuleEngineV4CastDenial NotCastKind { get; private set; }
        /// <summary>Son çizilen "atılmadı" karesi: harf rengi (alfa dahil), dağılma, toz / yarım harf dörtgeni sayısı.</summary>
        public Color NotCastColor { get; private set; }
        public float NotCastScatter { get; private set; }
        public int NotCastDustQuads { get; private set; }
        public int NotCastHalfQuads { get; private set; }
        public int VerbCell => _verbCell;
        public int AdjectiveCell => _adjCell;
        public RuleEngineV4UvRect VerbUv => RuleVfxSettings.Slice.Atlas.GlyphUv(_verbCell);
        public RuleEngineV4UvRect AdjectiveUv => RuleVfxSettings.Slice.Atlas.GlyphUv(_adjCell);
        /// <summary>Ortak batch materyali (siluet, harf, halka aynı).</summary>
        public Material LetterMaterial => _batch != null ? _batch.SharedMaterial : null;

        /// <summary>Test / araç: kural-motoru-v4 + Ejder_Glif_Sirasi yerine verilen eşleme.</summary>
        public RuleEngineV4GlyphMap GlyphMap
        {
            get => _glyphMap ??= LoadGlyphMap();
            set => _glyphMap = value;
        }

        public void Play(int verbRuneId, int adjectiveRuneId, Color coreColor)
        {
            _notCast = false;
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

        /// <summary>
        /// Kombo tanındı ama atılmadı (yalnız mana / bekleme): harfler yerinde kül grisi ve sönük belirir,
        /// titreyip dağılır. Diğer red türleri (ıska, boşa gitme) buraya gelmez; gelirse yok sayılır.
        /// </summary>
        public void PlayNotCast(int verbRuneId, int adjectiveRuneId, RuleEngineV4CastDenial denial)
        {
            if (!RuleEngineV4NotCastFx.Applies(denial))
                return;
            _playing = false;
            _notCast = true;
            _notCastAge = 0f;
            NotCastKind = denial;
            NotCastCount++;

            float size = RuleVfxDefaults.RuneLetterSizePx;
            float gap = RuleVfxDefaults.RuneLetterGapPx;
            _verbPos = new Vector2(-size * 0.5f - gap * 0.5f, RuleVfxArtDefaults.RuneAnchorY);
            _adjPos = new Vector2(size * 0.5f + gap * 0.5f, RuleVfxArtDefaults.RuneAnchorY);
            RuleEngineV4GlyphMap map = GlyphMap;
            _verbOn = map.TryVerbCell(verbRuneId, out int verbCell);
            _adjOn = map.TryAdjectiveCell(adjectiveRuneId, out int adjCell);
            if (_verbOn)
                _verbCell = Mathf.Clamp(verbCell, 0, RuleVfxArtDefaults.GlyphCellMaxIndex);
            else
                WarnMissingGlyph(RuleEngineV4GlyphDefaults.VerbPrefix, verbRuneId);
            if (_adjOn)
                _adjCell = Mathf.Clamp(adjCell, 0, RuleVfxArtDefaults.GlyphCellMaxIndex);
            else
                WarnMissingGlyph(RuleEngineV4GlyphDefaults.AdjectivePrefix, adjectiveRuneId);
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
            if (_notCast)
            {
                _notCastAge += Time.unscaledDeltaTime;
                if (RuleVfxSettings.Slice.NotCast.Done(_notCastAge))
                    _notCast = false;
                return;
            }
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

        public void EmitQuads(EjderVfxBatchHost batch)
        {
            if (!_playing && !_notCast)
                return;
            Camera cam = batch.ViewCamera;
            if (cam == null)
                return;
            RuleEngineV4VfxAtlas atlas = batch.Layout;
            float depth = Mathf.Max(RuleVfxDefaults.RuneQuadDepthM, cam.nearClipPlane * 2f);
            var frame = new LetterFrame(cam.transform, depth,
                WorldPerScreenPx(cam, depth) * cam.pixelWidth / RuleVfxDefaults.RuneCanvasReferenceWidthPx);
            if (_notCast)
            {
                EmitNotCast(batch, atlas, frame);
                return;
            }
            float half = RuleVfxDefaults.RuneLetterSizePx * 0.5f;
            if (_verbOn)
                EmitLetter(batch, frame, _verbPos, half, half, atlas.GlyphUv(_verbCell), WithAlpha(_verbTint, _alpha), 0f);
            if (_adjOn)
                EmitLetter(batch, frame, _adjPos, half, half, atlas.GlyphUv(_adjCell), WithAlpha(_adjTint, _alpha), 0f);
        }

        void EmitNotCast(EjderVfxBatchHost batch, RuleEngineV4VfxAtlas atlas, in LetterFrame frame)
        {
            RuleEngineV4NotCastFx fx = RuleVfxSettings.Slice.NotCast;
            fx.Sample(_notCastAge, out float shakePx, out float alpha, out float scatter);
            var ash = new Color(fx.AshR, fx.AshG, fx.AshB, alpha);
            NotCastColor = ash;
            NotCastScatter = scatter;
            NotCastDustQuads = 0;
            NotCastHalfQuads = 0;
            var shake = new Vector2(shakePx, 0f);
            if (_verbOn)
                EmitNotCastLetter(batch, atlas, frame, fx, _verbPos + shake, atlas.GlyphUv(_verbCell), ash, scatter);
            if (_adjOn)
                EmitNotCastLetter(batch, atlas, frame, fx, _adjPos + shake, atlas.GlyphUv(_adjCell), ash, scatter);
        }

        void EmitNotCastLetter(
            EjderVfxBatchHost batch,
            RuleEngineV4VfxAtlas atlas,
            in LetterFrame frame,
            RuleEngineV4NotCastFx fx,
            Vector2 pos,
            in RuleEngineV4UvRect uv,
            Color ash,
            float scatter)
        {
            float half = RuleVfxDefaults.RuneLetterSizePx * 0.5f;
            if (NotCastKind == RuleEngineV4CastDenial.Cooldown)
            {
                // Çatlak: harf dikey ortadan ikiye bölünür, yarılar aralık kadar ayrılır.
                float quarter = half * 0.5f;
                float split = fx.CrackGap(_notCastAge) * 0.5f;
                EmitLetter(batch, frame, pos + new Vector2(-quarter - split, 0f), quarter, half, uv.LeftHalf, ash, scatter);
                EmitLetter(batch, frame, pos + new Vector2(quarter + split, 0f), quarter, half, uv.RightHalf, ash, scatter);
                NotCastHalfQuads += 2;
                return;
            }

            EmitLetter(batch, frame, pos, half, half, uv, ash, scatter);
            // Mana yok: harfin altından kül tozu düşer.
            float dustHalf = fx.DustSizePx * 0.5f;
            float fall = fx.DustFall(_notCastAge);
            RuleEngineV4UvRect dustUv = atlas.DustUv;
            for (int i = 0; i < fx.DustCount; i++)
            {
                var p = new Vector2(pos.x + fx.DustX(i), pos.y - half - fall * (1f + (float)i / fx.DustCount));
                EmitLetter(batch, frame, p, dustHalf, dustHalf, dustUv, ash, 0f);
                NotCastDustQuads++;
            }
        }

        /// <summary>Kameraya bağlı harf düzlemi: referans piksel → dünya.</summary>
        readonly struct LetterFrame
        {
            public readonly Vector3 Origin;
            public readonly Vector3 Right;
            public readonly Vector3 Up;
            public readonly float WorldPerRefPx;

            public LetterFrame(Transform cam, float depth, float worldPerRefPx)
            {
                Origin = cam.position + cam.forward * depth;
                Right = cam.right;
                Up = cam.up;
                WorldPerRefPx = worldPerRefPx;
            }
        }

        static void EmitLetter(
            EjderVfxBatchHost batch,
            in LetterFrame f,
            Vector2 refPx,
            float halfWidthPx,
            float halfHeightPx,
            in RuleEngineV4UvRect uv,
            Color color,
            float dissolve)
        {
            float k = f.WorldPerRefPx;
            Vector3 center = f.Origin + f.Right * (refPx.x * k) + f.Up * (refPx.y * k);
            batch.AddQuad(center, f.Right * (halfWidthPx * k), f.Up * (halfHeightPx * k), uv, color, dissolve);
        }

        static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
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

        public void Bind(EjderVfxBatchHost batch)
        {
            if (_batch != null)
                _batch.Unregister(this);
            _batch = batch;
            if (_batch != null)
                _batch.Register(this);
        }

        /// <summary>Ton rün kimliğinden (hücreden değil): atlas sırası değişse de görünen renk aynı kalır.</summary>
        static float RuneHue(int runeId) =>
            Mathf.Clamp(runeId - 1, 0, RuleVfxArtDefaults.GlyphHueMod - 1) / (float)RuleVfxArtDefaults.GlyphHueMod;

        void OnDestroy()
        {
            if (_batch != null)
                _batch.Unregister(this);
        }
    }
}
