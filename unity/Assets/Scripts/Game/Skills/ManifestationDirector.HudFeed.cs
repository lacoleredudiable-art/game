using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using UnityEngine;

namespace Dovus.Game.Skills
{
    /// <summary>Hasar say??s?? beslemesi: isabet noktas?? (etki kayna????na bakan boss y??zeyi) + element rengi.</summary>
    public sealed partial class ManifestationDirector
    {
        Vector3? _lastImpactOrigin;

        void NoteImpactOrigin(LivingEffect logic)
        {
            if (logic != null)
                _lastImpactOrigin = new Vector3(logic.OriginX, 0f, logic.OriginZ);
        }

        Vector3? BossHitPoint()
        {
            if (_boss == null)
                return null;
            Vector3 center = _boss.transform.position;
            Vector3 from = _lastImpactOrigin ?? (_player != null ? _player.position : center);
            Vector3 dir = from - center;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f)
                dir = Vector3.forward;
            // Boss g??vde yar????ap?? ve kafa alt?? y??kseklik: say?? g??vdenin vurulan y??z??nde do??ar.
            return center + dir.normalized * _boss.BodyRadiusM + Vector3.up * (_boss.BodyRadiusM * 2f);
        }

        Color? DamageTint()
        {
            ElementPaintNode? paint = SelectedElementPaint;
            if (!paint.HasValue || string.IsNullOrEmpty(paint.Value.ColorHex))
                return null;
            string hex = paint.Value.ColorHex.StartsWith("#") ? paint.Value.ColorHex : "#" + paint.Value.ColorHex;
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : null;
        }
    }
}
