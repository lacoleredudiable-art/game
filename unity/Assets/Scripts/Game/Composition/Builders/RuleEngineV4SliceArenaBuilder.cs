using Dovus.Core.Actors;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using Dovus.Game.Actors;
using Dovus.Game.Skills.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Composition.Builders
{
    /// <summary>v4 dilim arena: sütunlar, dış duvar (fizik), 18–22 m görsel duvar ve duvar ötesi zemin, kozalar.</summary>
    public static class RuleEngineV4SliceArenaBuilder
    {
        static int _nextCocoonActor = RuleEngineV4SliceArenaBuilderDefaults.CocoonActorIdStart;

        public static void BuildIfEnabled(WorldContext ctx, RuleEngineV4SliceArena arena, RuleEngineV4SliceArenaVisuals visuals)
        {
            if (ctx.Tuning == null || !ctx.Tuning.RuleEngineV4.SliceScene || arena == null)
                return;

            var root = new GameObject("RuleEngineV4SliceArena");
            root.transform.SetParent(ctx.Host.transform, false);

            Transform columns = BuildColumns(root.transform, arena);
            BuildOuterWall(root.transform, arena);
            RuleEngineV4SliceArenaShellBuilder.BuildWall(root.transform, arena);
            RuleEngineV4SliceArenaShellBuilder.BuildBeyondGround(root.transform, (visuals ?? RuleEngineV4SliceArenaVisuals.Disabled).Beyond,
                arena.OuterWallVisualOuterRadiusM);
            Transform cocoons = BuildCocoons(ctx, root.transform, arena);
            RuleEngineV4SliceArenaVisualsBuilder.Build(ctx.Host.transform, columns, cocoons, visuals);
        }

        static Transform BuildColumns(Transform parent, RuleEngineV4SliceArena arena)
        {
            var columnsRoot = new GameObject("Columns");
            columnsRoot.transform.SetParent(parent, false);
            for (int i = 0; i < arena.Columns.Count; i++)
            {
                RuleEngineV4SliceArenaColumn col = arena.Columns[i];
                float r = Mathf.Max(RuleEngineV4SliceArenaBuilderDefaults.MinColumnRadiusFloorM, col.RadiusM);
                float h = Mathf.Max(RuleEngineV4SliceArenaBuilderDefaults.MinColumnHeightFloorM, col.HeightM);
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = $"ArenaColumn{i + 1}";
                go.transform.SetParent(columnsRoot.transform, false);
                go.transform.position = new Vector3(col.XM, h * 0.5f, col.ZM);
                go.transform.localScale = new Vector3(r * 2f, h * 0.5f, r * 2f);
                var collider = go.GetComponent<Collider>();
                if (collider != null)
                    collider.isTrigger = false;
                var body = go.AddComponent<RuleEngineV4PhysicsBodyHost>();
                body.WeightTier = RuleEngineV4WeightTier.Anchored;
                body.BodyRadiusM = r;
                go.AddComponent<RuleEngineV4MarkOnlySurfaceHost>();
                AddCameraBlocker(go.transform);
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = new Color(0.45f, 0.42f, 0.38f);
            }
            return columnsRoot.transform;
        }

        /// <summary>
        /// Unity'nin Cylinder ilkeli silindir değil kapsül çarpışıcı taşır: tepe ve dip yarım küre olduğundan görünen
        /// sütun (tepesi 1 m yarıçapa açılır) kamera taramasına göre dışarıda kalıyordu (teşhis 7, O-5.10).
        /// Gerçek silindir (yarıçap r, boy h; ölçek ebeveynden gelir) çocuk olarak eklenir; oyuncu çarpışıcısı ve ilkelin kendisi değişmez.
        /// </summary>
        static void AddCameraBlocker(Transform column)
        {
            var child = new GameObject("CameraBlocker");
            child.transform.SetParent(column, false);
            int layer = LayerMask.NameToLayer("CameraBlocker");
            child.layer = layer >= 0 ? layer : 0;
            // Ana ilkelin ölçeği (2r, h/2, 2r) ve boyu 2 birim: yerel birim silindir (yarıçap 0,5, boy 2) tam r×h verir.
            var mesh = new Mesh { name = "CameraBlockerCylinder" };
            int n = RuleEngineV4SliceArenaBuilderDefaults.ColumnBlockerSegments;
            var verts = new Vector3[n * 2];
            var tris = new int[(n - 2) * 3 * 2 + n * 6];
            for (int i = 0; i < n; i++)
            {
                float a = i * (2f * Mathf.PI / n);
                float x = Mathf.Cos(a) * 0.5f;
                float z = Mathf.Sin(a) * 0.5f;
                verts[i] = new Vector3(x, -1f, z);
                verts[n + i] = new Vector3(x, 1f, z);
            }
            int t = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                tris[t++] = i; tris[t++] = n + i; tris[t++] = j;
                tris[t++] = j; tris[t++] = n + i; tris[t++] = n + j;
            }
            for (int i = 1; i < n - 1; i++)
            {
                tris[t++] = 0; tris[t++] = i + 1; tris[t++] = i;
                tris[t++] = n; tris[t++] = n + i; tris[t++] = n + i + 1;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            var mc = child.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
        }

        static void BuildOuterWall(Transform parent, RuleEngineV4SliceArena arena)
        {
            var wallRoot = new GameObject("OuterWallCollision");
            wallRoot.transform.SetParent(parent, false);
            var geometry = new RuleEngineV4SliceArenaWall(arena);
            int segments = geometry.Segments;
            float height = Mathf.Max(RuleEngineV4SliceArenaBuilderDefaults.MinWallHeightM, arena.OuterWallHeightM);
            float thickness = geometry.ThicknessM;
            float depth = geometry.DepthM;
            float chord = geometry.ChordM;
            float centerRadius = geometry.CenterRadiusM;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * (2f * Mathf.PI / segments);
                Vector3 pos = new Vector3(Mathf.Sin(angle) * centerRadius, height * 0.5f, Mathf.Cos(angle) * centerRadius);
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = $"WallSeg{i}";
                seg.transform.SetParent(wallRoot.transform, false);
                seg.transform.position = pos;
                seg.transform.rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
                seg.transform.localScale = new Vector3(chord, height, depth);
                var col = seg.GetComponent<Collider>();
                if (col != null)
                    col.isTrigger = false;
                var body = seg.AddComponent<RuleEngineV4PhysicsBodyHost>();
                body.WeightTier = RuleEngineV4WeightTier.Anchored;
                body.BodyRadiusM = thickness * 0.5f;
                seg.AddComponent<RuleEngineV4MarkOnlySurfaceHost>();
                var renderer = seg.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        static Transform BuildCocoons(WorldContext ctx, Transform parent, RuleEngineV4SliceArena arena)
        {
            var cocoRoot = new GameObject("Cocoons");
            cocoRoot.transform.SetParent(parent, false);
            for (int i = 0; i < arena.Cocoons.Count; i++)
            {
                RuleEngineV4SliceArenaCocoon spec = arena.Cocoons[i];
                float r = Mathf.Max(RuleEngineV4SliceArenaBuilderDefaults.MinCocoonRadiusFloorM, spec.RadiusM);
                // Kapsül çapından kısa olamaz.
                float h = Mathf.Max(r * 2f, spec.HeightM);
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = $"ArenaCocoon{i + 1}";
                go.transform.SetParent(cocoRoot.transform, false);
                go.transform.position = new Vector3(spec.XM, h * 0.5f, spec.ZM);
                // Birincil kapsül: yarıçap 0,5, yükseklik 2 (yerel).
                go.transform.localScale = new Vector3(r * 2f, h * 0.5f, r * 2f);
                var col = go.GetComponent<CapsuleCollider>();
                col.isTrigger = true;
                var body = go.AddComponent<RuleEngineV4PhysicsBodyHost>();
                body.WeightTier = RuleEngineV4WeightTier.Anchored;
                body.BodyRadiusM = r;
                var cocoon = go.AddComponent<SliceArenaCocoonHost>();
                var actorId = new ActorId((_nextCocoonActor++).ToString());
                cocoon.Configure(actorId, spec.MaxHp);
                var target = go.AddComponent<TargetableHost>();
                target.BindLiveRegistry(ctx.Runtime.Targetables);
                target.Configure(1, go.name, actorId, () => !cocoon.IsBroken);
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = new Color(0.65f, 0.5f, 0.85f, 0.9f);
            }
            return cocoRoot.transform;
        }
    }
}
