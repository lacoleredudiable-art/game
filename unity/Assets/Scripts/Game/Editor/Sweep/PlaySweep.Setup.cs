#if UNITY_EDITOR
using Dovus.App.Sweep;
using Dovus.Core.Boss;
using Dovus.Core.Element;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using SkillId = Dovus.Core.Shared.SkillId;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Team;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Dovus.Game.Editor.Sweep
{
    public static partial class PlaySweep
    {
            // ---------------------------------------------------------------- kurulum

            static bool EnsureLoadout(int v, int a, out string error)
            {
                error = "";
                RuneLoadout current = _input.Engine?.Loadout;
                if (!BuildSelectHud.IsOpen && current != null && current.RuneIds.Contains(v) && current.RuneIds.Contains(a))
                    return true;

                int key = SweepComboCatalog.BuildKey(v, a);
                var ids = new List<int>(SweepComboCatalog.RuneGroups[key / 4]);
                ids.AddRange(SweepComboCatalog.RuneGroups[key % 4]);

                var screen = UnityEngine.Object.FindAnyObjectByType<BuildSelectHud>(FindObjectsInactive.Include);
                if (screen == null)
                {
                    error = "BuildSelectHud yok";
                    return false;
                }
                screen.Open();
                var selected = screen.SweepSelected;
                selected.Clear();
                selected.AddRange(ids);
                screen.SweepPassiveSelected?.Clear();
                var weapons = screen.SweepWeapons;
                EquipmentItem primary = FindWeapon(_cases[_index].Weapon);
                if (weapons != null && primary != null)
                {
                    weapons.Clear();
                    weapons.Add(primary);
                    EquipmentItem second = FindWeapon(_secondWeapon);
                    if (second == null || second.Id == primary.Id)
                        second = _md.AvailableWeapons.FirstOrDefault(w => w.Id != primary.Id);
                    if (second != null)
                        weapons.Add(second);
                }
                screen.SweepApplyAndStart();
                if (BuildSelectHud.IsOpen)
                    screen.SweepClose();
                current = _input.Engine?.Loadout;
                if (current == null || !current.RuneIds.Contains(v) || !current.RuneIds.Contains(a))
                {
                    error = "build uygulanmadı [" + string.Join(",", ids) + "]";
                    return false;
                }
                return true;
            }

            static EquipmentItem FindWeapon(string name)
            {
                if (string.IsNullOrEmpty(name) || _md == null)
                    return null;
                foreach (EquipmentItem w in _md.AvailableWeapons)
                {
                    if (string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)
                        || (w.Name != null && w.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
                        return w;
                }
                return null;
            }

            static void EnsureWeapon(string name)
            {
                EquipmentItem w = FindWeapon(name);
                if (w == null)
                    return;
                if (_md.EquippedWeapon != null && _md.EquippedWeapon.Id == w.Id)
                    return;
                EquipmentItem second = FindWeapon(_secondWeapon);
                if (second == null || second.Id == w.Id)
                    second = _md.AvailableWeapons.FirstOrDefault(x => x.Id != w.Id);
                _md.SetWeaponLoadout(w, second);
            }

            static void ForceClean()
            {
                _body?.Stop();
                _player.GetComponent<ActorView>()?.EndMotionAnim();
                _input.Engine?.Abort();
                _md?.SweepClearPending();
                _logs.Add("önceki cast 6 sn'de bitmedi, zorla temizlendi");
            }

            /// <summary>Bağ/hacim/tuzak: status board temizlense de sonraki casta Root/Slow ve boss çekişi taşır.</summary>
            static int MechanicLeftovers() =>
                _md != null ? _md.SweepMechanicWorldLeftoverCount() : 0;

            static void ClearMechanicWorld() => _md?.SweepClearMechanicWorld();

            static void ResetActors()
            {
                if (_bossDirector != null)
                {
                    _bossDirector.enabled = false;
                    _bossDirector.ClearReverse();
                }
                _bossStatus?.Board.Clear();
                _playerStatus?.Board.Clear();
                _ally?.Board?.Clear();
                // O7: kritik/sapma zarı her vakada aynı tohumdan — tarama deterministik kalır.
                _md?.ReseedCombatRng(CombatRng.SweepSeed);
                if (_bossVitals.IsDown || _bossVitals.Hp < _bossVitals.MaxHp * 0.6f)
                    _bossVitals.Revive();
                if (_playerVitals != null)
                {
                    _playerVitals.SuppressDown = true;
                    _playerVitals.ResetForSweepCase();
                }
                if (_ally != null)
                    _ally.SweepSetHp(Math.Max(1, _ally.MaxHp / 2));

                var resource = _player.GetComponent<PlayerResourceHost>();
                resource?.SweepRefillMana();
                var cooldown = _player.GetComponent<PlayerCooldownHost>();
                if (cooldown != null)
                    cooldown.Bind(cooldown.GlobalCooldownSec > 0f ? cooldown.GlobalCooldownSec : 0.3f, 1);

                _md.SweepSetClosingChainBonus(1f);
                _input.Dodge?.Reset();
                _player.GetComponent<PlayerDodgeController>()?.SkillIframe.Clear();
                _projectiles?.ClearAll();
                _ally?.GetComponent<ActorGroundingController>()?.SnapPlanted();
            }

            /// <summary>
            /// Koruyucu tetik (koruyucu_tetik can / kalkan) yalnız dost ya da oyuncu canı
            /// guard_threshold altına inince öder. Taramada boss kapalı, kimse vurmaz; bu yüzden
            /// planı tetik taşıyan kombolarda dost eşiğin 0.05 altında başlar. Skill kimliği yok:
            /// plan gramerden, eşik mechanic_grammar.params'tan okunur.
            /// </summary>
            static void PrepareGuardFixture(PlaySweepCase c)
            {
                if (_ally == null)
                    return;
                SkillResolution skill = _skills.Resolve(new[] { c.Verb, c.Adj });
                Dovus.Core.Mechanic.MechanicPlan plan = _md.SweepMechanicPlanFor(skill);
                if (plan == null)
                    return;
                if (!Dovus.Core.Mechanic.GuardTriggerDelivery.Owns(plan, "can")
                    && !Dovus.Core.Mechanic.GuardTriggerDelivery.Owns(plan, "kalkan"))
                    return;
                var grammar = _md.SweepMechanicEngine;
                double threshold = grammar != null ? grammar.Rules.Param("guard_threshold") : 0;
                if (threshold <= 0.05)
                    return;
                _ally.SweepSetHp(Math.Max(1, (int)Math.Floor((threshold - 0.05) * _ally.MaxHp)));
            }

            /// <summary>
            /// mermi_sil düzeneği: plan mermi siliyorsa (skill kimliği yok, gramer planından) cast anında
            /// boss tarafından dosta doğru süzülen yavaş, ZARARSIZ düzenek mermileri doğar — dostun,
            /// oyuncunun, oyuncu↔boss hattının ve oyuncu↔dost bağının üstünde. Zararsız: dostlara
            /// çarpmaz, yalnız silme/yutma/geri gönderme/perde kurallarını sınar. Sayaçlar burada sıfırlanır.
            /// </summary>
            static void PrepareProjectileFixture()
            {
                if (_projectiles == null)
                    return;
                _projectiles.ClearAll();
                _projectiles.Sim.ResetCounters();
                if (_info == null || !_info.ExpectsErase)
                    return;
                var combat = _md.SweepCombat;
                float dmg = combat != null ? combat.Boss.VolleyDamage : 6f;
                float radius = combat != null ? combat.Boss.VolleyRadiusM : 0.35f;
                const float speed = 0.3f;
                const float life = 10f;
                Vector3 b = Flat(_boss.position);
                Vector3 p = Flat(_player.position);
                void Toward(Vector3 at, Vector3 friend)
                {
                    Vector3 v = friend - at;
                    v.y = 0f;
                    v = v.sqrMagnitude > 0.0001f ? v.normalized * speed : Vector3.zero;
                    _projectiles.Spawn(at, v, radius, dmg, life, targetId: 1, harmless: true);
                }
                Vector3 pb = (b - p).sqrMagnitude > 0.0001f ? (b - p).normalized : Vector3.forward;
                Toward(p + pb * 1.2f, p);
                Toward(Vector3.Lerp(p, b, 0.5f), p);
                Toward(Vector3.Lerp(p, b, 0.8f), p);
                if (_ally != null)
                {
                    Vector3 a = Flat(_ally.transform.position);
                    Vector3 ab = (b - a).sqrMagnitude > 0.0001f ? (b - a).normalized : Vector3.forward;
                    Vector3 side = new Vector3(-ab.z, 0f, ab.x);
                    Toward(a + ab * 0.6f, a);
                    Toward(a + side * 0.6f, a);
                    Toward(Vector3.Lerp(p, a, 0.5f), a);
                }
                _projectiles.Sim.ResetCounters();
            }

            /// <summary>
            /// Emici çekmeleri boss'u her vakada oyuncuya taşır; toplanan kayma dostu (sabit) uzakta bırakır.
            /// Her vaka boss'un tarama başındaki yerinden başlar.
            /// </summary>
            static void ResetBossPosition()
            {
                if (_bossReactor == null || BossPulling())
                    return;
                if (Flat(_boss.position - _bossStart).magnitude >= 0.05f)
                {
                    _bossReactor.Home = _bossStart;
                    _boss.position = new Vector3(_bossStart.x, _boss.position.y, _bossStart.z);
                }
                _boss.GetComponent<ActorGroundingController>()?.SnapPlanted();
                Physics.SyncTransforms();
            }

            /// <summary>Portal, sınır ve takım bir sonraki vakaya taşmasın. Dost başlangıç yerine döner.</summary>
            static void ResetSweepActors()
            {
                ResetTeamCase();
                if (_ally == null || !_allyStartSet)
                    return;
                if (Flat(_ally.transform.position - _allyStart).magnitude < 0.02f)
                    return;
                _ally.transform.position = new Vector3(_allyStart.x, _allyStart.y, _allyStart.z);
                Physics.SyncTransforms();
            }

            static void PlacePlayer(float dist)
            {
                Vector3 b = _boss.position;
                _player.position = new Vector3(b.x, _player.position.y, b.z - dist);
                _player.GetComponent<ActorGroundingController>()?.SnapPlanted();
                _player.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                Physics.SyncTransforms();
                var targeting = _md.SweepTargeting;
                var target = _boss.GetComponentInChildren<TargetableHost>();
                if (targeting != null && target != null && targeting.Selected != target)
                    targeting.SweepSelect(target);
            }

            static string RejectReason(PlaySweepCase c)
            {
                if (_input.SweepInputLocked)
                    return "girdi kilitli (düşük can ya da BlocksCast)";
                RuneLoadout loadout = _input.Engine?.Loadout;
                bool hasVerb = false, hasAdj = false;
                for (int slot = 1; loadout != null && slot <= RuneLoadout.SlotCount; slot++)
                {
                    hasVerb |= loadout.RuneIdAtSlot(slot) == c.Verb;
                    hasAdj |= loadout.RuneIdAtSlot(slot) == c.Adj;
                }
                if (!hasVerb || !hasAdj)
                    return "rün build'de yok";
                var gate = _input.SweepSkillTargetGate;
                if (gate != null && !gate(_skills.Resolve(new[] { c.Verb, c.Adj })))
                {
                    string why = _logs.LastOrDefault(l => !l.StartsWith("SWEEP"));
                    return "hedef/menzil kapısı" + (string.IsNullOrEmpty(why) ? "" : " — " + why);
                }
                return "bilinmiyor";
            }

            static CaseInfo Describe(PlaySweepCase c)
            {
                var info = new CaseInfo();
                info.Adj = c.Adj;
                SkillResolution skill = _skills.Resolve(new[] { c.Verb, c.Adj });
                info.Name = skill.Identity.DisplayName;
                info.DamageSkill = skill.Combat.BaseDamage > 0f || skill.Combat.BaseHeal > 0f;
                if (_md.SweepMechanicPlanFor(skill) is Dovus.Core.Mechanic.MechanicPlan keys)
                {
                    info.ExpectsReverse = keys.Effects.Any(e => e.Target == "dusman" && e.Has("ters_kontrol"));
                    info.ExpectsDecoyAggro = keys.Effects.Any(e => e.Has("dikkat_ceker"));
                    Dovus.Core.Mechanic.MechanicEffect erase = keys.Effects.FirstOrDefault(e => e.Stat == "mermi_sil");
                    info.ExpectsErase = erase != null;
                    info.ExpectsAbsorb = erase != null && erase.Has("yut");
                    info.ExpectsReflect = erase != null && erase.Has("geri_gonder");
                    info.ExpectsLinkErase = erase != null && erase.Has("bag_hatti") && keys.Body.Link;
                }
                info.BossR = _md.SweepBossBodyRadius();
                float pr = _md.SweepPlayerBodyRadiusM();
                info.PlayerR = pr < 0.05f ? 0.5f : pr;
                var combat = _md.SweepCombat;
                info.RecoverySec = combat != null ? combat.Sentence.StepForDots(2).RecoverySec : 0.26f;

                var catalog = _md.SweepMotionCatalog;
                if (catalog == null || !catalog.TryPlay((SkillId)skill.Identity.Id, out MotionTemplate template))
                    return info;
                info.DeliveryDelaySec = DeliveryDelaySec(skill, template);
                PositionPlayback playback = _md.SweepPreparePositionPlayback(skill, template);
                info.Template = playback.Template ?? template;
                foreach (MotionPhase p in info.Template.Phases)
                {
                    info.ExpectedSec += p.DurationSec;
                    if (p.OvershootM > 0f)
                        info.PassThrough = true;
                    if (p.Hit != null)
                        info.HasHitPhase = true;
                }

                Vector3 b = _boss.position;
                info.SimStart = new Vector3(b.x, _player.position.y, b.z - c.StartDistM);
                info.StopGap = catalog.Fallbacks.StopGapM;
                Simulate(info, _boss.transform);
                return info;
            }

            /// <summary>Oyunla aynı teslim kuyruğu: işaretli an / yükseliş gecikmesi kaydı uzatır.</summary>
            static float DeliveryDelaySec(SkillResolution skill, MotionTemplate template)
            {
                if (!(_md.SweepMechanicPlanFor(skill) is Dovus.Core.Mechanic.MechanicPlan plan)
                    || !ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design)
                    || design.Mechanics == null)
                    return 0f;
                Dovus.Core.Mechanic.TemplateDeliveryOrder order = Dovus.Core.Mechanic.TemplateDelivery.Build(
                    plan, skill.Engine, template, design.Mechanics.Rules, 1f);
                return order.DelayedMark || order.RiseDelay ? Mathf.Max(0.05f, order.ActivationDelaySec) : 0f;
            }

            /// <summary>Kalıbı çevrimdışı koşturur. aim: oyunun kalıba verdiği hedef (boss, dost ya da yok).</summary>
            static void Simulate(CaseInfo info, Transform aim)
            {
                Vector3 s = info.SimStart;
                Vector3 b = _boss.position;
                MotionTarget target = default;
                if (aim != null)
                {
                    float r = aim == _boss.transform ? info.BossR : _md.SweepColliderRadius(aim);
                    bool hold = aim == _boss.transform
                        && EmiciApproach.ShouldHoldCaster(
                            info.Adj.ToString(CultureInfo.InvariantCulture), info.Template);
                    target = new MotionTarget(true, aim.position.x, aim.position.z, r, hold);
                }
                var runner = new MotionTemplateRunner();
                runner.Begin(info.Template, s.x, s.y, s.z, 0f, 1f, info.PlayerR, info.StopGap);
                float maxExc = 0f;
                for (int i = 0; i < 1200 && !runner.Finished; i++)
                {
                    runner.Tick(1f / 60f, target, new MotionStick(false, 0f, 0f));
                    maxExc = Mathf.Max(maxExc, Flat(new Vector3(runner.X, 0f, runner.Z) - s).magnitude);
                }
                info.SimFinal = new Vector3(runner.X, s.y, runner.Z);
                info.HasSim = true;
                info.SimAim = aim == null ? "yok" : aim == _boss.transform ? "boss" : aim.name;
                string stay = EmiciApproach.SweepStayCategory(
                    info.Adj.ToString(CultureInfo.InvariantCulture), info.Template);
                info.ExpectedCat = stay ?? DesignCategory(info.Template) ?? Category(s, info.SimFinal, b, maxExc);
            }

            static string DesignCategory(MotionTemplate t)
            {
                bool behind = false;
                bool back = false;
                foreach (MotionPhase p in t.Phases)
                {
                    if (p.Motion == "blink" || p.Land == "behind" || p.OvershootM > 0f)
                        behind = true;
                    if (p.Motion == "return")
                        back = true;
                }
                if (back)
                    return "başlangıç";
                return behind ? "arka" : null;
            }

            static string Category(Vector3 start, Vector3 final, Vector3 boss, float maxExcursion)
            {
                float moved = Flat(final - start).magnitude;
                if (maxExcursion > 0.6f && moved < 0.5f)
                    return "başlangıç";
                if (moved < InPlaceM)
                    return "yerinde";
                Vector3 toStart = Flat(start - boss).normalized;
                Vector3 rel = Flat(final - boss);
                float along = Vector3.Dot(rel, toStart);
                float lateral = Mathf.Abs(toStart.x * rel.z - toStart.z * rel.x);
                if (along < -0.2f)
                    return "arka";
                float startDist = Flat(start - boss).magnitude;
                if (rel.magnitude > startDist + 0.3f)
                    return "geri";
                if (lateral > 0.6f && lateral > Mathf.Abs(startDist - along))
                    return "yan";
                return "ön";
            }

    }

}
#endif
