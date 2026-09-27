using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>
    /// Assets/Art/Mixamo/*.fbx → Humanoid AnimatorController → Synty visuals.
    /// Menu: Dovus/Synty/Bind Mixamo Animator
    /// State isimleri AnimationBridge.MapToQuaterniusState ile uyumlu.
    /// </summary>
    public static class MixamoAnimatorBind
    {
        const string MixamoDir = "Assets/Art/Mixamo";
        const string OutDir = "Assets/Art/Mixamo/Animators";
        const string PlayerCtrl = OutDir + "/Player_Synty.controller";
        const string BossCtrl = OutDir + "/Boss_Synty.controller";
        const string PlayerVisual = "Assets/Art/Synty/Prefabs/PlayerVisual_Synty.prefab";
        const string BossVisual = "Assets/Art/Synty/Prefabs/BossVisual_Synty.prefab";

        [MenuItem("Dovus/Synty/Bind Mixamo Animator")]
        public static void Bind()
        {
            EnsureFolder(OutDir);
            ForceHumanoidOnMixamoFbxs();

            var clips = CollectClips();
            if (clips.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Mixamo yok",
                    "Assets/Art/Mixamo/ altına FBX koy (INDIR.txt listesi).\n" +
                    "Format: FBX for Unity, Without Skin, 30 fps.",
                    "Tamam");
                return;
            }

            AnimationClip idle = Pick(clips, "idle") ?? clips.Values.First();
            AnimationClip walk = Pick(clips, "walk", "walking") ?? idle;
            AnimationClip run = Pick(clips, "run", "running") ?? walk;
            AnimationClip dodge = Pick(clips, "roll", "dodge", "diving") ?? run;
            AnimationClip hit = Pick(clips, "hit", "reaction", "impact") ?? idle;
            AnimationClip death = Pick(clips, "death", "dying") ?? hit;
            // Dosya adıyla önce: Melee_Thrust / Melee_Slash / Melee_Punch / Spell_Cast / Dash
            AnimationClip pierce = Pick(clips, "melee_thrust", "thrust", "downward") ??
                                  Pick(clips, "attack") ?? idle;
            AnimationClip sweep = Pick(clips, "melee_slash", "horizontal", "slash") ?? pierce;
            AnimationClip slam = Pick(clips, "melee_punch", "punch", "hook") ?? sweep;
            AnimationClip channel = Pick(clips, "spell_cast", "spell", "magic", "casting") ?? idle;
            AnimationClip guard = Pick(clips, "dash", "roll", "dodge") ?? channel;
            AnimationClip strike = Pick(clips, "melee_thrust", "thrust") ?? pierce;

            BuildController(PlayerCtrl, idle, walk, run, dodge, hit, death,
                pierce, sweep, slam, channel, guard, strike);
            BuildController(BossCtrl, idle, walk, run, dodge, hit, death,
                pierce, sweep, slam, channel, guard, strike);

            AssignController(PlayerVisual, PlayerCtrl);
            AssignController(BossVisual, BossCtrl);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MixamoBind] OK — {clips.Count} clip, controllers → Synty visuals.");
        }

        static void ForceHumanoidOnMixamoFbxs()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { MixamoDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp == null)
                    continue;
                bool dirty = false;
                if (imp.animationType != ModelImporterAnimationType.Human)
                {
                    imp.animationType = ModelImporterAnimationType.Human;
                    dirty = true;
                }
                if (imp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    dirty = true;
                }
                if (!imp.importAnimation)
                {
                    imp.importAnimation = true;
                    dirty = true;
                }
                if (dirty)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        static Dictionary<string, AnimationClip> CollectClips()
        {
            var map = new Dictionary<string, AnimationClip>();
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { MixamoDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Animators/"))
                    continue;
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (o is not AnimationClip clip || clip.name.StartsWith("__preview__"))
                        continue;
                    string key = Normalize(Path.GetFileNameWithoutExtension(path) + " " + clip.name);
                    if (!map.ContainsKey(key))
                        map[key] = clip;
                }
            }
            return map;
        }

        static AnimationClip Pick(Dictionary<string, AnimationClip> clips, params string[] needles)
        {
            foreach (var kv in clips)
            {
                foreach (string n in needles)
                {
                    if (kv.Key.Contains(n))
                        return kv.Value;
                }
            }
            return null;
        }

        static string Normalize(string s) =>
            (s ?? "").ToLowerInvariant().Replace('_', ' ').Replace('-', ' ');

        static void BuildController(
            string path,
            AnimationClip idle, AnimationClip walk, AnimationClip run,
            AnimationClip dodge, AnimationClip hit, AnimationClip death,
            AnimationClip pierce, AnimationClip sweep, AnimationClip slam,
            AnimationClip channel, AnimationClip guard, AnimationClip strike)
        {
            // GUID koru — DeleteAsset+Create referansları kırıp T-pose'a düşürüyordu.
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ac == null)
                ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            else
            {
                // Eski state'leri temizle
                var sm0 = ac.layers[0].stateMachine;
                foreach (var s in sm0.states)
                    sm0.RemoveState(s.state);
                // orphan blend tree sub-assets temizlenmezse birikir; yeniden kurarken OK
            }

            // Speed param
            bool hasSpeed = false;
            foreach (var p in ac.parameters)
                if (p.name == "Speed") { hasSpeed = true; break; }
            if (!hasSpeed)
                ac.AddParameter("Speed", AnimatorControllerParameterType.Float);

            var sm = ac.layers[0].stateMachine;

            var loco = sm.AddState("Locomotion", new Vector3(300, 0, 0));
            var tree = new BlendTree
            {
                name = "LocomotionBT",
                blendParameter = "Speed",
                blendType = BlendTreeType.Simple1D,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, ac);
            // Idle → Walk → Run. Tam stick = Running.
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, 0.35f);
            tree.AddChild(run, 1f);
            var children = tree.children;
            if (children != null && children.Length > 0)
            {
                var idleChild = children[0];
                idleChild.timeScale = 0.02f;
                children[0] = idleChild;
                tree.children = children;
            }
            loco.motion = tree;
            sm.defaultState = loco;

            AddState(sm, "Dodge", dodge, 300, 80, speed: 1.0f);
            AddState(sm, "Hit", hit, 300, 160, speed: 1.0f);
            AddState(sm, "CastPierce", pierce, 520, 0, speed: 1.0f);
            AddState(sm, "CastSweep", sweep, 520, 80, speed: 1.0f);
            AddState(sm, "CastSlam", slam, 520, 160, speed: 1.0f);
            AddState(sm, "CastChannel", channel, 520, 240, speed: 1.0f);
            AddState(sm, "CastGuard", guard, 520, 320, speed: 1.0f);
            AddState(sm, "Death", death, 300, 240, speed: 1f);
            AddState(sm, "BasicStrike", strike, 520, 400, speed: 1.0f);

            EditorUtility.SetDirty(ac);
        }

        static void AddState(AnimatorStateMachine sm, string name, AnimationClip clip, float x, float y, float speed = 1f)
        {
            var st = sm.AddState(name, new Vector3(x, y, 0));
            st.motion = clip;
            st.speed = speed;
            // Sert kesim: kısa blend — oynak/smoothy geçiş yok.
            if (name != "Death" && name != "Locomotion")
            {
                var toLoco = st.AddTransition(sm.defaultState);
                toLoco.hasExitTime = true;
                toLoco.exitTime = 0.78f;
                toLoco.duration = 0.02f;
                toLoco.hasFixedDuration = true;
            }
        }

        static void AssignController(string prefabPath, string ctrlPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            // Nested Synty chr: property override Instantiate'te düşebiliyor → unpack.
            var nested = new System.Collections.Generic.List<GameObject>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject != root && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                    nested.Add(t.gameObject);
            }
            foreach (var go in nested)
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var anim = root.GetComponentInChildren<Animator>(true);
            if (anim == null)
            {
                Debug.LogWarning("[MixamoBind] Animator yok: " + prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }
            anim.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string name = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
