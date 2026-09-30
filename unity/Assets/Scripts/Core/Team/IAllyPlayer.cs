namespace Dovus.Core.Team
{
    /// <summary>
    /// Takım arkadaşı. Bot, dummy ve ileride ağ oyuncusu aynı kapıdan geçer.
    /// Konum okunur; ışınlamayı sistem ister, dünya uygular.
    /// </summary>
    public interface IAllyPlayer
    {
        int Id { get; }
        float X { get; }
        float Y { get; }
        float Z { get; }
        float Radius { get; }
        float HpRatio { get; }
        string LastSkillId { get; }
        bool TemplateOwnsPosition { get; }
    }
}
