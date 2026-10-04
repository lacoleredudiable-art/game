namespace Dovus.Game.Casting.Input
{
    public sealed class CastFeedback
    {
        readonly HexagonInputSession _s;

        public CastFeedback(HexagonInputSession session) => _s = session;

        public void NotifyInsufficientMana()
        {
            _s.Readout?.NoteDenied("yetersiz mana");
            _s.Syllable?.PlayDenied();
        }

        public void NotifyOnCooldown()
        {
            _s.Readout?.NoteDenied("soğumada");
            _s.Syllable?.PlayDenied();
        }
    }
}
