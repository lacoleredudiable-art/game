using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Game.Weapons;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Data
{
    /// <summary>
    /// Binding sıra adım 2 runtime fallback: canonical JSON parse'ından 12/10/6 SO üretir.
    /// Editor importer aynı Import metotlarıyla kalıcı .asset dosyaları oluşturabilir.
    /// </summary>
    public sealed class ElementSystemAssetCatalog
    {
        readonly List<RuneSO> _runes = new();
        readonly List<WeaponSO> _weapons = new();
        readonly List<ElementSO> _elements = new();
        readonly Dictionary<int, RuneSO> _runeById = new();
        readonly Dictionary<int, WeaponSO> _weaponById = new();
        readonly Dictionary<int, ElementSO> _elementById = new();

        ElementSystemAssetCatalog(string version)
        {
            Version = version ?? string.Empty;
        }

        public string Version { get; }
        public IReadOnlyList<RuneSO> Runes => _runes;
        public IReadOnlyList<WeaponSO> Weapons => _weapons;
        public IReadOnlyList<ElementSO> Elements => _elements;

        public static ElementSystemAssetCatalog CreateRuntime(ElementSystemDesign design)
        {
            if (design == null)
                throw new ArgumentNullException(nameof(design));
            var catalog = new ElementSystemAssetCatalog(design.Version);

            foreach (RuneDefinition source in design.SkillMotor.RuneDefinitions)
            {
                RuneSO asset = ScriptableObject.CreateInstance<RuneSO>();
                asset.hideFlags = HideFlags.DontSave;
                asset.Import(source);
                catalog._runes.Add(asset);
                catalog._runeById[asset.Id] = asset;
            }

            foreach (EquipmentItem source in design.Equipment.Items)
            {
                if (source.Slot != EquipmentSlot.Weapon)
                    continue;
                WeaponSO asset = ScriptableObject.CreateInstance<WeaponSO>();
                asset.hideFlags = HideFlags.DontSave;
                asset.Import(source);
                catalog._weapons.Add(asset);
                catalog._weaponById[asset.Id] = asset;
            }

            foreach (ElementPaintNode source in design.SkillMotor.ElementPaints)
            {
                ElementSO asset = ScriptableObject.CreateInstance<ElementSO>();
                asset.hideFlags = HideFlags.DontSave;
                asset.Import(source);
                catalog._elements.Add(asset);
                catalog._elementById[asset.Id] = asset;
            }

            if (catalog._runes.Count != 12 || catalog._weapons.Count != 10
                || catalog._elements.Count != 6)
            {
                throw new InvalidOperationException("Runtime SO catalog 12 rune / 10 weapon / 6 element üretmelidir.");
            }
            return catalog;
        }

        public WeaponSO FindWeapon(int id) =>
            _weaponById.TryGetValue(id, out WeaponSO value) ? value : null;

        public ElementSO FindElement(int id) =>
            _elementById.TryGetValue(id, out ElementSO value) ? value : null;
    }
}
