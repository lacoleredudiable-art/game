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
using Dovus.Game.DevTools;
using Dovus.Game.Feel;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// Dövüş olaylarını efekt + sese bağlar: boss windup/vuruş/kükreme, dodge, perfect dodge,
    /// oyuncu isabeti, UI dokunuşu. Yalnız dinler; oyun durumuna yazmaz.
    /// </summary>
    public sealed class PresentationFx : MonoBehaviour
    {
        BossDirector _boss;
        DodgeMotion _dodge;
        CombatFeel _feel;
        HexagonInput _input;
        FollowCamera _follow;
        CombatTuning _combat;
        Transform _bossTf;

        public void Bind(BossDirector boss, DodgeMotion dodge, CombatFeel feel, HexagonInput input, FollowCamera follow = null, CombatTuning combat = null)
        {
            Unbind();
            _boss = boss;
            _dodge = dodge;
            _feel = feel;
            _input = input;
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

        void OnWindup(BossAttackKind kind) => SfxDirector.Play(SfxLibrary.BossWindup);

        void OnStruck(BossAttackKind kind)
        {
            if (_boss == null)
                return;
            if (kind == BossAttackKind.FireCone)
            {
                Vector3 fwd = _bossTf.forward;
                fwd.y = 0f;
                Vector3 mouth = _boss.AttackOrigin + fwd.normalized * 0.6f + Vector3.up * 1.4f;
                FeelVfx.FireCone(mouth, fwd, _boss.AttackArcHalfAngleDeg, _boss.AttackRadiusM);
                SfxDirector.Play(SfxLibrary.BossFire);
            }
            else if (kind == BossAttackKind.Volley)
            {
                // Mermiler kendi görünür; şok dalgası çizilmez.
                SfxDirector.Play(SfxLibrary.BossFire);
            }
            else
            {
                FeelVfx.SlamImpact(_boss.AttackOrigin, _boss.AttackRadiusM);
                SfxDirector.Play(SfxLibrary.BossSlam);
                float px = _combat != null ? _combat.Feel.ShakeBossSlamPx : 19f;
                float decay = _combat != null ? _combat.Feel.ShakeDecay : 6f;
                _follow?.AddShakePxAtLeast(px, decay);
                DebugConfig.DevLog($"[Feel2Verify] boss-slam shake={px}px");
            }
        }

        void OnPhase(int phase)
        {
            if (phase > 1)
                SfxDirector.Play(SfxLibrary.BossRoar);
        }

        void OnSlide(Vector3 start, Vector3 dir)
        {
            FeelVfx.DodgeDust(start, dir);
            SfxDirector.Play(SfxLibrary.Dodge);
        }

        void OnExchange(ExchangeResult result)
        {
            if (result.Outcome == ExchangeOutcome.Dodged && result.Grade == DodgeGrade.Mukemmel)
                SfxDirector.Play(SfxLibrary.PerfectDodge);
            else if (result.Outcome == ExchangeOutcome.Hit)
                SfxDirector.Play(SfxLibrary.PlayerHurt);
        }

        void OnDot(int dot) => SfxDirector.Play(SfxLibrary.UiTap);
    }
}
