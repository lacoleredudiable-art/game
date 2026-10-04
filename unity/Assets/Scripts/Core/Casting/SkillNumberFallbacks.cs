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
    /// JSON alanı yokken kullanılan tek yer. Yeni his sayısı buraya eklenmez;
    /// bunlar yalnızca eksik alan yedeğidir.
    /// </summary>
    public static class SkillNumberFallbacks
    {
        public const float VerbDamageReference = 40f;
        public const float Damage = 0f;
        public const float CooldownSec = 1f;
        public const float ManaCost = 10f;
        public const float GlobalCooldownSec = 0.3f;
        public const int MaxConcurrentCasts = 1;
        public const float MaxMana = 100f;
        public const float ManaRegenPerSec = 8f;
        public const float ManaRegenDelaySec = 1.5f;
        public const float RangeM = 2.4f;
        public const float RadiusM = 1.15f;
        public const double RootImmunitySec = 0.5;
        public const double RootImmunityMs = RootImmunitySec * 1000.0;
        /// <summary>Tempo süresi JSON'da yoksa tek yedek: 1 sn.</summary>
        public const double TempoSyncFallbackMs = 1000.0;
        /// <summary>Tempo gücü JSON'da yoksa tek yedek (hareketin %70'i).</summary>
        public const float TempoSyncFallbackStrength = 0.7f;
        /// <summary>Kart "haste" der ama JSON sayı vermezse +%50.</summary>
        public const float SelfHasteBonus = 0.5f;
        /// <summary>Dost hedefi skill menzili JSON'da yoksa 6 m.</summary>
        public const float AllySkillRangeM = 6f;
    }
}
