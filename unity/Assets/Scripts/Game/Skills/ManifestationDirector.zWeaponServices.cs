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
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Skills.Hosts;
using Dovus.Game.Skills.Motion;
using Dovus.Game.Skills.State;
using Dovus.Game.Skills.Weapons;
using Dovus.Game.Weapons;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        internal MdWeaponServicesHost _weaponHost;
        internal WeaponPassiveRuntime _weaponPassives;
        internal OrbController _orbController;
        internal Weapons.CannonBlast _cannonBlast;

        internal void EnsureWeaponServices()
        {
            if (_weaponHost != null)
                return;
            _weaponHost = new MdWeaponServicesHost(this);
            _weaponPassives = new WeaponPassiveRuntime(_weaponHost);
            _orbController = new OrbController(_weaponHost);
            _cannonBlast = new Weapons.CannonBlast(_weaponHost);
        }

        public float EquippedBaseArmor => _equippedWeapon != null ? _equippedWeapon.BaseArmor : 0f;

        public bool WeaponIgnoresArmor
        {
            get
            {
              EnsureWeaponServices();
                return _weaponHost.WeaponIgnoresArmor;
            }
            private set
            {
              EnsureWeaponServices();
                _weaponHost.WeaponIgnoresArmor = value;
            }
        }

        public WeaponCombatProfile EquippedProfile =>
            _equippedWeapon != null ? _equippedWeapon.Profile : null;

        public double WorldTimeMs => _clock != null ? _clock.Director.WorldTimeMs : 0;

        public OrbAnchor Orb
        {
            get
            {
              EnsureWeaponServices();
                return _orbController.Orb;
            }
        }

        public float SwapButtonHoldSec
        {
            get
            {
              EnsureWeaponServices();
                return _orbController.SwapButtonHoldSec;
            }
        }

        public bool TryPlaceOrb(float targetX, float targetZ)
        {
          EnsureWeaponServices();
            return _orbController.TryPlace(targetX, targetZ);
        }

        public bool TryRecallOrb()
        {
          EnsureWeaponServices();
            return _orbController.TryRecall();
        }

        public bool ToggleOrb()
        {
          EnsureWeaponServices();
            return _orbController.Toggle();
        }

        void TickOrb(double worldMs)
        {
          EnsureWeaponServices();
            _orbController.Tick(worldMs);
        }

        void TickCannonRecoil()
        {
          EnsureWeaponServices();
            _cannonBlast.TickCannonRecoil();
        }

        float WeaponOutgoingDamageMult(in SkillResolution skill, bool isBasicStrike)
        {
          EnsureWeaponServices();
            return _weaponPassives.WeaponOutgoingDamageMult(skill, isBasicStrike);
        }

        internal bool TryTakeFreeMana()
        {
          EnsureWeaponServices();
            return _weaponPassives.TryTakeFreeMana();
        }

        internal void ConsumeWeaponBonus(StatusBoard cleanseTarget = null)
        {
          EnsureWeaponServices();
            _weaponPassives.ConsumeWeaponBonus(cleanseTarget);
        }

        internal float WeaponSupportPower(in SkillResolution skill)
        {
          EnsureWeaponServices();
            return _weaponPassives.WeaponSupportPower(skill);
        }

        internal float WeaponFriendlyScale()
        {
          EnsureWeaponServices();
            return _weaponPassives.WeaponFriendlyScale();
        }

        float WeaponCritAdd(in SkillResolution skill, bool isBasicStrike)
        {
          EnsureWeaponServices();
            return _weaponPassives.WeaponCritAdd(skill, isBasicStrike);
        }

        internal float WeaponDurationMult(in SkillResolution skill)
        {
          EnsureWeaponServices();
            return _weaponPassives.WeaponDurationMult(skill);
        }

        internal float WeaponCooldownMult()
        {
          EnsureWeaponServices();
            return _weaponPassives.WeaponCooldownMult();
        }

        internal void StopBasicCannonAtFirstBody(LivingEffect logic, Vector3 origin, Vector3 facing)
        {
          EnsureWeaponServices();
            _weaponPassives.StopBasicCannonAtFirstBody(logic, origin, facing);
        }

        internal float WeaponBasicReach(float fallback)
        {
          EnsureWeaponServices();
            return _weaponPassives.WeaponBasicReach(fallback);
        }

        internal void ApplyWeaponDelivery(
            in SkillResolution skill,
            SkillExecutorKind kind,
            Transform target,
            ref Vector3 origin,
            ref float range,
            ref float radius,
            ref string shape,
            ref float angleDeg,
            ref float speed)
        {
          EnsureWeaponServices();
            _weaponPassives.ApplyWeaponDelivery(
                skill, kind, target, ref origin, ref range, ref radius, ref shape, ref angleDeg, ref speed);
        }

        internal void NoteWeaponCast(in SkillResolution skill)
        {
          EnsureWeaponServices();
            _weaponPassives.NoteWeaponCast(skill);
        }

        void OnWeaponSwapCompleted(EquipmentItem weapon)
        {
          EnsureWeaponServices();
            _weaponPassives.OnWeaponSwapCompleted(weapon);
        }

        internal void NoteShieldBlockIfGuarding()
        {
          EnsureWeaponServices();
            _weaponPassives.NoteShieldBlockIfGuarding();
        }

        internal bool HammerStunReady(double worldMs)
        {
          EnsureWeaponServices();
            return _weaponPassives.HammerStunReady(worldMs);
        }

        internal void CommitHammerStun(double worldMs, bool ready, bool alreadyHad, bool applied)
        {
          EnsureWeaponServices();
            _weaponPassives.CommitHammerStun(worldMs, ready, alreadyHad, applied);
        }

        internal void TryLandWeaponStun(in SkillResolution skill, bool isBasicStrike)
        {
          EnsureWeaponServices();
            _weaponPassives.TryLandWeaponStun(skill, isBasicStrike);
        }

        internal void TryConsumeCounterWindow()
        {
          EnsureWeaponServices();
            _weaponPassives.TryConsumeCounterWindow();
        }

        internal void RememberHitPoint(Vector3? point)
        {
          EnsureWeaponServices();
            _weaponPassives.RememberHitPoint(point);
        }

        internal WeaponPassiveMods HitMods(in SkillResolution skill, bool isBasicStrike, bool consumeBonus)
        {
          EnsureWeaponServices();
            return _weaponPassives.HitMods(skill, isBasicStrike, consumeBonus);
        }

        internal bool SwapDrawUnlocked(double worldMs)
        {
          EnsureWeaponServices();
            return _weaponPassives.SwapDrawUnlocked(worldMs);
        }

        internal void TryCannonBlast(float impactX, float impactZ)
        {
          EnsureWeaponServices();
            _cannonBlast.TryCannonBlast(impactX, impactZ);
        }

        void CutTemplateForSwap(bool instant)
        {
          EnsureWeaponServices();
            _cannonBlast.CutTemplateForSwap(instant);
        }
    }
}
