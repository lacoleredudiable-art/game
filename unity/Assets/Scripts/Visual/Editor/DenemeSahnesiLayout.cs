using System;

namespace Dovus.Visual.EditorTools
{
    // DenemeSahnesiLayout.json için veri sınıfları (JsonUtility). Koordinatlar Unity uzayında, metre.
    // JSON kutuda tools dışı bir betikle (gen_layout.py) üretildi; elle düzenlenebilir.

    [Serializable]
    public sealed class DenemeLayout
    {
        public int version;
        public float groundTiling = 7f;
        public float detailTiling = 1.8f;
        public RockPlacement[] rocks = new RockPlacement[0];
        public DragonSpec dragon = new DragonSpec();
        public HeroSpec hero = new HeroSpec();
        public CameraSpec camera = new CameraSpec();
        public LavaLightSpec[] lavaLights = new LavaLightSpec[0];
        public BoundarySpec boundary = new BoundarySpec();
        public float[] probesFlat = new float[0];
        public LookSpec look = new LookSpec();
    }

    [Serializable]
    public sealed class RockPlacement
    {
        public string model;
        public string group;
        public float[] pos;
        public float rotY;
        public float[] tilt;
        public float scale = 1f;
    }

    [Serializable]
    public sealed class DragonSpec
    {
        public float[] pos = { 0f, -7f, 96f };
        public float rotY = 180f;
        public float scale = 46f;
        public float[] eyeL = { 0.11f, 0.66f, 0.36f };
        public float[] eyeR = { -0.11f, 0.66f, 0.36f };
        public float[] eyeScale = { 0.035f, 0.022f, 0.02f };
    }

    [Serializable]
    public sealed class HeroSpec
    {
        public float[] pos = { -0.7f, 0f, 0f };
        public float rotY;
    }

    [Serializable]
    public sealed class CameraSpec
    {
        public float[] start = { 1.1f, 1.35f, -8f };
        public float[] end = { 0.55f, 1.05f, -3.9f };
        public float[] lookAt = { -0.6f, 9.5f, 60f };
        public float fov = 50f;
        public float duration = 24f;
    }

    [Serializable]
    public sealed class LavaLightSpec
    {
        public float[] pos;
        public float range = 4f;
        public float intensity = 1.5f;
    }

    [Serializable]
    public sealed class BoundarySpec
    {
        public float radius = 33f;
        public int segments = 32;
        public float height = 6f;
        public float thickness = 1f;
    }

    [Serializable]
    public sealed class LookSpec
    {
        public float[] fogColor = { 0.69f, 0.718f, 0.737f };
        public float fogDensity = 0.0045f;
        public float[] ambientSky = { 0.66f, 0.70f, 0.73f };
        public float[] ambientEquator = { 0.52f, 0.55f, 0.58f };
        public float[] ambientGround = { 0.30f, 0.30f, 0.31f };
        public float[] sunColor = { 0.86f, 0.89f, 0.92f };
        public float sunIntensity = 0.85f;
        public float[] sunEuler = { 52f, -30f, 0f };
        public float shadowStrength = 0.38f;
        public float postExposure = 0.15f;
        public float contrast = -14f;
        public float saturation = -22f;
        public float[] colorFilter = { 0.95f, 0.975f, 1f };
        public float temperature = -8f;
        public float bloomThreshold = 0.95f;
        public float bloomIntensity = 0.65f;
        public float bloomScatter = 0.55f;
        public float[] bloomTint = { 1f, 0.86f, 0.72f };
        public float vignette = 0.12f;
        public float[] lavaEdge = { 0.06f, 0.045f, 0.04f };
        public float[] lavaCore = { 4.2f, 0.62f, 0.08f };
        public float lavaPulse = 0.25f;
        public float[] skyBottom = { 0.47f, 0.51f, 0.54f };
        public float[] skyTop = { 0.62f, 0.66f, 0.69f };
        public float skyFogStrength = 0.72f;
        public float[] mistColor = { 0.72f, 0.745f, 0.76f };
        public float mistAlpha = 0.38f;
        public float[] dragonColor = { 0.33f, 0.38f, 0.41f };
        public float[] eyeColor = { 5f, 2.2f, 0.5f };
        public float[] rockTint = { 0.86f, 0.89f, 0.92f };
        public float[] groundTint = { 0.95f, 0.97f, 1f };
    }
}
