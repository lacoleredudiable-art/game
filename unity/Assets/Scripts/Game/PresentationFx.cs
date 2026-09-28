using Dovus.Core.Combat;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
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
        Transform _bossTf;

        public void Bind(BossDirector boss, DodgeMotion dodge, CombatFeel feel, HexagonInput input)
        {
            Unbind();
            _boss = boss;
            _dodge = dodge;
            _feel = feel;
            _input = input;
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
            else
            {
                FeelVfx.SlamImpact(_boss.AttackOrigin, _boss.AttackRadiusM);
                SfxDirector.Play(SfxLibrary.BossSlam);
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
