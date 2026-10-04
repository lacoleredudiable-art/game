using Dovus.App.Boss;
using UnityEngine;

namespace Dovus.Game.Boss
{
    public sealed partial class BossDirector
    {
        void Approach(float dtSec)
        {
            if (dtSec <= 0f)
                return;
            Transform aim = AimTarget();
            if (aim == null)
            {
                _visual?.SetSpeed(0f);
                return;
            }

            float speedMult = 1f;
            if (_bossStatus != null)
            {
                if (BossApproachRules.BlocksMovement(_bossStatus != null, _bossStatus.EffectiveBlocksMovement))
                {
                    _visual?.SetSpeed(0f);
                    return;
                }

                speedMult = _bossStatus.EffectiveMoveSpeedMult;
                if (BossApproachRules.BlocksFromSpeedMult(_bossStatus != null, speedMult))
                {
                    _visual?.SetSpeed(0f);
                    return;
                }
            }

            Vector3 home = _reactor.Home;
            float pad = _colors != null ? _colors.Boss.BossApproachStopPadM : BossDirectorDefaults.FallbackApproachStopPadM;
            ApproachStep step = BossApproachRules.ComputeStep(
                home.x,
                home.z,
                aim.position.x,
                aim.position.z,
                _reactor.BodyRadiusM,
                AimTargetRadius(),
                pad,
                IsReversed,
                _combat.Boss.ApproachSpeedMps,
                speedMult,
                dtSec);
            if (step.Stop)
            {
                _visual?.SetSpeed(0f);
                return;
            }

            home.x = step.NewHomeX;
            home.z = step.NewHomeZ;
            _reactor.Home = home;
            _visual?.SetWalk(step.WalkMps);
            TurnToward(new Vector3(step.DirX, 0f, step.DirZ), dtSec);
        }

        void TurnToward(Vector3 dir, float dtSec)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude <= 0.0001f)
                return;
            float rate = _colors != null ? _colors.Boss.BossTurnRateDegPerSec : BossDirectorDefaults.FallbackTurnRateDegPerSec;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), rate * dtSec);
        }

        void FaceTarget()
        {
            Transform aim = AimTarget();
            if (aim == null)
                return;
            Vector3 to = aim.position - _reactor.Home;
            to.y = 0f;
            if (IsReversed)
                to = -to;
            if (to.sqrMagnitude > BossDirectorDefaults.PlanarDirEpsilonSqr)
                transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }
    }
}
