using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Dovus.Visual.EditorTools
{
    /// <summary>
    /// "Deneme sahnesi" (görsel test, $0): açık lav/bazalt ovası, gri bulutlu ışık, açık gri sis, ince lav
    /// çatlakları, sisin derinliğinde ejderha başı (yer tutucu), ölçek için kahraman, yavaş alçak kamera.
    /// Sahne YAML'ı elle yazılmaz (AGENTS kural 2): bu menü sahneyi baştan kurar ve kaydeder.
    /// Oynanış koduna dokunmaz; yalnız Assets/Art/DenemeSahnesi, Assets/Art/PolyHaven ve
    /// Assets/Scenes/DenemeSahnesi.unity yazılır.
    /// </summary>
    public static class DenemeSahnesiBuilder
    {
        public const string ScenePath = "Assets/Scenes/DenemeSahnesi.unity";
        const string ArtRoot = "Assets/Art/DenemeSahnesi";
        const string MeshDir = ArtRoot + "/Meshes";
        const string GenDir = ArtRoot + "/Generated";
        const string HeroMatDir = GenDir + "/HeroSoft";
        const string LayoutPath = ArtRoot + "/DenemeSahnesiLayout.json";
        const string PlaceholderPath = ArtRoot + "/Placeholder/DragonHead_AI_placeholder.obj";
        const string PolyHavenDir = "Assets/Art/PolyHaven";
        const string ProfilePath = GenDir + "/DenemeSahnesi_PostFX.asset";
        const string LightingPath = GenDir + "/DenemeSahnesi_Lighting.lighting";
        const string MixamoCharacterDir = "Assets/Art/Mixamo/Characters";
        const string MixamoPlayerController = "Assets/Art/Mixamo/Animators/Player_Synty.controller";
        const string SyntyHero = "Assets/Art/Synty/Prefabs/PlayerVisual_Synty.prefab";
        const string QuaterniusHero = "Assets/Art/Quaternius/Prefabs/PlayerVisual_Quaternius.prefab";
        const float ExpectedNearGroundWidth = 147.2f;

        static readonly string[] RockIds = { "rock_face_01", "rock_face_02", "boulder_01", "mountainside", "namaqualand_cliff_02" };
        static readonly string[] GeneratedMeshes = { "Ground_Near", "Ground_Far", "LavaCracks", "Skyline", "EdgeMist" };

        static readonly StringBuilder Report = new StringBuilder();

        // ------------------------------------------------------------------ menu

        [MenuItem("Dovus/Visual/Deneme Sahnesi - Build (sahneyi kur)")]
        static void BuildMenu() => Debug.Log(Build(false));

        [MenuItem("Dovus/Visual/Deneme Sahnesi - Bake Lighting (async)")]
        static void BakeMenu() => Debug.Log(StartBake());

        [MenuItem("Dovus/Visual/Deneme Sahnesi - Ejderhayi secili prefab ile degistir")]
        static void SwapMenu()
        {
            var prefab = Selection.activeObject as GameObject;
            Debug.Log(SwapDragon(prefab != null ? AssetDatabase.GetAssetPath(prefab) : null));
        }

        // ------------------------------------------------------------------ build

        /// <summary>
        /// Sahneyi baştan kurar ve <see cref="ScenePath"/>'e kaydeder. Otomasyon girişi (unity-mcp):
        /// <c>Dovus.Visual.EditorTools.DenemeSahnesiBuilder.Build(false)</c>. Dönen metin raporun kendisidir;
        /// "OK" ile başlar, sorun varsa "ABORT"/"WARN" satırları içerir.
        /// </summary>
        public static string Build(bool discardUnsavedSceneChanges)
        {
            Report.Clear();
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                return "ABORT: Unity Play modunda veya derliyor. Edit modunda tekrar çalıştır.";
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.isDirty && !discardUnsavedSceneChanges)
                    return $"ABORT: açık sahnede kaydedilmemiş değişiklik var ({s.path}). Kaydet ya da Build(true) ile at.";
            }

            try
            {
                EnsureFolder(GenDir);
                EnsureFolder(HeroMatDir);
                ConfigureImports();
                DenemeLayout layout = LoadLayout();
                if (layout == null)
                    return "ABORT: " + LayoutPath + " okunamadı.";

                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                LookSpec look = layout.look;

                ApplyRenderSettings(look);
                LightingSettings lighting = CreateLightingSettings();
                Lightmapping.lightingSettings = lighting;

                var env = new GameObject("Environment");
                Light sun = CreateSun(look);
                CreateLavaLights(layout);
                CreateGround(layout, env.transform);
                CreateGeneratedUnlit(layout, env.transform);
                CreateRocks(layout, env.transform);
                CreateBoundary(layout);
                CreateProbes(layout);
                GameObject dragon = CreateDragon(layout);
                GameObject hero = CreateHero(layout);
                Camera cam = CreateCamera(layout, look, out CameraPushIn pushIn);
                CreateVolume(look);
                CreateOverlay(cam, pushIn);

                RenderSettings.sun = sun;
                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Line(saved ? "scene saved: " + ScenePath : "WARN: scene could not be saved");
                Line($"dragon: {DescribeDragon(dragon)}");
                Line($"hero: {(hero != null ? hero.name : "none")}");
                Line("next: Dovus/Visual/Deneme Sahnesi - Bake Lighting (async), sonra sahneyi kaydet.");
                return "OK DenemeSahnesi built\n" + Report;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return "ABORT: exception " + e.GetType().Name + ": " + e.Message + "\n" + Report;
            }
        }

        static void Line(string s)
        {
            Report.AppendLine(s);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static DenemeLayout LoadLayout()
        {
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            if (ta == null) return null;
            return JsonUtility.FromJson<DenemeLayout>(ta.text);
        }

        static Color C(float[] a, float alpha = 1f)
        {
            if (a == null || a.Length < 3) return Color.magenta;
            return new Color(a[0], a[1], a[2], alpha);
        }

        static Vector3 V(float[] a)
        {
            if (a == null || a.Length < 3) return Vector3.zero;
            return new Vector3(a[0], a[1], a[2]);
        }

        // ------------------------------------------------------------------ imports (mobile sizes)

        static void ConfigureImports()
        {
            int changedTex = 0, changedModels = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PolyHavenDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                bool normal = path.Contains("_nor_gl_");
                bool detail = path.Contains("_detail_");
                bool dirty = false;
                if (normal && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
                if (ti.maxTextureSize != 1024) { ti.maxTextureSize = 1024; dirty = true; }
                if (!ti.mipmapEnabled) { ti.mipmapEnabled = true; dirty = true; }
                if (ti.wrapMode != TextureWrapMode.Repeat) { ti.wrapMode = TextureWrapMode.Repeat; dirty = true; }
                int aniso = path.Contains("dark_rock") || detail ? 4 : 2;
                if (ti.anisoLevel != aniso) { ti.anisoLevel = aniso; dirty = true; }
                TextureImporterPlatformSettings android = ti.GetPlatformTextureSettings("Android");
                if (!android.overridden || android.maxTextureSize != 1024 || android.format != TextureImporterFormat.ASTC_6x6)
                {
                    android.overridden = true;
                    android.maxTextureSize = 1024;
                    android.format = TextureImporterFormat.ASTC_6x6;
                    ti.SetPlatformTextureSettings(android);
                    dirty = true;
                }
                if (dirty) { ti.SaveAndReimport(); changedTex++; }
            }

            var models = new List<string>();
            foreach (string id in RockIds) models.Add($"{PolyHavenDir}/{id}/{id}_lod.obj");
            foreach (string m in GeneratedMeshes) models.Add($"{MeshDir}/{m}.obj");
            models.Add(PlaceholderPath);
            foreach (string path in models)
            {
                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null) { Line("WARN: model missing " + path); continue; }
                string file = Path.GetFileNameWithoutExtension(path);
                bool lightmapped = file.StartsWith("Ground") || path.StartsWith(PolyHavenDir);
                bool hasFileNormals = file.StartsWith("Ground") || file == "LavaCracks" || file.StartsWith("DragonHead");
                bool dirty = false;
                if (mi.materialImportMode != ModelImporterMaterialImportMode.None) { mi.materialImportMode = ModelImporterMaterialImportMode.None; dirty = true; }
                if (mi.animationType != ModelImporterAnimationType.None) { mi.animationType = ModelImporterAnimationType.None; dirty = true; }
                if (mi.importAnimation) { mi.importAnimation = false; dirty = true; }
                if (mi.generateSecondaryUV != lightmapped) { mi.generateSecondaryUV = lightmapped; dirty = true; }
                var normals = hasFileNormals ? ModelImporterNormals.Import : ModelImporterNormals.Calculate;
                if (mi.importNormals != normals) { mi.importNormals = normals; dirty = true; }
                if (!hasFileNormals && Math.Abs(mi.normalSmoothingAngle - 70f) > 0.1f) { mi.normalSmoothingAngle = 70f; dirty = true; }
                var tangents = path.StartsWith(PolyHavenDir) || file.StartsWith("Ground")
                    ? ModelImporterTangents.CalculateMikk : ModelImporterTangents.None;
                if (mi.importTangents != tangents) { mi.importTangents = tangents; dirty = true; }
                if (mi.isReadable) { mi.isReadable = false; dirty = true; }
                if (dirty) { mi.SaveAndReimport(); changedModels++; }
            }

            // OBJ birimi: ölçülen genişlik 147 m değilse tüm OBJ'lerin ölçeğini düzelt (tek seferlik).
            Mesh near = LoadMesh($"{MeshDir}/Ground_Near.obj");
            if (near != null)
            {
                float width = near.bounds.size.x;
                if (width > 0.01f && Mathf.Abs(width / ExpectedNearGroundWidth - 1f) > 0.05f)
                {
                    float fix = ExpectedNearGroundWidth / width;
                    Line($"WARN: OBJ unit mismatch (ground width {width:0.###}); globalScale x{fix:0.###}");
                    foreach (string path in models)
                    {
                        var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                        if (mi == null) continue;
                        mi.globalScale *= fix;
                        mi.SaveAndReimport();
                    }
                }
            }
            Line($"imports: {changedTex} textures, {changedModels} models reconfigured");
        }

        static Mesh LoadMesh(string modelPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Mesh>().FirstOrDefault();
        }

        // ------------------------------------------------------------------ materials

        static Material SaveMaterial(Material m, string name)
        {
            string path = $"{GenDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = m.shader;
                existing.CopyPropertiesFromMaterial(m);
                existing.shaderKeywords = m.shaderKeywords;
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(m);
                return existing;
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Shader FindShader(string name, string fallback)
        {
            Shader s = Shader.Find(name);
            if (s != null && !ShaderUtil.ShaderHasError(s)) return s;
            Line($"WARN: shader {name} missing or has errors; fallback {fallback}");
            return Shader.Find(fallback);
        }

        static Texture2D Tex(string path)
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (t == null) Line("WARN: texture missing " + path);
            return t;
        }

        static Material LitMaterial(string name, Texture2D albedo, Texture2D normal, Color tint, float smoothness, float tiling)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetColor("_BaseColor", tint);
            if (albedo != null) m.SetTexture("_BaseMap", albedo);
            m.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", 1f);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_SpecularHighlights", 0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.SetFloat("_EnvironmentReflections", 0f);
            m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            m.enableInstancing = false;
            return SaveMaterial(m, name);
        }

        static Material LavaMaterial(string name, Color core, Color edge, float coreWidth, float pulse, float fogStrength)
        {
            Shader s = FindShader("Dovus/Visual/LavaEmissive", "Universal Render Pipeline/Unlit");
            var m = new Material(s) { name = name };
            if (s != null && s.name == "Dovus/Visual/LavaEmissive")
            {
                m.SetColor("_CoreColor", core);
                m.SetColor("_EdgeColor", edge);
                m.SetFloat("_CoreWidth", coreWidth);
                m.SetFloat("_PulseAmount", pulse);
                m.SetFloat("_FogStrength", fogStrength);
                m.SetFloat("_Intensity", 1f);
            }
            else
            {
                m.SetColor("_BaseColor", core);
            }
            return SaveMaterial(m, name);
        }

        // ------------------------------------------------------------------ environment

        static void ApplyRenderSettings(LookSpec look)
        {
            Color fog = C(look.fogColor);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = C(look.ambientSky);
            RenderSettings.ambientEquatorColor = C(look.ambientEquator);
            RenderSettings.ambientGroundColor = C(look.ambientGround);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = look.fogDensity;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.reflectionIntensity = 0.3f;
            RenderSettings.subtractiveShadowColor = new Color(0.42f, 0.45f, 0.5f, 1f);
            DynamicGI.UpdateEnvironment();
        }

        static LightingSettings CreateLightingSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);
            LightingSettings ls = existing != null ? existing : new LightingSettings();
            ls.name = "DenemeSahnesi_Lighting";
            ls.bakedGI = true;
            ls.realtimeGI = false;
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            ls.mixedBakeMode = MixedLightingMode.Subtractive;
            ls.lightmapResolution = 3f;
            ls.lightmapPadding = 2;
            ls.lightmapMaxSize = 1024;
            ls.directSampleCount = 32;
            ls.indirectSampleCount = 256;
            ls.environmentSampleCount = 128;
            ls.maxBounces = 2;
            ls.ao = true;
            ls.aoMaxDistance = 1.5f;
            ls.directionalityMode = LightmapsMode.NonDirectional;
            ls.lightmapCompression = LightmapCompression.NormalQuality;
            if (existing == null) AssetDatabase.CreateAsset(ls, LightingPath);
            else EditorUtility.SetDirty(ls);
            return ls;
        }

        static Light CreateSun(LookSpec look)
        {
            var go = new GameObject("Sun_Overcast");
            var sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = C(look.sunColor);
            sun.intensity = look.sunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = look.shadowStrength;
            sun.lightmapBakeType = LightmapBakeType.Mixed;
            sun.shadowAngle = 12f; // geniş, yumuşak bake gölgesi (bulutlu gök)
            go.transform.rotation = Quaternion.Euler(V(look.sunEuler));
            return sun;
        }

        static void CreateLavaLights(DenemeLayout layout)
        {
            var parent = new GameObject("LavaGlow_BakedLights").transform;
            int i = 0;
            foreach (LavaLightSpec l in layout.lavaLights)
            {
                var go = new GameObject($"LavaGlow_{i++:00}");
                go.transform.SetParent(parent, false);
                go.transform.position = V(l.pos);
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.45f, 0.16f);
                light.range = l.range;
                light.intensity = l.intensity;
                light.shadows = LightShadows.None;
                light.lightmapBakeType = LightmapBakeType.Baked; // çalışma anında maliyetsiz
            }
        }

        static GameObject InstantiateModel(string path, string name, Transform parent)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) { Line("WARN: missing model " + path); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        static void SetMaterial(GameObject go, Material m)
        {
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
        }

        static void MarkStatic(GameObject go, bool gi, float lightmapScale)
        {
            StaticEditorFlags flags = StaticEditorFlags.BatchingStatic;
            if (gi) flags |= StaticEditorFlags.ContributeGI;
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.receiveGI = gi ? ReceiveGI.Lightmaps : ReceiveGI.LightProbes;
                if (!gi) continue;
                var so = new SerializedObject(r);
                SerializedProperty scale = so.FindProperty("m_ScaleInLightmap");
                if (scale != null) { scale.floatValue = lightmapScale; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
        }

        static void SetShadows(GameObject go, ShadowCastingMode mode)
        {
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true))
                r.shadowCastingMode = mode;
        }

        static void CreateGround(DenemeLayout layout, Transform parent)
        {
            LookSpec look = layout.look;
            Material ground = LitMaterial("Ground_Basalt",
                Tex($"{PolyHavenDir}/dark_rock/dark_rock_diff_1k.jpg"),
                Tex($"{PolyHavenDir}/dark_rock/dark_rock_nor_gl_1k.jpg"),
                C(look.groundTint), 0.1f, 1f / Mathf.Max(0.1f, layout.groundTiling));
            Texture2D detail = Tex($"{PolyHavenDir}/burned_ground_01/burned_ground_01_detail_1k.jpg");
            Texture2D detailN = Tex($"{PolyHavenDir}/burned_ground_01/burned_ground_01_nor_gl_1k.jpg");
            if (detail != null)
            {
                // URP Lit: detay UV'si taban UV'sine (zaten ölçekli) göre çarpılır.
                float rel = layout.groundTiling / Mathf.Max(0.1f, layout.detailTiling);
                ground.SetTexture("_DetailAlbedoMap", detail);
                ground.SetFloat("_DetailAlbedoMapScale", 1f);
                if (detailN != null)
                {
                    ground.SetTexture("_DetailNormalMap", detailN);
                    ground.SetFloat("_DetailNormalMapScale", 0.6f);
                }
                ground.SetTextureScale("_DetailAlbedoMap", new Vector2(rel, rel));
                ground.EnableKeyword("_DETAIL_MULX2");
                EditorUtility.SetDirty(ground);
            }

            GameObject near = InstantiateModel($"{MeshDir}/Ground_Near.obj", "Ground_Near", parent);
            GameObject far = InstantiateModel($"{MeshDir}/Ground_Far.obj", "Ground_Far", parent);
            if (near != null)
            {
                SetMaterial(near, ground);
                MarkStatic(near, true, 1f);
                SetShadows(near, ShadowCastingMode.Off);
                foreach (MeshFilter mf in near.GetComponentsInChildren<MeshFilter>())
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                CheckWinding(near, ground);
            }
            if (far != null)
            {
                SetMaterial(far, ground);
                MarkStatic(far, true, 0.03f);
                SetShadows(far, ShadowCastingMode.Off);
            }
        }

        /// <summary>OBJ eksen çevirisi beklenenden farklıysa zemin alttan görünür; o durumda çift yüzlü çiz.</summary>
        static void CheckWinding(GameObject go, Material mat)
        {
            MeshFilter mf = go.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;
            Mesh mesh = mf.sharedMesh;
            int[] tri = mesh.triangles;
            Vector3[] v = mesh.vertices;
            if (tri.Length < 3 || v.Length == 0)
            {
                Line("WARN: ground mesh not readable for winding check");
                return;
            }
            int up = 0, down = 0;
            for (int i = 0; i + 2 < tri.Length && i < 3000; i += 3)
            {
                Vector3 n = Vector3.Cross(v[tri[i + 1]] - v[tri[i]], v[tri[i + 2]] - v[tri[i]]);
                if (n.y > 0f) up++; else down++;
            }
            if (down > up)
            {
                mat.SetFloat("_Cull", 0f);
                EditorUtility.SetDirty(mat);
                Line($"WARN: ground winding faces down ({down}/{up + down}); ground material set to Cull Off");
            }
            else Line($"ground winding ok ({up}/{up + down} up)");
        }

        static void CreateGeneratedUnlit(DenemeLayout layout, Transform parent)
        {
            LookSpec look = layout.look;
            Material lava = LavaMaterial("Lava_Cracks", C(look.lavaCore), C(look.lavaEdge), 0.45f, look.lavaPulse, 1f);
            GameObject cracks = InstantiateModel($"{MeshDir}/LavaCracks.obj", "LavaCracks", parent);
            if (cracks != null)
            {
                SetMaterial(cracks, lava);
                MarkStatic(cracks, false, 0f);
                SetShadows(cracks, ShadowCastingMode.Off);
            }

            Shader skyShader = FindShader("Dovus/Visual/FogSilhouette", "Universal Render Pipeline/Unlit");
            var sky = new Material(skyShader) { name = "Skyline_Silhouette" };
            if (skyShader != null && skyShader.name == "Dovus/Visual/FogSilhouette")
            {
                sky.SetColor("_BottomColor", C(look.skyBottom));
                sky.SetColor("_TopColor", C(look.skyTop));
                sky.SetFloat("_FogStrength", look.skyFogStrength);
            }
            else sky.SetColor("_BaseColor", C(look.skyTop));
            sky = SaveMaterial(sky, "Skyline_Silhouette");
            GameObject skyline = InstantiateModel($"{MeshDir}/Skyline.obj", "Skyline_Far", parent);
            if (skyline != null)
            {
                SetMaterial(skyline, sky);
                MarkStatic(skyline, false, 0f);
                SetShadows(skyline, ShadowCastingMode.Off);
            }

            Shader mistShader = FindShader("Dovus/Visual/EdgeMist", "Universal Render Pipeline/Unlit");
            if (mistShader != null && mistShader.name == "Dovus/Visual/EdgeMist")
            {
                var mist = new Material(mistShader) { name = "Edge_Mist" };
                mist.SetColor("_MistColor", C(look.mistColor));
                mist.SetFloat("_Alpha", look.mistAlpha);
                mist = SaveMaterial(mist, "Edge_Mist");
                GameObject band = InstantiateModel($"{MeshDir}/EdgeMist.obj", "EdgeMist", parent);
                if (band != null)
                {
                    SetMaterial(band, mist);
                    MarkStatic(band, false, 0f);
                    SetShadows(band, ShadowCastingMode.Off);
                }
            }
            else Line("WARN: EdgeMist skipped (shader unavailable)");
        }

        static void CreateRocks(DenemeLayout layout, Transform parent)
        {
            LookSpec look = layout.look;
            var mats = new Dictionary<string, Material>();
            foreach (string id in RockIds)
            {
                mats[id] = LitMaterial("Rock_" + id,
                    Tex($"{PolyHavenDir}/{id}/{id}_diff_1k.jpg"),
                    Tex($"{PolyHavenDir}/{id}/{id}_nor_gl_1k.jpg"),
                    C(look.rockTint), 0.12f, 1f);
            }
            var groups = new Dictionary<string, Transform>();
            int count = 0;
            foreach (RockPlacement r in layout.rocks)
            {
                if (!mats.ContainsKey(r.model)) continue;
                if (!groups.TryGetValue(r.group, out Transform g))
                {
                    g = new GameObject("Rocks_" + r.group).transform;
                    g.SetParent(parent, false);
                    groups[r.group] = g;
                }
                GameObject go = InstantiateModel($"{PolyHavenDir}/{r.model}/{r.model}_lod.obj", $"{r.model}_{count++:000}", g);
                if (go == null) continue;
                float tx = r.tilt != null && r.tilt.Length > 0 ? r.tilt[0] : 0f;
                float tz = r.tilt != null && r.tilt.Length > 1 ? r.tilt[1] : 0f;
                go.transform.position = V(r.pos);
                go.transform.rotation = Quaternion.Euler(tx, r.rotY, tz);
                go.transform.localScale = Vector3.one * r.scale;
                SetMaterial(go, mats[r.model]);
                bool mountain = r.group == "mountain";
                float lmScale = mountain ? 0.03f : r.group == "mid" ? 0.15f : 0.5f;
                MarkStatic(go, true, lmScale);
                SetShadows(go, mountain ? ShadowCastingMode.Off : ShadowCastingMode.On);
            }
            Line($"rocks placed: {count}");
        }

        static void CreateBoundary(DenemeLayout layout)
        {
            BoundarySpec b = layout.boundary;
            var root = new GameObject("PlayBoundary_Invisible");
            int n = Mathf.Max(8, b.segments);
            float seg = 2f * Mathf.PI * b.radius / n * 1.08f;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                var go = new GameObject($"Wall_{i:00}");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(Mathf.Cos(a) * b.radius, b.height * 0.5f - 1f, Mathf.Sin(a) * b.radius);
                go.transform.rotation = Quaternion.LookRotation(-go.transform.position.normalized, Vector3.up);
                var box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(seg, b.height, b.thickness);
            }
        }

        static void CreateProbes(DenemeLayout layout)
        {
            var go = new GameObject("LightProbes");
            var group = go.AddComponent<LightProbeGroup>();
            float[] f = layout.probesFlat ?? new float[0];
            var list = new List<Vector3>();
            for (int i = 0; i + 2 < f.Length; i += 3) list.Add(new Vector3(f[i], f[i + 1], f[i + 2]));
            group.probePositions = list.ToArray();
        }

        // ------------------------------------------------------------------ dragon

        static GameObject CreateDragon(DenemeLayout layout)
        {
            DragonSpec d = layout.dragon;
            LookSpec look = layout.look;
            var root = new GameObject("Dragon");
            root.transform.position = V(d.pos);
            root.transform.rotation = Quaternion.Euler(0f, d.rotY, 0f);

            Material skin = LitMaterial("Dragon_Placeholder", null, null, C(look.dragonColor), 0.18f, 1f);
            GameObject visual = InstantiateModel(PlaceholderPath, "Visual_AIPlaceholder", root.transform);
            if (visual != null)
            {
                visual.transform.localScale = Vector3.one * d.scale;
                SetMaterial(visual, skin);
            }

            Material eyeMat = LavaMaterial("Dragon_EyeGlow", C(look.eyeColor), new Color(1f, 0.42f, 0.1f), 0.9f, 0.1f, 0.45f);
            var eyes = new List<Renderer>();
            foreach (float[] p in new[] { d.eyeL, d.eyeR })
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(eye.GetComponent<Collider>());
                eye.name = eyes.Count == 0 ? "Eye_L" : "Eye_R";
                eye.transform.SetParent(visual != null ? visual.transform : root.transform, false);
                eye.transform.localPosition = V(p);
                eye.transform.localScale = V(d.eyeScale);
                var r = eye.GetComponent<MeshRenderer>();
                r.sharedMaterial = eyeMat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                eyes.Add(r);
            }

            var breathing = root.AddComponent<DragonBreathing>();
            breathing.target = visual != null ? visual.transform : root.transform;
            var glow = root.AddComponent<DragonEyeGlow>();
            glow.eyes = eyes.ToArray();

            string dm = FindDungeonMasonDragon();
            if (!string.IsNullOrEmpty(dm))
                Line(SwapDragonInScene(root, dm));
            return root;
        }

        static string DescribeDragon(GameObject root)
        {
            if (root == null) return "none";
            foreach (Transform t in root.transform)
                if (t.gameObject.activeSelf && t.name.StartsWith("Visual")) return t.name;
            return "?";
        }

        /// <summary>Asset Store'dan eklenen Dungeon Mason ejderhasını bulur (yoksa null).</summary>
        static string FindDungeonMasonDragon()
        {
            string best = null;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string lower = path.ToLowerInvariant();
                if (!lower.Contains("dragon") || lower.StartsWith("assets/art/")) continue;
                if (!(lower.Contains("mason") || lower.Contains("boss monster") || lower.Contains("bossmonster"))) continue;
                if (lower.Contains("demo") || lower.Contains("sample")) continue;
                if (best == null || path.Length < best.Length) best = path;
            }
            return best;
        }

        /// <summary>
        /// Ejderha görselini verilen prefab ile değiştirir (Dungeon Mason için). Yer tutucu silinmez,
        /// kapatılır. Açık sahne DenemeSahnesi olmalı. Otomasyon: SwapDragon("Assets/.../Dragon.prefab").
        /// </summary>
        public static string SwapDragon(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath)) prefabPath = FindDungeonMasonDragon();
            if (string.IsNullOrEmpty(prefabPath)) return "ABORT: prefab seçilmedi ve Dungeon Mason ejderhası bulunamadı.";
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) return "ABORT: önce " + ScenePath + " sahnesini aç.";
            GameObject root = GameObject.Find("Dragon");
            if (root == null) return "ABORT: sahnede 'Dragon' yok; önce Build çalıştır.";
            string result = SwapDragonInScene(root, prefabPath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return result;
        }

        static string SwapDragonInScene(GameObject root, string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return "WARN: dragon prefab not loadable: " + prefabPath;
            Transform old = root.transform.Find("Visual_AIPlaceholder");
            Bounds oldBounds = old != null ? WorldBounds(old.gameObject) : new Bounds(root.transform.position + Vector3.up * 20f, Vector3.one * 40f);

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            visual.name = "Visual_" + prefab.name;
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            Bounds nb = WorldBounds(visual);
            if (nb.size.y > 0.001f) visual.transform.localScale *= oldBounds.size.y / nb.size.y;
            nb = WorldBounds(visual);
            visual.transform.position += new Vector3(oldBounds.center.x - nb.center.x, oldBounds.min.y - nb.min.y, oldBounds.center.z - nb.center.z);

            // Göz parıltılarını "eye" adlı kemiklere taşı; yoksa yer tutucu konumunda kalsınlar.
            var eyeBones = visual.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.ToLowerInvariant().Contains("eye")).Take(2).ToArray();
            var glow = root.GetComponent<DragonEyeGlow>();
            if (glow != null && glow.eyes != null)
            {
                for (int i = 0; i < glow.eyes.Length; i++)
                {
                    if (glow.eyes[i] == null) continue;
                    Transform e = glow.eyes[i].transform;
                    Vector3 worldScale = e.lossyScale;
                    if (i < eyeBones.Length)
                    {
                        e.SetParent(eyeBones[i], false);
                        e.localPosition = Vector3.zero;
                    }
                    else e.SetParent(visual.transform, true);
                    Vector3 ps = e.parent.lossyScale;
                    e.localScale = new Vector3(worldScale.x / Mathf.Max(1e-4f, ps.x), worldScale.y / Mathf.Max(1e-4f, ps.y), worldScale.z / Mathf.Max(1e-4f, ps.z));
                }
            }

            if (old != null)
            {
                old.gameObject.SetActive(false);
                old.name = "Visual_AIPlaceholder";
            }
            var breathing = root.GetComponent<DragonBreathing>();
            if (breathing != null)
            {
                breathing.target = visual.transform;
                // Animator'lı ejderhada nefes zaten animasyonda; kod nefesini yarıya indir.
                if (visual.GetComponentInChildren<Animator>() != null) breathing.scaleAmplitude *= 0.5f;
            }
            return $"dragon swapped to {prefabPath} (eye bones found: {eyeBones.Length})";
        }

        static Bounds WorldBounds(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        // ------------------------------------------------------------------ hero

        static GameObject CreateHero(DenemeLayout layout)
        {
            string source = FindHero(out GameObject prefab);
            if (prefab == null)
            {
                Line("WARN: no hero asset found; capsule placeholder");
                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                cap.name = "Hero_Capsule";
                cap.transform.position = V(layout.hero.pos) + Vector3.up;
                return cap;
            }
            var hero = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            hero.name = "Hero (" + source + ")";
            hero.transform.position = V(layout.hero.pos);
            hero.transform.rotation = Quaternion.Euler(0f, layout.hero.rotY, 0f);

            Bounds b = WorldBounds(hero);
            if (b.size.y > 0.2f && (b.size.y < 1.5f || b.size.y > 2.2f))
            {
                hero.transform.localScale *= 1.8f / b.size.y;
                Line($"hero rescaled from {b.size.y:0.00} m to 1.8 m");
            }

            Animator anim = hero.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                if (anim.runtimeAnimatorController == null)
                {
                    var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MixamoPlayerController);
                    if (ctrl != null) anim.runtimeAnimatorController = ctrl;
                }
            }
            ApplySoftHeroMaterials(hero);
            foreach (Renderer r in hero.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.On;
                r.lightProbeUsage = LightProbeUsage.BlendProbes;
            }
            Line("hero source: " + source);
            return hero;
        }

        /// <summary>Sıra: Mixamo karakteri (kullanıcı ekler) → Synty Hero Knight (yerel) → Quaternius (repoda).</summary>
        static string FindHero(out GameObject prefab)
        {
            prefab = null;
            if (AssetDatabase.IsValidFolder(MixamoCharacterDir))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { MixamoCharacterDir }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (mi != null && mi.animationType != ModelImporterAnimationType.Human)
                    {
                        mi.animationType = ModelImporterAnimationType.Human;
                        mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                        mi.SaveAndReimport();
                    }
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (HasMeshes(go)) { prefab = go; return "Mixamo " + Path.GetFileNameWithoutExtension(path); }
                }
            }
            var synty = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyHero);
            if (HasMeshes(synty)) { prefab = synty; return "Synty PlayerVisual"; }
            var quat = AssetDatabase.LoadAssetAtPath<GameObject>(QuaterniusHero);
            if (HasMeshes(quat)) { prefab = quat; return "Quaternius PlayerVisual"; }
            return null;
        }

        static bool HasMeshes(GameObject go)
        {
            if (go == null) return false;
            if (go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(s => s.sharedMesh != null)) return true;
            return go.GetComponentsInChildren<MeshFilter>(true).Any(m => m.sharedMesh != null);
        }

        static readonly string[] AlbedoProps = { "_BaseMap", "_MainTex", "_Albedo_Map", "_AlbedoMap", "_Albedo", "_Texture", "_BaseColorMap" };
        static readonly string[] ColorProps = { "_BaseColor", "_Color", "_Albedo_Tint", "_Tint" };

        static void ApplySoftHeroMaterials(GameObject hero)
        {
            Shader soft = Shader.Find("Dovus/Visual/SoftHero");
            if (soft == null || ShaderUtil.ShaderHasError(soft))
            {
                Line("WARN: SoftHero shader unavailable; hero keeps its original materials");
                return;
            }
            var map = new Dictionary<Material, Material>();
            foreach (Renderer r in hero.GetComponentsInChildren<Renderer>(true))
            {
                Material[] src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    Material s = src[i];
                    if (s == null) { dst[i] = null; continue; }
                    if (!map.TryGetValue(s, out Material d))
                    {
                        d = new Material(soft) { name = s.name + "_Soft" };
                        Texture tex = null;
                        foreach (string p in AlbedoProps)
                            if (s.HasProperty(p) && s.GetTexture(p) != null) { tex = s.GetTexture(p); break; }
                        if (tex == null) tex = s.mainTexture;
                        if (tex != null) d.SetTexture("_BaseMap", tex);
                        Color col = Color.white;
                        foreach (string p in ColorProps)
                            if (s.HasProperty(p)) { col = s.GetColor(p); break; }
                        d.SetColor("_BaseColor", col);
                        string safe = string.Concat(d.name.Split(Path.GetInvalidFileNameChars()));
                        string path = $"{HeroMatDir}/{safe}.mat";
                        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (existing != null)
                        {
                            existing.shader = soft;
                            existing.CopyPropertiesFromMaterial(d);
                            Object.DestroyImmediate(d);
                            d = existing;
                            EditorUtility.SetDirty(d);
                        }
                        else AssetDatabase.CreateAsset(d, path);
                        map[s] = d;
                    }
                    dst[i] = d;
                }
                r.sharedMaterials = dst;
            }
            Line($"hero soft materials: {map.Count}");
        }

        // ------------------------------------------------------------------ camera / post / overlay

        static Camera CreateCamera(DenemeLayout layout, LookSpec look, out CameraPushIn pushIn)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = C(look.fogColor);
            cam.fieldOfView = layout.camera.fov;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 1100f;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            go.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None; // MSAA 4x URP asset'ten; cihazda düğmeyle SMAA/FXAA denenir
            data.renderShadows = true;

            pushIn = go.AddComponent<CameraPushIn>();
            pushIn.startPosition = V(layout.camera.start);
            pushIn.endPosition = V(layout.camera.end);
            pushIn.lookAt = V(layout.camera.lookAt);
            pushIn.duration = layout.camera.duration;
            go.transform.position = pushIn.startPosition;
            go.transform.rotation = Quaternion.LookRotation(pushIn.lookAt - pushIn.startPosition, Vector3.up);
            return cam;
        }

        static void CreateVolume(LookSpec look)
        {
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath) != null)
                AssetDatabase.DeleteAsset(ProfilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(look.postExposure);
            color.contrast.Override(look.contrast);
            color.saturation.Override(look.saturation);
            color.colorFilter.Override(C(look.colorFilter));

            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.Override(look.temperature);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(look.bloomThreshold);
            bloom.intensity.Override(look.bloomIntensity);
            bloom.scatter.Override(look.bloomScatter);
            bloom.tint.Override(C(look.bloomTint));
            bloom.highQualityFiltering.Override(false);
            bloom.downscale.Override(BloomDownscaleMode.Quarter);
            bloom.maxIterations.Override(5);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(look.vignette);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(new Color(0.22f, 0.24f, 0.26f));

            foreach (VolumeComponent c in profile.components)
            {
                c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(c, profile);
            }
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var go = new GameObject("PostFX_Volume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
        }

        static void CreateOverlay(Camera cam, CameraPushIn pushIn)
        {
            var go = new GameObject("Debug_FpsCounter");
            var fps = go.AddComponent<FpsCounter>();
            fps.targetCamera = cam;
            fps.pushIn = pushIn;
        }

        // ------------------------------------------------------------------ preview shot

        /// <summary>
        /// Edit modunda sahne kamerasından PNG alır (Play gerekmez). t: 0 = kamera başlangıcı, 1 = yaklaşma sonu.
        /// Otomasyon: CaptureShot(@"C:\Users\...\deneme-end.png", 1600, 720, 1f).
        /// </summary>
        public static string CaptureShot(string path, int width, int height, float t)
        {
            if (SceneManager.GetActiveScene().path != ScenePath) return "ABORT: önce " + ScenePath + " sahnesini aç.";
            var camGo = GameObject.Find("Main Camera");
            Camera cam = camGo != null ? camGo.GetComponent<Camera>() : null;
            var push = camGo != null ? camGo.GetComponent<CameraPushIn>() : null;
            if (cam == null || push == null) return "ABORT: Main Camera / CameraPushIn yok.";
            Vector3 oldPos = cam.transform.position;
            Quaternion oldRot = cam.transform.rotation;
            float e = Mathf.Clamp01(t);
            e = e * e * (3f - 2f * e);
            Vector3 p = Vector3.Lerp(push.startPosition, push.endPosition, e);
            cam.transform.position = p;
            cam.transform.rotation = Quaternion.LookRotation(push.lookAt - p, Vector3.up);
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            RenderTexture prevTarget = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                return "OK shot " + path;
            }
            finally
            {
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                cam.transform.position = oldPos;
                cam.transform.rotation = oldRot;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }

        /// <summary>4 shader'ın derleme durumunu döner ("OK" ya da hata mesajları).</summary>
        public static string ShaderReport()
        {
            var sb = new StringBuilder();
            foreach (string n in new[] { "Dovus/Visual/LavaEmissive", "Dovus/Visual/FogSilhouette", "Dovus/Visual/EdgeMist", "Dovus/Visual/SoftHero" })
            {
                Shader s = Shader.Find(n);
                if (s == null) { sb.AppendLine(n + ": NOT FOUND"); continue; }
                ShaderMessage[] msgs = ShaderUtil.GetShaderMessages(s);
                bool err = ShaderUtil.ShaderHasError(s);
                sb.AppendLine($"{n}: {(err ? "ERROR" : "OK")} ({msgs.Length} messages)");
                foreach (ShaderMessage m in msgs)
                    sb.AppendLine($"  [{m.severity}] {m.message} (line {m.line}, {m.platform})");
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ bake

        /// <summary>Işık bake'ini başlatır (asenkron). Bitince sahne otomatik kaydedilir. Durum: <see cref="BakeStatus"/>.</summary>
        public static string StartBake()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
                return "ABORT: önce " + ScenePath + " sahnesini aç (Build bunu yapar).";
            if (Lightmapping.isRunning) return "RUNNING: bake zaten sürüyor";
            Lightmapping.bakeCompleted -= OnBakeCompleted;
            Lightmapping.bakeCompleted += OnBakeCompleted;
            bool ok = Lightmapping.BakeAsync();
            return ok ? "STARTED: bake başladı; BakeStatus() ile izle" : "ABORT: BakeAsync başlatılamadı";
        }

        public static string BakeStatus()
        {
            if (Lightmapping.isRunning) return $"RUNNING {Lightmapping.buildProgress * 100f:0}%";
            int maps = LightmapSettings.lightmaps != null ? LightmapSettings.lightmaps.Length : 0;
            return $"IDLE lightmaps={maps} scene={SceneManager.GetActiveScene().path} dirty={SceneManager.GetActiveScene().isDirty}";
        }

        static void OnBakeCompleted()
        {
            Lightmapping.bakeCompleted -= OnBakeCompleted;
            Scene s = SceneManager.GetActiveScene();
            if (s.path == ScenePath)
            {
                EditorSceneManager.MarkSceneDirty(s);
                EditorSceneManager.SaveScene(s);
                Debug.Log("[DenemeSahnesi] bake bitti, sahne kaydedildi. " + BakeStatus());
            }
        }
    }
}
