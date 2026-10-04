using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core.Motion;
using Dovus.Core.Status;

namespace Dovus.Core.Portal
{
    public readonly struct Body
    {
        public Body(int id, float x, float y, float z, float radius, bool templateOwns, bool isBoss)
        {
            Id = id;
            X = x;
            Y = y;
            Z = z;
            Radius = radius > 0f ? radius : 0.5f;
            TemplateOwns = templateOwns;
            IsBoss = isBoss;
        }

        public int Id { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float Radius { get; }
        public bool TemplateOwns { get; }
        public bool IsBoss { get; }
    }
}
