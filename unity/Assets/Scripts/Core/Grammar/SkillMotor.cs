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
    public sealed class SkillMotor
    {
        readonly Dictionary<string, ElementNode> _elements = new(StringComparer.Ordinal);
        readonly Dictionary<string, VerbNode> _verbs = new(StringComparer.Ordinal);
        readonly Dictionary<string, AdjectiveNode> _adjectives = new(StringComparer.Ordinal);
        readonly Dictionary<int, RuneDefinition> _runes = new();
        readonly List<RuneDefinition> _runeDefinitions = new();
        readonly Dictionary<string, V61SkillNode> _v61Skills = new(StringComparer.Ordinal);
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

        public void ForEachSkill(Action<string, V61SkillNode> visit)
        {
            if (visit == null)
                throw new ArgumentNullException(nameof(visit));
            foreach (KeyValuePair<string, V61SkillNode> kv in _v61Skills)
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
            if (!_v61Skills.TryGetValue(skillId, out V61SkillNode skill))
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

        static string PassiveDescriptionFor(V61SkillNode skill, RuneDefinition adjectiveRune)
        {
            string passive = skill.Passive ?? string.Empty;
            if (!passive.Contains(" sn pasif: ", StringComparison.Ordinal))
                return passive;
            return SkillTextNumbers.PassiveText(
                adjectiveRune.AdjectiveFace,
                adjectiveRune.PassiveDurationDefault);
        }

        static void ParseV61(JsonValue root, SkillMotor motor)
        {
            motor._isV61 = true;
            motor._maxComboLength = root["combo_system"]["current_max_length"].AsInt(2);

            JsonValue verbBase = root["verb_base"];
            JsonValue adjectiveMods = root["adjective_mods"];
            foreach (JsonValue obj in root["runes"].AsArray())
            {
                int id = obj["id"].AsInt();
                if (id <= 0)
                    continue;

                string key = id.ToString(CultureInfo.InvariantCulture);
                string name = obj["name"].AsString();
                string verbFace = obj["verb_face"].AsString(name);
                string adjectiveFace = obj["adjective_face"].AsString();
                string family = obj["family"].AsString();
                string targetMode = obj["target_mode"].AsString();
                var rune = new RuneDefinition(
                    id,
                    name,
                    verbFace,
                    adjectiveFace,
                    obj["adjective_prefix"].AsString(),
                    obj["verb_noun"].AsString(),
                    family,
                    obj["category"].AsString(),
                    targetMode,
                    obj["base_effect"].AsString(),
                    obj["passive_duration_default"].AsFloat(0f));
                motor._runes[id] = rune;
                motor._runeDefinitions.Add(rune);

                // Eski tüketicilerin sözlük API'si korunur; içerik v6 rünlerinden üretilir.
                motor._elements[key] = new ElementNode(
                    key, name, "core", key, key, string.Empty, string.Empty,
                    obj["base_effect"].AsString(), string.Empty,
                    JsonValue.Null, JsonValue.Null, obj);

                JsonValue stats = verbBase[key];
                string[] mechanics = ReadV61Mechanics(stats, string.Empty);
                motor._verbs[key] = new VerbNode(
                    key,
                    verbFace,
                    family,
                    stats["action"].AsString(),
                    stats["base_damage"].AsFloat(0f),
                    stats["base_poise"].AsFloat(0f),
                    stats["hitbox"].AsString(),
                    stats["cast_mobility"].AsString("free_move"),
                    mechanics,
                    string.Empty,
                    targetMode,
                    stats["base_cooldown"].AsFloat(0f),
                    stats["base_cost"].AsFloat(0f),
                    EmptyBehaviors,
                    stats,
                    JsonValue.Null,
                    stats,
                    stats["base_damage"].AsFloat(0f) > 0f,
                    string.Empty,
                    string.Empty);

                JsonValue mods = adjectiveMods[key];
                motor._adjectives[key] = new AdjectiveNode(
                    key,
                    adjectiveFace,
                    obj["category"].AsString(),
                    mods["label"].AsString(adjectiveFace),
                    mods["damage_mult"].AsFloat(1f),
                    mods["hitbox_scale_mult"].AsFloat(1f),
                    mods["poise_damage_mult"].AsFloat(1f),
                    mods,
                    obj);
            }

            foreach (var verbKv in root["skills"]["by_verb"].AsObject())
            {
                foreach (JsonValue obj in verbKv.Value["skills"].AsArray())
                {
                    string id = obj["id"].AsString();
                    if (string.IsNullOrEmpty(id))
                        continue;
                    motor._v61Skills[id] = new V61SkillNode(
                        id,
                        obj["name"].AsString(),
                        obj["effect"].AsString(),
                        obj["passive"].AsString(),
                        obj["engine"],
                        obj["prose_mechanic"].AsString(),
                        obj["prose_feel"].AsString(),
                        obj["prose_visual"].AsString());
                }
            }

            foreach (JsonValue obj in root["elements"].AsArray())
            {
                int id = obj["id"].AsInt();
                if (id <= 0)
                    continue;
                motor._elementPaints.Add(new ElementPaintNode(
                    id,
                    obj["name"].AsString(),
                    obj["name_prefix"].AsString(),
                    obj["color"].AsString(),
                    obj["vfx"].AsString(),
                    obj.Has("status") ? obj["status"].AsString() : string.Empty,
                    obj.Has("status_effect") ? obj["status_effect"].AsString() : string.Empty,
                    obj.Has("status_duration") ? obj["status_duration"].AsFloat(0f) : 0f));
            }

            foreach (var group in root["ana_classes_80"]["groups"].AsObject())
            {
                foreach (JsonValue obj in group.Value["classes"].AsArray())
                {
                    IReadOnlyList<JsonValue> values = obj["runes"].AsArray();
                    var ids = new int[values.Count];
                    for (int i = 0; i < ids.Length; i++)
                        ids[i] = values[i].AsInt();
                    motor._mainClasses.Add(new MainClassNode(
                        obj["id"].AsInt(),
                        obj["name"].AsString(),
                        ids,
                        obj["category"].AsString(),
                        obj["his"].AsString()));
                }
            }

            if (motor._mainClasses.Count > 0)
                motor._defaultLoadout = new RuneLoadout(motor._mainClasses[0].RuneIds);
            else
            {
                var ids = new List<int>(RuneLoadout.SlotCount);
                foreach (var kv in motor._runes)
                {
                    if (ids.Count >= RuneLoadout.SlotCount) break;
                    ids.Add(kv.Key);
                }
                motor._defaultLoadout = new RuneLoadout(ids);
            }

            ParseStateMachine(root, motor._playerStates, motor._bossStates);

            if (motor._runes.Count != 12)
                throw new InvalidOperationException("element-sistemi v6: 12 rün beklenir.");
            if (motor._v61Skills.Count != GrammarDefaults.ExpectedV61SkillCount)
                throw new InvalidOperationException("element-sistemi v6: 144 skill beklenir.");
            if (motor._maxComboLength != 2)
                throw new InvalidOperationException("element-sistemi v6: mevcut kombo uzunluğu 2 olmalıdır.");
        }

        static string[] ReadV61Mechanics(JsonValue engine, string effectText)
        {
            var mechanics = new List<string>();
            string action = engine["action"].AsString();
            void Add(string value)
            {
                if (!string.IsNullOrEmpty(value) && !mechanics.Contains(value))
                    mechanics.Add(value);
            }

            if (action == "cc")
                Add(Dovus.Core.Status.CardEffectRules.CcKind(effectText, engine["cc_kind"].AsString()));
            else if (action == "cleanse")
                Add("cleanse");
            else if (action == "shield")
                Add("shield");
            // Zırh düşürme tek mekanizma: engine debuff_armor → ArmorSheet kırılması.
            // Ayrı armor_break durumu gelen hasarı bir de ×1,2 çarpardı.

            return mechanics.ToArray();
        }

        /// <summary>
        /// state_machine.player_states + boss_states. Bool/string karışık capability
        /// alanları (can_draw:"partial", can_move:"limited") metin olarak saklanır.
        /// </summary>
        static void ParseStateMachine(
            JsonValue root, List<PlayerStateNode> playerDst, List<BossStateNode> bossDst)
        {
            JsonValue sm = root["state_machine"];
            if (sm.Kind != JsonKind.Object)
                return;

            foreach (var kv in sm["player_states"].AsObject())
            {
                JsonValue obj = kv.Value;
                playerDst.Add(new PlayerStateNode(
                    id: kv.Key,
                    canDraw: ReadCapability(obj["can_draw"]),
                    canMove: ReadCapability(obj["can_move"]),
                    canDodge: ReadCapability(obj["can_dodge"]),
                    iFrames: obj["i_frames"].AsBool(false),
                    interruptible: obj["interruptible"].AsBool(false),
                    note: obj["note"].AsString(),
                    canSwap: ReadCapability(obj["can_swap"])));
            }

            foreach (var kv in sm["boss_states"].AsObject())
            {
                JsonValue obj = kv.Value;
                JsonValue duration = obj["duration_sec"];
                bossDst.Add(new BossStateNode(
                    id: kv.Key,
                    exitsTo: ReadStringArray(obj["exits_to"]),
                    hasDuration: duration.Kind == JsonKind.Number,
                    durationSec: duration.AsFloat(0f)));
            }
        }

        /// <summary>
        /// can_draw / can_move / can_dodge: JSON'da bool veya string olabilir
        /// ("partial", "based_on_cast_mobility", "limited", "dodge_direction").
        /// Eksikse "false".
        /// </summary>
        static string ReadCapability(JsonValue v)
        {
            if (v.Kind == JsonKind.Bool)
                return v.AsBool() ? "true" : "false";
            if (v.Kind == JsonKind.String)
            {
                string s = v.AsString();
                return string.IsNullOrEmpty(s) ? "false" : s;
            }
            return "false";
        }

        static string[] ReadStringArray(JsonValue arr)
        {
            IReadOnlyList<JsonValue> items = arr.AsArray();
            if (items.Count == 0) return Array.Empty<string>();
            var list = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
                list[i] = items[i].AsString();
            return list;
        }

        static readonly IReadOnlyDictionary<string, string> EmptyBehaviors =
            new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public readonly struct RuneDefinition
    {
        public RuneDefinition(
            int id, string name, string verbFace, string adjectiveFace,
            string adjectivePrefix, string verbNoun, string family, string category,
            string targetMode, string baseEffect, float passiveDurationDefault)
        {
            Id = id;
            Name = name ?? string.Empty;
            VerbFace = verbFace ?? string.Empty;
            AdjectiveFace = adjectiveFace ?? string.Empty;
            AdjectivePrefix = adjectivePrefix ?? string.Empty;
            VerbNoun = verbNoun ?? string.Empty;
            Family = family ?? string.Empty;
            Category = category ?? string.Empty;
            TargetMode = targetMode ?? string.Empty;
            BaseEffect = baseEffect ?? string.Empty;
            PassiveDurationDefault = passiveDurationDefault;
        }

        public int Id { get; }
        public string Name { get; }
        public string VerbFace { get; }
        public string AdjectiveFace { get; }
        public string AdjectivePrefix { get; }
        public string VerbNoun { get; }
        public string Family { get; }
        public string Category { get; }
        public string TargetMode { get; }
        public string BaseEffect { get; }
        public float PassiveDurationDefault { get; }
    }

    public readonly struct V61SkillNode
    {
        public V61SkillNode(
            string id, string name, string effect, string passive, JsonValue engine,
            string proseMechanic, string proseFeel, string proseVisual)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Effect = effect ?? string.Empty;
            Passive = passive ?? string.Empty;
            Engine = engine;
            ProseMechanic = proseMechanic ?? string.Empty;
            ProseFeel = proseFeel ?? string.Empty;
            ProseVisual = proseVisual ?? string.Empty;
        }

        public string Id { get; }
        public string Name { get; }
        public string Effect { get; }
        public string Passive { get; }
        public JsonValue Engine { get; }
        public string ProseMechanic { get; }
        public string ProseFeel { get; }
        public string ProseVisual { get; }
    }

    public readonly struct ElementPaintNode
    {
        public ElementPaintNode(
            int id,
            string name,
            string namePrefix,
            string colorHex,
            string vfx,
            string status = "",
            string statusEffect = "",
            float statusDurationSec = 0f)
        {
            Id = id;
            Name = name ?? string.Empty;
            NamePrefix = namePrefix ?? string.Empty;
            ColorHex = colorHex ?? string.Empty;
            Vfx = vfx ?? string.Empty;
            Status = status ?? string.Empty;
            StatusEffect = statusEffect ?? string.Empty;
            StatusDurationSec = statusDurationSec;
        }

        public int Id { get; }
        public string Name { get; }
        public string NamePrefix { get; }
        public string ColorHex { get; }
        public string Vfx { get; }
        /// <summary>elements[].status — burn, regen, haste, shield, cleanse, weaken.</summary>
        public string Status { get; }
        /// <summary>elements[].status_effect metni.</summary>
        public string StatusEffect { get; }
        /// <summary>elements[].status_duration saniye.</summary>
        public float StatusDurationSec { get; }
    }

    public readonly struct MainClassNode
    {
        public MainClassNode(int id, string name, int[] runeIds, string category, string feel)
        {
            Id = id;
            Name = name ?? string.Empty;
            RuneIds = runeIds ?? Array.Empty<int>();
            Category = category ?? string.Empty;
            Feel = feel ?? string.Empty;
        }

        public int Id { get; }
        public string Name { get; }
        public int[] RuneIds { get; }
        public string Category { get; }
        public string Feel { get; }
    }

    public readonly struct ElementNode
    {
        public ElementNode(
            string id, string name, string type, string verbId, string adjectiveId,
            string skillId, string skillName, string skillJob,
            string identity, JsonValue specialMechanics, JsonValue zoneEffect, JsonValue raw)
        {
            Id = id; Name = name; Type = type; VerbId = verbId; AdjectiveId = adjectiveId;
            SkillId = skillId; SkillName = skillName; SkillJob = skillJob;
            Identity = identity; SpecialMechanics = specialMechanics; ZoneEffect = zoneEffect;
            Raw = raw;
        }

        public string Id { get; }
        public string Name { get; }
        public string Type { get; }
        public string VerbId { get; }
        public string AdjectiveId { get; }
        public string SkillId { get; }
        public string SkillName { get; }
        public string SkillJob { get; }
        /// <summary>Bileşiğin kimlik/rol metni (v5.2 element_families_detailed.bileşikler.identity).</summary>
        public string Identity { get; }
        /// <summary>Bileşiğe özel mekanik nesnesi (varsa); motor henüz uygulamıyor, veri taşıyor.</summary>
        public JsonValue SpecialMechanics { get; }
        public JsonValue ZoneEffect { get; }
        /// <summary>Ham JSON nesnesi — henüz tipli alanı olmayan gelecek alanlar için.</summary>
        public JsonValue Raw { get; }
    }

    public readonly struct VerbNode
    {
        public VerbNode(
            string id, string name, string family, string action,
            float baseDamage, float basePoise, string hitbox, string castMobility, string[] mechanics,
            string animationType, string targetMode, float baseCooldownSec, float baseResourceCost,
            IReadOnlyDictionary<string, string> targetBehaviors, JsonValue special, JsonValue zoneEffect,
            JsonValue raw,
            bool critEligible = false, string elementOrigin = "", string damageType = "")
        {
            Id = id; Name = name; Family = family; Action = action;
            BaseDamage = baseDamage; BasePoise = basePoise; Hitbox = hitbox;
            CastMobility = castMobility; Mechanics = mechanics;
            AnimationType = animationType; TargetMode = targetMode;
            BaseCooldownSec = baseCooldownSec; BaseResourceCost = baseResourceCost;
            TargetBehaviors = targetBehaviors; Special = special; ZoneEffect = zoneEffect;
            Raw = raw;
            CritEligible = critEligible;
            ElementOrigin = elementOrigin ?? string.Empty;
            DamageType = damageType ?? string.Empty;
        }

        public string Id { get; }
        public string Name { get; }
        public string Family { get; }
        public string Action { get; }
        public float BaseDamage { get; }
        public float BasePoise { get; }
        public string Hitbox { get; }
        public string CastMobility { get; }
        public string[] Mechanics { get; }
        public string AnimationType { get; }
        public string TargetMode { get; }
        public float BaseCooldownSec { get; }
        /// <summary>docs/element-sistemi.json "base_resource_cost" — bazı fiillerde henüz yok (bkz. docs/durum.md).</summary>
        public float BaseResourceCost { get; }
        /// <summary>selective fiiller için self/enemy/ally davranış metni.</summary>
        public IReadOnlyDictionary<string, string> TargetBehaviors { get; }
        /// <summary>Fiile özel serbest-form veri (heal_value, shield_amount, vb.). Ham JSON, tip yok.</summary>
        public JsonValue Special { get; }
        public JsonValue ZoneEffect { get; }
        public JsonValue Raw { get; }
        /// <summary>docs/element-sistemi.json verbs[].crit_eligible</summary>
        public bool CritEligible { get; }
        /// <summary>docs/element-sistemi.json verbs[].element_origin</summary>
        public string ElementOrigin { get; }
        /// <summary>docs/element-sistemi.json verbs[].engine_base_stats.damage_type</summary>
        public string DamageType { get; }
    }

    public readonly struct AdjectiveNode
    {
        public AdjectiveNode(
            string id, string name, string category, string silhouetteAxis,
            float damageMult, float hitboxScaleMult, float poiseDamageMult,
            JsonValue engineModifiers, JsonValue raw)
        {
            Id = id; Name = name; Category = category; SilhouetteAxis = silhouetteAxis;
            DamageMult = damageMult; HitboxScaleMult = hitboxScaleMult; PoiseDamageMult = poiseDamageMult;
            EngineModifiers = engineModifiers; Raw = raw;
        }

        public string Id { get; }
        public string Name { get; }
        public string Category { get; }
        public string SilhouetteAxis { get; }
        public float DamageMult { get; }
        public float HitboxScaleMult { get; }
        public float PoiseDamageMult { get; }
        /// <summary>
        /// engine_modifiers'ın TAMAMI (20+ alan olabilir: trajectory_override, apply_slow,
        /// cooldown_mult, max_targets, ...). DamageMult/HitboxScaleMult/PoiseDamageMult zaten
        /// tipli; burada geri kalanına EngineModifiers["apply_slow"].AsFloat() gibi erişilir.
        /// </summary>
        public JsonValue EngineModifiers { get; }
        public JsonValue Raw { get; }
        public SkillEngineModifiers Engine => new SkillEngineModifiers(EngineModifiers);
    }

    /// <summary>docs/element-sistemi.json state_machine.player_states[id] — bkz. ParseStateMachine.</summary>
    public readonly struct PlayerStateNode
    {
        public PlayerStateNode(
            string id, string canDraw, string canMove, string canDodge,
            bool iFrames, bool interruptible = false, string note = "", string canSwap = "false")
        {
            Id = id ?? string.Empty;
            CanDraw = canDraw ?? "false";
            CanMove = canMove ?? "false";
            CanDodge = canDodge ?? "false";
            CanSwap = canSwap ?? "false";
            IFrames = iFrames;
            Interruptible = interruptible;
            Note = note ?? string.Empty;
        }

        public string Id { get; }
        /// <summary>"true" / "false" / "partial".</summary>
        public string CanDraw { get; }
        /// <summary>"true" / "false" / "based_on_cast_mobility" / "limited" / "dodge_direction".</summary>
        public string CanMove { get; }
        /// <summary>"true" / "false" (JSON'da yoksa "false").</summary>
        public string CanDodge { get; }
        /// <summary>"true" / "false" (JSON'da yoksa "false") — savaş içi silah swap.</summary>
        public string CanSwap { get; }
        public bool IFrames { get; }
        public bool Interruptible { get; }
        public string Note { get; }
    }

    /// <summary>docs/element-sistemi.json state_machine.boss_states[id] — bkz. ParseStateMachine.</summary>
    public readonly struct BossStateNode
    {
        public BossStateNode(string id, string[] exitsTo, bool hasDuration, float durationSec)
        {
            Id = id ?? string.Empty;
            ExitsTo = exitsTo ?? Array.Empty<string>();
            HasDuration = hasDuration;
            DurationSec = durationSec;
        }

        public string Id { get; }
        public IReadOnlyList<string> ExitsTo { get; }
        public bool HasDuration { get; }
        public float DurationSec { get; }
    }

    public readonly struct SkillResolution
    {
        public static SkillResolution Empty { get; } = default;

        public SkillResolution(
            string elementId, string elementName, string displayName, string skillId, string skillJob,
            string verbId, string verbName, string verbFamily, string action,
            float baseDamage, float basePoise, string hitbox, string castMobility, string[] mechanics,
            string adjectiveId, string adjectiveName, string silhouetteAxis,
            float damageMult, float hitboxScaleMult, float poiseDamageMult,
            int length, string lengthRole, float lengthCastMult, string lengthMobility,
            string flavorElement,
            string animationType = "", string targetMode = "", float baseCooldownSec = 0f,
            float baseResourceCost = 0f, IReadOnlyDictionary<string, string>? targetBehaviors = null,
            JsonValue? special = null, JsonValue? zoneEffect = null, JsonValue? engineModifiers = null,
            bool critEligible = false, string elementOrigin = "", string damageType = "",
            float lengthResourceCostMult = 1f, bool isComplete = true, float baseHeal = 0f,
            string passiveDescription = "", string proseFeel = "", string proseVisual = "")
        {
            ElementId = elementId ?? string.Empty;
            ElementName = elementName ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            SkillId = skillId ?? string.Empty;
            SkillJob = skillJob ?? string.Empty;
            VerbId = verbId ?? string.Empty;
            VerbName = verbName ?? string.Empty;
            VerbFamily = verbFamily ?? string.Empty;
            Action = action ?? string.Empty;
            BaseDamage = baseDamage;
            BasePoise = basePoise;
            Hitbox = hitbox ?? string.Empty;
            CastMobility = castMobility ?? string.Empty;
            Mechanics = mechanics ?? Array.Empty<string>();
            AdjectiveId = adjectiveId ?? string.Empty;
            AdjectiveName = adjectiveName ?? string.Empty;
            SilhouetteAxis = silhouetteAxis ?? string.Empty;
            DamageMult = damageMult;
            HitboxScaleMult = hitboxScaleMult;
            PoiseDamageMult = poiseDamageMult;
            Length = length;
            LengthRole = lengthRole ?? string.Empty;
            LengthCastMult = lengthCastMult;
            LengthMobility = lengthMobility ?? string.Empty;
            LengthResourceCostMult = lengthResourceCostMult > 0f ? lengthResourceCostMult : 1f;
            FlavorElement = flavorElement ?? string.Empty;
            AnimationType = animationType ?? string.Empty;
            TargetMode = targetMode ?? string.Empty;
            BaseCooldownSec = baseCooldownSec;
            BaseResourceCost = baseResourceCost;
            TargetBehaviors = targetBehaviors ?? EmptyBehaviors;
            Special = special ?? JsonValue.Null;
            ZoneEffect = zoneEffect ?? JsonValue.Null;
            EngineModifiers = engineModifiers ?? JsonValue.Null;
            CritEligible = critEligible;
            ElementOrigin = elementOrigin ?? string.Empty;
            DamageType = damageType ?? string.Empty;
            IsComplete = isComplete;
            BaseHeal = baseHeal;
            PassiveDescription = passiveDescription ?? string.Empty;
            ProseFeel = proseFeel ?? string.Empty;
            ProseVisual = proseVisual ?? string.Empty;
        }

        static readonly IReadOnlyDictionary<string, string> EmptyBehaviors =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public string ElementId { get; }
        public string ElementName { get; }
        public string DisplayName { get; }
        public string Name => DisplayName;
        public string SkillId { get; }
        public string SkillJob { get; }
        public string VerbId { get; }
        public string VerbName { get; }
        public string Verb => VerbName;
        public string VerbFamily { get; }
        public string Action { get; }
        public float BaseDamage { get; }
        public float BasePoise { get; }
        public string Hitbox { get; }
        public string CastMobility { get; }
        public string[] Mechanics { get; }
        public string AdjectiveId { get; }
        public string AdjectiveName { get; }
        public string Adjective => AdjectiveName;
        public string SilhouetteAxis { get; }
        public float DamageMult { get; }
        public float HitboxScaleMult { get; }
        public float PoiseDamageMult { get; }
        public int Length { get; }
        public string LengthRole { get; }
        public float LengthCastMult { get; }
        public string LengthMobility { get; }
        /// <summary>scaling_economy.lengths[N].resource_cost_mult (anti-ladder dışı maliyet).</summary>
        public float LengthResourceCostMult { get; }
        public string FlavorElement { get; }
        public bool IsEmpty => Length <= 0 || string.IsNullOrEmpty(DisplayName);

        // --- v4.2.2 / motor_parse_extension adım 1 ---
        public string AnimationType { get; }
        public string TargetMode { get; }
        public float BaseCooldownSec { get; }
        /// <summary>0 dönebilir: bazı fiillerde henüz tanımlı değil (bkz. docs/durum.md "Bilinen açıklar").</summary>
        public float BaseResourceCost { get; }
        public IReadOnlyDictionary<string, string> TargetBehaviors { get; }
        public JsonValue Special { get; }
        public JsonValue ZoneEffect { get; }
        public JsonValue EngineModifiers { get; }
        public SkillEngineModifiers Engine => new SkillEngineModifiers(EngineModifiers);
        public SkillId TypedSkillId => new SkillId(SkillId);
        public ElementId TypedElement => new ElementId(ElementId);
        public bool CritEligible { get; }
        public string ElementOrigin { get; }
        public string DamageType { get; }
        /// <summary>v6: false yalnız tek-rün fiil önizlemesinde; gerçek skill 2 ründür.</summary>
        public bool IsComplete { get; }
        public float BaseHeal { get; }
        public string PassiveDescription { get; }
        public string ProseFeel { get; }
        public string ProseVisual { get; }
    }
}
