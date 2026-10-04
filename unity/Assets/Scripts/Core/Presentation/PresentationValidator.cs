using System;
using Dovus.Core.Grammar;

namespace Dovus.Core.Presentation
{
    /// <summary>
    /// SkillResolution → prezentasyon katmanı köprüsü (hitbox + animation).
    /// Trajectory eşleştirmesi yapmaz (Görev 16); ManifestationDirector'a bağlanmaz (Faz 6).
    /// Eşleşmeyen id sessizce "yok" döner — çağıran docs/durum.md'ye yazar.
    /// </summary>
    public sealed class PresentationValidator
    {
        readonly PresentationCatalog _catalog;

        public PresentationValidator(PresentationCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public PresentationCatalog Catalog => _catalog;

        /// <summary>
        /// <see cref="SkillResolution.Hitbox"/> → Hitboxes;
        /// <see cref="SkillResolution.AnimationType"/> → Animations.
        /// </summary>
        public PresentationValidationResult Validate(in SkillResolution resolution)
        {
            string hitboxId = resolution.Hitbox ?? string.Empty;
            string animationId = resolution.AnimationType ?? string.Empty;

            bool hitboxFound = !string.IsNullOrEmpty(hitboxId)
                && _catalog.TryGetHitbox(hitboxId, out _);
            bool animationFound = !string.IsNullOrEmpty(animationId)
                && _catalog.TryGetAnimation(animationId, out _);

            return new PresentationValidationResult(
                hitboxId, animationId, hitboxFound, animationFound);
        }
    }
}
