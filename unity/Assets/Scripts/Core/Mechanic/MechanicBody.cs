using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Dovus.Core.Mechanic
{
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
}
