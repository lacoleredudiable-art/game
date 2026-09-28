using System.Globalization;
using System.Text;

namespace AtomSim;

/// <summary>Bir etki atomu: kime, ne, ne kadar, nasıl (modlar nitel özelliklerdir).</summary>
sealed class Effect
{
    public string Atom = "";      // deger | hiz | konum | varlik | yon | gorunurluk
    public string Stat = "";      // can, kalkan, zirh, hasar_buff, tempo, hareket, kendini_tasi, it, ...
    public string Target = "";    // dusman | dost | kendin | dusman_nesnesi
    public double Amount;
    public double DurationSec;
    public readonly SortedSet<string> Modes = new(StringComparer.Ordinal);

    public Effect Clone()
    {
        var e = new Effect { Atom = Atom, Stat = Stat, Target = Target, Amount = Amount, DurationSec = DurationSec };
        foreach (string m in Modes) e.Modes.Add(m);
        return e;
    }

    public bool IsHarm => Target is "dusman" or "dusman_nesnesi";

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
sealed class Body
{
    public string Path = "";        // temas, durtme, saplama, yay, agir_yay, yere_vurus, balistik, isin, yerlestirme, govde
    public string Shape = "";       // capsule, sphere, line, cone, point, cylinder, bulut, bag
    public string BornAt = "";      // sende | onunde | hedef_noktada
    public double SizeM;
    public double ReachM;
    public double SpeedMps;
    public double LifeSec;
    public string Contact = "tek";  // tek | tik | giris | cephe
    public string Permeability = "normal"; // normal | delici | kati
    public int Count = 1;
    public double CopyDelaySec;
    public int Chain;
    public double ChainMult = 1;
    public int MaxTargets;
    public bool Anchored, Grows, Link, Cloud, Vertical, Homing, Mirror, Attached, Continuous, Pull, Ramp, Unstoppable;
    public readonly SortedSet<string> Traits = new(StringComparer.Ordinal);

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

sealed class Plan
{
    public int Verb, Adjective, Weapon;
    public string VerbName = "", AdjectiveName = "", WeaponName = "";
    public Body Body = new();
    public List<Effect> Effects = new();
    public List<string> Trace = new();
    public List<string> Labels = new();
    public List<string> Conflicts = new();
    public List<string> Contradictions = new();
    public string Description = "";
    public bool Compatible;

    public string SkillId => Adjective > 0 ? $"{Verb}-{Adjective}" : $"{Verb}";

    public string QualSignature()
    {
        var effects = Effects.Select(e => e.Signature()).OrderBy(s => s, StringComparer.Ordinal);
        return Body.Signature() + " || " + string.Join(" ; ", effects);
    }
}
