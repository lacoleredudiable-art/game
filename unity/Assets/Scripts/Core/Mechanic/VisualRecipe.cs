using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dovus.Core.Tuning;

namespace Dovus.Core.Mechanic
{
    /// <summary>
    /// Skill görseli = madde (fiil) × yol (silah teslimi) × silüet (sıfatın gövde kuralları).
    /// Madde yalnız fiil kimliğidir; dizilim yalnız <see cref="MechanicBody"/>'den okunur.
    /// </summary>
    public sealed class VisualRecipe
    {
        public int Substance;
        public string Motion = string.Empty;
        public string Layout = string.Empty;
        public double PieceSizeM;
        public double PieceMoveSec;
        public readonly List<VisualPiece> Pieces = new List<VisualPiece>();
        /// <summary>Çerçeve taşıyıcıyı izler; döngüde parçalar taşıyıcının o anki yerine bırakılır (iz).</summary>
        public bool Follow;
        public bool FollowOwner;
        public bool Tether;
        public bool Grow;
        public bool Loop;
        public double LoopEverySec;
        public double GroundRingRadiusM;
        /// <summary>BornAt'tan: gövde sahibin önünde doğuyorsa çerçeve bu kadar ileri kayar (yalnız merkezli dizilimler).</summary>
        public double BornAheadM;
        public readonly SortedSet<string> Traits = new SortedSet<string>(StringComparer.Ordinal);

        public string Signature() =>
            $"madde={Substance}|yol={Motion}|dizilim={Layout}|parca={Pieces.Count}|{string.Join(",", Traits)}";
    }
}
