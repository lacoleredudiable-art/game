using System.Collections.Generic;

namespace UnityEngine
{
    public partial class Mesh
    {
        public MeshTopology GetTopology(int submesh) => MeshTopology.Triangles;

        public void GetTriangles(List<int> tris, int submesh)
        {
            tris.Clear();
            tris.AddRange(triangles);
        }
    }

    public partial class LineRenderer
    {
        public void BakeMesh(Mesh mesh, Camera camera, bool useTransform) { }
    }

    public partial class TrailRenderer
    {
        public void BakeMesh(Mesh mesh, Camera camera, bool useTransform) { }
    }
}
