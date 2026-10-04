using Dovus.Core.Status;

namespace Dovus.Core.Boss
{
    /// <summary>Boss saldırısının ayakları yerde mi, yoksa yer değiştirerek mi vurduğu.</summary>
    public enum BossAttackMotion
    {
        Standing,
        Charge,
        Leap,
        Dash
    }
}
