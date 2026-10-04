using System;
using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

namespace Dovus.Core.Equipment
{
    /// <summary>
    /// Silah taban zırhı. JSON'da base_armor yoksa 0.
    /// Ağır silah değerlerini başka iş koyar; bu katalog yalnız okur.
    /// </summary>
    public sealed class WeaponArmorCatalog
    {
        readonly Dictionary<string, float> _byId = new(StringComparer.Ordinal);

        public static WeaponArmorCatalog FromJson(string json) =>
            FromDocument(ElementSystemDocument.Parse(json));

        public static WeaponArmorCatalog FromDocument(ElementSystemDocument doc) =>
            FromJsonRoot(doc.Root);

        public static WeaponArmorCatalog FromJsonRoot(JsonValue root)
        {
            var catalog = new WeaponArmorCatalog();
            if (root.IsNull)
                return catalog;
            if (root["weapons"].Kind != JsonKind.Array)
                return catalog;
            foreach (JsonValue row in root["weapons"].AsArray())
            {
                int id = row["id"].AsInt();
                if (id <= 0)
                    continue;
                float armor = row.Has("base_armor") ? row["base_armor"].AsFloat(0f) : 0f;
                catalog._byId[id.ToString()] = armor;
                catalog._byId["weapon:" + id] = armor;
            }
            return catalog;
        }

        public float ArmorOf(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId))
                return 0f;
            return _byId.TryGetValue(weaponId, out float armor) ? armor : 0f;
        }
    }
}
