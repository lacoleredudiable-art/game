using System;
using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Presentation
{
    public readonly struct AnimationBinding
    {
        public AnimationBinding(
            string weaponKey,
            int verbId,
            string displayName,
            string animatorState)
        {
            WeaponKey = weaponKey ?? string.Empty;
            VerbId = verbId;
            DisplayName = displayName ?? string.Empty;
            AnimatorState = animatorState ?? string.Empty;
        }

        public string WeaponKey { get; }
        public int VerbId { get; }
        public string DisplayName { get; }
        public string AnimatorState { get; }
    }
}
