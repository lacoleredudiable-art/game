using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEngine
{
    public enum HideFlags
    {
        None = 0, HideInHierarchy = 1, HideInInspector = 2, DontSaveInEditor = 4, NotEditable = 8,
        DontSaveInBuild = 16, DontUnloadUnusedAsset = 32, DontSave = 52, HideAndDontSave = 61,
    }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum Space { World, Self }
    public enum FindObjectsInactive { Exclude, Include }
    public enum FindObjectsSortMode { None, InstanceID }
    public enum SendMessageOptions { RequireReceiver, DontRequireReceiver }

    public class Object
    {
        static int s_nextId = 1;
        readonly int _id;
        string _name = "";
        internal bool Destroyed;

        public Object()
        {
            _id = s_nextId++;
        }

        public virtual string name
        {
            get => _name;
            set => _name = value ?? "";
        }

        public HideFlags hideFlags { get; set; }

        public int GetInstanceID() => _id;
        public int GetEntityId() => _id;

        public override string ToString() => $"{name} ({GetType().Name})";

        internal bool IsAlive => !Destroyed;

        public static bool operator ==(Object a, Object b)
        {
            bool aNull = a is null || a.Destroyed;
            bool bNull = b is null || b.Destroyed;
            if (aNull || bNull) return aNull && bNull;
            return ReferenceEquals(a, b);
        }

        public static bool operator !=(Object a, Object b) => !(a == b);
        public static implicit operator bool(Object o) => !(o is null) && !o.Destroyed;
        public override bool Equals(object other) => other is Object o ? this == o : other == null && Destroyed;
        public override int GetHashCode() => _id;

        public static void Destroy(Object obj) => Destroy(obj, 0f);

        public static void Destroy(Object obj, float t)
        {
            if (obj is null || obj.Destroyed) return;
            World.ScheduleDestroy(obj, t);
        }

        public static void DestroyImmediate(Object obj) => World.DestroyNow(obj);
        public static void DestroyImmediate(Object obj, bool allowDestroyingAssets) => World.DestroyNow(obj);

        public static void DontDestroyOnLoad(Object target)
        {
            GameObject go = target as GameObject ?? (target as Component)?.gameObject;
            if (go != null) go.transform.root.gameObject.DontDestroy = true;
        }

        public static T Instantiate<T>(T original) where T : Object => (T)World.Clone(original, null, false, null, null);
        public static T Instantiate<T>(T original, Transform parent) where T : Object => (T)World.Clone(original, parent, false, null, null);
        public static T Instantiate<T>(T original, Transform parent, bool worldPositionStays) where T : Object =>
            (T)World.Clone(original, parent, worldPositionStays, null, null);
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object =>
            (T)World.Clone(original, null, false, position, rotation);
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object =>
            (T)World.Clone(original, parent, false, position, rotation);

        public static T FindAnyObjectByType<T>() where T : Object => World.Find<T>(false).FirstOrDefault();
        public static T FindAnyObjectByType<T>(FindObjectsInactive inactive) where T : Object =>
            World.Find<T>(inactive == FindObjectsInactive.Include).FirstOrDefault();
        public static T FindFirstObjectByType<T>() where T : Object => World.Find<T>(false).FirstOrDefault();
        public static T FindFirstObjectByType<T>(FindObjectsInactive inactive) where T : Object =>
            World.Find<T>(inactive == FindObjectsInactive.Include).FirstOrDefault();
        public static T FindObjectOfType<T>() where T : Object => World.Find<T>(false).FirstOrDefault();
        public static T FindObjectOfType<T>(bool includeInactive) where T : Object => World.Find<T>(includeInactive).FirstOrDefault();
        public static T[] FindObjectsOfType<T>() where T : Object => World.Find<T>(false).ToArray();
        public static T[] FindObjectsOfType<T>(bool includeInactive) where T : Object => World.Find<T>(includeInactive).ToArray();
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) where T : Object => World.Find<T>(false).ToArray();
        public static T[] FindObjectsByType<T>(FindObjectsInactive inactive, FindObjectsSortMode sortMode) where T : Object =>
            World.Find<T>(inactive == FindObjectsInactive.Include).ToArray();
    }

    public class Component : Object
    {
        internal GameObject _go;

        public GameObject gameObject => _go;
        public virtual Transform transform => _go?.transform;
        public string tag { get => _go.tag; set => _go.tag = value; }

        public override string name
        {
            get => _go != null ? _go.name : base.name;
            set { if (_go != null) _go.name = value; else base.name = value; }
        }

        public bool CompareTag(string tag) => _go != null && _go.CompareTag(tag);

        public T GetComponent<T>() => _go.GetComponent<T>();
        public Component GetComponent(Type type) => _go.GetComponent(type);
        public bool TryGetComponent<T>(out T component) => _go.TryGetComponent(out component);
        public T[] GetComponents<T>() => _go.GetComponents<T>();
        public void GetComponents<T>(List<T> results) => _go.GetComponents(results);
        public T GetComponentInChildren<T>() => _go.GetComponentInChildren<T>();
        public T GetComponentInChildren<T>(bool includeInactive) => _go.GetComponentInChildren<T>(includeInactive);
        public T[] GetComponentsInChildren<T>() => _go.GetComponentsInChildren<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) => _go.GetComponentsInChildren<T>(includeInactive);
        public void GetComponentsInChildren<T>(bool includeInactive, List<T> result) => _go.GetComponentsInChildren(includeInactive, result);
        public void GetComponentsInChildren<T>(List<T> result) => _go.GetComponentsInChildren(false, result);
        public T GetComponentInParent<T>() => _go.GetComponentInParent<T>();
        public T GetComponentInParent<T>(bool includeInactive) => _go.GetComponentInParent<T>(includeInactive);
        public T[] GetComponentsInParent<T>() => _go.GetComponentsInParent<T>();
        public T[] GetComponentsInParent<T>(bool includeInactive) => _go.GetComponentsInParent<T>(includeInactive);

        public void SendMessage(string methodName, object value = null, SendMessageOptions options = SendMessageOptions.RequireReceiver) =>
            _go.SendMessage(methodName, value, options);
    }

    public class Behaviour : Component
    {
        bool _enabled = true;

        public bool enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                World.OnEnabledChanged(this);
            }
        }

        public bool isActiveAndEnabled => _enabled && _go != null && _go.activeInHierarchy && !Destroyed;
    }

    /// <summary>Headless stub — Unity Coroutine tutamacı.</summary>
    public sealed class Coroutine
    {
        internal IEnumerator Routine;
    }

    public class MonoBehaviour : Behaviour
    {
        internal bool Awoken;
        internal bool Started;
        internal bool EnabledCalled;
        internal int Order;
        internal long Seq;
        readonly List<Coroutine> _coroutines = new();

        public bool useGUILayout { get; set; } = true;

        public static void print(object message) => Debug.Log(message);

        public void CancelInvoke() { }
        public bool IsInvoking() => false;

        public Coroutine StartCoroutine(IEnumerator routine)
        {
            var c = new Coroutine { Routine = routine };
            _coroutines.Add(c);
            // Headless: ilk MoveNext (setup); WaitForSeconds vb. sonraki tick yok.
            try { routine?.MoveNext(); }
            catch { /* stub */ }
            return c;
        }

        public void StopCoroutine(Coroutine routine)
        {
            if (routine != null)
                _coroutines.Remove(routine);
        }

        public void StopAllCoroutines() => _coroutines.Clear();
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject => (T)CreateInstance(typeof(T));

        public static ScriptableObject CreateInstance(Type type)
        {
            var so = (ScriptableObject)Activator.CreateInstance(type, true);
            World.InvokeMessage(so, "Awake");
            World.InvokeMessage(so, "OnEnable");
            World.RegisterAsset(so);
            return so;
        }
    }

    public class Transform : Component, IEnumerable
    {
        internal Transform _parent;
        internal readonly List<Transform> _children = new();
        Vector3 _localPosition;
        Quaternion _localRotation = Quaternion.identity;
        Vector3 _localScale = Vector3.one;

        public override Transform transform => this;
        public bool hasChanged { get; set; }

        public Transform parent
        {
            get => _parent;
            set => SetParent(value, true);
        }

        public Transform root
        {
            get
            {
                Transform t = this;
                while (t._parent != null) t = t._parent;
                return t;
            }
        }

        public int childCount => _children.Count;

        public Vector3 localPosition { get => _localPosition; set { _localPosition = value; hasChanged = true; } }
        public Quaternion localRotation { get => _localRotation; set { _localRotation = Quaternion.Normalize(value); hasChanged = true; } }
        public Vector3 localScale { get => _localScale; set { _localScale = value; hasChanged = true; } }

        public Vector3 localEulerAngles
        {
            get => _localRotation.eulerAngles;
            set => localRotation = Quaternion.Euler(value);
        }

        public Matrix4x4 localToWorldMatrix
        {
            get
            {
                Matrix4x4 local = Matrix4x4.TRS(_localPosition, _localRotation, _localScale);
                return _parent == null ? local : _parent.localToWorldMatrix * local;
            }
        }

        public Matrix4x4 worldToLocalMatrix => localToWorldMatrix.inverse;

        public Vector3 position
        {
            get
            {
                if (_parent == null) return _localPosition;
                return _parent.localToWorldMatrix.MultiplyPoint3x4(_localPosition);
            }
            set
            {
                localPosition = _parent == null ? value : _parent.localToWorldMatrix.inverse.MultiplyPoint3x4(value);
            }
        }

        public Quaternion rotation
        {
            get => _parent == null ? _localRotation : _parent.rotation * _localRotation;
            set => localRotation = _parent == null ? value : Quaternion.Inverse(_parent.rotation) * value;
        }

        public Vector3 eulerAngles
        {
            get => rotation.eulerAngles;
            set => rotation = Quaternion.Euler(value);
        }

        public Vector3 lossyScale
        {
            get
            {
                if (_parent == null) return _localScale;
                Vector3 p = _parent.lossyScale;
                return new Vector3(p.x * _localScale.x, p.y * _localScale.y, p.z * _localScale.z);
            }
        }

        public Vector3 forward
        {
            get => rotation * Vector3.forward;
            set => rotation = Quaternion.LookRotation(value);
        }

        public Vector3 right
        {
            get => rotation * Vector3.right;
            set => rotation = Quaternion.FromToRotation(Vector3.right, value);
        }

        public Vector3 up
        {
            get => rotation * Vector3.up;
            set => rotation = Quaternion.FromToRotation(Vector3.up, value);
        }

        public void SetParent(Transform p) => SetParent(p, true);

        public void SetParent(Transform p, bool worldPositionStays)
        {
            if (p == this) return;
            Vector3 wp = position;
            Quaternion wr = rotation;
            Vector3 ws = lossyScale;
            _parent?._children.Remove(this);
            _parent = p;
            p?._children.Add(this);
            if (worldPositionStays)
            {
                position = wp;
                rotation = wr;
                if (p == null) localScale = ws;
                else
                {
                    Vector3 ps = p.lossyScale;
                    localScale = new Vector3(Div(ws.x, ps.x), Div(ws.y, ps.y), Div(ws.z, ps.z));
                }
            }
            _go?.OnHierarchyChanged();
        }

        static float Div(float a, float b) => MathF.Abs(b) < 1e-8f ? a : a / b;

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        public void SetLocalPositionAndRotation(Vector3 localPosition, Quaternion localRotation)
        {
            this.localPosition = localPosition;
            this.localRotation = localRotation;
        }

        public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation)
        {
            position = this.position;
            rotation = this.rotation;
        }

        public Vector3 TransformPoint(Vector3 p) => localToWorldMatrix.MultiplyPoint3x4(p);
        public Vector3 TransformPoint(float x, float y, float z) => TransformPoint(new Vector3(x, y, z));
        public Vector3 InverseTransformPoint(Vector3 p) => localToWorldMatrix.inverse.MultiplyPoint3x4(p);
        public Vector3 TransformDirection(Vector3 d) => rotation * d;
        public Vector3 InverseTransformDirection(Vector3 d) => Quaternion.Inverse(rotation) * d;
        public Vector3 TransformVector(Vector3 v) => localToWorldMatrix.MultiplyVector(v);
        public Vector3 InverseTransformVector(Vector3 v) => localToWorldMatrix.inverse.MultiplyVector(v);

        public void Translate(Vector3 translation) => Translate(translation, Space.Self);

        public void Translate(Vector3 translation, Space relativeTo)
        {
            if (relativeTo == Space.World) position += translation;
            else position += TransformDirection(translation);
        }

        public void Translate(float x, float y, float z) => Translate(new Vector3(x, y, z));

        public void Rotate(Vector3 eulers) => Rotate(eulers, Space.Self);

        public void Rotate(Vector3 eulers, Space relativeTo)
        {
            Quaternion q = Quaternion.Euler(eulers.x, eulers.y, eulers.z);
            if (relativeTo == Space.Self) localRotation = localRotation * q;
            else rotation = rotation * (Quaternion.Inverse(rotation) * q * rotation);
        }

        public void Rotate(float x, float y, float z) => Rotate(new Vector3(x, y, z), Space.Self);
        public void Rotate(float x, float y, float z, Space relativeTo) => Rotate(new Vector3(x, y, z), relativeTo);

        public void Rotate(Vector3 axis, float angle) => Rotate(axis, angle, Space.Self);

        public void Rotate(Vector3 axis, float angle, Space relativeTo)
        {
            if (relativeTo == Space.Self) localRotation = localRotation * Quaternion.AngleAxis(angle, axis);
            else rotation = Quaternion.AngleAxis(angle, axis) * rotation;
        }

        public void RotateAround(Vector3 point, Vector3 axis, float angle)
        {
            Quaternion q = Quaternion.AngleAxis(angle, axis);
            position = point + q * (position - point);
            rotation = q * rotation;
        }

        public void LookAt(Transform target) => LookAt(target.position, Vector3.up);
        public void LookAt(Vector3 worldPosition) => LookAt(worldPosition, Vector3.up);

        public void LookAt(Vector3 worldPosition, Vector3 worldUp)
        {
            Vector3 d = worldPosition - position;
            if (d.sqrMagnitude > 1e-12f) rotation = Quaternion.LookRotation(d, worldUp);
        }

        public Transform Find(string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            string[] parts = n.Split('/');
            Transform cur = this;
            foreach (string part in parts)
            {
                Transform next = null;
                foreach (Transform c in cur._children)
                {
                    if (c.name == part) { next = c; break; }
                }
                if (next == null) return null;
                cur = next;
            }
            return cur;
        }

        public Transform GetChild(int index) => _children[index];

        public bool IsChildOf(Transform parent)
        {
            if (parent == null) return false;
            for (Transform t = this; t != null; t = t._parent)
                if (ReferenceEquals(t, parent)) return true;
            return false;
        }

        public int GetSiblingIndex() => _parent != null ? _parent._children.IndexOf(this) : World.RootIndex(this);

        public void SetSiblingIndex(int index)
        {
            if (_parent == null) return;
            _parent._children.Remove(this);
            index = Math.Clamp(index, 0, _parent._children.Count);
            _parent._children.Insert(index, this);
        }

        public void SetAsFirstSibling() => SetSiblingIndex(0);
        public void SetAsLastSibling() => SetSiblingIndex(int.MaxValue);

        public void DetachChildren()
        {
            foreach (Transform c in _children.ToArray()) c.SetParent(null, true);
        }

        public IEnumerator GetEnumerator() => _children.ToList().GetEnumerator();

        internal void CopyStateTo(Transform other)
        {
            other._localPosition = _localPosition;
            other._localRotation = _localRotation;
            other._localScale = _localScale;
        }
    }

}
