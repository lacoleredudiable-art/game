using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Motion
{
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
        public MotionAnimTable Anims { get; private set; } = MotionAnimTable.BuiltIn;
        public int SkillCount => _bySkill.Count;
        public int TemplateCount => _templates.Count;
        public int FamilyCount { get; private set; }
        public IReadOnlyList<MotionTemplate> Templates => _templates;
        /// <summary>Düz vuruş adımı; kombo listesine dahil değil.</summary>
        public MotionTemplate BasicStrike { get; private set; }

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
                Need(fb, "gap_m", MotionFallbacks.Coded.GapM, "motion.fallback.gap_m"),
                Need(fb, "stop_gap_m", MotionFallbacks.Coded.StopGapM, "motion.fallback.stop_gap_m"));

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
            catalog.Anims = MotionAnimTable.Parse(root);

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
                    phases,
                    row["aim"].AsString(MotionAim.Effect));
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

            if (root.Has("basic_strike") && root["basic_strike"].Kind == JsonKind.Object)
            {
                JsonValue row = root["basic_strike"];
                int familyId = row["family"].AsInt(0);
                families.TryGetValue(familyId, out var family);
                string templateId = row["id"].AsString("basic_strike");
                var phases = new List<MotionPhase>();
                foreach (JsonValue phase in row["phases"].AsArray())
                    phases.Add(ParsePhase(templateId, phase, fallbacks));
                catalog.BasicStrike = new MotionTemplate(
                    templateId,
                    row["name"].AsString(),
                    familyId,
                    family.Name,
                    family.Implemented,
                    phases,
                    row["aim"].AsString(MotionAim.Enemy));
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
                row.Has("behind_m") ? row["behind_m"].AsFloat(MotionTemplateCatalogDefaults.FallbackBehindM) : MotionTemplateCatalogDefaults.FallbackBehindM,
                row.Has("snap_at") ? row["snap_at"].AsFloat(0f) : 0f,
                curve,
                hit,
                row["land"].AsString(),
                row["plant"].AsBool(false),
                ReadAnim(templateId, name, row),
                row.Has("anim_speed") ? row["anim_speed"].AsFloat(1f) : 1f);
        }

        static string ReadAnim(string templateId, string phaseName, JsonValue row)
        {
            string anim = row["anim"].AsString();
            if (!string.IsNullOrEmpty(anim))
                return anim;
            string fallback = MotionAnimTable.FallbackKey(row["motion"].AsString("hold"));
            DesignWarnings.Once(
                "motion.anim.phase." + templateId + "." + phaseName,
                "Fazda animasyon anahtarı yok: " + templateId + " " + phaseName + ". Yedek " + fallback + ".");
            return fallback;
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
}
