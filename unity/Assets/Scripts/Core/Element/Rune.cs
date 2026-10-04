using Dovus.Core.Element;
namespace Dovus.Core.Element
{
    /// <summary>
    /// element-sistemi v6.1.1 çift-yüzlü rün kimlikleri. Ekrandaki altı nokta rün kimliği
    /// değildir; <see cref="RuneLoadout"/> seçili 6 rünü slotlara eşler.
    /// </summary>
    public enum Rune
    {
        Saldiri = 1,
        Iyilestirme = 2,
        Hareket = 3,
        Savunma = 4,
        Patlama = 5,
        Kontrol = 6,
        Zayiflatma = 7,
        Guclendirme = 8,
        Arindirma = 9,
        Yansima = 10,
        Cagirma = 11,
        Zaman = 12,

        // Kaynak uyumluluğu: eski görsel/motor sınıfları ilk altı id'yi bu adlarla kullanıyor.
        // Yeni çözümleme ve UI bu alias'ları değil v6 adlarını gösterir.
        Ates = Saldiri,
        Su = Iyilestirme,
        Hava = Hareket,
        Toprak = Savunma,
        Aydinlik = Patlama,
        Karanlik = Kontrol
    }
}
