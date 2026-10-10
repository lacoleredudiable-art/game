namespace Dovus.Core.Presentation
{
    /// <summary>efekt-motoru §1 ejderha bedeni motifi (şekilden bağımsız işaret).</summary>
    public enum VfxMotifKind : byte
    {
        None = 0,
        Claw = 1,       // Zarar — pençe
        Soot = 2,       // Zayıflatma — is
        Bone = 3,       // Çağırma — kemik
        Wing = 4,       // Hareket — kanat
        Scale = 5,      // Koruma — pul
        ScaleFlash = 6, // Yansıma — pul parlaması
        TailChain = 7,  // Kontrol — kuyruk zinciri
        TailArc = 8,    // Kuvvet — kuyruk yayı
        BreathMist = 9, // Şifa — nefes buğusu
        HeartEmber = 10,// Güçlendirme — yürek koru
        ScaleShed = 11, // Arındırma — pul dökme
        RingScale = 12  // Zaman — halkalı pul
    }
}
