namespace Dovus.Game.Diagnostics
{
    /// <summary>Debug build oynanış bayrakları — Composition'da tek örnek.</summary>
    public interface IDebugFlags
    {
        bool DevHp { get; set; }
        bool HalfHpStart { get; set; }
        bool DevHpActive { get; }
        float StartHpRatio { get; }
    }
}
