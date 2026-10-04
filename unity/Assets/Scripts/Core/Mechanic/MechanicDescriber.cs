using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Dovus.Core.Mechanic
{
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
