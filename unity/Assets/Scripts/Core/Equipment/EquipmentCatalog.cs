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
        public bool IsV61 { get; private set; }
        public float CompatibleDamageMult { get; private set; } = 1f;
        public float CompatibleCastTimeMult { get; private set; } = 1f;
        public float IncompatibleDamageMult { get; private set; } = 1f;
        public float IncompatibleCastTimeMult { get; private set; } = 1f;
        public string CompatibleUiColor { get; private set; } = string.Empty;
        public string IncompatibleUiColor { get; private set; } = string.Empty;
        public string CompatibleUiLabel { get; private set; } = string.Empty;
        public string IncompatibleUiLabel { get; private set; } = string.Empty;

        /// <summary>JSON rules.element_match_bonus ham metin (ör. "+%10 etki").</summary>
        public string ElementMatchBonusText { get; private set; } = string.Empty;

        public static EquipmentCatalog FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON boş.", nameof(json));

            JsonValue root = MiniJson.Parse(json);
            if (root["weapons"].Kind == JsonKind.Array)
                return ParseV61(root);

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

        static EquipmentCatalog ParseV61(JsonValue root)
        {
            JsonValue effects = root["uyumsuz_cizim"]["effects"];
            var catalog = new EquipmentCatalog
            {
                IsV61 = true,
                CompatibleDamageMult = effects["uyumlu"]["damage_mult"].AsFloat(1f),
                CompatibleCastTimeMult = effects["uyumlu"]["cast_time_mult"].AsFloat(1f),
                IncompatibleDamageMult = effects["uyumsuz"]["damage_mult"].AsFloat(1f),
                IncompatibleCastTimeMult = effects["uyumsuz"]["cast_time_mult"].AsFloat(1f),
                CompatibleUiColor = effects["uyumlu"]["ui_color"].AsString(),
                IncompatibleUiColor = effects["uyumsuz"]["ui_color"].AsString(),
                CompatibleUiLabel = root["uyumsuz_cizim"]["ui_etiketi"]["uyumlu"].AsString(),
                IncompatibleUiLabel = root["uyumsuz_cizim"]["ui_etiketi"]["uyumsuz"].AsString()
            };

            foreach (JsonValue row in root["weapons"].AsArray())
            {
                int numericId = row["id"].AsInt();
                if (numericId <= 0)
                    continue;
                IReadOnlyList<JsonValue> values = row["compatible_verbs"].AsArray();
                var verbs = new int[values.Count];
                for (int i = 0; i < verbs.Length; i++)
                    verbs[i] = values[i].AsInt();

                var item = new EquipmentItem(
                    "weapon:" + numericId,
                    row["name"].AsString(),
                    EquipmentSlot.Weapon,
                    string.Empty,
                    row["damage_mult"].AsFloat(1f),
                    row["cast_time_mult"].AsFloat(1f),
                    row["poise_mult"].AsFloat(1f),
                    row["range_mult"].AsFloat(1f),
                    row["mobility_mod"].AsInt(0),
                    row["identity_passive"].AsString(),
                    verbs,
                    row["animations_key"].AsString(),
                    row["type"].AsString(),
                    WeaponCombatProfile.FromRow(row));
                catalog._items.Add(item);
                catalog._byId[item.Id] = item;
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

        public EquipmentItem? FindWeapon(int id)
        {
            return TryGet("weapon:" + id.ToString(), out EquipmentItem item) ? item : null;
        }

        public EquipmentItem? FindWeapon(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            for (int i = 0; i < _items.Count; i++)
            {
                EquipmentItem item = _items[i];
                if (item.Slot == EquipmentSlot.Weapon
                    && string.Equals(item.Name, name, StringComparison.Ordinal))
                    return item;
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
