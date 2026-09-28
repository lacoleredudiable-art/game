using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// YÃ¼rÃ¼me/koÅŸu adÄ±mÄ±: yatay yol <see cref="StrideM"/>'yi her geÃ§tiÄŸinde ayak tozu + ses.
    /// Mixamo kliplerinde animasyon olayÄ± yok; mesafe tabanlÄ± adÄ±m kaymaz ve hÄ±za orantÄ±lÄ±dÄ±r.
    /// Dodge kaymasÄ± ve Ä±ÅŸÄ±nlanma sayÄ±lmaz.
    /// </summary>
    public sealed class FootstepEmitter : MonoBehaviour
    {
        /// <summary>Tek karede bundan uzun yol Ä±ÅŸÄ±nlanma/respawn sayÄ±lÄ±r.</summary>
        const float TeleportM = 2.5f;

        public float StrideM = 2.2f;
        public bool IsBoss;

        DodgeMotion _dodge;
        Vector3 _last;
        float _travelled;
        bool _hasLast;

        public int StepCount { get; private set; }

        void Start() => _dodge = GetComponent<DodgeMotion>();

        void LateUpdate()
        {
            Vector3 p = transform.position;
            p.y = 0f;
            if (!_hasLast)
            {
                _last = p;
                _hasLast = true;
                return;
            }

            float d = Vector3.Distance(p, _last);
            _last = p;
            if (d > TeleportM || (_dodge != null && _dodge.IsDisplacing))
                return;

            _travelled += d;
            if (_travelled < StrideM)
                return;
            _travelled -= StrideM;
            if (_travelled > StrideM)
                _travelled = 0f;

            StepCount++;
            FeelVfx.FootDust(transform.position, IsBoss);
            SfxDirector.Play(IsBoss ? SfxLibrary.BossStep : SfxLibrary.Footstep);
        }
    }
}
