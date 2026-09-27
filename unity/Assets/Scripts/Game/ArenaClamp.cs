using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Daire arena sınır clamp'i. Eski kare clamp köşelerde duvarın dışına izin veriyordu;
    /// 100 m çaplı salon için yatay düzlemde yarıçap kullanılır.
    /// </summary>
    public static class ArenaClamp
    {
        public static Vector3 XZ(Vector3 pos, float arenaRadiusM, float bodyRadiusM)
        {
            float limit = Mathf.Max(0f, arenaRadiusM - bodyRadiusM);
            float x = pos.x;
            float z = pos.z;
            float sqr = x * x + z * z;
            float limSqr = limit * limit;
            if (sqr > limSqr && sqr > 0.0001f)
            {
                float inv = limit / Mathf.Sqrt(sqr);
                pos.x = x * inv;
                pos.z = z * inv;
            }

            return pos;
        }
    }
}
