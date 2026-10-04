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
using Dovus.Core.Manifestation;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using Dovus.Game.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        /// <summary>GameBootstrapHost'??n atad?????? sabit silah (??r. Alev K??l??c??).</summary>
        public EquipmentItem EquippedWeapon => _equippedWeapon;

        public void ConfigureWeaponCycle(IReadOnlyList<EquipmentItem> weapons)
        {
            EnsureSkillServices();
            _weaponLoadout.ConfigureWeaponCycle(weapons);
        }

        public EquipmentItem CycleEquippedWeapon()
        {
            EnsureSkillServices();
            return _weaponLoadout.CycleEquippedWeapon();
        }

        /// <summary>v6: son kapan????ta silah ?? uyumsuz ??izim hasar ??arpan??.</summary>
        public float LastEquipmentMatchMult { get; private set; } = 1f;
        public bool LastWeaponCompatible { get; private set; } = true;
        public bool LastWeaponPassiveEnabled { get; private set; } = true;
        public string LastWeaponUiLabel { get; private set; } = string.Empty;
        public string LastResolvedSkillId { get; private set; } = string.Empty;
        public bool LastSkillEffectApplied { get; private set; }
        public Skill LastFactorySkill { get; private set; }
        public SkillExecutorKind LastExecutorKind { get; private set; } = SkillExecutorKind.Fallback;

        /// <summary>Ba??lama 9 / MCP: son ApplyClosingDamage ????kt??s?? (boss'a giden, armor ??ncesi).</summary>
        public float LastClosingDamageDealt { get; private set; }

        /// <summary>Ba??lama 10 / MCP: son ShoutSkill AnimationType id (katalog anahtar??).</summary>
        public string LastAnimationTypeId { get; private set; } = string.Empty;

        /// <summary>Ba??lama 10 / MCP: son denenen animator_state.</summary>
        public string LastAnimationState { get; private set; } = string.Empty;
        public string LastAnimationClip { get; private set; } = string.Empty;
        public bool LastAnimationUsedFallback { get; private set; }

        /// <summary>Ba??lama 10 / MCP: Controller'da state vard?? ve Play uyguland??.</summary>
        public bool LastAnimationPlayApplied { get; private set; }

        /// <summary>Ba??lama 10 / MCP: frame-timer k??pr??s?? (Play do??rulama).</summary>
        public AnimationBridge AnimationBridge => _animationBridge;
        public ElementPaintNode? SelectedElementPaint
        {
            get
            {
                EnsureSkillServices();
                int index = _weaponLoadout.ElementPaintIndex;
                return _skills != null
                    && _skills.ElementPaints.Count > 0
                    && index >= 0
                    && index < _skills.ElementPaints.Count
                        ? _skills.ElementPaints[index]
                        : null;
            }
        }
        public event Action<ElementPaintNode> ElementPaintChanged;

#if UNITY_EDITOR
        /// <summary>Ba??lama 10 / MCP: ShoutSkill i??indeki ApplySkillAnimation yolunu do??rudan dener.</summary>
        public void DebugApplySkillAnimation(SkillResolution skill)
        {
            EnsureLaunchServices();
            _skillPresentation.ApplySkillAnimation(skill);
        }
#endif

        public ElementPaintNode? CycleElementPaint()
        {
            EnsureSkillServices();
            return _weaponLoadout.CycleElementPaint();
        }

        public bool TrySetElementPaint(int elementId)
        {
            EnsureSkillServices();
            return _weaponLoadout.TrySetElementPaint(elementId);
        }

        SkillMotor Skills => _skills ??= SkillMotorLoader.Load();

        WeaponSkillCompatibility WeaponCompatibilityFor(SkillResolution skill)
        {
            EnsureSkillServices();
            return _weaponLoadout.WeaponCompatibilityFor(skill);
        }

        public LivingEffect ActiveLogic => _buildingView?.Logic;

        public int ActiveCount
        {
            get
            {
                EnsureSkillServices();
                return _effectSpawner.ActiveCount;
            }
        }

        public Transform CurrentFacingTarget
        {
            get
            {
                EnsureSkillServices();
                return _skillAim.CurrentFacingTarget;
            }
        }

        /// <summary>
        /// Sald??r?? boyunca g??vde ??ubu??a d??nmez. Hedef varsa ona kilitlenir;
        /// yoksa bak???? kal??r. Sald??r?? d??????nda se??ili hedef varsa eski kilit durur.
        /// </summary>
        public bool CombatFacingLocked
        {
            get
            {
                EnsureSkillServices();
                return _skillAim.CombatFacingLocked;
            }
        }

        bool PerformingAttack =>
            (_engine != null && (_engine.State.Phase == SentencePhase.Building
                || _engine.State.Phase == SentencePhase.Recovering))
            || PendingList.Count > 0
            || (_motionBody != null && _motionBody.IsDisplacing)
            || (_visual != null && _visual.IsAttackPose);

        public SlotPassiveDirector SlotPassives => _slotPassives;

        /// <summary>state_machine.player_states ??? SentencePhase/dodge/CC ile senkron.</summary>
        public PlayerStateMachine PlayerStates => _playerStates;

#if UNITY_EDITOR
        /// <summary>Edit??r/prob: Update beklemeden c??mle senkronu.</summary>
        public void ForceSync()
        {
            if (_clock == null)
                return;
            SyncFromSentence(_clock.Director.WorldTimeMs);
        }
#endif
    }
}
