using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dovus.Core.Tuning;

namespace Dovus.Core.Mechanic
{
    /// <summary>Reçetedeki tek görsel parça. Yerel çerçeve: X sağ, Y yukarı, Z ileri (cast yönü).</summary>
    public struct VisualPiece
    {
        public double X, Y, Z;
        public double DelaySec;
        public double Scale;
        public double StretchZ;
        /// <summary>Parça bu kadar aşağıdan doğup Y'ye yükselir.</summary>
        public double RiseM;
        /// <summary>Parça ömrü boyunca bu kadar yer değiştirir (çekim, kavis, yayılma).</summary>
        public double MoveX, MoveZ;
        /// <summary>Taşıyıcıya (mermi) bağlı, onunla uçar.</summary>
        public bool Carried;
        /// <summary>Katı cismi olmayan, yolun ucunda asılı kalan pus; döngüde yalnız bu parçalar yenilenir.</summary>
        public bool Haze;
    }

    /// <summary>
    /// Skill görseli = madde (fiil) × yol (silah teslimi) × silüet (sıfatın gövde kuralları).
    /// Madde yalnız fiil kimliğidir; dizilim yalnız <see cref="MechanicBody"/>'den okunur.
    /// </summary>
    public sealed class VisualRecipe
    {
        public int Substance;
        public string Motion = string.Empty;
        public string Layout = string.Empty;
        public double PieceSizeM;
        public double PieceMoveSec;
        public readonly List<VisualPiece> Pieces = new List<VisualPiece>();
        /// <summary>Çerçeve taşıyıcıyı izler; döngüde parçalar taşıyıcının o anki yerine bırakılır (iz).</summary>
        public bool Follow;
        public bool FollowOwner;
        public bool Tether;
        public bool Grow;
        public bool Loop;
        public double LoopEverySec;
        public double GroundRingRadiusM;
        /// <summary>BornAt'tan: gövde sahibin önünde doğuyorsa çerçeve bu kadar ileri kayar (yalnız merkezli dizilimler).</summary>
        public double BornAheadM;
        public readonly SortedSet<string> Traits = new SortedSet<string>(StringComparer.Ordinal);

        public string Signature() =>
            $"madde={Substance}|yol={Motion}|dizilim={Layout}|parca={Pieces.Count}|{string.Join(",", Traits)}";
    }

    public static class MechanicVisualComposer
    {
        public static VisualRecipe Compose(MechanicPlan plan, SkillVisualTuning t)
        {
            if (plan == null)
                return null;
            t ??= new SkillVisualTuning();
            MechanicBody b = plan.Body;
            double size = Math.Min(t.PieceMaxSizeM, Math.Max(t.PieceMinSizeM, b.SizeM * t.PieceSizeFrac));
            double reach = Math.Max(b.ReachM, b.SizeM);
            double step = Math.Max(MechanicVisualDefaults.MinPieceStepM, size * t.SpacingPieces);
            var r = new VisualRecipe
            {
                Substance = plan.Verb,
                Motion = b.Path ?? string.Empty,
                PieceSizeM = size,
                PieceMoveSec = t.PieceMoveSec,
                LoopEverySec = t.LoopEverySec
            };

            double endX = 0, endZ = 0;
            switch (b.Path)
            {
                case "balistik":
                    r.Layout = "tasinan";
                    r.Follow = true;
                    r.Loop = true;
                    r.LoopEverySec = t.TrailEverySec;
                    r.Traits.Add("iz");
                    Add(r, 0, 0, 0, 1, carried: true);
                    break;
                case "isin":
                    r.Layout = "hat";
                    Row(r, reach, step, t.BeamSpeedMps, t);
                    endZ = reach;
                    break;
                case "saplama":
                    r.Layout = "saplama";
                    Row(r, reach, step, t.ThrustSpeedMps, t);
                    endZ = reach;
                    break;
                case "durtme":
                    r.Layout = "durtme";
                    Row(r, Math.Min(reach, step * 2), step, t.ThrustSpeedMps, t);
                    endZ = Math.Min(reach, step * 2);
                    break;
                case "yay":
                    r.Layout = "yay";
                    Arc(r, reach * MechanicVisualDefaults.SweepReachFrac, t.SweepArcDeg, t.SweepSec, step, 1, t);
                    endZ = reach * MechanicVisualDefaults.SweepReachFrac;
                    break;
                case "agir_yay":
                    r.Layout = "agir_yay";
                    Arc(r, reach * MechanicVisualDefaults.SweepReachFrac, t.SweepArcDeg, t.HeavySweepSec, step, t.HeavyScale, t);
                    Add(r, 0, reach * MechanicVisualDefaults.SweepReachFrac, t.HeavySweepSec, t.HeavyScale);
                    endZ = reach * MechanicVisualDefaults.SweepReachFrac;
                    break;
                case "yere_vurus":
                    r.Layout = "carpma";
                    Add(r, 0, 0, 0, t.HeavyScale);
                    Ring(r, 0, 0, b.SizeM * 0.5, t.SlamRingPieces, t.SlamRingDelaySec, MechanicVisualDefaults.SlamRingDelayScale);
                    break;
                case "yerlestirme":
                    r.Layout = "yerlesim";
                    Scatter(r, 0, 0, b.SizeM * 0.5, t.ScatterPieces, t.SlamRingDelaySec * 0.5, 1);
                    for (int i = 0; i < r.Pieces.Count; i++)
                    {
                        VisualPiece p = r.Pieces[i];
                        p.RiseM = size;
                        r.Pieces[i] = p;
                    }
                    break;
                case "govde":
                    r.Layout = "govde";
                    r.FollowOwner = true;
                    Ring(r, 0, 0, size, t.OrbitPieces, 0, MechanicVisualDefaults.OrbitPieceDelayScale);
                    break;
                case "ok":
                    r.Layout = "ok";
                    Row(r, reach, step, t.ThrustSpeedMps, t);
                    endZ = reach;
                    break;
                case "sayfa":
                    r.Layout = "sayfa";
                    Row(r, reach * MechanicVisualDefaults.PageReachFrac, step, t.ThrustSpeedMps, t);
                    endZ = reach * MechanicVisualDefaults.PageReachFrac;
                    break;
                case "kure":
                    r.Layout = "kure";
                    Add(r, reach * MechanicVisualDefaults.SphereReachFrac, 0, 0, 1);
                    endZ = reach * MechanicVisualDefaults.SphereReachFrac;
                    break;
                default:
                    r.Layout = "temas";
                    Add(r, 0, reach, 0, 1);
                    endZ = reach;
                    break;
            }

            if (r.Layout is "carpma" or "yerlesim")
                r.BornAheadM = b.BornAt switch
                {
                    "hedef_noktada" or "dokunus" => reach,
                    "onunde" => reach * 0.5,
                    _ => 0
                };

            ApplyBody(r, b, t, size, step, endX, endZ);
            return r;
        }

        static void ApplyBody(VisualRecipe r, MechanicBody b, SkillVisualTuning t, double size, double step,
            double endX, double endZ)
        {
            if (b.Cloud)
            {
                int first = r.Pieces.Count;
                Scatter(r, endX, endZ, b.SizeM * 0.5, t.CloudPieces, MechanicVisualDefaults.MinPieceStepM, t.CloudScale);
                for (int i = first; i < r.Pieces.Count; i++)
                {
                    VisualPiece p = r.Pieces[i];
                    p.Haze = true;
                    r.Pieces[i] = p;
                }
                r.Loop = true;
                r.Traits.Add("bulut");
            }

            if (b.Permeability == "kati")
            {
                int n = Clamp((int)Math.Ceiling(b.SizeM / step), 3, t.MaxPiecesPerRow);
                double maxDelay = MaxDelay(r);
                for (int i = 0; i < n; i++)
                {
                    double x = endX + (i - (n - 1) * 0.5) * step;
                    r.Pieces.Add(new VisualPiece
                    {
                        X = x, Z = endZ, DelaySec = maxDelay + i * MechanicVisualDefaults.TrailDelayStepSec, Scale = 1, StretchZ = 1, RiseM = size
                    });
                }
                r.Traits.Add("duvar");
            }

            if (b.MaxTargets == 1 && r.Pieces.Count(p => !p.Haze) > 1)
            {
                VisualPiece last = r.Pieces.Where(p => !p.Haze).OrderBy(p => p.DelaySec).Last();
                List<VisualPiece> haze = r.Pieces.Where(p => p.Haze).ToList();
                r.Pieces.Clear();
                last.Scale *= t.SingleTargetScale;
                r.Pieces.Add(last);
                r.Pieces.AddRange(haze);
                r.Traits.Add("tek");
            }

            if (b.Permeability == "delici")
            {
                Map(r, p => { p.StretchZ = t.PierceStretch; return p; });
                r.Traits.Add("delici");
            }

            if (b.Unstoppable)
            {
                Map(r, p => { p.Scale *= t.HeavyScale; return p; });
                r.Traits.Add("sarsilmaz");
            }

            if (b.Vertical)
            {
                double d = MaxDelay(r);
                for (int i = 0; i < 3; i++)
                    r.Pieces.Add(new VisualPiece
                    {
                        X = endX, Y = i * size, Z = endZ, DelaySec = d + i * t.RiseSec * MechanicVisualDefaults.RiseStaggerRatio,
                        Scale = 1, StretchZ = 1, RiseM = size * MechanicVisualDefaults.RiseHeightMult
                    });
                r.Traits.Add("dikey");
            }

            if (b.Ramp)
            {
                var ordered = r.Pieces.Select((p, i) => (p, i)).OrderBy(x => x.p.DelaySec).ToList();
                for (int k = 0; k < ordered.Count; k++)
                {
                    VisualPiece p = ordered[k].p;
                    p.Scale *= 1 + k * t.RampStepScale;
                    r.Pieces[ordered[k].i] = p;
                }
                r.Traits.Add("rampa");
            }

            if (b.Homing)
            {
                int k = 0;
                Map(r, p => { p.MoveX += (k++ % 2 == 0 ? 1 : -1) * t.CurveM; return p; });
                r.Traits.Add("gudum");
            }

            if (b.Pull)
            {
                Map(r, p =>
                {
                    double sx = endX + (p.X - endX) * t.PullStartFrac;
                    double sz = endZ + (p.Z - endZ) * t.PullStartFrac;
                    p.MoveX += (endX - sx) * MechanicVisualDefaults.CarriedMoveLerpRatio;
                    p.MoveZ += (endZ - sz) * MechanicVisualDefaults.CarriedMoveLerpRatio;
                    p.X = sx;
                    p.Z = sz;
                    return p;
                });
                r.Traits.Add("cekim");
            }

            if (b.Grows)
            {
                r.Grow = true;
                Map(r, p => { p.MoveX += (p.X - endX) * 0.5; p.MoveZ += (p.Z - endZ) * 0.5; return p; });
                r.Traits.Add("buyur");
            }

            if (b.Chain > 0)
            {
                double d = MaxDelay(r);
                for (int c = 1; c <= b.Chain; c++)
                    r.Pieces.Add(new VisualPiece
                    {
                        X = endX + (c % 2 == 1 ? 1 : -1) * t.ChainHopM * MechanicVisualDefaults.PageReachFrac,
                        Z = endZ + c * t.ChainHopM,
                        DelaySec = d + c * t.ChainHopSec,
                        Scale = MechanicVisualDefaults.OrbitPieceDelayScale, StretchZ = 1
                    });
                r.Traits.Add("seker" + b.Chain.ToString(CultureInfo.InvariantCulture));
            }

            if (b.Count > 1)
            {
                var copy = r.Pieces.ToList();
                double delay = b.CopyDelaySec > 0 ? b.CopyDelaySec : t.ChainHopSec * 2;
                for (int k = 1; k < b.Count; k++)
                    foreach (VisualPiece src in copy)
                    {
                        VisualPiece p = src;
                        p.DelaySec += k * delay;
                        p.X += k * t.EchoOffsetM;
                        r.Pieces.Add(p);
                    }
                r.Traits.Add("kopya" + b.Count.ToString(CultureInfo.InvariantCulture));
            }

            if (b.Mirror)
            {
                var copy = r.Pieces.ToList();
                foreach (VisualPiece src in copy)
                {
                    VisualPiece p = src;
                    p.X = -p.X;
                    p.Z = -p.Z;
                    p.MoveX = -p.MoveX;
                    p.MoveZ = -p.MoveZ;
                    r.Pieces.Add(p);
                }
                r.Traits.Add("ayna");
            }

            if (b.Link)
            {
                r.Tether = true;
                r.Traits.Add("bag");
            }

            if (b.Anchored)
            {
                r.GroundRingRadiusM = Math.Max(size, b.SizeM * 0.5);
                r.Traits.Add("capa");
            }

            if (b.Continuous)
            {
                r.Loop = true;
                r.Traits.Add("akis");
            }

            if (b.Attached)
            {
                r.FollowOwner = true;
                r.Traits.Add("govdeye_bagli");
            }
        }

        static void Add(VisualRecipe r, double x, double z, double delay, double scale, bool carried = false) =>
            r.Pieces.Add(new VisualPiece { X = x, Z = z, DelaySec = delay, Scale = scale, StretchZ = 1, Carried = carried });

        static void Row(VisualRecipe r, double reach, double step, double speedMps, SkillVisualTuning t)
        {
            double start = Math.Min(step * 0.5, reach);
            int n = Clamp((int)Math.Ceiling((reach - start) / step) + 1, 1, t.MaxPiecesPerRow);
            for (int i = 0; i < n; i++)
            {
                double z = n == 1 ? reach : start + (reach - start) * i / (n - 1);
                Add(r, 0, z, z / Math.Max(MechanicVisualDefaults.MinSpeedDivisorMps, speedMps), 1);
            }
        }

        static void Arc(VisualRecipe r, double radius, double arcDeg, double sweepSec, double step, double scale,
            SkillVisualTuning t)
        {
            double half = arcDeg * 0.5 * Math.PI / MechanicVisualDefaults.RadToDeg;
            int n = Clamp((int)Math.Ceiling(radius * half * 2 / step) + 1, 3, t.MaxPiecesPerRow);
            for (int i = 0; i < n; i++)
            {
                double f = i / (double)(n - 1);
                double a = half - 2 * half * f;
                Add(r, Math.Sin(a) * radius, Math.Cos(a) * radius, sweepSec * f, scale);
            }
        }

        static void Ring(VisualRecipe r, double cx, double cz, double radius, int n, double delay, double scale)
        {
            for (int i = 0; i < n; i++)
            {
                double a = 2 * Math.PI * i / n;
                Add(r, cx + Math.Sin(a) * radius, cz + Math.Cos(a) * radius, delay, scale);
            }
        }

        static void Scatter(VisualRecipe r, double cx, double cz, double radius, int n, double delayStep, double scale)
        {
            const double golden = 2.39996322972865332;
            for (int i = 0; i < n; i++)
            {
                double rr = radius * Math.Sqrt((i + 0.5) / n);
                double a = i * golden;
                Add(r, cx + Math.Sin(a) * rr, cz + Math.Cos(a) * rr, i * delayStep, scale);
            }
        }

        static void Map(VisualRecipe r, Func<VisualPiece, VisualPiece> f)
        {
            for (int i = 0; i < r.Pieces.Count; i++)
                r.Pieces[i] = f(r.Pieces[i]);
        }

        static double MaxDelay(VisualRecipe r) => r.Pieces.Count == 0 ? 0 : r.Pieces.Max(p => p.DelaySec);

        static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
