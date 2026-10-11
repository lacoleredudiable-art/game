using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Arena;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Composition.Builders
{
    /// <summary>
    /// Dilim arenası görselleri — yalnız kural-motoru-v4.json "slice_arena_visuals"tan okunur.
    /// Sütun/koza modelleri mevcut çarpışma gövdelerinin görsel çocuğu olur (collider/gövde/hedef bileşenleri
    /// dokunulmaz, ilkel renderer kapanır). Kaburga, köy, ocak, rün taşı ve zemin koru ayrı "ArenaVisuals"
    /// kökünde; hiçbirinde Collider yok, katman fizik sorgularına girmez.
    /// </summary>
    public static class RuleEngineV4SliceArenaVisualsBuilder
    {
        public static void Build(Transform host, Transform columns, Transform cocoons, RuleEngineV4SliceArenaVisuals spec)
        {
            if (spec == null || !spec.Enabled)
                return;
            var registry = AssetLoader.Load<ArenaVisualRegistry>(RuleEngineV4SliceArenaVisualsBuilderDefaults.RegistryResource, null);
            if (registry == null)
                return;

            DressBodies(columns, registry.Find(spec.ColumnModel), spec.ColumnModel);
            DressBodies(cocoons, registry.Find(spec.CocoonModel), spec.CocoonModel);

            var root = new GameObject(RuleEngineV4SliceArenaVisualsBuilderDefaults.RootName);
            root.transform.SetParent(host, false);
            BuildRibs(root.transform, registry, spec.Ribs);
            BuildVillage(root.transform, registry, spec);
            if (spec.HasRuneStone)
                Place(root.transform, registry.Find(spec.RuneStoneModel), "RuneStone", spec.RuneStone);
            if (spec.HasFloorGlow && registry.FloorGlow != null)
                FlatQuad(root.transform, "FloorGlow", Vector3.up * spec.FloorGlowLiftM, spec.FloorGlowRadiusM, registry.FloorGlow);
            SetLayer(root.transform);
        }

        /// <summary>Model, gövdenin tabanına dünya ölçeği 1 ile oturur; gövde ölçeği çocukta geri alınır.</summary>
        static void DressBodies(Transform group, ArenaVisualRegistry.ModelEntry entry, string key)
        {
            if (group == null || entry == null)
                return;
            foreach (Transform body in group)
            {
                var bodyRenderer = body.GetComponent<Renderer>();
                float baseY = bodyRenderer != null ? bodyRenderer.bounds.min.y : body.position.y;
                var holder = new GameObject(RuleEngineV4SliceArenaVisualsBuilderDefaults.VisualChildPrefix + key);
                holder.transform.SetParent(body, false);
                Vector3 s = body.lossyScale;
                holder.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
                holder.transform.position = new Vector3(body.position.x, baseY, body.position.z);
                holder.transform.rotation = Quaternion.identity;
                Spawn(entry, holder.transform);
                SetLayer(holder.transform);
                if (bodyRenderer != null)
                    bodyRenderer.enabled = false;
            }
        }

        static void BuildRibs(Transform root, ArenaVisualRegistry registry, RuleEngineV4SliceArenaRibs ribs)
        {
            ArenaVisualRegistry.ModelEntry lod0 = registry.Find(ribs.Model);
            if (lod0 == null)
                return;
            if (ribs.Colliders)
                Debug.LogWarning("[arena] slice_arena_visuals.ribs.colliders=true desteklenmez; kemerler çarpışmasız kurulur");
            ArenaVisualRegistry.ModelEntry lod1 = registry.Find(ribs.Lod1Model);
            var ribsRoot = new GameObject("Ribs");
            ribsRoot.transform.SetParent(root, false);
            IReadOnlyList<RuleEngineV4SliceArenaPlacement> poses = ribs.Layout();
            for (int i = 0; i < poses.Count; i++)
            {
                Transform rib = Place(ribsRoot.transform, null, $"Rib{i}", poses[i]);
                Renderer[] near = Spawn(lod0, rib).GetComponentsInChildren<Renderer>();
                Renderer[] far = lod1 != null ? Spawn(lod1, rib).GetComponentsInChildren<Renderer>() : near;
                AddLodGroup(rib, near, far, ribs);
            }
        }

        static void AddLodGroup(Transform rib, Renderer[] near, Renderer[] far, RuleEngineV4SliceArenaRibs ribs)
        {
#if !SWEEP_HEADLESS
            var group = rib.gameObject.AddComponent<LODGroup>();
            group.SetLODs(new[]
            {
                new LOD(ribs.Lod1ScreenHeight, near),
                new LOD(ribs.CullScreenHeight, far),
            });
            group.RecalculateBounds();
#endif
        }

        static void BuildVillage(Transform root, ArenaVisualRegistry registry, RuleEngineV4SliceArenaVisuals spec)
        {
            var village = new GameObject("Village");
            village.transform.SetParent(root, false);
            ArenaVisualRegistry.ModelEntry wall = registry.Find(spec.WallModel);
            for (int i = 0; i < spec.Walls.Count; i++)
                Place(village.transform, wall, $"Wall{i}", spec.Walls[i]);

            RuleEngineV4SliceArenaHearth h = spec.Hearth;
            if (!h.Enabled)
                return;
            Transform hearth = Place(village.transform, null, "Hearth", new RuleEngineV4SliceArenaPlacement(h.XM, h.ZM, 0f, 1f));
            if (registry.HearthEmbers != null)
                FlatQuad(hearth, "Embers", Vector3.up * RuleEngineV4SliceArenaVisualsBuilderDefaults.HearthEmberLiftM, h.EmberRadiusM, registry.HearthEmbers);
            ArenaVisualRegistry.ModelEntry stone = registry.Find(h.StoneModel);
            for (int i = 0; i < h.StoneCount; i++)
            {
                float deg = i * 360f / Mathf.Max(1, h.StoneCount);
                Vector3 offset = Quaternion.Euler(0f, deg, 0f) * Vector3.forward * h.StoneRingM;
                Place(hearth, stone, $"Stone{i}", new RuleEngineV4SliceArenaPlacement(offset.x, offset.z, deg, h.StoneScale));
            }
        }

        static Transform Place(Transform parent, ArenaVisualRegistry.ModelEntry entry, string name, RuleEngineV4SliceArenaPlacement p)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(p.XM, 0f, p.ZM);
            go.transform.localRotation = Quaternion.Euler(0f, p.YawDeg, 0f);
            go.transform.localScale = Vector3.one * p.Scale;
            if (entry != null)
                Spawn(entry, go.transform);
            return go.transform;
        }

        /// <summary>FBX kökünün içe aktarma dönüşü/ölçeği korunur (Instantiate yerel değerleri taşır).</summary>
        static GameObject Spawn(ArenaVisualRegistry.ModelEntry entry, Transform parent)
        {
            GameObject model = Object.Instantiate(entry.Model, parent, false);
            model.name = entry.Model.name;
            if (entry.KorOverlay != null)
            {
                foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
                {
                    Material[] body = r.sharedMaterials;
                    var mats = new Material[body.Length + 1];
                    body.CopyTo(mats, 0);
                    mats[body.Length] = entry.KorOverlay;
                    r.sharedMaterials = mats;
                }
            }
            return model;
        }

        static void FlatQuad(Transform parent, string name, Vector3 localPos, float radiusM, Material material)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = localPos;
            quad.transform.localRotation = Quaternion.Euler(RuleEngineV4SliceArenaVisualsBuilderDefaults.FlatQuadPitchDeg, 0f, 0f);
            quad.transform.localScale = new Vector3(radiusM * 2f, radiusM * 2f, 1f);
            var renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void SetLayer(Transform t)
        {
            t.gameObject.layer = RuleEngineV4SliceArenaVisualsBuilderDefaults.VisualLayer;
            foreach (Transform c in t)
                SetLayer(c);
        }
    }
}
