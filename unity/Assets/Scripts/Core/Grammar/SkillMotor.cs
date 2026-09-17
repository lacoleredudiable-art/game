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
        readonly List<ActiveModeNode> _activeModes = new();
        readonly List<PassiveNode> _passives = new();
        readonly List<ChainNode> _chains = new();
        readonly List<StatusInteractionNode> _statusInteractions = new();
        readonly List<ZoneNode> _zones = new();
        readonly List<SpaceEffectNode> _spaceEffects = new();
        readonly List<TimeEffectNode> _timeEffects = new();
        readonly List<PlayerStateNode> _playerStates = new();
        readonly List<BossStateNode> _bossStates = new();
        int _maxActiveZones;
        int _maxActiveLinks;
        int _maxActiveTimeFields;

        public int ElementCount => _elements.Count;
        public int VerbCount => _verbs.Count;
        public int AdjectiveCount => _adjectives.Count;

        /// <summary>
        /// docs/element-sistemi.json "active_modes" (ulti): aynı elementin 4'lüsü (X-X-X-X).
        /// 16 Eylül: "v5.2.1 hiç aktif olmadı" güven kaygısına karşılık motor artık bunu da
        /// okuyor — tetikleme/efekt uygulaması Core/Combat/ActiveModeDirector işi.
        /// </summary>
        public IReadOnlyList<ActiveModeNode> ActiveModes => _activeModes;

        /// <summary>docs/element-sistemi.json passives.list — yalnızca okuma; uygulama ayrı görev.</summary>
        public IReadOnlyList<PassiveNode> Passives => _passives;

        /// <summary>docs/element-sistemi.json chain_mechanics.chains — yalnızca okuma.</summary>
        public IReadOnlyList<ChainNode> Chains => _chains;

        /// <summary>
        /// status_interaction_table altındaki tüm kategori dizileri düz liste.
        /// StatusReactionTable.Rebuild bunu okuyup genellenebilir kuralları üretir.
        /// </summary>
        public IReadOnlyList<StatusInteractionNode> StatusInteractions => _statusInteractions;

        /// <summary>manipulation_layers.zone_layer.zones — yalnızca okuma.</summary>
        public IReadOnlyList<ZoneNode> Zones => _zones;

        /// <summary>manipulation_layers.zone_layer.max_active_zones.</summary>
        public int MaxActiveZones => _maxActiveZones;

        /// <summary>manipulation_layers.space_layer.effects — yalnızca okuma; uygulama ayrı.</summary>
        public IReadOnlyList<SpaceEffectNode> SpaceEffects => _spaceEffects;

        /// <summary>manipulation_layers.space_layer.max_active_links.</summary>
        public int MaxActiveLinks => _maxActiveLinks;

        /// <summary>manipulation_layers.time_layer.effects — yalnızca okuma; uygulama Bağlama 8.</summary>
        public IReadOnlyList<TimeEffectNode> TimeEffects => _timeEffects;

        /// <summary>manipulation_layers.time_layer.max_active_fields.</summary>
        public int MaxActiveTimeFields => _maxActiveTimeFields;

        /// <summary>
        /// docs/element-sistemi.json state_machine.player_states — yalnızca okuma.
        /// Runtime geçişler Core/Combat/PlayerStateMachine; SentencePhase'e bağlanmadı.
        /// </summary>
        public IReadOnlyList<PlayerStateNode> PlayerStates => _playerStates;

        /// <summary>docs/element-sistemi.json state_machine.boss_states — yalnızca okuma.</summary>
        public IReadOnlyList<BossStateNode> BossStates => _bossStates;
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
            ParseActiveModes(root, motor._activeModes);
            ParsePassives(root, motor._passives);
            ParseChains(root, motor._chains);
            ParseStatusInteractions(root, motor._statusInteractions);
            motor._maxActiveZones = ParseZones(root, motor._zones);
            motor._maxActiveLinks = ParseSpaceEffects(root, motor._spaceEffects);
            motor._maxActiveTimeFields = ParseTimeEffects(root, motor._timeEffects);
            ParseStateMachine(root, motor._playerStates, motor._bossStates);
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
                engineModifiers: adj.EngineModifiers,
                critEligible: verb.CritEligible,
                elementOrigin: verb.ElementOrigin,
                damageType: verb.DamageType,
                lengthResourceCostMult: length.ResourceCostMult);
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
                    obj,
                    obj["crit_eligible"].AsBool(false),
                    obj["element_origin"].AsString(),
                    stats["damage_type"].AsString());
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

        /// <summary>
        /// active_modes.modes — 6 element × 1 ulti. `cost` alanı sürümde ya düz metin
        /// ("hareket edemezsin") ya nesne ({"hp_per_sec_percent":3}); ikisi de burada okunur.
        /// Sayı uydurma yok: "savunma %50 düşer" gibi metinlerden çarpan metnin İÇİNDEKİ
        /// yüzdeden türetilir (bkz. ParseDefenseDropMult), elle bir sabit yazılmadı.
        /// </summary>
        static void ParseActiveModes(JsonValue root, List<ActiveModeNode> dst)
        {
            foreach (JsonValue obj in root["active_modes"]["modes"].AsArray())
            {
                string id = obj["id"].AsString();
                if (string.IsNullOrEmpty(id)) continue;

                IReadOnlyList<JsonValue> combo = obj["trigger_combo"].AsArray();
                int triggerDot = combo.Count > 0 ? combo[0].AsInt() : 0;

                JsonValue durationVal = obj["duration_sec"];
                JsonValue cond = obj["activation_condition"];
                JsonValue decond = obj["deactivation_condition"];
                JsonValue cost = obj["cost"];
                string costText = cost.Kind == JsonKind.String ? cost.AsString() : string.Empty;

                dst.Add(new ActiveModeNode(
                    id: id,
                    name: obj["name"].AsString(),
                    element: obj["element"].AsString(),
                    triggerDot: triggerDot,
                    hasDuration: durationVal.Kind == JsonKind.Number,
                    durationSec: durationVal.AsFloat(0f),
                    cooldownSec: obj["cooldown_sec"].AsFloat(0f),
                    resourceCost: obj["resource_cost"].AsFloat(0f),
                    readAs: obj["read_as"].AsString(),
                    activationType: cond["type"].AsString(),
                    activationWithinSec: cond["within_sec"].AsFloat(0f),
                    activationThreshold: cond.Has("threshold") ? cond["threshold"].AsFloat(0f) : cond["hp_below"].AsFloat(0f),
                    activationMinCount: cond["min_count"].AsInt(0),
                    deactivationType: decond["type"].AsString(),
                    deactivationThreshold: decond["threshold"].AsFloat(0f),
                    blocksMovement: costText.Contains("hareket edemezsin"),
                    hpPerSecPercentCost: cost.Kind == JsonKind.Object ? cost["hp_per_sec_percent"].AsFloat(0f) : 0f,
                    defenseDropMult: ParseDefenseDropMult(costText),
                    healBreaksMode: costText.Contains("healer") || costText.Contains("iyileş"),
                    effects: obj["effects"]));
            }
        }

        /// <summary>"savunma %50 düşer" → 1.5f. Yüzde bulunamazsa 1f (etkisiz).</summary>
        static float ParseDefenseDropMult(string costText)
        {
            if (string.IsNullOrEmpty(costText) || !costText.Contains("savunma"))
                return 1f;
            int i = 0;
            while (i < costText.Length && !char.IsDigit(costText[i])) i++;
            int start = i;
            while (i < costText.Length && char.IsDigit(costText[i])) i++;
            if (i == start) return 1f;
            int percent = int.Parse(costText.Substring(start, i - start), CultureInfo.InvariantCulture);
            return 1f + percent / 100f;
        }

        /// <summary>passives.list — tetik/efekt uygulaması ayrı görev; burada yalnızca okuma.</summary>
        static void ParsePassives(JsonValue root, List<PassiveNode> dst)
        {
            foreach (JsonValue obj in root["passives"]["list"].AsArray())
            {
                string id = obj["id"].AsString();
                if (string.IsNullOrEmpty(id)) continue;

                IReadOnlyList<JsonValue> comboArr = obj["trigger_combo"].AsArray();
                var combo = new int[comboArr.Count];
                for (int i = 0; i < comboArr.Count; i++)
                    combo[i] = comboArr[i].AsInt();

                dst.Add(new PassiveNode(
                    id: id,
                    element: obj["element"].AsString(),
                    triggerCombo: combo,
                    durationSec: obj["duration_sec"].AsFloat(0f),
                    effects: obj["effects"]));
            }
        }

        /// <summary>chain_mechanics.chains</summary>
        static void ParseChains(JsonValue root, List<ChainNode> dst)
        {
            foreach (JsonValue obj in root["chain_mechanics"]["chains"].AsArray())
            {
                string element = obj["element"].AsString();
                if (string.IsNullOrEmpty(element)) continue;

                IReadOnlyList<JsonValue> linkArr = obj["links"].AsArray();
                var links = new float[linkArr.Count];
                for (int i = 0; i < linkArr.Count; i++)
                    links[i] = linkArr[i].AsFloat();

                dst.Add(new ChainNode(
                    element: element,
                    pattern: obj["pattern"].AsString(),
                    effect: obj["effect"].AsString(),
                    links: links,
                    finisher: obj["finisher"].AsString()));
            }
        }

        /// <summary>
        /// status_interaction_table — description/binding gibi meta alanları atlar;
        /// dizi olan her kategoriyi düz listeye toplar (anahtar adları JSON'dan).
        /// </summary>
        static void ParseStatusInteractions(JsonValue root, List<StatusInteractionNode> dst)
        {
            JsonValue table = root["status_interaction_table"];
            if (table.Kind != JsonKind.Object) return;

            foreach (var kv in table.AsObject())
            {
                if (kv.Value.Kind != JsonKind.Array) continue;
                foreach (JsonValue obj in kv.Value.AsArray())
                {
                    string name = obj["name"].AsString();
                    if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(obj["a"].AsString()))
                        continue;
                    dst.Add(new StatusInteractionNode(
                        a: obj["a"].AsString(),
                        b: obj["b"].AsString(),
                        name: name,
                        effect: obj["effect"].AsString(),
                        readAs: obj["read_as"].AsString()));
                }
            }
        }

        /// <summary>
        /// manipulation_layers.zone_layer.zones + max_active_zones.
        /// Dönüş: max_active_zones (yoksa 0).
        /// </summary>
        static int ParseZones(JsonValue root, List<ZoneNode> dst)
        {
            JsonValue layer = root["manipulation_layers"]["zone_layer"];
            foreach (JsonValue obj in layer["zones"].AsArray())
            {
                string id = obj["id"].AsString();
                if (string.IsNullOrEmpty(id)) continue;
                dst.Add(new ZoneNode(
                    id: id,
                    element: obj["element"].AsString(),
                    movement: obj["movement"].AsString(),
                    durationSec: obj["duration_sec"].AsFloat(0f),
                    manipulation: obj.Has("manipulation") ? obj["manipulation"] : JsonValue.Null));
            }

            return layer["max_active_zones"].AsInt(0);
        }

        /// <summary>
        /// manipulation_layers.space_layer.effects + max_active_links.
        /// Tipine göre alanlar opsiyonel (distance_m / i_frame_ms / …) — Has ile okunur.
        /// Dönüş: max_active_links (yoksa 0). Uygulama yok; SkillMotionMotor hâlâ tuning sabitleri.
        /// </summary>
        static int ParseSpaceEffects(JsonValue root, List<SpaceEffectNode> dst)
        {
            JsonValue layer = root["manipulation_layers"]["space_layer"];
            foreach (JsonValue obj in layer["effects"].AsArray())
            {
                string id = obj["id"].AsString();
                if (string.IsNullOrEmpty(id)) continue;
                dst.Add(new SpaceEffectNode(
                    id: id,
                    element: obj["element"].AsString(),
                    type: obj["type"].AsString(),
                    hasDistanceM: obj.Has("distance_m"),
                    distanceM: obj["distance_m"].AsFloat(0f),
                    hasIFrameMs: obj.Has("i_frame_ms"),
                    iFrameMs: obj["i_frame_ms"].AsInt(0),
                    hasDamageOnPass: obj.Has("damage_on_pass"),
                    damageOnPass: obj["damage_on_pass"].AsBool(false),
                    hasDamageOnCross: obj.Has("damage_on_cross"),
                    damageOnCross: obj["damage_on_cross"].AsFloat(0f),
                    hasDurationSec: obj.Has("duration_sec"),
                    durationSec: obj["duration_sec"].AsFloat(0f)));
            }
            return layer["max_active_links"].AsInt(0);
        }

        /// <summary>
        /// manipulation_layers.time_layer.effects + max_active_fields.
        /// Tipine göre alanlar opsiyonel (delay_sec / damage_ratio / multiplier).
        /// Dönüş: max_active_fields (yoksa 0).
        /// </summary>
        static int ParseTimeEffects(JsonValue root, List<TimeEffectNode> dst)
        {
            JsonValue layer = root["manipulation_layers"]["time_layer"];
            foreach (JsonValue obj in layer["effects"].AsArray())
            {
                string id = obj["id"].AsString();
                if (string.IsNullOrEmpty(id)) continue;
                dst.Add(new TimeEffectNode(
                    id: id,
                    element: obj["element"].AsString(),
                    type: obj["type"].AsString(),
                    hasDelaySec: obj.Has("delay_sec"),
                    delaySec: obj["delay_sec"].AsFloat(0f),
                    hasDamageRatio: obj.Has("damage_ratio"),
                    damageRatio: obj["damage_ratio"].AsFloat(0f),
                    hasMultiplier: obj.Has("multiplier"),
                    multiplier: obj["multiplier"].AsFloat(0f)));
            }
            return layer["max_active_fields"].AsInt(0);
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
                    note: obj["note"].AsString()));
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
    }

    /// <summary>docs/element-sistemi.json active_modes.modes[i] — bkz. SkillMotor.ParseActiveModes.</summary>
    public readonly struct ActiveModeNode
    {
        public ActiveModeNode(
            string id, string name, string element, int triggerDot,
            bool hasDuration, float durationSec, float cooldownSec, float resourceCost, string readAs,
            string activationType, float activationWithinSec, float activationThreshold, int activationMinCount,
            string deactivationType, float deactivationThreshold,
            bool blocksMovement, float hpPerSecPercentCost, float defenseDropMult, bool healBreaksMode,
            JsonValue effects)
        {
            Id = id; Name = name; Element = element; TriggerDot = triggerDot;
            HasDuration = hasDuration; DurationSec = durationSec; CooldownSec = cooldownSec;
            ResourceCost = resourceCost; ReadAs = readAs;
            ActivationType = activationType; ActivationWithinSec = activationWithinSec;
            ActivationThreshold = activationThreshold; ActivationMinCount = activationMinCount;
            DeactivationType = deactivationType; DeactivationThreshold = deactivationThreshold;
            BlocksMovement = blocksMovement; HpPerSecPercentCost = hpPerSecPercentCost;
            DefenseDropMult = defenseDropMult; HealBreaksMode = healBreaksMode;
            Effects = effects;
        }

        public string Id { get; }
        public string Name { get; }
        public string Element { get; }
        /// <summary>1-6: aynı elementin 4 katı (X-X-X-X). trigger_combo[0]'dan türetilir.</summary>
        public int TriggerDot { get; }
        public bool HasDuration { get; }
        public float DurationSec { get; }
        public float CooldownSec { get; }
        public float ResourceCost { get; }
        public string ReadAs { get; }
        public string ActivationType { get; }
        public float ActivationWithinSec { get; }
        public float ActivationThreshold { get; }
        public int ActivationMinCount { get; }
        public string DeactivationType { get; }
        public float DeactivationThreshold { get; }
        /// <summary>cost metni "hareket edemezsin" içeriyorsa true (Kutsal Kaynak, Aşılmaz Duvar).</summary>
        public bool BlocksMovement { get; }
        /// <summary>cost.hp_per_sec_percent (Öfke Patlaması). Yoksa 0.</summary>
        public float HpPerSecPercentCost { get; }
        /// <summary>cost metnindeki "savunma %N düşer" → 1+N/100 (Fırtına Akışı). Yoksa 1.</summary>
        public float DefenseDropMult { get; }
        /// <summary>cost metni "healer iyileştirirse biter" (Kan Çılgınlığı).</summary>
        public bool HealBreaksMode { get; }
        public JsonValue Effects { get; }

        public float GetEffect(string key, float fallback = 0f) => Effects[key].AsFloat(fallback);
        public bool GetEffectBool(string key, bool fallback = false) => Effects[key].AsBool(fallback);
    }

    /// <summary>docs/element-sistemi.json passives.list[i] — bkz. SkillMotor.ParsePassives.</summary>
    public readonly struct PassiveNode
    {
        public PassiveNode(
            string id, string element, int[] triggerCombo, float durationSec, JsonValue effects)
        {
            Id = id ?? string.Empty;
            Element = element ?? string.Empty;
            TriggerCombo = triggerCombo ?? Array.Empty<int>();
            DurationSec = durationSec;
            Effects = effects;
        }

        public string Id { get; }
        public string Element { get; }
        public int[] TriggerCombo { get; }
        public float DurationSec { get; }
        public JsonValue Effects { get; }

        public float GetEffect(string key, float fallback = 0f) => Effects[key].AsFloat(fallback);
        public bool GetEffectBool(string key, bool fallback = false) => Effects[key].AsBool(fallback);
    }

    /// <summary>docs/element-sistemi.json chain_mechanics.chains[i]</summary>
    public readonly struct ChainNode
    {
        public ChainNode(
            string element, string pattern, string effect, float[] links, string finisher)
        {
            Element = element ?? string.Empty;
            Pattern = pattern ?? string.Empty;
            Effect = effect ?? string.Empty;
            Links = links ?? Array.Empty<float>();
            Finisher = finisher ?? string.Empty;
        }

        public string Element { get; }
        public string Pattern { get; }
        public string Effect { get; }
        public float[] Links { get; }
        public string Finisher { get; }
    }

    /// <summary>
    /// status_interaction_table altındaki bir satır (kategori fark etmeksizin düz liste).
    /// </summary>
    public readonly struct StatusInteractionNode
    {
        public StatusInteractionNode(
            string a, string b, string name, string effect, string readAs)
        {
            A = a ?? string.Empty;
            B = b ?? string.Empty;
            Name = name ?? string.Empty;
            Effect = effect ?? string.Empty;
            ReadAs = readAs ?? string.Empty;
        }

        public string A { get; }
        public string B { get; }
        public string Name { get; }
        public string Effect { get; }
        public string ReadAs { get; }
    }

    /// <summary>manipulation_layers.zone_layer.zones[i]</summary>
    public readonly struct ZoneNode
    {
        public ZoneNode(
            string id, string element, string movement, float durationSec,
            JsonValue? manipulation = null)
        {
            Id = id ?? string.Empty;
            Element = element ?? string.Empty;
            Movement = movement ?? string.Empty;
            DurationSec = durationSec;
            Manipulation = manipulation ?? JsonValue.Null;
        }

        public string Id { get; }
        public string Element { get; }
        public string Movement { get; }
        public float DurationSec { get; }
        /// <summary>Bazı zonelerde var (lav_halkasi); yoksa Null.</summary>

        public JsonValue Manipulation { get; }
    }

    /// <summary>
    /// manipulation_layers.space_layer.effects[i]. Tipine göre alanlar opsiyonel —
    /// Has* bayrakları JSON'da anahtarın varlığını gösterir (0/false ≠ yok).
    /// </summary>
    public readonly struct SpaceEffectNode
    {
        public SpaceEffectNode(
            string id, string element, string type,
            bool hasDistanceM, float distanceM,
            bool hasIFrameMs, int iFrameMs,
            bool hasDamageOnPass, bool damageOnPass,
            bool hasDamageOnCross, float damageOnCross,
            bool hasDurationSec, float durationSec)
        {
            Id = id ?? string.Empty;
            Element = element ?? string.Empty;
            Type = type ?? string.Empty;
            HasDistanceM = hasDistanceM;
            DistanceM = distanceM;
            HasIFrameMs = hasIFrameMs;
            IFrameMs = iFrameMs;
            HasDamageOnPass = hasDamageOnPass;
            DamageOnPass = damageOnPass;
            HasDamageOnCross = hasDamageOnCross;
            DamageOnCross = damageOnCross;
            HasDurationSec = hasDurationSec;
            DurationSec = durationSec;
        }

        public string Id { get; }
        public string Element { get; }
        public string Type { get; }
        public bool HasDistanceM { get; }
        public float DistanceM { get; }
        public bool HasIFrameMs { get; }
        public int IFrameMs { get; }
        public bool HasDamageOnPass { get; }
        public bool DamageOnPass { get; }
        public bool HasDamageOnCross { get; }
        public float DamageOnCross { get; }
        public bool HasDurationSec { get; }
        public float DurationSec { get; }
    }

    /// <summary>
    /// manipulation_layers.time_layer.effects[i]. Tipine göre alanlar opsiyonel —
    /// Has* bayrakları JSON'da anahtarın varlığını gösterir.
    /// </summary>
    public readonly struct TimeEffectNode
    {
        public TimeEffectNode(
            string id, string element, string type,
            bool hasDelaySec, float delaySec,
            bool hasDamageRatio, float damageRatio,
            bool hasMultiplier, float multiplier)
        {
            Id = id ?? string.Empty;
            Element = element ?? string.Empty;
            Type = type ?? string.Empty;
            HasDelaySec = hasDelaySec;
            DelaySec = delaySec;
            HasDamageRatio = hasDamageRatio;
            DamageRatio = damageRatio;
            HasMultiplier = hasMultiplier;
            Multiplier = multiplier;
        }

        public string Id { get; }
        public string Element { get; }
        public string Type { get; }
        public bool HasDelaySec { get; }
        public float DelaySec { get; }
        public bool HasDamageRatio { get; }
        public float DamageRatio { get; }
        public bool HasMultiplier { get; }
        public float Multiplier { get; }
    }

    /// <summary>docs/element-sistemi.json state_machine.player_states[id] — bkz. ParseStateMachine.</summary>
    public readonly struct PlayerStateNode
    {
        public PlayerStateNode(
            string id, string canDraw, string canMove, string canDodge,
            bool iFrames, bool interruptible = false, string note = "")
        {
            Id = id ?? string.Empty;
            CanDraw = canDraw ?? "false";
            CanMove = canMove ?? "false";
            CanDodge = canDodge ?? "false";
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
            JsonValue? special = null, JsonValue? zoneEffect = null, JsonValue? engineModifiers = null,
            bool critEligible = false, string elementOrigin = "", string damageType = "",
            float lengthResourceCostMult = 1f)
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
        public bool CritEligible { get; }
        public string ElementOrigin { get; }
        public string DamageType { get; }
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
