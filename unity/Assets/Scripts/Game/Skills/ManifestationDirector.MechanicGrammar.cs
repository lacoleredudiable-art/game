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
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Data;
using Dovus.Game.DevTools;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    /// <summary>
    /// mechanic_grammar kÃ¶prÃ¼sÃ¼: her cast'te (fiil, sÄ±fat, takÄ±lÄ± silah) planÄ± kurulur, HUD'a
    /// kÄ±sa adÄ± yazÄ±lÄ±r ve mevcut motorun karÅŸÄ±lamadÄ±ÄŸÄ± atomlar dÃ¼nyaya uygulanÄ±r.
    /// EÅŸleme atom tÃ¼rÃ¼ne gÃ¶redir (tempo â†’ Stun/Slow/Haste, kÃ¶k, Ã§ekme, Ä±ÅŸÄ±nlanma...);
    /// skill'e Ã¶zel dal yoktur. Hasar/heal/kopya/Ã§aÄŸÄ±rma/yansÄ±ma ve uyumsuz silah cezasÄ±
    /// mevcut motorda kalÄ±r â€” burada tekrar uygulanmaz.
    /// </summary>
    public sealed partial class ManifestationDirector
    {
        readonly Dictionary<(int, int, int), MechanicPlan> _mechanicPlans = new();
        string _cardEffect = string.Empty;

        /// <summary>Son cast'in gramer planÄ± (test/HUD).</summary>
        public MechanicPlan LastMechanicPlan { get; set; }

        int EquippedWeaponNumber()
        {
            if (_equippedWeapon == null)
                return 0;
            string id = _equippedWeapon.Id ?? string.Empty;
            int colon = id.LastIndexOf(':');
            int.TryParse(colon >= 0 ? id.Substring(colon + 1) : id, out int weaponId);
            return weaponId;
        }

        MechanicPlan MechanicPlanFor(in SkillResolution skill)
        {
            MechanicGrammar grammar = MechanicEngine;
            if (grammar == null || skill.IsEmpty
                || !int.TryParse(skill.VerbId, out int verb)
                || !int.TryParse(skill.AdjectiveId, out int adjective))
                return null;
            int weapon = EquippedWeaponNumber();
            var key = (verb, adjective, weapon);
            if (!_mechanicPlans.TryGetValue(key, out MechanicPlan plan))
            {
                plan = grammar.Compose(verb, adjective, weapon);
                _mechanicPlans[key] = plan;
            }
            return plan;
        }

        /// <summary>KapanÄ±ÅŸ patlamasÄ±nda: plan kurulur, kendine yÃ¶nelik atomlar uygulanÄ±r.</summary>
        void BeginMechanicPlan(in SkillResolution skill, Vector3 aimDir, Vector3 landedAt)
        {
            MechanicPlan plan = MechanicPlanFor(skill);
            LastMechanicPlan = plan;
            _cardEffect = skill.SkillJob ?? string.Empty;
            if (plan == null)
                return;
            DebugConfig.DevLog($"[Mechanic] {plan.SkillId}/{plan.WeaponName}: {MechanicDescriber.ShortTitle(plan)} â€” {plan.Description}");
            ApplyMechanicSelfEffects(plan, aimDir);
            if (_clock != null)
                BeginMechanicWorld(plan, aimDir, landedAt, _clock.Director.WorldTimeMs);
        }

        void ApplyMechanicSelfEffects(MechanicPlan plan, Vector3 aimDir)
        {
            if (_player == null || _clock == null)
                return;
            double now = _clock.Director.WorldTimeMs;
            StatusBoard self = _playerStatus != null ? _playerStatus.Board : null;
            bool bodyOnSelf = plan.Body.BornAt == "sende";
            var applied = new List<string>();

            foreach (MechanicEffect e in plan.Effects)
            {
                if (e.Target != "kendin" && e.Target != "dost")
                    continue;
                switch (e.Atom, e.Stat)
                {
                    case ("hiz", "tempo") when (bodyOnSelf || e.Has("aktarim"))
                        && e.Amount > 1 && e.DurationSec > 0 && self != null:
                        ApplyOnce(
                            self,
                            StatusKind.Haste,
                            e.DurationSec * 1000.0,
                            WeaponPassiveRules.ScaleFriendlyMagnitude((float)e.Amount, WeaponFriendlyScale()),
                            applied);
                        break;
                    case ("gorunurluk", "gizlen") when bodyOnSelf && e.DurationSec > 0 && self != null:
                        ApplyOnce(self, StatusKind.Stealth, e.DurationSec * 1000.0, 1f, applied);
                        break;
                    case ("konum", "hedefin_arkasina"):
                        if (TemplateOwnsPosition(plan, e.Stat))
                        {
                            applied.Add("arkaya iniÅŸ kalÄ±pta");
                            break;
                        }
                        // KalÄ±p oyuncuyu oynatmÄ±yorsa eski Ä±ÅŸÄ±nlanma durur.
                        float dashSec = _combat != null ? _combat.SkillMotion.DashDurationSec : 0f;
                        After(now, dashSec, TeleportBehindBoss);
                        applied.Add("arkaya Ä±ÅŸÄ±nlanma");
                        break;
                    case ("konum", "isaret_geri_don"):
                        Vector3 mark = _player.position;
                        if (TemplateOwnsPosition(plan, e.Stat))
                        {
                            applied.Add($"iÅŸaret ({mark.x:0.#},{mark.z:0.#}) dÃ¶nÃ¼ÅŸ kalÄ±pta");
                            break;
                        }
                        After(now, (float)Math.Max(e.DurationSec, 0.0), () => TeleportPlayer(mark));
                        applied.Add($"iÅŸaret â†’ {e.DurationSec:0.#}sn sonra dÃ¶nÃ¼ÅŸ");
                        break;
                    case ("konum", "portal"):
                        if (Dovus.Core.Portal.PortalSystem.IsPortalSkill(plan.SkillId))
                        {
                            applied.Add("portal sistemi");
                            break;
                        }
                        EnsureMechanicsServices();
                        _mechanicPortals.OpenPortal(plan, aimDir, now + e.DurationSec * 1000.0);
                        applied.Add($"portal {e.DurationSec:0.#}sn");
                        break;
                    case ("varlik", "durum_aktar"):
                        EnsureMechanicsServices();
                        _mechanicWorld.ApplyStatusTransfer(applied);
                        break;
                }
            }
            if (applied.Count > 0)
                DebugConfig.DevLog($"[Mechanic] kendine {plan.SkillId}/{plan.WeaponName}: {string.Join(", ", applied)}");
        }

        bool TemplateOwnsPosition(MechanicPlan plan, string stat)
        {
            if (!_templateOwnsPosition || plan == null)
                return false;
            PositionOwnership.LogSuppressed(plan.SkillId, stat);
            return true;
        }

        /// <summary>
        /// mechanic_grammar "ters_kontrol" (TersCevir Ã— hareket): boss'un kontrolÃ¼ sÃ¼re boyunca ters â€”
        /// yaklaÅŸma hedeften uzaklaÅŸÄ±r, windup kilidi ters yÃ¶ne bakar. KÃ¶k/yavaÅŸlatma ayrÄ±ca uygulanÄ±r.
        /// SÃ¼re etkinin kendi sÃ¼resi; yoksa BossTuning.ReverseFallbackSec.
        /// </summary>
        void ApplyBossReverse(MechanicEffect e, List<string> applied)
        {
            if (_bossDirector == null || _clock == null)
                return;
            double sec = e.DurationSec > 0
                ? e.DurationSec
                : (_combat != null ? _combat.Boss.ReverseFallbackSec : 1.5f);
            _bossDirector.ApplyReverse(_clock.Director.WorldTimeMs + sec * 1000.0);
            applied.Add("ters kontrol");
        }

        /// <summary>
        /// GÃ¶vde dÃ¼ÅŸmana deÄŸdiÄŸinde (bir kez): dÃ¼ÅŸmana yÃ¶nelik atomlar.
        /// casterMoves false: kalÄ±p sonrasÄ± teslim kuyruÄŸu oyuncuyu yerinden oynatmaz.
        /// </summary>
        void ApplyMechanicHitEffects(MechanicPlan plan, Vector3 center, bool casterMoves = true)
        {
            if (plan == null || _bossStatus == null || _boss == null)
                return;
            StatusBoard boss = _bossStatus.Board;
            MechanicGrammar grammar = MechanicEngine;
            var applied = new List<string>();

            foreach (MechanicEffect e in plan.Effects)
            {
                if (e.Atom == "deger" && e.Stat == "zirh" && e.Target == "kendin" && e.Has("aktarim"))
                {
                    ApplyStolenArmor(e, applied);
                    continue;
                }
                if (e.Target != "dusman")
                    continue;
                double ms = e.DurationSec * 1000.0;
                bool hasteCard = CardEffectRules.WantsSelfHaste(_cardEffect);
                switch (e.Atom, e.Stat)
                {
                    case ("hiz", "tempo"):
                        if (hasteCard)
                            break;
                        if (e.Has("dondur") || e.Amount <= 0)
                            ApplyOnce(boss, StatusKind.Stun, ms, 1f, applied);
                        else if (e.Amount < 1)
                            ApplyOnce(boss, StatusKind.Slow, ms, (float)e.Amount, applied);
                        break;
                    case ("hiz", "hareket"):
                        if (e.Has("ters_kontrol"))
                            ApplyBossReverse(e, applied);
                        if (hasteCard && !CardEffectRules.Names(_cardEffect, "root"))
                            break;
                        if (e.Amount <= 0)
                        {
                            bool daze = e.Has("havada") || e.Has("sersem");
                            bool hammer = e.Has("sersem")
                                && EquippedProfile != null
                                && EquippedProfile.Passive.Kind == WeaponPassiveKind.YereCakma;
                            double now = _clock != null ? _clock.Director.WorldTimeMs : 0;
                            bool ready = !hammer || HammerStunReady(now);
                            if (hammer && !ready)
                                break;
                            StatusKind lockKind = CardEffectRules.MovementLockKind(_cardEffect, daze);
                            bool had = boss.Has(lockKind);
                            ApplyOnce(
                                boss,
                                lockKind,
                                ms, 1f, applied,
                                lockKind == StatusKind.Root ? "hit:" + plan.SkillId : null);
                            if (hammer)
                                CommitHammerStun(now, ready, had, !had && boss.Has(lockKind));
                        }
                        else if (e.Amount < 1)
                            ApplyOnce(boss, StatusKind.Slow, ms, (float)e.Amount, applied);
                        break;
                    case ("deger", "hasar_buff"):
                        if (BossStatusMath.TryEnemyDamageDebuff(e.Amount, e.DurationSec, out float weaken, out double weakenMs))
                            ApplyOnce(boss, StatusKind.Weaken, weakenMs, weaken, applied);
                        break;
                    case ("gorunurluk", "kor"):
                        if (CardEffectRules.AccuracyIsSlow(_cardEffect))
                        {
                            float slow = e.Amount > 0 && e.Amount < 1
                                ? (float)e.Amount
                                : SkillNumberFallbacks.TempoSyncFallbackStrength;
                            ApplyOnce(boss, StatusKind.Slow, ms, slow, applied);
                        }
                        else
                            ApplyOnce(boss, StatusKind.Blind, ms, (float)Math.Max(e.Amount, 0.0), applied);
                        break;
                    case ("konum", "cek"):
                        // Girdap merkezi deÄŸil: her zaman oyuncunun Ã¶nÃ¼ndeki temas noktasÄ±.
                        PullBossToPlayerContact();
                        applied.Add("Ã§ekme");
                        break;
                    case ("konum", "it") when e.Has("yukari_firlat") && grammar != null:
                        ApplyOnce(boss, StatusKind.Stun, grammar.Rules.Param("knockup_sec") * 1000.0, 1f, applied);
                        break;
                    case ("konum", "yer_degistir") when _clock != null && _player != null:
                        if (!casterMoves)
                        {
                            applied.Add("yer deÄŸiÅŸtirme kuyrukta yok");
                            break;
                        }
                        if (TemplateOwnsPosition(plan, e.Stat))
                        {
                            applied.Add("yer deÄŸiÅŸtirme kalÄ±pta");
                            break;
                        }
                        // Temas anÄ±ndaki tarafÄ±n aynasÄ±; dash konumu sÃ¼rdÃ¼ÄŸÃ¼ iÃ§in dash bitince iner.
                        Vector3 swapTo = MirroredAcrossBoss(_player.position);
                        float dashSec = _combat != null ? _combat.SkillMotion.DashDurationSec : 0f;
                        After(_clock.Director.WorldTimeMs, dashSec, () => TeleportPlayer(swapTo));
                        applied.Add("yer deÄŸiÅŸtirme");
                        break;
                    case ("hiz", "geri_sar") when _clock != null:
                        EnsureMechanicsServices();
                        _mechanicWorld.RewindBoss(e.Amount, _clock.Director.WorldTimeMs, applied);
                        break;
                    case ("varlik", "durum_aktar"):
                        EnsureMechanicsServices();
                        _mechanicWorld.ApplyStatusTransfer(applied);
                        break;
                    case ("varlik", "durum_ekle"):
                        ApplyStatusAdd(e, applied);
                        break;
                    case ("varlik", "iyi_durum_sil"):
                        EnsureMechanicsServices();
                        _mechanicWorld.PurgeBossBuffs(applied);
                        break;
                }
            }
            if (JsonEffectRules.LiftsBoss(plan))
                LiftBoss(applied);
            ProjectileEraseOnHit(plan, center);
            if (applied.Count > 0)
                DebugConfig.DevLog($"[Mechanic] isabet {plan.SkillId}/{plan.WeaponName}: {string.Join(", ", applied)}");
        }

        static void ApplyOnce(
            StatusBoard board, StatusKind kind, double ms, float magnitude, List<string> applied,
            string sourceId = null)
        {
            // AynÄ± cast'in eski motor durumu zaten verdiyse sÃ¼re ikinci kez uzamasÄ±n.
            // KÃ¶k ayrÄ±: ikinci kaynak sÃ¼reyi uzatmaz, en uzun olan kalÄ±r.
            if (ms <= 0 || (kind != StatusKind.Root && board.Has(kind)))
                return;
            board.Apply(kind, ms, magnitude, sourceId);
            applied.Add($"{kind} {ms / 1000.0:0.##}sn");
        }

        void After(double now, float delaySec, Action run)
        {
            EnsureMechanicsServices();
            _mechanicPortals.ScheduleAfter(now, delaySec, run);
        }

        void TickMechanics(double worldMs)
        {
            EnsureMechanicsServices();
            _mechanicPortals.TickTimers(worldMs);
            _mechanicPortals.TickPortals(worldMs);
            _mechanicWorld.Tick(worldMs);
            TickProjectileErase(worldMs);
            _jsonEffects.Tick(worldMs);
        }

        Vector3 ClampToArena(Vector3 pos) =>
            _motor != null ? ArenaClamp.XZ(pos, _motor.Tuning.Arena.ArenaHalfSizeM, _motor.BodyRadiusM) : pos;

        void TeleportPlayer(Vector3 pos)
        {
            if (_player == null)
                return;
            pos.y = _player.position.y;
            _player.position = ClampToArena(pos);
        }

        void TeleportBehindBoss()
        {
            if (_player == null || _boss == null)
                return;
            Vector3 bossPos = _boss.transform.position;
            Vector3 away = bossPos - _player.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = _boss.transform.forward;
            float offset = _combat != null ? _combat.Manifestation.BasicStrikeRangeM * 0.5f : 1f;
            TeleportPlayer(bossPos + away.normalized * offset);
        }

        /// <summary>
        /// Boss prototipte _home'a Ã§apalÄ± (BossReactor her kare geri Ã§eker); yer deÄŸiÅŸtirme
        /// oyuncuyu boss'un karÅŸÄ± tarafÄ±na, aynÄ± mesafeye taÅŸÄ±r.
        /// </summary>
        Vector3 MirroredAcrossBoss(Vector3 from)
        {
            if (_boss == null)
                return from;
            Vector3 bossPos = _boss.transform.position;
            return bossPos + (bossPos - from);
        }

    }
}
