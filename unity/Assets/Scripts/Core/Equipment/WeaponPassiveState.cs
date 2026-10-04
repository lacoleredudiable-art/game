using System;
using Dovus.Core.Shared;

namespace Dovus.Core.Equipment
{
    /// <summary>Pasifin zamanı olan parçaları: çekiç beklemesi, kalkan penceresi, kitap sayacı, swap bonusu.</summary>
    public sealed class WeaponPassiveState
    {
        double _hammerReadyMs;
        double _counterUntilMs;
        double _chainStartMs = double.NegativeInfinity;
        int _chainCount;
        int _bonusWeaponId;
        double _bonusUntilMs;
        bool _bonusArmed;
        string _bonusId = string.Empty;

        public bool CounterArmed(double nowMs) => nowMs < _counterUntilMs;

        public int ChainCount => _chainCount;

        /// <summary>
        /// Aralık dolduysa sayaç bayat: 0 döner ve birikmiş sırayı siler.
        /// Yeni vuruş beklemeden okunur.
        /// </summary>
        public int EffectiveChain(double nowMs, float gapSec)
        {
            if (nowMs - _chainStartMs > Math.Max(0f, gapSec) * Units.SecToMs)
            {
                _chainCount = 0;
                return 0;
            }
            return _chainCount;
        }

        public bool BonusArmed(double nowMs) => _bonusArmed && nowMs < _bonusUntilMs;

        public void NoteBlock(double nowMs, float windowSec)
        {
            _counterUntilMs = nowMs + Math.Max(0f, windowSec) * Units.SecToMs;
        }

        /// <summary>Penceredeki ilk skill vuruşu bonusu yer. Sonrakiler yemez.</summary>
        public bool TryConsumeBlock(double nowMs)
        {
            if (!(nowMs < _counterUntilMs))
                return false;
            _counterUntilMs = double.NegativeInfinity;
            return true;
        }

        public bool HammerReady(double nowMs) => nowMs >= _hammerReadyMs;

        public void CommitHammer(double nowMs, float icdSec)
        {
            _hammerReadyMs = nowMs + Math.Max(0f, icdSec) * Units.SecToMs;
        }

        /// <summary>Art arda skill. Aralık aşılırsa sayaç 1'den başlar. Dönüş bu vuruşun sırası.</summary>
        public int NoteSkill(double nowMs, float gapSec)
        {
            if (nowMs - _chainStartMs > Math.Max(0f, gapSec) * Units.SecToMs)
                _chainCount = 0;
            _chainCount++;
            _chainStartMs = nowMs;
            return _chainCount;
        }

        public void ArmSwapBonus(int weaponId, string bonusId, double nowMs, float windowSec)
        {
            _bonusWeaponId = weaponId;
            _bonusId = bonusId ?? string.Empty;
            _bonusUntilMs = nowMs + Math.Max(0f, windowSec) * Units.SecToMs;
            _bonusArmed = true;
        }

        public bool PeekBonus(int weaponId, double nowMs, out WeaponSwapBonusSpec spec, WeaponCombatProfile profile)
        {
            spec = WeaponSwapBonusSpec.None;
            if (!_bonusArmed || weaponId != _bonusWeaponId || nowMs >= _bonusUntilMs)
                return false;
            spec = profile != null ? profile.SwapBonus : WeaponSwapBonusSpec.None;
            return spec.Id.Length > 0;
        }

        public bool TryConsumeBonus(int weaponId, double nowMs, out WeaponSwapBonusSpec spec, WeaponCombatProfile profile)
        {
            spec = WeaponSwapBonusSpec.None;
            if (!_bonusArmed || weaponId != _bonusWeaponId || nowMs >= _bonusUntilMs)
            {
                if (_bonusArmed && nowMs >= _bonusUntilMs)
                    _bonusArmed = false;
                return false;
            }
            _bonusArmed = false;
            spec = profile != null ? profile.SwapBonus : WeaponSwapBonusSpec.None;
            return spec.Id.Length > 0;
        }

    }
}
