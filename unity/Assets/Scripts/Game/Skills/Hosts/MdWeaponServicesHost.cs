using Dovus.Core.Boss;
using Dovus.Core.Casting;
using Dovus.Core.Damage;
using Dovus.Core.Dodge;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Hud;
using Dovus.Core.Input;
using Dovus.Core.Manifestation;
using Dovus.Core.Passives;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Skills.Motion;
using Dovus.Game.Skills.State;
using Dovus.Game.Skills.Weapons;
using Dovus.Game.Skills;
using Dovus.Game.Weapons;
using UnityEngine;

namespace Dovus.Game.Skills.Hosts
{
public sealed class MdWeaponServicesHost
        : IOrbControllerHost,
            ICannonBlastHost
    {
        readonly ManifestationDirector _md;

        internal MdWeaponServicesHost(ManifestationDirector md) => _md = md;

        public CastSessionState Cast => _md.CastSession;

        public bool WeaponIgnoresArmor { get; set; }

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
        public bool CasterRecoilSuppressed
        {
            get => _md.CastSession.CasterRecoilSuppressed;
            set => _md.CastSession.CasterRecoilSuppressed = value;
        }

        bool ICannonBlastHost.CasterRecoilSuppressed
        {
            get => _md.CastSession.CasterRecoilSuppressed;
            set => _md.CastSession.CasterRecoilSuppressed = value;
        }

        public float LastHitX
        {
            get => _md.CastSession.LastHitX;
            set => _md.CastSession.LastHitX = value;
        }

        public float LastHitZ
        {
            get => _md.CastSession.LastHitZ;
            set => _md.CastSession.LastHitZ = value;
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

        public System.Collections.Generic.IReadOnlyList<TargetableHost> LiveTargetables =>
            _md._liveTargetables != null ? _md._liveTargetables.Live : System.Array.Empty<TargetableHost>();

        public System.Collections.Generic.IReadOnlyList<SummonExecutor> LiveSummonExecutors =>
            _md._liveSummons != null ? _md._liveSummons.Live : System.Array.Empty<SummonExecutor>();
    }
}
