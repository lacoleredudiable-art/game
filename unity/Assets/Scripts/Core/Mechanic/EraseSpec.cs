using System;
using System.Linq;

namespace Dovus.Core.Mechanic
{
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
}
