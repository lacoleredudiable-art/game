using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Dovus.Core.Mechanic
{
    /// <summary>Ortaya çıkan özellik desenine isim verir. Motor etiketi okumaz; yalnız sunum/rapor.</summary>
    public static class MechanicLabeler
    {
        public static readonly HashSet<string> Generic = new HashSet<string>(StringComparer.Ordinal)
        {
            "Dalga", "Delici", "Seken", "Güdümlü", "Bağ", "Akış", "Çift", "Ayna eşli", "Yükselen", "Sert"
        };

        public static void Label(MechanicPlan p)
        {
            MechanicBody b = p.Body;
            List<string> l = p.Labels;
            bool M(string mode) => p.HasMode(mode);
            bool S(string stat) => p.HasStat(stat);

            if (b.Permeability == "kati" && b.Anchored)
                l.Add(b.Path == "isin" ? "Hat duvar" : b.Vertical ? "Yerden taş duvar" : "Duvar");
            if (S("portal")) l.Add("Portal");
            if (S("yer_degistir")) l.Add("Yer değiştirme");
            if (M("sana_dogru")) l.Add("Hedefi sana çekme");
            if (M("merkeze")) l.Add("İçe çöküş");
            if (M("bag_boyunca")) l.Add("Bağla çekme");
            if (S("isaret_geri_don")) l.Add("İşaretle-geri dön");
            if (S("hedefin_arkasina")) l.Add("Arkaya ışınlanma");
            if (M("gorunmez_gecis")) l.Add("Görünmez geçiş");
            if (M("sicrayip_cakil") || M("yol:sicrayip_cakilma")) l.Add("Sıçrayıp çakılma");
            if (M("yol:isarete_isinlanma")) l.Add("Işınlanma");
            if (M("yol:firlatilma")) l.Add("Fırlatılma");
            if (M("yol:kalkan_hucumu")) l.Add("Kalkan hücumu");
            if (M("faz")) l.Add("Faz geçişi");
            if (M("suzulme")) l.Add("Süzülme");
            if (M("sekmeli")) l.Add("Sekmeli zıplama");
            if (M("inis_dalgasi")) l.Add("İniş şok dalgası");
            if (M("dondur")) l.Add("Dondurma");
            if (M("zaman_alani")) l.Add("Zaman alanı");
            if (p.Effects.Any(e => e.Stat == "tempo" && e.Has("aktarim"))) l.Add("Zaman çalma");
            if (M("hizlanma")) l.Add("Hızlanma");
            if (M("geri_sarma")) l.Add("Geri sarma");
            if (M("senkron")) l.Add("Zaman senkronu");
            if (M("titrer")) l.Add("Titreyen zaman");
            if (M("yanki")) l.Add("Yankı");
            if (S("klon")) l.Add("Klon");
            if (M("ayna_klon")) l.Add("Ayna klon");
            if (M("taret")) l.Add("Taret");
            if (S("yem_kopya")) l.Add("Yem kopya");
            if (M("suikastci")) l.Add("Suikastçı minyon");
            if (M("bagli_muhafiz")) l.Add("Muhafız");
            if (M("can_emen")) l.Add("Can emen minyon");
            if (M("ziplayan")) l.Add("Zıplayan minyon");
            if (M("halka") && S("aktor_yarat")) l.Add("Minyon halkası");
            if (M("gorunmez") && S("aktor_yarat")) l.Add("Görünmez minyon");
            if (b.Cloud) l.Add(b.Shape == "sis_koridoru" ? "Sis koridoru" : "Sis");
            if (b.Pull) l.Add("Girdap");
            if (M("tuzak")) l.Add(b.Traits.Contains("mayin") ? "Mayın" : "Tuzak");
            if (M("totem")) l.Add("Totem");
            if (M("kiskac")) l.Add("Kıskaç");
            if (M("ayna_yuzey")) l.Add("Ayna yüzey");
            if (M("dunyada")) l.Add("Uzak yansıtıcı");
            if (M("dokunulana")) l.Add("Dosta yansıtma");
            if (M("engel")) l.Add("Mermi kesen engel");
            if (M("arinma_alani")) l.Add("Arınma alanı");
            if (S("durum_aktar")) l.Add("Durum aktarma");
            if (S("iyi_durum_sil")) l.Add("Buff silme");
            if (M("koruyucu_tetik")) l.Add("Koruyucu tetik");
            if (M("isaretli_an")) l.Add("Gecikmeli an");
            if (M("savusturma")) l.Add("Kusursuz savuşturma");
            if (M("emme")) l.Add("Emme/çalma");
            if (M("can_bagi")) l.Add("Can bağı");
            if (S("yonlendir")) l.Add("Hasar yönlendirme");
            if (M("ters_kontrol")) l.Add("Ters kontrol");
            if (M("havaya_at") || M("havada")) l.Add("Havaya atma");
            if (M("bataklik")) l.Add("Bataklık");
            if (M("akinti")) l.Add("Akıntı");
            if (M("ters_kopya")) l.Add("Ters kopya");
            if (M("geri_gonder")) l.Add("Mermi geri gönderme");
            if (M("tasma")) l.Add("Tasma");
            if (M("tasar")) l.Add("Taşma");
            if (M("cana_cevir")) l.Add("Hasar emme");
            if (M("tumunu_sil")) l.Add("Tam arındırma");
            if (M("guce_cevir")) l.Add("Arınıp güçlenme");
            if (M("bag_bagisiklik")) l.Add("Bağışıklık bağı");
            if (M("bolunen")) l.Add("Bölünen yansıma");
            if (M("sersem")) l.Add("Sersemletme");
            if (b.Traits.Contains("cit")) l.Add("Çit");
            if (b.Traits.Contains("inen_akis_alani")) l.Add("İnen akış alanı");

            if (b.Grows) l.Add("Dalga");
            if (b.Permeability == "delici") l.Add("Delici");
            if (b.Chain > 0) l.Add("Seken");
            if (b.Homing) l.Add("Güdümlü");
            if (b.Link) l.Add("Bağ");
            if (b.Continuous) l.Add("Akış");
            if (b.Count > 1) l.Add("Çift");
            if (b.Mirror) l.Add("Ayna eşli");
            if (b.Ramp) l.Add("Yükselen");
            if (l.Count == 0 && (M("sert") || M("guclu"))) l.Add("Sert");
        }
    }
}
