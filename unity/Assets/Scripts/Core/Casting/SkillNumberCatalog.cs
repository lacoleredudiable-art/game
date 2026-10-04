using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

using Dovus.Core.Shared;
namespace Dovus.Core.Casting
{
    /// <summary>
    /// Skill sayıları (hasar, süre, soğuma, mana, menzil, yarıçap) tek JSON'dan.
    /// Alan yoksa bir kez uyarır ve <see cref="SkillNumberFallbacks"/> kullanır.
    /// </summary>
    public sealed class SkillNumberCatalog : ISkillRepository
    {
        readonly Dictionary<int, VerbNumbers> _verbs = new();

        internal SkillNumberCatalog()
        {
        }

        public float VerbDamageReference { get; internal set; } = SkillNumberFallbacks.VerbDamageReference;
        public float GlobalCooldownSec { get; internal set; } = SkillNumberFallbacks.GlobalCooldownSec;
        public int MaxConcurrentCasts { get; internal set; } = SkillNumberFallbacks.MaxConcurrentCasts;
        public float MaxMana { get; internal set; } = SkillNumberFallbacks.MaxMana;
        public float ManaRegenPerSec { get; internal set; } = SkillNumberFallbacks.ManaRegenPerSec;
        public float ManaRegenDelaySec { get; internal set; } = SkillNumberFallbacks.ManaRegenDelaySec;
        public double RootImmunityMs { get; internal set; } = SkillNumberFallbacks.RootImmunityMs;
        public float AllySkillRangeM { get; internal set; } = SkillNumberFallbacks.AllySkillRangeM;

        public static SkillNumberCatalog FromJson(string json) =>
            FromDocument(ElementSystemDocument.Parse(json));

        public static SkillNumberCatalog FromDocument(ElementSystemDocument doc) =>
            FromJsonRoot(doc.Root);

        public static SkillNumberCatalog FromJsonRoot(JsonValue root) => SkillNumberParser.Parse(root);

        Dictionary<StatusKind, int> _ccMs = new();

        internal void ImportVerb(
            int verbId,
            float damage,
            float cooldownSec,
            float manaCost,
            float durationSec,
            float rangeM,
            float radiusM) =>
            _verbs[verbId] = new VerbNumbers(damage, cooldownSec, manaCost, durationSec, rangeM, radiusM);

        internal void ImportCcDurations(Dictionary<StatusKind, int> ccMs) => _ccMs = ccMs ?? new Dictionary<StatusKind, int>();

        public bool TryGetVerb(int verbId, out float damage, out float cooldownSec, out float manaCost,
            out float durationSec, out float rangeM, out float radiusM)
        {
            if (_verbs.TryGetValue(verbId, out VerbNumbers n))
            {
                damage = n.Damage;
                cooldownSec = n.CooldownSec;
                manaCost = n.ManaCost;
                durationSec = n.DurationSec;
                rangeM = n.RangeM;
                radiusM = n.RadiusM;
                return true;
            }

            damage = SkillNumberFallbacks.Damage;
            cooldownSec = SkillNumberFallbacks.CooldownSec;
            manaCost = SkillNumberFallbacks.ManaCost;
            durationSec = 0f;
            rangeM = SkillNumberFallbacks.RangeM;
            radiusM = SkillNumberFallbacks.RadiusM;
            return false;
        }

        public float RadiusM(int verbId) =>
            _verbs.TryGetValue(verbId, out VerbNumbers n) ? n.RadiusM : SkillNumberFallbacks.RadiusM;

        public float RangeM(int verbId) =>
            _verbs.TryGetValue(verbId, out VerbNumbers n) ? n.RangeM : SkillNumberFallbacks.RangeM;

        /// <summary>
        /// Düz vuruş menzili ve kapsül yarıçapı = fiil 1 hitbox'ı (hitbox_vfx). Alan yoksa tuning yedeği kalır
        /// ve katalog zaten bir kez uyarmıştır.
        /// </summary>
        public void ApplyBasicStrikeRange(ManifestationTuning tuning)
        {
            if (tuning == null)
                return;
            tuning.BasicStrikeRangeM = RangeM(1);
            tuning.BasicStrikeRadiusM = RadiusM(1);
        }

        /// <summary>
        /// CC sürelerini JSON cc_priority'den canlı ayara yazar.
        /// Alan yoksa StatusTuning'deki adlı yedek kalır.
        /// </summary>
        public void ApplyCcDurations(StatusTuning tuning)
        {
            if (tuning == null)
                return;
            void Set(StatusKind kind, Action<int> write)
            {
                if (_ccMs.TryGetValue(kind, out int ms) && ms > 0)
                    write(ms);
            }

            Set(StatusKind.Stun, v => tuning.StunMs = v);
            Set(StatusKind.Root, v => tuning.RootMs = v);
            Set(StatusKind.Silence, v => tuning.SilenceMs = v);
            Set(StatusKind.Slow, v => tuning.SlowMs = v);
            Set(StatusKind.Blind, v => tuning.BlindMs = v);
            Set(StatusKind.Disarm, v => tuning.DisarmMs = v);
            Set(StatusKind.Taunt, v => tuning.TauntMs = v);
        }

        readonly struct VerbNumbers
        {
            public VerbNumbers(float damage, float cooldownSec, float manaCost, float durationSec, float rangeM, float radiusM)
            {
                Damage = damage;
                CooldownSec = cooldownSec;
                ManaCost = manaCost;
                DurationSec = durationSec;
                RangeM = rangeM;
                RadiusM = radiusM;
            }

            public float Damage { get; }
            public float CooldownSec { get; }
            public float ManaCost { get; }
            public float DurationSec { get; }
            public float RangeM { get; }
            public float RadiusM { get; }
        }
    }
}
