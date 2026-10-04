using Dovus.Core.Boss;
using Dovus.Core.Casting;
using Dovus.Core.Damage;
using Dovus.Core.Data;
using Dovus.Core.Dodge;
using Dovus.Core.Element;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Hud;
using Dovus.Core.Input;
using Dovus.Core.Mechanic;
using Dovus.Core.Passives;
using Dovus.Core.Presentation;
using Dovus.Game.Audio;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using Dovus.Game.Vfx;
using System;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Composition
{
    /// <summary>
    /// Salt-okunur runtime asset'leri composition kökünde bir kez yükler.
    /// Bootstrap dışı (editör, elle sahne) için <see cref="Standalone"/> aynı yedekleri kullanır.
    /// </summary>
    public sealed class AssetCatalog
    {
        public const string ElementResourcePath = "ElementSystem/element-sistemi";
        public const string ElementRequiredVersion = "6.1.1";

        static AssetCatalog _standalone;

        ElementSystemDesign _elementDesign;
        bool _elementLoadAttempted;

        public HudTheme HudTheme { get; }
        public SfxLibrary Sfx { get; }
        public VfxLibrary Vfx { get; }

        AssetCatalog(HudTheme hudTheme, SfxLibrary sfx, VfxLibrary vfx)
        {
            HudTheme = hudTheme;
            Sfx = sfx;
            Vfx = vfx;
        }

        /// <summary>Bootstrap kurmadığı bağımsız tüketiciler için tek giriş (aynı yedek davranış).</summary>
        public static AssetCatalog Standalone => _standalone ??= Load();

        public static AssetCatalog Load() =>
            new AssetCatalog(ResolveHudTheme(), ResolveSfxLibrary(), ResolveVfxLibrary());

        public void ClearElementCache()
        {
            _elementDesign = null;
            _elementLoadAttempted = false;
        }

        public static void ResetStandaloneForEditor()
        {
            _standalone?.ClearElementCache();
            _standalone = null;
        }

        public bool TryGetElementDesign(out ElementSystemDesign design)
        {
            if (_elementLoadAttempted)
            {
                design = _elementDesign;
                return design != null;
            }

            _elementLoadAttempted = true;
            if (TryParseElementJson(out ElementSystemDesign parsed))
            {
                _elementDesign = parsed;
                design = parsed;
                return true;
            }

            design = null;
            return false;
        }

        internal static bool TryParseElementJson(out ElementSystemDesign design)
        {
            TextAsset asset = AssetLoader.Load<TextAsset>(ElementResourcePath, null);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                design = null;
                return false;
            }

            try
            {
                ElementSystemDocument doc = ElementSystemDocument.Parse(asset.text);
                if (!ElementSystemHeader.TryParse(doc, 300, out ElementSystemHeader header))
                    throw new InvalidOperationException("element-sistemi kökü okunamadı.");
                string version = header.Version;
                if (!string.Equals(version, ElementRequiredVersion, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"element-sistemi version {version}; {ElementRequiredVersion} bekleniyor.");
                if (!header.Binding)
                    throw new InvalidOperationException("element-sistemi binding=true değil.");

                SkillMotor motor = SkillMotor.FromDocument(doc);
                EquipmentCatalog equipment = EquipmentCatalog.FromDocument(doc);
                AnimationDatabase animations = AnimationDatabase.FromDocument(doc);
                if (motor.RuneCount != 12 || motor.SkillCount != 144
                    || equipment.Items.Count != 10 || motor.ElementPaints.Count != 6
                    || animations.Count != 120)
                {
                    throw new InvalidOperationException(
                        "v6.1.1 cardinality: 12 rune / 144 skill / 10 weapon / 6 element / 120 animation beklenir.");
                }

                design = new ElementSystemDesign(
                    asset.text, doc, version, motor, equipment, animations);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[JSONLoader] v6.1.1 yüklenemedi: {e.Message}");
                design = null;
                return false;
            }
        }

        static HudTheme ResolveHudTheme()
        {
            HudTheme loaded = AssetLoader.Load<HudTheme>("HudTheme", null);
            if (loaded != null)
                return loaded;
            HudTheme fallback = ScriptableObject.CreateInstance<HudTheme>();
            fallback.hideFlags = HideFlags.DontSave;
            return fallback;
        }

        static SfxLibrary ResolveSfxLibrary()
        {
            SfxLibrary loaded = AssetLoader.Load<SfxLibrary>("SfxLibrary", null);
            if (loaded != null)
                return loaded;
            SfxLibrary fallback = ScriptableObject.CreateInstance<SfxLibrary>();
            fallback.hideFlags = HideFlags.DontSave;
            return fallback;
        }

        static VfxLibrary ResolveVfxLibrary()
        {
            VfxLibrary loaded = AssetLoader.Load<VfxLibrary>("VfxLibrary", null);
            if (loaded != null)
                return loaded;
            VfxLibrary fallback = ScriptableObject.CreateInstance<VfxLibrary>();
            fallback.hideFlags = HideFlags.DontSave;
            return fallback;
        }
    }
}
