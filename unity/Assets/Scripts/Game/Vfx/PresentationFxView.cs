using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Audio;
using Dovus.Game.Boss;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Diagnostics;
using Dovus.Game.Feel;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// Dövüş olaylarını efekt + sese bağlar: boss windup/vuruş/kükreme, dodge, perfect dodge,
    /// oyuncu isabeti, UI dokunuşu. Yalnız dinler; oyun durumuna yazmaz.
    /// </summary>
    public sealed class PresentationFxView : MonoBehaviour
    {
        BossDirector _boss;
        DodgeMotionController _dodge;
        CombatFeelDirector _feel;
        HexagonInputController _input;
        FollowCameraController _follow;
        CombatTuning _combat;
        Transform _bossTf;
        SfxDirector _sfx;
        FeelVfxRuntime _feelVfx;

        public void BindFeelVfx(FeelVfxRuntime feelVfx) => _feelVfx = feelVfx;

        public void Bind(BossDirector boss, DodgeMotionController dodge, CombatFeelDirector feel, HexagonInputController input, SfxDirector sfx, FollowCameraController follow = null, CombatTuning combat = null)
        {
            Unbind();
            _boss = boss;
            _dodge = dodge;
            _feel = feel;
            _input = input;
            _sfx = sfx;
            _follow = follow;
            _combat = combat;
            _bossTf = boss != null ? boss.transform : null;

            if (_boss != null)
            {
                _boss.AttackWindupStarted += OnWindup;
                _boss.AttackStruck += OnStruck;
                _boss.BossPhaseChanged += OnPhase;
            }
            if (_dodge != null)
                _dodge.SlideStarted += OnSlide;
            if (_feel != null)
                _feel.Exchanged += OnExchange;
            if (_input != null)
                _input.DotAccepted += OnDot;
        }

        void OnDestroy() => Unbind();

        void Unbind()
        {
            if (_boss != null)
            {
                _boss.AttackWindupStarted -= OnWindup;
                _boss.AttackStruck -= OnStruck;
                _boss.BossPhaseChanged -= OnPhase;
            }
            if (_dodge != null)
                _dodge.SlideStarted -= OnSlide;
            if (_feel != null)
                _feel.Exchanged -= OnExchange;
            if (_input != null)
                _input.DotAccepted -= OnDot;
        }

        void OnWindup(BossAttackKind kind) => _sfx?.Play(SfxLibrary.BossWindup);

        void OnStruck(BossAttackKind kind)
        {
            if (_boss == null)
                return;
            if (kind == BossAttackKind.FireCone)
            {
                Vector3 fwd = _bossTf.forward;
                fwd.y = 0f;
                Vector3 mouth = _boss.AttackOrigin + fwd.normalized * VfxDefaults.BossMouthForwardOffsetM + Vector3.up * VfxDefaults.BossMouthLiftM;
                _feelVfx?.FireCone(mouth, fwd, _boss.AttackArcHalfAngleDeg, _boss.AttackRadiusM);
                _sfx?.Play(SfxLibrary.BossFire);
            }
            else if (kind == BossAttackKind.Volley)
            {
                // Mermiler kendi görünür; şok dalgası çizilmez.
                _sfx?.Play(SfxLibrary.BossFire);
            }
            else
            {
                _feelVfx?.SlamImpact(_boss.AttackOrigin, _boss.AttackRadiusM);
                _sfx?.Play(SfxLibrary.BossSlam);
                float px = _combat != null ? _combat.Feel.ShakeBossSlamPx : VfxDefaults.DefaultBossSlamShakePx;
                float decay = _combat != null ? _combat.Feel.ShakeDecay : VfxDefaults.DefaultCameraShakeDecay;
                _follow?.AddShakePxAtLeast(px, decay);
                DebugConfig.DevLog($"[Feel2Verify] boss-slam shake={px}px");
            }
        }

        void OnPhase(int phase)
        {
            if (phase > 1)
                _sfx?.Play(SfxLibrary.BossRoar);
        }

        void OnSlide(Vector3 start, Vector3 dir)
        {
            _feelVfx?.DodgeDust(start, dir);
            _sfx?.Play(SfxLibrary.Dodge);
        }

        void OnExchange(ExchangeResult result)
        {
            if (result.Outcome == ExchangeOutcome.Dodged && result.Grade == DodgeGrade.Mukemmel)
                _sfx?.Play(SfxLibrary.PerfectDodge);
            else if (result.Outcome == ExchangeOutcome.Hit)
                _sfx?.Play(SfxLibrary.PlayerHurt);
        }

        void OnDot(int dot) => _sfx?.Play(SfxLibrary.UiTap);
    }
}
