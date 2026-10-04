#if UNITY_EDITOR
namespace Dovus.Game.Editor
{
    /// <summary>FixedAngleCapture batchmode varsayılanları (PLAN 2B.16).</summary>
    public static class FixedAngleCaptureDefaults
    {
        public const string ScenePath = "Assets/Scenes/Prototype.unity";
        public const string AnglesRepoRelative = "tools/capture/angles.json";
        public const string HudAngleName = "hud";
        /// <summary>Play sonrası bootstrap bekleme (sn); spec'te yok — PlayMode smoke 5s referans.</summary>
        public const float BootstrapSettleSec = 3f;
    }
}
#endif
