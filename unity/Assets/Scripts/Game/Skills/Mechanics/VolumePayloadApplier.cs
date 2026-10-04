using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.DevTools;
using System;
using System.Linq;
using UnityEngine;

namespace Dovus.Game.Skills.Mechanics
{
    public sealed class VolumePayloadApplier
    {
        readonly IJsonEffectHost _host;
        readonly JsonEffectRuntime _json;

        public VolumePayloadApplier(IJsonEffectHost host, JsonEffectRuntime json)
        {
            _host = host;
            _json = json;
        }

        double JsonParam(string key, double fallback) => _host.JsonParam(key, fallback);
        double JsonNow => _host.JsonNow;

        public void TickVolumePayload(MechanicVolume v, bool bossInside, bool playerInside, bool allyInside, double refreshMs)
        {
            if (v.Plan == null || _host.Clock == null)
                return;
            double now = _host.Clock.Director.WorldTimeMs;
            double flow = JsonParam("flow_tick_fraction", 0.33);
            double trapMult = JsonParam("trap_trigger_mult", 0.5);
            float growth = JsonEffectRules.RampedRatio(1f, v.StartMs, v.UntilMs, now, JsonParam("ramp_max", 1.5));
            string src = "payload:" + v.Plan.SkillId;
            bool trapFires = v.Profile.Trap && bossInside && now >= v.ArmAtMs
                && (!v.Triggered || JsonEffectRules.TrapRepeats(v.Plan.Body));
            foreach (MechanicEffect e in v.Plan.Effects)
            {
                VolumePayloadKind kind = JsonEffectRules.PayloadKind(e);
                if (kind == VolumePayloadKind.None)
                    continue;
                if (kind == VolumePayloadKind.Trap && !trapFires)
                    continue;
                float scale = JsonEffectRules.PayloadScale(kind, flow, trapMult, growth);
                if (kind == VolumePayloadKind.Trap && v.Triggered)
                    scale *= (float)flow; // cit: sonraki tikler
                if (e.Target == "dusman")
                {
                    if (bossInside)
                        ApplyHostilePayload(v, e, kind, scale, refreshMs, src);
                }
                else
                    ApplyFriendlyPayload(v, e, kind, scale, growth, playerInside, allyInside, refreshMs, src);
            }
            if (trapFires && !v.Triggered)
            {
                v.Triggered = true;
                _host.Readout?.NoteSkill(v.Skill.DisplayName, "tuzak tetiklendi", new Color(1f, 0.6f, 0.3f));
                JsonEffectRuntime.JsonLog("tuzak tetiklendi " + v.Plan.SkillId);
                if (!JsonEffectRules.TrapRepeats(v.Plan.Body))
                    v.UntilMs = Math.Min(v.UntilMs, now + 250.0);
            }
        }

        void ApplyHostilePayload(MechanicVolume v, MechanicEffect e, VolumePayloadKind kind, float scale, double refreshMs, string src)
        {
            double now = JsonNow;
            bool trap = kind == VolumePayloadKind.Trap;
            switch (e.Stat)
            {
                case "can" when e.Amount < 0:
                    float dealt = _json.ApplyJsonTickDamage(v.Closing, v.Skill, scale);
                    if (dealt > 0f)
                        JsonEffectRuntime.JsonLog($"{kind} tik {dealt:0.#}");
                    break;
                case "it":
                    _json.PushBossFromPlayer((float)JsonParam("it_push_m", 2.0), now);
                    break;
                case "zirh":
                    _host.BossStatus?.Armor.ApplyShred(
                        (float)Math.Abs(e.Amount), now, now + (trap ? Math.Max(1.0, e.DurationSec) * 1000.0 : refreshMs));
                    break;
                case "hareket":
                    double ms = trap ? Math.Max(0.5, e.DurationSec) * 1000.0 : refreshMs;
                    if (e.Amount <= 0)
                        _host.BossStatus?.Board.Apply(StatusKind.Root, ms, 1f, src);
                    else if (e.Amount < 1)
                        _host.BossStatus?.Board.Apply(StatusKind.Slow, ms, (float)e.Amount, src);
                    break;
            }
        }

        void ApplyFriendlyPayload(
            MechanicVolume v, MechanicEffect e, VolumePayloadKind kind, float scale, float growth,
            bool playerInside, bool allyInside, double refreshMs, string src)
        {
            float level = kind == VolumePayloadKind.Growing ? growth : 1f;
            switch (e.Stat)
            {
                case "can" when e.Amount > 0:
                    if (!v.Skill.IsEmpty && (playerInside || allyInside))
                        _host.ApplyClosingHeal(v.Closing, v.Skill, scale, 1f, v.Center, v.RadiusM);
                    break;
                case "kalkan":
                    float cap = _host.ShieldAbsorbFor(v.Skill) * level * _host.WeaponFriendlyScale();
                    float step = cap * (float)JsonParam("flow_tick_fraction", 0.33);
                    if (playerInside && _host.PlayerStatus != null)
                        TopUpShield(_host.PlayerStatus.Board, step, cap, refreshMs, src);
                    if (allyInside && _host.Ally != null)
                    {
                        _host.Ally.EnsureStatusBoard();
                        TopUpShield(_host.Ally.Board, step, cap, refreshMs, src);
                    }
                    break;
                case "hasar_buff":
                    if (!playerInside)
                        break;
                    float buff = (float)Math.Abs(e.Amount) * level * _host.WeaponFriendlyScale();
                    double now = JsonNow;
                    if (now >= _host.SelfDamageBuffUntilMs || _host.SelfDamageBuff < buff)
                        _host.SelfDamageBuff = buff;
                    _host.SelfDamageBuffUntilMs = Math.Max(_host.SelfDamageBuffUntilMs, now + refreshMs);
                    break;
            }
        }

        static void TopUpShield(StatusBoard board, float step, float cap, double refreshMs, string src)
        {
            if (board == null || cap <= 0f)
                return;
            float next = Mathf.Min(cap, board.ShieldRemaining + step);
            if (next > board.ShieldRemaining)
                board.Apply(StatusKind.Shield, Math.Max(refreshMs, 1000.0), next, src);
        }

        public void TickLinkFlow(MechanicLink link, double worldMs)
        {
            if (link.Plan == null || link.Skill.IsEmpty || !JsonEffectRules.LinkFlowsDamage(link.Plan))
                return;
            if (_host.Boss == null || link.Target != _host.Boss.transform || worldMs < link.NextFlowMs)
                return;
            link.NextFlowMs = worldMs + Math.Max(50.0, link.FlowTickMs);
            float dealt = _json.ApplyJsonTickDamage(link.Closing, link.Skill, (float)JsonParam("flow_tick_fraction", 0.33));
            if (dealt > 0f)
                JsonEffectRuntime.JsonLog($"baÄŸ akÄ±ÅŸÄ± {dealt:0.#}");
        }

    }
}
