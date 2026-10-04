using System;

namespace Dovus.Core.Capture
{
    /// <summary>tools/capture/angles.json tek önayar satırı (PLAN 2B.16).</summary>
    public sealed class CaptureAnglePreset
    {
        public CaptureAnglePreset(
            string name,
            float posX,
            float posY,
            float posZ,
            float eulerX,
            float eulerY,
            float eulerZ,
            float fov,
            int width,
            int height)
        {
            Name = name ?? string.Empty;
            PosX = posX;
            PosY = posY;
            PosZ = posZ;
            EulerX = eulerX;
            EulerY = eulerY;
            EulerZ = eulerZ;
            Fov = fov;
            Width = width;
            Height = height;
        }

        public string Name { get; }
        public float PosX { get; }
        public float PosY { get; }
        public float PosZ { get; }
        public float EulerX { get; }
        public float EulerY { get; }
        public float EulerZ { get; }
        public float Fov { get; }
        public int Width { get; }
        public int Height { get; }
    }
}
