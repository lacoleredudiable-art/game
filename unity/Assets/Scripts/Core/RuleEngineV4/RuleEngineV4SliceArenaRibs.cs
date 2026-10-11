using System;
using System.Collections.Generic;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Kaburga kemerleri: eksen boyunca eşit aralık, sabit tohumla ölçek/yaw sapması.
    /// Yürek kaburgası (heart_index) ek ölçek çarpanı alır.
    /// </summary>
    public sealed class RuleEngineV4SliceArenaRibs
    {
        public string Model { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibModel;
        public string Lod1Model { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibLod1Model;
        public int Count { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibCount;
        public float Scale { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibScale;
        public float SpacingM { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibSpacingM;
        public string Axis { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.AxisNorthToSouth;
        public float StartZM { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibStartZM;
        public int HeartIndex { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibHeartIndex;
        public float HeartScaleMult { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibHeartScaleMult;
        public float JitterScale { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibJitterScale;
        public float JitterRotDeg { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibJitterRotDeg;
        public int Seed { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibSeed;
        public bool Colliders { get; private set; }
        public float Lod1ScreenHeight { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibLod1ScreenHeight;
        public float CullScreenHeight { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibCullScreenHeight;

        public static RuleEngineV4SliceArenaRibs Default => new();

        public static RuleEngineV4SliceArenaRibs FromJson(JsonValue node)
        {
            var r = new RuleEngineV4SliceArenaRibs();
            if (node.IsNull)
                return r;
            r.Model = node["model"].AsString(r.Model);
            r.Lod1Model = node["lod1_model"].AsString(r.Lod1Model);
            r.Count = Math.Max(0, node["count"].AsInt(r.Count));
            r.Scale = node["scale"].AsFloat(r.Scale);
            r.SpacingM = node["spacing_m"].AsFloat(r.SpacingM);
            r.Axis = node["axis"].AsString(r.Axis);
            r.StartZM = node["start_z_m"].AsFloat(r.StartZM);
            r.HeartIndex = node["heart_index"].AsInt(r.HeartIndex);
            r.HeartScaleMult = node["heart_scale_mult"].AsFloat(r.HeartScaleMult);
            r.JitterScale = node["jitter_scale"].AsFloat(r.JitterScale);
            r.JitterRotDeg = node["jitter_rot_deg"].AsFloat(r.JitterRotDeg);
            r.Seed = node["seed"].AsInt(r.Seed);
            r.Colliders = node["colliders"].AsBool(false);
            r.Lod1ScreenHeight = node["lod1_screen_height"].AsFloat(r.Lod1ScreenHeight);
            r.CullScreenHeight = node["cull_screen_height"].AsFloat(r.CullScreenHeight);
            return r;
        }

        /// <summary>
        /// Aynı tohum → aynı dizi. Sapma simetrik: ölçek 1±jitter_scale, yaw ±jitter_rot_deg.
        /// Yürek kaburgasının ölçek sapması yalnız artı yönde: heart_scale_mult &gt; 1+jitter_scale iken en kalın o kalır.
        /// </summary>
        public IReadOnlyList<RuleEngineV4SliceArenaPlacement> Layout()
        {
            var poses = new List<RuleEngineV4SliceArenaPlacement>(Count);
            SeededRng rng = SeededRng.Seeded(Seed);
            float step = Axis == RuleEngineV4SliceArenaVisualsDefaults.AxisSouthToNorth ? SpacingM : -SpacingM;
            for (int i = 0; i < Count; i++)
            {
                bool heart = i == HeartIndex;
                float jitter = Symmetric(rng);
                float scaleJitter = 1f + (heart ? Math.Abs(jitter) : jitter) * JitterScale;
                float yaw = Symmetric(rng) * JitterRotDeg;
                float scale = Scale * scaleJitter * (heart ? HeartScaleMult : 1f);
                poses.Add(new RuleEngineV4SliceArenaPlacement(0f, StartZM + step * i, yaw, scale));
            }
            return poses;
        }

        static float Symmetric(SeededRng rng) => (float)(rng.NextDouble() * 2.0 - 1.0);
    }
}
