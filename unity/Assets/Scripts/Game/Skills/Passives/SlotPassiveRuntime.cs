using Dovus.Core.Combat;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Passives
{
    public sealed class SlotPassiveRuntime
    {
        readonly ISlotPassiveRuntimeHost _host;
        readonly List<PassiveEchoShot> _passiveEchoes = new();
        readonly PassiveFlowRunner _passiveFlows = new();

        public PassiveFlowRunner PassiveFlows => _passiveFlows;

        public SlotPassiveRuntime(ISlotPassiveRuntimeHost host) => _host = host;

        public void Tick(double worldMs)
        {
            _host.SlotPassives?.Tick(worldMs);
            TickPassiveEchoes(worldMs);
            TickPassiveFlows(worldMs);
            if (_host.SlotPassives != null && _host.SlotPassives.ActiveCount > 0)
                _host.PassiveHud?.Sync(_host.SlotPassives.Active, worldMs);
            else
                _host.PassiveHud?.Refresh();
        }

        public void TryTriggerPassive(IReadOnlyList<SentenceWord> words, double worldMs)
        {
            if (words == null || words.Count == 0)
                return;

            if (words.Count >= 2 && _host.SlotPassives != null && _host.Engine?.Loadout != null)
            {
                int adjectiveRuneId = (int)words[1].Rune;
                if (_host.Engine.Loadout.IsPassive(adjectiveRuneId)
                    && _host.Skills.TryGetRune(adjectiveRuneId, out RuneDefinition rune)
                    && _host.Skills.TryGetAdjective(adjectiveRuneId.ToString(), out AdjectiveNode adjective)
                    && _host.SlotPassives.Activate(
                        adjectiveRuneId,
                        rune.AdjectiveFace,
                        rune.PassiveDurationDefault,
                        adjective.Engine,
                        worldMs))
                {
                    _host.Readout?.NoteSkill(
                        rune.AdjectiveFace + " pasif",
                        rune.PassiveDurationDefault.ToString("0.#") + " sn",
                        Color.cyan);
                    _host.PassiveHud?.Sync(_host.SlotPassives.Active, worldMs);
                }
            }
        }

        public void SchedulePassiveEcho(
            PendingClosing ctx,
            SkillResolution skill,
            float slash,
            float chain,
            float echoDelay,
            float echoPower,
            int slotCastId,
            double worldMs)
        {
            _passiveEchoes.Add(new PassiveEchoShot
            {
                DueMs = worldMs + echoDelay * 1000.0,
                Power = echoPower,
                SlotCastId = slotCastId,
                Closing = ctx.Closing,
                Skill = skill,
                Slash = slash,
                Chain = chain,
            });
        }

        void TickPassiveEchoes(double worldMs)
        {
            for (int i = _passiveEchoes.Count - 1; i >= 0; i--)
            {
                PassiveEchoShot echo = _passiveEchoes[i];
                if (worldMs < echo.DueMs)
                    continue;
                _passiveEchoes.RemoveAt(i);
                int prev = _host.SlotQueryCastId;
                _host.SlotQueryCastId = echo.SlotCastId;
                try
                {
                    if (_host.IsHealSkill(echo.Skill))
                        _host.ApplyClosingHeal(echo.Closing, echo.Skill, echo.Power, echo.Chain);
                    else
                        _host.ApplyClosingDamage(
                            echo.Closing,
                            echo.Skill,
                            isBasicStrike: false,
                            echo.Slash,
                            echo.Power,
                            echo.Chain);
                }
                finally
                {
                    _host.SlotQueryCastId = prev;
                }
            }
        }

        void TickPassiveFlows(double worldMs)
        {
            if (_host.BossVitals == null || _host.BossVitals.IsDown)
                return;
            float damage = _passiveFlows.Collect(worldMs);
            if (damage <= 0f)
                return;
            _host.BossVitals.ApplyDamage(damage);
            _host.DamageHud?.ShowDamage(
                damage,
                false,
                _host.BossHitPoint(),
                _host.DamageTint(),
                victimIsBoss: true);
        }

        struct PassiveEchoShot
        {
            public double DueMs;
            public float Power;
            public int SlotCastId;
            public ClosingHit Closing;
            public SkillResolution Skill;
            public float Slash;
            public float Chain;
        }
    }
}
