using Dovus.Game.Arena;
using Dovus.Game.Config;
using Dovus.Game.DevTools;
using Dovus.Game.Assets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Dovus.Game.Composition.Builders
{
    public sealed class ArenaBuilder
    {
        public void BuildArena(WorldContext ctx)
        {
            var tuning = ctx.Tuning;
            ctx.Arena = CreateArena(tuning);
            float walkHalf = tuning.Arena.ArenaHalfSizeM;
            ctx.WalkHalf = walkHalf;
            ctx.Combat.SkillMotion.ArenaHalfSizeM = walkHalf;
            if (AssetLoader.Load<Material>("Presentation/ParticlesUnlitAnchor", null) == null)
                Debug.LogWarning("[Feel] Presentation/ParticlesUnlitAnchor yok — parçacık shader strip riski.");
            if (AssetLoader.Load<CombatAmbienceAssets>(CombatAmbienceAssets.ResourcePath, null) == null)
                LavaDecor.Build(ctx.Arena.transform, walkHalf);
            CombatAmbienceEnvironment.Build(ctx.Arena, walkHalf, tuning);
            DebugConfig.DevLog($"[Arena] circle r={walkHalf:0.##}m wallH={tuning.Arena.ArenaWallHeightM:0.#}m");
            ctx.SpawnMaxR = walkHalf * ArenaBuilderDefaults.SpawnMaxRadiusFraction;
        }

        public Light BuildSun(WorldContext ctx)
        {
            var sunGo = new GameObject("Sun");
            var light = sunGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = ArenaBuilderDefaults.SunIntensity;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(ArenaBuilderDefaults.SunPitchDeg, -ArenaBuilderDefaults.SunYawDeg, 0f);
            RenderSettings.sun = light;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.48f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.26f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.11f, 0.10f);
            ctx.Sun = light;
            return light;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        static GameObject CreateArena(GameTuning tuning)
        {
            Color wall = new Color(0.28f, 0.26f, 0.24f);
            return CircularArena.Build(
                tuning.Arena.ArenaHalfSizeM,
                tuning.Arena.ArenaWallHeightM,
                tuning.Arena.ArenaWallThicknessM,
                tuning.Visuals.GroundColor,
                wall);
        }
    }
}
