namespace Dovus.Core.Grammar
{
    /// <summary>
    /// Beşgen komşuluk: her noktanın 2 komşusu (kısa sıçrama) ve 2 uzağı (uzun sıçrama).
    /// Saat yönünde 1–2–3–4–5; komşu = ±1 mod 5, uzak = ±2 mod 5. Elle dizi tablosu yok.
    /// </summary>
    public static class PentagonLayout
    {
        public const int DotCount = 5;

        public static bool IsValidDot(int dot) => dot is >= 1 and <= DotCount;

        /// <summary>İki nokta arasındaki sıçrama türü (aynı nokta = Repeat).</summary>
        public static JumpKind ClassifyJump(int fromDot, int toDot)
        {
            if (!IsValidDot(fromDot) || !IsValidDot(toDot))
                return JumpKind.None;

            if (fromDot == toDot)
                return JumpKind.Repeat;

            int distance = CircularDistance(fromDot, toDot);
            return distance == 1 ? JumpKind.Short : JumpKind.Long;
        }

        public static bool AreNeighbors(int a, int b) =>
            IsValidDot(a) && IsValidDot(b) && a != b && CircularDistance(a, b) == 1;

        public static bool AreFar(int a, int b) =>
            IsValidDot(a) && IsValidDot(b) && a != b && CircularDistance(a, b) == 2;

        public static void GetNeighbors(int dot, out int neighborA, out int neighborB)
        {
            neighborA = Wrap(dot - 1);
            neighborB = Wrap(dot + 1);
        }

        public static void GetFarDots(int dot, out int farA, out int farB)
        {
            farA = Wrap(dot - 2);
            farB = Wrap(dot + 2);
        }

        /// <summary>Beşgende en kısa yay mesafesi (1 veya 2; aynı noktada 0).</summary>
        public static int CircularDistance(int a, int b)
        {
            int d = System.Math.Abs(a - b);
            if (d > DotCount / 2)
                d = DotCount - d;
            return d;
        }

        static int Wrap(int dot)
        {
            int m = (dot - 1) % DotCount;
            if (m < 0) m += DotCount;
            return m + 1;
        }
    }
}
