using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Status;

namespace Dovus.Core.Casting
{
    public readonly struct TargetResolution
    {
        public TargetResolution(bool allowed, bool useSelf, int targetId, TargetFailure failure)
        {
            Allowed = allowed;
            UseSelf = useSelf;
            TargetId = targetId;
            Failure = failure;
        }

        public bool Allowed { get; }
        public bool UseSelf { get; }
        public int TargetId { get; }
        public TargetFailure Failure { get; }
    }
}
