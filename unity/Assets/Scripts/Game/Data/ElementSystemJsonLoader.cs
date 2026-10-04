using Dovus.Core.Combat;
using Dovus.Core.Data;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Presentation;
using System;
using UnityEngine;

namespace Dovus.Game.Data
{
    /// <summary>
    /// Binding sıra adım 1: tek canonical Resources JSON'u bir kez yükler ve bütün
    /// tüketicilere aynı parse edilmiş tasarımı verir.
    /// </summary>
    public static class ElementSystemJsonLoader
    {
        public const string ResourcePath = "ElementSystem/element-sistemi";
        public const string RequiredVersion = "6.1.1";

        static ElementSystemDesign _cached;

        public static bool TryLoad(out ElementSystemDesign design)
        {
            if (_cached != null)
            {
                design = _cached;
                return true;
            }

            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
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
                if (!string.Equals(version, RequiredVersion, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"element-sistemi version {version}; {RequiredVersion} bekleniyor.");
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

                _cached = new ElementSystemDesign(
                    asset.text, doc, version, motor, equipment, animations);
                design = _cached;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[JSONLoader] v6.1.1 yüklenemedi: {e.Message}");
                design = null;
                return false;
            }
        }

        public static ElementSystemDesign LoadRequired()
        {
            if (TryLoad(out ElementSystemDesign design))
                return design;
            throw new InvalidOperationException(
                $"Resources/{ResourcePath}.json canonical v{RequiredVersion} yüklenemedi.");
        }

        public static void ClearCache() => _cached = null;
    }

    public sealed class ElementSystemDesign
    {
        public ElementSystemDesign(
            string json,
            ElementSystemDocument document,
            string version,
            SkillMotor skillMotor,
            EquipmentCatalog equipment,
            AnimationDatabase animations)
        {
            Json = json ?? string.Empty;
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Version = version ?? string.Empty;
            SkillMotor = skillMotor ?? throw new ArgumentNullException(nameof(skillMotor));
            Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            Animations = animations ?? throw new ArgumentNullException(nameof(animations));
        }

        public string Json { get; }
        public ElementSystemDocument Document { get; }
        public string Version { get; }
        public SkillMotor SkillMotor { get; }
        public EquipmentCatalog Equipment { get; }
        public AnimationDatabase Animations { get; }

        MechanicGrammar _mechanics;
        bool _mechanicsTried;

        /// <summary>mechanic_grammar motoru (ilk istekte kurulur); JSON'da yoksa null.</summary>
        public MechanicGrammar Mechanics
        {
            get
            {
                if (_mechanicsTried)
                    return _mechanics;
                _mechanicsTried = true;
                try
                {
                    MechanicRules rules = MechanicRules.FromDocument(Document);
                    _mechanics = rules.IsValid ? new MechanicGrammar(rules) : null;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Mechanic] mechanic_grammar yüklenemedi: {e.Message}");
                    _mechanics = null;
                }
                return _mechanics;
            }
        }
    }
}
