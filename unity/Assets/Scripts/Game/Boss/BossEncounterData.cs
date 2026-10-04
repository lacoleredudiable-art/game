using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Data;
using Dovus.Core.Tuning;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Boss
{
    /// <summary>
    /// KarÅŸÄ±laÅŸma tasarÄ±m sayÄ±larÄ± <c>Resources/Bosses/*.json</c> (BossHudData ile aynÄ± dosya).
    /// JSON ayrÄ±ÅŸtÄ±rma <see cref="BossEncounterMapper"/> (Core).
    /// </summary>
    public static class BossEncounterData
    {
        public static TargetingConfig LoadTargeting(string resourcePath = "Bosses/karadul")
        {
            if (!TryLoadText(resourcePath, out string json))
                return new TargetingConfig();
            try
            {
                return BossEncounterMapper.LoadTargeting(json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BossEncounterData] {resourcePath} okunamadÄ±: {e.Message}");
                return new TargetingConfig();
            }
        }

        public static bool ApplyVolley(BossTuning tuning, string resourcePath = "Bosses/karadul")
        {
            if (tuning == null || !TryLoadText(resourcePath, out string json))
                return false;
            try
            {
                return BossEncounterMapper.ApplyVolley(tuning, json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BossEncounterData] {resourcePath} volley okunamadÄ±: {e.Message}");
                return false;
            }
        }

        public static bool TryParseKindToken(string token, out BossAttackKind kind) =>
            BossEncounterMapper.TryParseKindToken(token, out kind);

        public static BossAttackKind[] LoadPhaseAttackKinds(string resourcePath, int phase)
        {
            if (!TryLoadText(resourcePath, out string json))
                return System.Array.Empty<BossAttackKind>();
            return BossEncounterMapper.LoadPhaseAttackKinds(json, phase);
        }

        public static bool TryLoadAttack(string resourcePath, string attackId, out BossAttackEntry entry)
        {
            entry = null;
            if (!TryLoadText(resourcePath, out string json))
                return false;
            return BossEncounterMapper.TryLoadAttack(json, attackId, out entry);
        }

        public static IReadOnlyList<BossAttackEntry> LoadAttacks(string resourcePath = "Bosses/karadul")
        {
            if (!TryLoadText(resourcePath, out string json))
                return System.Array.Empty<BossAttackEntry>();
            return BossEncounterMapper.LoadAttacks(json);
        }

        static bool TryLoadText(string resourcePath, out string json)
        {
            json = null;
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return false;
            json = asset.text;
            return true;
        }
    }
}
