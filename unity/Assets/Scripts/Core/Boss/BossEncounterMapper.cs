using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using System.Collections.Generic;

using Dovus.Core.Shared;
namespace Dovus.Core.Boss
{
    public static class BossEncounterMapper
    {
        public static bool TryParseDocument(string json, out BossEncounterDocument document)
        {
            document = null;
            if (string.IsNullOrWhiteSpace(json))
                return false;
            JsonValue root = MiniJson.Parse(json);
            if (root.IsNull)
                return false;
            document = new BossEncounterDocument(root);
            return true;
        }

        public static TargetingConfig LoadTargeting(string json)
        {
            if (!TryParseDocument(json, out BossEncounterDocument doc))
                return new TargetingConfig();
            return TargetingConfig.FromJson(doc.Root);
        }

        public static TargetingConfig LoadTargeting(BossEncounterDocument document)
        {
            if (document == null || !document.IsValid)
                return new TargetingConfig();
            return TargetingConfig.FromJson(document.Root);
        }

        /// <summary>Volley kind'lı saldırı satırını BossTuning'e yazar.</summary>
        public static bool ApplyVolley(BossTuning tuning, string json)
        {
            if (tuning == null || !TryParseDocument(json, out BossEncounterDocument doc))
                return false;
            return ApplyVolley(tuning, doc);
        }

        public static bool ApplyVolley(BossTuning tuning, BossEncounterDocument document)
        {
            if (tuning == null || document == null || !document.IsValid)
                return false;
            JsonValue root = document.Root;
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

        public static BossAttackKind[] LoadPhaseAttackKinds(BossEncounterDocument document, int phase)
        {
            if (document == null || !document.IsValid)
                return System.Array.Empty<BossAttackKind>();
            JsonValue root = document.Root;
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

        public static BossAttackKind[] LoadPhaseAttackKinds(string json, int phase)
        {
            if (!TryParseDocument(json, out BossEncounterDocument doc))
                return System.Array.Empty<BossAttackKind>();
            return LoadPhaseAttackKinds(doc, phase);
        }

        public static bool TryLoadAttack(BossEncounterDocument document, string attackId, out BossAttackEntry entry)
        {
            entry = null;
            if (document == null || !document.IsValid)
                return false;
            if (!TryFindAttack(document.Root, attackId, out JsonValue a))
                return false;
            entry = ParseAttackEntry(a);
            return entry != null;
        }

        public static bool TryLoadAttack(string json, string attackId, out BossAttackEntry entry)
        {
            entry = null;
            if (!TryParseDocument(json, out BossEncounterDocument doc))
                return false;
            return TryLoadAttack(doc, attackId, out entry);
        }

        public static IReadOnlyList<BossAttackEntry> LoadAttacks(BossEncounterDocument document)
        {
            var list = new List<BossAttackEntry>();
            if (document == null || !document.IsValid)
                return list;
            foreach (JsonValue a in document.Root["attacks"].AsArray())
            {
                BossAttackEntry e = ParseAttackEntry(a);
                if (e != null)
                    list.Add(e);
            }
            return list;
        }

        public static IReadOnlyList<BossAttackEntry> LoadAttacks(string json)
        {
            if (!TryParseDocument(json, out BossEncounterDocument doc))
                return System.Array.Empty<BossAttackEntry>();
            return LoadAttacks(doc);
        }

        public static bool TryParseHud(string json, out BossHudSnapshot snapshot)
        {
            snapshot = null;
            if (!TryParseDocument(json, out BossEncounterDocument doc))
                return false;
            JsonValue root = doc.Root;
            snapshot = new BossHudSnapshot();
            snapshot.Name = root["name"].AsString(snapshot.Name);
            snapshot.Subtitle = root["subtitle"].AsString(string.Empty);
            foreach (JsonValue p in root["vitals"]["phases"].AsArray())
            {
                IReadOnlyList<JsonValue> range = p["range"].AsArray();
                float upper = range.Count > 0 ? range[0].AsFloat(100f) / 100f : 1f;
                snapshot.Phases.Add((p["phase"].AsInt(), p["name"].AsString(string.Empty), upper));
            }
            foreach (JsonValue a in root["attacks"].AsArray())
            {
                string id = a["id"].AsString(string.Empty);
                string display = a["name"].AsString(string.Empty);
                snapshot.AttackNamesById[id] = display;
                BossAttackKind? kind = ResolveKind(a);
                if (kind.HasValue && !string.IsNullOrEmpty(display))
                    snapshot.AttackNamesByKind[kind.Value] = display;
            }
            return true;
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
    }
}
