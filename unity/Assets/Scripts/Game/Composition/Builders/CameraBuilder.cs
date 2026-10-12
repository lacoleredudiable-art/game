using Dovus.Game.Arena;
using Dovus.Game.Cameras;
using Dovus.Game.Skills.RuleEngineV4;
using Dovus.Game.Vfx;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game.Composition.Builders
{
    public sealed class CameraBuilder
    {
        public FollowCameraController Build(WorldContext ctx)
        {
            var tuning = ctx.Tuning;
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var camera = camGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = tuning.Visuals.BackgroundColor;
            camera.nearClipPlane = CameraBuilderDefaults.NearClipM;
            camera.farClipPlane = CameraBuilderDefaults.FarClipM;

            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var follow = camGo.AddComponent<FollowCameraController>();
            follow.Tuning = tuning;
            follow.Target = ctx.Player.transform;
            follow.BossTarget = ctx.Boss.transform;
            follow.BindCollisionFiltering(ctx.Player.transform, ctx.Boss.transform, ctx.AllyDummyController?.transform);
            if (tuning.RuleEngineV4.SliceScene)
                BindSliceCamera(ctx, follow);
            camera.fieldOfView = follow.BaseFrame.FovDeg;
            Vector3 startOffset = tuning.Camera.CameraShoulderOffset
                + Vector3.back * follow.BaseFrame.DistanceM;
            camGo.transform.position = ctx.Player.transform.position + startOffset;
            ctx.FollowCameraController = follow;
            ctx.MainCamera = camera;
            return follow;
        }

        /// <summary>
        /// O-5.1–O-5.4 tek kaynak (slice_camera); O-5.8 ve O-5.15 için boss gövdesi: yarıçap world_physics'ten,
        /// tepe görsel boydan (O-6.10 <c>BossVisualHeightM</c>, kökün altındaki taban ActorsBuilder'daki gibi).
        /// </summary>
        static void BindSliceCamera(WorldContext ctx, FollowCameraController follow)
        {
            var catalog = new RuleEngineV4CatalogAccess().Catalog;
            follow.UseBaseFrame(RuleEngineV4CameraFrame.Resolve(catalog.SliceCamera, ctx.Tuning.Camera));
            float footBelowRootM = CompositionConstants.BossHeightM * 0.5f;
            follow.BindBossBody(new CameraBossBody(
                ctx.Boss.transform,
                RuleEngineV4BossBody.RadiusM("CameraBuilder.kamera"),
                ctx.Tuning.Player.BossVisualHeightM - footBelowRootM));
        }

        public void ApplyAtmosphere(WorldContext ctx)
        {
            ArenaFog? sliceFog = ctx.Tuning.RuleEngineV4.SliceScene
                ? (ArenaFog?)RuleEngineV4SliceArenaShellBuilder.Fog(new RuleEngineV4CatalogAccess().Catalog.SliceArenaVisuals.Beyond)
                : null;
            SceneAtmosphere.Apply(ctx.Sun, ctx.MainCamera, ctx.Tuning, sliceFog);
            if (ctx.MainCamera != null && ctx.FollowCameraController != null)
                ctx.MainCamera.fieldOfView = ctx.FollowCameraController.BaseFrame.FovDeg;
            BillboardVfxView.CreateEmberField(ctx.Boss.transform, new Color(1f, 0.45f, 0.12f), rate: CameraBuilderDefaults.BossEmberRate);
        }
    }
}
