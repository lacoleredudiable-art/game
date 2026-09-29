using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Grammar;

namespace Dovus.Core.Motion
{
    public enum MotionAvailability
    {
        Missing = 0,
        Pending = 1,
        Ready = 2
    }

    /// <summary>Bir skill'in kalıba bağı. Etiketler mekanik değildir; yalnız saklanır.</summary>
    public sealed class MotionBinding
    {
        public MotionBinding(
            MotionTemplate template,
            bool implemented,
            IReadOnlyList<string> tags,
            float sinirThreshold)
        {
            Template = template;
            Implemented = implemented;
            Tags = tags ?? Array.Empty<string>();
            SinirThreshold = sinirThreshold;
        }

        public MotionTemplate Template { get; }
        public bool Implemented { get; }
        public IReadOnlyList<string> Tags { get; }
        public float SinirThreshold { get; }

        public bool HasTag(string tag)
        {
            for (int i = 0; i < Tags.Count; i++)
                if (string.Equals(Tags[i], tag, StringComparison.Ordinal))
                    return true;
            return false;
        }
    }

    public sealed class MotionHitSpec
    {
        public MotionHitSpec(
            string shape,
            string anchor,
            float lengthM,
            float radiusM,
            float at,
            float share,
            string payload,
            float everySec)
        {
            Shape = shape ?? "capsule";
            Anchor = anchor ?? "forward";
            LengthM = lengthM;
            RadiusM = radiusM;
            At = at;
            Share = share;
            Payload = payload ?? "damage";
            EverySec = everySec;
        }

        public string Shape { get; }
        public string Anchor { get; }
        public float LengthM { get; }
        public float RadiusM { get; }
        public float At { get; }
        public float Share { get; }
        public string Payload { get; }
        public float EverySec { get; }
    }

    public sealed class MotionPhase
    {
        public MotionPhase(
            string name,
            string motion,
            float durationSec,
            string facing,
            string homing,
            string gate,
            float maxHoldSec,
            float distanceM,
            float forwardM,
            float side,
            float sideM,
            float heightM,
            float yawDeg,
            float gapM,
            float overshootM,
            float shotM,
            float driftM,
            float walkMps,
            float behindM,
            float snapAt,
            float[] curve,
            MotionHitSpec hit)
        {
            Name = name ?? string.Empty;
            Motion = motion ?? "hold";
            DurationSec = durationSec;
            Facing = string.IsNullOrEmpty(facing) ? "target" : facing;
            Homing = string.IsNullOrEmpty(homing) ? "none" : homing;
            Gate = gate ?? string.Empty;
            MaxHoldSec = maxHoldSec;
            DistanceM = distanceM;
            ForwardM = forwardM;
            Side = side;
            SideM = sideM;
            HeightM = heightM;
            YawDeg = yawDeg;
            GapM = gapM;
            OvershootM = overshootM;
            ShotM = shotM;
            DriftM = driftM;
            WalkMps = walkMps;
            BehindM = behindM;
            SnapAt = snapAt;
            Curve = curve;
            Hit = hit;
        }

        public string Name { get; }
        public string Motion { get; }
        public float DurationSec { get; }
        public string Facing { get; }
        public string Homing { get; }
        public string Gate { get; }
        public float MaxHoldSec { get; }
        public float DistanceM { get; }
        public float ForwardM { get; }
        public float Side { get; }
        public float SideM { get; }
        public float HeightM { get; }
        public float YawDeg { get; }
        public float GapM { get; }
        public float OvershootM { get; }
        public float ShotM { get; }
        public float DriftM { get; }
        public float WalkMps { get; }
        public float BehindM { get; }
        public float SnapAt { get; }
        public float[] Curve { get; }
        public MotionHitSpec Hit { get; }
    }

    public sealed class MotionTemplate
    {
        public MotionTemplate(
            string id,
            string name,
            int familyId,
            string familyName,
            bool implemented,
            IReadOnlyList<MotionPhase> phases)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            FamilyId = familyId;
            FamilyName = familyName ?? string.Empty;
            Implemented = implemented;
            Phases = phases ?? Array.Empty<MotionPhase>();
        }

        public string Id { get; }
        public string Name { get; }
        public int FamilyId { get; }
        public string FamilyName { get; }
        public bool Implemented { get; }
        public IReadOnlyList<MotionPhase> Phases { get; }
    }

    /// <summary>
    /// 144 komboyu hareket kalıbına bağlar. Sayı yoksa bir kez uyarır ve yedek kullanır.
    /// Ailesi bitmemiş kalıp oynatılmaz; çağıran eski davranışı sürdürür.
    /// </summary>
    public sealed class MotionTemplateCatalog
    {
        public const string TagPortal = "portal";
        public const string TagSinir = "sinir_modu";
        public const string TagTakim = "takim_kombosu";
        public const string TagSilah = "silah_kesme";

        readonly Dictionary<string, MotionBinding> _bySkill = new(StringComparer.Ordinal);
        readonly Dictionary<string, MotionTemplate> _byId = new(StringComparer.Ordinal);
        readonly List<MotionTemplate> _templates = new();

        MotionTemplateCatalog(MotionFallbacks fallbacks)
        {
            Fallbacks = fallbacks;
        }

        public MotionFallbacks Fallbacks { get; }
        public int SkillCount => _bySkill.Count;
        public int TemplateCount => _templates.Count;
        public int FamilyCount { get; private set; }
        public IReadOnlyList<MotionTemplate> Templates => _templates;

        public static MotionTemplateCatalog Empty { get; } =
            new MotionTemplateCatalog(MotionFallbacks.Coded);

        public static MotionTemplateCatalog FromJson(string json) =>
            FromJsonRoot(MiniJson.Parse(json));

        public static MotionTemplateCatalog FromJsonRoot(JsonValue root)
        {
            JsonValue fb = root["fallbacks"];
            var fallbacks = new MotionFallbacks(
                Need(fb, "phase_sec", MotionFallbacks.Coded.PhaseSec, "motion.fallback.phase_sec"),
                Need(fb, "step_m", MotionFallbacks.Coded.StepM, "motion.fallback.step_m"),
                Need(fb, "hit_length_m", MotionFallbacks.Coded.HitLengthM, "motion.fallback.hit_length_m"),
                Need(fb, "hit_radius_m", MotionFallbacks.Coded.HitRadiusM, "motion.fallback.hit_radius_m"),
                Need(fb, "max_hold_sec", MotionFallbacks.Coded.MaxHoldSec, "motion.fallback.max_hold_sec"),
                Need(fb, "walk_mps", MotionFallbacks.Coded.WalkMps, "motion.fallback.walk_mps"),
                Need(fb, "height_m", MotionFallbacks.Coded.HeightM, "motion.fallback.height_m"),
                Need(fb, "gap_m", MotionFallbacks.Coded.GapM, "motion.fallback.gap_m"));

            var catalog = new MotionTemplateCatalog(fallbacks);
            var families = new Dictionary<int, (string Name, bool Implemented)>();
            foreach (JsonValue row in root["families"].AsArray())
            {
                int id = row["id"].AsInt(0);
                if (id <= 0)
                    continue;
                families[id] = (row["name"].AsString(), row["implemented"].AsBool(false));
            }
            catalog.FamilyCount = families.Count;

            foreach (JsonValue row in root["templates"].AsArray())
            {
                int familyId = row["family"].AsInt(0);
                families.TryGetValue(familyId, out var family);
                string templateId = row["id"].AsString();
                var phases = new List<MotionPhase>();
                foreach (JsonValue phase in row["phases"].AsArray())
                    phases.Add(ParsePhase(templateId, phase, fallbacks));

                var template = new MotionTemplate(
                    templateId,
                    row["name"].AsString(),
                    familyId,
                    family.Name,
                    family.Implemented,
                    phases);
                catalog._templates.Add(template);
                if (!string.IsNullOrEmpty(templateId))
                    catalog._byId[templateId] = template;

                foreach (JsonValue combo in row["combos"].AsArray())
                {
                    string skillId = combo["id"].AsString();
                    if (string.IsNullOrEmpty(skillId))
                        continue;
                    var tags = new List<string>();
                    foreach (JsonValue tag in combo["tags"].AsArray())
                    {
                        string text = tag.AsString();
                        if (!string.IsNullOrEmpty(text))
                            tags.Add(text);
                    }
                    float sinir = combo.Has("sinir") ? combo["sinir"].AsFloat(0f) : 0f;
                    catalog._bySkill[skillId] = new MotionBinding(template, family.Implemented, tags, sinir);
                }
            }

            return catalog;
        }

        public bool TryGet(string skillId, out MotionBinding binding)
        {
            if (!string.IsNullOrEmpty(skillId) && _bySkill.TryGetValue(skillId, out MotionBinding found))
            {
                binding = found;
                return true;
            }

            binding = null!;
            return false;
        }

        public bool TryGetTemplate(string templateId, out MotionTemplate template) =>
            _byId.TryGetValue(templateId ?? string.Empty, out template);

        /// <summary>
        /// Oynatılacak kalıp. Aile bitmemişse false döner, bir kez uyarır; çağıran eski yolu kullanır.
        /// </summary>
        public bool TryPlay(string skillId, out MotionTemplate template)
        {
            template = null;
            if (!TryGet(skillId, out MotionBinding binding))
            {
                DesignWarnings.Once(
                    "motion.missing." + skillId,
                    "Hareket kalıbı yok: " + skillId + ". Eski davranış sürüyor.");
                return false;
            }

            if (!binding.Implemented)
            {
                DesignWarnings.Once(
                    "motion.pending." + binding.Template.FamilyId.ToString(CultureInfo.InvariantCulture),
                    "Hareket kalıbı bekliyor: " + binding.Template.FamilyName
                    + ". Eski davranış sürüyor.");
                return false;
            }

            template = binding.Template;
            return template.Phases.Count > 0;
        }

        public int CountImplementedFamilies()
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < _templates.Count; i++)
                if (_templates[i].Implemented)
                    seen.Add(_templates[i].FamilyId);
            return seen.Count;
        }

        public int CountReadySkills()
        {
            int n = 0;
            foreach (KeyValuePair<string, MotionBinding> kv in _bySkill)
                if (kv.Value.Implemented)
                    n++;
            return n;
        }

        static MotionPhase ParsePhase(string templateId, JsonValue row, MotionFallbacks fb)
        {
            string name = row["name"].AsString("faz");
            string warn = "motion.phase." + templateId + "." + name + ".sec";
            float sec = row.Has("sec")
                ? row["sec"].AsFloat(fb.PhaseSec)
                : Warn(warn, fb.PhaseSec, "faz süresi");

            float[] curve = null;
            if (row.Has("curve"))
            {
                var list = new List<float>();
                foreach (JsonValue sample in row["curve"].AsArray())
                    list.Add(sample.AsFloat(0f));
                if (list.Count > 0)
                    curve = list.ToArray();
            }

            MotionHitSpec hit = null;
            if (row.Has("hit") && row["hit"].Kind == JsonKind.Object)
            {
                JsonValue h = row["hit"];
                string hitWarn = "motion.hit." + templateId + "." + name;
                float length = h.Has("length_m")
                    ? h["length_m"].AsFloat(fb.HitLengthM)
                    : Warn(hitWarn + ".length_m", fb.HitLengthM, "vuruş boyu");
                float radius = h.Has("radius_m")
                    ? h["radius_m"].AsFloat(fb.HitRadiusM)
                    : Warn(hitWarn + ".radius_m", fb.HitRadiusM, "vuruş yarıçapı");
                hit = new MotionHitSpec(
                    h["shape"].AsString("capsule"),
                    h["anchor"].AsString("forward"),
                    length,
                    radius,
                    h.Has("at") ? h["at"].AsFloat(0.5f) : 0.5f,
                    h.Has("share") ? h["share"].AsFloat(1f) : 1f,
                    h["payload"].AsString("damage"),
                    h.Has("every_sec") ? h["every_sec"].AsFloat(0f) : 0f);
            }

            return new MotionPhase(
                name,
                row["motion"].AsString("hold"),
                sec,
                row["facing"].AsString("target"),
                row["homing"].AsString("none"),
                row["gate"].AsString(),
                row.Has("max_hold_sec") ? row["max_hold_sec"].AsFloat(fb.MaxHoldSec) : fb.MaxHoldSec,
                row.Has("distance_m") ? row["distance_m"].AsFloat(fb.StepM) : 0f,
                row.Has("forward_m") ? row["forward_m"].AsFloat(0f) : 0f,
                row.Has("side") ? row["side"].AsFloat(1f) : 1f,
                row.Has("side_m") ? row["side_m"].AsFloat(0f) : 0f,
                row.Has("height_m") ? row["height_m"].AsFloat(fb.HeightM) : 0f,
                row.Has("yaw_deg") ? row["yaw_deg"].AsFloat(0f) : 0f,
                row.Has("gap_m") ? row["gap_m"].AsFloat(fb.GapM) : fb.GapM,
                row.Has("overshoot_m") ? row["overshoot_m"].AsFloat(0f) : 0f,
                row.Has("shot_m") ? row["shot_m"].AsFloat(0f) : 0f,
                row.Has("drift_m") ? row["drift_m"].AsFloat(0f) : 0f,
                row.Has("walk_mps") ? row["walk_mps"].AsFloat(fb.WalkMps) : 0f,
                row.Has("behind_m") ? row["behind_m"].AsFloat(1.15f) : 1.15f,
                row.Has("snap_at") ? row["snap_at"].AsFloat(0f) : 0f,
                curve,
                hit);
        }

        static float Need(JsonValue row, string key, float coded, string warnKey)
        {
            if (row.Has(key))
                return row[key].AsFloat(coded);
            return Warn(warnKey, coded, key);
        }

        static float Warn(string key, float fallback, string label)
        {
            DesignWarnings.Once(
                key,
                "Hareket verisinde " + label + " yok. Yedek " +
                fallback.ToString("0.###", CultureInfo.InvariantCulture) + ".");
            return fallback;
        }
    }

    public sealed class MotionFallbacks
    {
        public static readonly MotionFallbacks Coded = new(0.28f, 1.2f, 1.5f, 0.4f, 0.9f, 1.6f, 0.9f, 0.9f);

        public MotionFallbacks(
            float phaseSec,
            float stepM,
            float hitLengthM,
            float hitRadiusM,
            float maxHoldSec,
            float walkMps,
            float heightM,
            float gapM)
        {
            PhaseSec = phaseSec;
            StepM = stepM;
            HitLengthM = hitLengthM;
            HitRadiusM = hitRadiusM;
            MaxHoldSec = maxHoldSec;
            WalkMps = walkMps;
            HeightM = heightM;
            GapM = gapM;
        }

        public float PhaseSec { get; }
        public float StepM { get; }
        public float HitLengthM { get; }
        public float HitRadiusM { get; }
        public float MaxHoldSec { get; }
        public float WalkMps { get; }
        public float HeightM { get; }
        public float GapM { get; }
    }
}
