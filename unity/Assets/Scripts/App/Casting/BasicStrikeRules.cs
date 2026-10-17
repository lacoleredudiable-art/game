namespace Dovus.App.Casting
{
    public static class BasicStrikeRules
    {
        public static float SubHitScale(int hits) => 1f / System.Math.Max(1, hits);
    }
}
