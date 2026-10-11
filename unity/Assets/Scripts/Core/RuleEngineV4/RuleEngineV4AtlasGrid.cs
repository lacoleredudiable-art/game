using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Atlas içinde ızgara bölge; hücre 0 = sol üst, satır satır.</summary>
    public sealed class RuleEngineV4AtlasGrid
    {
        public int XPx { get; private set; }
        public int YPx { get; private set; }
        public int Cols { get; private set; }
        public int Rows { get; private set; }
        public int CellPx { get; private set; }
        public int Count => Cols * Rows;
        public int WidthPx => Cols * CellPx;
        public int HeightPx => Rows * CellPx;

        public RuleEngineV4AtlasGrid(int xPx, int yPx, int cols, int rows, int cellPx)
        {
            XPx = xPx;
            YPx = yPx;
            Cols = cols;
            Rows = rows;
            CellPx = cellPx;
        }

        public static RuleEngineV4AtlasGrid FromJson(JsonValue n, RuleEngineV4AtlasGrid fallback)
        {
            if (n.Kind != JsonKind.Object)
                return fallback;
            return new RuleEngineV4AtlasGrid(
                n["x_px"].AsInt(fallback.XPx),
                n["y_px"].AsInt(fallback.YPx),
                n["cols"].AsInt(fallback.Cols),
                n["rows"].AsInt(fallback.Rows),
                n["cell_px"].AsInt(fallback.CellPx));
        }

        /// <summary>Hücre dizini sınırlanır (0…Count−1).</summary>
        public RuleEngineV4UvRect Cell(int index, int atlasPx)
        {
            int last = Count - 1;
            int idx = index < 0 ? 0 : index > last ? last : index;
            int col = Cols > 0 ? idx % Cols : 0;
            int row = Cols > 0 ? idx / Cols : 0;
            return RuleEngineV4UvRect.FromTopPixels(XPx + col * CellPx, YPx + row * CellPx, CellPx, CellPx, atlasPx);
        }
    }
}
