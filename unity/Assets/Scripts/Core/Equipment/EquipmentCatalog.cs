using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

namespace Dovus.Core.Equipment
{
    /// <summary>
    /// docs/element-sistemi.json equipment_system — 6 element × 3 slot = 18 örnek item.
    /// SkillMotor.FromJson deseni: MiniJson ağaç, uygulama/UI yok.
    /// </summary>
    public sealed class EquipmentCatalog
    {
        readonly List<EquipmentItem> _items = new();
        readonly Dictionary<string, EquipmentItem> _byId = new(StringComparer.Ordinal);

        EquipmentCatalog() { }

        public IReadOnlyList<EquipmentItem> Items => _items;

        /// <summary>JSON rules.element_match_bonus ham metin (ör. "+%10 etki").</summary>
        public string ElementMatchBonusText { get; private set; } = string.Empty;

        public static EquipmentCatalog FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON boş.", nameof(json));

            JsonValue root = MiniJson.Parse(json);
            JsonValue eq = root["equipment_system"];
            var catalog = new EquipmentCatalog
            {
                ElementMatchBonusText = eq["rules"]["element_match_bonus"].AsString()
            };

            foreach (JsonValue row in eq["examples"].AsArray())
            {
                string element = row["element"].AsString();
                if (string.IsNullOrEmpty(element))
                    continue;

                TryAdd(catalog, row["weapon"].AsString(), EquipmentSlot.Weapon, "weapon", element);
                TryAdd(catalog, row["armor"].AsString(), EquipmentSlot.Armor, "armor", element);
                TryAdd(catalog, row["accessory"].AsString(), EquipmentSlot.Accessory, "accessory", element);
            }

            return catalog;
        }

        public bool TryGet(string id, out EquipmentItem item) =>
            _byId.TryGetValue(id ?? string.Empty, out item!);

        /// <summary>İlk eşleşen yuva+element; yoksa null.</summary>
        public EquipmentItem? Find(EquipmentSlot slot, string element)
        {
            if (string.IsNullOrEmpty(element))
                return null;
            for (int i = 0; i < _items.Count; i++)
            {
                EquipmentItem it = _items[i];
                if (it.Slot == slot && string.Equals(it.Element, element, StringComparison.Ordinal))
                    return it;
            }
            return null;
        }

        static void TryAdd(
            EquipmentCatalog catalog,
            string name,
            EquipmentSlot slot,
            string slotKey,
            string element)
        {
            if (string.IsNullOrEmpty(name))
                return;

            string id = slotKey + ":" + element;
            var item = new EquipmentItem(id, name, slot, element);
            catalog._items.Add(item);
            catalog._byId[id] = item;
        }
    }
}
