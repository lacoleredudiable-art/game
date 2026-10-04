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
using Dovus.Game.Skills.Motion;
using Dovus.Game.Skills.Weapons;
using Dovus.Game.Weapons;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        WeaponServicesHost _weaponHost;
        WeaponPassiveRuntime _weaponPassives;
        OrbController _orbController;
        Weapons.CannonBlast _cannonBlast;

        void EnsureWeaponServices()
        {
            if (_weaponHost != null)
                return;
            _weaponHost = new WeaponServicesHost(this);
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

        bool TryTakeFreeMana()
        {
            EnsureWeaponServices();
            return _weaponPassives.TryTakeFreeMana();
        }

        void ConsumeWeaponBonus(StatusBoard cleanseTarget = null)
        {
            EnsureWeaponServices();
            _weaponPassives.ConsumeWeaponBonus(cleanseTarget);
        }

        float WeaponSupportPower(in SkillResolution skill)
        {
            EnsureWeaponServices();
            return _weaponPassives.WeaponSupportPower(skill);
        }

        float WeaponFriendlyScale()
        {
            EnsureWeaponServices();
            return _weaponPassives.WeaponFriendlyScale();
        }

        float WeaponCritAdd(in SkillResolution skill, bool isBasicStrike)
        {
            EnsureWeaponServices();
            return _weaponPassives.WeaponCritAdd(skill, isBasicStrike);
        }

        float WeaponDurationMult(in SkillResolution skill)
        {
            EnsureWeaponServices();
            return _weaponPassives.WeaponDurationMult(skill);
        }

        float WeaponCooldownMult()
        {
            EnsureWeaponServices();
            return _weaponPassives.WeaponCooldownMult();
        }

        void StopBasicCannonAtFirstBody(LivingEffect logic, Vector3 origin, Vector3 facing)
        {
            EnsureWeaponServices();
            _weaponPassives.StopBasicCannonAtFirstBody(logic, origin, facing);
        }

        float WeaponBasicReach(float fallback)
        {
            EnsureWeaponServices();
            return _weaponPassives.WeaponBasicReach(fallback);
        }

        void ApplyWeaponDelivery(
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

        void NoteWeaponCast(in SkillResolution skill)
        {
            EnsureWeaponServices();
            _weaponPassives.NoteWeaponCast(skill);
        }

        void OnWeaponSwapCompleted(EquipmentItem weapon)
        {
            EnsureWeaponServices();
            _weaponPassives.OnWeaponSwapCompleted(weapon);
        }

        void NoteShieldBlockIfGuarding()
        {
            EnsureWeaponServices();
            _weaponPassives.NoteShieldBlockIfGuarding();
        }

        bool HammerStunReady(double worldMs)
        {
            EnsureWeaponServices();
            return _weaponPassives.HammerStunReady(worldMs);
        }

        void CommitHammerStun(double worldMs, bool ready, bool alreadyHad, bool applied)
        {
            EnsureWeaponServices();
            _weaponPassives.CommitHammerStun(worldMs, ready, alreadyHad, applied);
        }

        void TryLandWeaponStun(in SkillResolution skill, bool isBasicStrike)
        {
            EnsureWeaponServices();
            _weaponPassives.TryLandWeaponStun(skill, isBasicStrike);
        }

        void TryConsumeCounterWindow()
        {
            EnsureWeaponServices();
            _weaponPassives.TryConsumeCounterWindow();
        }

        void RememberHitPoint(Vector3? point)
        {
            EnsureWeaponServices();
            _weaponPassives.RememberHitPoint(point);
        }

        internal WeaponPassiveMods HitMods(in SkillResolution skill, bool isBasicStrike, bool consumeBonus)
        {
            EnsureWeaponServices();
            return _weaponPassives.HitMods(skill, isBasicStrike, consumeBonus);
        }

        bool SwapDrawUnlocked(double worldMs)
        {
            EnsureWeaponServices();
            return _weaponPassives.SwapDrawUnlocked(worldMs);
        }

        void TryCannonBlast(float impactX, float impactZ)
        {
            EnsureWeaponServices();
            _cannonBlast.TryCannonBlast(impactX, impactZ);
        }

        void CutTemplateForSwap(bool instant)
        {
            EnsureWeaponServices();
            _cannonBlast.CutTemplateForSwap(instant);
        }

        void PushCannonBodies(float impactX, float impactZ, float splash, float arena, float bossR)
        {
            EnsureWeaponServices();
            _cannonBlast.PushCannonBodies(impactX, impactZ, splash, arena, bossR);
        }

        bool _casterRecoilSuppressed;

        float _lastHitX;
        float _lastHitZ;

        sealed class WeaponServicesHost
            : IWeaponPassiveRuntimeHost,
                IOrbControllerHost,
                ICannonBlastHost
        {
            readonly ManifestationDirector _md;

            internal bool WeaponIgnoresArmor;

            internal WeaponServicesHost(ManifestationDirector md) => _md = md;

            public Transform Player => _md._player;
            public BossReactorController Boss => _md._boss;
            public ActorStatusHost BossStatus => _md._bossStatus;
            public GameClockHost Clock => _md._clock;
            public EquipmentItem EquippedWeapon => _md._equippedWeapon;
            public WeaponCombatProfile EquippedProfile => _md.EquippedProfile;
            public SentenceEngine Engine => _md._engine;
            public ActorView Visual => _md._visual;
            public double LastMovedMs => _md._lastMovedMs;
            public bool PerformingAttack => _md.PerformingAttack;
            bool IWeaponPassiveRuntimeHost.WeaponIgnoresArmor
            {
                get => WeaponIgnoresArmor;
                set => WeaponIgnoresArmor = value;
            }
            public OrbAnchor Orb
            {
                get
                {
                    _md.EnsureWeaponServices();
                    return _md._orbController.Orb;
                }
            }

            public PlayerTargetingController Targeting => _md._targeting;
            public AllyDummyController Ally => _md._ally;
            public KinematicMotorController Motor => _md._motor;
            public MotionTemplateBodyHost MotionBody => _md._motionBody;
            public WeaponSwapState WeaponSwap => _md._weaponSwap;
            public double WorldTimeMs => _md.WorldTimeMs;
            bool IWeaponPassiveRuntimeHost.CasterRecoilSuppressed
            {
                get => _md._casterRecoilSuppressed;
                set => _md._casterRecoilSuppressed = value;
            }

            bool ICannonBlastHost.CasterRecoilSuppressed
            {
                get => _md._casterRecoilSuppressed;
                set => _md._casterRecoilSuppressed = value;
            }

            float IWeaponPassiveRuntimeHost.LastHitX
            {
                get => _md._lastHitX;
                set => _md._lastHitX = value;
            }

            float IWeaponPassiveRuntimeHost.LastHitZ
            {
                get => _md._lastHitZ;
                set => _md._lastHitZ = value;
            }

            public bool RecoilInTemplate
            {
                get
                {
                    _md.EnsureMotionServices();
                    return _md._motionDriver.RecoilInTemplate;
                }
            }

            public void SetRecoilInTemplate(bool value)
            {
                _md.EnsureMotionServices();
                _md._motionDriver.SetRecoilInTemplate(value);
            }

            public Vector3 FlatBodyForward()
            {
                _md.EnsureSkillServices();
                return _md._skillAim.FlatBodyForward();
            }

            public WeaponSkillCompatibility WeaponCompatibilityFor(SkillResolution skill) =>
                _md.WeaponCompatibilityFor(skill);

            public float PlayerBodyRadiusM() => _md.PlayerBodyRadiusM();
            public float BossBodyRadius() => _md.BossBodyRadius();

            public void GrantShortShield(float points, float durationSec)
            {
                if (_md._player == null || points <= 0f)
                    return;
                WeaponShortShieldHost host = _md._player.GetComponent<WeaponShortShieldHost>();
                if (host == null)
                    host = _md._player.gameObject.AddComponent<WeaponShortShieldHost>();
                if (_md._clock != null)
                    host.Bind(_md._clock);
                host.Grant(points, WorldTimeMs, durationSec);
            }

            public void ResetBasicChain() => _md._visual?.ResetBasicChain();
            public void SetSwapInstantDrawUntil(double worldMs) => _md._weaponPassives.SetSwapInstantDrawUntil(worldMs);
            public void StopMotionBody() => _md._motionBody?.Stop();
            public void EndMotionAnim() => _md._visual?.EndMotionAnim();
            public void AbortRecoveringSentence()
            {
                if (_md._engine != null && _md._engine.State.Phase == SentencePhase.Recovering)
                    _md._engine.Abort();
            }
        }
    }
}
