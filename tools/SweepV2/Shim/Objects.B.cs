using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEngine
{
    public struct Scene
    {
        internal int Handle;
        public string name => "Prototype";
        public string path => "Assets/Scenes/Prototype.unity";
        public bool isLoaded => true;
        public bool IsValid() => true;
        public int rootCount => World.RootCount();
        public GameObject[] GetRootGameObjects() => World.Roots().ToArray();
    }

    public sealed class GameObject : Object
    {
        Transform _transform;
        readonly List<Component> _components = new();
        bool _activeSelf = true;
        string _tag = "Untagged";
        internal bool DontDestroy;
        internal bool IsAsset;
        internal bool ActiveCached = true;

        public GameObject() : this("New Game Object") { }

        public GameObject(string name) : this(name, Array.Empty<Type>()) { }

        public GameObject(string name, params Type[] components)
        {
            this.name = name;
            bool rect = components != null && components.Any(t => typeof(RectTransform).IsAssignableFrom(t));
            _transform = rect ? new RectTransform() : new Transform();
            _transform._go = this;
            _components.Add(_transform);
            World.Register(this);
            if (components != null)
            {
                foreach (Type t in components)
                {
                    if (typeof(Transform).IsAssignableFrom(t)) continue;
                    AddComponent(t);
                }
            }
        }

        internal GameObject(string name, bool asset)
        {
            this.name = name;
            IsAsset = asset;
            _transform = new Transform { _go = this };
            _components.Add(_transform);
            World.Register(this);
        }

        public Transform transform => _transform;
        public GameObject gameObject => this;
        public int layer { get; set; }
        public bool isStatic { get; set; }
        public Scene scene => new();

        public string tag
        {
            get => _tag;
            set => _tag = string.IsNullOrEmpty(value) ? "Untagged" : value;
        }

        public bool CompareTag(string tag) => _tag == tag;

        public bool activeSelf => _activeSelf;

        public bool activeInHierarchy
        {
            get
            {
                if (Destroyed) return false;
                for (Transform t = _transform; t != null; t = t._parent)
                    if (!t._go._activeSelf) return false;
                return true;
            }
        }

        public void SetActive(bool value)
        {
            if (_activeSelf == value) return;
            _activeSelf = value;
            World.OnActiveChanged(this);
        }

        internal void OnHierarchyChanged() => World.OnActiveChanged(this);

        internal IReadOnlyList<Component> Components => _components;

        internal void RemoveComponent(Component c) => _components.Remove(c);

        public T AddComponent<T>() where T : Component => (T)AddComponent(typeof(T));

        public Component AddComponent(Type type)
        {
            if (typeof(RectTransform).IsAssignableFrom(type))
            {
                if (_transform is RectTransform existing) return existing;
                SwapToRectTransform();
                return _transform;
            }
            if (typeof(Transform).IsAssignableFrom(type)) return _transform;

            foreach (RequireComponent req in type.GetCustomAttributes(typeof(RequireComponent), true))
            {
                foreach (Type r in new[] { req.m_Type0, req.m_Type1, req.m_Type2 })
                {
                    if (r != null && GetComponent(r) == null) AddComponent(r);
                }
            }

            if (NeedsRect(type) && !(_transform is RectTransform)) SwapToRectTransform();

            var c = (Component)Activator.CreateInstance(type, true);
            c._go = this;
            _components.Add(c);
            World.OnComponentAdded(c);
            return c;
        }

        static bool NeedsRect(Type t)
        {
            for (Type k = t; k != null; k = k.BaseType)
            {
                if (k.FullName == "UnityEngine.UI.Graphic" || k.FullName == "UnityEngine.Canvas"
                    || k.FullName == "UnityEngine.UI.Selectable" || k.FullName == "UnityEngine.UI.CanvasScaler")
                    return true;
            }
            return false;
        }

        void SwapToRectTransform()
        {
            Transform old = _transform;
            var rt = new RectTransform { _go = this };
            old.CopyStateTo(rt);
            rt._parent = old._parent;
            if (old._parent != null)
            {
                int i = old._parent._children.IndexOf(old);
                old._parent._children[i] = rt;
            }
            foreach (Transform c in old._children)
            {
                c._parent = rt;
                rt._children.Add(c);
            }
            old._children.Clear();
            _components[_components.IndexOf(old)] = rt;
            _transform = rt;
            old.Destroyed = true;
        }

        public T GetComponent<T>()
        {
            foreach (Component c in _components)
                if (c is T t && !c.Destroyed) return t;
            return default;
        }

        public Component GetComponent(Type type)
        {
            foreach (Component c in _components)
                if (type.IsInstanceOfType(c) && !c.Destroyed) return c;
            return null;
        }

        public bool TryGetComponent<T>(out T component)
        {
            component = GetComponent<T>();
            return component != null && !(component is Object o && o.Destroyed);
        }

        public T[] GetComponents<T>()
        {
            var list = new List<T>();
            GetComponents(list);
            return list.ToArray();
        }

        public void GetComponents<T>(List<T> results)
        {
            results.Clear();
            foreach (Component c in _components)
                if (c is T t && !c.Destroyed) results.Add(t);
        }

        public T GetComponentInChildren<T>() => GetComponentInChildren<T>(false);

        public T GetComponentInChildren<T>(bool includeInactive)
        {
            if (!includeInactive && !activeInHierarchy) return default;
            return FindInChildren<T>(_transform, includeInactive);
        }

        static T FindInChildren<T>(Transform t, bool includeInactive)
        {
            GameObject go = t._go;
            if (!includeInactive && !go._activeSelf) return default;
            T self = go.GetComponent<T>();
            if (self != null) return self;
            foreach (Transform c in t._children)
            {
                T r = FindInChildren<T>(c, includeInactive);
                if (r != null) return r;
            }
            return default;
        }

        public T[] GetComponentsInChildren<T>() => GetComponentsInChildren<T>(false);

        public T[] GetComponentsInChildren<T>(bool includeInactive)
        {
            var list = new List<T>();
            GetComponentsInChildren(includeInactive, list);
            return list.ToArray();
        }

        public void GetComponentsInChildren<T>(bool includeInactive, List<T> result)
        {
            result.Clear();
            if (!includeInactive && !activeInHierarchy) return;
            Collect(_transform, includeInactive, result);
        }

        static void Collect<T>(Transform t, bool includeInactive, List<T> result)
        {
            GameObject go = t._go;
            if (!includeInactive && !go._activeSelf) return;
            foreach (Component c in go._components)
                if (c is T x && !c.Destroyed) result.Add(x);
            foreach (Transform ch in t._children) Collect(ch, includeInactive, result);
        }

        public T GetComponentInParent<T>() => GetComponentInParent<T>(false);

        public T GetComponentInParent<T>(bool includeInactive)
        {
            for (Transform t = _transform; t != null; t = t._parent)
            {
                if (!includeInactive && !t._go.activeInHierarchy) continue;
                T c = t._go.GetComponent<T>();
                if (c != null) return c;
            }
            return default;
        }

        public T[] GetComponentsInParent<T>() => GetComponentsInParent<T>(false);

        public T[] GetComponentsInParent<T>(bool includeInactive)
        {
            var list = new List<T>();
            for (Transform t = _transform; t != null; t = t._parent)
            {
                if (!includeInactive && !t._go.activeInHierarchy) continue;
                foreach (Component c in t._go._components)
                    if (c is T x && !c.Destroyed) list.Add(x);
            }
            return list.ToArray();
        }

        public void SendMessage(string methodName, object value = null, SendMessageOptions options = SendMessageOptions.RequireReceiver)
        {
            foreach (Component c in _components.ToArray())
            {
                if (c is MonoBehaviour mb) World.InvokeMessage(mb, methodName, value);
            }
        }

        public static GameObject Find(string name) =>
            World.AllGameObjects().FirstOrDefault(g => g.activeInHierarchy && (g.name == name || PathOf(g) == name.TrimStart('/')));

        static string PathOf(GameObject g)
        {
            var parts = new List<string>();
            for (Transform t = g.transform; t != null; t = t._parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        public static GameObject FindWithTag(string tag) =>
            World.AllGameObjects().FirstOrDefault(g => g.activeInHierarchy && g._tag == tag);

        public static GameObject FindGameObjectWithTag(string tag) => FindWithTag(tag);

        public static GameObject[] FindGameObjectsWithTag(string tag) =>
            World.AllGameObjects().Where(g => g.activeInHierarchy && g._tag == tag).ToArray();

        public static GameObject CreatePrimitive(PrimitiveType type)
        {
            var go = new GameObject(type.ToString());
            go.AddComponent<MeshFilter>().sharedMesh = Mesh.Primitive(type);
            go.AddComponent<MeshRenderer>();
            switch (type)
            {
                case PrimitiveType.Sphere:
                    go.AddComponent<SphereCollider>();
                    break;
                case PrimitiveType.Capsule:
                case PrimitiveType.Cylinder:
                    go.AddComponent<CapsuleCollider>();
                    break;
                case PrimitiveType.Cube:
                    go.AddComponent<BoxCollider>();
                    break;
                default:
                    go.AddComponent<MeshCollider>();
                    break;
            }
            return go;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RequireComponent : Attribute
    {
        public Type m_Type0, m_Type1, m_Type2;
        public RequireComponent(Type requiredComponent) { m_Type0 = requiredComponent; }
        public RequireComponent(Type requiredComponent, Type requiredComponent2) { m_Type0 = requiredComponent; m_Type1 = requiredComponent2; }
        public RequireComponent(Type a, Type b, Type c) { m_Type0 = a; m_Type1 = b; m_Type2 = c; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DefaultExecutionOrder : Attribute
    {
        public int order { get; }
        public DefaultExecutionOrder(int order) { this.order = order; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DisallowMultipleComponent : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ExecuteAlways : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ExecuteInEditMode : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AddComponentMenu : Attribute
    {
        public AddComponentMenu(string menuName) { }
        public AddComponentMenu(string menuName, int order) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeReference : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HideInInspector : Attribute { }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class HeaderAttribute : Attribute
    {
        public readonly string header;
        public HeaderAttribute(string header) { this.header = header; }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class SpaceAttribute : Attribute
    {
        public SpaceAttribute() { }
        public SpaceAttribute(float height) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RangeAttribute : Attribute
    {
        public readonly float min, max;
        public RangeAttribute(float min, float max) { this.min = min; this.max = max; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinAttribute : Attribute
    {
        public readonly float min;
        public MinAttribute(float min) { this.min = min; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TextAreaAttribute : Attribute
    {
        public TextAreaAttribute() { }
        public TextAreaAttribute(int minLines, int maxLines) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MultilineAttribute : Attribute
    {
        public MultilineAttribute() { }
        public MultilineAttribute(int lines) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ContextMenuItemAttribute : Attribute
    {
        public ContextMenuItemAttribute(string name, string function) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ContextMenu : Attribute
    {
        public ContextMenu(string itemName) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string fileName { get; set; }
        public string menuName { get; set; }
        public int order { get; set; }
    }

    public enum RuntimeInitializeLoadType
    {
        AfterSceneLoad = 0, BeforeSceneLoad = 1, AfterAssembliesLoaded = 2, BeforeSplashScreen = 3, SubsystemRegistration = 4,
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeLoadType loadType { get; }
        public RuntimeInitializeOnLoadMethodAttribute() { loadType = RuntimeInitializeLoadType.AfterSceneLoad; }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { this.loadType = loadType; }
    }
}

namespace UnityEngine.Scripting
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class PreserveAttribute : Attribute { }
}

namespace UnityEngine.Serialization
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class FormerlySerializedAsAttribute : Attribute
    {
        public FormerlySerializedAsAttribute(string oldName) { }
    }
}
