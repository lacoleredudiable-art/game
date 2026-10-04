using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.DevTools;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    /// <summary>
    /// weapon_skill_interaction.swap: build'de 2 silah, savaÅŸta aralarÄ±nda swap.
    /// Silah uyumu/Ã§arpan/pasif/executor yolu her cast'te EquippedWeapon'dan okunur.
    /// </summary>
    public sealed partial class ManifestationDirector
    {
        WeaponSwapState _weaponSwap;

        public WeaponSwapState WeaponSwap => _weaponSwap;

        /// <summary>Build ekranÄ±ndaki silah seÃ§imi iÃ§in canonical 10 silah (id sÄ±rasÄ±).</summary>
        public IReadOnlyList<EquipmentItem> AvailableWeapons
        {
            get
            {
                EnsureSkillServices();
                return _weaponLoadout.CycleWeapons;
            }
        }

        public void ConfigureWeaponSwap(WeaponSwapRules rules)
        {
            if (rules == null)
                return;
            _weaponSwap = new WeaponSwapState(rules);
            _weaponSwap.SetLoadout(_equippedWeapon, DefaultReserveWeapon(_equippedWeapon));
            LogLoadout("baÅŸlangÄ±Ã§");
        }

        public void SetWeaponLoadout(EquipmentItem primary, EquipmentItem secondary)
        {
            if (primary == null)
                return;
            _equippedWeapon = primary;
            LastFactorySkill = null;
            _weaponSwap?.SetLoadout(primary, secondary);
            SyncCycleIndex();
            SyncVisualDelivery();
            LogLoadout("build");
        }

        readonly SustainedCastLock _sustainedCast = new SustainedCastLock();

        bool SustainedSkillActive(double worldMs) => _sustainedCast.Active(worldMs);

        /// <summary>O10: kanallÄ±/basÄ±lÄ± skill cast edildi â†’ sÃ¼resi boyunca swap kilitli.</summary>
        void NoteSustainedCast(in SkillResolution skill)
        {
            if (!IsSustained(skill) || _clock == null)
                return;
            double sec = skill.Engine.IsNull ? 0.0 : skill.Engine.ChannelSec(0f);
            if (_motionBody != null && _motionBody.IsDisplacing
                && string.Equals(_motionBody.SkillId, skill.SkillId, System.StringComparison.Ordinal))
                sec = System.Math.Max(sec, _motionBody.PlayLengthSec);
            _sustainedCast.Begin(_clock.Director.WorldTimeMs, sec);
        }

        /// <summary>Swap butonu / Q tuÅŸu. Reddedilirse sebep readout'a dÃ¼ÅŸer.</summary>
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
            // O10: kanallÄ±/basÄ±lÄ± skill (channel_sec ya da IsSustained) sÃ¼rerken kilit; etiketli pencere istisnasÄ± MayBegin'de.
            bool holding = SustainedSkillActive(worldMs);
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
                    // Kesilen ya da boÅŸtaki gÃ¶vde havada kalmasÄ±n; pencere dÄ±ÅŸÄ±nda oynayan kalÄ±p sÃ¼rer.
                    if (_motionBody == null && _player != null)
                        _motionBody = _player.GetComponent<MotionTemplateBody>();
                    if (_motionBody != null && !_motionBody.IsDisplacing)
                        _motionBody.CancelToGround();
                    DebugConfig.DevLog($"[WeaponSwap] baÅŸladÄ± â†’ {_weaponSwap.Reserve?.Name}");
                    break;
                case WeaponSwapResult.OnCooldown:
                    _readout?.NoteDenied("swap soÄŸumada");
                    break;
                case WeaponSwapResult.StateBlocked:
                    _readout?.NoteDenied("ÅŸimdi swap yok");
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

            // KalÄ±p Play anÄ±nda kopyalanmÄ±ÅŸtÄ±r; sonraki cast yeni silahÄ± okur.
            // Kesilen ya da boÅŸtaki gÃ¶vde CancelToGround ile zemine iner (yukarÄ±da).
            _equippedWeapon = _weaponSwap.Active;
            LastFactorySkill = null;
            OnWeaponSwapCompleted(_equippedWeapon);
            SyncCycleIndex();
            SyncVisualDelivery();
            if (_weaponSwap.Rules.CancelsCombo)
                _closingChainBonus = 1f;
            LogLoadout("swap");
        }

        /// <summary>
        /// Build seÃ§imi yapÄ±lmadan aÃ§Ä±lÄ±ÅŸ: birincil yakÄ±n ise ilk ranged silah, deÄŸilse ilk
        /// yakÄ±n silah yedek olur â€” swap farkÄ± ilk denemede gÃ¶rÃ¼nsÃ¼n diye.
        /// </summary>
        EquipmentItem DefaultReserveWeapon(EquipmentItem primary)
        {
            bool primaryRanged = SkillExecutorRouter.IsRangedWeapon(primary);
            EnsureSkillServices();
            IReadOnlyList<EquipmentItem> cycle = _weaponLoadout.CycleWeapons;
            for (int i = 0; i < cycle.Count; i++)
            {
                EquipmentItem w = cycle[i];
                if (w == null || (primary != null && string.Equals(w.Id, primary.Id, StringComparison.Ordinal)))
                    continue;
                if (SkillExecutorRouter.IsRangedWeapon(w) != primaryRanged)
                    return w;
            }
            return null;
        }

        void SyncCycleIndex()
        {
            EnsureSkillServices();
            _weaponLoadout.SyncCycleIndex();
        }

        void LogLoadout(string reason)
        {
            if (_weaponSwap == null)
                return;
            DebugConfig.DevLog(
                $"[WeaponSwap] {reason}: aktif={_weaponSwap.Active?.Name ?? "â€”"} "
                + $"yedek={_weaponSwap.Reserve?.Name ?? "â€”"}");
        }
    }
}
