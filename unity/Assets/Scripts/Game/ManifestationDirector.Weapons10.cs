using System;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// 10 silah teslimi, pasif ve silah kesme. Hasar formülüne dokunmaz;
    /// çarpanı kapanışın üstüne ekler. Hareket kalıbının yolunu yazmaz.
    /// </summary>
    public sealed partial class ManifestationDirector
    {
        readonly WeaponPassiveState _weaponPassives = new();
        readonly OrbAnchor _orb = new();
        double _swapInstantDrawUntilMs;
        float _lastHitX;
        float _lastHitZ;

        /// <summary>Hasar ajanı bunu okur. Kuşanılmış silahın temel zırhı.</summary>
        public float EquippedBaseArmor => _equippedWeapon != null ? _equippedWeapon.BaseArmor : 0f;

        /// <summary>Yay değiştirme bonusu: sıradaki vuruş zırhı yok sayar.</summary>
        public bool WeaponIgnoresArmor { get; private set; }

        public WeaponCombatProfile EquippedProfile => _equippedWeapon != null ? _equippedWeapon.Profile : null;

        public OrbAnchor Orb => _orb;

        bool SwapDrawUnlocked(double worldMs) => worldMs < _swapInstantDrawUntilMs;

        float WeaponOutgoingDamageMult(in SkillResolution skill, bool isBasicStrike)
        {
            return HitMods(skill, isBasicStrike, false).DamageMult;
        }

        void ConsumeWeaponBonus()
        {
            if (_equippedWeapon == null || _clock == null || EquippedProfile == null)
                return;
            if (_weaponPassives.TryConsumeBonus(
                    EquippedProfile.Id,
                    _clock.Director.WorldTimeMs,
                    out WeaponSwapBonusSpec bonus,
                    EquippedProfile))
                WeaponIgnoresArmor = bonus.IgnoreArmor;
        }

        float WeaponSupportPower(in SkillResolution skill)
        {
            if (_equippedWeapon == null)
                return 1f;
            int verb = VerbOf(skill.SkillId);
            bool enabled = _equippedWeapon.IsCompatibleWithVerb(verb);
            return WeaponPassiveRules.SupportPower(
                _equippedWeapon.Profile,
                enabled,
                verb,
                WeaponCompatibilityFor(skill).DamageMult);
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
            if (profile.ArcDeg > 0f)
            {
                shape = "cone";
                angleDeg = profile.ArcDeg;
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

        bool HammerStunAllowed(double worldMs)
        {
            WeaponCombatProfile profile = EquippedProfile;
            if (profile == null || profile.Passive.Id != "yere_cakma")
                return true;
            return _weaponPassives.TryHammerStun(worldMs, profile.Passive.IcdSec);
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
            int chain = profile.Passive.Id == "dolu_sayfa" ? _weaponPassives.ChainCount : 0;
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
