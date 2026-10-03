namespace Dovus.Core.Shared
{
    public interface IClock
    {
        double NowMs { get; }
        double DeltaMs { get; }
        /// <summary>Unity <c>Time.deltaTime</c> ile birebir kare süresi (saniye).</summary>
        float DeltaSec { get; }
    }
}
