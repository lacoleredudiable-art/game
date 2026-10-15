using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Dovus.Game.Composition;
using Dovus.Game.Vfx;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// k3 (Blender → Mixamo → FBX) içe aktarma: model blend shape'leri, VUR klip olayları (klipler.csv,
    /// zaman = (kare-1)/30), k3 controller'ı, silah yürüme setleri, prefab olay alıcısı, k3_Mat emission ve dilim oyuncusu.
    /// Menu: Dovus/k3/*. Batch: -executeMethod Dovus.Game.Editor.K3AssetSetup.Reimport
    /// </summary>
    public static partial class K3AssetSetup
    {
        internal const string Root = "Assets/Art/Characters/k3";
        internal const string PrefabPath = Root + "/k3.prefab";
        internal const string ClipsDir = Root + "/Klipler";
        internal const string ClipCsvPath = Root + "/klipler.csv";
        const string ModelFbx = Root + "/Model/k3_Armature.fbx";
        const string MaterialPath = Root + "/Model/k3_Mat.mat";
        internal const string ControllerPath = Root + "/k3.controller";
        const string ScenePath = "Assets/Scenes/Prototype.unity";
        const string SliceVisualField = "_slicePlayerVisualPrefab";
        internal const float Fps = 30f;
        const string WeaponsDir = Root + "/Silahlar";
        const string WeaponTexturesDir = WeaponsDir + "/doku";
        internal const string LoopPrefix = "Loko_";

        /// <summary>Blender'dan aktarılan klipler. Cekic_* / Loko_Agir_* bu listede yok: çekiç ayarlarına dokunulmaz.</summary>
        internal static readonly string[] ExportClips =
        {
            "Buyu_Arindirma", "Buyu_Kontrol", "Buyu_Sifa",
            "Kilic_ATIL", "Kilic_DURUS", "Kilic_VUR",
            "Loko_Kilic_Idle", "Loko_Kilic_Run", "Loko_Yay_Idle", "Loko_Yay_Run",
            "Ortak_HitAgir", "Ortak_HitHafif", "Ortak_Inis", "Ortak_Isaret", "Ortak_Olum", "Ortak_Takla", "Ortak_Zipla",
            "Yay_DURUS", "Yay_VUR",
        };

        static readonly Color EmissionColor = new(1f, 0.28f, 0.06f);

        /// <summary>
        /// Kural: silah onu tutan elin Grip noktasına sıfır konum/sıfır açıyla takılır, model uzun ekseni +Z
        /// (Vfx_Kabza orijinde, Vfx_Tip +Z ucunda). Silah başına açı yok; tutuş pozu Grip noktasındadır.
        /// </summary>
        static readonly (string Name, string Fbx, string Socket)[] Weapons =
        {
            ("Silah_Kilic", "Kilic", GripRight),
            ("Silah_Yay", "Yay", GripLeft),
        };

        internal const string GripRight = "Grip_R";
        internal const string GripLeft = "Grip_L";
        static readonly (string Legacy, string Grip)[] LegacySockets = { ("Socket_R", GripRight), ("Socket_L", GripLeft) };

        /// <summary>
        /// Grip noktaları = Blender Tutus_* boşlukları (soket-lokal; dönüş wxyz). Unity'de kemik yerel ekseni
        /// X'te aynalı: konum (x,y,z) → (-x,y,z), dönüş (w,x,y,z) → Unity (x,-y,-z,w).
        /// Grip_R: eski Socket_R (el kemiği ucu, +Y 0.08 m) + Tutus_Kilic. Grip_L: Socket_L (kemik başı) + Tutus_Yay.
        /// Ek ±90° X verilmez: Blender v024'te silah uzun ekseni (+Z) Tutus altında birim dönüşle duruyor;
        /// ±90° X her karede Blender pozundan 90° sapar.
        /// </summary>
        static readonly (string Socket, Vector3 BlenderPos, Vector4 BlenderRotWxyz)[] SocketOffsets =
        {
            (GripRight, new Vector3(-0.0324f, 0.1050f, 0.0281f), new Vector4(0.5141f, 0.3602f, 0.2514f, 0.7367f)),
            (GripLeft, new Vector3(0.0306f, 0.1050f, 0.0319f), new Vector4(0.6578f, 0.2980f, -0.3196f, -0.6135f)),
        };

        /// <summary>ActorView'un genel state adları → k3 klibi.</summary>
        static readonly (string State, string Clip)[] StateAliases =
        {
            ("BasicStrike", "Kilic_VUR"),
            ("CastSweep", "Kilic_VUR"),
            ("CastPierce", "Kilic_ATIL"),
            ("CastGuard", "Kilic_DURUS"),
            ("CastSlam", "Cekic_VUR"),
            ("CastShoot", "Yay_VUR"),
            ("CastChannel", "Buyu_Kontrol"),
            ("Dodge", "Ortak_Takla"),
            ("Hit", "Ortak_HitHafif"),
            ("Death", "Ortak_Olum"),
        };

        static readonly string[] FloatParams = { "Speed", "StrikeSpeed", "Forward", "Strafe", "Focus", "Pierce", "Spread", "Lift" };
        static readonly string[] BoolParams = { "ChannelHold", "GuardHold" };
        internal const string LocoIdle = "Loko_Kilic_Idle";
        internal const string LocoRun = "Loko_Kilic_Run";
        const string NoReturnClip = "Ortak_Olum";
        const float ReturnExitTime = 0.9f;
        const float ReturnBlendSec = 0.1f;

        [MenuItem("Dovus/k3/Yeniden al (FBX ayarları + olaylar + controller + prefab)")]
        public static void Reimport()
        {
            Dictionary<string, ClipRow> rows = ReadClipCsv();
            Avatar avatar = ConfigureModel();
            foreach (string fbx in ExportClips)
                ConfigureClips(fbx, RowsFromSource(rows, fbx), avatar);
            ConfigureWeaponImports();
            ApplyEmission();
            AnimatorController ctrl = EnsureController();
            EnsureClipStates(ctrl);
            EnableWriteDefaults(ctrl);
            K3WeaponLocomotion.Ensure(ctrl);
            ConfigurePrefab(ctrl);
            AssetDatabase.SaveAssets();
            Debug.Log("[k3] yeniden alma tamam");
        }

        /// <summary>Yalnız klip içe aktarma ayarları + olaylar (klipler.csv); model ve prefab'a dokunmaz.</summary>
        [MenuItem("Dovus/k3/Klip ayarlarını yeniden uygula (klipler.csv)")]
        public static void ReapplyClips()
        {
            Dictionary<string, ClipRow> rows = ReadClipCsv();
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelFbx).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("k3 avatarı geçersiz: " + ModelFbx);
            foreach (string fbx in ExportClips)
                ConfigureClips(fbx, RowsFromSource(rows, fbx), avatar);
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ctrl != null)
            {
                EnsureClipStates(ctrl);
                EnableWriteDefaults(ctrl);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[k3] klip ayarları yeniden uygulandı");
        }

        /// <summary>Blender (w,x,y,z) soket-lokal dönüşü → Unity kemik-lokal dönüşü.</summary>
        internal static Quaternion BlenderToUnityLocal(Vector4 wxyz) => new(wxyz.y, -wxyz.z, -wxyz.w, wxyz.x);

        internal static Vector3 BlenderToUnityLocal(Vector3 p) => new(-p.x, p.y, p.z);

        [MenuItem("Dovus/k3/Silahları soketlere tak (Grip_R / Grip_L)")]
        public static void RemountWeapons()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                MountWeapons(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log("[k3] silahlar takıldı → " + PrefabPath);
        }

        [MenuItem("Dovus/k3/Dilim oyuncusu olarak bağla (Prototype)")]
        public static void BindSlicePlayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameBootstrapHost host = scene.GetRootGameObjects()
                .Select(r => r.GetComponentInChildren<GameBootstrapHost>(true))
                .FirstOrDefault(h => h != null);
            if (host == null)
                throw new InvalidOperationException("Prototype sahnesinde GameBootstrapHost yok");
            var so = new SerializedObject(host);
            so.FindProperty(SliceVisualField).objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[k3] Prototype " + SliceVisualField + " = " + PrefabPath);
        }

        internal static void SetPrefabChildRotation(string childName, Quaternion localRotation)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform child = FindDeep(root.transform, childName)
                    ?? throw new InvalidOperationException(childName + " yok");
                child.localRotation = localRotation;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Avatar ConfigureModel()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelFbx);
            importer.importBlendShapes = true;
            importer.SaveAndReimport();
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelFbx).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("k3 avatarı geçersiz: " + ModelFbx);
            return avatar;
        }

        /// <summary>
        /// Klip FBX'leri k3_Govde mesh'ini de taşıyor: malzeme/kamera/ışık alınmaz, yalnız animasyon kullanılır.
        /// Blend shape açık kalır: kapalıyken El_Kavra_L/R eğrileri klipten düşer.
        /// Unity'de mesh içe aktarmayı tamamen kapatan ayar yok; mesh alt varlığı hiçbir yerde kullanılmaz.
        /// </summary>
        static void ConfigureClips(string fbxName, List<(string Name, ClipRow Row)> rows, Avatar avatar)
        {
            string path = $"{ClipsDir}/{fbxName}.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importBlendShapes = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.preserveHierarchy = true;

            ModelImporterClipAnimation[] existing = importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;
            string take = existing[0].takeName;
            var clips = new List<ModelImporterClipAnimation>();
            foreach ((string clipName, ClipRow row) in rows)
            {
                // Aynı FBX'ten birden çok kesit (ör. Ortak_Takla takla / kalkış / geri sıçrama).
                ModelImporterClipAnimation clip = existing.FirstOrDefault(c => c.name == clipName)
                    ?? (clips.Count == 0 && rows.Count == 1 ? existing[0] : new ModelImporterClipAnimation { takeName = take });
                clip.name = clipName;
                clip.firstFrame = row.FirstFrame - 1;
                clip.lastFrame = row.LastFrame - 1;
                clip.loopTime = clipName.StartsWith(LoopPrefix, StringComparison.Ordinal);
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                // Bake Into Pose kapalı + Based Upon Center of Mass: klibin ileri koşusu kökte kalır; oyuncuda
                // applyRootMotion kapalı olduğundan atılışı yalnız hareket kalıbı taşır.
                clip.lockRootPositionXZ = !row.RootXzFromCenterOfMass;
                if (row.RootXzFromCenterOfMass)
                    clip.keepOriginalPositionXZ = false;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.events = BuildEvents(row, clip.lastFrame - clip.firstFrame);
                clips.Add(clip);
            }
            importer.clipAnimations = clips.ToArray();
            importer.SaveAndReimport();
        }

        static void EnsureClipStates(AnimatorController ctrl) =>
            K3TimedClipStates.Ensure(ctrl, LoadClips(), LoopPrefix);

        static void ConfigureWeaponImports()
        {
            foreach (var w in Weapons)
            {
                string path = $"{WeaponsDir}/{w.Fbx}.fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path)
                    ?? throw new InvalidOperationException(path + " yok");
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importBlendShapes = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.SaveAndReimport();
                if (!AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Any(m => m.mainTexture != null))
                {
                    Directory.CreateDirectory(WeaponTexturesDir);
                    importer.ExtractTextures(WeaponTexturesDir);
                    AssetDatabase.Refresh();
                    importer.SaveAndReimport();
                }
            }
        }

        static void EnableWriteDefaults(AnimatorController ctrl)
        {
            foreach (AnimatorControllerLayer layer in ctrl.layers)
            {
                foreach (ChildAnimatorState s in layer.stateMachine.states)
                    s.state.writeDefaultValues = true;
            }
            EditorUtility.SetDirty(ctrl);
        }

        /// <summary>Olay zamanı saniye = (kare-1)/30; Unity klip olayını klip boyuna oranla saklar.</summary>
        internal static AnimationEvent[] BuildEvents(ClipRow row, float spanFrames)
        {
            var events = new List<AnimationEvent>();
            void Add(string fn, int frameOffset) => events.Add(new AnimationEvent
            {
                functionName = fn,
                time = Mathf.Clamp01(frameOffset / Mathf.Max(1f, spanFrames)),
            });
            if (row.ImpactFrame > 0)
                Add("Impact", row.ImpactFrame - 1);
            if (row.TrailOnSec.HasValue)
                Add("Trail_On", Mathf.RoundToInt(row.TrailOnSec.Value * Fps));
            if (row.TrailOffSec.HasValue)
                Add("Trail_Off", Mathf.RoundToInt(row.TrailOffSec.Value * Fps));
            if (row.EjderSec.HasValue)
                Add("Ejder", Mathf.RoundToInt(row.EjderSec.Value * Fps));
            return events.OrderBy(e => e.time).ToArray();
        }

        static void ApplyEmission()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            mat.EnableKeyword("_EMISSION");
            // Çarpan 1: HDR yoğunluk 0 → renk olduğu gibi.
            mat.SetColor("_EmissionColor", EmissionColor);
            mat.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(mat);
        }

        static AnimatorController EnsureController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null)
                return existing;

            Dictionary<string, AnimationClip> clips = LoadClips();
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            foreach (string p in FloatParams)
                ctrl.AddParameter(new AnimatorControllerParameter
                {
                    name = p,
                    type = AnimatorControllerParameterType.Float,
                    defaultFloat = p == "StrikeSpeed" ? 1f : 0f,
                });
            foreach (string p in BoolParams)
                ctrl.AddParameter(p, AnimatorControllerParameterType.Bool);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState loco = ctrl.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(clips[LocoIdle], 0f);
            tree.AddChild(clips[LocoRun], 1f);
            loco.writeDefaultValues = true;
            sm.defaultState = loco;

            foreach (AnimationClip clip in clips.Values.Where(c => !c.name.StartsWith("Loko_", StringComparison.Ordinal)))
                AddAction(sm, clip.name, clip, loco);
            foreach ((string state, string clip) in StateAliases)
            {
                AnimatorState s = AddAction(sm, state, clips[clip], loco);
                if (state == "BasicStrike")
                {
                    s.speedParameter = "StrikeSpeed";
                    s.speedParameterActive = true;
                }
            }
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        internal static AnimatorState AddAction(AnimatorStateMachine sm, string name, AnimationClip clip, AnimatorState loco)
        {
            AnimatorState s = sm.AddState(name);
            s.motion = clip;
            s.writeDefaultValues = true;
            if (clip.name != NoReturnClip)
            {
                AnimatorStateTransition back = s.AddTransition(loco);
                back.hasExitTime = true;
                back.exitTime = ReturnExitTime;
                back.duration = ReturnBlendSec;
            }
            return s;
        }

        internal static Dictionary<string, AnimationClip> LoadClips()
        {
            var clips = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ClipsDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (AnimationClip c in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                {
                    if (!c.name.StartsWith("__preview__", StringComparison.Ordinal))
                        clips[c.name] = c;
                }
            }
            return clips;
        }

        static void ConfigurePrefab(AnimatorController ctrl)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Animator anim = root.GetComponentInChildren<Animator>(true);
                anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (anim.GetComponent<SkillAnimVfxEventHost>() == null)
                    anim.gameObject.AddComponent<SkillAnimVfxEventHost>();
                DropBoneTransformOverrides(root);
                MountWeapons(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Soketteki boş Silah_* yer tutucusunun yerine silah FBX'i; Vfx_Tip/Vfx_Kabza FBX'ten gelir.</summary>
        static void MountWeapons(GameObject root)
        {
            foreach ((string legacyName, string grip) in LegacySockets)
            {
                Transform legacy = FindDeep(root.transform, legacyName);
                if (legacy != null && FindDeep(root.transform, grip) == null)
                    legacy.name = grip;
            }

            foreach ((string socketName, Vector3 offset, Vector4 rotWxyz) in SocketOffsets)
            {
                Transform socket = FindDeep(root.transform, socketName)
                    ?? throw new InvalidOperationException(socketName + " yok");
                socket.localPosition = BlenderToUnityLocal(offset);
                socket.localRotation = BlenderToUnityLocal(rotWxyz);
            }

            foreach (var w in Weapons)
            {
                Transform socket = FindDeep(root.transform, w.Socket);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{WeaponsDir}/{w.Fbx}.fbx");
                Transform old = socket.Cast<Transform>().FirstOrDefault(c => c.name == w.Name);
                GameObject weapon = old != null && PrefabUtility.GetCorrespondingObjectFromSource(old.gameObject) == model
                    ? old.gameObject
                    : null;
                if (weapon == null)
                {
                    int sibling = socket.childCount;
                    if (old != null)
                    {
                        sibling = old.GetSiblingIndex();
                        UnityEngine.Object.DestroyImmediate(old.gameObject);
                    }
                    weapon = (GameObject)PrefabUtility.InstantiatePrefab(model, socket);
                    weapon.name = w.Name;
                    weapon.transform.SetSiblingIndex(sibling);
                }
                weapon.transform.localPosition = Vector3.zero;
                weapon.transform.localRotation = Quaternion.identity;
                weapon.transform.localScale = Vector3.one;
                foreach (string vfx in new[] { "Vfx_Tip", "Vfx_Kabza" })
                {
                    if (FindDeep(weapon.transform, vfx) == null)
                        throw new InvalidOperationException($"{w.Fbx}.fbx içinde {vfx} yok");
                }
            }
        }

        /// <summary>Controller ataması kemik pozunu override olarak yazabilir; kemik dinlenme pozu FBX'ten gelir.</summary>
        static void DropBoneTransformOverrides(GameObject instanceRoot)
        {
            PropertyModification[] mods = PrefabUtility.GetPropertyModifications(instanceRoot);
            if (mods == null)
                return;
            PropertyModification[] kept = mods
                .Where(m => !(m.target is Transform t && t.parent != null && m.propertyPath.StartsWith("m_Local", StringComparison.Ordinal)))
                .ToArray();
            if (kept.Length != mods.Length)
                PrefabUtility.SetPropertyModifications(instanceRoot, kept);
        }

        internal static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name)
                return t;
            foreach (Transform c in t)
            {
                Transform hit = FindDeep(c, name);
                if (hit != null)
                    return hit;
            }
            return null;
        }
    }
}
