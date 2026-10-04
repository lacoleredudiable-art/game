using System.Collections.Generic;
using Dovus.App.Casting;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Status;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Skills;
using Dovus.Game.Team;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Skills.Closing
{
    public sealed class ClosingDamageResolver
    {
        readonly IClosingDamageHost _host;
        readonly HashSet<LivingEffect> _closingStamped = new();
        static readonly Collider[] StrikeHits = new Collider[24];

        public ClosingDamageResolver(IClosingDamageHost host) => _host = host;

        public void ClearClosingStamp(LivingEffect logic) => _closingStamped.Remove(logic);

        public float Apply(
            ClosingHit closing,
            SkillResolution skill,
            bool isBasicStrike,
            float slashCommitMult,
            float effectScale = 1f,
            float? chainBonusOverride = null)
        {
            if (_host.BossVitals == null || _host.BossVitals.IsDown)
                return 0f;
            if (effectScale <= 0f)
                return 0f;

            DamageOutcome dealt = _host.ComputeOutgoingHit(
                closing, skill, isBasicStrike, slashCommitMult, effectScale, chainBonusOverride);
            if (dealt.Poise > 0f)
                _host.BossDirector?.ApplyPoiseDamage(dealt.Poise);
            float damage = dealt.Amount;
            bool isCrit = dealt.WasCrit;

            if (damage <= 0f)
            {
                _host.LastClosingDamageDealt = 0f;
                return 0f;
            }

            if (!isBasicStrike)
                _host.TryConsumeCounterWindow();

            _host.RememberHitPoint(_host.BossHitPoint());
            if (!isBasicStrike && !_host.JsonTickDamage)
                _host.TryCannonBlast(_host.LastHitX, _host.LastHitZ);
            _host.ConsumeWeaponBonus(_host.BossStatus != null ? _host.BossStatus.Board : null);
            _host.LastClosingDamageDealt = damage;
            _host.DamageHud?.ShowDamage(damage, isCrit, _host.BossHitPoint(), _host.DamageTint(), victimIsBoss: true);

            float lifesteal = _host.SlotPassives?.LifestealAddFor(_host.SlotQueryCastId) ?? 0f;
            lifesteal += ClosingHealRules.AdjectiveLifesteal(skill);
            lifesteal += PortalBorderTeamHost.Hub.LifestealAdd;
            if (lifesteal > 0f)
            {
                var vitals = _host.CachedPlayerVitals();
                int healAmt = Mathf.RoundToInt(damage * lifesteal);
                if (vitals != null && healAmt > 0)
                    vitals.ApplyHeal(healAmt);
            }

            bool killed = _host.BossVitals.ApplyDamage(damage);
            var bossVisual = _host.Boss != null ? _host.Boss.GetComponent<BossVisual>() : null;
            if (killed)
                return damage;

            _host.NotifyBossStruck(isCrit, allowHitstop: true);
            bossVisual?.PlayStagger();
            _host.StatusApplier.ApplySlotPassiveHitExtras(damage);
            return damage;
        }

        public void StampScar(LivingEffectView view, ClosingHit closing)
        {
            LivingEffect logic = view != null ? view.Logic : null;
            if (logic == null || _closingStamped.Contains(logic))
                return;

            Vector3 along = new Vector3(logic.DirX, 0f, logic.DirZ);
            Vector3 tip = new Vector3(logic.TipX, 0f, logic.TipZ);
            float scale = view.IsBasicStrike
                ? _host.Combat.Manifestation.BasicStrikeScarScaleM * (0.7f + 0.15f * closing.DotCount)
                : _host.Combat.Manifestation.ScarScaleM * (0.7f + 0.15f * closing.DotCount);

            ScarKind kind = view.IsBasicStrike
                ? ScarKind.Strike
                : closing.Type switch
                {
                    Rune.Aydinlik => ScarKind.Crack,
                    Rune.Ates => ScarKind.Needle,
                    Rune.Su => ScarKind.Swarm,
                    Rune.Toprak => ScarKind.Acid,
                    _ => ScarKind.Crack
                };

            if (kind == ScarKind.Crack && logic.Current.Focus > 0.5f)
            {
                Vector3 origin = new Vector3(logic.OriginX, 0f, logic.OriginZ);
                for (int i = 1; i <= 3; i++)
                {
                    float u = i / 3f;
                    _host.Scars.Stamp(Vector3.Lerp(origin, tip, u), scale * 0.85f, kind, along);
                }
            }
            else
            {
                _host.Scars.Stamp(tip, scale, kind, along);
            }

            _closingStamped.Add(logic);
        }

        public void ApplyBossClosingBasic(LivingEffect logic, ClosingHit closing)
        {
            if (logic == null)
                return;
            _host.NoteImpactOrigin(logic);
            if (_host.Boss == null || (_host.BossVitals != null && _host.BossVitals.IsDown))
                return;
            if (!IsClosingInRange(logic, closing))
                return;

            var man = _host.Combat.Manifestation;
            _host.Boss.React(
                new Vector3(logic.OriginX, 0f, logic.OriginZ),
                knockbackM: 0f,
                liftM: 0f,
                shakeSec: man.BossShakeSec * 0.35f,
                _host.Clock.Director.WorldTimeMs);
        }

        public void ApplyBossClosing(LivingEffect logic, ClosingHit closing, SkillResolution skill)
        {
            _host.NoteImpactOrigin(logic);
            if (_host.Boss == null || (_host.BossVitals != null && _host.BossVitals.IsDown))
                return;

            if (!skill.IsEmpty && StatusApplicator.IsSelfTargeted(skill))
            {
                if (!IsClosingInRange(logic, closing))
                    return;
                _host.Boss.React(
                    new Vector3(logic.OriginX, 0f, logic.OriginZ),
                    _host.Combat.Manifestation.BossKnockbackM * 0.08f,
                    0.04f,
                    _host.Combat.Manifestation.BossShakeSec * 0.35f,
                    _host.Clock.Director.WorldTimeMs);
                return;
            }

            Vector3 from = new Vector3(logic.OriginX, 0f, logic.OriginZ);
            var man = _host.Combat.Manifestation;
            float knock = man.BossKnockbackM;
            float lift = 0f;
            float shake = man.BossShakeSec;
            double worldMs = _host.Clock.Director.WorldTimeMs;

            string family = skill.IsEmpty ? string.Empty : skill.VerbFamily;
            if (!string.IsNullOrEmpty(family))
            {
                switch (family)
                {
                    case "strike":
                        knock = man.BossKnockbackM * (1.85f + 0.4f * logic.Current.Pierce);
                        lift = 0.05f;
                        shake = man.BossShakeSec * 0.55f;
                        break;
                    case "disrupt":
                        knock = man.BossKnockbackM * 0.12f;
                        lift = 0.08f;
                        shake = man.BossShakeSec * 1.6f;
                        if (!IsClosingInRange(logic, closing))
                            return;
                        _host.Boss.React(from, knock, lift, shake * 0.45f, worldMs);
                        _host.Boss.React(from + new Vector3(logic.DirZ, 0f, -logic.DirX) * 0.35f,
                            knock * 0.6f, lift * 0.5f, shake * 0.55f, worldMs);
                        _host.Boss.React(from + new Vector3(-logic.DirZ, 0f, logic.DirX) * 0.35f,
                            knock * 0.6f, lift * 0.5f, shake * 0.55f, worldMs);
                        return;
                    case "control":
                        if (!IsClosingInRange(logic, closing))
                            return;
                        _host.Boss.Pin(0.7f, worldMs);
                        return;
                    case "zone":
                        knock = man.BossKnockbackM * 0.55f;
                        lift = man.BossLiftM * (1.15f + 0.35f * logic.Current.Lift);
                        shake = man.BossShakeSec * 0.9f;
                        break;
                    case "motion":
                        knock = man.BossKnockbackM * 0.9f;
                        lift = 0.12f;
                        shake = man.BossShakeSec * 0.7f;
                        break;
                    case "special":
                        knock = man.BossKnockbackM * 0.25f;
                        lift = 0.2f;
                        shake = man.BossShakeSec * 1.1f;
                        break;
                    default:
                        break;
                }

                if (!IsClosingInRange(logic, closing))
                    return;
                _host.Boss.React(from, knock, lift, shake, worldMs);
                return;
            }

            switch (closing.Type)
            {
                case Rune.Aydinlik:
                    knock = man.BossKnockbackM * 0.55f;
                    lift = man.BossLiftM * (1.15f + 0.35f * logic.Current.Lift);
                    shake = man.BossShakeSec * 0.9f;
                    break;
                case Rune.Ates:
                    knock = man.BossKnockbackM * (1.85f + 0.4f * logic.Current.Pierce);
                    lift = 0.05f;
                    shake = man.BossShakeSec * 0.55f;
                    break;
                case Rune.Su:
                    knock = man.BossKnockbackM * 0.12f;
                    lift = 0.08f;
                    shake = man.BossShakeSec * 1.6f;
                    if (!IsClosingInRange(logic, closing))
                        return;
                    _host.Boss.React(from, knock, lift, shake * 0.45f, worldMs);
                    _host.Boss.React(from + new Vector3(logic.DirZ, 0f, -logic.DirX) * 0.35f,
                        knock * 0.6f, lift * 0.5f, shake * 0.55f, worldMs);
                    _host.Boss.React(from + new Vector3(-logic.DirZ, 0f, logic.DirX) * 0.35f,
                        knock * 0.6f, lift * 0.5f, shake * 0.55f, worldMs);
                    return;
                case Rune.Hava:
                    if (!IsClosingInRange(logic, closing))
                        return;
                    _host.Boss.Pin(0.55f, worldMs);
                    return;
                case Rune.Toprak:
                    knock = man.BossKnockbackM * 0.2f;
                    lift = 0f;
                    shake = man.BossShakeSec * 0.7f;
                    break;
            }

            if (!IsClosingInRange(logic, closing))
                return;

            _host.Boss.React(from, knock, lift, shake, worldMs);
        }

        public bool IsBossInStrikeCapsule(LivingEffect logic, float reachM)
        {
            if (_host.Boss == null || _host.Player == null || logic == null)
                return false;
            ManifestationTuning man = _host.Combat.Manifestation;
            float radius = man.BasicStrikeRadiusM;
            Vector3 dir = new Vector3(logic.DirX, 0f, logic.DirZ);
            if (dir.sqrMagnitude < 0.0001f)
                return false;
            dir.Normalize();
            StrikeCapsule.Segment(_host.PlayerBodyRadiusM(), reachM, radius, out float nearM, out float farM);
            Vector3 chest = _host.Player.position + Vector3.up * man.StrikeChestOffsetM;
            Vector3 low = chest + dir * nearM;
            Vector3 high = chest + dir * farM;
            int count = Physics.OverlapCapsuleNonAlloc(
                low, high, radius, StrikeHits, Physics.AllLayers, QueryTriggerInteraction.Collide);
            Transform bossT = _host.Boss.transform;
            for (int i = 0; i < count; i++)
            {
                Transform hit = StrikeHits[i].transform;
                if (hit == bossT || hit.IsChildOf(bossT))
                    return true;
            }
            return false;
        }

        public bool BasicTargetStillInReach(Transform target, float reachM)
        {
            if (target == null || _host.Player == null || _host.Boss == null)
                return false;
            if (target != _host.Boss.transform && !target.IsChildOf(_host.Boss.transform))
                return false;
            Targetable mark = target.GetComponent<Targetable>();
            if (mark == null)
                mark = target.GetComponentInParent<Targetable>();
            if (mark != null && !mark.IsAvailable)
                return false;
            float dist = mark != null
                ? mark.DistanceFrom(_host.Player.position)
                : PlanarMath.FlatDistance(
                    _host.Player.position.x,
                    _host.Player.position.z,
                    target.position.x,
                    target.position.z);
            return StrikeCapsule.EdgeInReach(dist, _host.PlayerBodyRadiusM(), reachM);
        }

        public float BasicStrikeYawDeg(Transform target)
        {
            if (_host.Player == null || target == null)
                return 0f;
            Vector3 to = target.position - _host.Player.position;
            to.y = 0f;
            Vector3 fwd = _host.Player.forward;
            fwd.y = 0f;
            if (to.sqrMagnitude < 0.0001f || fwd.sqrMagnitude < 0.0001f)
                return 0f;
            return Vector3.Angle(fwd, to);
        }

        public bool IsClosingInRange(LivingEffect logic, ClosingHit closing)
        {
            if (logic == null || _host.Boss == null)
                return false;
            Vector3 bossPos = _host.Boss.transform.position;
            float bang = _host.Combat.Manifestation.ClosingBangRadiusM;
            return ClosingRangeRules.IsClosingInRange(
                logic,
                bossPos.x,
                bossPos.z,
                closing.Type,
                bang,
                logic.BangRadiusM);
        }
    }
}
