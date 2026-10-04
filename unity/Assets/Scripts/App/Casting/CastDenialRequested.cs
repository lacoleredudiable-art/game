namespace Dovus.App.Casting
{
    public readonly struct CastDenialRequested
    {
        public CastDenialRequested(CastDenialReason reason) => Reason = reason;
        public CastDenialReason Reason { get; }
    }
}
