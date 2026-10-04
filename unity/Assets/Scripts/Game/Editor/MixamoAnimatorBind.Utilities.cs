using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Config;
using Dovus.Game.Skills;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Dovus.Game.Editor
{
    public static partial class MixamoAnimatorBind
    {
        static AnimatorController LoadOrCreate(string path)
        {
            // GUID koru — DeleteAsset+Create referansları kırıp T-pose'a düşürüyordu.
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
            // Yeniden kurulumda eski blend tree / üst gövde state machine alt-asset'leri birikmesin.
            foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(ac)))
            {
                if (sub is BlendTree bt)
                    Object.DestroyImmediate(bt, true);
                else if (sub is AnimatorStateMachine other && other != sm && other.name == "UpperBody")
                {
                    foreach (var s in other.states)
                        other.RemoveState(s.state);
                    Object.DestroyImmediate(other, true);
                }
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
            AnimationClip run, float idleTimeScale, float walkThreshold, float runThreshold)
        {
            var tree = new BlendTree
            {
                name = name,
                blendParameter = "Speed",
                blendType = BlendTreeType.Simple1D,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, ac);
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, walkThreshold);
            tree.AddChild(run, runThreshold);
            var children = tree.children;
            children[0].timeScale = idleTimeScale;
            tree.children = children;
            return tree;
        }

        /// <summary>
        /// Klibin zemin hızı (model birimi/sn): ayağın yere bastığı karelerde gövdeye göre yatay kayma
        /// hızının medyanı. In-place Mixamo kliplerinde kök hızı olmadığından tek güvenilir kaynak bu.
        /// Yön bağımsız: SampleAnimation kök dönüş ofsetini uygulamaz.
        /// </summary>
        static float MeasureGroundSpeed(AnimationClip clip)
        {
            const int samples = 120;
            const float groundBand = 0.025f;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerVisual);
            if (clip == null || prefab == null)
                return 0f;
            var go = (GameObject)Object.Instantiate(prefab);
            try
            {
                var an = go.GetComponentInChildren<Animator>(true);
                if (an == null || !an.isHuman)
                    return 0f;
                Transform root = an.transform;
                Transform[] feet =
                {
                    an.GetBoneTransform(HumanBodyBones.LeftToes) ?? an.GetBoneTransform(HumanBodyBones.LeftFoot),
                    an.GetBoneTransform(HumanBodyBones.RightToes) ?? an.GetBoneTransform(HumanBodyBones.RightFoot),
                };
                if (feet[0] == null || feet[1] == null)
                    return 0f;
                float dt = clip.length / samples;
                var y = new float[2, samples + 1];
                var xz = new Vector2[2, samples + 1];
                for (int i = 0; i <= samples; i++)
                {
                    clip.SampleAnimation(an.gameObject, dt * i);
                    for (int f = 0; f < 2; f++)
                    {
                        Vector3 p = root.InverseTransformPoint(feet[f].position);
                        y[f, i] = p.y;
                        xz[f, i] = new Vector2(p.x, p.z);
                    }
                }
                var speeds = new List<float>();
                for (int f = 0; f < 2; f++)
                {
                    float min = float.MaxValue;
                    for (int i = 0; i <= samples; i++)
                        min = Mathf.Min(min, y[f, i]);
                    for (int i = 0; i < samples; i++)
                    {
                        if (y[f, i] < min + groundBand)
                            speeds.Add((xz[f, i + 1] - xz[f, i]).magnitude / dt);
                    }
                }
                if (speeds.Count == 0)
                    return 0f;
                speeds.Sort();
                return Mathf.Max(0f, speeds[speeds.Count / 2]);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Yürüme/koşu/duruş kliplerinde ayak uçlarının ortalama yönünü hareket yönüne (kök ileri)
        /// hizalar: gerçek Animator'la ölçülen sapma klibin rotationOffset'ine eklenir. Burulmuş
        /// gövdeli balta klibinde koşarken sol ayak 60-70° yana dönüyordu (29 Eyl sahip bildirimi).
        /// </summary>
        static void AlignPlayerLocoFeet()
        {
            const float toleranceDeg = 1f;
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerCtrl);
            var tree = ac != null ? ac.layers[0].stateMachine.defaultState?.motion as BlendTree : null;
            if (tree == null)
                return;
            string log = "";
            foreach (ChildMotion child in tree.children)
            {
                var clip = child.motion as AnimationClip;
                string path = clip != null ? AssetDatabase.GetAssetPath(clip) : null;
                if (path == null || !path.StartsWith(PlayerDir + "/", System.StringComparison.Ordinal))
                    continue;
                float yaw = MeasureMeanFootYaw(ac, child.threshold, clip.length);
                if (float.IsNaN(yaw))
                    continue;
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                var clips = imp != null ? imp.clipAnimations : null;
                if (clips == null || clips.Length != 1)
                    continue;
                if (Mathf.Abs(yaw) > toleranceDeg)
                {
                    clips[0].rotationOffset = Mathf.DeltaAngle(0f, clips[0].rotationOffset + yaw);
                    imp.clipAnimations = clips;
                    imp.SaveAndReimport();
                }
                log += $" {clip.name}: sapma={yaw:F0}° ofset={clips[0].rotationOffset:F0}°";
            }
            Debug.Log("[MixamoBind] ayak hizası:" + log);
        }

        /// <summary>İki ayağın (parmak − bilek) yatay yönünün kök ileriye göre ortalama açısı; NaN = ölçülemedi.</summary>
        static float MeasureMeanFootYaw(AnimatorController ac, float speedParam, float clipLength)
        {
            const float stepSec = 1f / 60f;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerVisual);
            if (prefab == null)
                return float.NaN;
            var go = (GameObject)Object.Instantiate(prefab);
            try
            {
                var an = go.GetComponentInChildren<Animator>(true);
                if (an == null || !an.isHuman)
                    return float.NaN;
                an.runtimeAnimatorController = ac;
                Transform root = an.transform;
                Transform lf = an.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform rf = an.GetBoneTransform(HumanBodyBones.RightFoot);
                Transform lt = an.GetBoneTransform(HumanBodyBones.LeftToes);
                Transform rt = an.GetBoneTransform(HumanBodyBones.RightToes);
                if (lf == null || rf == null || lt == null || rt == null)
                    return float.NaN;
                an.Rebind();
                an.SetFloat("Speed", speedParam);
                an.Update(0f);
                int steps = Mathf.Max(30, Mathf.CeilToInt(clipLength / stepSec));
                float sum = 0f;
                for (int i = 0; i < steps; i++)
                {
                    an.Update(stepSec);
                    Vector3 l = lt.position - lf.position;
                    Vector3 r = rt.position - rf.position;
                    l.y = 0f;
                    r.y = 0f;
                    sum += Vector3.SignedAngle(root.forward, l, Vector3.up)
                        + Vector3.SignedAngle(root.forward, r, Vector3.up);
                }
                return sum / (steps * 2f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static void RemoveParam(AnimatorController ac, string name)
        {
            var ps = ac.parameters;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].name == name)
                {
                    ac.RemoveParameter(i);
                    return;
                }
            }
        }

        static BlendTree MakeLocomotionTree(AnimatorController ac, string name, AnimationClip idle, AnimationClip walk, AnimationClip run,
            float idleTimeScale)
        {
            var tree = new BlendTree
            {
                name = name,
                blendParameter = "Speed",
                blendType = BlendTreeType.Simple1D,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, ac);
            tree.AddChild(idle, 0f);
            if (run != null)
            {
                // Tam çubuk = koşu; ölü bölge üstü ~0.4 = yürüme (GameTuning.MinStickSpeedFrac).
                tree.AddChild(walk, 0.4f);
                tree.AddChild(run, 1f);
            }
            else
            {
                tree.AddChild(walk, 1f);
            }
            var children = tree.children;
            children[0].timeScale = idleTimeScale;
            tree.children = children;
            return tree;
        }

        static void AddActionState(AnimatorStateMachine sm, string name, AnimationClip clip, float x, float y,
            float exitTime, float blendSec, string holdParam = null, string speedParam = null)
        {
            var st = sm.AddState(name, new Vector3(x, y, 0));
            st.motion = clip;
            if (!string.IsNullOrEmpty(speedParam))
            {
                st.speedParameterActive = true;
                st.speedParameter = speedParam;
            }
            if (exitTime <= 0f)
                return;
            var toLoco = st.AddTransition(sm.defaultState);
            toLoco.hasExitTime = true;
            toLoco.exitTime = exitTime;
            toLoco.duration = blendSec;
            toLoco.hasFixedDuration = true;
            // Hold sinyali true iken dönüşü engeller: klip Loop Time olduğundan her turda tekrar
            // dener, sinyal bitince (false) ilk uygun turda ~blendSec'le lokomosyona döner.
            if (!string.IsNullOrEmpty(holdParam))
                toLoco.AddCondition(AnimatorConditionMode.IfNot, 0, holdParam);
        }

        /// <summary>
        /// O-anim(c): sağ yön klibi yok — humanoid mirror ile aynı sol klip ters oynar
        /// (<see cref="ActorView.DriveMotion"/> blend.Strafe &gt; 0'da bu state'i seçer).
        /// </summary>
        static void AddMirroredState(AnimatorStateMachine sm, string name, AnimationClip clip, float x, float y,
            float exitTime, float blendSec)
        {
            var st = sm.AddState(name, new Vector3(x, y, 0));
            st.motion = clip;
            st.mirrorParameterActive = false;
            st.mirror = true;
            var toLoco = st.AddTransition(sm.defaultState);
            toLoco.hasExitTime = true;
            toLoco.exitTime = exitTime;
            toLoco.duration = blendSec;
            toLoco.hasFixedDuration = true;
        }

        static void EnsureParam(AnimatorController ac, string name, AnimatorControllerParameterType type, float defaultFloat = 0f)
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
            ac.AddParameter(new AnimatorControllerParameter { name = name, type = type, defaultFloat = defaultFloat });
        }

        /// <summary>O-anim(c): CastChannel/CastGuard hold klipleri — döngü isteyen tek seferlik olmayan klipler.</summary>
        static bool IsHoldClipName(string fileName) =>
            fileName is "Player_Block_Hold" or "SS_Block" or "Fist_Block"
                or "Player_Spell_Cast" or "Caster_2H_Cast" or "Bow_Draw";

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
                // Döngü klipleri (idle/walk/run) loop. Tek klipli Mixamo FBX'te klip adı "mixamo.com"
                // gelir; runtime (BossView/ActorView) klibi adıyla bulabilsin diye dosya adı verilir.
                string fileName = Path.GetFileNameWithoutExtension(path);
                string lower = fileName.ToLowerInvariant();
                // O-anim(c): basılı/kanal tutuş klipleri de döngü — sürdürülen cast/kalkan sinyali
                // kesilene kadar Animator'da döner (bkz. ActorView.SetHoldFlags).
                bool loop = lower.Contains("idle") || lower.Contains("walk") || lower.Contains("run")
                    || IsHoldClipName(fileName);
                // Oyuncu locomotion'ı yerinde oynar: kök dönüşü (gövde yönüne göre), yüksekliği ve XZ'si
                // poza gömülür; yön farkını AlignPlayerLocoFeet ölçüp rotationOffset'e yazar. Bu Mixamo
                // dosyalarında "Original" kök gövdeye göre ~42° dönük (duruşta bile ayaklar yana bakıyor).
                // Boss hariç: BossView walk.averageSpeed okur.
                bool playerLoco = loop && path.StartsWith(PlayerDir + "/", System.StringComparison.Ordinal);
                var clips = imp.clipAnimations;
                if (clips == null || clips.Length == 0)
                    clips = imp.defaultClipAnimations;
                bool clipsDirty = false;
                for (int i = 0; i < clips.Length; i++)
                {
                    if (loop && !clips[i].loopTime)
                    {
                        clips[i].loopTime = true;
                        clipsDirty = true;
                    }
                    if (playerLoco && !(clips[i].lockRootRotation && !clips[i].keepOriginalOrientation
                            && clips[i].lockRootHeightY && clips[i].keepOriginalPositionY
                            && clips[i].lockRootPositionXZ && clips[i].keepOriginalPositionXZ))
                    {
                        clips[i].lockRootRotation = true;
                        clips[i].keepOriginalOrientation = false;
                        clips[i].lockRootHeightY = true;
                        clips[i].keepOriginalPositionY = true;
                        clips[i].heightFromFeet = false;
                        clips[i].lockRootPositionXZ = true;
                        clips[i].keepOriginalPositionXZ = true;
                        clipsDirty = true;
                    }
                    if (clips.Length == 1 && clips[i].name != fileName)
                    {
                        clips[i].name = fileName;
                        clipsDirty = true;
                    }
                }
                if (clipsDirty)
                {
                    imp.clipAnimations = clips;
                    dirty = true;
                }
                if (dirty)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        static Dictionary<string, AnimationClip> CollectClips(string dir, bool recursive)
        {
            var map = new Dictionary<string, AnimationClip>();
            if (!AssetDatabase.IsValidFolder(dir))
                return map;
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Animators/"))
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
            // İğne sırası önceliktir: ilk iğneye uyan klip, sonrakilere uyanlardan önce gelir.
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

        static void AssignController(string prefabPath, string ctrlPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogWarning("[MixamoBind] prefab yok: " + prefabPath);
                return;
            }
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            // Nested Synty chr: property override Instantiate'te düşebiliyor → unpack.
            var nested = new List<GameObject>();
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
