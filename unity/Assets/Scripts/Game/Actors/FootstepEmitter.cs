using Dovus.Game.Audio;
using Dovus.Game.Vfx;
﻿using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// Yürüme/koşu adımı: yatay yol <see cref="StrideM"/>'yi her geçtiğinde ayak tozu + ses.
    /// Mixamo kliplerinde animasyon olayı yok; mesafe tabanlı adım kaymaz ve hıza orantılıdır.
    /// Dodge kayması ve ışınlanma sayılmaz.
    /// </summary>
    public sealed class FootstepEmitter : MonoBehaviour
    {
        /// <summary>Tek karede bundan uzun yol ışınlanma/respawn sayılır.</summary>
        const float TeleportM = 2.5f;

        public float StrideM = FootstepEmitterDefaults.StrideM;
        public bool IsBoss;

        SfxDirector _sfx;
        DodgeMotion _dodge;
        Vector3 _last;

        public void Bind(SfxDirector sfx) => _sfx = sfx;
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
            _sfx?.Play(IsBoss ? SfxLibrary.BossStep : SfxLibrary.Footstep);
        }
    }
}
