using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dovus.Game;
using UnityEngine;

namespace SweepV2
{
    /// <summary>
    /// Prototype.unity'yi koddan kurar: Bootstrap nesnesi + sahnedeki serileştirilmiş
    /// PrototypeTuning değerleri + görsel prefab yerine insansı iskelet.
    /// </summary>
    static class SceneSetup
    {
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static PrototypeBootstrap Build(string repoRoot)
        {
            RunInitializers(RuntimeInitializeLoadType.SubsystemRegistration);
            RunInitializers(RuntimeInitializeLoadType.AfterAssembliesLoaded);
            RunInitializers(RuntimeInitializeLoadType.BeforeSplashScreen);
            RunInitializers(RuntimeInitializeLoadType.BeforeSceneLoad);

            string scene = Path.Combine(repoRoot, "unity", "Assets", "Scenes", "Prototype.unity");
            var tuning = new PrototypeTuning();
            int applied = ApplySceneTuning(tuning, File.ReadAllLines(scene));

            var go = new GameObject("Bootstrap");
            go.SetActive(false);
            var boot = go.AddComponent<PrototypeBootstrap>();
            Set(boot, "_tuning", tuning);
            Set(boot, "_playerVisualPrefab", HumanoidVisual("PlayerVisual_Headless", 1.8f, 0.5f));
            Set(boot, "_bossVisualPrefab", HumanoidVisual("BossVisual_Headless", 1.8f, 0.6f));
            go.SetActive(true);

            RunInitializers(RuntimeInitializeLoadType.AfterSceneLoad);
            UnityEngine.SceneManagement.SceneManager.RaiseLoaded();
            Console.WriteLine($"[SweepV2] sahne kuruldu: {applied} tuning alanı Prototype.unity'den");
            return boot;
        }

        static void Set(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BF)
                          ?? throw new MissingFieldException(target.GetType().Name, field);
            f.SetValue(target, value);
        }

        static void RunInitializers(RuntimeInitializeLoadType when)
        {
            var methods = typeof(PrototypeBootstrap).Assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Select(m => (m, attr: m.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>()))
                .Where(x => x.attr != null && x.attr.loadType == when)
                .Select(x => x.m)
                .ToList();
            foreach (MethodInfo m in methods)
            {
                try
                {
                    m.Invoke(null, null);
                }
                catch (TargetInvocationException e)
                {
                    Debug.LogException(e.InnerException ?? e);
                }
            }
        }

        /// <summary>Sahne YAML'ındaki `_tuning:` bloğu → alan alan. Yazılmayan alanlar C# varsayılanında kalır (Unity gibi).</summary>
        static int ApplySceneTuning(PrototypeTuning tuning, string[] lines)
        {
            int start = Array.FindIndex(lines, l => l.TrimEnd() == "  _tuning:");
            if (start < 0) throw new InvalidDataException("Prototype.unity: _tuning bloğu yok");
            int applied = 0;
            var num = new Regex(@"^    (\w+): (.+)$");
            for (int i = start + 1; i < lines.Length; i++)
            {
                Match m = num.Match(lines[i]);
                if (!m.Success) break;
                FieldInfo f = typeof(PrototypeTuning).GetField(m.Groups[1].Value, BF);
                if (f == null) continue;
                object v = Parse(f.FieldType, m.Groups[2].Value.Trim());
                if (v == null) continue;
                f.SetValue(tuning, v);
                applied++;
            }
            return applied;
        }

        static object Parse(Type t, string s)
        {
            var ci = CultureInfo.InvariantCulture;
            if (t == typeof(float)) return float.Parse(s, ci);
            if (t == typeof(int)) return int.Parse(s, ci);
            if (t == typeof(bool)) return s == "1";
            if (t == typeof(string)) return s;
            if (t.IsEnum) return Enum.ToObject(t, int.Parse(s, ci));
            if (t == typeof(Vector3) || t == typeof(Vector2) || t == typeof(Color))
            {
                var parts = new Dictionary<string, float>();
                foreach (Match m in Regex.Matches(s, @"(\w+): (-?[\d.eE+-]+)"))
                    parts[m.Groups[1].Value] = float.Parse(m.Groups[2].Value, ci);
                float G(string k) => parts.TryGetValue(k, out float v) ? v : 0f;
                if (t == typeof(Vector3)) return new Vector3(G("x"), G("y"), G("z"));
                if (t == typeof(Vector2)) return new Vector2(G("x"), G("y"));
                return new Color(G("r"), G("g"), G("b"), G("a"));
            }
            return null;
        }

        /// <summary>
        /// Synty prefab'ının başsız karşılığı: insansı Animator + gövde kutusu + ayak kemikleri.
        /// Kontrolcü yok (Play'de klipler yalnız görsel); ActorGrounding ayağı kemikten ölçer.
        /// </summary>
        static GameObject HumanoidVisual(string name, float heightM, float widthM)
        {
            var root = new GameObject(name, true);
            var anim = root.AddComponent<Animator>();
            anim.isHuman = true;

            var body = Child(root.transform, "Body", Vector3.zero);
            var skin = body.gameObject.AddComponent<SkinnedMeshRenderer>();
            var mesh = new Mesh { name = name + "_Body" };
            float hw = widthM * 0.5f;
            var verts = new List<Vector3>();
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                verts.Add(new Vector3(hw * x, 0f, hw * 0.6f * z));
                verts.Add(new Vector3(hw * x, heightM, hw * 0.6f * z));
            }
            mesh.vertices = verts.ToArray();
            skin.sharedMesh = mesh;

            var hips = Child(root.transform, "Hips", new Vector3(0f, heightM * 0.53f, 0f));
            Child(hips, "LeftFoot", new Vector3(-0.1f, 0.08f - heightM * 0.53f, 0f));
            Child(hips, "RightFoot", new Vector3(0.1f, 0.08f - heightM * 0.53f, 0f));
            Child(hips, "LeftToes", new Vector3(-0.1f, 0.03f - heightM * 0.53f, 0.12f));
            Child(hips, "RightToes", new Vector3(0.1f, 0.03f - heightM * 0.53f, 0.12f));
            return root;
        }

        static Transform Child(Transform parent, string name, Vector3 local)
        {
            var go = new GameObject(name, true);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            return go.transform;
        }
    }
}
