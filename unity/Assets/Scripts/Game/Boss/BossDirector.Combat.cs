using Dovus.App.Boss;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Data;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using UnityEngine;

namespace Dovus.Game.Boss
{
    public sealed partial class BossDirector
    {
        readonly BossPoiseState _poiseState = new();
        BossStrikeApplier _strikeApplier;

        BossStrikeApplier StrikeApplier => _strikeApplier ??= new BossStrikeApplier(this);

        void ResolveStrike() => StrikeApplier.ResolveStrike();

        void OnVolleyProjectilePlayerHit() => StrikeApplier.OnVolleyProjectilePlayerHit();

        bool IsEnraged() =>
            _bossVitals != null
            && BossDirectorRules.IsEnraged(_bossVitals.Hp, _bossVitals.MaxHp);

        float CurrentPhaseSpeed() =>
            BossDirectorRules.CurrentPhaseSpeed(
                _bossStatus?.Board,
                CurrentMotion());

        bool SelectNextAttack()
        {
            if (_attack == null || _combat == null)
                return false;

            bool enraged = IsEnraged();
            Transform aim = AimTarget();
            float pounceDist = 0f;
            if (aim != null && _reactor != null)
            {
                Vector3 toAim = aim.position - _reactor.Home;
                toAim.y = 0f;
                pounceDist = toAim.magnitude;
            }

            if (!_attackSelector.TrySelect(
                    enraged,
                    _phase1AttackKinds,
                    _phase2AttackKinds,
                    k =>
                    {
                        if (k == BossAttackKind.Volley && _projectiles == null)
                            return false;
                        if (k == BossAttackKind.Pounce
                            && (aim == null || !BossAttackKindPicker.PounceInRange(pounceDist, _combat.Boss)))
                            return false;
                        return AttackKindAllowed(k);
                    },
                    _combat.Boss.MaxSameAttackKindStreak,
                    _combat.Boss.MaxSameVariantStreak,
                    _rng,
                    out BossAttackChoice choice))
                return false;

            BossAttackKind kind = choice.Kind;
            _activeAttackEntry = _attackEntriesByKind.TryGetValue(kind, out BossAttackEntry entry)
                ? entry
                : null;

            if (kind == BossAttackKind.FireCone)
            {
                _attack.ApplyFireCone();
                return true;
            }
            if (kind == BossAttackKind.Volley)
            {
                _attack.ApplyVolley(enraged);
                return true;
            }
            if (kind == BossAttackKind.WebField)
            {
                _attack.ApplyWebField();
                return true;
            }
            if (kind == BossAttackKind.Pounce)
            {
                Vector3 land = aim != null ? aim.position : _reactor.Home;
                float arenaR = _combat.SkillMotion.ArenaHalfSizeM - _combat.Boss.PounceWallMarginM;
                land = ArenaClamp.XZ(land, Mathf.Max(0f, arenaR), 0f);
                _attack.SetLanding(land.x, land.z);
                _attack.ApplyPounce();
                return true;
            }

            _attack.ApplyVariant(choice.Variant.Value);
            return true;
        }

        bool AttackKindAllowed(BossAttackKind kind) =>
            BossDirectorRules.AttackKindAllowed(_bossStatus?.Board, kind);

        void PickTarget()
        {
            if (_targets == null)
            {
                _target = _player;
                _targetKind = TargetKind.Player;
                _targetId = -1;
                return;
            }

            _targetId = _targets.Pick(_rng.NextDouble());
            HostileTargets.Entry e = _targetId >= 0 ? _targets.Find(_targetId) : null;
            _target = e?.Transform;
            _targetKind = e?.Kind ?? TargetKind.Player;
            if (e == null)
                _targetId = -1;
        }

        Transform AimTarget()
        {
            if (_targets == null)
                return _player != null && !PlayerStealthed ? _player : null;
            return _target;
        }

        float AimTargetRadius()
        {
            if (_targets != null)
            {
                HostileTargets.Entry e = _targetId >= 0 ? _targets.Find(_targetId) : null;
                if (e != null && e.Kind != TargetKind.Player)
                    return e.RadiusM;
            }

            return _playerMotor != null ? _playerMotor.BodyRadiusM : 0.5f;
        }

        void HandlePlayerDown(double worldMs)
        {
            bool down = _vitals != null && _vitals.IsDown;
            if (down && !_poiseState.PlayerWasDown)
            {
                Vector3 spawn = _vitals.SpawnPos;
                float safe = _combat.Boss.RadiusM;
                if (BossDirectorRules.ShouldSnapBossHomeToOrigin(
                        _reactor.Home.x,
                        _reactor.Home.z,
                        spawn.x,
                        spawn.z,
                        safe))
                    _reactor.Home = _originHome;

                _telegraph?.Hide();
                _feel?.ClearThreat();
                _brain?.EnterIdle(worldMs);
            }

            _poiseState.PlayerWasDown = down;
        }

        void BeginPounceLeap()
        {
            Vector3 home = _reactor.Home;
            if (!_poiseState.TryPlanPounceLeap(
                    home.x,
                    home.z,
                    _attack.LandingX,
                    _attack.LandingZ,
                    0.05f))
                return;
            float dx = _poiseState.PounceLandX - home.x;
            float dz = _poiseState.PounceLandZ - home.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            Vector3 fwd = new Vector3(dx / dist, 0f, dz / dist);
            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            float airSec = BossDirectorRules.PounceAirSec(_combat.Boss.PounceAirSec);
            float heightM = MotionFallbacks.Coded.HeightM;
            var phase = new MotionPhase(
                "pounce", "leap", airSec, "travel", "none", string.Empty, 0f,
                dist, 0f, 0f, 0f, heightM, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, null, null);
            var template = new MotionTemplate(
                "boss_pounce", "boss_pounce", 0, "boss", true, new[] { phase });
            _motionBody.Play(
                template,
                () => new MotionTarget(true, _poiseState.PounceLandX, _poiseState.PounceLandZ),
                () => false,
                null,
                _reactor.BodyRadiusM);
        }
    }
}
