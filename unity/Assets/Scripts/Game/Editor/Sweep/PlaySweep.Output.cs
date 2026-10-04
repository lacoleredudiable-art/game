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
            // ---------------------------------------------------------------- ????kt??

            /// <summary>Bo??sa docs/play-sweep. Ba??s??z ko??ucu (tools/SweepV2) kendi klas??r??n?? verir.</summary>
            public static string OutputDir { get; set; } = "";

            static string OutDir()
            {
                string dir = !string.IsNullOrEmpty(OutputDir)
                    ? Path.GetFullPath(OutputDir)
                    : Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "docs", "play-sweep"));
                Directory.CreateDirectory(dir);
                return dir;
            }

            static void WriteOutputs()
            {
                int pass = Results.Count(r => r.Pass);
                var byWeapon = Results.GroupBy(r => r.Case.Weapon)
                    .Select(g => $"{g.Key}: {g.Count(r => r.Pass)}/{g.Count()}");
                LastSummary = $"{pass}/{Results.Count} ge??ti @ {_speed:0.#}x ({string.Join(", ", byWeapon)})";

                var csv = new StringBuilder();
                csv.AppendLine("kombo,isim,silah,kalip,cast,isabet,konum,govdeye_girmedi,tek_sistem,hata_yok,sure,sicrama_yok,yerde,etki_kontrol,gecti,"
                               + "beklenen_konum,gercek_konum,hasar,etki,min_merkez_m,temas_m,kalip_sn,beklenen_sn,toplam_sn,ayak_m,inis_m,bacak,notlar");
                foreach (PlaySweepResult r in Results)
                {
                    csv.AppendLine(string.Join(",", new[]
                    {
                        r.Case.Id, SweepCsvFormat.Quote(r.Name), SweepCsvFormat.Quote(r.Weapon), SweepCsvFormat.Quote(r.Template), SweepCsvFormat.Bit(r.Cast), SweepCsvFormat.Bit(r.Hit), SweepCsvFormat.Bit(r.Position),
                        SweepCsvFormat.Bit(r.NotInside), SweepCsvFormat.Bit(r.OneSystem), SweepCsvFormat.Bit(r.NoErrors), SweepCsvFormat.Bit(r.OnTime), SweepCsvFormat.Bit(r.NoTeleport), SweepCsvFormat.Bit(r.Grounded), SweepCsvFormat.Bit(r.Effect), SweepCsvFormat.Bit(r.Pass),
                        SweepCsvFormat.Quote(r.ExpectedPos), SweepCsvFormat.Quote(r.ActualPos), SweepCsvFormat.Number(r.Damage), SweepCsvFormat.Quote(r.Effects),
                        SweepCsvFormat.Number(r.MinDist == float.MaxValue ? 0f : r.MinDist), SweepCsvFormat.Number(r.Contact), SweepCsvFormat.Number(r.TemplateSec),
                        SweepCsvFormat.Number(r.ExpectedSec), SweepCsvFormat.Number(r.TotalSec), SweepCsvFormat.Number(r.FootLiveM), SweepCsvFormat.Number(r.FootSettleM), SweepCsvFormat.Quote(r.Legs), SweepCsvFormat.Quote(string.Join("; ", r.Notes)),
                    }));
                }

                string dir = OutDir();
                string safe = _label.Replace('+', '-');
                File.WriteAllText(Path.Combine(dir, safe + ".csv"), csv.ToString(), new UTF8Encoding(false));

                var detail = new StringBuilder();
                detail.AppendLine("# Play taramas?? " + _label + " @ " + _speed.ToString("0.#", CultureInfo.InvariantCulture)
                                   + "x ??? " + LastSummary);
                detail.AppendLine(WorstFeet());
                foreach (PlaySweepResult r in Results.Where(x => !x.Pass))
                {
                    var failed = new List<string>();
                    if (!r.Cast) failed.Add("cast");
                    if (r.Cast && !r.Hit) failed.Add("isabet");
                    if (r.Cast && !r.Position) failed.Add($"konum({r.ExpectedPos}???{r.ActualPos})");
                    if (r.Cast && !r.NotInside) failed.Add("g??vde");
                    if (r.Cast && !r.OneSystem) failed.Add("tek-sistem");
                    if (r.Cast && !r.NoErrors) failed.Add("hata");
                    if (r.Cast && !r.OnTime) failed.Add("s??re");
                    if (r.Cast && !r.NoTeleport) failed.Add("s????rama");
                    if (r.Cast && !r.Grounded) failed.Add("yerde");
                    if (r.Cast && !r.Effect) failed.Add("etki");
                    detail.AppendLine($"{r.Case.Id} {r.Name} [{r.Weapon}] KALDI: {string.Join(", ", failed)} ??? {string.Join("; ", r.Notes)}");
                }
                detail.AppendLine();
                detail.Append(_detail);
                File.WriteAllText(Path.Combine(dir, safe + "-detay.txt"), detail.ToString(), new UTF8Encoding(false));
            }

            static void WriteTrace(PlaySweepCase c, PlaySweepResult r)
            {
                _detail.AppendLine($"=== {c.Label} {c.Id} {r.Name} [{r.Weapon}] ba??lang???? {c.StartDistM:F1} m, kal??p {r.Template}, " +
                                   $"boss r={_info.BossR:F2}, oyuncu r={_info.PlayerR:F2}, temas {r.Contact:F2}" +
                                   (Mathf.Abs(c.BossShiftX) > 0.001f ? $", boss kaymas?? {c.BossShiftX:F1} m" : "") +
                                   (Mathf.Abs(c.PlayerShiftM) > 0.001f ? $", oyuncu kaymas?? {c.PlayerShiftM:F1} m @{c.PlayerShiftAtSec:F2} sn" : ""));
                _detail.AppendLine($"  sonu??: {(r.Pass ? "GE??T??" : "KALDI")} isabet={r.Hit} konum={r.ExpectedPos}???{r.ActualPos} g??vde={r.NotInside} " +
                                   $"tek={r.OneSystem} hata={r.NoErrors} s??re={r.OnTime} s????rama_yok={r.NoTeleport} yerde={r.Grounded} " +
                                   $"ayak={r.FootLiveM:F2}/{r.FootSettleM:F2} hasar={r.Damage:F1} etki=[{r.Effects}] bacak=[{r.Legs}]");
                foreach (string n in r.Notes)
                    _detail.AppendLine("  not: " + n);
                foreach (string l in _logs.Distinct().Take(12))
                    _detail.AppendLine("  log: " + l);
                Vector3 start = _pre.P;
                Vector3 back = Flat(start - _pre.B).normalized;
                _detail.AppendLine("  t | faz | merkez_m | taraf(+??n/-arka) | yan_m | h??z_mps | yaw | taban_state(norm) | ??st_state | Speed | Playback | bossHP | oyuncuHP");
                for (int i = 0; i < _frames.Count; i++)
                {
                    Frame f = _frames[i];
                    Vector3 rel = Flat(f.P - f.B);
                    float v = i > 0 ? Flat(f.P - _frames[i - 1].P).magnitude / Mathf.Max(0.001f, f.T - _frames[i - 1].T) : 0f;
                    float side = rel.magnitude > 0.001f ? Vector3.Dot(rel.normalized, back) : 0f;
                    float lateral = back.x * rel.z - back.z * rel.x;
                    _detail.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "  {0:F3} | {1} | {2:F2} | {3:F2} | {4:F2} | {5:F1} | {6:F0} | {7}({8:F2}) | {9} | {10:F2} | {11:F2} | {12:F1} | {13}{14}",
                        f.T, f.Playing ? f.Phase : "-", rel.magnitude, side, lateral, v, f.Yaw, f.Base, f.BaseNorm,
                        f.Upper, f.Speed, f.Playback, f.BossHp, f.PlayerHp,
                        HitMark(i)));
                }
                _detail.AppendLine();
            }

            static string HitMark(int i)
            {
                float t0 = _frames[i].T;
                float tPrev = i > 0 ? _frames[i - 1].T : -1f;
                var marks = new List<string>();
                for (int k = 0; k < _hitTimes.Count; k++)
                {
                    if (_hitTimes[k] > tPrev && _hitTimes[k] <= t0)
                        marks.Add($"VURU??@boss{Flat(_hitOrigins[k] - _frames[i].B).magnitude:F2}m");
                }
                return marks.Count > 0 ? " | " + string.Join(" ", marks) : "";
            }

            static string WorstFeet()
            {
                if (Results.Count == 0)
                    return "Yere basma: ??l????m yok";
                var ranked = Results
                    .Select(r => (r, err: Mathf.Max(r.FootLiveM, r.FootSettleM)))
                    .OrderByDescending(x => x.err)
                    .Take(8)
                    .ToList();
                string list = string.Join(", ", ranked.Select(x =>
                    $"{x.r.Case.Id} canl?? {x.r.FootLiveM:F2} m / ini?? {x.r.FootSettleM:F2} m"));
                return "Yere basma en k??t?? (havada de??il ??? "
                       + Grounding.LiveSlackM.ToString("F2", CultureInfo.InvariantCulture)
                       + " m ?? kare/(1/60), biti??+0,30 sn ??? "
                       + Grounding.SettleSlackM.ToString("F2", CultureInfo.InvariantCulture)
                       + " m, h??zdan ba????ms??z): " + list;
            }

    }

}
#endif
