using Dovus.Core;
using Dovus.Core.Combat;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.DevTools;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Skills.Motion
{
    public sealed class MotionHitResolver
    {
        readonly IMotionHitResolverHost _host;

        public MotionHitResolver(IMotionHitResolverHost host) => _host = host;

        public void OnMotionTemplateHit(MotionHit hit)
        {
            int prevCast = _host.SlotQueryCastId;
            _host.SlotQueryCastId = _host.Motion.TemplateSlotCastId;
            try
            {
            if (hit.Payload == "marker" && hit.Anchor == "plant")
            {
                _host.Motion.SpawnFuse(hit);
                return;
            }

            _host.Motion.SpawnMotionHitVisual(hit);
            if (hit.Anchor == "plant")
                _host.Motion.ClearFuse();
            if (hit.Payload == "none" || _host.Motion.TemplateSkill.IsEmpty)
                return;

            double now = _host.Clock != null ? _host.Clock.Director.WorldTimeMs : 0;
            if (_host.TemplateDelivery.ShouldDeferTemplateGameplay(now))
            {
                _host.TemplateDelivery.NoteDeferredHit(hit.Share);
                return;
            }

            if (_host.TemplateDelivery.TemplateHitAlreadyDetonated())
                return;

            bool friendly = _host.IsFriendlyFieldVerb(_host.Motion.TemplateSkill) || _host.IsHealSkill(_host.Motion.TemplateSkill);
            bool geometry = BossReachedMotionHit(hit);
            bool arc = !friendly && !geometry && _host.TemplateDelivery.WeaponArcConnects(hit);
            if (!friendly && !geometry && !arc)
                arc = _host.AoeReachedMotionHit(hit);
            bool reached = friendly || geometry;
            if (!friendly && (geometry || arc))
            {
                _host.ApplyClosingDamage(
                    _host.Motion.TemplatePending.Closing,
                    _host.Motion.TemplateSkill,
                    false,
                    0f,
                    hit.Share,
                    _host.Motion.TemplateChain);
            }

            // Fiil hasarı kapanışta iner. Emici aktarımın eksi canı base_damage 0 iken
            // ayrıca boss'a yazılır; yoksa 1-2 gibi vuruşlar iki kez vurur.
            if (geometry && _host.Motion.TemplateSkill.BaseDamage <= 0.01f)
                ApplyDrainDamage(hit.Share);

            // 2-9 şifası koruyucu tetikte bir kez iner; kalıp vuruşu aynı cast'i ödemez.
            if (friendly && _host.IsHealSkill(_host.Motion.TemplateSkill)
                && GuardTriggerDelivery.AllowImmediate(_host.LastMechanicPlan, "can"))
            {
                if (DrainNumbers.TryShare(_host.LastMechanicPlan, hit.Share, out _, out float drainHeal) && drainHeal > 0.5f)
                    _host.ApplyClosingHealAmount(_host.Motion.TemplateSkill, Mathf.RoundToInt(drainHeal), null, 0f);
                else
                {
                    _host.ApplyClosingHeal(
                        _host.Motion.TemplatePending.Closing,
                        _host.Motion.TemplateSkill,
                        hit.Share,
                        _host.Motion.TemplateChain);
                }
            }

            bool selfPulse = hit.Anchor is "self" or "ring";
            if ((reached || selfPulse || arc) && !_host.Motion.TemplateStatusSent)
            {
                // 4-9 kalkanı da tetiğin; StatusApplicator aynı cast'te kalkan basmasın.
                if (GuardTriggerDelivery.AllowImmediate(_host.LastMechanicPlan, "kalkan"))
                    _host.ApplyClosingStatuses(_host.Motion.TemplatePending, _host.Motion.TemplateSkill, bossReached: !friendly && (geometry || arc));
                if (!friendly)
                {
                    _host.ApplyMechanicHitEffects(
                        _host.LastMechanicPlan,
                        new Vector3(hit.OriginX, 0f, hit.OriginZ));
                }
                _host.Motion.SetTemplateStatusSent(true);
            }

            if (!friendly && (geometry || arc))
                _host.TemplateDelivery.NoteTemplateHostileHit(new Vector3(hit.OriginX, 0f, hit.OriginZ));
            }
            finally
            {
                _host.SlotQueryCastId = prevCast;
            }
        }

        public void ApplyDrainDamage(float share)
        {
            if (!DrainNumbers.TryShare(_host.LastMechanicPlan, share, out float damage, out _) || damage <= 0.01f)
                return;
            if (_host.BossStatus != null)
                _host.BossStatus.ApplyDamage(damage);
            else
                _host.BossVitals?.ApplyDamage(damage);
        }

        public bool BossReachedMotionHit(in MotionHit hit)
        {
            if (_host.Boss == null)
                return false;
            Vector3 boss = _host.Boss.transform.position;
            float extra = _host.Motion.BossBodyRadius();
            var origin = new Vector3(hit.OriginX, boss.y, hit.OriginZ);
            bool templateHit;
            if (hit.Anchor is "self" or "ring" or "target" or "behind" or "plant" or "target_side"
                || hit.Shape == "sphere")
            {
                Vector3 flat = boss - origin;
                flat.y = 0f;
                templateHit = flat.magnitude <= hit.RadiusM + extra;
            }
            else
            {
                Vector3 dir = new Vector3(hit.DirX, 0f, hit.DirZ);
                if (dir.sqrMagnitude < 0.0001f)
                    dir = Vector3.forward;
                dir.Normalize();
                Vector3 end = origin + dir * Mathf.Max(hit.LengthM, 0.2f);
                templateHit = DistancePointSegment(boss, origin, end) <= hit.RadiusM + extra;
            }
            if (templateHit)
                return true;
            return JsonEdgeReachesBoss(boss, extra);
        }

        /// <summary>
        /// Göğüs ofseti dikeydir (0,35 m); yatay menzil kenardan kenara JSON boyudur.
        /// Kalıp vuruşu kısa kalsa da fiil hitbox'ı yetiyorsa isabet sayılır.
        /// </summary>
        public bool JsonEdgeReachesBoss(Vector3 boss, float bossRadius)
        {
            if (_host.Player == null || _host.Motion.TemplateSkill.IsEmpty)
                return false;
            float reach = JsonEdgeReachM(_host.Motion.TemplateSkill);
            if (reach <= 0f)
                return false;
            float current = _host.FlatDistance(_host.Player.position, boss);
            return MotionCastReach.CenterInReach(
                MotionCastReach.CloserCenter(current, _host.Motion.TemplateStartCenter),
                _host.PlayerBodyRadiusM(),
                bossRadius,
                reach);
        }

        public float JsonEdgeReachM(in SkillResolution skill)
        {
            if (!_host.TryVerbHitbox(skill, out VerbHitboxSpec spec))
                return 0f;
            int.TryParse(skill.AdjectiveId, out int adjectiveId);
            int weaponId = _host.EquippedWeaponNumber();
            float rangeMult = _host.EquippedWeapon != null ? _host.EquippedWeapon.RangeMult : 1f;
            float weaponScale = _host.VerbData?.WeaponSizeMult(weaponId, rangeMult) ?? rangeMult;
            float table = _host.VerbData?.AdjectiveSizeMult(adjectiveId) ?? 1f;
            float engineScale = skill.Engine.HitboxScaleMult(0f);
            float adjective = HitboxSizing.AdjectiveScale(table, engineScale);
            adjective *= _host.SlotPassives?.HitboxSizeMultFor(_host.Motion.TemplateSlotCastId) ?? 1f;
            return HitboxSizing.Resolve(spec, weaponScale, adjective).ReachM;
        }

        float BossBodyRadius()
        {
            if (_host.Boss == null)
                return 0.6f;
            Collider col = _host.Boss.GetComponentInChildren<Collider>();
            if (col == null)
            {
                DesignWarnings.Once(
                    "motion.boss_radius",
                    "Boss gövdesi okunamadı. Vuruş payı yedek 0.6 m.");
                return 0.6f;
            }
            return Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
        }

        static float DistancePointSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.0001f)
                return Vector3.Distance(point, a);
            float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / len2);
            return Vector3.Distance(point, a + ab * t);
        }

    }
}
