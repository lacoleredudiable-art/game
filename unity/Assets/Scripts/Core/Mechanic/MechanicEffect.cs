using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Dovus.Core.Mechanic
{
    /// <summary>Bir etki atomu: kime, ne, ne kadar, nasıl (modlar nitel özelliklerdir).</summary>
    public sealed class MechanicEffect
    {
        /// <summary>deger | hiz | konum | varlik | yon | gorunurluk</summary>
        public string Atom = string.Empty;
        /// <summary>can, kalkan, zirh, hasar_buff, tempo, hareket, kendini_tasi, it, ...</summary>
        public string Stat = string.Empty;
        /// <summary>dusman | dost | kendin | dusman_nesnesi | alan</summary>
        public string Target = string.Empty;
        public double Amount;
        public double DurationSec;
        public readonly SortedSet<string> Modes = new SortedSet<string>(StringComparer.Ordinal);

        public bool Has(string mode) => Modes.Contains(mode);

        public string Signature() => $"{Atom}:{Stat}>{Target}[{string.Join(",", Modes)}]";

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Stat).Append('→').Append(Target);
            if (Amount != 0) sb.Append(' ').Append(Amount.ToString("0.##", CultureInfo.InvariantCulture));
            if (DurationSec > 0) sb.Append(' ').Append(DurationSec.ToString("0.#", CultureInfo.InvariantCulture)).Append("sn");
            if (Modes.Count > 0) sb.Append(" {").Append(string.Join(",", Modes)).Append('}');
            return sb.ToString();
        }
    }
}
