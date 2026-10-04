using Dovus.Core.Boss;
using Dovus.Core.Casting;
using Dovus.Core.Data;
using Dovus.Core.Equipment;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Core.Mechanic;
using Dovus.Game.Composition;
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
        public const string ResourcePath = AssetCatalog.ElementResourcePath;
        public const string RequiredVersion = AssetCatalog.ElementRequiredVersion;

        public static bool TryLoad(out ElementSystemDesign design) =>
            AssetCatalog.Standalone.TryGetElementDesign(out design);

        public static ElementSystemDesign LoadRequired()
        {
            if (TryLoad(out ElementSystemDesign design))
                return design;
            throw new InvalidOperationException(
                $"Resources/{ResourcePath}.json canonical v{RequiredVersion} yüklenemedi.");
        }

        public static void ClearCache() => AssetCatalog.ResetStandaloneForEditor();
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
