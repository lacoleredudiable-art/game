using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Presentation
{
    public readonly struct TrajectoryNode
    {
        public TrajectoryNode(
            string id, string name, string motionCurve, float speedMpsDefault, JsonValue raw)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            MotionCurve = motionCurve ?? string.Empty;
            SpeedMpsDefault = speedMpsDefault;
            Raw = raw ?? JsonValue.Null;
        }

        public string Id { get; }
        public string Name { get; }
        public string MotionCurve { get; }
        public float SpeedMpsDefault { get; }
        public JsonValue Raw { get; }

        public float GetFloat(string key, float fallback = 0f) => Raw[key].AsFloat(fallback);
        public bool GetBool(string key, bool fallback = false) => Raw[key].AsBool(fallback);
        public string GetString(string key, string fallback = "") => Raw[key].AsString(fallback);
    }
}
