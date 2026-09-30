using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace UnityEngine
{
    /// <summary>
    /// Başsız sahne: GameObject kaydı, mesaj sırası (Awake/OnEnable/Start/Update/LateUpdate),
    /// ertelenmiş Destroy ve PlayerLoop PostLateUpdate kancaları. Unity'nin kare sırasını izler.
    /// </summary>
    public static class World
    {
        static readonly List<GameObject> s_objects = new();
        static readonly List<MonoBehaviour> s_behaviours = new();
        static readonly List<ScriptableObject> s_assets = new();
        static readonly List<(Object obj, double at)> s_timedDestroy = new();
        static readonly List<Object> s_pendingDestroy = new();
        static readonly Dictionary<(Type, string), Action<object>> s_messages = new();
        static readonly Dictionary<(Type, string), MethodInfo> s_messagesWithArg = new();
        static readonly Dictionary<Type, int> s_orders = new();
        static long s_seq;
        static bool s_sorted = true;
        static readonly List<MonoBehaviour> s_frame = new();

        public static int FrameCount { get; private set; }

        internal static IEnumerable<GameObject> AllGameObjects()
        {
            for (int i = 0; i < s_objects.Count; i++)
            {
                GameObject g = s_objects[i];
                if (!g.Destroyed && !g.IsAsset) yield return g;
            }
        }

        internal static void Register(GameObject go) => s_objects.Add(go);

        internal static void RegisterAsset(ScriptableObject so) => s_assets.Add(so);

        internal static int RootCount()
        {
            int n = 0;
            foreach (GameObject g in s_objects)
                if (!g.Destroyed && !g.IsAsset && !g.DontDestroy && g.transform._parent == null) n++;
            return n;
        }

        internal static IEnumerable<GameObject> Roots() =>
            s_objects.Where(g => !g.Destroyed && !g.IsAsset && !g.DontDestroy && g.transform._parent == null);

        internal static int RootIndex(Transform t) => Roots().Select(g => g.transform).ToList().IndexOf(t);

        internal static IEnumerable<T> Find<T>(bool includeInactive) where T : Object
        {
            if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
            {
                foreach (ScriptableObject so in s_assets)
                    if (so is T t && !so.Destroyed) yield return t;
                yield break;
            }
            if (typeof(T) == typeof(GameObject))
            {
                foreach (GameObject g in AllGameObjects())
                    if (includeInactive || g.activeInHierarchy) yield return g as T;
                yield break;
            }
            for (int i = 0; i < s_objects.Count; i++)
            {
                GameObject g = s_objects[i];
                if (g.Destroyed || g.IsAsset) continue;
                if (!includeInactive && !g.activeInHierarchy) continue;
                IReadOnlyList<Component> comps = g.Components;
                for (int k = 0; k < comps.Count; k++)
                {
                    Component c = comps[k];
                    if (c is T t && !c.Destroyed)
                    {
                        if (!includeInactive && c is Behaviour b && !b.enabled) continue;
                        yield return t;
                    }
                }
            }
        }

        static int OrderOf(Type t)
        {
            if (s_orders.TryGetValue(t, out int o)) return o;
            var attr = (DefaultExecutionOrder)Attribute.GetCustomAttribute(t, typeof(DefaultExecutionOrder), true);
            o = attr?.order ?? 0;
            s_orders[t] = o;
            return o;
        }

        internal static void OnComponentAdded(Component c)
        {
            if (c is Collider col) Physics.Register(col);
            if (c is Camera cam) Camera.Register(cam);
            if (!(c is MonoBehaviour mb)) return;
            mb.Order = OrderOf(mb.GetType());
            mb.Seq = ++s_seq;
            if (mb.gameObject.IsAsset) return;
            s_behaviours.Add(mb);
            s_sorted = false;
            if (mb.gameObject.activeInHierarchy) Activate(mb);
        }

        static void Activate(MonoBehaviour mb)
        {
            if (!mb.Awoken)
            {
                mb.Awoken = true;
                InvokeMessage(mb, "Awake");
                if (mb.Destroyed) return;
            }
            if (mb.enabled && !mb.EnabledCalled && mb.gameObject.activeInHierarchy)
            {
                mb.EnabledCalled = true;
                InvokeMessage(mb, "OnEnable");
            }
        }

        static void Deactivate(MonoBehaviour mb)
        {
            if (!mb.EnabledCalled) return;
            mb.EnabledCalled = false;
            InvokeMessage(mb, "OnDisable");
        }

        internal static void OnEnabledChanged(Behaviour b)
        {
            if (!(b is MonoBehaviour mb) || mb.Destroyed || mb.gameObject == null || mb.gameObject.IsAsset) return;
            if (!mb.gameObject.activeInHierarchy) return;
            if (mb.enabled) Activate(mb);
            else Deactivate(mb);
        }

        internal static void OnActiveChanged(GameObject root)
        {
            if (root.IsAsset) return;
            var list = new List<MonoBehaviour>();
            CollectBehaviours(root.transform, list);
            foreach (MonoBehaviour mb in list)
            {
                if (mb.Destroyed) continue;
                if (mb.gameObject.activeInHierarchy) Activate(mb);
                else Deactivate(mb);
            }
        }

        static void CollectBehaviours(Transform t, List<MonoBehaviour> list)
        {
            foreach (Component c in t.gameObject.Components)
                if (c is MonoBehaviour mb) list.Add(mb);
            foreach (Transform ch in t._children) CollectBehaviours(ch, list);
        }

        internal static void ScheduleDestroy(Object obj, float t)
        {
            if (t <= 0f) s_pendingDestroy.Add(obj);
            else s_timedDestroy.Add((obj, Time.timeAsDouble + t));
        }

        internal static void DestroyNow(Object obj)
        {
            if (obj is null || obj.Destroyed) return;
            switch (obj)
            {
                case GameObject go:
                    DestroyGameObject(go);
                    break;
                case Transform _:
                    Debug.LogError("Destroying the transform component is not allowed.");
                    break;
                case Component c:
                    DestroyComponent(c);
                    break;
                default:
                    obj.Destroyed = true;
                    if (obj is ScriptableObject so)
                    {
                        InvokeMessage(so, "OnDisable");
                        InvokeMessage(so, "OnDestroy");
                    }
                    break;
            }
        }

        static void DestroyGameObject(GameObject go)
        {
            var all = new List<GameObject>();
            CollectTree(go.transform, all);
            foreach (GameObject g in all)
            {
                foreach (Component c in g.Components.ToArray())
                    if (c is MonoBehaviour mb && !mb.Destroyed) Deactivate(mb);
            }
            foreach (GameObject g in all)
            {
                foreach (Component c in g.Components.ToArray())
                {
                    if (c.Destroyed) continue;
                    if (c is MonoBehaviour mb && mb.Awoken) InvokeMessage(mb, "OnDestroy");
                    c.Destroyed = true;
                    if (c is Collider col) Physics.Unregister(col);
                }
                g.Destroyed = true;
            }
            go.transform._parent?._children.Remove(go.transform);
            go.transform._parent = null;
        }

        static void CollectTree(Transform t, List<GameObject> all)
        {
            all.Add(t.gameObject);
            foreach (Transform c in t._children.ToArray()) CollectTree(c, all);
        }

        static void DestroyComponent(Component c)
        {
            if (c is MonoBehaviour mb)
            {
                Deactivate(mb);
                if (mb.Awoken) InvokeMessage(mb, "OnDestroy");
            }
            c.Destroyed = true;
            if (c is Collider col) Physics.Unregister(col);
            c.gameObject?.RemoveComponent(c);
        }

        public static void InvokeMessage(object target, string name)
        {
            Type t = target.GetType();
            if (!s_messages.TryGetValue((t, name), out Action<object> call))
            {
                call = BuildCall(t, name);
                s_messages[(t, name)] = call;
            }
            if (call == null) return;
            try
            {
                call(target);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal static void InvokeMessage(object target, string name, object arg)
        {
            Type t = target.GetType();
            if (!s_messagesWithArg.TryGetValue((t, name), out MethodInfo m))
            {
                m = FindMethod(t, name, 1);
                s_messagesWithArg[(t, name)] = m;
            }
            if (m == null)
            {
                InvokeMessage(target, name);
                return;
            }
            try
            {
                m.Invoke(target, new[] { arg });
            }
            catch (TargetInvocationException e)
            {
                Debug.LogException(e.InnerException ?? e);
            }
        }

        static MethodInfo FindMethod(Type t, string name, int args)
        {
            for (Type k = t; k != null && k != typeof(MonoBehaviour) && k != typeof(ScriptableObject); k = k.BaseType)
            {
                foreach (MethodInfo m in k.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (m.Name == name && m.GetParameters().Length == args && !m.IsGenericMethodDefinition)
                        return m;
                }
            }
            return null;
        }

        static Action<object> BuildCall(Type t, string name)
        {
            MethodInfo m = FindMethod(t, name, 0);
            if (m == null) return null;
            ParameterExpression p = Expression.Parameter(typeof(object), "o");
            Expression call = Expression.Call(Expression.Convert(p, m.DeclaringType), m);
            return Expression.Lambda<Action<object>>(call, p).Compile();
        }

        internal static bool HasMessage(Type t, string name)
        {
            if (!s_messages.TryGetValue((t, name), out Action<object> call))
            {
                call = BuildCall(t, name);
                s_messages[(t, name)] = call;
            }
            return call != null;
        }

        static void SortBehaviours()
        {
            if (s_sorted) return;
            s_behaviours.RemoveAll(b => b.Destroyed);
            s_behaviours.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Seq.CompareTo(b.Seq));
            s_sorted = true;
        }

        /// <summary>Tek kare: Start → Update → LateUpdate → PostLateUpdate kancaları → editör update → Destroy.</summary>
        public static void Step(float unscaledDt)
        {
            FrameCount++;
            Time.Advance(unscaledDt);
            SortBehaviours();
            s_frame.Clear();
            s_frame.AddRange(s_behaviours);

            for (int i = 0; i < s_frame.Count; i++)
            {
                MonoBehaviour mb = s_frame[i];
                if (mb.Started || mb.Destroyed || !mb.isActiveAndEnabled) continue;
                mb.Started = true;
                InvokeMessage(mb, "Start");
            }

            Physics.StepTriggers();

            RunPhase("Update");
            RunPhase("LateUpdate");
            StepParticles();
            LowLevel.PlayerLoop.RunPostLateUpdate();
            UnityEditor.EditorApplication.RunUpdate();
            ProcessDestroys();
        }

        static readonly List<ParticleSystem> s_particles = new();

        internal static void TrackParticle(ParticleSystem ps)
        {
            if (!s_particles.Contains(ps)) s_particles.Add(ps);
        }

        static void StepParticles()
        {
            double now = Time.timeAsDouble;
            for (int i = s_particles.Count - 1; i >= 0; i--)
            {
                ParticleSystem ps = s_particles[i];
                if (ps.Destroyed || ps.gameObject == null || ps.gameObject.Destroyed)
                {
                    s_particles.RemoveAt(i);
                    continue;
                }
                if (!ps.Finished(now)) continue;
                s_particles.RemoveAt(i);
                if (ps.StopAction == ParticleSystemStopAction.Destroy) Object.Destroy(ps.gameObject);
                else if (ps.StopAction == ParticleSystemStopAction.Disable) ps.gameObject.SetActive(false);
            }
        }

        static void RunPhase(string phase)
        {
            for (int i = 0; i < s_frame.Count; i++)
            {
                MonoBehaviour mb = s_frame[i];
                if (!mb.Started || mb.Destroyed || !mb.isActiveAndEnabled) continue;
                InvokeMessage(mb, phase);
            }
        }

        public static void ProcessDestroys()
        {
            if (s_pendingDestroy.Count > 0)
            {
                Object[] now = s_pendingDestroy.ToArray();
                s_pendingDestroy.Clear();
                foreach (Object o in now) DestroyNow(o);
            }
            if (s_timedDestroy.Count > 0)
            {
                double t = Time.timeAsDouble;
                for (int i = s_timedDestroy.Count - 1; i >= 0; i--)
                {
                    if (s_timedDestroy[i].at <= t)
                    {
                        Object o = s_timedDestroy[i].obj;
                        s_timedDestroy.RemoveAt(i);
                        DestroyNow(o);
                    }
                }
            }
            if (s_objects.Count > 4096)
                s_objects.RemoveAll(g => g.Destroyed);
        }

        // ------------------------------------------------------------ Instantiate

        static readonly MethodInfo s_memberwiseClone =
            typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static Object Clone(Object original, Transform parent, bool worldStays, Vector3? pos, Quaternion? rot)
        {
            if (original == null) throw new ArgumentException("The Object you want to instantiate is null.");
            if (original is ScriptableObject so)
            {
                var copy = (ScriptableObject)s_memberwiseClone.Invoke(so, null);
                RegisterAsset(copy);
                return copy;
            }

            GameObject srcGo = original as GameObject ?? (original as Component)?.gameObject;
            if (srcGo == null) throw new NotSupportedException("Instantiate: " + original.GetType().Name);
            var map = new Dictionary<Object, Object>();
            var added = new List<Component>();
            GameObject clone = CloneTree(srcGo, null, map, added);
            clone.name = srcGo.name + "(Clone)";

            foreach (Component c in added) RemapFields(c, map);

            if (parent != null) clone.transform.SetParent(parent, worldStays);
            if (pos.HasValue) clone.transform.position = pos.Value;
            if (rot.HasValue) clone.transform.rotation = rot.Value;

            foreach (Component c in added) OnComponentAdded(c);

            return original is Component oc ? (Object)map[oc] : clone;
        }

        static GameObject CloneTree(GameObject src, Transform parent, Dictionary<Object, Object> map, List<Component> added)
        {
            var go = new GameObject(src.name, typeof(RectTransform).IsInstanceOfType(src.transform)
                ? new[] { typeof(RectTransform) } : Type.EmptyTypes);
            go.layer = src.layer;
            go.tag = src.tag;
            src.transform.CopyStateTo(go.transform);
            if (!src.activeSelf) go.SetActive(false);
            map[src] = go;
            map[src.transform] = go.transform;
            if (parent != null)
            {
                go.transform._parent = parent;
                parent._children.Add(go.transform);
            }
            foreach (Component c in src.Components)
            {
                if (c is Transform || c.Destroyed) continue;
                var copy = (Component)s_memberwiseClone.Invoke(c, null);
                copy._go = go;
                if (copy is MonoBehaviour mb)
                {
                    mb.Awoken = false;
                    mb.Started = false;
                    mb.EnabledCalled = false;
                }
                AddRaw(go, copy);
                map[c] = copy;
                added.Add(copy);
            }
            foreach (Transform ch in src.transform._children)
                CloneTree(ch.gameObject, go.transform, map, added);
            return go;
        }

        static readonly FieldInfo s_componentsField =
            typeof(GameObject).GetField("_components", BindingFlags.Instance | BindingFlags.NonPublic);

        static void AddRaw(GameObject go, Component c) => ((List<Component>)s_componentsField.GetValue(go)).Add(c);

        static void RemapFields(object target, Dictionary<Object, Object> map)
        {
            for (Type t = target.GetType(); t != null && t != typeof(Component) && t != typeof(object); t = t.BaseType)
            {
                foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!typeof(Object).IsAssignableFrom(f.FieldType)) continue;
                    if (f.GetValue(target) is Object v && map.TryGetValue(v, out Object repl))
                        f.SetValue(target, repl);
                }
            }
        }

        /// <summary>Test/araç: tüm sahneyi boşalt.</summary>
        public static void Reset()
        {
            foreach (GameObject g in s_objects.ToArray())
                if (!g.Destroyed && g.transform._parent == null) DestroyNow(g);
            s_objects.Clear();
            s_behaviours.Clear();
            s_pendingDestroy.Clear();
            s_timedDestroy.Clear();
            s_particles.Clear();
        }
    }
}
