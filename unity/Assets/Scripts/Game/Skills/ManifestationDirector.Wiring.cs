using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Core.Presentation;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.DevTools;
using Dovus.Game.Hud;
using Dovus.Game.Team;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        public void Bind(
            GameClock clock,
            HexagonInput input,
            Transform player,
            ActorPose pose,
            BossReactor boss,
            BossVitals bossVitals,
            GroundScarField scars,
            PrototypeTuning colors,
            DamageNumberHud damageHud = null,
            BossDirector bossDirector = null,
            ActorStatus playerStatus = null,
            ActorStatus bossStatus = null,
            SentenceDebugHud debugHud = null,
            ReactionReadout readout = null,
            FollowCamera camera = null,
            AllyDummy ally = null,
            HexagonView hexagonView = null,
            PassiveHud passiveHud = null,
            EquipmentItem equippedWeapon = null,
            EquipmentBonusResolver equipmentBonus = null,
            SkillMotor skills = null,
            SkillFactory skillFactory = null,
            AnimationDatabase animationDatabase = null)
        {
            _clock = clock;
            if (_input != null)
                _input.SkillCancelledByDodge -= CancelActiveSkillForDodge;
            _input = input;
            if (_input != null)
            {
                _input.SkillCancelledByDodge -= CancelActiveSkillForDodge;
                _input.SkillCancelledByDodge += CancelActiveSkillForDodge;
            }
            _engine = input.Engine;
            _combat = input.Combat;
            _colors = colors;
            _player = player;
            _pose = pose;
            _visual = player != null ? player.GetComponent<ActorVisual>() : null;
            _boss = boss;
            WireBossVitalsEvents(_bossVitals, bossVitals);
            _scars = scars;
            _damageHud = damageHud;
            _bossDirector = bossDirector;
            _motor = player.GetComponent<KinematicMotor>();
            if (_playerStatus != null)
            {
                _playerStatus.DamageTaken -= OnPlayerDamageTaken;
                _playerStatus.DamageBlocked -= OnPlayerDamageBlocked;
            }
            _playerStatus = playerStatus;
            WireBossStatusDot(_bossStatus, bossStatus);
            _debugHud = debugHud;
            _readout = readout;
            _camera = camera;
            _ally = ally;
            _ally?.BindStatusClock(clock, _combat != null ? _combat.Status : null);
            EnsureMotionReady();
            _passiveHud = passiveHud;
            _hexagonView = hexagonView;
            _equippedWeapon = equippedWeapon;
            RefreshDefenderArmor();
            EnsureBossArmor();
            _equipmentBonus = equipmentBonus;
            _playerResource = player != null ? player.GetComponent<PlayerResource>() : null;
            _playerCooldown = player != null ? player.GetComponent<PlayerCooldown>() : null;
            _skills = skills ?? SkillMotorLoader.Load();
            _skillFactory = skillFactory ?? new SkillFactory(_skills, _equipmentBonus);
            _animationDatabase = animationDatabase ?? LoadAnimationDatabase();
            EnsureCoreServices();
            EnsureSkillServices();
            _weaponLoadout.ResetElementPaintIndex();
            EnsureLaunchServices();
            _skillPresentation.EnsureCatalog();
            _playerStates = new PlayerStateMachine(_skills.PlayerStates);
            input.BindPlayerStates(_playerStates, () => PendingList.Count > 0);
            var motorForStates = player != null ? player.GetComponent<KinematicMotor>() : null;
            motorForStates?.BindPlayerStates(_playerStates);
            _slotPassives = new SlotPassiveDirector();
            _slotPassiveNeedsWeapon = false;
            if (ElementSystemJsonLoader.TryLoad(out ElementSystemDesign slotDesign))
                _slotPassiveNeedsWeapon = PassiveSlotPolicy.RequiresWeaponCompatibility(slotDesign.Document);
            _closingChainBonus = 1f;
            if (_playerStatus != null)
            {
                _playerStatus.SlotPassiveDirector = _slotPassives;
                _playerStatus.ReflectBossVitals = bossVitals;
                _playerStatus.IncomingDamageRedirect = RedirectMechanicDamage;
                _playerStatus.ReflectSink = ApplyReflectedDamage;
                _playerStatus.DamageTaken += OnPlayerDamageTaken;
                _playerStatus.DamageBlocked += OnPlayerDamageBlocked;
            }

            if (_engine != null && !_hooked)
            {
                _engine.SentenceCompleted += OnSentenceCompleted;
                _hooked = true;
            }
            SyncVisualDelivery();
            EnsureClosingServices();
            EnsureLaunchServices();
            _skillPresentation.EnsureCatalog();
        }
    }
}
