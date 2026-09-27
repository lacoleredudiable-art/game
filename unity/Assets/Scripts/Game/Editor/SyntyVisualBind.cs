using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dovus.Game.EditorTools
{
    /// <summary>
    /// POLYGON Dungeon Pack → PrototypeBootstrap görselleri.
    /// Menu: Dovus/Synty/Bind Player Boss Arena
    /// </summary>
    public static class SyntyVisualBind
    {
        const string PlayerSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Characters/SM_Chr_Hero_Knight_Male_01.prefab";
        const string BossSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Characters/SM_Chr_Rock_Golem_01.prefab";
        const string FloorSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Environments/Optimized/SM_Env_Tiles_Texture_01.prefab";
        const string WallSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Environments/Optimized/SM_Env_Wall_01_Texture.prefab";
        const string PillarSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Environments/Pillars/SM_Env_Pillar_Round_01.prefab";
        const string TorchSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Props/SM_Prop_Torch_Ornate_01.prefab";
        const string CrateSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Props/SM_Prop_Crate_Wood_01.prefab";
        const string BrazierSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Props/SM_Prop_Brazier_01.prefab";
        const string BannerSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Weapons/SM_Wep_Banner_01.prefab";

        const string CeilingSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Environments/Walls/SM_Env_Ceiling_Stone_Flat_01.prefab";
        const string DoorSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Environments/Walls/SM_Env_DoorDouble_Flat_01.prefab";

        const string OutDir = "Assets/Art/Synty/Prefabs";
        const string PlayerOut = OutDir + "/PlayerVisual_Synty.prefab";
        const string BossOut = OutDir + "/BossVisual_Synty.prefab";
        const string ArenaOut = OutDir + "/ArenaVisual_Synty.prefab";
        const string ScenePath = "Assets/Scenes/Prototype.unity";

        // 6×6 karo × ~5 m → ~30 m oda (önceki 4×4 / 20 m'den geniş).
        const float TileM = 5f;
        const int HalfTiles = 3;

        [MenuItem("Dovus/Synty/Bind Player Boss Arena")]
        public static void BindAll()
        {
            if (!File.Exists(PlayerSrc) || !File.Exists(BossSrc) || !File.Exists(FloorSrc))
            {
                Debug.LogError("[SyntyBind] POLYGON prefab bulunamadı — unitypackage import edildi mi?");
                return;
            }

            if (!Directory.Exists(OutDir))
                Directory.CreateDirectory(OutDir);

            var playerSrc = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSrc);
            var bossSrc = AssetDatabase.LoadAssetAtPath<GameObject>(BossSrc);
            var floorSrc = AssetDatabase.LoadAssetAtPath<GameObject>(FloorSrc);
            var wallSrc = AssetDatabase.LoadAssetAtPath<GameObject>(WallSrc);
            var pillarSrc = AssetDatabase.LoadAssetAtPath<GameObject>(PillarSrc);
            var torchSrc = AssetDatabase.LoadAssetAtPath<GameObject>(TorchSrc);
            var crateSrc = AssetDatabase.LoadAssetAtPath<GameObject>(CrateSrc);
            var brazierSrc = AssetDatabase.LoadAssetAtPath<GameObject>(BrazierSrc);
            var bannerSrc = AssetDatabase.LoadAssetAtPath<GameObject>(BannerSrc);
            var ceilingSrc = AssetDatabase.LoadAssetAtPath<GameObject>(CeilingSrc);
            var doorSrc = AssetDatabase.LoadAssetAtPath<GameObject>(DoorSrc);

            // Synty character prefab'ları atlas: tüm mesh'ler aynı GO'da — yalnız hedefi açık bırak.
            GameObject playerWrap = WrapCharacter(
                "PlayerVisual_Synty", playerSrc,
                keepMeshSubstring: "Hero_Knight_Male",
                keepItemSubstrings: new[] { "Sword", "Shield" },
                feetY: 0f,
                scale: 1f);
            GameObject bossWrap = WrapCharacter(
                "BossVisual_Synty", bossSrc,
                keepMeshSubstring: "Rock_Golem",
                keepItemSubstrings: null,
                feetY: 0f,
                scale: 1.35f);
            GameObject arena = BuildArena(
                floorSrc, wallSrc, pillarSrc, torchSrc, crateSrc, bannerSrc, brazierSrc,
                ceilingSrc, doorSrc);

            var playerPrefab = PrefabUtility.SaveAsPrefabAsset(playerWrap, PlayerOut);
            var bossPrefab = PrefabUtility.SaveAsPrefabAsset(bossWrap, BossOut);
            var arenaPrefab = PrefabUtility.SaveAsPrefabAsset(arena, ArenaOut);
            Object.DestroyImmediate(playerWrap);
            Object.DestroyImmediate(bossWrap);
            Object.DestroyImmediate(arena);

            AssignBootstrap(playerPrefab, bossPrefab, arenaPrefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[SyntyBind] Player/Boss/Arena bağlandı (mesh prune + zengin arena).");
        }

        static GameObject WrapCharacter(
            string name,
            GameObject src,
            string keepMeshSubstring,
            string[] keepItemSubstrings,
            float feetY,
            float scale)
        {
            var root = new GameObject(name);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(src);
            visual.name = "Mesh";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, feetY, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * scale;

            PruneSyntyCharacterAtlas(visual, keepMeshSubstring, keepItemSubstrings);

            var anim = visual.GetComponentInChildren<Animator>();
            if (anim != null && anim.runtimeAnimatorController == null)
                Debug.LogWarning("[SyntyBind] " + name + " Animator controller yok — Mixamo idle/cast sonra.");

            return root;
        }

        /// <summary>
        /// Synty Chr prefab'ı tek atlas mesh seti taşır; kullanılmayan Character_* / Item_* kapatılmazsa
        /// T-pose gri kalabalık + yanlış silüet görünür.
        /// </summary>
        static void PruneSyntyCharacterAtlas(GameObject root, string keepMesh, string[] keepItems)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                bool isCharacter = n.StartsWith("Character_");
                bool isItem = n.StartsWith("SM_Item_");
                if (!isCharacter && !isItem)
                    continue;

                bool keep = false;
                if (isCharacter && n.IndexOf(keepMesh, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    keep = true;
                if (isItem && keepItems != null)
                {
                    for (int i = 0; i < keepItems.Length; i++)
                    {
                        if (n.IndexOf(keepItems[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            keep = true;
                            break;
                        }
                    }
                }

                if (!keep)
                    t.gameObject.SetActive(false);
                else
                    t.gameObject.SetActive(true);
            }
        }

        static GameObject BuildArena(
            GameObject floorTile,
            GameObject wallPiece,
            GameObject pillar,
            GameObject torch,
            GameObject crate,
            GameObject banner,
            GameObject brazier,
            GameObject ceiling,
            GameObject door)
        {
            var root = new GameObject("ArenaVisual_Synty");
            float edge = HalfTiles * TileM; // 15 m — yürüyüş yarım kenarı ~14 m
            int wallCount = HalfTiles * 2; // 6 parça / kenar

            for (int x = -HalfTiles; x < HalfTiles; x++)
            {
                for (int z = -HalfTiles; z < HalfTiles; z++)
                {
                    var tileGo = (GameObject)PrefabUtility.InstantiatePrefab(floorTile);
                    tileGo.name = "Floor_" + x + "_" + z;
                    tileGo.transform.SetParent(root.transform, false);
                    tileGo.transform.localPosition = new Vector3(
                        x * TileM + TileM * 0.5f, 0f, z * TileM + TileM * 0.5f);
                }
            }

            // Kapalı oda: dört duvar (boşluk yok).
            if (wallPiece != null)
            {
                PlaceWallRow(root, wallPiece, "Wall_N", new Vector3(0f, 0f, edge), 0f, wallCount);
                PlaceWallRow(root, wallPiece, "Wall_S", new Vector3(0f, 0f, -edge), 180f, wallCount);
                PlaceWallRow(root, wallPiece, "Wall_E", new Vector3(edge, 0f, 0f), 90f, wallCount);
                PlaceWallRow(root, wallPiece, "Wall_W", new Vector3(-edge, 0f, 0f), -90f, wallCount);
            }

            // Kuzey ortada kapı nişi — “gerçek oda” hissi.
            if (door != null)
                Place(root, door, "Door_N", new Vector3(0f, 0f, edge - 0.05f), 180f);

            // Tavan — dışarı/void görünmesin.
            if (ceiling != null)
            {
                for (int x = -HalfTiles; x < HalfTiles; x++)
                {
                    for (int z = -HalfTiles; z < HalfTiles; z++)
                    {
                        var c = (GameObject)PrefabUtility.InstantiatePrefab(ceiling);
                        c.name = "Ceil_" + x + "_" + z;
                        c.transform.SetParent(root.transform, false);
                        c.transform.localPosition = new Vector3(
                            x * TileM + TileM * 0.5f, 0f, z * TileM + TileM * 0.5f);
                    }
                }
            }

            if (pillar != null)
            {
                float p = edge - 2.2f;
                Place(root, pillar, "Pillar_NE", new Vector3(p, 0f, p), 0f);
                Place(root, pillar, "Pillar_NW", new Vector3(-p, 0f, p), 0f);
                Place(root, pillar, "Pillar_SE", new Vector3(p, 0f, -p), 0f);
                Place(root, pillar, "Pillar_SW", new Vector3(-p, 0f, -p), 0f);
                // Ortaya yakın ek sütunlar — geniş salonu doldurur.
                Place(root, pillar, "Pillar_N", new Vector3(0f, 0f, p), 0f);
                Place(root, pillar, "Pillar_S", new Vector3(0f, 0f, -p), 0f);
            }

            if (torch != null)
            {
                float t = edge - 0.35f;
                Place(root, torch, "Torch_N", new Vector3(4f, 0f, t), 180f);
                Place(root, torch, "Torch_N2", new Vector3(-4f, 0f, t), 180f);
                Place(root, torch, "Torch_S", new Vector3(4f, 0f, -t), 0f);
                Place(root, torch, "Torch_S2", new Vector3(-4f, 0f, -t), 0f);
                Place(root, torch, "Torch_E", new Vector3(t, 0f, 4f), -90f);
                Place(root, torch, "Torch_W", new Vector3(-t, 0f, 4f), 90f);
            }

            if (brazier != null)
            {
                Place(root, brazier, "Brazier_A", new Vector3(5f, 0f, 5f), 0f);
                Place(root, brazier, "Brazier_B", new Vector3(-5f, 0f, 5f), 0f);
                Place(root, brazier, "Brazier_C", new Vector3(5f, 0f, -5f), 0f);
                Place(root, brazier, "Brazier_D", new Vector3(-5f, 0f, -5f), 0f);
            }

            if (crate != null)
            {
                Place(root, crate, "Crate_A", new Vector3(8f, 0f, 8f), 20f);
                Place(root, crate, "Crate_B", new Vector3(-7.5f, 0f, 6f), -35f);
                Place(root, crate, "Crate_C", new Vector3(7f, 0f, -8f), 55f);
                Place(root, crate, "Crate_D", new Vector3(-8f, 0f, -6.5f), -15f);
            }

            if (banner != null)
            {
                Place(root, banner, "Banner_N", new Vector3(-5f, 0f, edge - 0.15f), 180f);
                Place(root, banner, "Banner_N2", new Vector3(5f, 0f, edge - 0.15f), 180f);
            }

            return root;
        }

        static void Place(GameObject root, GameObject prefab, string name, Vector3 pos, float yaw)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        static void PlaceWallRow(GameObject root, GameObject wallPrefab, string prefix, Vector3 center, float yaw, int count)
        {
            float spacing = TileM;
            Vector3 along = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            for (int i = 0; i < count; i++)
            {
                float t = (i - (count - 1) * 0.5f) * spacing;
                Place(root, wallPrefab, prefix + "_" + i, center + along * t, yaw);
            }
        }

        static void AssignBootstrap(GameObject player, GameObject boss, GameObject arena)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PrototypeBootstrap bootstrap = Object.FindAnyObjectByType<PrototypeBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError("[SyntyBind] PrototypeBootstrap yok: " + ScenePath);
                return;
            }

            var so = new SerializedObject(bootstrap);
            so.FindProperty("_playerVisualPrefab").objectReferenceValue = player;
            so.FindProperty("_bossVisualPrefab").objectReferenceValue = boss;
            so.FindProperty("_arenaVisualPrefab").objectReferenceValue = arena;

            SerializedProperty tuning = so.FindProperty("_tuning");
            if (tuning != null)
            {
                SerializedProperty scale = tuning.FindPropertyRelative("ArenaVisualScale");
                if (scale != null)
                    scale.floatValue = 1f;
                SerializedProperty half = tuning.FindPropertyRelative("ArenaHalfSizeM");
                if (half != null)
                    half.floatValue = 14f; // geniş oda; FitHalf sonra mesh'ten düzeltir
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
