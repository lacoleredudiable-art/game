using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dovus.Game.EditorTools
{
    /// <summary>
    /// POLYGON Demo'daki Long_Hall / Center_Hall → PrototypeBootstrap.
    /// Menu: Dovus/Synty/Bind Demo Hall Arena
    /// </summary>
    public static class SyntyDemoHallBind
    {
        const string DemoScene = "Assets/Synty/PolygonDungeon/Scenes/Demo.unity";
        const string ProtoScene = "Assets/Scenes/Prototype.unity";
        const string OutDir = "Assets/Art/Synty/Prefabs";
        const string ArenaOut = OutDir + "/ArenaVisual_Synty.prefab";
        const string PlayerOut = OutDir + "/PlayerVisual_Synty.prefab";
        const string BossOut = OutDir + "/BossVisual_Synty.prefab";

        const string PlayerSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Characters/SM_Chr_Hero_Knight_Male_01.prefab";
        const string BossSrc =
            "Assets/Synty/PolygonDungeon/Prefabs/Characters/SM_Chr_Rock_Golem_01.prefab";

        // Promo görseline en yakın salon.
        const string HallRootName = "Long_Hall";

        [MenuItem("Dovus/Synty/Bind Demo Hall Arena")]
        public static void BindDemoHall()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SyntyHall] Play mode'da çalışmaz — durdur.");
                return;
            }

            if (!File.Exists(DemoScene))
            {
                Debug.LogError("[SyntyHall] Demo.unity yok.");
                return;
            }

            if (!Directory.Exists(OutDir))
                Directory.CreateDirectory(OutDir);

            // Karakter wrapper'ları (atlas prune).
            EnsureCharacterPrefabs();

            var demo = EditorSceneManager.OpenScene(DemoScene, OpenSceneMode.Single);
            GameObject hallSrc = null;
            GameObject lightsSrc = null;
            foreach (var go in demo.GetRootGameObjects())
            {
                if (go.name == HallRootName)
                    hallSrc = go;
                if (go.name == "Lights - URP")
                    lightsSrc = go;
            }

            if (hallSrc == null)
            {
                Debug.LogError("[SyntyHall] " + HallRootName + " bulunamadı.");
                return;
            }

            var wrapper = new GameObject("ArenaVisual_Synty");
            var hall = Object.Instantiate(hallSrc);
            hall.name = HallRootName;
            hall.transform.SetParent(wrapper.transform, true);

            // Salon merkezini origin'e, zemini y≈0'a al.
            Bounds b = ComputeBounds(hall);
            Vector3 shift = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            hall.transform.position += shift;

            if (lightsSrc != null)
            {
                var lights = Object.Instantiate(lightsSrc);
                lights.name = "HallLights_URP";
                lights.transform.SetParent(wrapper.transform, true);
                lights.transform.position += shift;
                // BIRP ışıklarını kapat — URP projede.
                StripNonLightNoise(lights);
            }

            // Fog plane'ler demo'ya özel; arena'da siyahlık yapmasın.
            // (Hall içinde değil.)

            var arenaPrefab = PrefabUtility.SaveAsPrefabAsset(wrapper, ArenaOut);
            Object.DestroyImmediate(wrapper);

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerOut);
            var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossOut);
            AssignBootstrap(playerPrefab, bossPrefab, arenaPrefab);

            EditorSceneManager.OpenScene(ProtoScene, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            Debug.Log("[SyntyHall] Long_Hall → ArenaVisual_Synty bağlandı. Bounds was " + b.size);
        }

        static void EnsureCharacterPrefabs()
        {
            if (File.Exists(PlayerOut) && File.Exists(BossOut))
                return;

            // Sadece karakter — BindAll arenasını ezmesin.
            if (!Directory.Exists(OutDir))
                Directory.CreateDirectory(OutDir);

            var playerSrc = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSrc);
            var bossSrc = AssetDatabase.LoadAssetAtPath<GameObject>(BossSrc);
            if (playerSrc == null || bossSrc == null)
            {
                Debug.LogError("[SyntyHall] Karakter prefab kaynağı yok.");
                return;
            }

            // SyntyVisualBind public API yoksa burada minimal prune.
            SyntyVisualBind.BindAll(); // geçici: karakter+arena; hemen Long_Hall ile arena üzerine yazılır.
        }

        static Bounds ComputeBounds(GameObject root)
        {
            var rends = root.GetComponentsInChildren<Renderer>(true);
            Bounds b = default;
            bool has = false;
            foreach (var r in rends)
            {
                if (!has)
                {
                    b = r.bounds;
                    has = true;
                }
                else
                    b.Encapsulate(r.bounds);
            }
            return has ? b : new Bounds(root.transform.position, Vector3.one * 20f);
        }

        static void StripNonLightNoise(GameObject lightsRoot)
        {
            // Sadece Light component'li objeler kalsın diye gerek yok —
            // Synty lights hierarchy Light içerir. Disabled light'ları aç.
            foreach (var light in lightsRoot.GetComponentsInChildren<Light>(true))
            {
                light.enabled = true;
                if (light.type == LightType.Point || light.type == LightType.Spot)
                {
                    // Salon dışı ışıkları zayıf bırakma — intensity düşükse yükseltme.
                    if (light.intensity < 0.2f)
                        light.intensity = 1.2f;
                }
            }
        }

        static void AssignBootstrap(GameObject player, GameObject boss, GameObject arena)
        {
            var scene = EditorSceneManager.OpenScene(ProtoScene, OpenSceneMode.Single);
            var bootstrap = Object.FindAnyObjectByType<PrototypeBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError("[SyntyHall] PrototypeBootstrap yok.");
                return;
            }

            var so = new SerializedObject(bootstrap);
            if (player != null)
                so.FindProperty("_playerVisualPrefab").objectReferenceValue = player;
            if (boss != null)
                so.FindProperty("_bossVisualPrefab").objectReferenceValue = boss;
            so.FindProperty("_arenaVisualPrefab").objectReferenceValue = arena;

            var tuning = so.FindProperty("_tuning");
            if (tuning != null)
            {
                var scale = tuning.FindPropertyRelative("ArenaVisualScale");
                if (scale != null)
                    scale.floatValue = 1f;
                var half = tuning.FindPropertyRelative("ArenaHalfSizeM");
                // Long_Hall ~90 m uzun / ~38 m geniş — yürüyüş yarım kenarı geniş eksenden.
                if (half != null)
                    half.floatValue = 18f;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
