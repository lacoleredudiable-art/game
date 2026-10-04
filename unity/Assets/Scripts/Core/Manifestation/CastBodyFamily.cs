using Dovus.Core.Element;
using Dovus.Core.Grammar;

namespace Dovus.Core.Manifestation
{
    /// <summary>
    /// ~30–40 body clip ailesinden biri. Kombo tablosu değil — fiil tohumu + silüet eksenleri.
    /// docs/animasyon-omurgasi.md
    /// </summary>
    public enum CastBodyFamily : byte
    {
        None = 0,
        Pierce = 1,
        Sweep = 2,
        Slam = 3,
        Channel = 4,
        Guard = 5,
    }
}
