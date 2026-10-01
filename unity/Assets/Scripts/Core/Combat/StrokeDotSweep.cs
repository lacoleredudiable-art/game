using System;
using System.Collections.Generic;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Çizim tanıma (denetim B ek): parmak örnekleri kare başına bir kez gelir; hızlı bir çizgi iki
    /// örnek arasında bir rün noktasının üstünden geçip onu atlayabiliyordu (yalnız örnek noktası
    /// test ediliyordu). Burada iki örnek arasındaki DOĞRU PARÇASI her nokta dairesiyle kesiştirilir;
    /// girilen noktalar giriş sırasına göre döner. Saf (Unity'siz), CoreTests ölçer.
    /// </summary>
    public static class StrokeDotSweep
    {
        public readonly struct Hit
        {
            public Hit(int dot, float t, float x, float y)
            {
                Dot = dot;
                T = t;
                X = x;
                Y = y;
            }

            /// <summary>1 tabanlı nokta numarası (dotX[0] = nokta 1).</summary>
            public int Dot { get; }
            /// <summary>Parça üstündeki giriş oranı [0,1].</summary>
            public float T { get; }
            public float X { get; }
            public float Y { get; }
        }

        /// <summary>
        /// a→b parçasının girdiği nokta daireleri, giriş sırasıyla <paramref name="into"/>'ya yazılır
        /// (önce temizlenir). a zaten dairenin içindeyse giriş t=0'dır.
        /// </summary>
        public static void Sweep(
            float ax, float ay, float bx, float by,
            IReadOnlyList<float> dotX, IReadOnlyList<float> dotY, float radius,
            List<Hit> into)
        {
            into.Clear();
            if (radius <= 0f || dotX == null || dotY == null)
                return;
            float dx = bx - ax;
            float dy = by - ay;
            float a = dx * dx + dy * dy;
            float r2 = radius * radius;
            int n = Math.Min(dotX.Count, dotY.Count);
            for (int i = 0; i < n; i++)
            {
                float fx = ax - dotX[i];
                float fy = ay - dotY[i];
                float c = fx * fx + fy * fy - r2;
                float t;
                if (c <= 0f)
                {
                    t = 0f;
                }
                else
                {
                    if (a < 1e-6f)
                        continue;
                    float b = 2f * (fx * dx + fy * dy);
                    float disc = b * b - 4f * a * c;
                    if (disc < 0f)
                        continue;
                    t = (-b - (float)Math.Sqrt(disc)) / (2f * a);
                    if (t < 0f || t > 1f)
                        continue;
                }
                into.Add(new Hit(i + 1, t, ax + dx * t, ay + dy * t));
            }
            if (into.Count > 1)
                into.Sort((p, q) => p.T.CompareTo(q.T));
        }

        /// <summary>Bir noktanın içinde mi (en yakın, yoksa 0).</summary>
        public static int Inside(float x, float y, IReadOnlyList<float> dotX, IReadOnlyList<float> dotY, float radius)
        {
            int best = 0;
            float bestD = radius * radius;
            int n = Math.Min(dotX.Count, dotY.Count);
            for (int i = 0; i < n; i++)
            {
                float ex = x - dotX[i];
                float ey = y - dotY[i];
                float d = ex * ex + ey * ey;
                if (d <= bestD)
                {
                    bestD = d;
                    best = i + 1;
                }
            }
            return best;
        }
    }
}
