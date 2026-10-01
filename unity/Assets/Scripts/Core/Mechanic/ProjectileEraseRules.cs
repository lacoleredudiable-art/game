using System;
using System.Linq;

namespace Dovus.Core.Mechanic
{
    public enum EraseShape : byte
    {
        None,
        /// <summary>Hacim diski (merkez sabit).</summary>
        Disk,
        /// <summary>Oyuncu↔dost bağ şeridi (bag_hatti).</summary>
        Segment,
        /// <summary>Oyuncudan nişan boyunca delici silme hattı (delici).</summary>
        Line,
        /// <summary>Merkez her kare oyuncuyu izler (surekli_perde).</summary>
        Follow
    }

    public enum EraseMode : byte
    {
        None,
        Delete,
        Absorb,
        Reflect,
        Shroud,
        Targeted
    }

    /// <summary>Bir planın düşman mermisine ne yaptığı. Sayılar gövde / adjective_mods / params'tan.</summary>
    public readonly struct EraseSpec
    {
        public EraseSpec(
            EraseShape shape, EraseMode mode, float radiusM, float growTo, float ratePerSec,
            int chainCount, bool twice, bool anchored, float lifesteal, float reflectMult,
            float widthM, float lengthM, double twiceDelaySec)
        {
            Shape = shape;
            Mode = mode;
            RadiusM = radiusM;
            GrowTo = growTo;
            RatePerSec = ratePerSec;
            ChainCount = chainCount;
            Twice = twice;
            Anchored = anchored;
            Lifesteal = lifesteal;
            ReflectMult = reflectMult;
            WidthM = widthM;
            LengthM = lengthM;
            TwiceDelaySec = twiceDelaySec;
        }

        public EraseShape Shape { get; }
        public EraseMode Mode { get; }
        public float RadiusM { get; }
        /// <summary>1 = sabit; &gt;1 yarıçap ömür boyunca bu katsayıya büyür (yukselen_perde).</summary>
        public float GrowTo { get; }
        /// <summary>Targeted: saniyede kaç tek mermi silinir (hedefli).</summary>
        public float RatePerSec { get; }
        public int ChainCount { get; }
        /// <summary>iki_kez: kopya vuruşunda bir kez daha siler.</summary>
        public bool Twice { get; }
        public double TwiceDelaySec { get; }
        /// <summary>engel: döküm noktasına çapalı disk.</summary>
        public bool Anchored { get; }
        /// <summary>Absorb: yutulan mermi hasarının can olarak dönen payı.</summary>
        public float Lifesteal { get; }
        /// <summary>Reflect: geri dönen merminin hasar çarpanı.</summary>
        public float ReflectMult { get; }
        /// <summary>Segment/Line şerit genişliği (tam genişlik).</summary>
        public float WidthM { get; }
        /// <summary>Line uzunluğu (en az; vuruş noktası daha uzaksa oraya kadar).</summary>
        public float LengthM { get; }

        public bool IsEmpty => Mode == EraseMode.None || Shape == EraseShape.None;

        public override string ToString() =>
            $"{Shape}/{Mode} r={RadiusM:0.##} grow={GrowTo:0.##} rate={RatePerSec:0.##} w={WidthM:0.##} len={LengthM:0.##}"
            + (Twice ? " x2" : "") + (Anchored ? " capali" : "");
    }

    /// <summary>
    /// MechanicPlan → EraseSpec (saf). mermi_sil etkisinin moduna göre: yut → emme, engel → çapalı disk,
    /// geri_gonder → yansıtma, delici → hat, sis_perdesi → perde, hedefli → tek tek, yukselen_perde →
    /// büyüyen disk, surekli_perde → oyuncuyu izleyen disk, bag_hatti → bağ şeridi, diğerleri → silme.
    /// Skill kimliği okunmaz; yalnız etki modu ve gövde (AGENTS kural 6).
    /// </summary>
    public static class ProjectileEraseRules
    {
        public static bool Erases(MechanicPlan plan) =>
            plan != null && plan.Effects.Any(e => e.Stat == "mermi_sil");

        public static EraseSpec For(MechanicPlan plan, MechanicRules rules)
        {
            MechanicEffect e = plan?.Effects.FirstOrDefault(x => x.Stat == "mermi_sil");
            if (e == null)
                return default;
            MechanicBody b = plan.Body;
            float radius = (float)Math.Max(0.05, b.SizeM);
            EraseShape shape = EraseShape.Disk;
            EraseMode mode = EraseMode.Delete;
            float grow = 1f, rate = 0f, lifesteal = 0f, reflectMult = 0f, width = 0f, length = 0f;
            bool anchored = false;

            if (e.Has("yut"))
            {
                mode = EraseMode.Absorb;
                lifesteal = (float)(rules?.AdjNum(plan.Adjective, "lifesteal", 0) ?? 0);
            }
            else if (e.Has("engel"))
                anchored = true;
            else if (e.Has("geri_gonder"))
            {
                mode = EraseMode.Reflect;
                reflectMult = (float)Param(rules, "projectile_reflect_mult", 1.0);
            }
            else if (e.Has("delici"))
            {
                shape = EraseShape.Line;
                width = (float)Param(rules, "erase_line_width_m", 1.0);
                length = (float)Math.Max(0.1, b.ReachM);
            }
            else if (e.Has("sis_perdesi"))
                mode = EraseMode.Shroud;
            else if (e.Has("hedefli"))
            {
                mode = EraseMode.Targeted;
                rate = (float)Param(rules, "targeted_erase_per_sec", 2.0);
            }
            else if (e.Has("yukselen_perde"))
                grow = (float)Math.Max(1.0, Param(rules, "ramp_max", 1.5));
            else if (e.Has("surekli_perde"))
                shape = EraseShape.Follow;
            else if (e.Has("bag_hatti"))
            {
                shape = EraseShape.Segment;
                width = (float)Math.Max(0.02, b.SizeM * 0.1);
            }

            bool twice = e.Has("iki_kez");
            return new EraseSpec(
                shape, mode, radius, grow, rate,
                b.Chain, twice, anchored, lifesteal, reflectMult, width, length,
                twice ? Math.Max(0.0, b.CopyDelaySec) : 0.0);
        }

        static double Param(MechanicRules rules, string key, double fallback)
        {
            if (rules == null)
                return fallback;
            double v = rules.Param(key);
            return v > 0 ? v : fallback;
        }
    }
}
