using System.Collections.Generic;
using Dovus.Core.Portal;
using Dovus.Core.Shared;
using Dovus.Core.Team;

namespace CoreTests;

partial class SkillMechanicTagTests
{
    internal static readonly IReadOnlyDictionary<string, PortalOp> ExpectedPortalOps =
        new Dictionary<string, PortalOp>
        {
            ["1-10"] = PortalOp.BackDoor,
            ["2-6"] = PortalOp.Hook,
            ["3-4"] = PortalOp.AnchorOrRecall,
            ["3-10"] = PortalOp.Pair,
            ["8-1"] = PortalOp.Gate,
            ["8-8"] = PortalOp.MirrorGate,
            ["9-10"] = PortalOp.Swap,
            ["10-10"] = PortalOp.Mirror,
            ["11-8"] = PortalOp.Sink,
            ["11-10"] = PortalOp.GatherTeam,
        };

    internal static readonly IReadOnlyDictionary<string, TeamOp> ExpectedTeamOps =
        new Dictionary<string, TeamOp>
        {
            ["3-10"] = TeamOp.Marker,
            ["5-4"] = TeamOp.Mine,
            ["6-8"] = TeamOp.HangBoss,
            ["7-6"] = TeamOp.Rope,
            ["7-9"] = TeamOp.Mark,
            ["8-3"] = TeamOp.Ball,
            ["8-6"] = TeamOp.Link,
            ["10-10"] = TeamOp.Marker,
            ["11-4"] = TeamOp.Turret,
            ["12-6"] = TeamOp.HasteRope,
        };

    internal static readonly string[] ExpectedSwapCancelSkillIds =
    {
        "1-1", "1-4", "1-5", "1-8", "1-12",
        "3-5", "3-8", "5-5", "6-3", "10-1",
    };
}
