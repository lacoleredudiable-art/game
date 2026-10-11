using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Dovus.Game.Editor
{
    /// <summary>klipler.csv okuyucu: kare aralığı, impact, iz penceresi, ejder anı, kök XZ dayanağı, kaynak FBX.</summary>
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
            /// <summary>Klibin kesildiği FBX (Klipler/&lt;kaynak&gt;.fbx); boşsa klibin kendi adı.</summary>
            public readonly string Source;

            public ClipRow(int first, int last, int impact, float? trailOn, float? trailOff, float? ejder, bool rootXzFromCenterOfMass, string source)
            {
                FirstFrame = first;
                LastFrame = last;
                ImpactFrame = impact;
                TrailOnSec = trailOn;
                TrailOffSec = trailOff;
                EjderSec = ejder;
                RootXzFromCenterOfMass = rootXzFromCenterOfMass;
                Source = source;
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
            int cSource = Col("kaynak");
            var rows = new Dictionary<string, ClipRow>(StringComparer.Ordinal);
            foreach (string line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                string[] f = line.Split(';');
                string source = cSource >= 0 && cSource < f.Length && !string.IsNullOrEmpty(f[cSource]) ? f[cSource] : f[0];
                rows[f[0]] = new ClipRow(
                    int.Parse(f[cFirst], CultureInfo.InvariantCulture),
                    int.Parse(f[cLast], CultureInfo.InvariantCulture),
                    string.IsNullOrEmpty(f[cImpact]) ? 0 : int.Parse(f[cImpact], CultureInfo.InvariantCulture),
                    Sec(f[cOn]), Sec(f[cOff]), Sec(f[cEjder]),
                    cRoot >= 0 && cRoot < f.Length && f[cRoot] == RootXzCenterOfMass,
                    source);
            }
            return rows;
        }

        /// <summary>Bir FBX'ten kesilen klipler (CSV sırasıyla).</summary>
        internal static List<(string Name, ClipRow Row)> RowsFromSource(Dictionary<string, ClipRow> rows, string fbx) =>
            rows.Where(kv => kv.Value.Source == fbx).Select(kv => (kv.Key, kv.Value)).ToList();

        static float? Sec(string s) =>
            string.IsNullOrEmpty(s) ? null : float.Parse(s, CultureInfo.InvariantCulture);
    }
}
