using System;
using Dovus.Core.Data;

using Dovus.Core.Shared;
namespace Dovus.Core.Equipment
{
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
            _swapEndsMs = nowMs + Rules.AnimationSec * Units.SecToMs;
            _cooldownEndsMs = nowMs + Rules.CooldownSec * Units.SecToMs;
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
            return (float)Math.Min(1.0, CooldownRemainingMs(nowMs) / (Rules.CooldownSec * Units.SecToMs));
        }
    }
}
