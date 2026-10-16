using System;
using System.Collections.Generic;
using Dovus.Core.Portal;
using Dovus.Core.Shared;
using Dovus.Core.Team;

namespace CoreTests;

partial class SkillMechanicTagTests
{
    internal static readonly IReadOnlyDictionary<string, PortalOp> ExpectedPortalOps =
        new Dictionary<string, PortalOp>();

    internal static readonly IReadOnlyDictionary<string, TeamOp> ExpectedTeamOps =
        new Dictionary<string, TeamOp>();

    internal static readonly string[] ExpectedSwapCancelSkillIds = Array.Empty<string>();
}
