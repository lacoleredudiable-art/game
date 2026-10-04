using System;
using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Passives
{
    /// <summary>
    /// passive_slot_system silah uyumu şartı yazmıyorsa rün pasifi silahtan bağımsız açılır.
    /// </summary>
    public static class PassiveSlotPolicy
    {
        public static bool RequiresWeaponCompatibility(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;
            return RequiresWeaponCompatibility(ElementSystemDocument.Parse(json).Root);
        }

        public static bool RequiresWeaponCompatibility(ElementSystemDocument doc)
        {
            if (doc == null)
                return false;
            return RequiresWeaponCompatibility(doc.Root);
        }

        public static bool RequiresWeaponCompatibility(JsonValue root)
        {
            if (root.IsNull)
                return false;
            return MentionsWeaponGate(root["passive_slot_system"]);
        }

        public static bool ShouldArm(bool runeIsSlottedPassive, bool weaponPassiveEnabled, bool policyRequiresWeapon)
        {
            if (!runeIsSlottedPassive)
                return false;
            if (policyRequiresWeapon && !weaponPassiveEnabled)
                return false;
            return true;
        }

        static bool MentionsWeaponGate(JsonValue node)
        {
            if (node.IsNull)
                return false;
            if (node.Kind == JsonKind.String)
                return LooksLikeWeaponGate(node.AsString());
            if (node.Kind == JsonKind.Array)
            {
                IReadOnlyList<JsonValue> items = node.AsArray();
                for (int i = 0; i < items.Count; i++)
                {
                    if (MentionsWeaponGate(items[i]))
                        return true;
                }
                return false;
            }
            if (node.Kind != JsonKind.Object)
                return false;
            foreach (KeyValuePair<string, JsonValue> pair in node.AsObject())
            {
                if (LooksLikeWeaponGate(pair.Key) || MentionsWeaponGate(pair.Value))
                    return true;
            }
            return false;
        }

        static bool LooksLikeWeaponGate(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            if (text.IndexOf("compatible_verbs", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (text.IndexOf("weapon compat", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (text.IndexOf("silah uyum", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }
    }
}
