using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Dovus.Core.Mechanic
{
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
