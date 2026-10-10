using Dovus.Core.Actors;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    static class RuleEngineV4SceneTargetMetrics
    {
        public static void Read(TargetableHost host, out float hpRatio, out float purifyNeed)
        {
            hpRatio = 1f;
            purifyNeed = 0f;
            if (host == null)
                return;

            Transform root = host.transform;
            if (root.TryGetComponent(out AllyDummyController ally))
            {
                hpRatio = ally.Ratio;
                purifyNeed = ScoreBoard(ally.Board);
                return;
            }

            if (root.TryGetComponent(out SliceLightMinionHost minion))
            {
                int max = Mathf.Max(1, minion.MaxHp);
                hpRatio = (float)minion.Hp / max;
                return;
            }

            if (root.TryGetComponent(out ActorStatusHost status))
            {
                purifyNeed = ScoreBoard(status.Board);
            }
        }

        static float ScoreBoard(StatusBoard board)
        {
            if (board == null || board.ActiveCount == 0)
                return 0f;
            float score = 0f;
            foreach (StatusKind k in board.ActiveKinds)
            {
                if (StatusKindUtil.IsHardCc(k))
                    score += 3f;
                else if (StatusKindUtil.IsSoftCc(k))
                    score += 2f;
                else if (StatusKindUtil.IsDebuff(k))
                    score += 1f;
            }
            return score;
        }
    }
}
