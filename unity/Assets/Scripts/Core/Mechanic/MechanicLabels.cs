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

    public static class MechanicDescriber
    {
        static readonly Dictionary<string, string> PathTr = new Dictionary<string, string>
        {
            ["temas"] = "yumruk teması",
            ["durtme"] = "hançer dürtüşü",
            ["saplama"] = "mızrak saplaması",
            ["yay"] = "kılıç yayı",
            ["agir_yay"] = "ağır balta yayı",
            ["yere_vurus"] = "yere vuruş",
            ["balistik"] = "top mermisi (yay çizip düşer)",
            ["isin"] = "asa ışını (hat)",
            ["yerlestirme"] = "tılsım (hedefte belirir)",
            ["govde"] = "kalkan gövdesi",
            ["ok"] = "düz ok",
            ["sayfa"] = "uçan sayfa",
            ["kure"] = "yüzen küre"
        };

        static readonly Dictionary<string, string> ShapeTr = new Dictionary<string, string>
        {
            ["capsule"] = "kapsül",
            ["sphere"] = "küre",
            ["line"] = "çizgi",
            ["cone"] = "koni",
            ["point"] = "nokta",
            ["cylinder"] = "silindir",
            ["bulut"] = "bulut",
            ["bag"] = "bağ",
            ["sis_koridoru"] = "sis koridoru"
        };

        static readonly Dictionary<string, string> BornTr = new Dictionary<string, string>
        {
            ["sende"] = "sende",
            ["dokunus"] = "dokunduğun ilk kişide",
            ["onunde"] = "önünde",
            ["onunde_hat"] = "önünde uzun hat",
            ["onunde_yay"] = "önünde yay",
            ["hedef_noktada"] = "hedef noktada",
            ["hat"] = "senden hedefe hat"
        };

        static readonly Dictionary<string, string> TargetTr = new Dictionary<string, string>
        {
            ["dusman"] = "düşman",
            ["dost"] = "dost",
            ["kendin"] = "sen",
            ["dusman_nesnesi"] = "düşman mermisi",
            ["alan"] = "gövdenin olduğu yer"
        };

        static string Tr(Dictionary<string, string> map, string key) =>
            map.TryGetValue(key, out string v) ? v : key;

        public static string Describe(MechanicPlan p)
        {
            MechanicBody b = p.Body;
            var sb = new StringBuilder();
            sb.Append(Tr(PathTr, b.Path)).Append(" → ").Append(Tr(ShapeTr, b.Shape))
              .Append(' ').Append(b.SizeM.ToString("0.#", CultureInfo.InvariantCulture)).Append("m, ")
              .Append(Tr(BornTr, b.BornAt));

            var flags = new List<string>();
            if (b.Anchored) flags.Add("yerinde çapalı");
            if (b.Permeability == "kati") flags.Add("katı");
            if (b.Permeability == "delici") flags.Add("delip geçer");
            if (b.Grows) flags.Add("dışa büyür");
            if (b.Link) flags.Add("sen↔hedef bağı");
            if (b.Cloud) flags.Add("sis hacmi");
            if (b.Vertical) flags.Add("yerden yükselir");
            if (b.Homing) flags.Add("hedefe kilitli");
            if (b.Mirror) flags.Add("karşı noktada eşi");
            if (b.Attached) flags.Add("gövdene bağlı");
            if (b.Continuous) flags.Add("sürekli akar");
            if (b.Pull) flags.Add("içine çeker");
            if (b.Ramp) flags.Add("gücü artar");
            if (b.Unstoppable) flags.Add("sarsılmaz");
            if (b.MaxTargets == 1) flags.Add("tek hedef");
            if (b.Chain > 0) flags.Add($"{b.Chain} kez seker");
            if (b.Count > 1) flags.Add($"{b.Count} kez (kopya)");
            if (b.Contact == "giris") flags.Add("içine girene");
            if (b.Contact == "tik") flags.Add("tik tik");
            if (b.Contact == "cephe") flags.Add("cephe geçerken");
            if (b.Traits.Contains("uyumsuz")) flags.Add("uyumsuz silah");
            if (flags.Count > 0) sb.Append(" (").Append(string.Join(", ", flags)).Append(')');

            sb.Append(". Etkiler: ");
            sb.Append(string.Join("; ", p.Effects.Select(e =>
            {
                string amount = e.Amount != 0 ? " " + e.Amount.ToString("0.##", CultureInfo.InvariantCulture) : "";
                string dur = e.DurationSec > 0 ? " " + e.DurationSec.ToString("0.#", CultureInfo.InvariantCulture) + "sn" : "";
                string modes = e.Modes.Count > 0 ? " [" + string.Join(",", e.Modes) + "]" : "";
                return $"{e.Stat}→{Tr(TargetTr, e.Target)}{amount}{dur}{modes}";
            })));
            return sb.ToString();
        }

        /// <summary>HUD için kısa başlık: özel etiketler önce, genel desenler sonra.</summary>
        public static string ShortTitle(MechanicPlan p, int max = 2)
        {
            IEnumerable<string> special = p.Labels.Where(l => !MechanicLabeler.Generic.Contains(l));
            IEnumerable<string> generic = p.Labels.Where(l => MechanicLabeler.Generic.Contains(l));
            return string.Join(" · ", special.Concat(generic).Take(max));
        }
    }
}
