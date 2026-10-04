namespace Dovus.Core.Casting
{
    /// <summary>Saldırı sırasında gövde kimi izler. Çubuk yönü burada yok.</summary>
    public enum AttackFaceKind : byte
    {
        /// <summary>Saldırı yok: hareket kendi kuralını kullanır.</summary>
        Movement,
        /// <summary>Dash: mevcut yönlü nişan. Çubuk vuruşun ortasında gövdeyi çevirmez.</summary>
        Directional,
        /// <summary>Seçili hedef, yoksa menzildeki otomatik hedef.</summary>
        LockedTarget,
        /// <summary>Hedef yok: bakış kalır, çubuğa dönülmez.</summary>
        Hold
    }
}
