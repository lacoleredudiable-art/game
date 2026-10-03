using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Boss
{
    /// <summary>
    /// Karşılaşma tasarım sayıları <c>Resources/Bosses/*.json</c> (BossHudData ile aynı dosya).
    /// "targeting", volley sayıları, faz saldırı kind listesi ve saldırı başına ek alanlar.
    /// </summary>
    public static class BossEncounterData
    {
        public readonly struct BossOnHitStatus
        {
            public BossOnHitStatus(string id, float durationSec)
            {
                Id = id ?? string.Empty;
                DurationSec = durationSec;
            }

            public string Id { get; }
            public float DurationSec { get; }
            public bool IsValid => !string.IsNullOrEmpty(Id);
        }

        public readonly struct BossFieldSpec
        {
            public BossFieldSpec(
                float radiusM,
                float lifeSec,
                int maxCount,
                string status,
                float refreshSec,
                float minCenterDistM)
            {
                RadiusM = radiusM;
                LifeSec = lifeSec;
                MaxCount = maxCount;
                Status = status ?? string.Empty;
                RefreshSec = refreshSec;
                MinCenterDistM = minCenterDistM;
            }

            public float RadiusM { get; }
            public float LifeSec { get; }
            public int MaxCount { get; }
            public string Status { get; }
            public float RefreshSec { get; }
            public float MinCenterDistM { get; }
        }

        public readonly struct BossLeapSpec
        {
            public BossLeapSpec(
                float minRangeM,
                float maxRangeM,
                float airSec,
                float landRadiusM,
                float wallMarginM)
            {
                MinRangeM = minRangeM;
                MaxRangeM = maxRangeM;
                AirSec = airSec;
                LandRadiusM = landRadiusM;
                WallMarginM = wallMarginM;
            }

            public float MinRangeM { get; }
            public float MaxRangeM { get; }
            public float AirSec { get; }
            public float LandRadiusM { get; }
            public float WallMarginM { get; }
        }

        public sealed class BossAttackEntry
        {
            public string Id = string.Empty;
            public BossAttackKind Kind;
            public IReadOnlyList<string> Mechanics = System.Array.Empty<string>();
            public BossOnHitStatus OnHitStatus;
            public BossFieldSpec? Field;
            public BossLeapSpec? Leap;
        }

        public static TargetingConfig LoadTargeting(string resourcePath = "Bosses/karadul")
        {
            if (!TryLoadRoot(resourcePath, out JsonValue root))
                return new TargetingConfig();
            try
            {
                return TargetingConfig.FromJson(root);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BossEncounterData] {resourcePath} okunamadı: {e.Message}");
                return new TargetingConfig();
            }
        }

        /// <summary>Volley kind'lı saldırı satırını BossTuning'e yazar. Dosya yoksa varsayılan kalır.</summary>
        public static bool ApplyVolley(BossTuning tuning, string resourcePath = "Bosses/karadul")
        {
            if (tuning == null || !TryLoadRoot(resourcePath, out JsonValue root))
                return false;
            try
            {
                foreach (JsonValue a in root["attacks"].AsArray())
                {
                    if (ResolveKind(a) != BossAttackKind.Volley)
                        continue;
                    tuning.VolleyWindupMs = a["windup_ms"].AsInt(tuning.VolleyWindupMs);
                    tuning.VolleyCount = a["count"].AsInt(tuning.VolleyCount);
                    tuning.VolleyCountEnraged = a["count_enraged"].AsInt(tuning.VolleyCountEnraged);
                    tuning.VolleySpreadDeg = a["spread_deg"].AsFloat(tuning.VolleySpreadDeg);
                    tuning.VolleySpeedMps = a["speed_mps"].AsFloat(tuning.VolleySpeedMps);
                    tuning.VolleyDamage = a["damage"].AsInt(tuning.VolleyDamage);
                    tuning.VolleyRadiusM = a["radius_m"].AsFloat(tuning.VolleyRadiusM);
                    tuning.VolleyLifeSec = a["life_sec"].AsFloat(tuning.VolleyLifeSec);
                    return true;
                }
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BossEncounterData] {resourcePath} volley okunamadı: {e.Message}");
                return false;
            }
        }

        /// <summary><c>kind</c> yoksa karadul uyumu için <c>id</c> slam / fire_cone / volley eşlenir.</summary>
        public static BossAttackKind? ResolveKind(JsonValue attack)
        {
            if (attack == null || attack.IsNull)
                return null;
            string kindToken = attack["kind"].AsString(null);
            if (!string.IsNullOrEmpty(kindToken) && TryParseKindToken(kindToken, out BossAttackKind fromKind))
                return fromKind;
            string id = attack["id"].AsString(null);
            if (!string.IsNullOrEmpty(id) && TryParseKindToken(id, out BossAttackKind fromId))
                return fromId;
            return null;
        }

        public static bool TryParseKindToken(string token, out BossAttackKind kind)
        {
            switch (token)
            {
                case "slam":
                    kind = BossAttackKind.Slam;
                    return true;
                case "fire_cone":
                    kind = BossAttackKind.FireCone;
                    return true;
                case "volley":
                    kind = BossAttackKind.Volley;
                    return true;
                case "web_field":
                    kind = BossAttackKind.WebField;
                    return true;
                case "pounce":
                    kind = BossAttackKind.Pounce;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        public static BossAttackKind[] LoadPhaseAttackKinds(JsonValue root, int phase)
        {
            if (root == null || root.IsNull)
                return System.Array.Empty<BossAttackKind>();
            foreach (JsonValue p in root["vitals"]["phases"].AsArray())
            {
                if (p["phase"].AsInt() != phase)
                    continue;
                IReadOnlyList<JsonValue> ids = p["attacks"].AsArray();
                var kinds = new List<BossAttackKind>(ids.Count);
                for (int i = 0; i < ids.Count; i++)
                {
                    string id = ids[i].AsString(string.Empty);
                    if (TryFindAttack(root, id, out JsonValue atk))
                    {
                        BossAttackKind? k = ResolveKind(atk);
                        if (k.HasValue)
                            kinds.Add(k.Value);
                    }
                }
                return kinds.ToArray();
            }
            return System.Array.Empty<BossAttackKind>();
        }

        public static BossAttackKind[] LoadPhaseAttackKinds(string resourcePath, int phase)
        {
            if (!TryLoadRoot(resourcePath, out JsonValue root))
                return System.Array.Empty<BossAttackKind>();
            return LoadPhaseAttackKinds(root, phase);
        }

        public static bool TryLoadAttack(JsonValue root, string attackId, out BossAttackEntry entry)
        {
            entry = null;
            if (!TryFindAttack(root, attackId, out JsonValue a))
                return false;
            entry = ParseAttackEntry(a);
            return entry != null;
        }

        public static bool TryLoadAttack(string resourcePath, string attackId, out BossAttackEntry entry)
        {
            entry = null;
            if (!TryLoadRoot(resourcePath, out JsonValue root))
                return false;
            return TryLoadAttack(root, attackId, out entry);
        }

        public static IReadOnlyList<BossAttackEntry> LoadAttacks(JsonValue root)
        {
            var list = new List<BossAttackEntry>();
            if (root == null || root.IsNull)
                return list;
            foreach (JsonValue a in root["attacks"].AsArray())
            {
                BossAttackEntry e = ParseAttackEntry(a);
                if (e != null)
                    list.Add(e);
            }
            return list;
        }

        public static IReadOnlyList<BossAttackEntry> LoadAttacks(string resourcePath = "Bosses/karadul")
        {
            if (!TryLoadRoot(resourcePath, out JsonValue root))
                return System.Array.Empty<BossAttackEntry>();
            return LoadAttacks(root);
        }

        static BossAttackEntry ParseAttackEntry(JsonValue a)
        {
            if (a == null || a.IsNull)
                return null;
            BossAttackKind? kind = ResolveKind(a);
            if (!kind.HasValue)
                return null;
            var entry = new BossAttackEntry
            {
                Id = a["id"].AsString(string.Empty),
                Kind = kind.Value,
                Mechanics = ReadStringList(a["mechanics"])
            };
            JsonValue hit = a["on_hit_status"];
            if (!hit.IsNull)
            {
                entry.OnHitStatus = new BossOnHitStatus(
                    hit["id"].AsString(string.Empty),
                    hit["duration_sec"].AsFloat(0f));
            }
            JsonValue field = a["field"];
            if (!field.IsNull)
            {
                entry.Field = new BossFieldSpec(
                    field["radius_m"].AsFloat(0f),
                    field["life_sec"].AsFloat(0f),
                    field["max_count"].AsInt(0),
                    field["status"].AsString(string.Empty),
                    field["refresh_sec"].AsFloat(0f),
                    field["min_center_dist_m"].AsFloat(0f));
            }
            JsonValue leap = a["leap"];
            if (!leap.IsNull)
            {
                entry.Leap = new BossLeapSpec(
                    leap["min_range_m"].AsFloat(0f),
                    leap["max_range_m"].AsFloat(0f),
                    leap["air_sec"].AsFloat(0f),
                    leap["land_radius_m"].AsFloat(0f),
                    leap["wall_margin_m"].AsFloat(0f));
            }
            return entry;
        }

        static bool TryFindAttack(JsonValue root, string attackId, out JsonValue attack)
        {
            attack = JsonValue.Null;
            if (string.IsNullOrEmpty(attackId) || root == null || root.IsNull)
                return false;
            foreach (JsonValue a in root["attacks"].AsArray())
            {
                if (a["id"].AsString() == attackId)
                {
                    attack = a;
                    return true;
                }
            }
            return false;
        }

        static string[] ReadStringList(JsonValue arr)
        {
            IReadOnlyList<JsonValue> items = arr.AsArray();
            if (items.Count == 0)
                return System.Array.Empty<string>();
            var list = new List<string>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Kind == JsonKind.String)
                    list.Add(items[i].AsString());
            }
            return list.ToArray();
        }

        static bool TryLoadRoot(string resourcePath, out JsonValue root)
        {
            root = JsonValue.Null;
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return false;
            root = MiniJson.Parse(asset.text);
            return !root.IsNull;
        }
    }
}
