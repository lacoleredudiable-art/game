using Dovus.App.Casting;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Game.Actors;
using Dovus.Game.Data;
using Dovus.Core.Mechanic;
using UnityEngine;

namespace Dovus.Game.Skills.Closing
{
    public sealed class ClosingHealResolver
    {
        readonly IClosingHealHost _host;

        public ClosingHealResolver(IClosingHealHost host) => _host = host;

        public void Apply(
            ClosingHit closing,
            SkillResolution skill,
            float effectScale = 1f,
            float? chainBonusOverride = null,
            Vector3? fieldCenter = null,
            float fieldRadiusM = 0f)
        {
            int amount = CalculateAmount(closing, skill, effectScale, chainBonusOverride);
            ApplyAmount(skill, amount, fieldCenter, fieldRadiusM);
        }

        public int CalculateAmount(
            ClosingHit closing,
            SkillResolution skill,
            float effectScale,
            float? chainBonusOverride)
        {
            if (skill.IsEmpty || !ClosingHealRules.IsHealSkill(skill) || effectScale <= 0f)
                return 0;
            float per = _host.Combat != null ? _host.Combat.ClosingDamagePerEffect : 1f;
            float chain = chainBonusOverride ?? _host.ClosingChainBonus;
            float weapon = _host.WeaponSupportPower(skill);
            float healBase = skill.BaseHeal > 0f
                ? skill.BaseHeal
                : closing.TotalEffect * per;
            return Mathf.Max(0, Mathf.RoundToInt(healBase * chain * weapon * effectScale));
        }

        public void ApplyAmount(
            SkillResolution skill,
            int amount,
            Vector3? fieldCenter,
            float fieldRadiusM,
            Transform preferredTarget = null)
        {
            if (amount <= 0)
                return;
            DamageOutcome healedBy = DamagePipeline.Resolve(new DamageQuery
            {
                Heal = true,
                HealPower = amount,
                HealMultiplier = _host.HealBuffMultiplier(skill),
                ScaleMagnitudes = true
            });
            amount = Mathf.Max(0, Mathf.RoundToInt(healedBy.Amount));
            if (_host.PlayerStatus != null)
                _host.PlayerStatus.LastThreat = healedBy.Threat;
            if (amount <= 0)
                return;

            var playerVitals = _host.CachedPlayerVitals();
            bool spatial = fieldCenter.HasValue && fieldRadiusM > 0f;
            bool preferAlly = _host.Ally != null && preferredTarget == _host.Ally.transform;
            bool allyInRange = preferAlly || !spatial || (_host.Ally != null
                && PlanarMath.FlatDistance(
                    _host.Ally.transform.position.x,
                    _host.Ally.transform.position.z,
                    fieldCenter.Value.x,
                    fieldCenter.Value.z) <= fieldRadiusM);
            bool selfInRange = !spatial || (_host.Player != null
                && PlanarMath.FlatDistance(
                    _host.Player.position.x,
                    _host.Player.position.z,
                    fieldCenter.Value.x,
                    fieldCenter.Value.z) <= fieldRadiusM);
            bool allyNeeds = _host.Ally != null && allyInRange && _host.Ally.Hp < _host.Ally.MaxHp;
            bool selfNeeds = playerVitals != null && selfInRange
                && !playerVitals.IsDown && playerVitals.Hp < playerVitals.MaxHp;
            bool preferSelf = _host.Player != null && preferredTarget == _host.Player;
            bool selfDown = playerVitals != null && playerVitals.IsDown;
            if ((preferAlly && !allyNeeds) || (preferSelf && !selfNeeds))
            {
                _host.Readout?.NoteSkill(skill.DisplayName, preferSelf && selfDown ? "dÃ¼ÅŸtÃ¼" : "zaten full", new Color(0.7f, 0.9f, 0.75f));
                return;
            }
            if (!allyNeeds && !selfNeeds)
            {
                _host.Readout?.NoteSkill(skill.DisplayName, selfDown ? "dÃ¼ÅŸtÃ¼" : "zaten full", new Color(0.7f, 0.9f, 0.75f));
                _host.ApplyHealOverflow(skill, amount, 0, false);
                return;
            }

            JsonEffectRules.SelectHealTargets(
                allyNeeds,
                selfNeeds,
                allyNeeds ? _host.Ally.Ratio : 1f,
                selfNeeds ? (float)playerVitals.Hp / playerVitals.MaxHp : 1f,
                preferAlly,
                preferSelf,
                _host.FriendlyTargetCap(skill),
                out bool healAlly,
                out bool healSelf);

            if (healAlly)
            {
                int healedAlly = _host.Ally.ApplyHeal(amount);
                _host.LastFriendlyWasAlly = true;
                if (healedAlly > 0)
                {
                    _host.DamageHud?.ShowDamage(-healedAlly);
                    _host.Readout?.NoteSkill(skill.DisplayName, "ally +" + healedAlly, new Color(0.4f, 1f, 0.65f));
                    _host.DebugHud?.NoteSkillBang(skill.DisplayName, "ally +" + healedAlly);
                    _host.Ally.EnsureStatusBoard();
                    _host.ConsumeWeaponBonus(_host.Ally.Board);
                }
                _host.ApplyHealOverflow(skill, amount, healedAlly, true);
            }
            if (!healSelf)
                return;

            int healed = playerVitals.ApplyHeal(amount);
            _host.LastFriendlyWasAlly = false;
            if (healed > 0)
            {
                _host.ConsumeWeaponBonus(_host.PlayerStatus != null ? _host.PlayerStatus.Board : null);
                _host.DamageHud?.ShowDamage(-healed);
                _host.Readout?.NoteSkill(skill.DisplayName, "self +" + healed, new Color(0.4f, 1f, 0.65f));
                _host.DebugHud?.NoteSkillBang(skill.DisplayName, "self +" + healed);
            }
            _host.ApplyHealOverflow(skill, amount, healed, false);
        }
    }
}
