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
using Dovus.Core.Shared;
using Dovus.Core.Manifestation;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Weapons;
using System;
using UnityEngine;

namespace Dovus.Game.Skills.Weapons
{
    public sealed class WeaponPassiveRuntime
    {
        readonly IWeaponPassiveRuntimeHost _host;
        readonly WeaponPassiveState _passives = new();
        double _swapInstantDrawUntilMs;

        public WeaponPassiveRuntime(IWeaponPassiveRuntimeHost host) => _host = host;



        /// <summary>Yay değiştirme bonusu: sıradaki vuruş zırhı yok sayar.</summary>
        public bool SwapDrawUnlocked(double worldMs) => worldMs < _swapInstantDrawUntilMs;

        public float WeaponOutgoingDamageMult(in SkillResolution skill, bool isBasicStrike)
        {
            return HitMods(skill, isBasicStrike, false).DamageMult;
        }

        public bool TryTakeFreeMana()
        {
            WeaponCombatProfile profile = _host.EquippedProfile;
            if (profile == null || !profile.SwapBonus.FreeMana || _host.Clock == null)
                return false;
            return _passives.TryConsumeBonus(
                profile.Id, _host.Clock.Director.WorldTimeMs, out _, profile);
        }

        public void ConsumeWeaponBonus(StatusBoard cleanseTarget = null)
        {
            if (_host.EquippedWeapon == null || _host.Clock == null || _host.EquippedProfile == null)
                return;
            if (!_passives.TryConsumeBonus(
                    _host.EquippedProfile.Id,
                    _host.Clock.Director.WorldTimeMs,
                    out WeaponSwapBonusSpec bonus,
                    _host.EquippedProfile))
                return;
            _host.WeaponIgnoresArmor = bonus.IgnoreArmor;
            if (bonus.Cleanse)
                OneNegativeCleanse.TryRemove(cleanseTarget);
            if (bonus.Shield > 0f)
                _host.GrantShortShield(bonus.Shield, bonus.ShieldSec);
        }

                public float WeaponSupportPower(in SkillResolution skill)
        {
            if (_host.EquippedWeapon == null)
                return 1f;
            int verb = VerbOf(skill.Identity.Id);
            bool enabled = _host.EquippedWeapon.IsCompatibleWithVerb(verb);
            float power = WeaponPassiveRules.SupportPower(
                _host.EquippedWeapon.Profile,
                enabled,
                verb,
                _host.WeaponCompatibilityFor(skill).DamageMult);
            float heal = HitMods(skill, false, false).HealMult;
            return heal > 1f ? heal : power;
        }

        /// <summary>Tılsım kuşanılıyken şifa, kalkan ve buff büyüklüğü. Hasar çarpanı değil.</summary>
        public float WeaponFriendlyScale()
        {
            if (_host.EquippedProfile == null || _host.EquippedProfile.Passive.Kind != WeaponPassiveKind.HolyEffect)
                return 1f;
            float heal = HitMods(SkillResolution.Empty, false, false).HealMult;
            return heal > 0f ? heal : 1f;
        }

        public float WeaponCritAdd(in SkillResolution skill, bool isBasicStrike)
        {
            return HitMods(skill, isBasicStrike, false).CritChanceAdd;
        }

        public float WeaponDurationMult(in SkillResolution skill)
        {
            if (skill.IsEmpty)
                return 1f;
            bool sustained = IsSustained(skill);
            WeaponPassiveMods mods = Evaluate(skill, false, sustained, false);
            return mods.DurationMult;
        }

        public float WeaponCooldownMult() =>
            _host.EquippedProfile != null ? _host.EquippedProfile.CooldownMult : 1f;

        public void StopBasicCannonAtFirstBody(LivingEffect logic, Vector3 origin, Vector3 facing)
        {
            WeaponCombatProfile profile = _host.EquippedProfile;
            if (logic == null || profile == null || profile.HitShape != "ballistic" || _host.Boss == null)
                return;
            float reach = WeaponBasicReach(profile.BasicReachM > 0f ? profile.BasicReachM : WeaponPassiveRuntimeDefaults.BasicReachFallbackM);
            Vector3 boss = _host.Boss.transform.position;
            if (!CannonShot.TryImpact(
                    origin.x, origin.z, facing.x, facing.z, reach,
                    boss.x, boss.z, _host.BossBodyRadius(),
                    out _, out _, out float dist))
                return;
            logic.StopAt(dist);
        }

        public float WeaponBasicReach(float fallback)
        {
            WeaponCombatProfile profile = _host.EquippedProfile;
            if (profile == null || profile.BasicReachM <= 0f)
                return fallback;
            return profile.BasicReachM;
        }

        public void ApplyWeaponDelivery(
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
            WeaponCombatProfile profile = _host.EquippedProfile;
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
            else if (profile.HitShape == "orb" && _host.Player != null)
            {
                origin = new Vector3(_host.Orb.X, _host.Player.position.y, _host.Orb.Z);
                if (profile.OrbSpellM > 0f)
                    range = profile.OrbSpellM;
            }
            if (profile.Passive.Kind == WeaponPassiveKind.LongEnchant && angleDeg <= 0f && profile.SwapBonus.AreaMult > 1f
                && _passives.BonusArmed(_host.Clock != null ? _host.Clock.Director.WorldTimeMs : 0))
            {
                range *= profile.SwapBonus.AreaMult;
                radius *= profile.SwapBonus.AreaMult;
            }
        }

        public void NoteWeaponCast(in SkillResolution skill)
        {
            WeaponCombatProfile profile = _host.EquippedProfile;
            if (profile == null || _host.Clock == null || skill.IsEmpty)
                return;
            if (profile.Passive.Kind == WeaponPassiveKind.FullPage)
                _passives.NoteSkill(_host.Clock.Director.WorldTimeMs, profile.Passive.ChainGapSec);
        }

        public void OnWeaponSwapCompleted(EquipmentItem weapon)
        {
            _host.Visual?.ResetBasicChain();
            WeaponCombatProfile profile = weapon != null ? weapon.Profile : null;
            if (profile == null || _host.Clock == null)
                return;
            _passives.ArmSwapBonus(
                profile.Id,
                profile.SwapBonusId,
                _host.Clock.Director.WorldTimeMs,
                profile.SwapBonus.WindowSec);
            if (profile.SwapBonus.SpawnsAtLastHit)
                _host.Orb.SnapToHand(_host.LastHitX, _host.LastHitZ);
            _host.WeaponIgnoresArmor = false;
        }

        public void NoteShieldBlockIfGuarding()
        {
            WeaponCombatProfile profile = _host.EquippedProfile;
            if (profile == null || profile.Passive.Kind != WeaponPassiveKind.CounterStrike || _host.Clock == null || _host.Player == null)
                return;
            if (!_host.PerformingAttack && (_host.Engine == null || _host.Engine.State.Phase != SentencePhase.Recovering))
                return;
            if (_host.Boss != null)
            {
                Vector3 toBoss = _host.Boss.transform.position - _host.Player.position;
                toBoss.y = 0f;
                if (toBoss.sqrMagnitude > WeaponPassiveRuntimeDefaults.ToBossDistEpsilonSqr && Vector3.Dot(_host.Player.forward, toBoss.normalized) < WeaponPassiveRuntimeDefaults.BackstabDotThreshold)
                    return;
            }
            _passives.NoteBlock(_host.Clock.Director.WorldTimeMs, profile.Passive.WindowSec);
        }

        public bool HammerStunReady(double worldMs)
        {
            WeaponCombatProfile profile = _host.EquippedProfile;
            if (profile == null || profile.Passive.Kind != WeaponPassiveKind.GroundSlam)
                return true;
            return _passives.HammerReady(worldMs);
        }

        public void CommitHammerStun(double worldMs, bool ready, bool alreadyHad, bool applied)
        {
            WeaponCombatProfile profile = _host.EquippedProfile;
            if (profile == null || profile.Passive.Kind != WeaponPassiveKind.GroundSlam)
                return;
            if (!WeaponPassiveRules.CommitStunOnLand(ready, alreadyHad, applied))
                return;
            _passives.CommitHammer(worldMs, profile.Passive.IcdSec);
        }

        public void TryLandWeaponStun(in SkillResolution skill, bool isBasicStrike)
        {
            WeaponPassiveMods mods = HitMods(skill, isBasicStrike, false);
            if (!mods.Stun || mods.StunSec <= 0f || _host.BossStatus == null || _host.Clock == null)
                return;
            double now = _host.Clock.Director.WorldTimeMs;
            bool ready = _passives.HammerReady(now);
            bool had = _host.BossStatus.Board.Has(StatusKind.Stun);
            if (!ready || had)
                return;
            _host.BossStatus.Board.Apply(StatusKind.Stun, mods.StunSec * SkillsTimeDefaults.SecToMs, 1f);
            CommitHammerStun(now, ready, had, _host.BossStatus.Board.Has(StatusKind.Stun));
        }

        public void TryConsumeCounterWindow()
        {
            if (_host.EquippedProfile == null || _host.EquippedProfile.Passive.Kind != WeaponPassiveKind.CounterStrike || _host.Clock == null)
                return;
            _passives.TryConsumeBlock(_host.Clock.Director.WorldTimeMs);
        }

        public void RememberHitPoint(Vector3? point)
        {
            if (!point.HasValue)
                return;
            _host.LastHitX = point.Value.x;
            _host.LastHitZ = point.Value.z;
        }

        public WeaponPassiveMods HitMods(in SkillResolution skill, bool isBasicStrike, bool consumeBonus)
        {
            WeaponPassiveMods mods = Evaluate(skill, isBasicStrike, IsSustained(skill), true);
            return WithBonus(mods, peek: !consumeBonus);
        }

        WeaponPassiveMods WithBonus(WeaponPassiveMods mods, bool peek)
        {
            if (_host.EquippedWeapon == null || _host.Clock == null || _host.EquippedProfile == null)
                return mods;
            double now = _host.Clock.Director.WorldTimeMs;
            WeaponSwapBonusSpec bonus;
            bool armed = peek
                ? _passives.PeekBonus(_host.EquippedProfile.Id, now, out bonus, _host.EquippedProfile)
                : _passives.TryConsumeBonus(_host.EquippedProfile.Id, now, out bonus, _host.EquippedProfile);
            if (!armed)
                return mods;
            if (!peek)
                _host.WeaponIgnoresArmor = bonus.IgnoreArmor;
            float damage = mods.DamageMult * bonus.DamageMult;
            if (bonus.CountsAsBackstab && _host.EquippedProfile.Passive.Kind == WeaponPassiveKind.Backstab)
                damage *= _host.EquippedProfile.Passive.DamageMult;
            if (bonus.CountsAsStill && _host.EquippedProfile.Passive.Kind == WeaponPassiveKind.SteadyAim)
                damage *= _host.EquippedProfile.Passive.DamageMult;
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
            WeaponCombatProfile profile = _host.EquippedProfile;
            if (profile == null)
                return WeaponPassiveMods.Identity;
            int verb = skill.IsEmpty ? 1 : VerbOf(skill.Identity.Id);
            bool enabled = isBasicStrike || _host.EquippedWeapon.IsCompatibleWithVerb(verb);
            float sinceMoved = _host.Clock == null ? WeaponPassiveRuntimeDefaults.SinceMovedFallbackSec : (float)((_host.Clock.Director.WorldTimeMs - _host.LastMovedMs) / SkillsTimeDefaults.SecToMs);
            int chain = 0;
            if (profile.Passive.Kind == WeaponPassiveKind.FullPage && _host.Clock != null)
                chain = _passives.EffectiveChain(
                    _host.Clock.Director.WorldTimeMs, profile.Passive.ChainGapSec);
            var query = new WeaponPassiveQuery(
                verb,
                enabled,
                harmful,
                sustained,
                BehindAngleDeg(),
                sinceMoved,
                _passives.CounterArmed(_host.Clock != null ? _host.Clock.Director.WorldTimeMs : 0),
                chain,
                OrbAngleDeg(),
                WeaponPassiveRules.IsSupportVerb(verb));
            return WeaponPassiveRules.Evaluate(profile, query);
        }

        public static bool IsSustained(in SkillResolution skill)
        {
            if (skill.IsEmpty || skill.Engine.IsNull)
                return false;
            if (skill.Engine.ChannelSec(0f) > 0f)
                return true;
            string id = skill.Identity.Id;
            return id is SkillIds.FlowingStrike or SkillIds.FlowingTime or SkillIds.FlowingHeal or SkillIds.FlowingBlast or SkillIds.FlowingAscent;
        }

        float BehindAngleDeg()
        {
            if (_host.Player == null || _host.Boss == null)
                return 180f;
            Vector3 back = -_host.Boss.transform.forward;
            back.y = 0f;
            Vector3 fromBoss = _host.Player.position - _host.Boss.transform.position;
            fromBoss.y = 0f;
            if (back.sqrMagnitude < 0.0001f || fromBoss.sqrMagnitude < 0.0001f)
                return 180f;
            return Vector3.Angle(back, fromBoss);
        }

        float OrbAngleDeg()
        {
            if (_host.Player == null || _host.Boss == null)
                return 0f;
            Vector3 boss = _host.Boss.transform.position;
            Vector3 toPlayer = _host.Player.position - boss;
            Vector3 toOrb = new Vector3(_host.Orb.X - boss.x, 0f, _host.Orb.Z - boss.z);
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f || toOrb.sqrMagnitude < 0.0001f)
                return 0f;
            return Vector3.Angle(toPlayer, toOrb);
        }

        public void SetSwapInstantDrawUntil(double worldMs) => _swapInstantDrawUntilMs = worldMs;

        static int VerbOf(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return 0;
            int dash = skillId.IndexOf('-');
            if (dash <= 0)
                return 0;
            return int.TryParse(skillId.Substring(0, dash), out int verb) ? verb : 0;
        }
    }
}
