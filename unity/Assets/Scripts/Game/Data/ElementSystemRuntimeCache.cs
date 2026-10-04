using Dovus.Core.Boss;
using Dovus.Core.Casting;
using Dovus.Core.Data;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Game.Assets;
using System;
using UnityEngine;

namespace Dovus.Game.Data
{
    /// <summary>element-sistemi.json tek yükleme önbelleği (AssetCatalog'dan ayrı).</summary>
    public static class ElementSystemRuntimeCache
    {
        public const string ResourcePath = "ElementSystem/element-sistemi";
        public const string RequiredVersion = "6.1.1";

        static ElementSystemDesign _design;
        static bool _loadAttempted;

        public static void ResetForEditor()
        {
            _design = null;
            _loadAttempted = false;
        }

        public static bool TryGet(out ElementSystemDesign design)
        {
            if (_loadAttempted)
            {
                design = _design;
                return design != null;
            }

            _loadAttempted = true;
            if (TryParseJson(out ElementSystemDesign parsed))
            {
                _design = parsed;
                design = parsed;
                return true;
            }

            design = null;
            return false;
        }

        internal static bool TryParseJson(out ElementSystemDesign design)
        {
            TextAsset asset = AssetLoader.Load<TextAsset>(ResourcePath, null);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                design = null;
                return false;
            }

            try
            {
                ElementSystemDocument doc = ElementSystemDocument.Parse(asset.text);
                if (!ElementSystemHeader.TryParse(doc, ElementSystemLoaderDefaults.ElementHeaderScanMaxChars, out ElementSystemHeader header))
                    throw new InvalidOperationException("element-sistemi kökü okunamadı.");
                string version = header.Version;
                if (!string.Equals(version, RequiredVersion, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"element-sistemi version {version}; {RequiredVersion} bekleniyor.");
                if (!header.Binding)
                    throw new InvalidOperationException("element-sistemi binding=true değil.");

                SkillMotor motor = SkillMotor.FromDocument(doc);
                EquipmentCatalog equipment = EquipmentCatalog.FromDocument(doc);
                AnimationDatabase animations = AnimationDatabase.FromDocument(doc);
                if (motor.RuneCount != 12 || motor.SkillCount != ElementSystemLoaderDefaults.ExpectedSkillCount
                    || equipment.Items.Count != 10 || motor.ElementPaints.Count != 6
                    || animations.Count != ElementSystemLoaderDefaults.ExpectedAnimationCount)
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
    }
}
