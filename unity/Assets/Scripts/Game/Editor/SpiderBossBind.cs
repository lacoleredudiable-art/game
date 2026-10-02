#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Dovus.Game;

namespace Dovus.Game.EditorTools
{
    /// <summary>
    /// CC0 Quaternius Spider → boss AnimatorController + AglarinKralicesi visual prefab.
    /// Menu: Dovus/Bind Spider Boss
    /// </summary>
    public static class SpiderBossBind
    {
        public const string ArtRoot = "Assets/Art/Bosses/Spider";
        public const string ModelFbx = ArtRoot + "/Spider_Quaternius.fbx";
        public const string ControllerPath = ArtRoot + "/SpiderBoss.controller";
        public const string PrefabPath = "Assets/Resources/Bosses/Visuals/AglarinKralicesi.prefab";

        /// <summary>Leap saldırısı; <see cref="BossVisual"/> henüz state sabiti taşımıyor — controller'da hazır.</summary>
        public const string StatePounce = "BossPounce";

        [MenuItem("Dovus/Bind Spider Boss")]
        public static void Bind()
        {
            if (!File.Exists(ModelFbx))
            {
                EditorUtility.DisplayDialog(
                    "Spider modeli yok",
                    ModelFbx + " bulunamadı.\n" +
                    "CC0 Quaternius Spider FBX'i Art/Bosses/Spider/ altına koy.",
                    "Tamam");
                return;
            }

            EnsureFolder(ArtRoot);
            EnsureFolder("Assets/Resources/Bosses/Visuals");
            ConfigureSpiderFbx();
            AssetDatabase.Refresh();

            var clips = CollectClips(ArtRoot, recursive: false);
            if (clips.Count == 0)
            {
                Debug.LogError("[SpiderBossBind] FBX'ten klip çıkmadı — import ayarlarını kontrol et.");
                return;
            }

            BuildSpiderController(new ClipSource(clips));
            BuildVisualPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SpiderBossBind] OK — clips={clips.Count} → {ControllerPath}, {PrefabPath}");
        }

        sealed class ClipSource
        {
            readonly Dictionary<string, AnimationClip> _clips;

            public ClipSource(Dictionary<string, AnimationClip> clips) => _clips = clips;

            public AnimationClip Pick(params string[] needles) => SpiderBossBind.Pick(_clips, needles);

            public AnimationClip Any() => _clips.Values.FirstOrDefault();
        }

        static void BuildSpiderController(ClipSource c)
        {
            AnimationClip idle = c.Pick("idle") ?? c.Any();
            AnimationClip walk = c.Pick("walk") ?? idle;
            AnimationClip attack = c.Pick("attack", "bite", "slam") ?? idle;
            AnimationClip jump = c.Pick("jump", "pounce", "leap") ?? attack;
            AnimationClip death = c.Pick("death", "dying") ?? idle;
            AnimationClip stagger = c.Pick("stagger", "hit", "impact", "reaction") ?? idle;

            var ac = LoadOrCreate(ControllerPath);
            EnsureParam(ac, BossVisual.ParamSpeed, AnimatorControllerParameterType.Float);
            EnsureParam(ac, BossVisual.ParamLocoSpeed, AnimatorControllerParameterType.Float, 1f);
            EnsureParam(ac, BossVisual.ParamActionSpeed, AnimatorControllerParameterType.Float, 1f);

            var sm = ResetBaseLayer(ac);
            var loco = sm.AddState(BossVisual.StateLocomotion, new Vector3(300, 0, 0));
            loco.motion = MakeLocomotionTree(ac, "SpiderLocomotionBT", idle, walk, 1f);
            loco.speedParameterActive = true;
            loco.speedParameter = BossVisual.ParamLocoSpeed;
            sm.defaultState = loco;

            AddBossAction(sm, BossVisual.StateSlam, attack, 520, 0, speedParam: true);
            AddBossAction(sm, BossVisual.StateBreath, attack, 520, 80, speedParam: true);
            AddBossAction(sm, BossVisual.StateRoar, idle, 520, 160, speedParam: false);
            AddBossAction(sm, StatePounce, jump, 520, -80, speedParam: true);
            AddBossAction(sm, BossVisual.StateStagger, stagger, 520, 240, speedParam: false);
            AddBossAction(sm, BossVisual.StateDeath, death, 300, 240, speedParam: false, returns: false);

            ClearBaseLayerMask(ac);
            EditorUtility.SetDirty(ac);
        }

        static void BuildVisualPrefab()
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFbx);
            if (src == null)
            {
                Debug.LogError("[SpiderBossBind] Model import failed: " + ModelFbx);
                return;
            }

            var meshRoot = (GameObject)PrefabUtility.InstantiatePrefab(src);
            meshRoot.name = "Spider_Quaternius";
            var root = new GameObject("AglarinKralicesi");
            meshRoot.transform.SetParent(root.transform, false);
            meshRoot.transform.localPosition = Vector3.zero;
            meshRoot.transform.localRotation = Quaternion.identity;
            meshRoot.transform.localScale = Vector3.one;

            var anim = meshRoot.GetComponentInChildren<Animator>(true);
            if (anim == null)
                anim = meshRoot.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        static void ConfigureSpiderFbx()
        {
            var imp = AssetImporter.GetAtPath(ModelFbx) as ModelImporter;
            if (imp == null)
                return;

            bool dirty = false;
            if (imp.animationType != ModelImporterAnimationType.Generic)
            {
                imp.animationType = ModelImporterAnimationType.Generic;
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

            var clips = imp.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = imp.defaultClipAnimations;
            bool clipsDirty = false;
            for (int i = 0; i < clips.Length; i++)
            {
                string lower = clips[i].name.ToLowerInvariant();
                bool loop = lower.Contains("idle") || lower.Contains("walk");
                if (loop && !clips[i].loopTime)
                {
                    clips[i].loopTime = true;
                    clipsDirty = true;
                }
            }
            if (clipsDirty)
            {
                imp.clipAnimations = clips;
                dirty = true;
            }

            if (dirty)
                imp.SaveAndReimport();
        }

        static void AddBossAction(AnimatorStateMachine sm, string name, AnimationClip clip, float x, float y,
            bool speedParam, bool returns = true)
        {
            var st = sm.AddState(name, new Vector3(x, y, 0));
            st.motion = clip;
            if (speedParam)
            {
                st.speedParameterActive = true;
                st.speedParameter = BossVisual.ParamActionSpeed;
            }
            if (!returns)
                return;
            var back = st.AddTransition(sm.defaultState);
            back.hasExitTime = true;
            back.exitTime = 0.92f;
            back.duration = 0.2f;
            back.hasFixedDuration = true;
        }

        static AnimatorController LoadOrCreate(string path)
        {
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            return ac != null ? ac : AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        static AnimatorStateMachine ResetBaseLayer(AnimatorController ac)
        {
            var sm = ac.layers[0].stateMachine;
            foreach (var s in sm.states)
                sm.RemoveState(s.state);
            if (ac.layers.Length > 1)
                ac.layers = new[] { ac.layers[0] };
            foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(ac)))
            {
                if (sub is BlendTree bt)
                    Object.DestroyImmediate(bt, true);
            }
            return sm;
        }

        static void ClearBaseLayerMask(AnimatorController ac)
        {
            var layers = ac.layers;
            layers[0].avatarMask = null;
            ac.layers = layers;
        }

        static BlendTree MakeLocomotionTree(AnimatorController ac, string name, AnimationClip idle, AnimationClip walk,
            float idleTimeScale)
        {
            var tree = new BlendTree
            {
                name = name,
                blendParameter = BossVisual.ParamSpeed,
                blendType = BlendTreeType.Simple1D,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, ac);
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, 1f);
            var children = tree.children;
            children[0].timeScale = idleTimeScale;
            tree.children = children;
            return tree;
        }

        static void EnsureParam(AnimatorController ac, string name, AnimatorControllerParameterType type,
            float defaultFloat = 0f)
        {
            var ps = ac.parameters;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].name != name)
                    continue;
                if (ps[i].type == type)
                {
                    ps[i].defaultFloat = defaultFloat;
                    ac.parameters = ps;
                    return;
                }
                ac.RemoveParameter(i);
                break;
            }
            ac.AddParameter(new AnimatorControllerParameter
            {
                name = name,
                type = type,
                defaultFloat = defaultFloat
            });
        }

        static Dictionary<string, AnimationClip> CollectClips(string dir, bool recursive)
        {
            var map = new Dictionary<string, AnimationClip>();
            if (!AssetDatabase.IsValidFolder(dir))
                return map;
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".controller", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (!recursive && folder != dir)
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
            foreach (string n in needles)
            {
                foreach (var kv in clips)
                {
                    if (kv.Key.Contains(n))
                        return kv.Value;
                }
            }
            return null;
        }

        static string Normalize(string s) =>
            (s ?? "").ToLowerInvariant().Replace('_', ' ').Replace('-', ' ');

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
#endif
