using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Weapons;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    /// <summary>
    /// 10 silah teslimi, pasif ve silah kesme. Hasar formülüne dokunmaz;
    /// çarpanı kapanışın üstüne ekler. Hareket kalıbının yolunu yazmaz.
    /// </summary>
    public sealed partial class ManifestationDirector
    {
        readonly WeaponPassiveState _weaponPassives = new();
        readonly OrbAnchor _orb = new();
        readonly CannonRecoil _cannonRecoil = new();
        /// <summary>Teslim kuyruğu ve çağrılan aktör hasarı oyuncuyu geri tepmez.</summary>
        bool _casterRecoilSuppressed;
        readonly List<Transform> _cannonBodies = new();
        bool _cannonQueued;
        float _cannonImpactX;
        float _cannonImpactZ;
        double _swapInstantDrawUntilMs;
        float _lastHitX;
        float _lastHitZ;

        /// <summary>Hasar ajanı bunu okur. Kuşanılmış silahın temel zırhı.</summary>
        public float EquippedBaseArmor => _equippedWeapon != null ? _equippedWeapon.BaseArmor : 0f;

        /// <summary>Yay değiştirme bonusu: sıradaki vuruş zırhı yok sayar.</summary>
        public bool WeaponIgnoresArmor { get; private set; }

        public WeaponCombatProfile EquippedProfile => _equippedWeapon != null ? _equippedWeapon.Profile : null;

        public double WorldTimeMs => _clock != null ? _clock.Director.WorldTimeMs : 0;

        public OrbAnchor Orb => _orb;

        bool SwapDrawUnlocked(double worldMs) => worldMs < _swapInstantDrawUntilMs;

        float WeaponOutgoingDamageMult(in SkillResolution skill, bool isBasicStrike)
        {
            return HitMods(skill, isBasicStrike, false).DamageMult;
        }

        bool TryTakeFreeMana()
        {
            WeaponCombatProfile profile = EquippedProfile;
            if (profile == null || !profile.SwapBonus.FreeMana || _clock == null)
                return false;
            return _weaponPassives.TryConsumeBonus(
                profile.Id, _clock.Director.WorldTimeMs, out _, profile);
        }

        void ConsumeWeaponBonus(StatusBoard cleanseTarget = null)
        {
            if (_equippedWeapon == null || _clock == null || EquippedProfile == null)
                return;
            if (!_weaponPassives.TryConsumeBonus(
                    EquippedProfile.Id,
                    _clock.Director.WorldTimeMs,
                    out WeaponSwapBonusSpec bonus,
                    EquippedProfile))
                return;
            WeaponIgnoresArmor = bonus.IgnoreArmor;
            if (bonus.Cleanse)
                OneNegativeCleanse.TryRemove(cleanseTarget);
            if (bonus.Shield > 0f)
                GrantShortShield(bonus.Shield, bonus.ShieldSec);
        }

        void GrantShortShield(float points, float durationSec)
        {
            if (_player == null || points <= 0f)
                return;
            WeaponShortShieldHost host = _player.GetComponent<WeaponShortShieldHost>();
            if (host == null)
                host = _player.gameObject.AddComponent<WeaponShortShieldHost>();
            if (_clock != null)
                host.Bind(_clock);
            host.Grant(points, WorldTimeMs, durationSec);
        }

        float WeaponSupportPower(in SkillResolution skill)
        {
            if (_equippedWeapon == null)
                return 1f;
            int verb = VerbOf(skill.SkillId);
            bool enabled = _equippedWeapon.IsCompatibleWithVerb(verb);
            float power = WeaponPassiveRules.SupportPower(
                _equippedWeapon.Profile,
                enabled,
                verb,
                WeaponCompatibilityFor(skill).DamageMult);
            float heal = HitMods(skill, false, false).HealMult;
            return heal > 1f ? heal : power;
        }

        /// <summary>Tılsım kuşanılıyken şifa, kalkan ve buff büyüklüğü. Hasar çarpanı değil.</summary>
        float WeaponFriendlyScale()
        {
            if (EquippedProfile == null || EquippedProfile.Passive.Id != "kutsal_etki")
                return 1f;
            float heal = HitMods(SkillResolution.Empty, false, false).HealMult;
            return heal > 0f ? heal : 1f;
        }

        float WeaponCritAdd(in SkillResolution skill, bool isBasicStrike)
        {
            return HitMods(skill, isBasicStrike, false).CritChanceAdd;
        }

        float WeaponDurationMult(in SkillResolution skill)
        {
            if (skill.IsEmpty)
                return 1f;
            bool sustained = IsSustained(skill);
            WeaponPassiveMods mods = Evaluate(skill, false, sustained, false);
            return mods.DurationMult;
        }

        float WeaponCooldownMult() =>
            EquippedProfile != null ? EquippedProfile.CooldownMult : 1f;

        void StopBasicCannonAtFirstBody(LivingEffect logic, Vector3 origin, Vector3 facing)
        {
            WeaponCombatProfile profile = EquippedProfile;
            if (logic == null || profile == null || profile.HitShape != "ballistic" || _boss == null)
                return;
            float reach = WeaponBasicReach(profile.BasicReachM > 0f ? profile.BasicReachM : 25f);
            Vector3 boss = _boss.transform.position;
            if (!CannonShot.TryImpact(
                    origin.x, origin.z, facing.x, facing.z, reach,
                    boss.x, boss.z, BossBodyRadius(),
                    out _, out _, out float dist))
                return;
            logic.StopAt(dist);
        }

        float WeaponBasicReach(float fallback)
        {
            WeaponCombatProfile profile = EquippedProfile;
            if (profile == null || profile.BasicReachM <= 0f)
                return fallback;
            return profile.BasicReachM;
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
            if (kind == SkillExecutorKind.Movement)
                return;
            WeaponCombatProfile profile = EquippedProfile;
            if (profile == null)
                return;
            if (profile.ReachM > 0f)
                range = profile.ReachM;
            WeaponPassiveMods arcMods = HitMods(skill, false, false);
            float arc = arcMods.ArcDeg;
            if (arc > 0f)
            {
                shape = "cone";
                angleDeg = arc;
            }
            else if (!string.IsNullOrEmpty(profile.HitShape))
            {
                shape = profile.HitShape switch
                {
                    "arrow" or "page" => "line",
                    "ballistic" or "beam" or "orb" => "sphere",
                    "seal" or "point" or "ground" => "point",
                    "wall" => "box",
                    _ => shape
                };
            }
            if (profile.RadiusM > 0f)
                radius = profile.RadiusM;
            else if (profile.HitShape is "arrow" or "page" && profile.BasicRadiusM > 0f)
                radius = profile.BasicRadiusM;
            if (!profile.EffectTravels && target != null)
            {
                origin = target.position;
                speed = 0f;
                range = Mathf.Max(range, 0.5f);
            }
            else if (profile.HitShape == "orb" && _player != null)
            {
                origin = new Vector3(_orb.X, _player.position.y, _orb.Z);
                if (profile.OrbSpellM > 0f)
                    range = profile.OrbSpellM;
            }
            if (profile.Passive.Id == "uzun_buyu" && angleDeg <= 0f && profile.SwapBonus.AreaMult > 1f
                && _weaponPassives.BonusArmed(_clock != null ? _clock.Director.WorldTimeMs : 0))
            {
                range *= profile.SwapBonus.AreaMult;
                radius *= profile.SwapBonus.AreaMult;
            }
        }

        void TickOrb(double worldMs)
        {
            if (_player == null || EquippedProfile == null || EquippedProfile.HitShape != "orb")
                return;
            Vector3 p = _player.position;
            if (_orb.AtHand && !_orb.IsMoving)
                _orb.SnapToHand(p.x, p.z);
            _orb.Tick(worldMs, p.x, p.z);
        }

        public bool TryPlaceOrb(float targetX, float targetZ)
        {
            if (_player == null || EquippedProfile == null || EquippedProfile.OrbPlaceM <= 0f || _clock == null)
                return false;
            Vector3 p = _player.position;
            return _orb.TryPlace(p.x, p.z, targetX, targetZ, _clock.Director.WorldTimeMs, EquippedProfile);
        }

        public bool TryRecallOrb()
        {
            if (_player == null || EquippedProfile == null || _clock == null)
                return false;
            Vector3 p = _player.position;
            return _orb.TryRecall(p.x, p.z, _clock.Director.WorldTimeMs, EquippedProfile);
        }

        /// <summary>
        /// K3: silah düğmesi dokunuşu her zaman <see cref="TryRequestWeaponSwap"/>. Küre kuşanılıyken
        /// düğmeyi bu kadar (JSON weapons[].orb.hold_sec) basılı tutmak <see cref="ToggleOrb"/> yapar; 0 = uzun basma yok.
        /// </summary>
        public float SwapButtonHoldSec => IsOrbWeapon() ? EquippedProfile.OrbHoldSec : 0f;

        /// <summary>Editör kısayolu ve HUD. Çizim alanına dokunmaz.</summary>
        public bool ToggleOrb()
        {
            if (!IsOrbWeapon() || _player == null || _clock == null)
                return false;
            if (OrbHudCommand.Tap(_orb.AtHand) == OrbGestureResult.Place)
            {
                if (!TryCurrentOrbTarget(out float x, out float z))
                    return false;
                return TryPlaceOrb(x, z);
            }
            return TryRecallOrb();
        }

        bool IsOrbWeapon()
        {
            WeaponCombatProfile profile = EquippedProfile;
            return profile != null && profile.HitShape == "orb" && profile.OrbPlaceM > 0f;
        }

        bool TryCurrentOrbTarget(out float x, out float z)
        {
            Transform mark = null;
            if (_targeting != null && _targeting.Selected != null && _targeting.Selected.IsAvailable)
                mark = _targeting.Selected.transform;
            if (mark == null && _boss != null)
                mark = _boss.transform;
            if (mark != null && mark != _player)
            {
                x = mark.position.x;
                z = mark.position.z;
                return true;
            }
            Vector3 face = FlatBodyForward();
            float dist = EquippedProfile != null && EquippedProfile.OrbPlaceM > 0f
                ? EquippedProfile.OrbPlaceM
                : 8f;
            Vector3 p = _player.position;
            x = p.x + face.x * dist;
            z = p.z + face.z * dist;
            return true;
        }

        void NoteWeaponCast(in SkillResolution skill)
        {
            WeaponCombatProfile profile = EquippedProfile;
            if (profile == null || _clock == null || skill.IsEmpty)
                return;
            if (profile.Passive.Id == "dolu_sayfa")
                _weaponPassives.NoteSkill(_clock.Director.WorldTimeMs, profile.Passive.ChainGapSec);
        }

        void OnWeaponSwapCompleted(EquipmentItem weapon)
        {
            _visual?.ResetBasicChain();
            WeaponCombatProfile profile = weapon != null ? weapon.Profile : null;
            if (profile == null || _clock == null)
                return;
            _weaponPassives.ArmSwapBonus(
                profile.Id,
                profile.SwapBonusId,
                _clock.Director.WorldTimeMs,
                profile.SwapBonus.WindowSec);
            if (profile.SwapBonus.SpawnsAtLastHit)
                _orb.SnapToHand(_lastHitX, _lastHitZ);
            WeaponIgnoresArmor = false;
        }

        void NoteShieldBlockIfGuarding()
        {
            WeaponCombatProfile profile = EquippedProfile;
            if (profile == null || profile.Passive.Id != "karsi_saldiri" || _clock == null || _player == null)
                return;
            if (!PerformingAttack && (_engine == null || _engine.State.Phase != SentencePhase.Recovering))
                return;
            if (_boss != null)
            {
                Vector3 toBoss = _boss.transform.position - _player.position;
                toBoss.y = 0f;
                if (toBoss.sqrMagnitude > 0.01f && Vector3.Dot(_player.forward, toBoss.normalized) < 0.2f)
                    return;
            }
            _weaponPassives.NoteBlock(_clock.Director.WorldTimeMs, profile.Passive.WindowSec);
        }

        bool HammerStunReady(double worldMs)
        {
            WeaponCombatProfile profile = EquippedProfile;
            if (profile == null || profile.Passive.Id != "yere_cakma")
                return true;
            return _weaponPassives.HammerReady(worldMs);
        }

        void CommitHammerStun(double worldMs, bool ready, bool alreadyHad, bool applied)
        {
            WeaponCombatProfile profile = EquippedProfile;
            if (profile == null || profile.Passive.Id != "yere_cakma")
                return;
            if (!WeaponPassiveRules.CommitStunOnLand(ready, alreadyHad, applied))
                return;
            _weaponPassives.CommitHammer(worldMs, profile.Passive.IcdSec);
        }

        void TryLandWeaponStun(in SkillResolution skill, bool isBasicStrike)
        {
            WeaponPassiveMods mods = HitMods(skill, isBasicStrike, false);
            if (!mods.Stun || mods.StunSec <= 0f || _bossStatus == null || _clock == null)
                return;
            double now = _clock.Director.WorldTimeMs;
            bool ready = _weaponPassives.HammerReady(now);
            bool had = _bossStatus.Board.Has(StatusKind.Stun);
            if (!ready || had)
                return;
            _bossStatus.Board.Apply(StatusKind.Stun, mods.StunSec * 1000.0, 1f);
            CommitHammerStun(now, ready, had, _bossStatus.Board.Has(StatusKind.Stun));
        }

        void TryConsumeCounterWindow()
        {
            if (EquippedProfile == null || EquippedProfile.Passive.Id != "karsi_saldiri" || _clock == null)
                return;
            _weaponPassives.TryConsumeBlock(_clock.Director.WorldTimeMs);
        }

        void RememberHitPoint(Vector3? point)
        {
            if (!point.HasValue)
                return;
            _lastHitX = point.Value.x;
            _lastHitZ = point.Value.z;
        }

        WeaponPassiveMods HitMods(in SkillResolution skill, bool isBasicStrike, bool consumeBonus)
        {
            WeaponPassiveMods mods = Evaluate(skill, isBasicStrike, IsSustained(skill), true);
            return WithBonus(mods, peek: !consumeBonus);
        }

        WeaponPassiveMods WithBonus(WeaponPassiveMods mods, bool peek)
        {
            if (_equippedWeapon == null || _clock == null || EquippedProfile == null)
                return mods;
            double now = _clock.Director.WorldTimeMs;
            WeaponSwapBonusSpec bonus;
            bool armed = peek
                ? _weaponPassives.PeekBonus(EquippedProfile.Id, now, out bonus, EquippedProfile)
                : _weaponPassives.TryConsumeBonus(EquippedProfile.Id, now, out bonus, EquippedProfile);
            if (!armed)
                return mods;
            if (!peek)
                WeaponIgnoresArmor = bonus.IgnoreArmor;
            float damage = mods.DamageMult * bonus.DamageMult;
            if (bonus.CountsAsBackstab && EquippedProfile.Passive.Id == "sirt_vurusu")
                damage *= EquippedProfile.Passive.DamageMult;
            if (bonus.CountsAsStill && EquippedProfile.Passive.Id == "sabit_nisan")
                damage *= EquippedProfile.Passive.DamageMult;
            return new WeaponPassiveMods(
                damage,
                mods.HealMult,
                mods.DurationMult,
                mods.CritChanceAdd,
                mods.Stun,
                mods.StunSec,
                mods.ArcAllies,
                bonus.ArcDeg > 0f ? bonus.ArcDeg : mods.ArcDeg,
                bonus.IgnoreArmor,
                bonus.Cleanse || mods.CleanseOne,
                bonus.Shield,
                bonus.ShieldSec,
                bonus.FreeMana,
                bonus.AreaMult,
                bonus.PoiseMult);
        }

        WeaponPassiveMods Evaluate(in SkillResolution skill, bool isBasicStrike, bool sustained, bool harmful)
        {
            WeaponCombatProfile profile = EquippedProfile;
            if (profile == null)
                return WeaponPassiveMods.Identity;
            int verb = skill.IsEmpty ? 1 : VerbOf(skill.SkillId);
            bool enabled = isBasicStrike || _equippedWeapon.IsCompatibleWithVerb(verb);
            float sinceMoved = _clock == null ? 99f : (float)((_clock.Director.WorldTimeMs - _lastMovedMs) / 1000.0);
            int chain = 0;
            if (profile.Passive.Id == "dolu_sayfa" && _clock != null)
                chain = _weaponPassives.EffectiveChain(
                    _clock.Director.WorldTimeMs, profile.Passive.ChainGapSec);
            var query = new WeaponPassiveQuery(
                verb,
                enabled,
                harmful,
                sustained,
                BehindAngleDeg(),
                sinceMoved,
                _weaponPassives.CounterArmed(_clock != null ? _clock.Director.WorldTimeMs : 0),
                chain,
                OrbAngleDeg(),
                WeaponPassiveRules.IsSupportVerb(verb));
            return WeaponPassiveRules.Evaluate(profile, query);
        }

        static bool IsSustained(in SkillResolution skill)
        {
            if (skill.IsEmpty || skill.EngineModifiers.IsNull)
                return false;
            JsonValue engine = skill.EngineModifiers;
            if (engine["channel_sec"].AsFloat(0f) > 0f)
                return true;
            string id = skill.SkillId;
            return id is "1-12" or "12-12" or "2-12" or "5-12" or "8-12";
        }

        float BehindAngleDeg()
        {
            if (_player == null || _boss == null)
                return 180f;
            Vector3 back = -_boss.transform.forward;
            back.y = 0f;
            Vector3 fromBoss = _player.position - _boss.transform.position;
            fromBoss.y = 0f;
            if (back.sqrMagnitude < 0.0001f || fromBoss.sqrMagnitude < 0.0001f)
                return 180f;
            return Vector3.Angle(back, fromBoss);
        }

        float OrbAngleDeg()
        {
            if (_player == null || _boss == null)
                return 0f;
            Vector3 boss = _boss.transform.position;
            Vector3 toPlayer = _player.position - boss;
            Vector3 toOrb = new Vector3(_orb.X - boss.x, 0f, _orb.Z - boss.z);
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f || toOrb.sqrMagnitude < 0.0001f)
                return 0f;
            return Vector3.Angle(toPlayer, toOrb);
        }

        void TryCannonBlast(float impactX, float impactZ)
        {
            WeaponCombatProfile profile = EquippedProfile;
            if (profile == null || profile.HitShape != "ballistic")
                return;
            if (_motionBody != null && _motionBody.IsDisplacing)
            {
                _cannonQueued = true;
                _cannonImpactX = impactX;
                _cannonImpactZ = impactZ;
                return;
            }

            ApplyCannonBlast(impactX, impactZ);
        }

        void TickCannonRecoil()
        {
            bool playing = _motionBody != null && _motionBody.IsDisplacing;
            if (_cannonQueued && !playing)
            {
                _cannonQueued = false;
                ApplyCannonBlast(_cannonImpactX, _cannonImpactZ);
            }
            if (!playing)
                _recoilInTemplate = false;
        }

        void ApplyCannonBlast(float impactX, float impactZ)
        {
            WeaponCombatProfile profile = EquippedProfile;
            if (profile == null || _player == null)
                return;
            float splash = profile.BasicRadiusM > 0f ? profile.BasicRadiusM : 3f;
            float bossPush = profile.BossPushM > 0f ? profile.BossPushM : 0.5f;
            float arena = _motor != null ? _motor.Tuning.ArenaHalfSizeM : 50f;
            float playerR = PlayerBodyRadiusM();
            float bossR = 0.85f;
            if (_boss != null && _boss.BodyRadiusM > 0.01f)
                bossR = _boss.BodyRadiusM;

            if (_boss != null)
            {
                Vector3 boss = _boss.transform.position;
                float dx = boss.x - impactX;
                float dz = boss.z - impactZ;
                if (dx * dx + dz * dz <= splash * splash)
                    _boss.React(new Vector3(impactX, boss.y, impactZ), bossPush, 0f, 0.12f, WorldTimeMs);
            }

            PushCannonBodies(impactX, impactZ, splash, arena, bossR);

            if (profile.RecoilM <= 0f || _recoilInTemplate || _casterRecoilSuppressed)
                return;
            Vector3 player = _player.position;
            float dirX = player.x - impactX;
            float dirZ = player.z - impactZ;
            if (dirX * dirX + dirZ * dirZ < 0.0001f)
            {
                dirX = -_player.forward.x;
                dirZ = -_player.forward.z;
            }

            float x = player.x;
            float z = player.z;
            float bossX = _boss != null ? _boss.transform.position.x : player.x;
            float bossZ = _boss != null ? _boss.transform.position.z : player.z;
            _cannonRecoil.Queue(
                dirX, dirZ, profile.RecoilM, bossX, bossZ,
                playerR + bossR + 0.15f, arena, playerR);
            if (_cannonRecoil.TryApply(false, ref x, ref z))
                _player.position = new Vector3(x, player.y, z);
        }

        void PushCannonBodies(float impactX, float impactZ, float splash, float arena, float bossR)
        {
            _cannonBodies.Clear();
            SummonExecutor[] summons = FindObjectsByType<SummonExecutor>(FindObjectsSortMode.None);
            for (int i = 0; i < summons.Length; i++)
            {
                if (summons[i] != null)
                    summons[i].CollectWithin(impactX, impactZ, splash, _cannonBodies);
            }

            IReadOnlyList<Targetable> targets = Targetable.Live;
            float splashSq = splash * splash;
            for (int i = 0; i < targets.Count; i++)
            {
                Targetable target = targets[i];
                if (target == null)
                    continue;
                Transform body = target.transform;
                if (body == _player || (_boss != null && body == _boss.transform) || (_ally != null && body == _ally.transform))
                    continue;
                float dx = body.position.x - impactX;
                float dz = body.position.z - impactZ;
                if (dx * dx + dz * dz <= splashSq)
                    _cannonBodies.Add(body);
            }

            float bossX = _boss != null ? _boss.transform.position.x : impactX;
            float bossZ = _boss != null ? _boss.transform.position.z : impactZ;
            float minSep = 0.4f + bossR + 0.15f;
            for (int i = 0; i < _cannonBodies.Count; i++)
            {
                Transform body = _cannonBodies[i];
                if (body == null)
                    continue;
                float x = body.position.x;
                float z = body.position.z;
                CannonBlast.Move(ref x, ref z, x - impactX, z - impactZ, splash, bossX, bossZ, minSep, arena, 0.4f);
                body.position = new Vector3(x, body.position.y, z);
            }
        }

        void CutTemplateForSwap(bool instant)
        {
            if (_motionBody != null && _motionBody.IsDisplacing)
            {
                _motionBody.Stop();
                _visual?.EndMotionAnim();
            }
            if (_engine != null && _engine.State.Phase == SentencePhase.Recovering)
                _engine.Abort();
            if (instant && _clock != null && _weaponSwap != null)
                _swapInstantDrawUntilMs = _clock.Director.WorldTimeMs + _weaponSwap.Rules.AnimationSec * 1000.0;
        }
    }
}
