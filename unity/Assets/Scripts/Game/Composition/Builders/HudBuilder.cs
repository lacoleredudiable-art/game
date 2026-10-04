using Dovus.Game.Casting;
using Dovus.Game.DevTools;
using Dovus.Game.Hud;
using UnityEngine;

namespace Dovus.Game.Composition.Builders
{
    public sealed class HudBuilder
    {
        public void Build(WorldContext ctx)
        {
            var tuning = ctx.Tuning;
            var combat = ctx.Combat;
            var root = ctx.HexagonRoot;
            var view = ctx.HexagonView;
            var input = ctx.HexagonInput;
            var vitals = ctx.PlayerVitals;
            var playerStatus = ctx.PlayerStatus;
            var bossStatus = ctx.BossStatus;

            var vitalsHud = root.AddComponent<VitalsHud>();
            vitalsHud.Configure(vitals, ctx.BossVitals, tuning, view.CanvasRoot, ctx.AllyDummy, ctx.PlayerResource);
            ctx.VitalsHud = vitalsHud;

            if (playerStatus != null)
            {
                var playerStrip = root.AddComponent<StatusIconStrip>();
                float stripY = vitalsHud.PlayerStackBottomCanvasY
                    - HexagonLayoutScreen.DpToPixels(tuning.Hud.StatusIconGapDp + 4f);
                float left = HexagonLayoutScreen.SafeLeftInsetPx()
                    + HexagonLayoutScreen.DpToPixels(tuning.Hud.VitalsMarginDp);
                playerStrip.Configure(
                    playerStatus.Board, tuning, view.CanvasRoot,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(left, stripY), "PlayerStatusStrip");
            }

            if (bossStatus != null)
            {
                var bossStrip = root.AddComponent<StatusIconStrip>();
                float stripY = vitalsHud.BossStackBottomCanvasY
                    - HexagonLayoutScreen.DpToPixels(tuning.Hud.StatusIconGapDp + 2f);
                bossStrip.Configure(
                    bossStatus.Board, tuning, view.CanvasRoot,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, stripY), "BossStatusStrip");
            }

            if (ctx.AllyDummy != null)
                ctx.AllyDummy.EnsureStatusBoard();

            var lockHud = root.AddComponent<RecoveryLockHud>();
            lockHud.Configure(input.Engine, combat, tuning, view.CanvasRoot, vitalsHud.BarCount);
            lockHud.BindVitalsHud(vitalsHud);

            if (DebugConfig.Enabled)
            {
                var frameHud = root.AddComponent<FrameTimeHud>();
                frameHud.Configure(tuning, view.CanvasRoot);
            }

            var damageHud = root.AddComponent<DamageNumberHud>();
            damageHud.Configure(tuning, view.CanvasRoot);
            ctx.DamageNumberHud = damageHud;

            var passiveHud = root.AddComponent<PassiveHud>();
            passiveHud.Configure(view);
            passiveHud.BindRunes(ctx.RuneManager);
            ctx.PassiveHud = passiveHud;

            ctx.DodgeMotion.Bind(ctx.Clock, input, ctx.Boss.transform, ctx.Afterimage, ctx.FollowCamera);

            var chargeHud = root.AddComponent<DodgeChargeHud>();
            chargeHud.Bind(input, view);
            if (DebugConfig.Enabled)
            {
                var practice = root.AddComponent<DodgePractice>();
                practice.Bind(ctx.Player.transform, ctx.Boss.transform);
            }
        }
    }
}
