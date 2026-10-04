using Dovus.Core;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Boss;
using Dovus.Game.DevTools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dovus.Game.Skills.Mechanics
{
    public sealed class JsonEffectRuntime
    {
        readonly IJsonEffectHost _host;
        readonly MechanicWorldRuntime _world;
        readonly ParryWindow _parry = new ParryWindow();
        float _reflectRampBase;
        double _reflectRampStartMs;
        double _reflectRampUntilMs;
        double _reflectSplitUntilMs;

        public JsonEffectRuntime(IJsonEffectHost host, MechanicWorldRuntime world)
        {
            _host = host;
            _world = world;
        }

        MechanicRules JsonRules => _host.JsonRules;
        double JsonNow => _host.JsonNow;
        double JsonParam(string key, double fallback) => _host.JsonParam(key, fallback);
        bool BossDisplaceable => _host.BossDisplaceable;
        StatusTuning JsonStatusTuning => _host.JsonStatusTuning;
        public static void JsonLog(string message) => DebugConfig.DevLog("[Mechanic] json " + message);

        public void NoteJsonCast(in SkillResolution skill, ClosingHit closing)
        {
            _host.JsonCastSkill = skill;
            _host.JsonCastClosing = closing;
        }

        public int FriendlyTargetCap(in SkillResolution skill) =>
            JsonEffectRules.FriendlyCap(!skill.IsEmpty && !skill.Engine.IsNull
                ? skill.Engine.MaxTargets(0)
                : 0);

        public int JsonCleanseCount(in SkillResolution skill) =>
            JsonEffectRules.CleanseCount(
                !skill.IsEmpty && !skill.Engine.IsNull ? skill.Engine.CleanseCount(0) : 0,
                _host.MechanicPlanFor(skill));

        public float ShieldAbsorbFor(in SkillResolution skill)
        {
            float absorb = !skill.IsEmpty && !skill.Engine.IsNull
                ? skill.Engine.ShieldAbsorb(0f)
                : 0f;
            return absorb > 0f ? absorb : JsonStatusTuning.ShieldAbsorb;
        }

        /// <summary>
        /// Dünya/bağ tiki hasarı: normal hasar hattı, ama silahın vuruş-üstü itmesi (Top top patlaması
        /// ve geri tepmesi) tetiklenmez — tik bir hareket sistemi değildir.
        /// </summary>
        public float ApplyJsonTickDamage(ClosingHit closing, in SkillResolution skill, float scale)
        {
            if (skill.IsEmpty || scale <= 0f)
                return 0f;
            _host.JsonTickDamage = true;
            try
            {
                return _host.ApplyClosingDamage(closing, skill, false, 0f, scale, 1f);
            }
            finally
            {
                _host.JsonTickDamage = false;
            }
        }

        public void PushBossFromPlayer(float meters, double now)
        {
            if (meters <= 0f || _host.Player == null || !BossDisplaceable)
                return;
            float shake = _host.Combat != null ? _host.Combat.Manifestation.BossShakeSec * JsonEffectRuntimeDefaults.TravelHitShakeMult : JsonEffectRuntimeDefaults.TravelHitShakeMultAlt;
            _host.Boss.React(_host.Player.position, meters, JsonEffectRuntimeDefaults.BossReactLiftM, shake, now);
            JsonLog($"itme {meters:0.##}m");
        }

        // ---- per frame (called from TickMechanics) ----
        public void Tick(double worldMs)
        {
            if (_host.PlayerStatus != null && worldMs < _reflectRampUntilMs)
            {
                float ratio = JsonEffectRules.RampedRatio(
                    _reflectRampBase, _reflectRampStartMs, _reflectRampUntilMs, worldMs, JsonParam("ramp_max", JsonEffectRuntimeDefaults.ReflectRampMaxFallback));
                _host.PlayerStatus.GrantReflect(ratio, _reflectRampUntilMs);
            }
            if (_host.Ally == null)
                return;
            Vector3 a = _host.Ally.transform.position;
            foreach (MechanicVolume v in _world.Volumes)
            {
                if (!v.Profile.Reflector || !JsonEffectRules.ReflectorFollowsAlly(v.Plan))
                    continue;
                v.Center = new Vector3(a.x, v.Center.y, a.z);
                if (v.View != null)
                    v.View.transform.position = new Vector3(a.x, v.View.transform.position.y, a.z);
            }
        }

        // ---- cast time (end of ApplySelfCastEffects) ----
        public void ApplyJsonSelfCast(MechanicPlan plan, float reflectRatio, float windowSec, double now)
        {
            if (plan == null)
                return;
            double untilMs = now + Math.Max(JsonEffectRuntimeDefaults.ReflectWindowMinSec, windowSec) * SkillsTimeDefaults.SecToMs;
            if (JsonEffectRules.IsParry(plan))
            {
                _parry.Arm(untilMs, JsonEffectRules.ParryRatio(plan));
                JsonLog($"savuşturma penceresi {(untilMs - now) / 1000.0:0.##}sn");
            }
            if (JsonEffectRules.IsSplitReflect(plan))
                _reflectSplitUntilMs = untilMs;
            if (JsonEffectRules.IsRampReflect(plan) && reflectRatio > 0f && _host.HasSelfReflect(plan))
            {
                _reflectRampBase = reflectRatio;
                _reflectRampStartMs = now;
                _reflectRampUntilMs = untilMs;
            }
            double hidden = JsonEffectRules.HiddenSec(plan, windowSec);
            if (hidden > 0 && _host.PlayerStatus != null)
            {
                _host.PlayerStatus.Board.Apply(StatusKind.Stealth, hidden * SkillsTimeDefaults.SecToMs, 1f, "gizli:" + plan.SkillId);
                JsonLog($"gizli {hidden:0.##}sn");
            }
            if (JsonEffectRules.Overflows(plan, "kalkan"))
                _host.TasarShieldUntilMs = now + JsonStatusTuning.ShieldMs;
            ApplyMirroredDebuff(plan);
        }

        /// <summary>8-1 taşma: güç buff'ı zaten açıkken yenisi gelirse eskisi bir sonraki vuruşa tek seferlik ek olur.</summary>
        public void CaptureBuffOverflow(MechanicPlan plan, double now)
        {
            if (!JsonEffectRules.Overflows(plan, "hasar_buff") || now >= _host.SelfDamageBuffUntilMs || _host.SelfDamageBuff <= 0f)
                return;
            _host.OverflowNextHitBonus = _host.SelfDamageBuff;
            JsonLog($"taşma → sonraki vuruş +{_host.OverflowNextHitBonus * 100f:0}%");
        }

        public float ConsumeOverflowBonus(bool isBasicStrike)
        {
            if (isBasicStrike || _host.OverflowNextHitBonus <= 0f)
                return 1f;
            float mult = 1f + _host.OverflowNextHitBonus;
            _host.OverflowNextHitBonus = 0f;
            return mult;
        }

        /// <summary>4-1 taşma: kalkan vuruşla kırılınca yakındaki boss itilir.</summary>
        public void OnJsonShieldBlocked()
        {
            double now = JsonNow;
            if (_host.TasarShieldUntilMs <= 0 || now >= _host.TasarShieldUntilMs || _host.PlayerStatus == null)
                return;
            if (_host.PlayerStatus.Board.ShieldRemaining > JsonEffectRuntimeDefaults.ShieldHeldEpsilon)
                return;
            _host.TasarShieldUntilMs = 0;
            if (_host.Player == null || _host.Boss == null
                || _host.FlatDistance(_host.Player.position, _host.Boss.transform.position) > JsonParam("shield_shock_radius_m", JsonEffectRuntimeDefaults.ShieldShockRadiusFallbackM))
                return;
            PushBossFromPlayer((float)JsonParam("it_push_m", 2.0), now);
            _host.Readout?.NoteSkill("Taşma", "kalkan kırıldı → şok", new Color(0.6f, 0.85f, 1f));
            JsonLog("kalkan kırıldı → şok");
        }

        // ---- incoming damage ----
        public bool TryParry(float incoming)
        {
            if (!_parry.TryConsume(JsonNow, incoming, out float reflected))
                return false;
            ApplyReflectedDamage(reflected);
            _host.Readout?.NoteSkill("Savuşturma", "+" + Mathf.RoundToInt(reflected), new Color(1f, 0.9f, 0.5f));
            JsonLog($"savuşturma {incoming:0.#} yutuldu, {reflected:0.#} geri");
            return true;
        }

        public void ApplyReflectedDamage(float amount)
        {
            if (amount <= 0f || _host.BossVitals == null || _host.BossVitals.IsDown)
                return;
            double now = JsonNow;
            if (now < _reflectSplitUntilMs)
            {
                JsonEffectRules.SplitReflect(amount, out float first, out float second);
                _host.BossVitals.ApplyDamage(first);
                JsonLog($"bölünen yansıma {first:0.#}+{second:0.#}");
                _host.ScheduleAfter(now, (float)JsonParam("split_reflect_delay_sec", JsonEffectRuntimeDefaults.SplitReflectDelaySecFallback), () =>
                {
                    if (_host.BossVitals != null && !_host.BossVitals.IsDown)
                        _host.BossVitals.ApplyDamage(second);
                });
                return;
            }
            _host.BossVitals.ApplyDamage(amount);
        }

        // ---- grammar atoms ----
        public void ApplyStolenArmor(MechanicEffect e, List<string> applied)
        {
            if (_host.PlayerStatus == null || _host.BossStatus == null)
                return;
            float flat = JsonEffectRules.StolenArmorFlat(e.Amount, _host.BossStatus.Armor.Base);
            if (flat <= 0f)
                return;
            _host.PlayerStatus.Armor.GrantBuff(flat, JsonNow + Math.Max(0.5, e.DurationSec) * SkillsTimeDefaults.SecToMs);
            applied.Add($"zırh çalma +{flat:0.#}");
        }

        public void ApplyStatusAdd(MechanicEffect e, List<string> applied)
        {
            if (_host.BossStatus == null)
                return;
            if (_host.LastStatusTransferMoved > 0)
            {
                applied.Add("durum ekle: aktarım yaptı");
                return;
            }
            StatusTuning t = JsonStatusTuning;
            int n = JsonEffectRules.StatusAddCount(e.Amount);
            _host.BossStatus.Board.Apply(StatusKind.Weaken, t.WeakenMs, t.WeakenOutgoingMult, "durum_ekle");
            if (n >= 2)
                _host.BossStatus.Board.Apply(StatusKind.ArmorBreak, t.ArmorBreakMs, t.ArmorBreakDamageTakenMult, "durum_ekle");
            applied.Add("durum ekle ×" + n);
        }

        public void LiftBoss(List<string> applied)
        {
            if (_host.Player == null || !BossDisplaceable)
                return;
            float lift = (float)JsonParam("knockup_lift_m", JsonEffectRuntimeDefaults.KnockupLiftFallbackM);
            float shake = _host.Combat != null ? _host.Combat.Manifestation.BossShakeSec * JsonEffectRuntimeDefaults.TravelHitShakeMult : JsonEffectRuntimeDefaults.TravelHitShakeMultAlt;
            _host.Boss.React(_host.Player.position, 0f, lift, shake, JsonNow);
            applied.Add($"havaya atma {lift:0.#}m");
        }

        public void ApplyMirroredDebuff(MechanicPlan plan)
        {
            MechanicEffect e = JsonEffectRules.MirroredEnemyDebuff(plan);
            if (e == null || _host.BossStatus == null)
                return;
            if (BossStatusMath.TryEnemyDamageDebuff(e.Amount, e.DurationSec, out float weaken, out double ms))
                _host.BossStatus.Board.Apply(StatusKind.Weaken, ms, weaken, "ters_kopya:" + plan.SkillId);
        }

        // ---- statuses / friendlies ----
        public void ApplyPurgePower(in SkillResolution skill, int removed)
        {
            if (removed <= 0 || !JsonEffectRules.PurgeGrantsPower(_host.MechanicPlanFor(skill)))
                return;
            float bonus = JsonEffectRules.PurgePower(removed, JsonParam("cleanse_power_per_status", JsonEffectRuntimeDefaults.CleansePowerPerStatusFallback));
            double now = JsonNow;
            float sec = !skill.Engine.IsNull ? skill.Engine.BuffDurationSec(JsonEffectRuntimeDefaults.PurgeBuffDurationFallbackSec) : JsonEffectRuntimeDefaults.PurgeBuffDurationFallbackSec;
            _host.SelfDamageBuff = (now < _host.SelfDamageBuffUntilMs ? _host.SelfDamageBuff : 0f) + bonus;
            _host.SelfDamageBuffUntilMs = Math.Max(_host.SelfDamageBuffUntilMs, now + Math.Max(0.5f, sec) * SkillsTimeDefaults.SecToMs);
            _host.Readout?.NoteSkill(skill.DisplayName, $"güç +{bonus * 100f:0}%", new Color(1f, 0.8f, 0.4f));
            JsonLog($"güce çevir {removed} durum → +{bonus * 100f:0}%");
        }

        public void ShareFriendlyStatuses(in SkillResolution skill, StatusBoard applied)
        {
            if (applied == null || _host.Ally == null || _host.Player == null || skill.IsEmpty || FriendlyTargetCap(skill) < 2)
                return;
            MechanicPlan plan = _host.MechanicPlanFor(skill);
            float range = Mathf.Max(JsonEffectRuntimeDefaults.MechanicRangeMinM, plan != null ? (float)plan.Body.SizeM : 0f);
            if (_host.FlatDistance(_host.Player.position, _host.Ally.transform.position) > range)
                return;
            _host.Ally.EnsureStatusBoard();
            StatusBoard other = applied == _host.Ally.Board
                ? (_host.PlayerStatus != null ? _host.PlayerStatus.Board : null)
                : _host.Ally.Board;
            if (other == null)
                return;
            if (applied.TryGet(StatusKind.Shield, out double rem, out float mag, out _) && mag > 0f && other.ShieldRemaining < mag)
            {
                other.Apply(StatusKind.Shield, rem, mag, "yayma:" + skill.SkillId);
                JsonLog($"kalkan paylaşıldı {mag:0.#}");
            }
            if (string.Equals(skill.Action, "cleanse", StringComparison.Ordinal))
                JsonLog("arınma paylaşıldı " + other.CleanseHostile(_host.JsonCleanseCount(skill)));
        }

        public void ApplyHealOverflow(in SkillResolution skill, int amount, int healed, bool toAlly)
        {
            if (!JsonEffectRules.Overflows(_host.MechanicPlanFor(skill), "can"))
                return;
            int over = JsonEffectRules.OverflowHeal(amount, healed);
            if (over <= 0)
                return;
            if (toAlly)
                _host.Ally?.EnsureStatusBoard();
            StatusBoard board = toAlly
                ? (_host.Ally != null ? _host.Ally.Board : null)
                : (_host.PlayerStatus != null ? _host.PlayerStatus.Board : null);
            if (board == null)
                return;
            float shield = over / CombatScale.DamageAndHp;
            board.Apply(StatusKind.Shield, JsonStatusTuning.ShieldMs, board.ShieldRemaining + shield, "tasar:" + skill.SkillId);
            _host.Readout?.NoteSkill(skill.DisplayName, "taşma → kalkan " + over, new Color(0.6f, 0.85f, 1f));
            JsonLog("taşma → kalkan " + over);
        }

        /// <summary>dosttan_dosta: sekme beat'i sıradaki dosta (oyuncu ↔ ally) gider.</summary>
        public bool TryBounceFriendly(float power)
        {
            SkillResolution skill = _host.DeliverySkill;
            if (skill.IsEmpty || !(_host.IsFriendlyFieldVerb(skill) || _host.IsHealSkill(skill)))
                return false;
            if (!JsonEffectRules.IsFriendlyBounce(_host.MechanicPlanFor(skill)))
                return false;
            bool toAlly = _host.Ally != null && JsonEffectRules.NextBounceIsAlly(_host.LastFriendlyWasAlly);
            bool cleanse = string.Equals(skill.Action, "cleanse", StringComparison.Ordinal);
            if (_host.IsHealSkill(skill) && !cleanse)
            {
                int amount = _host.CalculateClosingHealAmount(_host.DeliveryPending.Closing, skill, power, _host.TemplateChain);
                _host.ApplyClosingHealAmount(skill, amount, null, 0f, toAlly ? _host.Ally.transform : _host.Player);
            }
            if (toAlly)
                _host.Ally.EnsureStatusBoard();
            StatusBoard board = toAlly ? _host.Ally.Board : (_host.PlayerStatus != null ? _host.PlayerStatus.Board : null);
            if (board != null)
            {
                if (string.Equals(skill.Action, "shield", StringComparison.Ordinal))
                {
                    float absorb = _host.ShieldAbsorbFor(skill) * power * _host.WeaponFriendlyScale();
                    if (board.ShieldRemaining < absorb)
                        board.Apply(StatusKind.Shield, JsonStatusTuning.ShieldMs, absorb, "sekme:" + skill.SkillId);
                }
                if (cleanse)
                    board.CleanseHostile(_host.JsonCleanseCount(skill));
            }
            _host.LastFriendlyWasAlly = toAlly;
            JsonLog("dosttan dosta → " + (toAlly ? "ally" : "self"));
            return true;
        }

        /// <summary>inen_akis_alani: akış tiki yalnız iniş alanındaki hedefe (x-12 Top).</summary>
        public bool LandingFieldAllows(in SkillResolution skill)
        {
            MechanicPlan plan = _host.MechanicPlanFor(skill);
            if (plan == null || !JsonEffectRules.LandingFieldOnly(plan.Body))
                return true;
            bool friendly = _host.IsFriendlyFieldVerb(skill) || _host.IsHealSkill(skill);
            foreach (MechanicVolume v in _world.Volumes)
            {
                if (v.Plan != plan)
                    continue;
                if (!friendly && _host.Boss != null && _host.FlatDistance(_host.Boss.transform.position, v.Center) <= v.RadiusM)
                    return true;
                if (friendly && ((_host.Player != null && _host.FlatDistance(_host.Player.position, v.Center) <= v.RadiusM)
                    || (_host.Ally != null && _host.FlatDistance(_host.Ally.transform.position, v.Center) <= v.RadiusM)))
                    return true;
            }
            return false;
        }

        // ---- links ----

        // ---- template hits ----
        public bool AoeReachedMotionHit(in MotionHit hit)
        {
            if (_host.Boss == null || _host.TemplateSkill.IsEmpty || _host.TemplateSkill.Engine.IsNull
                || !_host.TemplateSkill.Engine.Aoe(false))
                return false;
            Vector3 b = _host.Boss.transform.position;
            float dist = _host.FlatDistance(b, new Vector3(hit.OriginX, b.y, hit.OriginZ));
            float body = _host.LastMechanicPlan != null ? (float)_host.LastMechanicPlan.Body.SizeM : 0f;
            return JsonEffectRules.AoeConnects(true, dist, hit.RadiusM, body, _host.BossBodyRadius());
        }

        // ---- weapon basic strike ----
        public bool BasicCadenceReady(double now)
        {
            WeaponCombatProfile w = _host.EquippedProfile;
            bool enforce = _host.Combat != null && _host.Combat.EnforceCooldown;
            return w == null || JsonEffectRules.BasicReady(now, _host.LastBasicStrikeMs, w.BasicIntervalSec, w.BasicHits, enforce);
        }

        public int BasicHitsNow() => _host.EquippedProfile != null ? Math.Max(1, _host.EquippedProfile.BasicHits) : 1;

        public void ScheduleBasicSubHits(ClosingHit closing, int hits, float reach)
        {
            WeaponCombatProfile w = _host.EquippedProfile;
            if (w == null || hits <= 1 || _host.Clock == null)
                return;
            float scale = JsonEffectRules.BasicSubHitScale(hits);
            double now = _host.Clock.Director.WorldTimeMs;
            for (int i = 1; i < hits; i++)
            {
                _host.ScheduleAfter(now, (float)JsonEffectRules.BasicSubHitDelaySec(i, w.BasicIntervalSec), () =>
                {
                    if (_host.Boss != null && _host.BasicTargetStillInReach(_host.Boss.transform, reach))
                        JsonLog($"düz vuruş alt-vuruş {_host.ApplyClosingDamage(closing, SkillResolution.Empty, true, 0f, scale, 1f):0.#}");
                });
            }
        }

        public void ApplyBasicExtras(float dealt, int hits)
        {
            WeaponCombatProfile w = _host.EquippedProfile;
            if (w == null || dealt <= 0f)
                return;
            string label = JsonEffectRules.BasicKindLabel(w.BasicKind, hits);
            if (label.Length > 0)
                _host.DebugHud?.NoteSkillBang("Düz vuruş", label);
            if (w.BasicAllyHeal <= 0f || _host.Ally == null || _host.Ally.Hp >= _host.Ally.MaxHp)
                return;
            DamageOutcome heal = DamagePipeline.Resolve(new DamageQuery
            {
                Heal = true,
                HealPower = w.BasicAllyHeal,
                HealMultiplier = 1f,
                ScaleMagnitudes = true
            });
            int healed = _host.Ally.ApplyHeal(Mathf.RoundToInt(heal.Amount));
            if (healed > 0)
            {
                _host.DamageHud?.ShowDamage(-healed);
                _host.Readout?.NoteSkill("Mühür", "ally +" + healed, new Color(0.4f, 1f, 0.65f));
                JsonLog("mühür ally +" + healed);
            }
        }
    
    }
}
