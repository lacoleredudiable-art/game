namespace Dovus.Core.Equipment
{
    /// <summary>
    /// Tek ekipman satırı — equipment_system.examples içindeki weapon/armor/accessory adı
    /// + o satırın elementi. Id: "{slot}:{element}" (ör. weapon:Ateş).
    /// </summary>
    public sealed class EquipmentItem
    {
        public EquipmentItem(string id, string name, EquipmentSlot slot, string element)
            : this(id, name, slot, element, 1f, 1f, 1f, 1f, 0, string.Empty, null, string.Empty)
        {
        }

        public EquipmentItem(
            string id,
            string name,
            EquipmentSlot slot,
            string element,
            float damageMult,
            float castTimeMult,
            float poiseMult,
            float rangeMult,
            int mobilityMod,
            string identityPassive,
            int[]? compatibleVerbs,
            string animationsKey,
            string type = "",
            WeaponCombatProfile profile = null)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Slot = slot;
            Element = element ?? string.Empty;
            DamageMult = damageMult > 0f ? damageMult : 1f;
            CastTimeMult = castTimeMult > 0f ? castTimeMult : 1f;
            PoiseMult = poiseMult > 0f ? poiseMult : 1f;
            RangeMult = rangeMult > 0f ? rangeMult : 1f;
            MobilityMod = mobilityMod;
            IdentityPassive = identityPassive ?? string.Empty;
            CompatibleVerbs = compatibleVerbs ?? System.Array.Empty<int>();
            AnimationsKey = animationsKey ?? string.Empty;
            Type = type ?? string.Empty;
            Profile = profile;
        }

        public string Id { get; }
        public string Name { get; }
        public EquipmentSlot Slot { get; }
        public string Element { get; }
        public float DamageMult { get; }
        public float CastTimeMult { get; }
        public float PoiseMult { get; }
        public float RangeMult { get; }
        public int MobilityMod { get; }
        public string IdentityPassive { get; }
        public int[] CompatibleVerbs { get; }
        public string AnimationsKey { get; }
        public string Type { get; }
        /// <summary>10 silah teslimi. Eski ekipman satırlarında yok.</summary>
        public WeaponCombatProfile Profile { get; }
        /// <summary>Hasar ajanının okuyacağı zırh. Dokümanda sayı yoksa ağır silahta orta, hafifte 0.</summary>
        public float BaseArmor => Profile != null ? Profile.BaseArmor : 0f;

        public bool IsCompatibleWithVerb(int verbId)
        {
            for (int i = 0; i < CompatibleVerbs.Length; i++)
                if (CompatibleVerbs[i] == verbId)
                    return true;
            return false;
        }
    }
}
