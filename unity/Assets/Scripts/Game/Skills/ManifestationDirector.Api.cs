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
        /// <summary>GameBootstrap'ın atadığı sabit silah (ör. Alev Kılıcı).</summary>
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

        /// <summary>v6: son kapanışta silah × uyumsuz çizim hasar çarpanı.</summary>
        public float LastEquipmentMatchMult { get; private set; } = 1f;
        public bool LastWeaponCompatible { get; private set; } = true;
        public bool LastWeaponPassiveEnabled { get; private set; } = true;
        public string LastWeaponUiLabel { get; private set; } = string.Empty;
        public string LastResolvedSkillId { get; private set; } = string.Empty;
        public bool LastSkillEffectApplied { get; private set; }
        public Skill LastFactorySkill { get; private set; }
        public SkillExecutorKind LastExecutorKind { get; private set; } = SkillExecutorKind.Fallback;

        /// <summary>Bağlama 9 / MCP: son ApplyClosingDamage çıktısı (boss'a giden, armor öncesi).</summary>
        public float LastClosingDamageDealt { get; private set; }

        /// <summary>Bağlama 10 / MCP: son ShoutSkill AnimationType id (katalog anahtarı).</summary>
        public string LastAnimationTypeId { get; private set; } = string.Empty;

        /// <summary>Bağlama 10 / MCP: son denenen animator_state.</summary>
        public string LastAnimationState { get; private set; } = string.Empty;
        public string LastAnimationClip { get; private set; } = string.Empty;
        public bool LastAnimationUsedFallback { get; private set; }

        /// <summary>Bağlama 10 / MCP: Controller'da state vardı ve Play uygulandı.</summary>
        public bool LastAnimationPlayApplied { get; private set; }

        /// <summary>Bağlama 10 / MCP: frame-timer köprüsü (Play doğrulama).</summary>
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
        /// <summary>Bağlama 10 / MCP: ShoutSkill içindeki ApplySkillAnimation yolunu doğrudan dener.</summary>
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
        /// Saldırı boyunca gövde çubuğa dönmez. Hedef varsa ona kilitlenir;
        /// yoksa bakış kalır. Saldırı dışında seçili hedef varsa eski kilit durur.
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

        /// <summary>state_machine.player_states — SentencePhase/dodge/CC ile senkron.</summary>
        public PlayerStateMachine PlayerStates => _playerStates;

#if UNITY_EDITOR
        /// <summary>Editör/prob: Update beklemeden cümle senkronu.</summary>
        public void ForceSync()
        {
            if (_clock == null)
                return;
            SyncFromSentence(_clock.Director.WorldTimeMs);
        }
#endif
    }
}
