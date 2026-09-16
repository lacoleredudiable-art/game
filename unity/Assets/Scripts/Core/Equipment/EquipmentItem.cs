namespace Dovus.Core.Equipment
{
    /// <summary>
    /// Tek ekipman satırı — equipment_system.examples içindeki weapon/armor/accessory adı
    /// + o satırın elementi. Id: "{slot}:{element}" (ör. weapon:Ateş).
    /// </summary>
    public sealed class EquipmentItem
    {
        public EquipmentItem(string id, string name, EquipmentSlot slot, string element)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Slot = slot;
            Element = element ?? string.Empty;
        }

        public string Id { get; }
        public string Name { get; }
        public EquipmentSlot Slot { get; }
        public string Element { get; }
    }
}
