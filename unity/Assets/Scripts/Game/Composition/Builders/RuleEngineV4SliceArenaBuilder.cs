using Dovus.Core.Actors;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using Dovus.Game.Actors;
using Dovus.Game.Skills.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Composition.Builders
{
    /// <summary>v4 dilim arena: sütunlar, dış duvar (fizik), 18–22 m görsel halka, kozalar.</summary>
    public static class RuleEngineV4SliceArenaBuilder
    {
        static int _nextCocoonActor = RuleEngineV4SliceArenaBuilderDefaults.CocoonActorIdStart;

        public static void BuildIfEnabled(WorldContext ctx, RuleEngineV4SliceArena arena)
        {
            if (ctx.Tuning == null || !ctx.Tuning.RuleEngineV4.SliceScene || arena == null)
                return;

            var root = new GameObject("RuleEngineV4SliceArena");
            root.transform.SetParent(ctx.Host.transform, false);

            BuildColumns(root.transform, arena);
            BuildOuterWall(root.transform, arena);
            BuildVisualRing(root.transform, arena);
            BuildCocoons(ctx, root.transform, arena);
        }

        static void BuildColumns(Transform parent, RuleEngineV4SliceArena arena)
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
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = new Color(0.45f, 0.42f, 0.38f);
            }
        }

        static void BuildOuterWall(Transform parent, RuleEngineV4SliceArena arena)
        {
            var wallRoot = new GameObject("OuterWallCollision");
            wallRoot.transform.SetParent(parent, false);
            int segments = Mathf.Max((int)RuleEngineV4SliceArenaBuilderDefaults.MinWallSegmentCount, arena.OuterWallSegments);
            float radius = arena.OuterWallCollisionRadiusM;
            float height = Mathf.Max(RuleEngineV4SliceArenaBuilderDefaults.MinWallHeightM, arena.OuterWallHeightM);
            float arc = 2f * Mathf.PI * radius / segments;
            float thickness = Mathf.Max(
                RuleEngineV4SliceArenaBuilderDefaults.WallThicknessMinM,
                arc * RuleEngineV4SliceArenaBuilderDefaults.WallThicknessArcScale);
            float depth = thickness * RuleEngineV4SliceArenaBuilderDefaults.WallSegmentDepthScale;
            // Dış köşeler tam çarpışma yarıçapında: segmentler boşluksuz birleşir, 18 m dışına taşmaz.
            float halfStep = Mathf.PI / segments;
            float chord = 2f * radius * Mathf.Sin(halfStep);
            float centerRadius = radius * Mathf.Cos(halfStep) - depth * 0.5f;
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
                var renderer = seg.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        static void BuildVisualRing(Transform parent, RuleEngineV4SliceArena arena)
        {
            float inner = arena.OuterWallVisualInnerRadiusM;
            float outer = arena.OuterWallVisualOuterRadiusM;
            if (outer <= inner)
                return;

            var ringRoot = new GameObject("OuterWallVisual");
            ringRoot.transform.SetParent(parent, false);
            int segments = Mathf.Max((int)RuleEngineV4SliceArenaBuilderDefaults.MinVisualRingSegments, arena.OuterWallSegments);
            float midR = (inner + outer) * 0.5f;
            float width = outer - inner;
            float y = RuleEngineV4SliceArenaBuilderDefaults.VisualRingLiftM;
            var lr = ringRoot.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = segments;
            lr.widthMultiplier = width;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(0.35f, 0.55f, 0.75f, 0.35f);
            lr.endColor = lr.startColor;
            var pts = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * (2f * Mathf.PI / segments);
                pts[i] = new Vector3(Mathf.Sin(angle) * midR, y, Mathf.Cos(angle) * midR);
            }
            lr.SetPositions(pts);
        }

        static void BuildCocoons(WorldContext ctx, Transform parent, RuleEngineV4SliceArena arena)
        {
            var cocoRoot = new GameObject("Cocoons");
            cocoRoot.transform.SetParent(parent, false);
            for (int i = 0; i < arena.Cocoons.Count; i++)
            {
                RuleEngineV4SliceArenaCocoon spec = arena.Cocoons[i];
                float r = Mathf.Max(RuleEngineV4SliceArenaBuilderDefaults.MinCocoonRadiusFloorM, spec.RadiusM);
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"ArenaCocoon{i + 1}";
                go.transform.SetParent(cocoRoot.transform, false);
                go.transform.position = new Vector3(spec.XM, r, spec.ZM);
                go.transform.localScale = Vector3.one * (r * 2f);
                var col = go.GetComponent<SphereCollider>();
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
        }
    }
}
