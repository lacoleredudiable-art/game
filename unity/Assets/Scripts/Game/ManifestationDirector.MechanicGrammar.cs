using System;
using System.Collections.Generic;
using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// mechanic_grammar köprüsü: her cast'te (fiil, sıfat, takılı silah) planı kurulur, HUD'a
    /// kısa adı yazılır ve mevcut motorun karşılamadığı atomlar dünyaya uygulanır.
    /// Eşleme atom türüne göredir (tempo → Stun/Slow/Haste, kök, çekme, ışınlanma...);
    /// skill'e özel dal yoktur. Hasar/heal/kopya/çağırma/yansıma ve uyumsuz silah cezası
    /// mevcut motorda kalır — burada tekrar uygulanmaz.
    /// </summary>
    public sealed partial class ManifestationDirector
    {
        struct MechanicTimer
        {
            public double DueMs;
            public Action Run;
        }

        sealed class PortalPair
        {
            public GameObject A;
            public GameObject B;
            public double UntilMs;
            public bool Inside;
        }

        readonly Dictionary<(int, int, int), MechanicPlan> _mechanicPlans = new();
        string _cardEffect = string.Empty;
        readonly List<MechanicTimer> _mechanicTimers = new();
        readonly List<PortalPair> _portals = new();

        /// <summary>Son cast'in gramer planı (test/HUD).</summary>
        public MechanicPlan LastMechanicPlan { get; private set; }

        MechanicGrammar MechanicEngine =>
            ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design) ? design.Mechanics : null;

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

        /// <summary>Kapanış patlamasında: plan kurulur, kendine yönelik atomlar uygulanır.</summary>
        void BeginMechanicPlan(in SkillResolution skill, Vector3 aimDir, Vector3 landedAt)
        {
            MechanicPlan plan = MechanicPlanFor(skill);
            LastMechanicPlan = plan;
            _cardEffect = skill.SkillJob ?? string.Empty;
            if (plan == null)
                return;
            Debug.Log($"[Mechanic] {plan.SkillId}/{plan.WeaponName}: {MechanicDescriber.ShortTitle(plan)} — {plan.Description}");
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
                            applied.Add("arkaya iniş kalıpta");
                            break;
                        }
                        // Kalıp oyuncuyu oynatmıyorsa eski ışınlanma durur.
                        float dashSec = _combat != null ? _combat.SkillMotion.DashDurationSec : 0f;
                        After(now, dashSec, TeleportBehindBoss);
                        applied.Add("arkaya ışınlanma");
                        break;
                    case ("konum", "isaret_geri_don"):
                        Vector3 mark = _player.position;
                        if (TemplateOwnsPosition(plan, e.Stat))
                        {
                            applied.Add($"işaret ({mark.x:0.#},{mark.z:0.#}) dönüş kalıpta");
                            break;
                        }
                        After(now, (float)Math.Max(e.DurationSec, 0.0), () => TeleportPlayer(mark));
                        applied.Add($"işaret → {e.DurationSec:0.#}sn sonra dönüş");
                        break;
                    case ("konum", "portal"):
                        if (Dovus.Core.Portal.PortalSystem.IsPortalSkill(plan.SkillId))
                        {
                            applied.Add("portal sistemi");
                            break;
                        }
                        OpenPortal(plan, aimDir, now + e.DurationSec * 1000.0);
                        applied.Add($"portal {e.DurationSec:0.#}sn");
                        break;
                    case ("varlik", "durum_aktar"):
                        ApplyStatusTransfer(applied);
                        break;
                }
            }
            if (applied.Count > 0)
                Debug.Log($"[Mechanic] kendine {plan.SkillId}/{plan.WeaponName}: {string.Join(", ", applied)}");
        }

        bool TemplateOwnsPosition(MechanicPlan plan, string stat)
        {
            if (!_templateOwnsPosition || plan == null)
                return false;
            PositionOwnership.LogSuppressed(plan.SkillId, stat);
            return true;
        }

        /// <summary>Gövde düşmana değdiğinde (bir kez): düşmana yönelik atomlar.</summary>
        void ApplyMechanicHitEffects(MechanicPlan plan, Vector3 center)
        {
            if (plan == null || _bossStatus == null || _boss == null)
                return;
            StatusBoard boss = _bossStatus.Board;
            MechanicGrammar grammar = MechanicEngine;
            var applied = new List<string>();

            foreach (MechanicEffect e in plan.Effects)
            {
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
                        if (hasteCard && !CardEffectRules.Names(_cardEffect, "root"))
                            break;
                        if (e.Amount <= 0)
                        {
                            bool daze = e.Has("havada") || e.Has("sersem");
                            bool hammer = e.Has("sersem")
                                && EquippedProfile != null
                                && EquippedProfile.Passive.Id == "yere_cakma";
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
                        // Girdap merkezi değil: her zaman oyuncunun önündeki temas noktası.
                        PullBossToPlayerContact();
                        applied.Add("çekme");
                        break;
                    case ("konum", "it") when e.Has("yukari_firlat") && grammar != null:
                        ApplyOnce(boss, StatusKind.Stun, grammar.Rules.Param("knockup_sec") * 1000.0, 1f, applied);
                        break;
                    case ("konum", "yer_degistir") when _clock != null && _player != null:
                        if (TemplateOwnsPosition(plan, e.Stat))
                        {
                            applied.Add("yer değiştirme kalıpta");
                            break;
                        }
                        // Temas anındaki tarafın aynası; dash konumu sürdüğü için dash bitince iner.
                        Vector3 swapTo = MirroredAcrossBoss(_player.position);
                        float dashSec = _combat != null ? _combat.SkillMotion.DashDurationSec : 0f;
                        After(_clock.Director.WorldTimeMs, dashSec, () => TeleportPlayer(swapTo));
                        applied.Add("yer değiştirme");
                        break;
                    case ("hiz", "geri_sar") when _clock != null:
                        RewindBoss(e.Amount, _clock.Director.WorldTimeMs, applied);
                        break;
                    case ("varlik", "durum_aktar"):
                        ApplyStatusTransfer(applied);
                        break;
                    case ("varlik", "iyi_durum_sil"):
                        PurgeBossBuffs(applied);
                        break;
                }
            }
            if (applied.Count > 0)
                Debug.Log($"[Mechanic] isabet {plan.SkillId}/{plan.WeaponName}: {string.Join(", ", applied)}");
        }

        static void ApplyOnce(
            StatusBoard board, StatusKind kind, double ms, float magnitude, List<string> applied,
            string sourceId = null)
        {
            // Aynı cast'in eski motor durumu zaten verdiyse süre ikinci kez uzamasın.
            // Kök ayrı: ikinci kaynak süreyi uzatmaz, en uzun olan kalır.
            if (ms <= 0 || (kind != StatusKind.Root && board.Has(kind)))
                return;
            board.Apply(kind, ms, magnitude, sourceId);
            applied.Add($"{kind} {ms / 1000.0:0.##}sn");
        }

        void After(double now, float delaySec, Action run) =>
            _mechanicTimers.Add(new MechanicTimer { DueMs = now + delaySec * 1000.0, Run = run });

        void TickMechanics(double worldMs)
        {
            for (int i = _mechanicTimers.Count - 1; i >= 0; i--)
            {
                if (worldMs < _mechanicTimers[i].DueMs)
                    continue;
                Action run = _mechanicTimers[i].Run;
                _mechanicTimers.RemoveAt(i);
                run();
            }
            TickPortals(worldMs);
            TickMechanicWorld(worldMs);
        }

        Vector3 ClampToArena(Vector3 pos) =>
            _motor != null ? ArenaClamp.XZ(pos, _motor.Tuning.ArenaHalfSizeM, _motor.BodyRadiusM) : pos;

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
        /// Boss prototipte _home'a çapalı (BossReactor her kare geri çeker); yer değiştirme
        /// oyuncuyu boss'un karşı tarafına, aynı mesafeye taşır.
        /// </summary>
        Vector3 MirroredAcrossBoss(Vector3 from)
        {
            if (_boss == null)
                return from;
            Vector3 bossPos = _boss.transform.position;
            return bossPos + (bossPos - from);
        }

        // ---------------------------------------------------------------- portal

        void OpenPortal(MechanicPlan plan, Vector3 aimDir, double untilMs)
        {
            if (_player == null)
                return;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 0.0001f)
                aimDir = _player.forward;
            Vector3 a = _player.position;
            Vector3 b = ClampToArena(a + aimDir.normalized * (float)plan.Body.ReachM);
            _portals.Add(new PortalPair
            {
                A = CreatePortalGate(a),
                B = CreatePortalGate(b),
                UntilMs = untilMs,
                Inside = true
            });
        }

        GameObject CreatePortalGate(Vector3 pos)
        {
            MechanicGrammar grammar = MechanicEngine;
            float r = grammar != null ? (float)grammar.Rules.Param("portal_trigger_radius_m") : 1f;
            GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gate.name = "MechanicPortal";
            Destroy(gate.GetComponent<Collider>());
            gate.transform.SetParent(transform, true);
            gate.transform.position = new Vector3(pos.x, 0.03f, pos.z);
            gate.transform.localScale = new Vector3(r * 2f, 0.02f, r * 2f);
            return gate;
        }

        void TickPortals(double worldMs)
        {
            if (_portals.Count == 0 || _player == null)
                return;
            MechanicGrammar grammar = MechanicEngine;
            float r = grammar != null ? (float)grammar.Rules.Param("portal_trigger_radius_m") : 1f;
            Vector3 p = _player.position;
            for (int i = _portals.Count - 1; i >= 0; i--)
            {
                PortalPair pair = _portals[i];
                if (worldMs >= pair.UntilMs || pair.A == null || pair.B == null)
                {
                    if (pair.A != null) Destroy(pair.A);
                    if (pair.B != null) Destroy(pair.B);
                    _portals.RemoveAt(i);
                    continue;
                }
                Vector3 a = pair.A.transform.position;
                Vector3 b = pair.B.transform.position;
                bool inA = FlatDistance(p, a) <= r;
                bool inB = FlatDistance(p, b) <= r;
                // Çıkış kapısında belirince oyuncu kapıdan çıkana kadar tekrar geçiş yok.
                if (!inA && !inB)
                    pair.Inside = false;
                else if (!pair.Inside)
                {
                    TeleportPlayer(inA ? b : a);
                    pair.Inside = true;
                }
            }
        }
    }
}
