namespace Dovus.Game.Vfx
{
    /// <summary>Ortak atlas batch'ine her kare dörtgen yazan efekt (siluet, harf flaşı, hedef halkası).</summary>
    public interface IEjderVfxQuadSource
    {
        void EmitQuads(EjderVfxBatchHost batch);
    }
}
