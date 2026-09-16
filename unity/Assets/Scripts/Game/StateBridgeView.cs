using Dovus.Core.Combat;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>İşaret diskleri + portal hattı (oyuncu camgöbeği/mor — tehdit kırmızısı değil).</summary>
    public sealed class StateBridgeView : MonoBehaviour
    {
        StateBridgeBoard _board;
        Transform _root;
        readonly System.Collections.Generic.List<Transform> _markXforms = new();
        readonly System.Collections.Generic.List<LineRenderer> _bridgeLines = new();
        Material _markMat;
        Material _bridgeMat;

        public void Bind(StateBridgeBoard board)
        {
            _board = board;
            if (_root == null)
            {
                var go = new GameObject("StateBridgeVisuals");
                go.transform.SetParent(transform, false);
                _root = go.transform;
            }

            _markMat ??= MakeMat(new Color(0.35f, 0.95f, 1f, 0.85f));
            _bridgeMat ??= MakeMat(new Color(0.55f, 0.35f, 1f, 0.9f));
        }

        public void Sync()
        {
            if (_board == null || _root == null)
                return;

            EnsureCount(_markXforms, _board.Marks.Count, CreateMark);
            for (int i = 0; i < _markXforms.Count; i++)
            {
                bool on = i < _board.Marks.Count;
                _markXforms[i].gameObject.SetActive(on);
                if (!on) continue;
                var m = _board.Marks[i];
                _markXforms[i].position = new Vector3(m.X, 0.05f, m.Z);
            }

            EnsureCount(_bridgeLines, _board.Bridges.Count, CreateBridge);
            for (int i = 0; i < _bridgeLines.Count; i++)
            {
                bool on = i < _board.Bridges.Count;
                _bridgeLines[i].gameObject.SetActive(on);
                if (!on) continue;
                var b = _board.Bridges[i];
                _bridgeLines[i].positionCount = 2;
                _bridgeLines[i].SetPosition(0, new Vector3(b.AX, 0.2f, b.AZ));
                _bridgeLines[i].SetPosition(1, new Vector3(b.BX, 0.2f, b.BZ));
            }
        }

        Transform CreateMark()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Mark";
            go.transform.SetParent(_root, false);
            go.transform.localScale = new Vector3(1.1f, 0.04f, 1.1f);
            Object.Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = _markMat;
            return go.transform;
        }

        LineRenderer CreateBridge()
        {
            var go = new GameObject("Bridge");
            go.transform.SetParent(_root, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = _bridgeMat;
            lr.widthMultiplier = 0.35f;
            lr.numCapVertices = 4;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return lr;
        }

        static void EnsureCount<T>(System.Collections.Generic.List<T> list, int need, System.Func<T> create)
        {
            while (list.Count < need)
                list.Add(create());
        }

        static Material MakeMat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }
    }
}
