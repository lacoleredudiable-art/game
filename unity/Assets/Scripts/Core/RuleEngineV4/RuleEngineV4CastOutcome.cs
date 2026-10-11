namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Mana harcanmış skillin sonucu. Boşa gitme (mana harcanır, yarım bekleme) yalnız iki durumda:
    /// geçerli hedef yok (boş plan) ve bekleyen skillin (Tetikli/Ortak) süresi doldu.
    /// Menzil sonuna kadar kimseye değmeyen mermi ıskadır: tam bekleme.
    /// </summary>
    public enum RuleEngineV4CastOutcome
    {
        Delivered = 0,
        Missed = 1,
        NoValidTarget = 2,
        PendingExpired = 3,
    }
}
