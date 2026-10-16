using System;
using System.Collections.Generic;

namespace Dovus.Core.Trial
{
    /// <summary>
    /// Denemeye taşınan iki silah (kimlik olarak) ve savaş içi aktif/yedek durumu.
    /// Yalnız veri + durum: animasyon, soket ve iptal kuralı yok (Aşama 7).
    /// Eski WeaponSwapState/WeaponLoadoutCycle (element-sistemi, donduruldu) buna bağlanmaz.
    /// </summary>
    public sealed class WeaponLoadout
    {
        readonly string?[] _slots = new string?[TrialDefaults.LoadoutSlots];
        readonly HashSet<string> _available;

        public WeaponLoadout(IEnumerable<string> availableWeaponIds)
        {
            if (availableWeaponIds == null)
                throw new ArgumentNullException(nameof(availableWeaponIds));
            _available = new HashSet<string>(availableWeaponIds, StringComparer.Ordinal);
        }

        public int ActiveIndex { get; private set; }
        public int SwapCount { get; private set; }

        public bool IsComplete => _slots[0] != null && _slots[1] != null;
        public string? Active => _slots[ActiveIndex];
        public string? Reserve => _slots[1 - ActiveIndex];
        public string? Slot(int index) => index is 0 or 1 ? _slots[index] : null;

        /// <summary>Slota silah koyar. Bilinmeyen kimlik ya da diğer slotta zaten seçili silah reddedilir.</summary>
        public bool TrySelect(int slot, string weaponId)
        {
            if (slot is < 0 or >= TrialDefaults.LoadoutSlots)
                return false;
            if (!_available.Contains(weaponId))
                return false;
            if (string.Equals(_slots[1 - slot], weaponId, StringComparison.Ordinal))
                return false;
            _slots[slot] = weaponId;
            return true;
        }

        public void ClearSlot(int slot)
        {
            if (slot is 0 or 1)
                _slots[slot] = null;
        }

        /// <summary>
        /// Savaş içi geçiş: aktif ile yedek yer değiştirir. Zamanlama (bekleme, animasyon) bu sınıfın işi değil;
        /// çağıran izin verip vermediğini <paramref name="allowed"/> ile bildirir.
        /// </summary>
        public bool TrySwap(bool allowed = true)
        {
            if (!allowed || !IsComplete)
                return false;
            ActiveIndex = 1 - ActiveIndex;
            SwapCount++;
            return true;
        }

        /// <summary>Yeni deneme: seçim korunur, aktif silah yine birinci slot olur.</summary>
        public void ResetForAttempt()
        {
            ActiveIndex = 0;
            SwapCount = 0;
        }
    }
}
