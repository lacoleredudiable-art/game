using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Tuning;
using Dovus.Game.Audio;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using Dovus.Game.Feel;
using Dovus.Game.Hud;
using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// Oyuncu dodge hakları ve i-frame sorgusu.
    /// </summary>
    public sealed class PlayerDodgeController : MonoBehaviour
    {
        GameClockHost _clock;
        HexagonInputController _input;
        FollowCameraController _camera;
        ReactionReadoutHud _readout;
        ActorView _visual;
        SfxDirector _sfx;

        public void BindSfx(SfxDirector sfx) => _sfx = sfx;

        /// <summary>
        /// Skill hareketi / çağırma anı dokunulmazlığı (F1): 3-7 dash 400 ms, ışınlanma 220 ms,
        /// 11-10 çağırma anı... Stasis değil — oyuncu donmaz, yalnız pencere içindeki vuruş yutulur.
        /// </summary>
        public SkillIframeWindow SkillIframe { get; } = new SkillIframeWindow();

        public void OpenSkillIframe(int durationMs)
        {
            if (durationMs > 0)
                SkillIframe.Open(WorldMs(), durationMs);
        }

        public bool IsSkillInvulnerable => SkillIframe.IsActive(WorldMs());

        /// <summary>Dodge ya da skill i-frame'i açık mı (mermi bunu okur).</summary>
        public static bool IsInvulnerableNow(Component host)
        {
            PlayerDodgeController rig = host != null ? host.GetComponent<PlayerDodgeController>() : null;
            return rig != null && (rig.IsInvulnerable || rig.IsSkillInvulnerable);
        }

        public void Bind(
            GameClockHost clock,
            HexagonInputController input,
            FollowCameraController camera,
            ReactionReadoutHud readout,
            CombatFeelDirector feel)
        {
            _clock = clock;
            _input = input;
            _camera = camera;
            _readout = readout;
        }

        /// <summary>Hasar borusu bunu okur. true ise bu karede vuruş yutulur.</summary>
        public bool IsInvulnerable
        {
            get
            {
                DodgeState dodge = _input != null ? _input.Dodge : null;
                return dodge != null && dodge.IsInvulnerable(WorldMs());
            }
        }

        /// <summary>
        /// Gelen vuruşun tek sorusu. dodgeable false ise i-frame yutmaz.
        /// true dönerse hasar yazılmamalı.
        /// </summary>
        public static bool BlocksIncoming(Component host, bool dodgeable = true)
        {
            if (host == null || !dodgeable)
                return false;
            PlayerDodgeController rig = host.GetComponent<PlayerDodgeController>();
            if (rig == null)
                return false;
            return rig.TryAbsorbHit() || rig.IsSkillInvulnerable;
        }

        public bool TryAbsorbHit() => IsInvulnerable;

        int WorldMs() => _clock != null ? (int)_clock.Director.WorldTimeMs : 0;
    }
}
