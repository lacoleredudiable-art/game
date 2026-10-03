using Dovus.Core.Portal;
using Dovus.Core.Team;

namespace Dovus.Core.Data
{
    public static class SkillMechanicOpParse
    {
        public static PortalOp ParsePortalOp(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return PortalOp.None;
            switch (raw)
            {
                case "back_door": return PortalOp.BackDoor;
                case "hook": return PortalOp.Hook;
                case "anchor_or_recall": return PortalOp.AnchorOrRecall;
                case "pair": return PortalOp.Pair;
                case "gate": return PortalOp.Gate;
                case "mirror_gate": return PortalOp.MirrorGate;
                case "swap": return PortalOp.Swap;
                case "mirror": return PortalOp.Mirror;
                case "sink": return PortalOp.Sink;
                case "gather_team": return PortalOp.GatherTeam;
                default: return PortalOp.None;
            }
        }

        public static TeamOp ParseTeamOp(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return TeamOp.None;
            switch (raw)
            {
                case "marker": return TeamOp.Marker;
                case "mine": return TeamOp.Mine;
                case "hang_boss": return TeamOp.HangBoss;
                case "rope": return TeamOp.Rope;
                case "mark": return TeamOp.Mark;
                case "ball": return TeamOp.Ball;
                case "link": return TeamOp.Link;
                case "turret": return TeamOp.Turret;
                case "haste_rope": return TeamOp.HasteRope;
                default: return TeamOp.None;
            }
        }
    }
}
