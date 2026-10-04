using System;
using Dovus.Core.Data;

using Dovus.Core.Shared;
namespace Dovus.Core.Equipment
{
    /// <summary>docs/element-sistemi.json weapon_skill_interaction.swap — savaş içi 2 silah.</summary>
    public sealed class WeaponSwapRules
    {
        public WeaponSwapRules(
            bool enabled,
            int weaponsCarried,
            float cooldownSec,
            float animationSec,
            bool cancelsCombo,
            bool dodgeCancelsSwap,
            bool recoveryCancel)
        {
            Enabled = enabled;
            WeaponsCarried = weaponsCarried;
            CooldownSec = Math.Max(0f, cooldownSec);
            AnimationSec = Math.Max(0f, animationSec);
            CancelsCombo = cancelsCombo;
            DodgeCancelsSwap = dodgeCancelsSwap;
            RecoveryCancel = recoveryCancel;
        }

        public bool Enabled { get; }
        public int WeaponsCarried { get; }
        public float CooldownSec { get; }
        public float AnimationSec { get; }
        public bool CancelsCombo { get; }
        public bool DodgeCancelsSwap { get; }
        public bool RecoveryCancel { get; }

        public static WeaponSwapRules FromJsonRoot(JsonValue root)
        {
            JsonValue s = root["weapon_skill_interaction"]["swap"];
            return new WeaponSwapRules(
                enabled: s["enabled"].AsBool(false),
                weaponsCarried: s["weapons_carried"].AsInt(1),
                cooldownSec: s["cooldown_sec"].AsFloat(),
                animationSec: s["animation_sec"].AsFloat(),
                cancelsCombo: s["cancels_combo"].AsBool(false),
                dodgeCancelsSwap: s["dodge_cancels_swap"].AsBool(false),
                recoveryCancel: s["recovery_cancel"].AsBool(false));
        }

        public static WeaponSwapRules FromJson(string json) =>
            FromDocument(ElementSystemDocument.Parse(json));

        public static WeaponSwapRules FromDocument(ElementSystemDocument doc) =>
            FromJsonRoot(doc.Root);
    }
}
