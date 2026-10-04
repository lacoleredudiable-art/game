using System;
using Dovus.Core.Grammar;

namespace Dovus.Core.Presentation
{
    /// <summary>Hitbox + AnimationType katalog eşleşme sonucu (trajectory yok).</summary>
    public readonly struct PresentationValidationResult
    {
        public PresentationValidationResult(
            string hitboxId,
            string animationTypeId,
            bool hitboxFound,
            bool animationFound)
        {
            HitboxId = hitboxId ?? string.Empty;
            AnimationTypeId = animationTypeId ?? string.Empty;
            HitboxFound = hitboxFound;
            AnimationFound = animationFound;
        }

        public string HitboxId { get; }
        public string AnimationTypeId { get; }
        public bool HitboxFound { get; }
        public bool AnimationFound { get; }

        /// <summary>Her iki id de katalogda.</summary>
        public bool IsValid => HitboxFound && AnimationFound;
    }
}
