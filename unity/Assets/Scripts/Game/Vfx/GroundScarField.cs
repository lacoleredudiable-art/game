using Dovus.Game.Casting;
using Dovus.Game.Config;
using System.Collections.Generic;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    public enum ScarKind
    {
        Crack,
        Needle,
        Swarm,
        Acid,
        Strike
    }

    /// <summary>
    /// T4: kalıcı dünya izi. Sayı yazılmaz — yerde çatlak / birikinti kalır.
    /// İz süreye bağlı silinmez (§8/T4); tavan dolunca en eski iz DÖNÜŞTÜRÜLÜR
    /// (T11 kare bütçesi — bkz. GameTuning.GroundScarCapCount).
    /// </summary>
    public sealed class GroundScarField : MonoBehaviour
    {
        readonly List<GameObject> _scars = new();
        int _writeIndex;
        GameTuning _tuning;
        Material _cyanMat;
        Material _purpleMat;
        Material _acidMat;

        public void Configure(GameTuning tuning)
        {
            _tuning = tuning;
            _cyanMat = MakeMat(tuning.Visuals.InkCyan * GroundScarFieldDefaults.cyanMat);
            _purpleMat = MakeMat(tuning.Visuals.InkPurple * GroundScarFieldDefaults.purpleMat);
            _acidMat = MakeMat(tuning.Visuals.AcidGreen * GroundScarFieldDefaults.acidMat);
        }

        public void Stamp(Vector3 worldPos, float scaleM, ScarKind kind, Vector3 along)
        {
            int cap = Mathf.Max(1, _tuning != null ? _tuning.Visuals.GroundScarCapCount : 60);

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

            worldPos.y = GroundScarFieldDefaults.y;
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
                    go.transform.localScale = new Vector3(scaleM * GroundScarFieldDefaults.localScale, scaleM * GroundScarFieldDefaults.SlashScarYMult, 1f);
                    renderer.sharedMaterial = _purpleMat;
                    break;
                case ScarKind.Needle:
                    go.transform.rotation = Quaternion.LookRotation(Vector3.down, along);
                    go.transform.localScale = new Vector3(scaleM * GroundScarFieldDefaults.BurnScarXMult, scaleM * GroundScarFieldDefaults.BurnScarYMult, 1f);
                    renderer.sharedMaterial = _cyanMat;
                    break;
                case ScarKind.Swarm:
                    go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    go.transform.localScale = new Vector3(scaleM * GroundScarFieldDefaults.BloomScarMult, scaleM * GroundScarFieldDefaults.BloomScarMult, 1f);
                    renderer.sharedMaterial = _purpleMat;
                    break;
                case ScarKind.Acid:
                    go.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                    go.transform.localScale = new Vector3(scaleM * GroundScarFieldDefaults.ShrinkScarXMult, scaleM * GroundScarFieldDefaults.ShrinkScarYMult, 1f);
                    renderer.sharedMaterial = _acidMat;
                    break;
                case ScarKind.Strike:
                    // Düz vuruş: kısa dar çizik — cümle halka/çatlak izinden ayrılır.
                    go.transform.rotation = Quaternion.LookRotation(Vector3.down, along);
                    go.transform.localScale = new Vector3(scaleM * GroundScarFieldDefaults.NeedleScarXMult, scaleM * GroundScarFieldDefaults.NeedleScarYMult, 1f);
                    renderer.sharedMaterial = _cyanMat;
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
            c.a = GroundScarFieldDefaults.a;
            SetMatColor(mat, c);
            return mat;
        }

        // "Sprites/Default" gerçekten alfa harmanlar (InkTrail.EnsureMaterial ile aynı desen);
        // URP Unlit varsayılan OPAK olduğu için izin 0.85 alfası hiçbir şey yapmıyordu.
        static Shader FindTransparentUnlitShader()
        {
            var shader = AssetLoader.FindShader("Sprites/Default", null);
            if (shader == null) shader = AssetLoader.FindShader("Universal Render Pipeline/Unlit", null);
            if (shader == null) shader = AssetLoader.FindShader("Unlit/Color", null);
            return shader != null ? shader : AssetLoader.FindShader("Hidden/Internal-Colored", null);
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
