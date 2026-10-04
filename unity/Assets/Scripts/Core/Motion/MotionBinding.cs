using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Motion
{
    /// <summary>Bir skill'in kalıba bağı. Etiketler mekanik değildir; yalnız saklanır.</summary>
    public sealed class MotionBinding
    {
        public MotionBinding(
            MotionTemplate template,
            bool implemented,
            IReadOnlyList<string> tags,
            float sinirThreshold)
        {
            Template = template;
            Implemented = implemented;
            Tags = tags ?? Array.Empty<string>();
            SinirThreshold = sinirThreshold;
        }

        public MotionTemplate Template { get; }
        public bool Implemented { get; }
        public IReadOnlyList<string> Tags { get; }
        public float SinirThreshold { get; }

        public bool HasTag(string tag)
        {
            for (int i = 0; i < Tags.Count; i++)
                if (string.Equals(Tags[i], tag, StringComparison.Ordinal))
                    return true;
            return false;
        }
    }
}
