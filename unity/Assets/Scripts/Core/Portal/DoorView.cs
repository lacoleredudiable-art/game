using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core.Motion;
using Dovus.Core.Status;

namespace Dovus.Core.Portal
{
    public readonly struct DoorView
    {
        public DoorView(int id, int linkId, float x, float z, float radius, string skillId, float lifeSec, bool projectilesOnly)
        {
            Id = id;
            LinkId = linkId;
            X = x;
            Z = z;
            Radius = radius;
            SkillId = skillId ?? string.Empty;
            LifeSec = lifeSec;
            ProjectilesOnly = projectilesOnly;
        }

        public int Id { get; }
        public int LinkId { get; }
        public float X { get; }
        public float Z { get; }
        public float Radius { get; }
        public string SkillId { get; }
        public float LifeSec { get; }
        public bool ProjectilesOnly { get; }
    }
}
