using System;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

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

    public enum WeaponSwapResult
    {
        Started,
        Disabled,
        NoSecondWeapon,
        OnCooldown,
        AlreadySwapping,
        StateBlocked
    }

    /// <summary>
    /// İki silah yuvası + swap animasyonu + bekleme. Silah animasyon bitince değişir;
    /// bekleme swap başlarken başlar (dodge iptali beklemeyi sıfırlamaz — spam engeli).
    /// Zaman parametre olarak geçer (dünya ms).
    /// </summary>
    public sealed class WeaponSwapState
    {
        readonly EquipmentItem?[] _slots = new EquipmentItem?[2];
        double _swapEndsMs;
        double _cooldownEndsMs;

        public WeaponSwapState(WeaponSwapRules rules)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public WeaponSwapRules Rules { get; }
        public int ActiveIndex { get; private set; }
        public bool IsSwapping { get; private set; }
        public EquipmentItem? Active => _slots[ActiveIndex];
        public EquipmentItem? Reserve => _slots[1 - ActiveIndex];
        public EquipmentItem? Slot(int index) => index is 0 or 1 ? _slots[index] : null;

        public void SetLoadout(EquipmentItem? primary, EquipmentItem? secondary)
        {
            _slots[0] = primary;
            _slots[1] = secondary;
            ActiveIndex = 0;
            IsSwapping = false;
            _swapEndsMs = 0;
            _cooldownEndsMs = 0;
        }

        /// <summary>Debug silah döngüsü: aktif yuvadaki silahı yerinde değiştirir.</summary>
        public void ReplaceActive(EquipmentItem? item) => _slots[ActiveIndex] = item;

        public WeaponSwapResult TryBegin(double nowMs, bool stateAllowsSwap)
        {
            if (!Rules.Enabled || Rules.WeaponsCarried < 2)
                return WeaponSwapResult.Disabled;
            if (Reserve == null)
                return WeaponSwapResult.NoSecondWeapon;
            if (IsSwapping)
                return WeaponSwapResult.AlreadySwapping;
            if (nowMs < _cooldownEndsMs)
                return WeaponSwapResult.OnCooldown;
            if (!stateAllowsSwap)
                return WeaponSwapResult.StateBlocked;

            IsSwapping = true;
            _swapEndsMs = nowMs + Rules.AnimationSec * 1000.0;
            _cooldownEndsMs = nowMs + Rules.CooldownSec * 1000.0;
            return WeaponSwapResult.Started;
        }

        /// <summary>true = swap bu tick'te tamamlandı, aktif silah değişti.</summary>
        public bool Tick(double nowMs)
        {
            if (!IsSwapping || nowMs < _swapEndsMs)
                return false;
            IsSwapping = false;
            ActiveIndex = 1 - ActiveIndex;
            return true;
        }

        /// <summary>dodge_cancels_swap: animasyon sürerken dodge swap'ı düşürür.</summary>
        public bool CancelByDodge()
        {
            if (!IsSwapping || !Rules.DodgeCancelsSwap)
                return false;
            IsSwapping = false;
            return true;
        }

        public double CooldownRemainingMs(double nowMs) => Math.Max(0.0, _cooldownEndsMs - nowMs);

        public float Cooldown01(double nowMs)
        {
            if (Rules.CooldownSec <= 0f)
                return 0f;
            return (float)Math.Min(1.0, CooldownRemainingMs(nowMs) / (Rules.CooldownSec * 1000.0));
        }
    }
}
