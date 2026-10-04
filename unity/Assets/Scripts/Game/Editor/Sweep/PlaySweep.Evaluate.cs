#if UNITY_EDITOR
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
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
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

namespace Dovus.Game.Editor
{
    public static partial class PlaySweep
    {
            // ---------------------------------------------------------------- değerlendirme

            static PlaySweepResult NewResult(PlaySweepCase c)
            {
                var r = new PlaySweepResult { Case = c, Weapon = c.Weapon };
                try
                {
                    r.Name = _skills.Resolve(new[] { c.Verb, c.Adj }).DisplayName;
                }
                catch
                {
                    r.Name = "";
                }
                return r;
            }

            static PlaySweepResult Evaluate(PlaySweepCase c, bool timedOut)
            {
                PlaySweepResult r = NewResult(c);
                r.Cast = true;
                r.Name = _info.Name;
                r.Template = _info.Template != null ? _info.Template.Id : "";
                r.Weapon = _md.EquippedWeapon != null ? _md.EquippedWeapon.Name : c.Weapon;
                r.ExpectedSec = _info.ExpectedSec;
                r.Contact = _info.BossR + _info.PlayerR;
                if (_frames.Count == 0)
                {
                    r.Notes.Add("kare yok");
                    return r;
                }

                Frame last = _frames[_frames.Count - 1];
                int i0 = _frames.FindIndex(x => x.Playing);
                int i1 = _frames.FindLastIndex(x => x.Playing);
                if (timedOut)
                    r.Notes.Add($"zaman aşımı {last.T:F1} sn");

                // 1) isabet ya da amaçlanan etki
                r.Damage = _pre.BossHp - _frames.Min(x => x.BossHp);
                var effects = new List<string>();
                var preBoss = new HashSet<string>();
                var bossKinds = new HashSet<string>();
                var playerKinds = new HashSet<string>();
                foreach (Frame f in _frames)
                {
                    foreach (string k in (f.BossKinds ?? "").Split('+'))
                        if (k.Length > 0) bossKinds.Add(k);
                    foreach (string k in (f.PlayerKinds ?? "").Split('+'))
                        if (k.Length > 0) playerKinds.Add(k);
                }
                if (bossKinds.Count > 0)
                    effects.Add("boss:" + string.Join("+", bossKinds));
                if (playerKinds.Count > 0)
                    effects.Add("oyuncu:" + string.Join("+", playerKinds));
                float bossSlow = _frames.Min(x => Mathf.Min(x.BossMove, x.BossAction));
                if (bossSlow < 0.999f)
                    effects.Add($"boss hız x{bossSlow:F2}");
                float haste = _frames.Max(x => Mathf.Max(x.PlayerMove, x.PlayerAction));
                if (haste > 1.001f)
                    effects.Add($"oyuncu hız x{haste:F2}");
                float shield = _frames.Max(x => x.Shield);
                if (shield > 0.01f)
                    effects.Add($"kalkan {shield:F0}");
                int heal = _frames.Max(x => x.PlayerHp) - _pre.PlayerHp;
                if (heal > 0)
                    effects.Add($"oyuncu +{heal} can");
                int allyHeal = _frames.Max(x => x.AllyHp) - _pre.AllyHp;
                if (allyHeal > 0)
                    effects.Add($"dost +{allyHeal} can");
                if (_info.ExpectsReverse)
                {
                    if (_frames.Any(x => x.BossReversed))
                        effects.Add("boss ters kontrol");
                    else
                    {
                        r.Effect = false;
                        r.Notes.Add("ters_kontrol: boss ters kontrole girmedi");
                    }
                }
                if (_info.ExpectsDecoyAggro)
                {
                    if (_frames.Any(x => x.DecoyAggro))
                        effects.Add("yem boss aggro");
                    else
                    {
                        r.Effect = false;
                        r.Notes.Add("dikkat_ceker: yem boss hedefi olmadı");
                    }
                }
                if (_info.ExpectsErase)
                    EvaluateProjectiles(r, effects, heal);
                int roots = _player.gameObject.scene.rootCount - _pre.RootCount;
                r.Effects = string.Join(", ", effects);
                r.Hit = r.Damage > 0.01f || effects.Count > 0;
                if (!r.Hit && !_info.DamageSkill && (!_info.HasHitPhase || _hitOrigins.Count > 0))
                {
                    r.Hit = true;
                    r.Notes.Add("hasarsız skill (base_damage 0): etki hareketin kendisi");
                }
                else if (!r.Hit)
                {
                    r.Notes.Add("hasar yok, etki görülmedi" + (roots > 0 ? $" (+{roots} sahne nesnesi)" : ""));
                }
                if (_hitOrigins.Count > 0)
                {
                    var parts = new List<string>();
                    for (int i = 0; i < _hitOrigins.Count && i < 6; i++)
                    {
                        Frame at = FrameAt(_hitTimes[i]);
                        parts.Add($"{_hitTimes[i]:F2}s@boss{Flat(_hitOrigins[i] - at.B).magnitude:F2}m/oyuncu{Flat(_hitOrigins[i] - at.P).magnitude:F2}m");
                    }
                    r.Notes.Add($"vuruş {_hitOrigins.Count}: " + string.Join(" ", parts));
                }

                // 2) konum — kalıp bittiği karede, boss'un o anki yerine göre.
                // İşaretli ışın (9-10 yer değiştirme) tek karede uzun bir adımdır; konum
                // hesabı o adımı ve sonrasını ışınsız yere indirir. Sıçrama kuralı ayrı.
                Vector3 start = _pre.P;
                int endIdx = i1 >= 0 && i1 + 1 < _frames.Count ? i1 + 1 : _frames.Count - 1;
                Frame end = _frames[endIdx];
                float offX = 0f;
                float offZ = 0f;
                float maxExc = 0f;
                Vector3 scored = start;
                int lastFrame = endIdx < _frames.Count ? endIdx : _frames.Count - 1;
                for (int i = 0; i <= lastFrame; i++)
                {
                    Vector3 p = _frames[i].P;
                    if (i > 0 && _frames[i].Teleport)
                    {
                        Vector3 step = Flat(p - _frames[i - 1].P);
                        SweepJumpRule.NoteTeleport(ref offX, ref offZ, step.x, step.z, true);
                    }
                    p.x -= offX;
                    p.z -= offZ;
                    maxExc = Mathf.Max(maxExc, Flat(p - start).magnitude);
                    scored = p;
                }
                r.ActualPos = Category(start, scored, end.B, maxExc);
                r.ExpectedPos = string.IsNullOrEmpty(_info.ExpectedCat) ? "?" : _info.ExpectedCat;
                float simErr = _info.HasSim ? Flat(scored - _info.SimFinal).magnitude : 0f;
                float beam = Mathf.Sqrt(offX * offX + offZ * offZ);
                bool steered = c.Stick.sqrMagnitude > 0.0001f;
                bool bossMoved = Flat(end.B - _pre.B).magnitude > 0.1f;
                r.Position = steered
                             || (r.ActualPos == r.ExpectedPos && (!_info.HasSim || simErr <= SimMatchM || bossMoved));
                r.Notes.Add($"kalıp sonu: merkeze {Flat(scored - end.B).magnitude:F2} m, başlangıçtan {Flat(scored - start).magnitude:F2} m, " +
                            $"kalıp simülasyonundan {simErr:F2} m" + (bossMoved ? $", boss {Flat(end.B - _pre.B).magnitude:F2} m kaydı" : "") +
                            (steered ? ", çubukla yönlendirildi" : "") +
                            (beam > 0.05f ? $", ışın {beam:F2} m konumdan çıkarıldı" : "") +
                            (_info.SimAim != "boss" ? $", kalıp hedefi {_info.SimAim}" : ""));

                // 3) boss gövdesine girmedi
                r.MinDist = float.MaxValue;
                float minRest = float.MaxValue;
                float minAt = 0f;
                for (int i = 0; i < _frames.Count; i++)
                {
                    Frame f = _frames[i];
                    float d = Flat(f.P - f.B).magnitude;
                    if (d < r.MinDist)
                    {
                        r.MinDist = d;
                        minAt = f.T;
                    }
                    float v = i > 0 ? Flat(f.P - _frames[i - 1].P).magnitude / Mathf.Max(0.001f, f.T - _frames[i - 1].T) : 0f;
                    if (v < 1f)
                        minRest = Mathf.Min(minRest, d);
                }
                float limit = r.Contact - InsideTolM;
                r.NotInside = _info.PassThrough ? minRest >= limit : r.MinDist >= limit;
                if (r.MinDist < limit)
                    r.Notes.Add($"gövdeye girdi: merkeze {r.MinDist:F2} m (t={minAt:F2}, temas {r.Contact:F2})" + (_info.PassThrough ? " geçiş tasarım gereği" : ""));

                // 4) tek sistem
                float templateMove = 0f;
                float otherMove = 0f;
                int dodgeFrames = 0;
                for (int i = 1; i < _frames.Count; i++)
                {
                    Frame a = _frames[i - 1];
                    Frame b = _frames[i];
                    Vector3 actual = Flat(b.P - a.P);
                    Vector3 tpl = Vector3.zero;
                    if (b.Playing && !a.Playing)
                        tpl = Flat(b.Runner - a.P);
                    else if (a.Playing)
                        tpl = Flat(b.Runner - a.Runner);
                    templateMove += tpl.magnitude;
                    if (!b.Teleport)
                        otherMove += (actual - tpl).magnitude;
                    if (b.Dodge) dodgeFrames++;
                }
                int systems = (templateMove > TemplateMoveM ? 1 : 0) + (otherMove > OtherMoveM ? 1 : 0)
                              + (dodgeFrames > 0 ? 1 : 0);
                r.OneSystem = systems <= 1;
                if (!r.OneSystem || otherMove > OtherMoveM)
                    r.Notes.Add($"yer değiştiren: kalıp {templateMove:F2} m, başka {otherMove:F2} m, dodge {dodgeFrames} kare");
                // 7) ışınlanma / titreme yok (blink fazı tasarım gereği sıçrar)
                int teleports = 0;
                int reversals = 0;
                float maxJump = 0f;
                float jumpAt = 0f;
                float jumpLimit = 0f;
                float jumpDt = 0f;
                float worstRatio = 0f;
                for (int i = 1; i < _frames.Count; i++)
                {
                    Vector3 step = Flat(_frames[i].P - _frames[i - 1].P);
                    float stepLimit = JumpLimit(_frames[i].Dt);
                    if (step.magnitude / stepLimit > worstRatio)
                    {
                        worstRatio = step.magnitude / stepLimit;
                        maxJump = step.magnitude;
                        jumpAt = _frames[i].T;
                        jumpLimit = stepLimit;
                        jumpDt = _frames[i].Dt;
                    }
                    if (SweepJumpRule.IsIllegalJump(step.magnitude, stepLimit, IsBlinkPhase(_frames[i].Phase), _frames[i].Teleport))
                        teleports++;
                    if (i >= 2)
                    {
                        Vector3 prev = Flat(_frames[i - 1].P - _frames[i - 2].P);
                        if (step.magnitude > 0.2f && prev.magnitude > 0.2f && Vector3.Dot(step.normalized, prev.normalized) < -0.5f)
                            reversals++;
                    }
                }
                r.NoTeleport = teleports == 0 && reversals < 3;
                Frame jumpFrame = FrameAt(jumpAt);
                string phaseAtJump = jumpFrame.Phase;
                string designed = jumpFrame.Teleport ? ", ışın tasarım gereği" : IsBlinkPhase(phaseAtJump) ? ", blink tasarım gereği" : "";
                if (maxJump > jumpLimit)
                    r.Notes.Add($"tek karede {maxJump:F2} m sıçrama (sınır {jumpLimit:F2} m @ {jumpDt * 1000f:F0} ms, t={jumpAt:F2}{(string.IsNullOrEmpty(phaseAtJump) ? "" : ", faz " + phaseAtJump)}" +
                                designed + ")");
                if (reversals >= 3)
                    r.Notes.Add($"titreme: {reversals} karede ≥0,2 m ileri-geri");
                float bossJump = 0f;
                float bossJumpAt = 0f;
                float bossLimit = 0f;
                float bossRatio = 0f;
                for (int i = 1; i < _frames.Count; i++)
                {
                    float step = Flat(_frames[i].B - _frames[i - 1].B).magnitude;
                    float stepLimit = JumpLimit(_frames[i].Dt);
                    if (step / stepLimit > bossRatio)
                    {
                        bossRatio = step / stepLimit;
                        bossJump = step;
                        bossJumpAt = _frames[i].T;
                        bossLimit = stepLimit;
                    }
                }
                if (bossJump > bossLimit)
                {
                    r.NoTeleport = false;
                    r.Notes.Add($"boss tek karede {bossJump:F2} m sıçradı (sınır {bossLimit:F2} m, t={bossJumpAt:F2})");
                }

                // 8) ayaklar yerde — havada olmayan kareler ve skill bitiminden 0,3 sn sonra
                EvaluateGround(r);

                // 5) hata yok
                var errors = _logs.Where(l => l.StartsWith("ERROR") || l.StartsWith("SWEEP-EXC")).ToList();
                r.NoErrors = errors.Count == 0;
                if (errors.Count > 0)
                    r.Notes.Add("hata: " + string.Join(" | ", errors.Take(2)));
                var warns = _logs.Where(l => l.StartsWith("WARN")).Distinct().Take(2).ToList();
                if (warns.Count > 0)
                    r.Notes.Add("uyarı: " + string.Join(" | ", warns.Select(w => w.Substring(5))));

                // 6) süre
                if (i0 < 0)
                {
                    r.OnTime = false;
                    r.Notes.Add("kalıp hiç oynamadı");
                }
                else
                {
                    float tStart = _frames[i0].T;
                    float tEnd = i1 + 1 < _frames.Count ? _frames[i1 + 1].T : _frames[i1].T;
                    r.TemplateSec = tEnd - tStart;
                    int bangIdx = _frames.FindIndex(x => x.Bang);
                    float bang = _scheduledBang >= 0f ? _scheduledBang : bangIdx >= 0 ? _frames[bangIdx].T : tStart;
                    float idleAt = last.T;
                    for (int i = Math.Max(i1, 0); i < _frames.Count; i++)
                    {
                        if (!_frames[i].Busy)
                        {
                            idleAt = _frames[i].T;
                            break;
                        }
                    }
                    r.TotalSec = idleAt;
                    float totalLimit = bang + _info.ExpectedSec + TotalTolSec;
                    bool startOk = tStart - bang <= StartTolSec;
                    bool templateOk = r.TemplateSec <= _info.ExpectedSec + TemplateTolSec;
                    bool totalOk = r.TotalSec <= totalLimit;
                    r.OnTime = startOk && templateOk && totalOk && !timedOut;
                    if (bang > _info.RecoverySec + StartTolSec)
                        r.Notes.Add($"toparlanma {bang:F2} sn (tablo {_info.RecoverySec:F2})");
                    if (!startOk)
                        r.Notes.Add($"kalıp bang'den {tStart - bang:F2} sn sonra başladı");
                    if (!templateOk)
                        r.Notes.Add($"kalıp {r.TemplateSec:F2} sn (beklenen {_info.ExpectedSec:F2})");
                    if (!totalOk)
                        r.Notes.Add($"cast {r.TotalSec:F2} sn'de bitti (beklenen ≤ {totalLimit:F2})");
                }

                r.Legs = LegSummary();
                return r;
            }

    }
}
#endif
