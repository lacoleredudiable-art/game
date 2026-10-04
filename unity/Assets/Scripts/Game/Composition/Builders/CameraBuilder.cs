using Dovus.Game.Arena;
using Dovus.Game.Cameras;
using Dovus.Game.Vfx;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game.Composition.Builders
{
    public sealed class CameraBuilder
    {
        public FollowCamera Build(WorldContext ctx)
        {
            var tuning = ctx.Tuning;
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var camera = camGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = tuning.Visuals.BackgroundColor;
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 250f;
            camera.fieldOfView = tuning.Camera.CameraFovDeg;

            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var follow = camGo.AddComponent<FollowCamera>();
            follow.Tuning = tuning;
            follow.Target = ctx.Player.transform;
            follow.BossTarget = ctx.Boss.transform;
            follow.BindCollisionFiltering(ctx.Player.transform, ctx.Boss.transform, ctx.AllyDummy?.transform);
            Vector3 startOffset = tuning.Camera.CameraShoulderOffset
                + Vector3.back * tuning.Camera.CameraDistanceM;
            camGo.transform.position = ctx.Player.transform.position + startOffset;
            ctx.FollowCamera = follow;
            ctx.MainCamera = camera;
            return follow;
        }

        public void ApplyAtmosphere(WorldContext ctx)
        {
            SceneAtmosphere.Apply(ctx.Sun, ctx.MainCamera, ctx.Tuning);
            BillboardVfx.CreateEmberField(ctx.Boss.transform, new Color(1f, 0.45f, 0.12f), rate: 14f);
        }
    }
}
