using System;

namespace Dovus.Core.Combat
{
    /// <summary>Çubuk varsa o yön, yoksa bakışın tersi.</summary>
    public static class DodgeDirection
    {
        public static void Resolve(
            float stickX, float stickZ,
            float faceX, float faceZ,
            out float x, out float z)
        {
            float stickMag = stickX * stickX + stickZ * stickZ;
            if (stickMag > 0.0001f)
            {
                float inv = 1f / MathF.Sqrt(stickMag);
                x = stickX * inv;
                z = stickZ * inv;
                return;
            }

            float faceMag = faceX * faceX + faceZ * faceZ;
            if (faceMag > 0.0001f)
            {
                float inv = 1f / MathF.Sqrt(faceMag);
                x = -faceX * inv;
                z = -faceZ * inv;
                return;
            }

            x = 0f;
            z = -1f;
        }
    }
}
