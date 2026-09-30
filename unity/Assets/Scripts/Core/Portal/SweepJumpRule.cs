namespace Dovus.Core.Portal
{
    /// <summary>
    /// Tek kare sıçrama kuralı. Blink ve işaretli ışın (yer değiştirme, çapa, kapı) sayılmaz.
    /// Başka hiçbir adım muaf değildir.
    /// </summary>
    public static class SweepJumpRule
    {
        public static bool IsIllegalJump(float stepM, float limitM, bool blinkPhase, bool intentionalTeleport)
        {
            if (blinkPhase || intentionalTeleport)
                return false;
            return stepM > limitM;
        }
    }
}
