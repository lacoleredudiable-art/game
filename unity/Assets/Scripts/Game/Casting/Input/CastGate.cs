using Dovus.App.Casting;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Game.Data;

namespace Dovus.Game.Casting.Input
{
    public sealed class CastGate
    {
        readonly HexagonInputSession _s;
        readonly CastFeedback _feedback;

        public CastGate(HexagonInputSession session, CastFeedback feedback)
        {
            _s = session;
            _feedback = feedback;
        }

        public bool TryAllowSentenceStart(int verbDot)
        {
            if (_s.Engine == null || _s.Combat == null)
                return true;

            var state = _s.Engine.State;
            float cost = LookupBaseResourceCost(verbDot);
            bool canAfford = _s.Resource == null || _s.Resource.CanAfford(cost);
            double worldMs = _s.Clock != null ? _s.Clock.Director.WorldTimeMs : 0;
            bool canGlobal = _s.Cooldown == null || _s.Cooldown.CanStartGlobalGate(worldMs);

            var block = CastGateRules.SentenceStartBlockReason(
                state.Phase,
                state.Words.Count,
                _s.Combat.Sentence.MaxSentenceDots,
                _s.Combat.EnforceResourceCost,
                _s.Resource != null,
                canAfford,
                _s.Combat.EnforceCooldown,
                _s.Cooldown != null,
                canGlobal);

            if (block == CastGateRules.SentenceStartBlock.InsufficientMana)
            {
                _feedback.NotifyInsufficientMana();
                return false;
            }

            if (block == CastGateRules.SentenceStartBlock.GlobalCooldown)
            {
                _feedback.NotifyOnCooldown();
                return false;
            }

            return true;
        }

        public bool WouldStartSentence()
        {
            if (_s.Engine == null || _s.Combat == null)
                return _s.Engine != null && CastGateRules.WouldStartSentence(_s.Engine.State.Phase, 0, int.MaxValue);
            var state = _s.Engine.State;
            return CastGateRules.WouldStartSentence(
                state.Phase, state.Words.Count, _s.Combat.Sentence.MaxSentenceDots);
        }

        public bool CanGlobalCooldownGate()
        {
            if (_s.Combat == null || !_s.Combat.EnforceCooldown)
                return true;
            if (_s.Cooldown == null)
                return true;

            double worldMs = _s.Clock != null ? _s.Clock.Director.WorldTimeMs : 0;
            return _s.Cooldown.CanStartGlobalGate(worldMs);
        }

        public bool TryAllowComboCooldownForNextDot(int nextDot)
        {
            if (_s.Engine == null || _s.Combat == null || !_s.Combat.EnforceCooldown || _s.Cooldown == null)
                return true;

            EnsureSkills();
            var state = _s.Engine.State;
            bool skillEmpty = true;
            bool comboEmpty = true;
            bool canStart = true;

            if (_s.Skills != null
                && state.Phase == SentencePhase.Building
                && state.Words.Count == 1)
            {
                int verbRuneId = (int)state.Words[0].Rune;
                int adjectiveRuneId = _s.Engine.Loadout.RuneIdAtSlot(nextDot);
                SkillResolution skill = _s.Skills.Resolve(new[] { verbRuneId, adjectiveRuneId });
                skillEmpty = skill.IsEmpty;
                if (!skillEmpty)
                {
                    string comboKey = ComboCooldownKey.For(skill);
                    comboEmpty = string.IsNullOrEmpty(comboKey);
                    if (!comboEmpty && _s.Cooldown != null)
                    {
                        double worldMs = _s.Clock != null ? _s.Clock.Director.WorldTimeMs : 0;
                        canStart = _s.Cooldown.CanStart(comboKey, worldMs);
                    }
                }
            }

            return CastGateRules.TryAllowComboCooldownForNextDot(
                _s.Combat.EnforceCooldown,
                _s.Cooldown != null,
                state.Phase,
                state.Words.Count,
                skillEmpty,
                comboEmpty,
                canStart);
        }

        public bool TryAllowProspectiveTarget(int nextDot)
        {
            if (_s.Engine == null)
                return true;

            EnsureSkills();
            var state = _s.Engine.State;
            bool hasGate = _s.SkillTargetGate != null;
            bool hasSkills = _s.Skills != null;
            bool skillEmpty = true;
            bool gateAllows = true;

            if (hasGate && state.Phase == SentencePhase.Building && state.Words.Count == 1 && hasSkills)
            {
                int verbRuneId = (int)state.Words[0].Rune;
                int adjectiveRuneId = _s.Engine.Loadout.RuneIdAtSlot(nextDot);
                SkillResolution skill = _s.Skills.Resolve(new[] { verbRuneId, adjectiveRuneId });
                skillEmpty = skill.IsEmpty;
                gateAllows = skillEmpty || _s.SkillTargetGate(skill);
            }

            return CastGateRules.TryAllowProspectiveTarget(
                hasGate, state.Phase, state.Words.Count, hasSkills, skillEmpty, gateAllows);
        }

        public float LookupBaseResourceCost(int verbDot)
        {
            SkillResolution skill = LookupVerbSkill(verbDot);
            return skill.IsEmpty ? 0f : skill.Costs.BaseResourceCost;
        }

        public SkillResolution LookupVerbSkill(int verbDot)
        {
            EnsureSkills();
            if (_s.Skills == null)
                return SkillResolution.Empty;

            int runeId = _s.Engine != null
                ? _s.Engine.Loadout.RuneIdAtSlot(verbDot)
                : verbDot;
            return _s.Skills.Resolve(new[] { runeId });
        }

        public void EnsureSkills()
        {
            if (_s.Skills != null)
                return;
            _s.Skills = SkillMotorLoader.Load();
        }
    }
}
