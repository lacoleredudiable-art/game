using System.Collections.Generic;

namespace Dovus.Core.Targeting
{
    public static class LockPointRules
    {
        /// <summary>Yeni kilitte varsayılan nokta: erişilebilir gövde, yoksa ilk erişilebilir nokta.</summary>
        public static int DefaultPointId(IReadOnlyList<LockPoint> points)
        {
            int firstAvailable = TargetLock.NoPoint;
            for (int i = 0; i < points.Count; i++)
            {
                LockPoint p = points[i];
                if (!p.Available)
                    continue;
                if (p.Kind == LockPointKind.Body)
                    return p.Id;
                if (firstAvailable == TargetLock.NoPoint)
                    firstAvailable = p.Id;
            }
            return firstAvailable;
        }

        public static bool TryFind(IReadOnlyList<LockPoint> points, int id, out LockPoint found)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Id != id)
                    continue;
                found = points[i];
                return true;
            }
            found = default;
            return false;
        }

        /// <summary>
        /// Kamera / nişan için gerçek bakış noktası. Seçili nokta kafa ve çok yüksekse (O-5.18) gövdeye düşer.
        /// Seçili nokta yoksa ya da erişilemezse varsayılan nokta. Hiç nokta yoksa false.
        /// </summary>
        public static bool TryResolveAim(
            IReadOnlyList<LockPoint> points,
            int selectedPointId,
            float highHeadThresholdM,
            out LockPoint aim)
        {
            if (!TryFind(points, selectedPointId, out aim) || !aim.Available)
            {
                int fallback = DefaultPointId(points);
                if (fallback == TargetLock.NoPoint || !TryFind(points, fallback, out aim))
                {
                    aim = default;
                    return false;
                }
            }

            if (aim.Kind == LockPointKind.Head && aim.HeightM > highHeadThresholdM)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    if (points[i].Available && points[i].Kind == LockPointKind.Body)
                    {
                        aim = points[i];
                        break;
                    }
                }
            }
            return true;
        }
    }
}
