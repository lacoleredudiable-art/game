using Dovus.Core;
using Dovus.Core.Combat;
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

namespace Dovus.Game.Skills
{
    /// <summary>
    /// JSON etkileri (element-sistemi.json): etiket olarak kalan mod/anahtarların oyundaki karşılığı.
    /// Oyuncuyu asla taşımaz; boss yalnız BossReactor + ForcedDisplacement ile.
    /// </summary>
    public sealed partial class ManifestationDirector
    {
        readonly ParryWindow _parry = new ParryWindow();
        float _reflectRampBase;
        double _reflectRampStartMs;
        double _reflectRampUntilMs;
        double _reflectSplitUntilMs;
        int _lastStatusTransferMoved;
        bool _lastFriendlyWasAlly;
        double _tasarShieldUntilMs;
        float _overflowNextHitBonus;
        double _lastBasicStrikeMs = -1;
        SkillResolution _jsonCastSkill = SkillResolution.Empty;
        ClosingHit _jsonCastClosing;
        bool _jsonTickDamage;

        MechanicRules JsonRules => MechanicEngine != null ? MechanicEngine.Rules : null;
        double JsonNow => _clock != null ? _clock.Director.WorldTimeMs : 0;

        double JsonParam(string key, double fallback)
        {
            MechanicRules rules = JsonRules;
            double v = rules != null ? rules.Param(key) : 0;
            return v > 0 ? v : fallback;
        }

        bool BossDisplaceable =>
            _boss != null && ForcedDisplacement.Allows(_bossStatus != null ? _bossStatus.Board : null);

        StatusTuning JsonStatusTuning => _combat != null ? _combat.Status : new StatusTuning();

        /// <summary>Play/başsız tarama detay dosyasına düşer ("[Mechanic]" öneki yakalanır).</summary>
        static void JsonLog(string message) => DebugConfig.DevLog("[Mechanic] json " + message);

        void NoteJsonCast(in SkillResolution skill, ClosingHit closing)
        {
            _jsonCastSkill = skill;
            _jsonCastClosing = closing;
        }

        int FriendlyTargetCap(in SkillResolution skill) =>
            JsonEffectRules.FriendlyCap(!skill.IsEmpty && !skill.Engine.IsNull
                ? skill.Engine.MaxTargets(0)
                : 0);

        int JsonCleanseCount(in SkillResolution skill) =>
            JsonEffectRules.CleanseCount(
                !skill.IsEmpty && !skill.Engine.IsNull ? skill.Engine.CleanseCount(0) : 0,
                MechanicPlanFor(skill));

        float ShieldAbsorbFor(in SkillResolution skill)
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
        float ApplyJsonTickDamage(ClosingHit closing, in SkillResolution skill, float scale)
        {
            if (skill.IsEmpty || scale <= 0f)
                return 0f;
            _jsonTickDamage = true;
            try
            {
                return ApplyClosingDamage(closing, skill, false, 0f, scale, 1f);
            }
            finally
            {
                _jsonTickDamage = false;
            }
        }

        void PushBossFromPlayer(float meters, double now)
        {
            if (meters <= 0f || _player == null || !BossDisplaceable)
                return;
            float shake = _combat != null ? _combat.Manifestation.BossShakeSec * 0.45f : 0.12f;
            _boss.React(_player.position, meters, 0.05f, shake, now);
            JsonLog($"itme {meters:0.##}m");
        }

        // ---- per frame (called from TickMechanics) ----
        void TickJsonEffects(double worldMs)
        {
            if (_playerStatus != null && worldMs < _reflectRampUntilMs)
            {
                float ratio = JsonEffectRules.RampedRatio(
                    _reflectRampBase, _reflectRampStartMs, _reflectRampUntilMs, worldMs, JsonParam("ramp_max", 1.5));
                _playerStatus.GrantReflect(ratio, _reflectRampUntilMs);
            }
            if (_ally == null)
                return;
            Vector3 a = _ally.transform.position;
            foreach (MechanicVolume v in _mechanicVolumes)
            {
                if (!v.Profile.Reflector || !JsonEffectRules.ReflectorFollowsAlly(v.Plan))
                    continue;
                v.Center = new Vector3(a.x, v.Center.y, a.z);
                if (v.View != null)
                    v.View.transform.position = new Vector3(a.x, v.View.transform.position.y, a.z);
            }
        }

        // ---- cast time (end of ApplySelfCastEffects) ----
        void ApplyJsonSelfCast(MechanicPlan plan, float reflectRatio, float windowSec, double now)
        {
            if (plan == null)
                return;
            double untilMs = now + Math.Max(0.2f, windowSec) * 1000.0;
            if (JsonEffectRules.IsParry(plan))
            {
                _parry.Arm(untilMs, JsonEffectRules.ParryRatio(plan));
                JsonLog($"savuşturma penceresi {(untilMs - now) / 1000.0:0.##}sn");
            }
            if (JsonEffectRules.IsSplitReflect(plan))
                _reflectSplitUntilMs = untilMs;
            if (JsonEffectRules.IsRampReflect(plan) && reflectRatio > 0f && HasSelfReflect(plan))
            {
                _reflectRampBase = reflectRatio;
                _reflectRampStartMs = now;
                _reflectRampUntilMs = untilMs;
            }
            double hidden = JsonEffectRules.HiddenSec(plan, windowSec);
            if (hidden > 0 && _playerStatus != null)
            {
                _playerStatus.Board.Apply(StatusKind.Stealth, hidden * 1000.0, 1f, "gizli:" + plan.SkillId);
                JsonLog($"gizli {hidden:0.##}sn");
            }
            if (JsonEffectRules.Overflows(plan, "kalkan"))
                _tasarShieldUntilMs = now + JsonStatusTuning.ShieldMs;
            ApplyMirroredDebuff(plan);
        }

        /// <summary>8-1 taşma: güç buff'ı zaten açıkken yenisi gelirse eskisi bir sonraki vuruşa tek seferlik ek olur.</summary>
        void CaptureBuffOverflow(MechanicPlan plan, double now)
        {
            if (!JsonEffectRules.Overflows(plan, "hasar_buff") || now >= _selfDamageBuffUntilMs || _selfDamageBuff <= 0f)
                return;
            _overflowNextHitBonus = _selfDamageBuff;
            JsonLog($"taşma → sonraki vuruş +{_overflowNextHitBonus * 100f:0}%");
        }

        float ConsumeOverflowBonus(bool isBasicStrike)
        {
            if (isBasicStrike || _overflowNextHitBonus <= 0f)
                return 1f;
            float mult = 1f + _overflowNextHitBonus;
            _overflowNextHitBonus = 0f;
            return mult;
        }

        /// <summary>4-1 taşma: kalkan vuruşla kırılınca yakındaki boss itilir.</summary>
        void OnJsonShieldBlocked()
        {
            double now = JsonNow;
            if (_tasarShieldUntilMs <= 0 || now >= _tasarShieldUntilMs || _playerStatus == null)
                return;
            if (_playerStatus.Board.ShieldRemaining > 0.01f)
                return;
            _tasarShieldUntilMs = 0;
            if (_player == null || _boss == null
                || FlatDistance(_player.position, _boss.transform.position) > JsonParam("shield_shock_radius_m", 3.0))
                return;
            PushBossFromPlayer((float)JsonParam("it_push_m", 2.0), now);
            _readout?.NoteSkill("Taşma", "kalkan kırıldı → şok", new Color(0.6f, 0.85f, 1f));
            JsonLog("kalkan kırıldı → şok");
        }

        // ---- incoming damage ----
        bool TryParry(float incoming)
        {
            if (!_parry.TryConsume(JsonNow, incoming, out float reflected))
                return false;
            ApplyReflectedDamage(reflected);
            _readout?.NoteSkill("Savuşturma", "+" + Mathf.RoundToInt(reflected), new Color(1f, 0.9f, 0.5f));
            JsonLog($"savuşturma {incoming:0.#} yutuldu, {reflected:0.#} geri");
            return true;
        }

        void ApplyReflectedDamage(float amount)
        {
            if (amount <= 0f || _bossVitals == null || _bossVitals.IsDown)
                return;
            double now = JsonNow;
            if (now < _reflectSplitUntilMs)
            {
                JsonEffectRules.SplitReflect(amount, out float first, out float second);
                _bossVitals.ApplyDamage(first);
                JsonLog($"bölünen yansıma {first:0.#}+{second:0.#}");
                After(now, (float)JsonParam("split_reflect_delay_sec", 0.25), () =>
                {
                    if (_bossVitals != null && !_bossVitals.IsDown)
                        _bossVitals.ApplyDamage(second);
                });
                return;
            }
            _bossVitals.ApplyDamage(amount);
        }

        // ---- grammar atoms ----
        void ApplyStolenArmor(MechanicEffect e, List<string> applied)
        {
            if (_playerStatus == null || _bossStatus == null)
                return;
            float flat = JsonEffectRules.StolenArmorFlat(e.Amount, _bossStatus.Armor.Base);
            if (flat <= 0f)
                return;
            _playerStatus.Armor.GrantBuff(flat, JsonNow + Math.Max(0.5, e.DurationSec) * 1000.0);
            applied.Add($"zırh çalma +{flat:0.#}");
        }

        void ApplyStatusAdd(MechanicEffect e, List<string> applied)
        {
            if (_bossStatus == null)
                return;
            if (_lastStatusTransferMoved > 0)
            {
                applied.Add("durum ekle: aktarım yaptı");
                return;
            }
            StatusTuning t = JsonStatusTuning;
            int n = JsonEffectRules.StatusAddCount(e.Amount);
            _bossStatus.Board.Apply(StatusKind.Weaken, t.WeakenMs, t.WeakenOutgoingMult, "durum_ekle");
            if (n >= 2)
                _bossStatus.Board.Apply(StatusKind.ArmorBreak, t.ArmorBreakMs, t.ArmorBreakDamageTakenMult, "durum_ekle");
            applied.Add("durum ekle ×" + n);
        }

        void LiftBoss(List<string> applied)
        {
            if (_player == null || !BossDisplaceable)
                return;
            float lift = (float)JsonParam("knockup_lift_m", 0.8);
            float shake = _combat != null ? _combat.Manifestation.BossShakeSec * 0.45f : 0.12f;
            _boss.React(_player.position, 0f, lift, shake, JsonNow);
            applied.Add($"havaya atma {lift:0.#}m");
        }

        void ApplyMirroredDebuff(MechanicPlan plan)
        {
            MechanicEffect e = JsonEffectRules.MirroredEnemyDebuff(plan);
            if (e == null || _bossStatus == null)
                return;
            if (BossStatusMath.TryEnemyDamageDebuff(e.Amount, e.DurationSec, out float weaken, out double ms))
                _bossStatus.Board.Apply(StatusKind.Weaken, ms, weaken, "ters_kopya:" + plan.SkillId);
        }

        // ---- statuses / friendlies ----
        void ApplyPurgePower(in SkillResolution skill, int removed)
        {
            if (removed <= 0 || !JsonEffectRules.PurgeGrantsPower(MechanicPlanFor(skill)))
                return;
            float bonus = JsonEffectRules.PurgePower(removed, JsonParam("cleanse_power_per_status", 0.1));
            double now = JsonNow;
            float sec = !skill.Engine.IsNull ? skill.Engine.BuffDurationSec(3f) : 3f;
            _selfDamageBuff = (now < _selfDamageBuffUntilMs ? _selfDamageBuff : 0f) + bonus;
            _selfDamageBuffUntilMs = Math.Max(_selfDamageBuffUntilMs, now + Math.Max(0.5f, sec) * 1000.0);
            _readout?.NoteSkill(skill.DisplayName, $"güç +{bonus * 100f:0}%", new Color(1f, 0.8f, 0.4f));
            JsonLog($"güce çevir {removed} durum → +{bonus * 100f:0}%");
        }

        void ShareFriendlyStatuses(in SkillResolution skill, StatusBoard applied)
        {
            if (applied == null || _ally == null || _player == null || skill.IsEmpty || FriendlyTargetCap(skill) < 2)
                return;
            MechanicPlan plan = MechanicPlanFor(skill);
            float range = Mathf.Max(3f, plan != null ? (float)plan.Body.SizeM : 0f);
            if (FlatDistance(_player.position, _ally.transform.position) > range)
                return;
            _ally.EnsureStatusBoard();
            StatusBoard other = applied == _ally.Board
                ? (_playerStatus != null ? _playerStatus.Board : null)
                : _ally.Board;
            if (other == null)
                return;
            if (applied.TryGet(StatusKind.Shield, out double rem, out float mag, out _) && mag > 0f && other.ShieldRemaining < mag)
            {
                other.Apply(StatusKind.Shield, rem, mag, "yayma:" + skill.SkillId);
                JsonLog($"kalkan paylaşıldı {mag:0.#}");
            }
            if (string.Equals(skill.Action, "cleanse", StringComparison.Ordinal))
                JsonLog("arınma paylaşıldı " + other.CleanseHostile(JsonCleanseCount(skill)));
        }

        void ApplyHealOverflow(in SkillResolution skill, int amount, int healed, bool toAlly)
        {
            if (!JsonEffectRules.Overflows(MechanicPlanFor(skill), "can"))
                return;
            int over = JsonEffectRules.OverflowHeal(amount, healed);
            if (over <= 0)
                return;
            if (toAlly)
                _ally?.EnsureStatusBoard();
            StatusBoard board = toAlly
                ? (_ally != null ? _ally.Board : null)
                : (_playerStatus != null ? _playerStatus.Board : null);
            if (board == null)
                return;
            float shield = over / CombatScale.DamageAndHp;
            board.Apply(StatusKind.Shield, JsonStatusTuning.ShieldMs, board.ShieldRemaining + shield, "tasar:" + skill.SkillId);
            _readout?.NoteSkill(skill.DisplayName, "taşma → kalkan " + over, new Color(0.6f, 0.85f, 1f));
            JsonLog("taşma → kalkan " + over);
        }

        /// <summary>dosttan_dosta: sekme beat'i sıradaki dosta (oyuncu ↔ ally) gider.</summary>
        bool TryBounceFriendly(float power)
        {
            SkillResolution skill = _deliverySkill;
            if (skill.IsEmpty || !(IsFriendlyFieldVerb(skill) || IsHealSkill(skill)))
                return false;
            if (!JsonEffectRules.IsFriendlyBounce(MechanicPlanFor(skill)))
                return false;
            bool toAlly = _ally != null && JsonEffectRules.NextBounceIsAlly(_lastFriendlyWasAlly);
            bool cleanse = string.Equals(skill.Action, "cleanse", StringComparison.Ordinal);
            if (IsHealSkill(skill) && !cleanse)
            {
                int amount = CalculateClosingHealAmount(_deliveryPending.Closing, skill, power, _templateChain);
                ApplyClosingHealAmount(skill, amount, null, 0f, toAlly ? _ally.transform : _player);
            }
            if (toAlly)
                _ally.EnsureStatusBoard();
            StatusBoard board = toAlly ? _ally.Board : (_playerStatus != null ? _playerStatus.Board : null);
            if (board != null)
            {
                if (string.Equals(skill.Action, "shield", StringComparison.Ordinal))
                {
                    float absorb = ShieldAbsorbFor(skill) * power * WeaponFriendlyScale();
                    if (board.ShieldRemaining < absorb)
                        board.Apply(StatusKind.Shield, JsonStatusTuning.ShieldMs, absorb, "sekme:" + skill.SkillId);
                }
                if (cleanse)
                    board.CleanseHostile(JsonCleanseCount(skill));
            }
            _lastFriendlyWasAlly = toAlly;
            JsonLog("dosttan dosta → " + (toAlly ? "ally" : "self"));
            return true;
        }

        // ---- world payload (totem / trap / mine / fence / cloud tick / growing) ----
        void TickVolumePayload(MechanicVolume v, bool bossInside, bool playerInside, bool allyInside, double refreshMs)
        {
            if (v.Plan == null || _clock == null)
                return;
            double now = _clock.Director.WorldTimeMs;
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
                _readout?.NoteSkill(v.Skill.DisplayName, "tuzak tetiklendi", new Color(1f, 0.6f, 0.3f));
                JsonLog("tuzak tetiklendi " + v.Plan.SkillId);
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
                    float dealt = ApplyJsonTickDamage(v.Closing, v.Skill, scale);
                    if (dealt > 0f)
                        JsonLog($"{kind} tik {dealt:0.#}");
                    break;
                case "it":
                    PushBossFromPlayer((float)JsonParam("it_push_m", 2.0), now);
                    break;
                case "zirh":
                    _bossStatus?.Armor.ApplyShred(
                        (float)Math.Abs(e.Amount), now, now + (trap ? Math.Max(1.0, e.DurationSec) * 1000.0 : refreshMs));
                    break;
                case "hareket":
                    double ms = trap ? Math.Max(0.5, e.DurationSec) * 1000.0 : refreshMs;
                    if (e.Amount <= 0)
                        _bossStatus?.Board.Apply(StatusKind.Root, ms, 1f, src);
                    else if (e.Amount < 1)
                        _bossStatus?.Board.Apply(StatusKind.Slow, ms, (float)e.Amount, src);
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
                        ApplyClosingHeal(v.Closing, v.Skill, scale, 1f, v.Center, v.RadiusM);
                    break;
                case "kalkan":
                    float cap = ShieldAbsorbFor(v.Skill) * level * WeaponFriendlyScale();
                    float step = cap * (float)JsonParam("flow_tick_fraction", 0.33);
                    if (playerInside && _playerStatus != null)
                        TopUpShield(_playerStatus.Board, step, cap, refreshMs, src);
                    if (allyInside && _ally != null)
                    {
                        _ally.EnsureStatusBoard();
                        TopUpShield(_ally.Board, step, cap, refreshMs, src);
                    }
                    break;
                case "hasar_buff":
                    if (!playerInside)
                        break;
                    float buff = (float)Math.Abs(e.Amount) * level * WeaponFriendlyScale();
                    double now = JsonNow;
                    if (now >= _selfDamageBuffUntilMs || _selfDamageBuff < buff)
                        _selfDamageBuff = buff;
                    _selfDamageBuffUntilMs = Math.Max(_selfDamageBuffUntilMs, now + refreshMs);
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

        /// <summary>inen_akis_alani: akış tiki yalnız iniş alanındaki hedefe (x-12 Top).</summary>
        bool LandingFieldAllows(in SkillResolution skill)
        {
            MechanicPlan plan = MechanicPlanFor(skill);
            if (plan == null || !JsonEffectRules.LandingFieldOnly(plan.Body))
                return true;
            bool friendly = IsFriendlyFieldVerb(skill) || IsHealSkill(skill);
            foreach (MechanicVolume v in _mechanicVolumes)
            {
                if (v.Plan != plan)
                    continue;
                if (!friendly && _boss != null && FlatDistance(_boss.transform.position, v.Center) <= v.RadiusM)
                    return true;
                if (friendly && ((_player != null && FlatDistance(_player.position, v.Center) <= v.RadiusM)
                    || (_ally != null && FlatDistance(_ally.transform.position, v.Center) <= v.RadiusM)))
                    return true;
            }
            return false;
        }

        // ---- links ----
        void TickLinkFlow(MechanicLink link, double worldMs)
        {
            if (link.Plan == null || link.Skill.IsEmpty || !JsonEffectRules.LinkFlowsDamage(link.Plan))
                return;
            if (_boss == null || link.Target != _boss.transform || worldMs < link.NextFlowMs)
                return;
            link.NextFlowMs = worldMs + Math.Max(50.0, link.FlowTickMs);
            float dealt = ApplyJsonTickDamage(link.Closing, link.Skill, (float)JsonParam("flow_tick_fraction", 0.33));
            if (dealt > 0f)
                JsonLog($"bağ akışı {dealt:0.#}");
        }

        // ---- template hits ----
        bool AoeReachedMotionHit(in MotionHit hit)
        {
            if (_boss == null || _templateSkill.IsEmpty || _templateSkill.Engine.IsNull
                || !_templateSkill.Engine.Aoe(false))
                return false;
            Vector3 b = _boss.transform.position;
            float dist = FlatDistance(b, new Vector3(hit.OriginX, b.y, hit.OriginZ));
            float body = LastMechanicPlan != null ? (float)LastMechanicPlan.Body.SizeM : 0f;
            return JsonEffectRules.AoeConnects(true, dist, hit.RadiusM, body, BossBodyRadius());
        }

        // ---- weapon basic strike ----
        bool BasicCadenceReady(double now)
        {
            WeaponCombatProfile w = EquippedProfile;
            bool enforce = _combat != null && _combat.EnforceCooldown;
            return w == null || JsonEffectRules.BasicReady(now, _lastBasicStrikeMs, w.BasicIntervalSec, w.BasicHits, enforce);
        }

        int BasicHitsNow() => EquippedProfile != null ? Math.Max(1, EquippedProfile.BasicHits) : 1;

        void ScheduleBasicSubHits(ClosingHit closing, int hits, float reach)
        {
            WeaponCombatProfile w = EquippedProfile;
            if (w == null || hits <= 1 || _clock == null)
                return;
            float scale = JsonEffectRules.BasicSubHitScale(hits);
            double now = _clock.Director.WorldTimeMs;
            for (int i = 1; i < hits; i++)
            {
                After(now, (float)JsonEffectRules.BasicSubHitDelaySec(i, w.BasicIntervalSec), () =>
                {
                    if (_boss != null && BasicTargetStillInReach(_boss.transform, reach))
                        JsonLog($"düz vuruş alt-vuruş {ApplyClosingDamage(closing, SkillResolution.Empty, true, 0f, scale, 1f):0.#}");
                });
            }
        }

        void ApplyBasicExtras(float dealt, int hits)
        {
            WeaponCombatProfile w = EquippedProfile;
            if (w == null || dealt <= 0f)
                return;
            string label = JsonEffectRules.BasicKindLabel(w.BasicKind, hits);
            if (label.Length > 0)
                _debugHud?.NoteSkillBang("Düz vuruş", label);
            if (w.BasicAllyHeal <= 0f || _ally == null || _ally.Hp >= _ally.MaxHp)
                return;
            DamageOutcome heal = DamagePipeline.Resolve(new DamageQuery
            {
                Heal = true,
                HealPower = w.BasicAllyHeal,
                HealMultiplier = 1f,
                ScaleMagnitudes = true
            });
            int healed = _ally.ApplyHeal(Mathf.RoundToInt(heal.Amount));
            if (healed > 0)
            {
                _damageHud?.ShowDamage(-healed);
                _readout?.NoteSkill("Mühür", "ally +" + healed, new Color(0.4f, 1f, 0.65f));
                JsonLog("mühür ally +" + healed);
            }
        }
    }
}
