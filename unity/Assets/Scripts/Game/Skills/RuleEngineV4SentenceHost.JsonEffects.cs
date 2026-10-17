using Dovus.App.Casting;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using Dovus.Game.Skills.RuleEngineV4;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class RuleEngineV4SentenceHost
    {
        internal void NoteJsonCast(in SkillResolution skill, ClosingHit closing)
        {
        }

        internal int FriendlyTargetCap(in SkillResolution skill) => 1;

        float ConsumeOverflowBonus(bool isBasicStrike) => 0f;

        internal void OnJsonShieldBlocked()
        {
        }

        internal void ApplyReflectedDamage(float amount)
        {
        }

        internal void ApplyPurgePower(in SkillResolution skill, int removed)
        {
        }

        internal int JsonCleanseCount(SkillResolution skill) => 0;

        internal void ShareFriendlyStatuses(in SkillResolution skill, StatusBoard applied)
        {
        }

        internal void ApplyHealOverflow(in SkillResolution skill, int amount, int healed, bool toAlly)
        {
        }

        internal bool TryBounceFriendly(float power) => false;

        internal bool LandingFieldAllows(in SkillResolution skill) => true;

        RuleEngineV4CatalogAccess _basicCadenceCatalog;

        internal bool TryEquippedBasicWeapon(out RuleEngineV4Weapon weapon)
        {
            weapon = default;
            if (_equippedWeapon?.Profile == null)
                return false;
            _basicCadenceCatalog ??= new RuleEngineV4CatalogAccess();
            return _basicCadenceCatalog.Catalog.TryGetWeapon(_equippedWeapon.Profile.Id, out weapon);
        }

        internal bool BasicCadenceReady(double now)
        {
            if (_combat != null && !_combat.EnforceCooldown)
                return true;
            if (!TryEquippedBasicWeapon(out RuleEngineV4Weapon weapon))
                return true;
            return BasicStrikeCadenceRules.CadenceReady(
                now,
                SkillWorld.LastBasicStrikeMs,
                weapon.PrefireSec,
                weapon.RecoverySec);
        }

        internal void ApplyBasicStrikePrefireLock(float prefireSec)
        {
            if (_playerStatus == null || prefireSec <= 0f)
                return;
            double now = _clock != null ? _clock.Director.WorldTimeMs : 0;
            _playerStatus.GrantCastMobility(
                SkillMobility.Rooted,
                now + prefireSec * Dovus.Core.Shared.Units.SecToMs);
        }

        internal int BasicHitsNow() => 1;

        internal void ScheduleBasicSubHits(ClosingHit closing, int hits, float reach)
        {
        }

        internal void ApplyBasicExtras(float dealt, int hits)
        {
        }

        internal bool IsEnemyBody(Transform body)
        {
            if (body == null)
                return false;
            if (_boss != null && body == _boss.transform)
                return true;
            if (_liveTargetables != null)
            {
                foreach (TargetableHost t in _liveTargetables.Live)
                {
                    if (t != null && t.transform == body)
                        return true;
                }
            }
            return false;
        }
    }
}
