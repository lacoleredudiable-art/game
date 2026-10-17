namespace Dovus.Core.Cancel
{
    /// <summary>İptal tablosunun bildiği eylem türleri (13 §9). Hareket ve çizim bu tablonun dışındadır.</summary>
    public enum ActionKind : byte
    {
        Dodge = 0,
        WeaponSwap = 1,
        Skill = 2,
        BasicStrike = 3,
    }
}
