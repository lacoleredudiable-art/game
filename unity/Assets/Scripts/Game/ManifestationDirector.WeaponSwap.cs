using System;
using System.Collections.Generic;
using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// weapon_skill_interaction.swap: build'de 2 silah, savaşta aralarında swap.
    /// Silah uyumu/çarpan/pasif/executor yolu her cast'te EquippedWeapon'dan okunur.
    /// </summary>
    public sealed partial class ManifestationDirector
    {
        WeaponSwapState _weaponSwap;

        public WeaponSwapState WeaponSwap => _weaponSwap;

        /// <summary>Build ekranındaki silah seçimi için canonical 10 silah (id sırası).</summary>
        public IReadOnlyList<EquipmentItem> AvailableWeapons => _cycleWeapons;

        public void ConfigureWeaponSwap(WeaponSwapRules rules)
        {
            if (rules == null)
                return;
            _weaponSwap = new WeaponSwapState(rules);
            _weaponSwap.SetLoadout(_equippedWeapon, DefaultReserveWeapon(_equippedWeapon));
            LogLoadout("başlangıç");
        }

        public void SetWeaponLoadout(EquipmentItem primary, EquipmentItem secondary)
        {
            if (primary == null)
                return;
            _equippedWeapon = primary;
            LastFactorySkill = null;
            _weaponSwap?.SetLoadout(primary, secondary);
            SyncCycleIndex();
            LogLoadout("build");
        }

        /// <summary>Swap butonu / Q tuşu. Reddedilirse sebep readout'a düşer.</summary>
        public WeaponSwapResult TryRequestWeaponSwap()
        {
            if (_weaponSwap == null || _clock == null)
                return WeaponSwapResult.Disabled;

            double worldMs = _clock.Director.WorldTimeMs;
            SyncPlayerStateMachine(worldMs);
            bool tagged = false;
            bool inWindow = false;
            if (_motionBody != null && _motionBody.IsDisplacing)
            {
                tagged = WeaponSwapCancel.IsTagged(_motionBody.SkillId);
                inWindow = WeaponSwapCancel.InWindow(_motionBody.PlayedSec, _motionBody.PlayLengthSec, tagged);
            }
            bool drawing = _engine != null && _engine.State.Phase == SentencePhase.Building;
            bool stunned = false;
            if (_playerStatus != null)
            {
                var board = _playerStatus.Board;
                stunned = board.Has(StatusKind.Stun) || board.Has(StatusKind.Stasis) || board.Has(StatusKind.Fear);
            }
            bool dodging = _input?.Dodge != null && _input.Dodge.IsActive((int)worldMs);
            bool holding = false;
            bool stateAllows = _playerStates == null || _playerStates.AllowsSwap;
            bool allows = WeaponSwapCancel.MayBegin(stateAllows, drawing, holding, dodging, stunned, inWindow, tagged);
            WeaponSwapResult result = _weaponSwap.TryBegin(worldMs, allows);
            switch (result)
            {
                case WeaponSwapResult.Started:
                    if (WeaponSwapCancel.CutsRecovery(inWindow)
                        || (_weaponSwap.Rules.RecoveryCancel
                            && _engine != null
                            && _engine.State.Phase == SentencePhase.Recovering))
                        CutTemplateForSwap(WeaponSwapCancel.UnlocksNextSkill(inWindow, tagged));
                    // Kesilen ya da boştaki gövde havada kalmasın; pencere dışında oynayan kalıp sürer.
                    if (_motionBody == null && _player != null)
                        _motionBody = _player.GetComponent<MotionTemplateBody>();
                    if (_motionBody != null && !_motionBody.IsDisplacing)
                        _motionBody.CancelToGround();
                    DebugConfig.DevLog($"[WeaponSwap] başladı → {_weaponSwap.Reserve?.Name}");
                    break;
                case WeaponSwapResult.OnCooldown:
                    _readout?.NoteDenied("swap soğumada");
                    break;
                case WeaponSwapResult.StateBlocked:
                    _readout?.NoteDenied("şimdi swap yok");
                    break;
            }
            return result;
        }

        void TickWeaponSwap(double worldMs)
        {
            if (_weaponSwap == null)
                return;

            if (_weaponSwap.IsSwapping
                && _input?.Dodge != null
                && _input.Dodge.IsActive((int)worldMs)
                && _weaponSwap.CancelByDodge())
            {
                DebugConfig.DevLog("[WeaponSwap] dodge iptal etti");
            }

            if (!_weaponSwap.Tick(worldMs))
                return;

            // Kalıp Play anında kopyalanmıştır; sonraki cast yeni silahı okur.
            // Kesilen ya da boştaki gövde CancelToGround ile zemine iner (yukarıda).
            _equippedWeapon = _weaponSwap.Active;
            LastFactorySkill = null;
            OnWeaponSwapCompleted(_equippedWeapon);
            SyncCycleIndex();
            if (_weaponSwap.Rules.CancelsCombo)
                _closingChainBonus = 1f;
            LogLoadout("swap");
        }

        /// <summary>
        /// Build seçimi yapılmadan açılış: birincil yakın ise ilk ranged silah, değilse ilk
        /// yakın silah yedek olur — swap farkı ilk denemede görünsün diye.
        /// </summary>
        EquipmentItem DefaultReserveWeapon(EquipmentItem primary)
        {
            bool primaryRanged = SkillExecutorRouter.IsRangedWeapon(primary);
            for (int i = 0; i < _cycleWeapons.Count; i++)
            {
                EquipmentItem w = _cycleWeapons[i];
                if (w == null || (primary != null && string.Equals(w.Id, primary.Id, StringComparison.Ordinal)))
                    continue;
                if (SkillExecutorRouter.IsRangedWeapon(w) != primaryRanged)
                    return w;
            }
            return null;
        }

        void SyncCycleIndex()
        {
            _cycleWeaponIndex = -1;
            if (_equippedWeapon == null)
                return;
            for (int i = 0; i < _cycleWeapons.Count; i++)
            {
                if (string.Equals(_cycleWeapons[i].Id, _equippedWeapon.Id, StringComparison.Ordinal))
                {
                    _cycleWeaponIndex = i;
                    return;
                }
            }
        }

        void LogLoadout(string reason)
        {
            if (_weaponSwap == null)
                return;
            DebugConfig.DevLog(
                $"[WeaponSwap] {reason}: aktif={_weaponSwap.Active?.Name ?? "—"} "
                + $"yedek={_weaponSwap.Reserve?.Name ?? "—"}");
        }
    }
}
