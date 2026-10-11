using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Dovus.Game.Editor
{
    /// <summary>klipler.csv okuyucu: kare aralığı, impact, iz penceresi, ejder anı, kök XZ dayanağı.</summary>
    public static partial class K3AssetSetup
    {
        internal readonly struct ClipRow
        {
            public readonly int FirstFrame;
            public readonly int LastFrame;
            public readonly int ImpactFrame;
            public readonly float? TrailOnSec;
            public readonly float? TrailOffSec;
            public readonly float? EjderSec;
            /// <summary>kok_xz = kutle_merkezi: kök XZ pose'a gömülmez, dayanak kütle merkezi.</summary>
            public readonly bool RootXzFromCenterOfMass;

            public ClipRow(int first, int last, int impact, float? trailOn, float? trailOff, float? ejder, bool rootXzFromCenterOfMass)
            {
                FirstFrame = first;
                LastFrame = last;
                ImpactFrame = impact;
                TrailOnSec = trailOn;
                TrailOffSec = trailOff;
                EjderSec = ejder;
                RootXzFromCenterOfMass = rootXzFromCenterOfMass;
            }
        }

        const string RootXzCenterOfMass = "kutle_merkezi";

        internal static Dictionary<string, ClipRow> ReadClipCsv()
        {
            string[] lines = File.ReadAllLines(ClipCsvPath);
            string[] head = lines[0].Split(';');
            int Col(string n) => Array.IndexOf(head, n);
            int cFirst = Col("bas_kare"), cLast = Col("son_kare"), cImpact = Col("impact_kare");
            int cOn = Col("trail_on_sn"), cOff = Col("trail_off_sn"), cEjder = Col("ejder_sn"), cRoot = Col("kok_xz");
            var rows = new Dictionary<string, ClipRow>(StringComparer.Ordinal);
            foreach (string line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                string[] f = line.Split(';');
                rows[f[0]] = new ClipRow(
                    int.Parse(f[cFirst], CultureInfo.InvariantCulture),
                    int.Parse(f[cLast], CultureInfo.InvariantCulture),
                    string.IsNullOrEmpty(f[cImpact]) ? 0 : int.Parse(f[cImpact], CultureInfo.InvariantCulture),
                    Sec(f[cOn]), Sec(f[cOff]), Sec(f[cEjder]),
                    cRoot >= 0 && f[cRoot] == RootXzCenterOfMass);
            }
            return rows;
        }

        static float? Sec(string s) =>
            string.IsNullOrEmpty(s) ? null : float.Parse(s, CultureInfo.InvariantCulture);
    }
}
