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
            static float TemplateEndedFor()
            {
                int last = -1;
                for (int i = 0; i < _frames.Count; i++)
                {
                    if (_frames[i].Playing)
                        last = i;
                }
                if (last < 0)
                    return _frames.Count > 0 ? _frames[_frames.Count - 1].T : 0f;
                return _frames[_frames.Count - 1].T - _frames[last].T;
            }

            /// <summary>
            /// mermi_sil etkisi: düzenek mermisinden en az biri silinmeli / yutulmalı / geri dönmeli / perdeye
            /// girmeli. yut: yutma + oyuncu canı arttı. geri_gonder: geri dönüş + boss canı düştü.
            /// bag_hatti: bağ şeridi sildi. Canlı mermi hiçbir karede tavanı (40) aşmamalı.
            /// </summary>
            static void EvaluateProjectiles(PlaySweepResult r, List<string> effects, int heal)
            {
                if (_projectiles == null)
                {
                    r.Effect = false;
                    r.Notes.Add("mermi_sil: mermi sahibi yok");
                    return;
                }
                HostileProjectiles sim = _projectiles.Sim;
                int erased = sim.ErasedTotal, absorbed = sim.AbsorbedTotal, reflected = sim.ReflectedTotal;
                int shrouded = sim.ShroudedTotal, linked = sim.LinkErasedTotal;
                int any = erased + absorbed + reflected + shrouded + linked;
                effects.Add($"mermi sil {erased} yut {absorbed} dön {reflected} perde {shrouded} bağ {linked}");
                if (any < 1)
                {
                    r.Effect = false;
                    r.Notes.Add("mermi_sil: düzenek mermisi silinmedi");
                }
                if (_info.ExpectsAbsorb && (absorbed < 1 || heal <= 0))
                {
                    r.Effect = false;
                    r.Notes.Add($"yut: yutulan {absorbed}, oyuncu canı +{heal}");
                }
                if (_info.ExpectsReflect && (reflected < 1 || r.Damage <= 0.01f))
                {
                    r.Effect = false;
                    r.Notes.Add($"geri_gonder: dönen {reflected}, boss hasarı {r.Damage:F1}");
                }
                if (_info.ExpectsLinkErase && linked < 1)
                {
                    r.Effect = false;
                    r.Notes.Add("bag_hatti: bağ şeridi mermi silmedi");
                }
                int peak = Math.Max(sim.PeakAlive, _frames.Count > 0 ? _frames.Max(x => x.ProjectilesAlive) : 0);
                if (peak > HostileProjectiles.DefaultMaxAlive)
                {
                    r.Effect = false;
                    r.Notes.Add($"mermi tavanı aşıldı: {peak}");
                }
            }

            static void EvaluateGround(PlaySweepResult r)
            {
                float live = 0f;
                float breach = 0f;
                float breachAt = 0f;
                float breachDt = SweepPace.ReferenceFrameSec;
                float breachLimit = SweepPace.GroundSlack(SweepPace.ReferenceFrameSec);
                string breachPhase = "";
                float worstRatio = 0f;
                bool liveOk = true;
                int lastPlay = -1;
                for (int i = 0; i < _frames.Count; i++)
                {
                    Frame f = _frames[i];
                    // Biten kalıbın ertelenmiş son karesi (tarama sıçrama ölçüsü için) iniş sayılır.
                    if (!f.Playing || f.RunnerDone)
                        continue;
                    lastPlay = i;
                    if (f.Airborne)
                        continue;
                    float err = Mathf.Abs(f.Feet - f.FootGround);
                    if (err > live)
                        live = err;
                    float limit = SweepPace.GroundSlack(f.Dt);
                    float ratio = limit > 0.0001f ? err / limit : err;
                    if (ratio > worstRatio)
                    {
                        worstRatio = ratio;
                        breach = err;
                        breachAt = f.T;
                        breachDt = f.Dt;
                        breachLimit = limit;
                        breachPhase = f.Phase;
                    }
                    if (err > limit + 0.0001f)
                        liveOk = false;
                }

                r.FootLiveM = live;
                float endT = lastPlay >= 0 ? _frames[lastPlay].T : 0f;
                float want = endT + Grounding.SettleAfterSec;
                Frame settle = _frames[_frames.Count - 1];
                bool haveSettle = false;
                for (int i = 0; i < _frames.Count; i++)
                {
                    if (_frames[i].T + 0.0001f >= want)
                    {
                        settle = _frames[i];
                        haveSettle = true;
                        break;
                    }
                }

                float settleErr = Mathf.Abs(settle.Feet - settle.FootGround);
                r.FootSettleM = settleErr;
                // Bitiş ölçüsü nokta örneği; hızla gevşemez. Canlı eşik kare süresiyle ölçeklenir.
                bool settleOk = haveSettle && settleErr <= Grounding.SettleSlackM + 0.0001f;
                r.Grounded = liveOk && settleOk;
                if (!liveOk)
                    r.Notes.Add($"ayak yerden {breach:F2} m (havada değil, sınır {breachLimit:F2} m @ {breachDt * 1000f:F0} ms, t={breachAt:F2}" +
                                (string.IsNullOrEmpty(breachPhase) ? "" : ", faz " + breachPhase) + ")");
                if (!haveSettle)
                    r.Notes.Add("ayak inişi: skill bitiminden 0,30 sn ölçülemedi");
                else if (!settleOk)
                    r.Notes.Add($"skill bitiminden {Grounding.SettleAfterSec:F2} sn sonra ayak {settleErr:F2} m (sınır {Grounding.SettleSlackM:F2} m)");

                float bossErr = Mathf.Abs(settle.BossFeet - settle.BossFootGround);
                float allyErr = Mathf.Abs(settle.AllyFeet - settle.AllyFootGround);
                if (bossErr > Grounding.LiveSlackM || allyErr > Grounding.LiveSlackM)
                {
                    r.Grounded = false;
                    r.Notes.Add($"zemin dışı: boss {bossErr:F2} m, dost {allyErr:F2} m");
                }
            }

            static float JumpLimit(float dt) => SweepPace.JumpLimit(dt);

            static bool IsBlinkPhase(string phase)
            {
                if (string.IsNullOrEmpty(phase) || _info?.Template == null)
                    return false;
                foreach (MotionPhase p in _info.Template.Phases)
                {
                    if (p.Name == phase)
                        return p.Motion == "blink";
                }
                return false;
            }

            static Frame FrameAt(float t)
            {
                Frame best = _frames[0];
                foreach (Frame f in _frames)
                {
                    if (f.T > t)
                        break;
                    best = f;
                }
                return best;
            }

            /// <summary>Oyuncu ≥1,5 m/s giderken bacak klibinin oynadığı kare oranı.</summary>
            static string LegSummary()
            {
                int moving = 0;
                int legs = 0;
                float ratio = 0f;
                for (int i = 1; i < _frames.Count; i++)
                {
                    Frame a = _frames[i - 1];
                    Frame b = _frames[i];
                    if (!b.Playing)
                        continue;
                    float dt = Mathf.Max(0.001f, b.T - a.T);
                    float v = Flat(b.P - a.P).magnitude / dt;
                    if (v < LegMoveMps)
                        continue;
                    moving++;
                    bool loco = b.Base == "Locomotion" && b.Speed > 0.05f;
                    if (loco)
                    {
                        legs++;
                        float scale = _animator != null ? _animator.transform.lossyScale.y : 1f;
                        ratio += b.Speed * Mathf.Abs(b.Playback) * scale / v;
                    }
                }
                if (moving == 0)
                    return "";
                return legs > 0
                    ? $"{legs}/{moving} kare koşu, ayak/gövde hız oranı {ratio / legs:F2}"
                    : $"0/{moving} kare koşu";
            }

    }
}
#endif
