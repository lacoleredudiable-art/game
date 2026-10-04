using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Presentation
{
    public readonly struct HitboxNode
    {
        public HitboxNode(string id, string name, string shape, JsonValue raw)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Shape = shape ?? string.Empty;
            Raw = raw ?? JsonValue.Null;
        }

        public string Id { get; }
        public string Name { get; }
        public string Shape { get; }
        public JsonValue Raw { get; }

        public float GetFloat(string key, float fallback = 0f) => Raw[key].AsFloat(fallback);
        public bool GetBool(string key, bool fallback = false) => Raw[key].AsBool(fallback);
        public string GetString(string key, string fallback = "") => Raw[key].AsString(fallback);
    }
}
