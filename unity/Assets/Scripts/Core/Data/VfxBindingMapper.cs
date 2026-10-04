using System.Collections.Generic;

using Dovus.Core.Shared;
namespace Dovus.Core.Data
{
    public static class VfxBindingMapper
    {
        public static bool TryParse(string json, out VfxBindingData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json))
                return false;
            JsonValue root = MiniJson.Parse(json);
            if (root.IsNull)
                return false;
            data = Parse(root);
            return true;
        }

        public static VfxBindingData Parse(JsonValue root)
        {
            var data = new VfxBindingData();
            JsonValue vb = root["vfx_binding"];

            foreach (KeyValuePair<string, JsonValue> kv in vb["element_colors"].AsObject())
            {
                string hex = kv.Value["primary"].AsString();
                if (!string.IsNullOrEmpty(hex))
                    data.ElementPrimaryHex[kv.Key] = hex;
            }

            foreach (KeyValuePair<string, JsonValue> kv in vb["trail_vfx"].AsObject())
            {
                string style = kv.Value["vfx_style"].AsString();
                data.TrailEntries.Add((kv.Key, style ?? string.Empty));
            }

            foreach (KeyValuePair<string, JsonValue> kv in vb["impact_vfx"].AsObject())
                data.ImpactStyleIds.Add(kv.Key);

            return data;
        }
    }
}
