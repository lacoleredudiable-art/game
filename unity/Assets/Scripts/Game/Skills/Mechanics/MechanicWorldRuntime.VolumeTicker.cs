using System;
using Dovus.Core;
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
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dovus.Game.Skills.Mechanics
{
    public sealed partial class MechanicWorldRuntime
    {
        void TickMechanicVolume(MechanicVolume volume)
        {
            bool bossInside = _host.Boss != null && _host.FlatDistance(_host.Boss.transform.position, volume.Center) <= volume.RadiusM;
            bool playerInside = _host.Player != null && _host.FlatDistance(_host.Player.position, volume.Center) <= volume.RadiusM;
            bool allyInside = _host.Ally != null && _host.FlatDistance(_host.Ally.transform.position, volume.Center) <= volume.RadiusM;
            double refreshMs = volume.TickMs * MechanicVolumeTickerDefaults.RefreshTickMult;
            string volumeId = volume.Plan != null && !string.IsNullOrEmpty(volume.Plan.SkillId)
                ? volume.Plan.SkillId
                : "volume";

            MechanicEffect flickerTempo = volume.Plan.Effects.FirstOrDefault(
                e => e.Stat == "tempo" && e.Has("titrer") && e.Target == "dusman");
            if (flickerTempo != null && bossInside)
            {
                double flickerSec = _host.MechanicEngine?.Rules.Param("flicker_sec") ?? 0;
                bool on = flickerSec <= 0
                    || ((long)(volume.NextTickMs / (flickerSec * SkillsTimeDefaults.SecToMs)) & 1) == 0;
                if (on && flickerTempo.Amount < 1)
                    _host.BossStatus?.Board.Apply(StatusKind.Slow, refreshMs, (float)flickerTempo.Amount, "flicker:" + volumeId);
                else
                    _host.BossStatus?.Board.RemoveKinds(SlowOnly);
            }

            if (volume.Profile.TempoField)
            {
                foreach (MechanicEffect e in volume.Plan.Effects.Where(e => e.Stat == "tempo" && e.Has("zaman_alani")))
                {
                    if (e.Target == "dusman" && bossInside && e.Amount < 1)
                        _host.BossStatus?.Board.Apply(StatusKind.Slow, refreshMs, (float)e.Amount, "field:" + volumeId);
                    if ((e.Target == "dost" || e.Target == "kendin") && e.Amount > 1)
                    {
                        float haste = WeaponPassiveRules.ScaleFriendlyMagnitude(
                            (float)e.Amount, _host.WeaponFriendlyScale());
                        if (playerInside)
                            _host.PlayerStatus?.Board.Apply(StatusKind.Haste, refreshMs, haste, "field:" + volumeId);
                        if (allyInside)
                        {
                            _host.Ally.EnsureStatusBoard();
                            _host.Ally.Board.Apply(StatusKind.Haste, refreshMs, haste, "field:" + volumeId);
                        }
                    }
                }
            }

            if (volume.Profile.Cloud)
            {
                if (playerInside)
                    _host.PlayerStatus?.Board.Apply(StatusKind.Stealth, refreshMs, 1f);
                if (allyInside)
                {
                    _host.Ally.EnsureStatusBoard();
                    _host.Ally.Board.Apply(StatusKind.Stealth, refreshMs, 1f);
                }
                if (bossInside)
                {
                    MechanicEffect blind = volume.Plan.Effects.FirstOrDefault(e => e.Stat == "kor");
                    _host.BossStatus?.Board.Apply(StatusKind.Blind, refreshMs, (float)Math.Max(0, blind?.Amount ?? 0));
                }
            }

            if (EmiciPull.VortexActs(volume.Profile.Vortex, bossInside)
                && _host.Boss != null && _host.Player != null
                && ForcedDisplacement.Allows(_host.BossStatus != null ? _host.BossStatus.Board : null))
                PullBossToPlayerContact();
            if (volume.Profile.Continuous && bossInside)
            {
                if (volume.Plan.Effects.Any(e => e.Has("akinti")))
                    _host.BossStatus?.ApplyKnockbackFrom(_host.Player != null ? _host.Player.position : volume.Center);
                MechanicEffect mire = volume.Plan.Effects.FirstOrDefault(e => e.Has("bataklik"));
                if (mire != null)
                {
                    if (mire.Amount <= 0)
                        _host.BossStatus?.Board.Apply(
                            StatusKind.Root, refreshMs, 1f, "mire:" + (volume.Plan != null ? volume.Plan.SkillId : "volume"));
                    else if (mire.Amount < 1)
                        _host.BossStatus?.Board.Apply(StatusKind.Slow, refreshMs, (float)mire.Amount, "mire:" + volumeId);
                }
            }
            if (volume.Profile.CleanseField)
            {
                int cleanseCount = _host.JsonCleanseCount(volume.Skill);
                if (playerInside)
                    _host.PlayerStatus?.Board.CleanseHostile(cleanseCount);
                if (allyInside)
                {
                    _host.Ally.EnsureStatusBoard();
                    _host.Ally.Board.CleanseHostile(cleanseCount);
                }
            }
            if (volume.Profile.Payload)
                _payload.TickVolumePayload(volume, bossInside, playerInside, allyInside, refreshMs);
        }

        void TickMechanicLinks(double worldMs)
        {
            for (int i = Links.Count - 1; i >= 0; i--)
            {
                MechanicLink link = Links[i];
                if (worldMs >= link.UntilMs || link.Line == null || _host.Player == null || link.Target == null)
                {
                    if (link.Line != null)
                        _host.DestroyUnityObject(link.Line.gameObject);
                    Links.RemoveAt(i);
                    continue;
                }
                link.Line.SetPosition(0, _host.Player.position + Vector3.up);
                link.Line.SetPosition(1, link.Target.position + Vector3.up);
                if (_host.Boss != null && link.Target == _host.Boss.transform)
                {
                    float distance = _host.FlatDistance(_host.Player.position, _host.Boss.Home);
                    float maxLength = Mathf.Max(0f, (float)link.Plan.Body.ReachM);
                    if (maxLength > 0f && distance > maxLength
                        && ForcedDisplacement.Allows(_host.BossStatus != null ? _host.BossStatus.Board : null))
                        _host.Boss.MoveHomeToward(_host.Player.position, distance - maxLength);
                }
                _payload.TickLinkFlow(link, worldMs);
                string linkId = link.Plan != null && !string.IsNullOrEmpty(link.Plan.SkillId)
                    ? link.Plan.SkillId
                    : "link";
                foreach (MechanicEffect e in link.Plan.Effects)
                {
                    if (e.Stat == "durum_sil" && e.Has("bag_bagisiklik"))
                    {
                        _host.PlayerStatus?.Board.CleanseHostile();
                        if (_host.Ally != null)
                        {
                            _host.Ally.EnsureStatusBoard();
                            _host.Ally.Board.CleanseHostile();
                        }
                        continue;
                    }
                    if (e.Target != "dusman" || e.Atom != "hiz")
                        continue;
                    if (e.Stat == "tempo" && e.Has("senkron"))
                    {
                        SkillResolution linked = _host.SkillFromPlan(link.Plan);
                        if (CardEffectRules.WantsSelfHaste(linked.SkillJob))
                            continue;
                        TempoSyncRules.Read(e.DurationSec, (float)e.Amount, out double syncMs, out float syncStrength);
                        if (_host.PlayerStatus != null && _host.PlayerStatus.EffectiveBlocksMovement)
                        {
                            _host.BossStatus?.Board.Apply(
                                StatusKind.Root, syncMs, 1f,
                                "link-tempo:" + (link.Plan != null ? link.Plan.SkillId : "link"));
                        }
                        else if (_host.PlayerStatus != null && _host.PlayerStatus.EffectiveMoveSpeedMult < 1f)
                            _host.BossStatus?.Board.Apply(StatusKind.Slow, syncMs, syncStrength, "link:" + linkId);
                        continue;
                    }
                    SkillResolution linkedLock = _host.SkillFromPlan(link.Plan);
                    if (CardEffectRules.WantsSelfHaste(linkedLock.SkillJob)
                        && !CardEffectRules.Names(linkedLock.SkillJob, "root"))
                        continue;
                    double refresh = Math.Max(100, e.DurationSec * SkillsTimeDefaults.SecToMs);
                    if (e.Amount <= 0)
                        _host.BossStatus?.Board.Apply(
                            StatusKind.Root, refresh, 1f,
                            "link:" + (link.Plan != null ? link.Plan.SkillId : "link"));
                    else if (e.Amount < 1)
                        _host.BossStatus?.Board.Apply(StatusKind.Slow, refresh, (float)e.Amount, "link:" + linkId);
                }
            }
        }

        void TickGuardTriggers(double worldMs)
        {
            double threshold = _host.MechanicEngine?.Rules.Param("guard_threshold") ?? 0;
            PlayerVitalsHost playerVitals = _host.CachedPlayerVitals();
            for (int i = _guardTriggers.Count - 1; i >= 0; i--)
            {
                GuardTrigger guard = _guardTriggers[i];
                bool expired = worldMs >= guard.UntilMs;
                bool allyLow = _host.Ally != null && _host.Ally.Ratio <= threshold;
                bool playerLow = playerVitals != null && playerVitals.MaxHp > 0
                    && (float)playerVitals.Hp / playerVitals.MaxHp <= threshold;
                if (!expired && !allyLow && !playerLow)
                    continue;

                if (!expired && _guardOnce.TryApply(guard.Id))
                {
                    float scale = guard.NeedsHoly ? _host.WeaponFriendlyScale() : 1f;
                    int amount = Mathf.Max(0, Mathf.RoundToInt((float)Math.Abs(guard.Effect.Amount) * scale));
                    if (guard.Effect.Stat == "can")
                    {
                        // Miktar gramerden (ham). Diğer şifalar gibi ActorStatusHost.ApplyHeal ölçekler.
                        if (allyLow && _host.Ally != null)
                        {
                            float healMult = _host.Ally.Board != null ? _host.Ally.Board.HealEffectivenessMult : 1f;
                            float scaled = DamagePipeline.Resolve(new DamageQuery
                            {
                                Heal = true,
                                HealPower = amount,
                                HealMultiplier = healMult > 0f ? healMult : 1f,
                                ScaleMagnitudes = true
                            }).Amount;
                            _host.Ally.ApplyHeal(Mathf.CeilToInt(scaled));
                        }
                        else if (_host.PlayerStatus != null)
                            _host.PlayerStatus.ApplyHeal(amount);
                        else
                            playerVitals?.ApplyHeal(Mathf.CeilToInt(CombatScale.Magnitude(amount)));
                    }
                    else if (guard.Effect.Stat == "kalkan")
                    {
                        StatusBoard board = allyLow ? _host.Ally?.Board : _host.PlayerStatus?.Board;
                        board?.Apply(StatusKind.Shield, Math.Max(100, guard.Effect.DurationSec * SkillsTimeDefaults.SecToMs), amount);
                    }
                    else if (guard.Effect.Stat == "hasar_buff" && _host.Clock != null)
                    {
                        float buff = WeaponPassiveRules.ScaleFriendlyMagnitude(
                            (float)Math.Abs(guard.Effect.Amount), scale);
                        _host.SelfDamageBuff = Mathf.Max(_host.SelfDamageBuff, buff);
                        _host.SelfDamageBuffUntilMs = Math.Max(
                            _host.SelfDamageBuffUntilMs,
                            _host.Clock.Director.WorldTimeMs + Math.Max(100, guard.Effect.DurationSec * SkillsTimeDefaults.SecToMs));
                    }
                }
                if (guard.View != null)
                    _host.DestroyUnityObject(guard.View);
                _guardTriggers.RemoveAt(i);
            }
        }

    }
}
