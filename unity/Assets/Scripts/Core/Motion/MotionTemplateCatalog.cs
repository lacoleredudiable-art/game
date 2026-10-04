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
    public sealed class MotionTemplateCatalog : IMotionTemplateRepository
    {
        public const string TagPortal = "portal";
        public const string TagSinir = "sinir_modu";
        public const string TagTakim = "takim_kombosu";
        public const string TagSilah = "silah_kesme";

        readonly Dictionary<string, MotionBinding> _bySkill = new(StringComparer.Ordinal);
        readonly Dictionary<string, MotionTemplate> _byId = new(StringComparer.Ordinal);
        readonly List<MotionTemplate> _templates = new();

        internal MotionTemplateCatalog(MotionFallbacks fallbacks)
        {
            Fallbacks = fallbacks;
        }

        public MotionFallbacks Fallbacks { get; }
        public MotionAnimTable Anims { get; internal set; } = MotionAnimTable.BuiltIn;
        public int SkillCount => _bySkill.Count;
        public int TemplateCount => _templates.Count;
        public int FamilyCount { get; internal set; }
        public IReadOnlyList<MotionTemplate> Templates => _templates;
        /// <summary>Düz vuruş adımı; kombo listesine dahil değil.</summary>
        public MotionTemplate BasicStrike { get; internal set; }

        public static MotionTemplateCatalog Empty { get; } =
            new MotionTemplateCatalog(MotionFallbacks.Coded);

        public static MotionTemplateCatalog FromJson(string json) =>
            FromJsonRoot(MiniJson.Parse(json));

        public static MotionTemplateCatalog FromJsonRoot(JsonValue root) =>
            MotionTemplateParser.Parse(root);

        internal void ImportTemplate(MotionTemplate template)
        {
            _templates.Add(template);
            if (!string.IsNullOrEmpty(template.Id))
                _byId[template.Id] = template;
        }

        internal void ImportBinding(
            string skillId,
            MotionTemplate template,
            bool familyImplemented,
            List<string> tags,
            float sinir) =>
            _bySkill[skillId] = new MotionBinding(template, familyImplemented, tags, sinir);

        public bool TryGet(SkillId skillId, out MotionBinding binding)
        {
            if (!skillId.IsEmpty && _bySkill.TryGetValue(skillId.Value, out MotionBinding found))
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
        public bool TryPlay(SkillId skillId, out MotionTemplate template)
        {
            template = null;
            if (!TryGet(skillId, out MotionBinding binding))
            {
                DesignWarnings.Once(
                    "motion.missing." + skillId.Value,
                    "Hareket kalıbı yok: " + skillId.Value + ". Eski davranış sürüyor.");
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
    }
}
