namespace Dovus.Core.Boss
{
    /// <summary>Vuruş kutularının ölçüldüğü model pozları (`art-kaynak/kul-ejderi/v2/ejder.blend`).</summary>
    public enum KulEjderiPose
    {
        /// <summary>Faz 1, `Poz_Yatis`.</summary>
        Lying,
        /// <summary>Faz 2, `Poz_Dogrulmus`.</summary>
        Reared,
        /// <summary>Faz 3, `Poz_Gogus_Yerde`.</summary>
        ChestDown,
        /// <summary>Sendeleme ve Kor Mührü çökmesi, `Poz_Sendeleme` (KE-7.8, KE-8.1).</summary>
        Slumped
    }
}
