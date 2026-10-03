using Dovus.Game.DevTools;
using UnityEngine;

namespace Dovus.Game.Cameras
{
    /// <summary>
    /// Ambiyans kayaları salt görsel; oyun simülasyonu Unity fizik kullanmaz. Kamera SphereCast
    /// için runtime ucuz collider'lar (bounds'tan BoxCollider veya convex MeshCollider).
    /// </summary>
    public static class CameraAmbienceColliders
    {
        const string BlockerChildName = "CameraBlocker";
        static bool _loggedLayerChoice;

        public static void EnsureOnCombatAmbience(Transform combatAmbienceRoot)
        {
            if (combatAmbienceRoot == null)
                return;
            Transform rocks = combatAmbienceRoot.Find("Rocks_Boundary");
            if (rocks == null)
                return;

            int layer = LayerMask.NameToLayer("CameraBlocker");
            if (layer < 0)
                layer = 0;
            if (!_loggedLayerChoice)
            {
                DebugConfig.DevLog(layer == 0
                    ? "[CameraAmbienceColliders] CameraBlocker layer yok; Default + kök filtresi"
                    : $"[CameraAmbienceColliders] blocker layer=CameraBlocker ({layer})");
                _loggedLayerChoice = true;
            }

            int added = 0;
            foreach (Transform rock in rocks)
            {
                if (rock == null)
                    continue;
                if (rock.Find(BlockerChildName) != null)
                    continue;
                if (!TryAddBlocker(rock, layer))
                    continue;
                added++;
            }

            if (added > 0)
                DebugConfig.DevLog($"[CameraAmbienceColliders] runtime colliders added={added} under Rocks_Boundary");
        }

        static bool TryAddBlocker(Transform rock, int layer)
        {
            var renderers = rock.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return false;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            if (b.size.sqrMagnitude < 0.01f)
                return false;

            var blockerGo = new GameObject(BlockerChildName);
            blockerGo.layer = layer;
            blockerGo.transform.SetParent(rock, false);
            blockerGo.transform.localPosition = rock.InverseTransformPoint(b.center);
            blockerGo.transform.localRotation = Quaternion.identity;

            var box = blockerGo.AddComponent<BoxCollider>();
            Vector3 localSize = rock.InverseTransformVector(b.size);
            box.size = new Vector3(
                Mathf.Abs(localSize.x),
                Mathf.Abs(localSize.y),
                Mathf.Abs(localSize.z));
            box.center = Vector3.zero;
            box.isTrigger = false;
            return true;
        }
    }
}
