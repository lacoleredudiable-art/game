using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game
{
    public enum ScarKind
    {
        Crack,
        Needle,
        Swarm,
        Acid
    }

    /// <summary>
    /// T4: kalıcı dünya izi. Sayı yazılmaz — yerde çatlak / birikinti kalır.
    /// İz süreye bağlı silinmez (§8/T4); tavan dolunca en eski iz DÖNÜŞTÜRÜLÜR
    /// (T11 kare bütçesi — bkz. PrototypeTuning.GroundScarCapCount).
    /// </summary>
    public sealed class GroundScarField : MonoBehaviour
    {
        readonly List<GameObject> _scars = new();
        int _writeIndex;
        PrototypeTuning _tuning;
        Material _cyanMat;
        Material _purpleMat;
        Material _acidMat;

        public void Configure(PrototypeTuning tuning)
        {
            _tuning = tuning;
            _cyanMat = MakeMat(tuning.InkCyan * 0.55f);
            _purpleMat = MakeMat(tuning.InkPurple * 0.55f);
            _acidMat = MakeMat(tuning.AcidGreen * 0.7f);
        }

        public void Stamp(Vector3 worldPos, float scaleM, ScarKind kind, Vector3 along)
        {
            int cap = Mathf.Max(1, _tuning != null ? _tuning.GroundScarCapCount : 60);

            GameObject go;
            if (_scars.Count < cap)
            {
                go = CreateScarObject("Scar_" + kind);
                _scars.Add(go);
            }
            else
            {
                // Tavan dolu: en eski izi (round-robin sırayla) yeniden kullan — yok edip
                // yeniden yaratma. Uzun dövüşte sahnedeki nesne sayısı burada sabitlenir.
                go = _scars[_writeIndex];
                go.name = "Scar_" + kind;
            }
            _writeIndex = (_writeIndex + 1) % cap;

            worldPos.y = 0.02f;
            go.transform.position = worldPos;
            if (!go.activeSelf)
                go.SetActive(true);

            along.y = 0f;
            if (along.sqrMagnitude < 1e-4f)
                along = Vector3.forward;
            along.Normalize();

            var renderer = go.GetComponent<Renderer>();
            switch (kind)
            {
                case ScarKind.Crack:
                    go.transform.rotation = Quaternion.LookRotation(Vector3.down, along);
                    go.transform.localScale = new Vector3(scaleM * 0.22f, scaleM * 2.4f, 1f);
                    renderer.sharedMaterial = _purpleMat;
                    break;
                case ScarKind.Needle:
                    go.transform.rotation = Quaternion.LookRotation(Vector3.down, along);
                    go.transform.localScale = new Vector3(scaleM * 0.12f, scaleM * 1.6f, 1f);
                    renderer.sharedMaterial = _cyanMat;
                    break;
                case ScarKind.Swarm:
                    go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    go.transform.localScale = new Vector3(scaleM * 1.1f, scaleM * 1.1f, 1f);
                    renderer.sharedMaterial = _purpleMat;
                    break;
                case ScarKind.Acid:
                    go.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                    go.transform.localScale = new Vector3(scaleM * 0.9f, scaleM * 0.7f, 1f);
                    renderer.sharedMaterial = _acidMat;
                    break;
            }
        }

        /// <summary>
        /// Mesh'i doğrudan ata — GameObject.CreatePrimitive'in otomatik eklediği Collider
        /// hiç oluşmaz (teknoloji-kararlari §4: fizik dışarıda, bir karelik collider bile yok).
        /// </summary>
        GameObject CreateScarObject(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Quad);
            go.AddComponent<MeshRenderer>();
            return go;
        }

        void OnDestroy()
        {
            if (_cyanMat != null) Destroy(_cyanMat);
            if (_purpleMat != null) Destroy(_purpleMat);
            if (_acidMat != null) Destroy(_acidMat);
        }

        static Material MakeMat(Color c)
        {
            var shader = FindTransparentUnlitShader();
            var mat = new Material(shader);
            ConfigureTransparentFallback(mat);
            c.a = 0.85f;
            SetMatColor(mat, c);
            return mat;
        }

        // "Sprites/Default" gerçekten alfa harmanlar (InkTrail.EnsureMaterial ile aynı desen);
        // URP Unlit varsayılan OPAK olduğu için izin 0.85 alfası hiçbir şey yapmıyordu.
        static Shader FindTransparentUnlitShader()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            return shader != null ? shader : Shader.Find("Hidden/Internal-Colored");
        }

        static void ConfigureTransparentFallback(Material mat)
        {
            if (!mat.HasProperty("_Surface"))
                return;

            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        static void SetMatColor(Material mat, Color c)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            else
                mat.color = c;
        }
    }
}
