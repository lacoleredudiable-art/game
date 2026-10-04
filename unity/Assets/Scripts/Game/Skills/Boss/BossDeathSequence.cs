using Dovus.Core.Combat;

namespace Dovus.Game.Skills.Boss
{
    public sealed class BossDeathSequence
    {
        readonly IBossDeathSequenceHost _host;
        readonly BossDeathSchedule _schedule = new();

        public BossDeathSchedule Schedule => _schedule;

        public BossDeathSequence(IBossDeathSequenceHost host) => _host = host;

        public void OnPlayerDamageBlocked(float absorbed)
        {
            _host.NoteShieldBlockIfGuarding();
            _host.OnJsonShieldBlocked();
        }

        public void OnBossDied()
        {
            if (_host.Clock == null)
                return;
            Begin(_host.Clock.Director.WorldTimeMs);
        }

        public void OnBossRevivedExternally()
        {
            if (!_schedule.Pending)
                return;
            _schedule.Cancel();
            _host.Boss?.EndCollapse();
            if (_host.Clock != null)
                _host.BossDirector?.NotifyBossRevived(_host.Clock.Director.WorldTimeMs);
        }

        public void OnBossDamageOverTime(float amount)
        {
            if (amount <= 0f || _host.Boss == null)
                return;
            _host.DamageHud?.ShowDamage(
                amount,
                false,
                _host.BossHitPoint(),
                _host.DamageTint(),
                victimIsBoss: true);
        }

        public void Begin(double worldMs)
        {
            float collapseSec = _host.Colors != null ? _host.Colors.BossDeathCollapseSec : 0.85f;
            if (!_schedule.Begin(worldMs, collapseSec))
                return;
            _host.BossDirector?.NotifyBossDown(worldMs);
            _host.Boss?.BeginCollapse(collapseSec, worldMs);
        }

        public void TickBossDeath()
        {
            if (_host.Clock == null || !_schedule.TryRevive(_host.Clock.Director.WorldTimeMs))
                return;

            _host.BossVitals?.Revive();
            _host.Boss?.EndCollapse();
            _host.BossDirector?.NotifyBossRevived(_host.Clock.Director.WorldTimeMs);
        }
    }
}
