using Dovus.Core.Actors;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    static class RuleEngineV4SceneTargetMetrics
    {
        public static void Read(TargetableHost host, out float hpRatio, out float purifyNeed)
        {
            hpRatio = 1;
            purifyNeed = 0;
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
                if (host.ActorId == ActorDefaults.BossId && status.TryGetHpRatio(out float bossRatio))
                    hpRatio = bossRatio;
            }
        }

        static float ScoreBoard(StatusBoard board)
        {
            if (board == null || board.ActiveCount == 0)
                return 0;
            float score = 0;
            foreach (StatusKind k in board.ActiveKinds)
            {
                if (StatusKindUtil.IsHardCc(k))
                    score += RuleEngineV4UnitySceneDefaults.StatusHardCcWeight;
                else if (StatusKindUtil.IsSoftCc(k))
                    score += RuleEngineV4UnitySceneDefaults.StatusSoftCcWeight;
                else if (StatusKindUtil.IsDebuff(k))
                    score += RuleEngineV4UnitySceneDefaults.StatusDebuffWeight;
            }
            return score;
        }
    }
}
