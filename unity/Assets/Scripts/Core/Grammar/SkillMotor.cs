using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Element;
using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    /// <summary>
    /// docs/element-sistemi.json. v6.1.1'de 12 çift-yüzlü rün + 2-rün
    /// fiil×sıfat gramerini çözer. Yalnız v6 şeması okunur (v5 ayrıştırıcıları ve gömülü
    /// v5 yedeği CLEANUP-2b ile kaldırıldı; v6 olmayan JSON reddedilir).
    /// Root/stun vb. dünyada uygulamak StatusDirector işi; bu sınıf yalnızca çözüm üretir.
    /// Parse artık MiniJson (gerçek ağaç) üzerinden — animation_type/target_mode/
    /// base_cooldown_sec/base_resource_cost/target_behaviors/special/zone_effect ve
    /// adjectives.engine_modifiers'ın tamamı okunur (docs/element-sistemi.json
    /// "motor_parse_extension" adım 1).
    /// </summary>
    public sealed partial class SkillMotor
    {
        readonly Dictionary<string, ElementNode> _elements = new(StringComparer.Ordinal);
        readonly Dictionary<string, VerbNode> _verbs = new(StringComparer.Ordinal);
        readonly Dictionary<string, AdjectiveNode> _adjectives = new(StringComparer.Ordinal);
        readonly Dictionary<int, RuneDefinition> _runes = new();
        readonly List<RuneDefinition> _runeDefinitions = new();
        readonly Dictionary<string, SkillCatalogEntry> _v61Skills = new(StringComparer.Ordinal);
        readonly List<ElementPaintNode> _elementPaints = new();
        readonly List<MainClassNode> _mainClasses = new();
        readonly List<PlayerStateNode> _playerStates = new();
        readonly List<BossStateNode> _bossStates = new();
        int _maxComboLength = 4;
        RuneLoadout _defaultLoadout = RuneLoadout.Sequential;
        string _version = string.Empty;
        bool _isV61;

        public int ElementCount => _elements.Count;
        public int VerbCount => _verbs.Count;
        public int AdjectiveCount => _adjectives.Count;
        public int RuneCount => _runes.Count;
        public IReadOnlyList<RuneDefinition> RuneDefinitions => _runeDefinitions;
        public int SkillCount => _v61Skills.Count;
        public string Version => _version;
        public bool IsV61 => _isV61;
        public int MaxComboLength => _maxComboLength;
        public RuneLoadout DefaultLoadout => _defaultLoadout;
        public IReadOnlyList<ElementPaintNode> ElementPaints => _elementPaints;
        public IReadOnlyList<MainClassNode> MainClasses => _mainClasses;

        /// <summary>
        /// docs/element-sistemi.json state_machine.player_states — yalnızca okuma.
        /// Runtime geçişler Core/Combat/PlayerStateMachine; SentencePhase'e bağlanmadı.
        /// </summary>
        public IReadOnlyList<PlayerStateNode> PlayerStates => _playerStates;

        /// <summary>docs/element-sistemi.json state_machine.boss_states — yalnızca okuma.</summary>
        public IReadOnlyList<BossStateNode> BossStates => _bossStates;

        /// <summary>
        /// Rünsüz, skillsiz motor. JSON yüklenemediğinde oyunun çökmemesi için döner:
        /// hiçbir skill çözülmez, <see cref="IsV61"/> false kalır. İçerik yedeği DEĞİL.
        /// </summary>
        public static SkillMotor CreateEmpty() => new SkillMotor();

        public static SkillMotor FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON boş.", nameof(json));
            return FromDocument(ElementSystemDocument.Parse(json));
        }

        public static SkillMotor FromDocument(ElementSystemDocument doc) =>
            FromJsonRoot(doc.Root);

        public static SkillMotor FromJsonRoot(JsonValue root)
        {
            var motor = new SkillMotor();
            motor._version = root["system"]["version"].AsString();
            if (root["runes"].Kind != JsonKind.Array)
                throw new InvalidOperationException(
                    "element-sistemi: v6 şeması (runes[]) bekleniyor; v5 şeması artık okunmuyor.");

            ParseV61(root, motor);
            return motor;
        }

        public bool TryGetRune(int id, out RuneDefinition rune) =>
            _runes.TryGetValue(id, out rune);

        public string RuneName(int id) =>
            _runes.TryGetValue(id, out RuneDefinition rune)
                ? rune.Name
                : (RuneInfo.TryFromId(id, out Rune legacy) ? RuneInfo.DisplayName(legacy) : $"#{id}");

        public RuneLoadout CreateLoadout(
            IReadOnlyList<int> runeIds,
            IReadOnlyList<int>? passiveRuneIds = null)
        {
            var loadout = new RuneLoadout(runeIds, passiveRuneIds);
            for (int i = 0; i < loadout.RuneIds.Count; i++)
                if (!_runes.ContainsKey(loadout.RuneIds[i]))
                    throw new ArgumentException("Build, katalogda olmayan rün içeriyor.", nameof(runeIds));
            return loadout;
        }

        public bool TryCreateMainClassLoadout(
            int mainClassId,
            IReadOnlyList<int>? passiveRuneIds,
            out RuneLoadout loadout)
        {
            for (int i = 0; i < _mainClasses.Count; i++)
            {
                if (_mainClasses[i].Id != mainClassId)
                    continue;
                loadout = CreateLoadout(_mainClasses[i].RuneIds, passiveRuneIds);
                return true;
            }

            loadout = _defaultLoadout;
            return false;
        }

        public bool TryGetVerb(string id, out VerbNode node) =>
            _verbs.TryGetValue(id, out node);

        public bool TryGetAdjective(string id, out AdjectiveNode node) =>
            _adjectives.TryGetValue(id, out node);

        public void ForEachSkill(Action<string, SkillCatalogEntry> visit)
        {
            if (visit == null)
                throw new ArgumentNullException(nameof(visit));
            foreach (KeyValuePair<string, SkillCatalogEntry> kv in _v61Skills)
                visit(kv.Key, kv.Value);
        }

        SkillResolution ResolveV61(IReadOnlyList<int> runeIds)
        {
            int len = runeIds.Count;
            if (len < 1 || len > _maxComboLength)
                return SkillResolution.Empty;
            if (!_runes.TryGetValue(runeIds[0], out RuneDefinition verbRune))
                return SkillResolution.Empty;

            string verbId = runeIds[0].ToString(CultureInfo.InvariantCulture);
            _verbs.TryGetValue(verbId, out VerbNode verb);

            if (len == 1)
            {
                return new SkillResolution(
                    elementId: string.Empty,
                    elementName: string.Empty,
                    displayName: verbRune.VerbFace,
                    skillId: "verb:" + verbId,
                    skillJob: verbRune.BaseEffect,
                    verbId: verbId,
                    verbName: verbRune.VerbFace,
                    verbFamily: verbRune.Family,
                    action: verb.Action,
                    baseDamage: verb.BaseDamage,
                    basePoise: verb.BasePoise,
                    hitbox: verb.Hitbox,
                    castMobility: verb.CastMobility,
                    mechanics: verb.Mechanics,
                    adjectiveId: string.Empty,
                    adjectiveName: string.Empty,
                    silhouetteAxis: string.Empty,
                    damageMult: 1f,
                    hitboxScaleMult: 1f,
                    poiseDamageMult: 1f,
                    length: 1,
                    lengthRole: "Fiil önizleme",
                    lengthCastMult: 1f,
                    lengthMobility: "free_move",
                    flavorElement: string.Empty,
                    targetMode: verb.TargetMode,
                    baseCooldownSec: verb.BaseCooldownSec,
                    baseResourceCost: verb.BaseResourceCost,
                    engineModifiers: verb.Raw,
                    critEligible: verb.BaseDamage > 0f,
                    isComplete: false,
                    baseHeal: verb.Raw["base_heal"].AsFloat(0f));
            }

            int adjectiveRuneId = runeIds[1];
            if (!_runes.TryGetValue(adjectiveRuneId, out RuneDefinition adjectiveRune))
                return SkillResolution.Empty;

            string skillId = verbId + "-" + adjectiveRuneId.ToString(CultureInfo.InvariantCulture);
            if (!_v61Skills.TryGetValue(skillId, out SkillCatalogEntry skill))
                return SkillResolution.Empty;

            JsonValue engine = skill.Engine;
            string adjectiveId = adjectiveRuneId.ToString(CultureInfo.InvariantCulture);
            _adjectives.TryGetValue(adjectiveId, out AdjectiveNode adjective);
            string[] mechanics = ReadV61Mechanics(engine, skill.Effect);

            return new SkillResolution(
                elementId: string.Empty,
                elementName: string.Empty,
                displayName: skill.Name,
                skillId: skill.Id,
                skillJob: skill.Effect,
                verbId: verbId,
                verbName: verbRune.VerbFace,
                verbFamily: verbRune.Family,
                action: engine["action"].AsString(verb.Action),
                baseDamage: engine["base_damage"].AsFloat(verb.BaseDamage),
                basePoise: engine["base_poise"].AsFloat(verb.BasePoise),
                hitbox: engine["hitbox"].AsString(verb.Hitbox),
                castMobility: engine["cast_mobility"].AsString(verb.CastMobility),
                mechanics: mechanics,
                adjectiveId: adjectiveId,
                adjectiveName: adjectiveRune.AdjectiveFace,
                silhouetteAxis: adjective.SilhouetteAxis,
                damageMult: engine["damage_mult"].AsFloat(1f),
                hitboxScaleMult: engine["hitbox_scale_mult"].AsFloat(1f),
                poiseDamageMult: engine["poise_damage_mult"].AsFloat(1f),
                length: 2,
                lengthRole: "2-rün skill",
                lengthCastMult: 1f,
                lengthMobility: "free_move",
                flavorElement: string.Empty,
                targetMode: verbRune.TargetMode,
                baseCooldownSec: engine["base_cooldown"].AsFloat(verb.BaseCooldownSec),
                baseResourceCost: engine["base_cost"].AsFloat(verb.BaseResourceCost),
                engineModifiers: engine,
                critEligible: engine["base_damage"].AsFloat(0f) > 0f,
                isComplete: true,
                baseHeal: engine["base_heal"].AsFloat(0f),
                passiveDescription: PassiveDescriptionFor(skill, adjectiveRune),
                proseFeel: skill.ProseFeel,
                proseVisual: skill.ProseVisual);
        }

        /// <summary>
        /// v6.1.1: 1 rün = fiil önizlemesi (IsComplete false), 2 rün = fiil×sıfat skill'i.
        /// Boş motorda (JSON yok) her zaman Empty.
        /// </summary>
        public SkillResolution Resolve(IReadOnlyList<int> runeIds)
        {
            if (runeIds == null || runeIds.Count == 0)
                return SkillResolution.Empty;
            return ResolveV61(runeIds);
        }

        public SkillResolution ResolveWords(IReadOnlyList<SentenceWord> words)
        {
            if (words == null || words.Count == 0)
                return SkillResolution.Empty;
            var ids = new int[words.Count];
            for (int i = 0; i < words.Count; i++)
                ids[i] = (int)words[i].Rune;
            return Resolve(ids);
        }

    }
}
