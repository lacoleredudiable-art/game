using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

namespace Dovus.Core.Presentation
{
    /// <summary>
    /// docs/prezentasyon-katmani.json — trajectory / hitbox / animasyon / uyumluluk matrisi.
    /// element-sistemi.json'dan bağımsız (system.binding: false). SkillMotor'a karışmaz.
    /// </summary>
    public sealed class PresentationCatalog
    {
        readonly Dictionary<string, TrajectoryNode> _trajectories = new(StringComparer.Ordinal);
        readonly Dictionary<string, HitboxNode> _hitboxes = new(StringComparer.Ordinal);
        readonly Dictionary<string, AnimationFrameNode> _animations = new(StringComparer.Ordinal);
        readonly Dictionary<string, Dictionary<string, CompatibilityResult>> _matrix =
            new(StringComparer.Ordinal);

        PresentationCatalog() { }

        public IReadOnlyDictionary<string, TrajectoryNode> Trajectories => _trajectories;
        public IReadOnlyDictionary<string, HitboxNode> Hitboxes => _hitboxes;
        public IReadOnlyDictionary<string, AnimationFrameNode> Animations => _animations;

        public static PresentationCatalog FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON boş.", nameof(json));

            JsonValue root = MiniJson.Parse(json);
            var catalog = new PresentationCatalog();
            ParseTrajectories(root, catalog._trajectories);
            ParseHitboxes(root, catalog._hitboxes);
            ParseAnimations(root, catalog._animations);
            ParseMatrix(root, catalog._matrix);
            return catalog;
        }

        /// <summary>
        /// trajectory_hitbox_matrix satırı. Bilinmeyen çift → Incompatible.
        /// </summary>
        public CompatibilityResult TryGetCompatibility(string trajectoryId, string hitboxId)
        {
            if (string.IsNullOrEmpty(trajectoryId) || string.IsNullOrEmpty(hitboxId))
                return CompatibilityResult.Incompatible;

            if (!_matrix.TryGetValue(trajectoryId, out Dictionary<string, CompatibilityResult>? row))
                return CompatibilityResult.Incompatible;

            return row.TryGetValue(hitboxId, out CompatibilityResult result)
                ? result
                : CompatibilityResult.Incompatible;
        }

        public bool TryGetTrajectory(string id, out TrajectoryNode node) =>
            _trajectories.TryGetValue(id ?? string.Empty, out node);

        public bool TryGetHitbox(string id, out HitboxNode node) =>
            _hitboxes.TryGetValue(id ?? string.Empty, out node);

        public bool TryGetAnimation(string id, out AnimationFrameNode node) =>
            _animations.TryGetValue(id ?? string.Empty, out node);

        static void ParseTrajectories(JsonValue root, Dictionary<string, TrajectoryNode> dest)
        {
            foreach (KeyValuePair<string, JsonValue> kv in
                     root["trajectory_library"]["trajectories"].AsObject())
            {
                JsonValue row = kv.Value;
                string id = FirstNonEmpty(row["id"].AsString(), kv.Key);
                if (string.IsNullOrEmpty(id))
                    continue;

                dest[id] = new TrajectoryNode(
                    id,
                    row["name"].AsString(),
                    row["motion_curve"].AsString(),
                    row["speed_mps_default"].AsFloat(),
                    row);
            }
        }

        static void ParseHitboxes(JsonValue root, Dictionary<string, HitboxNode> dest)
        {
            foreach (KeyValuePair<string, JsonValue> kv in
                     root["hitbox_library"]["hitboxes"].AsObject())
            {
                JsonValue row = kv.Value;
                string id = FirstNonEmpty(row["id"].AsString(), kv.Key);
                if (string.IsNullOrEmpty(id))
                    continue;

                dest[id] = new HitboxNode(
                    id,
                    row["name"].AsString(),
                    row["shape"].AsString(),
                    row);
            }
        }

        static void ParseAnimations(JsonValue root, Dictionary<string, AnimationFrameNode> dest)
        {
            foreach (KeyValuePair<string, JsonValue> kv in
                     root["animation_library"]["animations"].AsObject())
            {
                JsonValue row = kv.Value;
                string id = FirstNonEmpty(row["id"].AsString(), kv.Key);
                if (string.IsNullOrEmpty(id))
                    continue;

                dest[id] = new AnimationFrameNode(
                    id,
                    row["total_frames"].AsInt(),
                    row["total_duration_ms"].AsInt(),
                    row["startup_frames"].AsInt(),
                    ParseIntPair(row["active_frames"]),
                    row["recovery_frames"].AsInt(),
                    ParseOptionalIntPair(row["cancel_window"]),
                    row["damage_applied_at_frame"],
                    row.Has("spawn_vfx_at_frame") ? row["spawn_vfx_at_frame"].AsInt() : (int?)null,
                    row["animator_state"].AsString());
            }
        }

        static void ParseMatrix(
            JsonValue root,
            Dictionary<string, Dictionary<string, CompatibilityResult>> dest)
        {
            foreach (KeyValuePair<string, JsonValue> trajRow in
                     root["trajectory_hitbox_matrix"]["matrix"].AsObject())
            {
                var row = new Dictionary<string, CompatibilityResult>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, JsonValue> cell in trajRow.Value.AsObject())
                    row[cell.Key] = ParseCompatibilitySymbol(cell.Value.AsString());
                dest[trajRow.Key] = row;
            }
        }

        static CompatibilityResult ParseCompatibilitySymbol(string symbol)
        {
            if (symbol == "✓") return CompatibilityResult.Compatible;
            if (symbol == "⚠") return CompatibilityResult.Special;
            return CompatibilityResult.Incompatible;
        }

        static int[] ParseIntPair(JsonValue v)
        {
            IReadOnlyList<JsonValue> arr = v.AsArray();
            if (arr.Count < 2)
                return new[] { 0, 0 };
            return new[] { arr[0].AsInt(), arr[1].AsInt() };
        }

        static int[]? ParseOptionalIntPair(JsonValue v)
        {
            if (v.IsNull || v.Kind != JsonKind.Array)
                return null;
            IReadOnlyList<JsonValue> arr = v.AsArray();
            if (arr.Count < 2)
                return null;
            return new[] { arr[0].AsInt(), arr[1].AsInt() };
        }

        static string FirstNonEmpty(string a, string b) =>
            !string.IsNullOrEmpty(a) ? a : (b ?? string.Empty);
    }

    /// <summary>trajectory_hitbox_matrix hücre sembolü.</summary>
    public enum CompatibilityResult
    {
        Compatible,
        Special,
        Incompatible
    }

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

    public readonly struct AnimationFrameNode
    {
        public AnimationFrameNode(
            string id,
            int totalFrames,
            int totalDurationMs,
            int startupFrames,
            int[] activeFrames,
            int recoveryFrames,
            int[]? cancelWindow,
            JsonValue damageAppliedAtFrame,
            int? spawnVfxAtFrame,
            string animatorState)
        {
            Id = id ?? string.Empty;
            TotalFrames = totalFrames;
            TotalDurationMs = totalDurationMs;
            StartupFrames = startupFrames;
            ActiveFrames = activeFrames ?? new[] { 0, 0 };
            RecoveryFrames = recoveryFrames;
            CancelWindow = cancelWindow;
            DamageAppliedAtFrame = damageAppliedAtFrame ?? JsonValue.Null;
            SpawnVfxAtFrame = spawnVfxAtFrame;
            AnimatorState = animatorState ?? string.Empty;
        }

        public string Id { get; }
        public int TotalFrames { get; }
        public int TotalDurationMs { get; }
        public int StartupFrames { get; }
        /// <summary>[start, end] dahil aralık.</summary>
        public int[] ActiveFrames { get; }
        public int RecoveryFrames { get; }
        /// <summary>Yoksa null (channel/summon).</summary>
        public int[]? CancelWindow { get; }
        /// <summary>Sayı, "every_tick" veya null — tip zorlanmaz.</summary>
        public JsonValue DamageAppliedAtFrame { get; }
        public int? SpawnVfxAtFrame { get; }
        public string AnimatorState { get; }
    }
}
