using System.IO;
using Dovus.Game.Composition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// POLYGON Dungeon Pack → GameBootstrapHost görselleri.
    /// Menu: Dovus/Synty/Bind Player Boss
    /// </summary>
    public static class SyntyVisualBind
    {
        const string PlayerSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Characters/SM_Chr_Hero_Knight_Male_01.prefab";
        const string BossSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Characters/SM_Chr_Rock_Golem_01.prefab";

        const string OutDir = "Assets/Art/Synty/Prefabs";
        const string PlayerOut = OutDir + "/PlayerVisual_Synty.prefab";
        const string BossOut = OutDir + "/BossVisual_Synty.prefab";
        const string ScenePath = "Assets/Scenes/Prototype.unity";

        [MenuItem("Dovus/Synty/Bind Player Boss")]
        public static void BindAll()
        {
            if (!File.Exists(PlayerSrc) || !File.Exists(BossSrc))
            {
                Debug.LogError("[SyntyBind] POLYGON prefab bulunamadı — unitypackage import edildi mi?");
                return;
            }

            if (!Directory.Exists(OutDir))
                Directory.CreateDirectory(OutDir);

            var playerSrc = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSrc);
            var bossSrc = AssetDatabase.LoadAssetAtPath<GameObject>(BossSrc);

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

            var playerPrefab = PrefabUtility.SaveAsPrefabAsset(playerWrap, PlayerOut);
            var bossPrefab = PrefabUtility.SaveAsPrefabAsset(bossWrap, BossOut);
            Object.DestroyImmediate(playerWrap);
            Object.DestroyImmediate(bossWrap);

            AssignBootstrap(playerPrefab, bossPrefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[SyntyBind] Player/Boss bağlandı (mesh prune).");
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

        static void AssignBootstrap(GameObject player, GameObject boss)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameBootstrapHost bootstrap = Object.FindAnyObjectByType<GameBootstrapHost>();
            if (bootstrap == null)
            {
                Debug.LogError("[SyntyBind] GameBootstrapHost yok: " + ScenePath);
                return;
            }

            var so = new SerializedObject(bootstrap);
            so.FindProperty("_playerVisualPrefab").objectReferenceValue = player;
            so.FindProperty("_bossVisualPrefab").objectReferenceValue = boss;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
