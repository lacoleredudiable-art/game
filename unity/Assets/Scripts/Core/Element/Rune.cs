using Dovus.Core.Element;
namespace Dovus.Core.Element
{
    /// <summary>
    /// element-sistemi v6.1.1 rün kimlikleri (1..12). Ekrandaki altı nokta rün id'si değildir;
    /// <see cref="RuneLoadout"/> seçili 6 rünü slotlara eşler. Element ayrı kavramdır (<see cref="ElementId"/>).
    /// </summary>
    public enum Rune
    {
        Attack = 1,
        Heal = 2,
        Move = 3,
        Defense = 4,
        Burst = 5,
        Control = 6,
        Weaken = 7,
        Empower = 8,
        Cleanse = 9,
        Reflect = 10,
        Summon = 11,
        Time = 12
    }
}
