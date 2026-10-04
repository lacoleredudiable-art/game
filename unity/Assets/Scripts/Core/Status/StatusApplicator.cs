using Dovus.Core.Grammar;
using Dovus.Core.Shared;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Status
{
    /// <summary>
    /// SkillResolution.mechanics ÔåÆ StatusBoard. Knockback ayr─▒ bayrak (anl─▒k).
    /// </summary>
    public static class StatusApplicator
    {
        public readonly struct Result
        {
            public Result(bool knockback, bool cleansed, bool pull = false, int cleansedCount = 0)
            {
                CleansedCount = cleansedCount;
                Knockback = knockback;
                Cleansed = cleansed;
                Pull = pull;
            }

            public bool Knockback { get; }
            public bool Pull { get; }
            public bool Cleansed { get; }
            /// <summary>cleanse_count: bu cast'te silinen k├Ât├╝ durum say─▒s─▒.</summary>
            public int CleansedCount { get; }
        }

        /// <summary>
        /// self hitbox ÔåÆ caster board; aksi halde target board.
        /// cleanse: hedefteki d├╝┼şman durumlar─▒ temizlenir (self cleanse = caster).
        /// </summary>
        public static Result ApplySkill(
            SkillResolution skill,
            StatusBoard caster,
            StatusBoard target,
            StatusTuning tuning,
            MobilityCcData? mobilityCc = null,
            float friendlyMagnitude = 1f,
            int cleanseCount = 0)
        {
            if (skill.IsEmpty || tuning == null)
                return new Result(false, false);

            bool self = IsSelfTargeted(skill);
            StatusBoard board = self ? caster : target;
            if (board == null)
                return new Result(false, false);

            bool knockback = false;
            bool pull = false;
            bool cleansed = false;
            int cleansedCount = 0;
            string[] mechanics = skill.Mechanics ?? System.Array.Empty<string>();

            // "Savrulma Sersemli─şi": ayn─▒ vuru┼şta stun + knockback birlikteyse stun s├╝resi uzar
            // (StatusTuning.StunKnockbackDurationAddMs). Knockback kal─▒c─▒ bir status de─şil
            // (anl─▒k bayrak); "ayn─▒ cast" bilgisiyle burada ├Âzel i┼şleniyor.
            bool hasKnockbackThisCast = System.Array.IndexOf(mechanics, "knockback") >= 0;

            for (int i = 0; i < mechanics.Length; i++)
            {
                string id = mechanics[i];
                if (id == "cleanse")
                {
                    int want = cleanseCount > 0
                        ? cleanseCount
                        : skill.EngineModifiers != null && !skill.EngineModifiers.IsNull
                            ? skill.EngineModifiers["cleanse_count"].AsInt(0)
                            : 0;
                    cleansedCount += board.CleanseHostile(want > 0 ? want : int.MaxValue);
                    cleansed = true;
                    continue;
                }

                if (!StatusKindUtil.TryParse(id, out StatusKind kind) || kind == StatusKind.None)
                    continue;

                if (kind == StatusKind.Knockback)
                {
                    knockback = !self;
                    continue;
                }

                if (kind == StatusKind.Stun && hasKnockbackThisCast)
                {
                    board.Apply(kind, tuning.StunMs + tuning.StunKnockbackDurationAddMs, 1f);
                    continue;
                }

                ApplyKind(
                    board, kind, skill, tuning, mobilityCc, ParseAdjectiveId(skill.AdjectiveId),
                    friendlyMagnitude);
            }

            // S─▒fat engine_modifiers ÔÇö fiil mechanics d─▒┼ş─▒nda ek durum (3ÔÇÖl├╝/4ÔÇÖl├╝ fark─▒).
            ApplyAdjectiveModifiers(
                skill, board, caster, target, self, ref knockback, ref pull, tuning, mechanics,
                mobilityCc, ParseAdjectiveId(skill.AdjectiveId), friendlyMagnitude);

            return new Result(knockback, cleansed, pull, cleansedCount);
        }

        /// <summary>
        /// apply_slow / apply_root / apply_burn / apply_poison_on_hit / apply_silence /
        /// apply_knockback / apply_pull / apply_stealth / apply_confuse ÔÇö JSON adjectives.
        /// Fiil mechanicsÔÇÖte zaten varsa tekrar uygulanmaz. D├╝┼şmanca s─▒fat durumlar─▒ kendine
        /// y├Ânelik fiilde de (Hareket/├ça─ş─▒rma/Yans─▒ma) caster'a de─şil vurulan hedefe gider;
        /// hedef yoksa (hi├ğbir ┼şeye de─şmedi) uygulanmaz.
        /// </summary>
        static void ApplyAdjectiveModifiers(
            SkillResolution skill,
            StatusBoard board,
            StatusBoard caster,
            StatusBoard target,
            bool self,
            ref bool knockback,
            ref bool pull,
            StatusTuning tuning,
            string[] mechanics,
            MobilityCcData? mobilityCc,
            int adjectiveId,
            float friendlyMagnitude)
        {
            JsonValue mods = skill.EngineModifiers;
            if (mods.IsNull || mods.Kind != JsonKind.Object)
                return;

            bool HasMech(string id) => System.Array.IndexOf(mechanics, id) >= 0;
            bool hitsEnemy = CardEffectRules.HarmfulHitsEnemy(skill.TargetMode, skill.Action, skill.SkillJob);
            StatusBoard hostile = hitsEnemy ? (self ? target : board) : null;

            if (hostile != null)
                ApplyHostileAdjectiveModifiers(
                    mods, hostile, skill, tuning, mechanics, mobilityCc, adjectiveId);

            if (CardEffectRules.WantsSelfHaste(skill.SkillJob)
                && !string.Equals(skill.Action, "tempo", System.StringComparison.OrdinalIgnoreCase)
                && caster != null)
            {
                float haste = CardEffectRules.HasteMagnitude(
                    skill.SkillJob,
                    mods["self_haste"].AsFloat(0f),
                    mods["enemy_slow"].AsFloat(0f),
                    mods["self_damage_buff"].AsFloat(0f));
                double hasteMs = mods["buff_duration_sec"].AsFloat(0f) * 1000.0;
                if (hasteMs <= 0)
                    hasteMs = tuning.HasteMs;
                if (haste > 1f)
                {
                    float scaled = friendlyMagnitude > 0f ? haste * friendlyMagnitude : haste;
                    caster.Apply(StatusKind.Haste, hasteMs, scaled, EffectSource(skill, "haste"));
                }
            }

            if (ModifierTruthy(mods, "apply_knockback") && !self && !HasMech("knockback"))
                knockback = true;

            if (ModifierTruthy(mods, "apply_pull") && !self)
                pull = true;

            if (mods.Has("apply_damage_reduction") && !HasMech("damage_reduction"))
            {
                float mult = mods["apply_damage_reduction"].AsFloat(tuning.DamageReductionMult);
                if (mult > 0f && mult < 1f)
                    board.Apply(StatusKind.DamageReduction, tuning.DamageReductionMs, mult);
            }

            // gizleme: her zaman caster'a stealth (hedef board self olsa da caster ayn─▒).
            if (ModifierTruthy(mods, "apply_stealth") && !HasMech("stealth") && caster != null)
                caster.Apply(StatusKind.Stealth, tuning.StealthMs, 1f);

            // sasirtma: Confuse kind yok ÔåÆ Blind + Slow (durum.md ├Âncelik 2).
            if (ModifierTruthy(mods, "apply_confuse") && !self)
            {
                if (!HasMech("blind"))
                {
                    // S7: b├╝y├╝kl├╝k 1 = her vuru┼ş ─▒ska idi; accuracy_debuff varsa o, yoksa tuning varsay─▒lan─▒.
                    float confuseAccuracy = mods.Has("accuracy_debuff")
                        ? mods["accuracy_debuff"].AsFloat(0f)
                        : 0f;
                    board.Apply(
                        StatusKind.Blind,
                        mobilityCc?.ResolveCcDurationMs(StatusKind.Blind, adjectiveId, tuning.BlindMs)
                            ?? tuning.BlindMs,
                        StatusMath.BlindChanceFromAccuracy(
                            confuseAccuracy > 0f ? confuseAccuracy : tuning.BlindMissChance));
                }
                if (!HasMech("slow"))
                    board.Apply(
                        StatusKind.Slow,
                        mobilityCc?.ResolveCcDurationMs(StatusKind.Slow, adjectiveId, tuning.SlowMs)
                            ?? tuning.SlowMs,
                        tuning.SlowSpeedMult,
                        EffectSource(skill, "confuse-slow"));
            }
        }

        static void ApplyHostileAdjectiveModifiers(
            JsonValue mods,
            StatusBoard board,
            SkillResolution skill,
            StatusTuning tuning,
            string[] mechanics,
            MobilityCcData? mobilityCc,
            int adjectiveId)
        {
            bool HasMech(string id) => System.Array.IndexOf(mechanics, id) >= 0;
            bool keepEnemyLock = !CardEffectRules.WantsSelfHaste(skill.SkillJob)
                || CardEffectRules.Names(skill.SkillJob, "root");

            if (keepEnemyLock && mods.Has("apply_slow") && !HasMech("slow"))
            {
                float mult = mods["apply_slow"].AsFloat(0f);
                if (mult <= 0f)
                    mult = tuning.SlowSpeedMult;
                if (mult <= 0f || mult > 1f)
                    mult = tuning.SlowSpeedMult;
                board.Apply(
                    StatusKind.Slow,
                    mobilityCc?.ResolveCcDurationMs(StatusKind.Slow, adjectiveId, tuning.SlowMs)
                        ?? tuning.SlowMs,
                    mult,
                    EffectSource(skill, "slow"));
            }

            if (keepEnemyLock && ModifierTruthy(mods, "apply_root") && !HasMech("root"))
                board.Apply(
                    StatusKind.Root,
                    ExplicitOrFallback(mods, "cc_duration_sec", StatusKind.Root, adjectiveId, tuning.RootMs, mobilityCc, skill.SkillJob),
                    1f,
                    RootSource(skill, "adj"));
            float rootSec = mods["apply_root_sec"].AsFloat(0f);
            if (keepEnemyLock && rootSec > 0f && !HasMech("root"))
                board.Apply(
                    StatusKind.Root,
                    rootSec * 1000.0,
                    1f,
                    RootSource(skill, "adj"));

            if (ModifierTruthy(mods, "apply_burn") && !HasMech("burn"))
            {
                float burn = tuning.BurnDamagePerSec;
                float burnMult = mods["burn_damage_mult"].AsFloat(1f);
                if (burnMult > 0f)
                    burn *= burnMult;
                board.Apply(StatusKind.Burn, tuning.BurnMs, burn);
            }

            if (ModifierTruthy(mods, "apply_poison_on_hit") && !HasMech("poison"))
                board.Apply(StatusKind.Poison, tuning.PoisonMs, tuning.PoisonDamagePerSec);

            if (ModifierTruthy(mods, "apply_silence") && !HasMech("silence"))
                board.Apply(
                    StatusKind.Silence,
                    mobilityCc?.ResolveCcDurationMs(StatusKind.Silence, adjectiveId, tuning.SilenceMs)
                        ?? tuning.SilenceMs,
                    1f);

            // Kart "yava┼şlatma" diyorsa isabet cezas─▒ k├Âr de─şil yava┼şlatmad─▒r.
            // Kart "k├Âr" veya "isabet" diyorsa eski k├Âr e┼şlemesi kal─▒r.
            float accuracy = mods["accuracy_debuff"].AsFloat(0f);
            if (accuracy > 0f && CardEffectRules.AccuracyIsSlow(skill.SkillJob) && !HasMech("slow"))
            {
                float mult = accuracy <= 1f ? accuracy : tuning.SlowSpeedMult;
                double slowMs = mods["lifetime_add"].AsFloat(0f) * 1000.0;
                if (slowMs <= 0)
                    slowMs = mobilityCc?.ResolveCcDurationMs(StatusKind.Slow, adjectiveId, tuning.SlowMs)
                        ?? tuning.SlowMs;
                board.Apply(StatusKind.Slow, slowMs, mult, EffectSource(skill, "accuracy-slow"));
            }
            else if (accuracy > 0f && !HasMech("blind"))
                board.Apply(
                    StatusKind.Blind,
                    mobilityCc?.ResolveCcDurationMs(StatusKind.Blind, adjectiveId, tuning.BlindMs)
                        ?? tuning.BlindMs,
                    StatusMath.BlindChanceFromAccuracy(accuracy));
        }

        static bool ModifierTruthy(JsonValue mods, string key)
        {
            if (!mods.Has(key))
                return false;
            JsonValue v = mods[key];
            if (v.Kind == JsonKind.Bool)
                return v.AsBool(false);
            if (v.Kind == JsonKind.Number)
                return v.AsFloat(0f) > 0f;
            if (v.Kind == JsonKind.String)
                return !string.IsNullOrEmpty(v.AsString());
            return false;
        }

        public static bool IsSelfTargeted(SkillResolution skill)
        {
            string hit = skill.Hitbox ?? string.Empty;
            if (hit is "self" or "self_aura" or "target_ally" or "self_or_ally")
                return true;
            if (skill.TargetMode is "self_only" or "self_or_ally")
                return true;
            string family = skill.VerbFamily ?? string.Empty;
            return family is "mend" or "guard" or "purge";
        }

        static void ApplyKind(
            StatusBoard board,
            StatusKind kind,
            SkillResolution skill,
            StatusTuning t,
            MobilityCcData? mobilityCc,
            int adjectiveId,
            float friendlyMagnitude = 1f)
        {
            float Friendly(float magnitude) =>
                friendlyMagnitude > 0f ? magnitude * friendlyMagnitude : magnitude;
            double Duration(double fallback) =>
                ExplicitOrFallback(
                    skill.EngineModifiers, "cc_duration_sec", kind, adjectiveId, fallback, mobilityCc, skill.SkillJob);
            switch (kind)
            {
                case StatusKind.Stun:
                    board.Apply(kind, Duration(t.StunMs), 1f);
                    break;
                case StatusKind.Root:
                    board.Apply(kind, Duration(t.RootMs), 1f, RootSource(skill, "verb"));
                    break;
                case StatusKind.Silence:
                    board.Apply(kind, Duration(t.SilenceMs), 1f);
                    break;
                case StatusKind.Slow:
                    board.Apply(kind, Duration(t.SlowMs), t.SlowSpeedMult, EffectSource(skill, "slow"));
                    break;
                case StatusKind.Blind:
                    // S7: %100 ─▒ska tuza─ş─▒ yerine tuning ─▒ska ┼şans─▒.
                    board.Apply(kind, Duration(t.BlindMs),
                        StatusMath.BlindChanceFromAccuracy(t.BlindMissChance));
                    break;
                case StatusKind.Disarm:
                    board.Apply(kind, Duration(t.DisarmMs), 1f);
                    break;
                case StatusKind.Taunt:
                    // S7 notu: Taunt uygulan─▒yor ama boss hedeflemesi hen├╝z okumuyor (dikkat_ceker boss tasar─▒m PR'─▒na).
                    board.Apply(kind, Duration(t.TauntMs), 1f);
                    break;
                case StatusKind.Fear:
                    board.Apply(kind, t.FearMs, 1f);
                    break;
                case StatusKind.Stasis:
                    board.Apply(kind, t.StasisMs, 1f);
                    break;
                case StatusKind.Stealth:
                    board.Apply(kind, t.StealthMs, 1f);
                    break;
                case StatusKind.Burn:
                    board.Apply(kind, t.BurnMs, t.BurnDamagePerSec);
                    break;
                case StatusKind.ArmorBreak:
                    board.Apply(kind, t.ArmorBreakMs, t.ArmorBreakDamageTakenMult);
                    break;
                case StatusKind.Weaken:
                    board.Apply(kind, t.WeakenMs, t.WeakenOutgoingMult);
                    break;
                case StatusKind.GrievousWounds:
                    board.Apply(kind, t.GrievousMs, t.GrievousHealMult);
                    break;
                case StatusKind.Poison:
                    board.Apply(kind, t.PoisonMs, t.PoisonDamagePerSec);
                    break;
                case StatusKind.Shield:
                {
                    float absorb = skill.EngineModifiers != null && !skill.EngineModifiers.IsNull
                        ? skill.EngineModifiers["shield_absorb"].AsFloat(0f)
                        : 0f;
                    board.Apply(kind, t.ShieldMs, Friendly(absorb > 0f ? absorb : t.ShieldAbsorb));
                    break;
                }
                case StatusKind.Haste:
                    board.Apply(kind, t.HasteMs, Friendly(t.HasteSpeedMult), EffectSource(skill, "haste"));
                    break;
                case StatusKind.DamageReduction:
                    board.Apply(kind, t.DamageReductionMs, t.DamageReductionMult);
                    break;
                case StatusKind.Regen:
                    board.Apply(kind, t.RegenMs, Friendly(t.RegenPerSec));
                    break;
            }
        }

        /// <summary>
        /// Skill engine'i kendi s├╝resini yazd─▒ysa o kullan─▒l─▒r.
        /// Yazmad─▒ysa mobility_cc tablosu, o da yoksa tuning yede─şi.
        /// </summary>
        static double ExplicitOrFallback(
            JsonValue engine,
            string secondsField,
            StatusKind kind,
            int adjectiveId,
            double fallbackMs,
            MobilityCcData? mobilityCc,
            string effectText = "")
        {
            if (!engine.IsNull && engine.Kind == JsonKind.Object)
            {
                string cc = CardEffectRules.CcKind(effectText, engine["cc_kind"].AsString());
                bool matches = string.IsNullOrEmpty(cc)
                    || (StatusKindUtil.TryParse(cc, out StatusKind ccKind) && ccKind == kind);
                float seconds = engine[secondsField].AsFloat(0f);
                if (matches && seconds > 0f)
                    return seconds * 1000.0;
            }

            return mobilityCc?.ResolveCcDurationMs(kind, adjectiveId, fallbackMs) ?? fallbackMs;
        }

        static string EffectSource(SkillResolution skill, string part) =>
            "skill:" + (string.IsNullOrEmpty(skill.SkillId) ? "unknown" : skill.SkillId) + ":" + part;

        static string RootSource(SkillResolution skill, string part) =>
            EffectSource(skill, part);

        static int ParseAdjectiveId(string id) =>
            int.TryParse(id, out int value) ? value : 0;
    }
}