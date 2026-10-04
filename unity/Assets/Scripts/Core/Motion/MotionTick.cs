using System;
using System.Collections.Generic;
using Dovus.Core;

namespace Dovus.Core.Motion
{
    public readonly struct MotionTick
    {
        public MotionTick(
            float x, float y, float z,
            float faceX, float faceZ,
            bool finished,
            MotionHit[] hits,
            string anim = "",
            float animSpeed = 1f,
            float velX = 0f,
            float velZ = 0f,
            bool spin = false,
            bool airborne = false)
        {
            X = x;
            Y = y;
            Z = z;
            FaceX = faceX;
            FaceZ = faceZ;
            Finished = finished;
            Hits = hits ?? Array.Empty<MotionHit>();
            AnimKey = anim ?? string.Empty;
            AnimSpeed = animSpeed > MotionTemplateRunnerDefaults.MinRadiusM ? animSpeed : 1f;
            VelX = velX;
            VelZ = velZ;
            Spin = spin;
            Airborne = airborne;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float FaceX { get; }
        public float FaceZ { get; }
        public bool Finished { get; }
        public MotionHit[] Hits { get; }
        public string AnimKey { get; }
        public float AnimSpeed { get; }
        public float VelX { get; }
        public float VelZ { get; }
        /// <summary>Dönüş klibi ve gövde yaw'ı birlikte sürer.</summary>
        public bool Spin { get; }
        /// <summary>Bu kare havadaki bir fazdadır. Bittiği karede false: iniş başlayabilir.</summary>
        public bool Airborne { get; }
    }
}
