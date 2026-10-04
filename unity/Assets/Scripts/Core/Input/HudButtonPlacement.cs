using System;

namespace Dovus.Core.Input
{
    public readonly struct Circle2
    {
        public Circle2(float x, float y, float radius)
        {
            X = x;
            Y = y;
            Radius = radius;
        }

        public float X { get; }
        public float Y { get; }
        public float Radius { get; }
    }

    /// <summary>
    /// HUD düğme dairelerini yasak bölgelerden (rün noktaları, panel halkası, dodge) ayırır.
    /// </summary>
    public static class HudButtonPlacement
    {
        public static float EdgeGap(in Circle2 a, in Circle2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return MathF.Sqrt(dx * dx + dy * dy) - a.Radius - b.Radius;
        }

        public static bool Overlaps(in Circle2 button, in Circle2 forbidden, float gap) =>
            EdgeGap(button, forbidden) < gap;

        public static bool OverlapsAny(float x, float y, float buttonRadius, ReadOnlySpan<Circle2> forbidden, float gap)
        {
            var button = new Circle2(x, y, buttonRadius);
            for (int i = 0; i < forbidden.Length; i++)
            {
                if (Overlaps(button, forbidden[i], gap))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Çakışma yoksa konum aynen kalır. Varsa dodge etrafında döndür, sonra panel merkezinden dışarı it.
        /// </summary>
        public static void ResolveAwayFromForbidden(
            ref float x,
            ref float y,
            float buttonRadius,
            ReadOnlySpan<Circle2> forbidden,
            float gap,
            in Circle2 dodgeOrbit,
            float panelCenterX,
            float panelCenterY)
        {
            if (!OverlapsAny(x, y, buttonRadius, forbidden, gap))
                return;

            float orbitDist = dodgeOrbit.Radius + buttonRadius + gap;
            float startAngle = MathF.Atan2(y - dodgeOrbit.Y, x - dodgeOrbit.X);
            float away = MathF.Sign(
                (x - panelCenterX) * (dodgeOrbit.Y - panelCenterY)
                - (y - panelCenterY) * (dodgeOrbit.X - panelCenterX));
            if (away == 0f)
                away = 1f;

            const int orbitSteps = 72;
            for (int i = 0; i <= orbitSteps; i++)
            {
                float angle = startAngle + away * (i / (float)orbitSteps) * MathF.PI * 2f;
                float tx = dodgeOrbit.X + MathF.Cos(angle) * orbitDist;
                float ty = dodgeOrbit.Y + MathF.Sin(angle) * orbitDist;
                if (!OverlapsAny(tx, ty, buttonRadius, forbidden, gap))
                {
                    x = tx;
                    y = ty;
                    PushOutOfForbidden(ref x, ref y, buttonRadius, forbidden, gap, panelCenterX, panelCenterY);
                    return;
                }
            }

            PushOutOfForbidden(ref x, ref y, buttonRadius, forbidden, gap, panelCenterX, panelCenterY);
            EnforceClearance(ref x, ref y, buttonRadius, forbidden, gap);
        }

        public static void EnforceClearance(ref float x, ref float y, float buttonRadius, ReadOnlySpan<Circle2> forbidden, float gap)
        {
            for (int pass = 0; pass < HudButtonPlacementDefaults.Lit24; pass++)
            {
                if (!OverlapsAny(x, y, buttonRadius, forbidden, gap))
                    return;

                var button = new Circle2(x, y, buttonRadius);
                for (int i = 0; i < forbidden.Length; i++)
                {
                    if (EdgeGap(button, forbidden[i]) >= gap)
                        continue;

                    float dx = x - forbidden[i].X;
                    float dy = y - forbidden[i].Y;
                    float d = MathF.Sqrt(dx * dx + dy * dy);
                    if (d < 0.001f)
                    {
                        dx = 1f;
                        dy = 0f;
                        d = 1f;
                    }

                    float target = forbidden[i].Radius + buttonRadius + gap;
                    x = forbidden[i].X + dx / d * target;
                    y = forbidden[i].Y + dy / d * target;
                    button = new Circle2(x, y, buttonRadius);
                }
            }
        }

        static void PushOutOfForbidden(
            ref float x,
            ref float y,
            float buttonRadius,
            ReadOnlySpan<Circle2> forbidden,
            float gap,
            float panelCenterX,
            float panelCenterY)
        {
            float dx = x - panelCenterX;
            float dy = y - panelCenterY;
            float len = MathF.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f)
            {
                dx = 1f;
                dy = 0f;
                len = 1f;
            }

            for (int push = 0; push < HudButtonPlacementDefaults.Lit64 && OverlapsAny(x, y, buttonRadius, forbidden, gap); push++)
            {
                x += dx / len * MathF.Max(gap * HudButtonPlacementDefaults.LitN025f, 2f);
                y += dy / len * MathF.Max(gap * HudButtonPlacementDefaults.LitN025f, 2f);
            }

            for (int iter = 0; iter < HudButtonPlacementDefaults.Lit96; iter++)
            {
                bool any = false;
                var button = new Circle2(x, y, buttonRadius);
                for (int i = 0; i < forbidden.Length; i++)
                {
                    float edge = EdgeGap(button, forbidden[i]);
                    if (edge >= gap)
                        continue;
                    any = true;
                    float ox = x - forbidden[i].X;
                    float oy = y - forbidden[i].Y;
                    float d = MathF.Sqrt(ox * ox + oy * oy);
                    if (d < 0.001f)
                    {
                        ox = 1f;
                        oy = 0f;
                        d = 1f;
                    }

                    float need = gap - edge;
                    x += ox / d * need;
                    y += oy / d * need;
                    button = new Circle2(x, y, buttonRadius);
                }

                if (!any)
                    break;
            }

            EnforceClearance(ref x, ref y, buttonRadius, forbidden, gap);
        }
    }
}
