using System;
using System.Collections.Generic;
using Dovus.Core;

namespace Dovus.Core.Motion
{
    public readonly struct MotionTarget
    {
        public MotionTarget(
            bool has,
            float x,
            float z,
            float radiusM = 0f,
            bool holdApproach = false,
            bool hasObstacle = false,
            float obstacleX = 0f,
            float obstacleZ = 0f,
            float obstacleRadiusM = 0f)
        {
            Has = has;
            X = x;
            Z = z;
            RadiusM = Math.Max(0f, radiusM);
            HoldApproach = holdApproach;
            HasObstacle = hasObstacle;
            ObstacleX = obstacleX;
            ObstacleZ = obstacleZ;
            ObstacleRadiusM = Math.Max(0f, obstacleRadiusM);
        }

        public bool Has { get; }
        public float X { get; }
        public float Z { get; }
        /// <summary>Hedef collider yarıçapı. 0 ise kenar payı yalnız saldıran gövdesinden gelir.</summary>
        public float RadiusM { get; }
        /// <summary>Emici çekme sürerken kalıp bu hedefe yaklaşmaz; vuruş yerinde kalır.</summary>
        public bool HoldApproach { get; }
        /// <summary>Dost kancasının yolundaki boss. Varış dostun yakın kenarıdır, gövdenin içinden geçilmez.</summary>
        public bool HasObstacle { get; }
        public float ObstacleX { get; }
        public float ObstacleZ { get; }
        public float ObstacleRadiusM { get; }

        public MotionTarget At(float x, float z) =>
            new(Has, x, z, RadiusM, HoldApproach, HasObstacle, ObstacleX, ObstacleZ, ObstacleRadiusM);
    }
}
