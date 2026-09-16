using System;
using System.Collections.Generic;
using System.Globalization;

namespace Dovus.Core.Grammar
{
    /// <summary>
    /// docs/element-sistemi.json (v4.2.2) — 6 kök + 36 bileşik, fiil/sıfat sözlüğü.
    /// Kombo hasar tablosu değil: cümleyi element + verb + adjective + length ekonomisine çözer.
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
        readonly Dictionary<int, LengthTuning> _lengths = new();

        public int ElementCount => _elements.Count;
        public int VerbCount => _verbs.Count;
        public int AdjectiveCount => _adjectives.Count;
        public int CoreCount
        {
            get
            {
                int n = 0;
                foreach (var e in _elements.Values)
                    if (e.Type == "core") n++;
                return n;
            }
        }

        public static SkillMotor CreateDefault() => FromJson(EmbeddedFallback.Json);

        public static SkillMotor FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON boş.", nameof(json));

            JsonValue root = MiniJson.Parse(json);
            var motor = new SkillMotor();
            ParseElements(root, motor._elements);
            ParseVerbs(root, motor._verbs);
            ParseAdjectives(root, motor._adjectives);
            ParseLengths(root, motor._lengths);
            if (motor.CoreCount < 6)
                throw new InvalidOperationException("element-sistemi: 6 çekirdek element beklenir.");
            return motor;
        }

        public bool TryGetElement(string id, out ElementNode node) =>
            _elements.TryGetValue(id, out node);

        public bool TryGetVerb(string id, out VerbNode node) =>
            _verbs.TryGetValue(id, out node);

        public bool TryGetAdjective(string id, out AdjectiveNode node) =>
            _adjectives.TryGetValue(id, out node);

        public string CoreName(int id) =>
            _elements.TryGetValue(id.ToString(CultureInfo.InvariantCulture), out ElementNode e)
                ? e.Name
                : $"#{id}";

        /// <summary>
        /// Katlama grameri (sahip):
        /// 1 = kök skill (fiil+sıfat).
        /// 2 = bileşik satırının skill_name kartı (yalnızca cümle burada bitince).
        /// 3 = (ilk ikinin bileşik elementi) + 3. kökün sıfatı — 2'li skill kartı taşınmaz.
        /// 4 = (1-2 bileşik) + (3-4 bileşik) — bileşik×bileşik; yine 2'li kart değil.
        /// </summary>
        public SkillResolution Resolve(IReadOnlyList<int> runeIds)
        {
            if (runeIds == null || runeIds.Count == 0)
                return SkillResolution.Empty;

            int len = runeIds.Count;
            if (len > 4)
                return SkillResolution.Empty;

            _lengths.TryGetValue(len, out LengthTuning length);
            if (len == 1)
                length = new LengthTuning(1, "Tekil", 1f, 1f, 1f, 1f, "free_move");

            string verbId;
            string adjectiveId;
            string elementId;
            string elementName;
            string displayName;
            string skillId;
            string skillJob;
            string flavor = string.Empty;

            if (len == 1)
            {
                if (!TryCore(runeIds[0], out ElementNode core))
                    return SkillResolution.Empty;

                // Kök skill — bileşik yok; skill_name yoksa element adı.
                verbId = core.VerbId;
                adjectiveId = core.AdjectiveId;
                elementId = core.Id;
                elementName = core.Name;
                displayName = !string.IsNullOrEmpty(core.SkillName) ? core.SkillName : core.Name;
                skillId = !string.IsNullOrEmpty(core.SkillId) ? core.SkillId : "core:" + core.Id;
                skillJob = core.SkillJob;
            }
            else if (len == 2)
            {
                // Tam 2'li: bileşik element satırındaki SKILL kartı.
                ElementNode compound = ResolveCompound(runeIds[0], runeIds[1]);
                verbId = compound.VerbId;
                adjectiveId = compound.AdjectiveId;
                elementId = compound.Id;
                elementName = compound.Name;
                displayName = !string.IsNullOrEmpty(compound.SkillName)
                    ? compound.SkillName
                    : compound.Name;
                skillId = !string.IsNullOrEmpty(compound.SkillId)
                    ? compound.SkillId
                    : "pair:" + compound.Id;
                skillJob = compound.SkillJob;
            }
            else if (len == 3)
            {
                // (bileşik) + 3. kök sıfatı — bileşik element, 2'li skill değil.
                ElementNode left = ResolveCompound(runeIds[0], runeIds[1]);
                if (!TryCore(runeIds[2], out ElementNode third))
                    return SkillResolution.Empty;

                verbId = left.VerbId;
                adjectiveId = third.AdjectiveId;
                elementId = left.Id + "+" + third.Id;
                elementName = left.Name;
                _adjectives.TryGetValue(adjectiveId, out AdjectiveNode thirdAdj);
                string adjLabel = !string.IsNullOrEmpty(thirdAdj.Name) ? thirdAdj.Name : third.Name;
                displayName = left.Name + " · " + adjLabel;
                skillId = "fold3:" + left.Id + ":" + third.Id;
                skillJob = left.Name + " + " + third.Name + " sıfatı";
            }
            else // len == 4
            {
                // (bileşik) + (bileşik) — sol fiil, sağ sıfat.
                ElementNode left = ResolveCompound(runeIds[0], runeIds[1]);
                ElementNode right = ResolveCompound(runeIds[2], runeIds[3]);
                verbId = left.VerbId;
                adjectiveId = right.AdjectiveId;
                elementId = left.Id + "+" + right.Id;
                elementName = left.Name + "+" + right.Name;
                displayName = left.Name + "+" + right.Name;
                skillId = "fold4:" + left.Id + ":" + right.Id;
                skillJob = left.Name + " fiili + " + right.Name + " sıfatı";
            }

            _verbs.TryGetValue(verbId, out VerbNode verb);
            _adjectives.TryGetValue(adjectiveId, out AdjectiveNode adj);
            string[] mechanics = verb.Mechanics ?? Array.Empty<string>();

            return new SkillResolution(
                elementId: elementId,
                elementName: elementName,
                displayName: displayName,
                skillId: skillId,
                skillJob: skillJob,
                verbId: verbId,
                verbName: verb.Name ?? verbId,
                verbFamily: verb.Family ?? string.Empty,
                action: verb.Action ?? string.Empty,
                baseDamage: verb.BaseDamage,
                basePoise: verb.BasePoise,
                hitbox: verb.Hitbox ?? string.Empty,
                castMobility: verb.CastMobility ?? "free_move",
                mechanics: mechanics,
                adjectiveId: adjectiveId,
                adjectiveName: adj.Name ?? adjectiveId,
                silhouetteAxis: adj.SilhouetteAxis ?? string.Empty,
                damageMult: adj.DamageMult,
                hitboxScaleMult: adj.HitboxScaleMult,
                poiseDamageMult: adj.PoiseDamageMult,
                length: len,
                lengthRole: length.Role ?? string.Empty,
                lengthCastMult: length.CastTimeMult,
                lengthMobility: length.Mobility ?? "free_move",
                flavorElement: flavor,
                animationType: verb.AnimationType ?? string.Empty,
                targetMode: verb.TargetMode ?? string.Empty,
                baseCooldownSec: verb.BaseCooldownSec,
                baseResourceCost: verb.BaseResourceCost,
                targetBehaviors: verb.TargetBehaviors,
                special: verb.Special,
                zoneEffect: verb.ZoneEffect,
                engineModifiers: adj.EngineModifiers);
        }

        /// <summary>İki kök → bileşik ELEMENT (skill kartı değil). Tabloda yoksa sentetik.</summary>
        ElementNode ResolveCompound(int a, int b)
        {
            string pair = PairKey(a, b);
            if (_elements.TryGetValue(pair, out ElementNode compound) && compound.Type == "compound")
                return compound;

            TryCore(a, out ElementNode c0);
            TryCore(b, out ElementNode c1);
            return new ElementNode(
                pair,
                CoreName(a) + "+" + CoreName(b),
                "compound",
                c0.VerbId ?? string.Empty,
                c1.AdjectiveId ?? string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                JsonValue.Null,
                JsonValue.Null,
                JsonValue.Null);
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

        bool TryCore(int id, out ElementNode node) =>
            _elements.TryGetValue(id.ToString(CultureInfo.InvariantCulture), out node)
            && node.Type == "core";

        static string PairKey(int a, int b) =>
            a.ToString(CultureInfo.InvariantCulture) + "-" + b.ToString(CultureInfo.InvariantCulture);

        static void ParseElements(JsonValue root, Dictionary<string, ElementNode> dst)
        {
            foreach (JsonValue obj in root["elements"].AsArray())
            {
                string id = obj["id"].AsString();
                if (string.IsNullOrEmpty(id)) continue;
                dst[id] = new ElementNode(
                    id,
                    obj["name"].AsString(),
                    obj["type"].AsString(),
                    obj["verb_id"].AsString(),
                    obj["adjective_id"].AsString(),
                    obj["skill_id"].AsString(),
                    obj["skill_name"].AsString(),
                    obj["skill_job"].AsString(),
                    obj["identity"].AsString(),
                    obj["special_mechanics"],
                    obj["zone_effect"],
                    obj);
            }
        }

        static void ParseVerbs(JsonValue root, Dictionary<string, VerbNode> dst)
        {
            foreach (JsonValue obj in root["verbs"].AsArray())
            {
                string id = obj["id"].AsString();
                if (string.IsNullOrEmpty(id)) continue;
                JsonValue stats = obj["engine_base_stats"];
                dst[id] = new VerbNode(
                    id,
                    obj["name"].AsString(),
                    obj["family"].AsString(),
                    stats["action"].AsString(),
                    stats["base_damage_value"].AsFloat(),
                    stats["base_poise_damage"].AsFloat(),
                    stats["base_hitbox"].AsString(),
                    stats["cast_mobility"].AsString(),
                    ReadStringArray(obj["mechanics"]),
                    obj["animation_type"].AsString(),
                    obj["target_mode"].AsString(),
                    obj["base_cooldown_sec"].AsFloat(),
                    obj["base_resource_cost"].AsFloat(),
                    ReadTargetBehaviors(obj["target_behaviors"]),
                    obj["special"],
                    obj["zone_effect"],
                    obj);
            }
        }

        static void ParseAdjectives(JsonValue root, Dictionary<string, AdjectiveNode> dst)
        {
            foreach (JsonValue obj in root["adjectives"].AsArray())
            {
                string id = obj["id"].AsString();
                if (string.IsNullOrEmpty(id)) continue;
                JsonValue mods = obj["engine_modifiers"];
                dst[id] = new AdjectiveNode(
                    id,
                    obj["name"].AsString(),
                    obj["category"].AsString(),
                    obj["silhouette_axis"].AsString(),
                    mods["damage_mult"].AsFloat(1f),
                    mods["hitbox_scale_mult"].AsFloat(1f),
                    mods["poise_damage_mult"].AsFloat(1f),
                    mods,
                    obj);
            }
        }

        static void ParseLengths(JsonValue root, Dictionary<int, LengthTuning> dst)
        {
            JsonValue block = root["scaling_economy"]["lengths"];
            for (int n = 2; n <= 4; n++)
            {
                JsonValue obj = block[n.ToString(CultureInfo.InvariantCulture)];
                if (obj.IsNull) continue;
                dst[n] = new LengthTuning(
                    n,
                    obj["role"].AsString(),
                    obj["cast_time_mult"].AsFloat(1f),
                    obj["resource_cost_mult"].AsFloat(1f),
                    obj["damage_mult"].AsFloat(1f),
                    obj["poise_damage_mult"].AsFloat(1f),
                    obj["mobility"].AsString());
            }
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

        /// <summary>
        /// target_behaviors sürümden sürüme şekil değiştiriyor: düz string ya da
        /// {"read_as": "..."} nesnesi. self/enemy/ally anahtarlarını tek metne indirger.
        /// </summary>
        static IReadOnlyDictionary<string, string> ReadTargetBehaviors(JsonValue obj)
        {
            if (obj.Kind != JsonKind.Object) return EmptyBehaviors;
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in obj.AsObject())
                dict[kv.Key] = kv.Value.AsTextOrField("read_as", kv.Value.AsString());
            return dict;
        }

        static readonly IReadOnlyDictionary<string, string> EmptyBehaviors =
            new Dictionary<string, string>(StringComparer.Ordinal);
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
            JsonValue raw)
        {
            Id = id; Name = name; Family = family; Action = action;
            BaseDamage = baseDamage; BasePoise = basePoise; Hitbox = hitbox;
            CastMobility = castMobility; Mechanics = mechanics;
            AnimationType = animationType; TargetMode = targetMode;
            BaseCooldownSec = baseCooldownSec; BaseResourceCost = baseResourceCost;
            TargetBehaviors = targetBehaviors; Special = special; ZoneEffect = zoneEffect;
            Raw = raw;
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
    }

    public readonly struct LengthTuning
    {
        public LengthTuning(
            int length, string role, float castTimeMult, float resourceCostMult,
            float damageMult, float poiseDamageMult, string mobility)
        {
            Length = length; Role = role; CastTimeMult = castTimeMult;
            ResourceCostMult = resourceCostMult; DamageMult = damageMult;
            PoiseDamageMult = poiseDamageMult; Mobility = mobility;
        }

        public int Length { get; }
        public string Role { get; }
        public float CastTimeMult { get; }
        public float ResourceCostMult { get; }
        public float DamageMult { get; }
        public float PoiseDamageMult { get; }
        public string Mobility { get; }
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
            JsonValue? special = null, JsonValue? zoneEffect = null, JsonValue? engineModifiers = null)
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
            FlavorElement = flavorElement ?? string.Empty;
            AnimationType = animationType ?? string.Empty;
            TargetMode = targetMode ?? string.Empty;
            BaseCooldownSec = baseCooldownSec;
            BaseResourceCost = baseResourceCost;
            TargetBehaviors = targetBehaviors ?? EmptyBehaviors;
            Special = special ?? JsonValue.Null;
            ZoneEffect = zoneEffect ?? JsonValue.Null;
            EngineModifiers = engineModifiers ?? JsonValue.Null;
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
    }

    /// <summary>Resources yoksa test/editor yedeği — yalnızca 6 çekirdek iskeleti.</summary>
    static class EmbeddedFallback
    {
        public const string Json = @"{
  ""elements"": [
    {""id"":""1"",""name"":""Ateş"",""type"":""core"",""verb_id"":""saldiri"",""adjective_id"":""yogunlastirma""},
    {""id"":""2"",""name"":""Su"",""type"":""core"",""verb_id"":""iyilestirme"",""adjective_id"":""yayma""},
    {""id"":""3"",""name"":""Hava"",""type"":""core"",""verb_id"":""hareket"",""adjective_id"":""tasima""},
    {""id"":""4"",""name"":""Toprak"",""type"":""core"",""verb_id"":""savunma"",""adjective_id"":""sabitleme""},
    {""id"":""5"",""name"":""Aydınlık"",""type"":""core"",""verb_id"":""arindirma"",""adjective_id"":""saflastirma""},
    {""id"":""6"",""name"":""Karanlık"",""type"":""core"",""verb_id"":""gizlilik"",""adjective_id"":""ortme""},
    {""id"":""1-2"",""name"":""Buhar"",""type"":""compound"",""verb_id"":""gorus_kapatma"",""adjective_id"":""bulandirma"",""skill_id"":""buhar_perdesi"",""skill_name"":""Buhar Perdesi"",""skill_job"":""Kısa süreli görüş engeli bulutu""}
  ],
  ""verbs"": [
    {""id"":""saldiri"",""name"":""Saldırı"",""family"":""strike"",""animation_type"":""cast_projectile"",""target_mode"":""enemy_only"",""base_cooldown_sec"":3,""base_resource_cost"":10,""engine_base_stats"":{""action"":""damage"",""base_damage_value"":40,""base_poise_damage"":15,""base_hitbox"":""projectile"",""cast_mobility"":""free_move""},""mechanics"":[""burn""]},
    {""id"":""iyilestirme"",""name"":""İyileştirme"",""family"":""mend"",""animation_type"":""cast_self"",""target_mode"":""self_or_ally"",""base_cooldown_sec"":6,""base_resource_cost"":12,""engine_base_stats"":{""action"":""heal"",""base_damage_value"":0,""base_poise_damage"":0,""base_hitbox"":""self"",""cast_mobility"":""free_move""},""mechanics"":[""regen""],""special"":{""heal_value"":30,""regen_per_sec"":3,""duration_sec"":5}},
    {""id"":""hareket"",""name"":""Hareket"",""family"":""motion"",""animation_type"":""dash"",""target_mode"":""self_only"",""base_cooldown_sec"":4,""base_resource_cost"":8,""engine_base_stats"":{""action"":""dash"",""base_damage_value"":0,""base_poise_damage"":0,""base_hitbox"":""self"",""cast_mobility"":""free_move""},""mechanics"":[""haste""]},
    {""id"":""savunma"",""name"":""Savunma"",""family"":""guard"",""animation_type"":""cast_self"",""target_mode"":""self_only"",""base_cooldown_sec"":8,""base_resource_cost"":12,""engine_base_stats"":{""action"":""shield"",""base_damage_value"":0,""base_poise_damage"":0,""base_hitbox"":""self"",""cast_mobility"":""slowed_move""},""mechanics"":[""shield""]},
    {""id"":""arindirma"",""name"":""Arındırma"",""family"":""purge"",""animation_type"":""cast_self"",""target_mode"":""self_or_ally"",""base_cooldown_sec"":8,""base_resource_cost"":15,""engine_base_stats"":{""action"":""cleanse"",""base_damage_value"":0,""base_poise_damage"":0,""base_hitbox"":""self"",""cast_mobility"":""free_move""},""mechanics"":[""cleanse""]},
    {""id"":""gizlilik"",""name"":""Gizlilik"",""family"":""special"",""animation_type"":""dash"",""target_mode"":""selective"",""base_cooldown_sec"":8,""base_resource_cost"":18,""target_behaviors"":{""self"":""Görünmez ol"",""enemy"":""Boyut kesme"",""ally"":""Gölge çekme""},""engine_base_stats"":{""action"":""stealth"",""base_damage_value"":0,""base_poise_damage"":0,""base_hitbox"":""self"",""cast_mobility"":""free_move""},""mechanics"":[""fear""]},
    {""id"":""gorus_kapatma"",""name"":""Görüş Kapatma"",""family"":""disrupt"",""animation_type"":""cast_aoe"",""target_mode"":""enemy_only"",""base_cooldown_sec"":8,""engine_base_stats"":{""action"":""blind_area"",""base_damage_value"":0,""base_poise_damage"":0,""base_hitbox"":""static_cloud"",""cast_mobility"":""slowed_move""},""mechanics"":[""blind""]}
  ],
  ""adjectives"": [
    {""id"":""yogunlastirma"",""name"":""Yoğunlaştırma"",""category"":""quality"",""silhouette_axis"":""focus"",""engine_modifiers"":{""damage_mult"":0.85,""hitbox_scale_mult"":0.4,""poise_damage_mult"":1.3}},
    {""id"":""yayma"",""name"":""Yayma"",""category"":""scope"",""silhouette_axis"":""spread"",""engine_modifiers"":{""damage_mult"":0.9,""hitbox_scale_mult"":1.4,""poise_damage_mult"":0.9}},
    {""id"":""tasima"",""name"":""Taşıma"",""category"":""position"",""silhouette_axis"":""pierce"",""engine_modifiers"":{""damage_mult"":0.9,""hitbox_scale_mult"":1.0,""poise_damage_mult"":1.0}},
    {""id"":""sabitleme"",""name"":""Sabitleme"",""category"":""duration"",""silhouette_axis"":""lift"",""engine_modifiers"":{""damage_mult"":0.85,""hitbox_scale_mult"":1.1,""poise_damage_mult"":1.2}},
    {""id"":""saflastirma"",""name"":""Saflaştırma"",""category"":""quality"",""silhouette_axis"":""focus"",""engine_modifiers"":{""damage_mult"":0.9,""hitbox_scale_mult"":1.0,""poise_damage_mult"":1.0}},
    {""id"":""ortme"",""name"":""Örtme"",""category"":""scope"",""silhouette_axis"":""spread"",""engine_modifiers"":{""damage_mult"":0.85,""hitbox_scale_mult"":1.2,""poise_damage_mult"":0.8}},
    {""id"":""bulandirma"",""name"":""Bulandırma"",""category"":""quality"",""silhouette_axis"":""spread"",""engine_modifiers"":{""damage_mult"":0.8,""hitbox_scale_mult"":1.3,""poise_damage_mult"":0.7}}
  ],
  ""scaling_economy"": {
    ""lengths"": {
      ""2"":{""role"":""Temel"",""cast_time_mult"":1.0,""resource_cost_mult"":1.0,""damage_mult"":1.0,""poise_damage_mult"":1.0,""mobility"":""free_move""},
      ""3"":{""role"":""Durumsal"",""cast_time_mult"":1.4,""resource_cost_mult"":1.5,""damage_mult"":1.0,""poise_damage_mult"":1.0,""mobility"":""slowed_move""},
      ""4"":{""role"":""Dar Cevap"",""cast_time_mult"":2.0,""resource_cost_mult"":2.2,""damage_mult"":1.0,""poise_damage_mult"":1.0,""mobility"":""rooted""}
    }
  }
}";
    }
}
