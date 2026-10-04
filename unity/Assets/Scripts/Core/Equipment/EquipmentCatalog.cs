using System;
using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Shared;

namespace Dovus.Core.Equipment
{
    /// <summary>
    /// docs/element-sistemi.json v6.1.1 weapons[] (10 silah) + uyumsuz_cizim kuralları.
    /// SkillMotor.FromJson deseni: MiniJson ağaç, uygulama/UI yok. v5 equipment_system
    /// şeması artık okunmuyor (CLEANUP-2b).
    /// </summary>
    public sealed class EquipmentCatalog
    {
        readonly List<EquipmentItem> _items = new();
        readonly Dictionary<string, EquipmentItem> _byId = new(StringComparer.Ordinal);

        EquipmentCatalog() { }

        public IReadOnlyList<EquipmentItem> Items => _items;
        public float CompatibleDamageMult { get; private set; } = 1f;
        public float CompatibleCastTimeMult { get; private set; } = 1f;
        public float IncompatibleDamageMult { get; private set; } = 1f;
        public float IncompatibleCastTimeMult { get; private set; } = 1f;
        public string CompatibleUiColor { get; private set; } = string.Empty;
        public string IncompatibleUiColor { get; private set; } = string.Empty;
        public string CompatibleUiLabel { get; private set; } = string.Empty;
        public string IncompatibleUiLabel { get; private set; } = string.Empty;

        public static EquipmentCatalog FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON boş.", nameof(json));
            return FromDocument(ElementSystemDocument.Parse(json));
        }

        public static EquipmentCatalog FromDocument(ElementSystemDocument doc) =>
            FromJsonRoot(doc.Root);

        public static EquipmentCatalog FromJsonRoot(JsonValue root)
        {
            if (root["weapons"].Kind != JsonKind.Array)
                throw new InvalidOperationException(
                    "element-sistemi: v6 şeması (weapons[]) bekleniyor; v5 equipment_system artık okunmuyor.");
            return ParseV61(root);
        }

        static EquipmentCatalog ParseV61(JsonValue root)
        {
            JsonValue effects = root["uyumsuz_cizim"]["effects"];
            var catalog = new EquipmentCatalog
            {
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
    }
}
