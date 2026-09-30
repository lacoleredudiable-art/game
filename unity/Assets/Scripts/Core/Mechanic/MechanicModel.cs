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

    /// <summary>Etkinin dünyadaki bedeni. Nitel alanlar imzaya girer, sayılar girmez.</summary>
    public sealed class MechanicBody
    {
        /// <summary>temas, durtme, saplama, yay, agir_yay, yere_vurus, balistik, isin, yerlestirme, govde</summary>
        public string Path = string.Empty;
        public string Shape = string.Empty;
        /// <summary>sende | dokunus | onunde | onunde_hat | onunde_yay | hedef_noktada | hat</summary>
        public string BornAt = string.Empty;
        public double SizeM;
        public double ReachM;
        public double SpeedMps;
        public double LifeSec;
        /// <summary>tek | tik | giris | cephe</summary>
        public string Contact = "tek";
        /// <summary>normal | delici | kati</summary>
        public string Permeability = "normal";
        public int Count = 1;
        public double CopyDelaySec;
        public int Chain;
        public double ChainMult = 1;
        public int MaxTargets;
        public double CastTimeMult = 1;
        public bool Anchored, Grows, Link, Cloud, Vertical, Homing, Mirror, Attached, Continuous, Pull, Ramp, Unstoppable;
        public readonly SortedSet<string> Traits = new SortedSet<string>(StringComparer.Ordinal);

        public string Signature()
        {
            var f = new List<string> { $"yol={Path}", $"sekil={Shape}", $"dogar={BornAt}", $"temas={Contact}", $"gecir={Permeability}" };
            if (SpeedMps > 0) f.Add("ucar");
            if (Count > 1) f.Add("cogul");
            if (Chain > 0) f.Add("seker");
            if (Anchored) f.Add("capali");
            if (Grows) f.Add("genisler");
            if (Link) f.Add("bag");
            if (Cloud) f.Add("bulut");
            if (Vertical) f.Add("dikey");
            if (Homing) f.Add("gudum");
            if (Mirror) f.Add("ayna");
            if (Attached) f.Add("govdeye_bagli");
            if (Continuous) f.Add("surekli");
            if (Pull) f.Add("cekim");
            if (Ramp) f.Add("rampa");
            if (Unstoppable) f.Add("sarsilmaz");
            if (MaxTargets == 1) f.Add("tek_hedef");
            return string.Join("|", f);
        }
    }

    /// <summary>Skill(fiil, sıfat, silah) için motorun çıkardığı nitel plan.</summary>
    public sealed class MechanicPlan
    {
        public int Verb, Adjective, Weapon;
        public string VerbName = string.Empty, AdjectiveName = string.Empty, WeaponName = string.Empty;
        public readonly MechanicBody Body = new MechanicBody();
        public readonly List<MechanicEffect> Effects = new List<MechanicEffect>();
        public readonly List<string> Trace = new List<string>();
        public readonly List<string> Labels = new List<string>();
        public readonly List<string> Conflicts = new List<string>();
        public readonly List<string> Contradictions = new List<string>();
        public string Description = string.Empty;
        public bool Compatible;

        public string SkillId => Adjective > 0 ? $"{Verb}-{Adjective}" : Verb.ToString(CultureInfo.InvariantCulture);

        public bool HasMode(string mode) => Effects.Any(e => e.Modes.Contains(mode));
        public bool HasStat(string stat) => Effects.Any(e => e.Stat == stat);

        public MechanicEffect Find(string stat, string target = null) =>
            Effects.FirstOrDefault(e => e.Stat == stat && (target == null || e.Target == target));

        public string EffectSignature() =>
            string.Join(" ; ", Effects.Select(e => e.Signature()).OrderBy(s => s, StringComparer.Ordinal));

        public string QualSignature() => Body.Signature() + " || " + EffectSignature();
    }
}
