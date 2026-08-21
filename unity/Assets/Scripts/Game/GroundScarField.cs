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
    /// </summary>
    public sealed class GroundScarField : MonoBehaviour
    {
        readonly List<GameObject> _scars = new();
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
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Scar_" + kind;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            worldPos.y = 0.02f;
            go.transform.position = worldPos;

            along.y = 0f;
            if (along.sqrMagnitude < 1e-4f)
                along = Vector3.forward;
            along.Normalize();

            switch (kind)
            {
                case ScarKind.Crack:
                    go.transform.rotation = Quaternion.LookRotation(Vector3.down, along);
                    go.transform.localScale = new Vector3(scaleM * 0.22f, scaleM * 2.4f, 1f);
                    go.GetComponent<Renderer>().sharedMaterial = _purpleMat;
                    break;
                case ScarKind.Needle:
                    go.transform.rotation = Quaternion.LookRotation(Vector3.down, along);
                    go.transform.localScale = new Vector3(scaleM * 0.12f, scaleM * 1.6f, 1f);
                    go.GetComponent<Renderer>().sharedMaterial = _cyanMat;
                    break;
                case ScarKind.Swarm:
                    go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    go.transform.localScale = new Vector3(scaleM * 1.1f, scaleM * 1.1f, 1f);
                    go.GetComponent<Renderer>().sharedMaterial = _purpleMat;
                    break;
                case ScarKind.Acid:
                    go.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                    go.transform.localScale = new Vector3(scaleM * 0.9f, scaleM * 0.7f, 1f);
                    go.GetComponent<Renderer>().sharedMaterial = _acidMat;
                    break;
            }

            _scars.Add(go);
        }

        void OnDestroy()
        {
            if (_cyanMat != null) Destroy(_cyanMat);
            if (_purpleMat != null) Destroy(_purpleMat);
            if (_acidMat != null) Destroy(_acidMat);
        }

        static Material MakeMat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            c.a = 0.85f;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            else
                mat.color = c;
            return mat;
        }
    }
}
