using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game
{
    /// <summary>
    /// Ambiyans portu (PR #43 "deneme sahnesi" → ana dövüş sahnesi, bkz. AGENTS kural 2/3):
    /// sahne koddan kurulur, bu yüzden gerçek sanat varlıkları (<see cref="CombatAmbienceAssets"/>,
    /// bir Resources asset'i) çalışma anında yüklenip örneklenir. Açık gri lav/bazalt ovası +
    /// ufukta sisli dağlar + oyun alanı kenarında kayalar (collider'sız, duvar dışında).
    /// Varlık bulunamazsa (ör. headless/CI veya asset henüz üretilmemiş) eski prosedürel
    /// <see cref="ArenaHorizon"/>'a geri döner — oynanış/collider'lara hiç dokunulmaz.
    /// </summary>
    public static class CombatAmbienceEnvironment
    {
        // Takip kamerası oyuncu duvara dayandığında geriye/omuza doğru ~5 m taşabilir
        // (CameraDistanceM + omuz ofseti). Kayalar kameranın hiç giremeyeceği kadar
        // dışarıda başlar — yoksa büyük bir kaya kamerayı yutup ekranı tek renge boyar.
        const float OuterRingMarginM = 14f;
        const float OuterRingDepthM = 14f;
        const int RockCount = 22;
        const float RockMinScale = 1.0f;
        const float RockMaxScale = 1.8f;

        public static void Build(GameObject arenaRoot, float walkHalfM, PrototypeTuning tuning)
        {
            if (arenaRoot == null)
                return;

            var assets = Resources.Load<CombatAmbienceAssets>(CombatAmbienceAssets.ResourcePath);
            if (assets == null)
            {
                // Asset henüz üretilmedi (ör. temiz checkout) ya da headless koşucu — eski ova.
                ArenaHorizon.Build(arenaRoot.transform, walkHalfM);
                return;
            }

            CircularArena.SetWallRenderersVisible(arenaRoot, false);
            if (assets.GroundMaterial != null)
                CircularArena.SetFloorMaterial(arenaRoot, assets.GroundMaterial);

            Vector3 center = arenaRoot.transform.position;
            // Yakın öğeler (zemin/sis) arena yarıçapıyla ölçeklenir; Ground_Far/Skyline zaten
            // ufka kadar büyük tasarlandı (147→2026→1280 m), onlar sabit kalır (native ölçek 1).
            float nearScale = Mathf.Max(0.1f, walkHalfM) / CombatAmbienceAssets.DesignBoundaryRadiusM;

            var root = new GameObject("CombatAmbience");
            root.transform.SetParent(arenaRoot.transform, false);
            root.transform.position = center;

            PlaceEnvMesh(assets.GroundNearModel, assets.GroundMaterial, root.transform, nearScale, "Ground_Near");
            PlaceEnvMesh(assets.GroundFarModel, assets.GroundMaterial, root.transform, 1f, "Ground_Far");
            PlaceEnvMesh(assets.SkylineModel, assets.SkylineMaterial, root.transform, 1f, "Skyline_Far");
            PlaceEnvMesh(assets.EdgeMistModel, assets.EdgeMistMaterial, root.transform, nearScale, "EdgeMist");

            if (assets.LavaCracksModel != null)
            {
                // Tek izinli renkli/sıcak öğe: lav çatlağı. Oyun alanının ortasına, zemin ölçeğinde.
                GameObject cracks = PlaceEnvMesh(
                    assets.LavaCracksModel, assets.LavaCracksMaterial, root.transform,
                    nearScale * 0.5f, "LavaCracks");
                if (cracks != null)
                    cracks.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            }

            BuildRockRing(assets, root.transform, walkHalfM, Mathf.Max(1f, tuning?.ArenaWallThicknessM ?? 1.4f));
        }

        static GameObject PlaceEnvMesh(GameObject model, Material mat, Transform parent, float scale, string name)
        {
            if (model == null)
                return null;
            var go = UnityEngine.Object.Instantiate(model, parent);
            go.name = name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * scale;
            SetMaterial(go, mat);
            SetShadowsOff(go);
            return go;
        }

        static void BuildRockRing(CombatAmbienceAssets assets, Transform parent, float walkHalfM, float wallThicknessM)
        {
            if (assets.Rocks == null || assets.Rocks.Length == 0)
                return;

            var rocksRoot = new GameObject("Rocks_Boundary");
            rocksRoot.transform.SetParent(parent, false);

            float innerR = walkHalfM + wallThicknessM * 0.5f + OuterRingMarginM;
            float outerR = innerR + OuterRingDepthM;
            // Sabit seed: başsız tarama (SweepV2) her koşuda aynı sahneyi kursun (determinizm).
            var rng = new System.Random(1337);

            for (int i = 0; i < RockCount; i++)
            {
                CombatAmbienceAssets.RockKind kind = assets.Rocks[i % assets.Rocks.Length];
                if (kind.Model == null)
                    continue;

                float t = (i + (float)rng.NextDouble() * 0.8f) / RockCount;
                float angle = t * Mathf.PI * 2f;
                float radius = Mathf.Lerp(innerR, outerR, (float)rng.NextDouble());
                var pos = new Vector3(Mathf.Sin(angle) * radius, -0.3f, Mathf.Cos(angle) * radius);

                var go = UnityEngine.Object.Instantiate(kind.Model, rocksRoot.transform);
                go.name = $"{kind.Model.name}_{i:00}";
                go.transform.localPosition = pos;
                go.transform.localRotation = Quaternion.Euler(
                    (float)(rng.NextDouble() * 10f - 5f),
                    (float)(rng.NextDouble() * 360f),
                    (float)(rng.NextDouble() * 10f - 5f));
                go.transform.localScale = Vector3.one * (RockMinScale + (float)rng.NextDouble() * (RockMaxScale - RockMinScale));
                SetMaterial(go, kind.Material);
                SetShadowsOff(go);
            }
        }

        static void SetMaterial(GameObject go, Material material)
        {
            if (go == null || material == null)
                return;
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = material;
                if (mats.Length == 0)
                    mats = new[] { material };
                r.sharedMaterials = mats;
            }
        }

        static void SetShadowsOff(GameObject go)
        {
            // T4: telefonda sahne öğeleri için realtime gölge yok — tek yönlü ışık + ucuz shader.
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }
    }
}
