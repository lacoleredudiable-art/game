using UnityEngine;

namespace Dovus.Game.Casting
{
    /// <summary>
    /// Mürekkep LineRenderer'ları için ekran-piksel ortografik kamera.
    /// Ana kameradan bağımsız — takip/sarsıntı izi kaydırmaz (§2 ekrana sabit).
    /// </summary>
    public sealed class HexagonOverlayCameraView : MonoBehaviour
    {
        Camera _cam;

        public Camera Cam => _cam;

        public void Build(int layer)
        {
            _cam = gameObject.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.Nothing;
            _cam.depth = HexagonOverlayCameraViewDefaults.RenderDepth;
            _cam.cullingMask = 1 << layer;
            _cam.nearClipPlane = HexagonOverlayCameraViewDefaults.NearClipM;
            _cam.farClipPlane = HexagonOverlayCameraViewDefaults.FarClipM;
            _cam.allowHDR = false;
            _cam.allowMSAA = false;
            SyncToScreen();
        }

        void LateUpdate() => SyncToScreen();

        void SyncToScreen()
        {
            if (_cam == null)
                return;

            // 1 world unit = 1 pixel; merkez ekranın ortası.
            _cam.orthographicSize = Screen.height * 0.5f;
            transform.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, HexagonOverlayCameraViewDefaults.CameraBackOffsetZ);
            transform.rotation = Quaternion.identity;
        }

        public Vector3 ScreenToWorld(Vector2 screen)
        {
            return new Vector3(screen.x, screen.y, 0f);
        }
    }
}
