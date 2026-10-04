using Dovus.App.Casting;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using Dovus.Game.Actors;
using Dovus.Game.DevTools;
using System.Collections.Generic;

namespace Dovus.Game.Skills.Launch
{
    public sealed class CastSideEffects
    {
        readonly ICastSideEffectsHost _host;

        public CastSideEffects(ICastSideEffectsHost host) => _host = host;

        public void ApplyResourceCost(SkillResolution skill)
        {
            if (_host.PlayerResourceHost == null || skill.IsEmpty)
                return;
            float cost = SkillMobility.ResourceCost(skill);
            if (cost <= 0f)
                return;
            _host.PlayerResourceHost.Consume(cost, _host.TryTakeFreeMana());
        }

        public void ApplyCastMobility(SkillResolution skill, float durationSec)
        {
            if (_host.PlayerStatus == null || skill.IsEmpty || durationSec <= 0f)
                return;

            string mob;
            if (_host.MobilityCc != null
                && int.TryParse(skill.VerbId, out int verbId)
                && int.TryParse(skill.AdjectiveId, out int adjectiveId))
            {
                int weaponId = 0;
                if (_host.EquippedWeapon != null)
                {
                    string id = _host.EquippedWeapon.Id ?? string.Empty;
                    int colon = id.LastIndexOf(':');
                    int.TryParse(colon >= 0 ? id.Substring(colon + 1) : id, out weaponId);
                }
                mob = _host.MobilityCc.ResolveMobility(
                    verbId, adjectiveId, weaponId, _host.EquippedWeapon?.MobilityMod ?? 0);
            }
            else
            {
                mob = SkillMobility.Resolve(skill);
            }
            double now = _host.Clock != null ? _host.Clock.Director.WorldTimeMs : 0;
            _host.PlayerStatus.GrantCastMobility(mob, now + durationSec * SkillsTimeDefaults.SecToMs);
        }

        public void RefreshBuildingMobility(IReadOnlyList<SentenceWord> words)
        {
            if (words == null || words.Count < 3 || _host.Skills == null)
                return;
            SkillResolution skill = _host.ResolveSkillWords(words);
            if (skill.IsEmpty)
                return;
            ApplyCastMobility(skill, CastSideEffectsDefaults.CastMobilityDurationSec);
        }

        public void ApplyCooldown(SkillResolution skill, IReadOnlyList<SentenceWord> words, bool cosmeticIfDisabled)
        {
            if (skill.IsEmpty || words == null || words.Count == 0)
                return;

            bool enforce = _host.Combat != null && _host.Combat.EnforceCooldown;
            if (!enforce)
            {
                if (cosmeticIfDisabled)
                    PulseCosmeticCooldown(skill, words);
                return;
            }

            string comboKey = ComboCooldownKey.For(skill);
            if (_host.PlayerCooldownHost == null || string.IsNullOrEmpty(comboKey))
                return;

            float sec = skill.BaseCooldownSec * _host.WeaponCooldownMult();
            double worldMs = _host.Clock != null ? _host.Clock.Director.WorldTimeMs : 0;
            if (!_host.PlayerCooldownHost.TryBeginCast(comboKey, sec, worldMs))
                return;

            if (_host.HexagonView == null || sec <= 0f)
                return;

            _host.HexagonView.BeginTrackedCooldown(
                words[0].Dot,
                comboKey,
                sec,
                _host.PlayerCooldownHost,
                _host.Clock);
        }

        public void PulseCosmeticCooldown(SkillResolution skill, IReadOnlyList<SentenceWord> words)
        {
            if (_host.HexagonView == null || skill.IsEmpty || words == null || words.Count == 0)
                return;
            float sec = skill.BaseCooldownSec * _host.WeaponCooldownMult();
            if (sec <= 0f)
                return;
            _host.HexagonView.BeginCosmeticCooldown(words[0].Dot, sec);
        }

        public SkillMotionPlan ResolveSkillMotion(SkillResolution skill)
        {
            if (skill.IsEmpty || _host.Combat == null || _host.Player == null)
                return SkillMotionPlan.None;

            var t = _host.Combat.SkillMotion;
            t.ArenaHalfSizeM = _host.Colors != null ? _host.Colors.Arena.ArenaHalfSizeM : t.ArenaHalfSizeM;

            UnityEngine.Vector3 face = _host.Player.forward;
            face.y = 0f;
            if (face.sqrMagnitude < 0.0001f)
                face = UnityEngine.Vector3.forward;

            UnityEngine.Vector3 bossPos = _host.BossTransform != null
                ? _host.BossTransform.position
                : UnityEngine.Vector3.zero;
            bool bossAlive = _host.BossVitals == null || !_host.BossVitals.IsDown;

            var ctx = new SkillMotionContext(
                _host.Player.position.x, _host.Player.position.z,
                face.x, face.z,
                bossPos.x, bossPos.z,
                bossAlive,
                t.ArenaHalfSizeM);

            return SkillMotionMotor.Resolve(
                skill, ctx, t,
                _host.VerbData?.IFrameMsFor(skill.SkillId) ?? 0);
        }

        public void ApplySkillMotionIframe(in SkillResolution skill, in SkillMotionPlan plan)
        {
            if (plan.IsEmpty || plan.IframeMs <= 0 || _host.Player == null)
                return;
            PlayerDodgeController rig = _host.Player.GetComponent<PlayerDodgeController>();
            if (rig == null)
                return;
            rig.OpenSkillIframe(plan.IframeMs);
            DebugConfig.DevLog($"[Mechanic] i-frame {skill.SkillId} {plan.Kind} {plan.IframeMs} ms");
        }

        public void AnnotateMotion(SkillResolution skill, in SkillMotionPlan plan)
        {
            string tag = plan.Kind switch
            {
                SkillMotionKind.ZenitsuPass => "Zenitsu geçiş",
                SkillMotionKind.ShortBlink => "ışınlanma",
                SkillMotionKind.ForwardDash => "dash",
                SkillMotionKind.PlaceMark => "işaret",
                _ => null
            };
            if (tag == null) return;
            _host.DebugHud?.NoteSkillBang(skill.DisplayName, tag);
        }
    }
}
