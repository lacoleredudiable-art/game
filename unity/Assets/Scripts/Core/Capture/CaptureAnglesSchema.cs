using Dovus.Core.Shared;
using System;
using System.Collections.Generic;

namespace Dovus.Core.Capture
{
    /// <summary>tools/capture/angles.json okuma (MiniJson; PLAN 2B.16).</summary>
    public static class CaptureAnglesSchema
    {
        public static bool TryParse(string json, out CaptureAnglePreset[] presets, out string error)
        {
            presets = Array.Empty<CaptureAnglePreset>();
            error = null;
            JsonValue root = MiniJson.Parse(json);
            if (root.Kind != JsonKind.Object || !root.Has("angles"))
            {
                error = "angles.json: kök nesne ve 'angles' dizisi gerekli.";
                return false;
            }

            IReadOnlyList<JsonValue> arr = root["angles"].AsArray();
            if (arr.Count == 0)
            {
                error = "angles.json: en az bir önayar gerekli.";
                return false;
            }

            var list = new List<CaptureAnglePreset>(arr.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < arr.Count; i++)
            {
                JsonValue entry = arr[i];
                if (entry.Kind != JsonKind.Object)
                {
                    error = $"angles[{i}]: nesne bekleniyor.";
                    return false;
                }

                string name = entry["name"].AsString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    error = $"angles[{i}]: name boş.";
                    return false;
                }

                if (!seen.Add(name))
                {
                    error = "angles.json: name benzersiz olmalı: " + name;
                    return false;
                }

                if (!TryReadVec3(entry["position"], out float px, out float py, out float pz))
                {
                    error = $"angles[{i}] ({name}): position [x,y,z] gerekli.";
                    return false;
                }

                if (!TryReadVec3(entry["euler"], out float ex, out float ey, out float ez))
                {
                    error = $"angles[{i}] ({name}): euler [x,y,z] gerekli.";
                    return false;
                }

                float fov = entry["fov"].AsFloat();
                int width = entry["width"].AsInt();
                int height = entry["height"].AsInt();
                if (width <= 0 || height <= 0)
                {
                    error = $"angles[{i}] ({name}): width/height > 0 gerekli.";
                    return false;
                }

                list.Add(new CaptureAnglePreset(name, px, py, pz, ex, ey, ez, fov, width, height));
            }

            presets = list.ToArray();
            return true;
        }

        static bool TryReadVec3(JsonValue value, out float x, out float y, out float z)
        {
            x = y = z = 0f;
            IReadOnlyList<JsonValue> a = value.AsArray();
            if (a.Count < 3)
                return false;
            x = a[0].AsFloat();
            y = a[1].AsFloat();
            z = a[2].AsFloat();
            return true;
        }
    }
}
