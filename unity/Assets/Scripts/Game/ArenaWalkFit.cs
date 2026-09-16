using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Quaternius arena mesh'inden yürünebilir yarım kenar çıkarır.
    /// Fizik collider yok (teknoloji §4) — KinematicMotor soft clamp buna bağlanır.
    /// </summary>
    public static class ArenaWalkFit
    {
        /// <summary>
        /// Modüler floor karolarında <c>extents</c> tek karo boyutudur (~3m);
        /// yürüyüş için karoların dünya kenarı (|min|/|max|) alınır.
        /// İç sütun/kemer clamp'i ezmez — duvar yalnız dış halkaya yakınsa.
        /// </summary>
        public static float FitHalfSizeM(GameObject arena, float fallbackHalf, float insetM = 0.35f)
        {
            if (arena == null)
                return Mathf.Max(3f, fallbackHalf);

            var rends = arena.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0)
                return Mathf.Max(3f, fallbackHalf);

            float floorEdge = 0f;
            float allEdge = 0f;
            float wallPosX = float.PositiveInfinity;
            float wallNegX = float.NegativeInfinity;
            float wallPosZ = float.PositiveInfinity;
            float wallNegZ = float.NegativeInfinity;
            bool anyFloor = false;
            bool anyWall = false;

            for (int i = 0; i < rends.Length; i++)
            {
                Renderer r = rends[i];
                if (r == null || !r.enabled)
                    continue;

                string n = r.gameObject.name.ToLowerInvariant();
                Bounds b = r.bounds;
                float edge = MaxAbsEdge(b);
                allEdge = Mathf.Max(allEdge, edge);

                if (n.Contains("floor") || n.Contains("tile") || n.Contains("ground"))
                {
                    anyFloor = true;
                    floorEdge = Mathf.Max(floorEdge, edge);
                }

                // Dış duvar — column/pedestal değil.
                if (n.Contains("wall") || (n.Contains("arch") && !n.Contains("column")))
                {
                    anyWall = true;
                    Vector3 c = b.center;
                    if (c.x > 2.5f)
                        wallPosX = Mathf.Min(wallPosX, b.min.x);
                    if (c.x < -2.5f)
                        wallNegX = Mathf.Max(wallNegX, b.max.x);
                    if (c.z > 2.5f)
                        wallPosZ = Mathf.Min(wallPosZ, b.min.z);
                    if (c.z < -2.5f)
                        wallNegZ = Mathf.Max(wallNegZ, b.max.z);
                }
            }

            float half = fallbackHalf;
            if (anyFloor && floorEdge > 1f)
                half = floorEdge;
            else if (allEdge > 1f)
                half = allEdge * 0.92f;

            if (anyWall
                && !float.IsInfinity(wallPosX) && !float.IsInfinity(wallNegX)
                && !float.IsInfinity(wallPosZ) && !float.IsInfinity(wallNegZ))
            {
                float fromWalls = Mathf.Min(
                    Mathf.Abs(wallPosX), Mathf.Abs(wallNegX),
                    Mathf.Abs(wallPosZ), Mathf.Abs(wallNegZ));
                // Dış halka: floor kenarının ≥%75'i. İç kemer ezmez.
                if (fromWalls > 2f && fromWalls >= half * 0.75f)
                    half = Mathf.Min(half, fromWalls);
            }

            half = Mathf.Max(8f, half - Mathf.Max(0.1f, insetM));
            return half;
        }

        static float MaxAbsEdge(Bounds b) =>
            Mathf.Max(
                Mathf.Abs(b.max.x), Mathf.Abs(b.min.x),
                Mathf.Abs(b.max.z), Mathf.Abs(b.min.z));
    }
}
