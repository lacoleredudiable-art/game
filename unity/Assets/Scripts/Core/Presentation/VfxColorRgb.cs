namespace Dovus.Core.Presentation
{
    /// <summary>Lineer HDR çekirdek rengi + yoğunluk (§1).</summary>
    public readonly struct VfxColorRgb
    {
        public VfxColorRgb(float r, float g, float b, float intensity)
        {
            R = r;
            G = g;
            B = b;
            Intensity = intensity;
        }

        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float Intensity { get; }

        public static VfxColorRgb FromLinear(float r, float g, float b, float intensity) =>
            new VfxColorRgb(r, g, b, intensity);
    }
}
