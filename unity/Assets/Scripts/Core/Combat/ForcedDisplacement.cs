using Dovus.Core.Status;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Boss'u çeken yer değiştirme. Stasis ve saldırı kilidi bağışıklığı çekmeyi keser.
    /// Kök ve sersemlik sürüklenmeyi engellemez (knockback ile aynı: CC sırasında da uygulanır).
    /// Poise havuzu boss'ta tutulmuyor; poise kırılması oyuncu cast'ini keser, çekmeyi değil.
    /// </summary>
    public static class ForcedDisplacement
    {
        public static bool Allows(StatusBoard board)
        {
            if (board == null)
                return true;
            if (board.IsAttackLockImmune)
                return false;
            if (board.Has(StatusKind.Stasis))
                return false;
            return true;
        }
    }
}
