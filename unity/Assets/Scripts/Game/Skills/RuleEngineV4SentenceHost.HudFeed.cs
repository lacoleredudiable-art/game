using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Game.Hud;
using UnityEngine;

namespace Dovus.Game.Skills
{
    /// <summary>Hasar sayısı beslemesi: isabet noktası (etki kaynağına bakan boss yüzeyi) + element rengi.</summary>
    public sealed partial class RuleEngineV4SentenceHost
    {
        Vector3? _lastImpactOrigin;

        internal void NoteImpactOrigin(LivingEffect logic)
        {
            if (logic != null)
                _lastImpactOrigin = new Vector3(logic.OriginX, 0f, logic.OriginZ);
        }

        internal Vector3? BossHitPoint()
        {
            if (_boss == null)
                return null;
            Vector3 center = _boss.transform.position;
            Vector3 from = _lastImpactOrigin ?? (_player != null ? _player.position : center);
            return BossDamageAnchor.HitPoint(_boss, from);
        }

        internal Color? DamageTint()
        {
            ElementPaintNode? paint = SelectedElementPaint;
            if (!paint.HasValue || string.IsNullOrEmpty(paint.Value.ColorHex))
                return null;
            string hex = paint.Value.ColorHex.StartsWith("#") ? paint.Value.ColorHex : "#" + paint.Value.ColorHex;
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : null;
        }
    }
}
