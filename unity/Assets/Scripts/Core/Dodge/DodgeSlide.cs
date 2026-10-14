using System;
using Dovus.Core.Tuning;

namespace Dovus.Core.Dodge
{
    /// <summary>
    /// Dodge engele çarpınca yüzey boyunca kayar (O-4.9). Saf C#: dünya sorguları temsilciyle gelir.
    /// Normal engelden DIŞARI bakar. Taraf +1 = normalin saat yönünde 90° döndürülmüşü (nz, -nx).
    /// </summary>
    public sealed class DodgeSlide
    {
        /// <summary>Düz hareket (from → to). Kesildiyse true; sonuç konum ve kesen yüzeyin normali.</summary>
        public delegate bool MoveFn(float fx, float fz, float tx, float tz, out float rx, out float rz, out float nx, out float nz);

        /// <summary>Konumun reachM içindeki yüzeyi: normal ve kenar payı kadar uzaktaki düzeltilmiş konum. Yüzey yoksa false.</summary>
        public delegate bool ContactFn(float x, float z, float reachM, out float nx, out float nz, out float cx, out float cz);

        /// <summary>Nokta dolu mu (engel ya da aktif telegraph alanı)?</summary>
        public delegate bool FullFn(float x, float z);

        public bool Active { get; private set; }
        public int Side { get; private set; }
        public float BudgetM { get; private set; }
        public float NormalX { get; private set; }
        public float NormalZ { get; private set; }

        public void Reset()
        {
            Active = false;
            Side = 0;
            BudgetM = 0f;
        }

        /// <summary>Çubuk yönünün yüzeye teğet bileşeni, 0-1 (1 = yüzeye paralel).</summary>
        public static float TangentT(float dirX, float dirZ, float nx, float nz)
        {
            float dn = dirX * nx + dirZ * nz;
            float t2 = 1f - dn * dn;
            return t2 <= 0f ? 0f : MathF.Sqrt(t2);
        }

        /// <summary>Çarpmada kaymayı başlatır. Bütçe = R × max(t, SlideMinRatio); taraf burada kilitlenir.</summary>
        public bool Begin(
            float dirX, float dirZ, float nx, float nz,
            float contactX, float contactZ, float remainingM,
            DodgeTuning cfg, FullFn full)
        {
            Reset();
            float nLen = MathF.Sqrt(nx * nx + nz * nz);
            if (nLen < 0.0001f || remainingM <= DodgeDefaults.SlideEpsilonM)
                return false;
            nx /= nLen;
            nz /= nLen;

            float t = TangentT(dirX, dirZ, nx, nz);
            float budget = remainingM * MathF.Max(t, cfg.SlideMinRatio);
            if (budget <= DodgeDefaults.SlideEpsilonM)
                return false;

            int side;
            if (t >= cfg.EngineChoiceT)
                side = dirX * nz - dirZ * nx >= 0f ? 1 : -1;
            else
                side = ChooseEmptier(nx, nz, contactX, contactZ, cfg.SlideScanPadM, full);

            Side = side;
            BudgetM = budget;
            NormalX = nx;
            NormalZ = nz;
            Active = true;
            return true;
        }

        /// <summary>Başlangıç sonrası yüzey normalini yenile (konum kenar payına çekildikten sonra). Taraf kilitli kalır.</summary>
        public void Rebase(float nx, float nz)
        {
            float len = MathF.Sqrt(nx * nx + nz * nz);
            if (len < 0.0001f)
                return;
            NormalX = nx / len;
            NormalZ = nz / len;
        }

        /// <summary>Motor seçimi: SlideScanPadM kadar ileri tara, daha boş tarafı al; eşitse saat yönü (+1).</summary>
        public static int ChooseEmptier(float nx, float nz, float cx, float cz, float padM, FullFn full)
        {
            int cw = Score(1, nx, nz, cx, cz, padM, full);
            int ccw = Score(-1, nx, nz, cx, cz, padM, full);
            return ccw < cw ? -1 : 1;
        }

        static int Score(int side, float nx, float nz, float cx, float cz, float padM, FullFn full)
        {
            if (full == null)
                return 0;
            float tx = side * nz;
            float tz = -side * nx;
            int score = 0;
            const int Samples = 2;
            for (int i = 1; i <= Samples; i++)
            {
                float d = padM * i / Samples;
                if (full(cx + tx * d, cz + tz * d))
                    score++;
            }
            return score;
        }

        /// <summary>
        /// Bir sabit adım: stepM kadar (bütçeyle sınırlı) yüzeyi takip eder. Adım başına en çok SlideMaxIter iterasyon.
        /// Gerçek ilerleme beklenenin SlideStallRatio oranından azsa kayma biter.
        /// </summary>
        public void Advance(float stepM, ref float x, ref float z, DodgeTuning cfg, MoveFn move, ContactFn contact)
        {
            if (!Active)
                return;

            float want = MathF.Min(stepM, BudgetM);
            if (want <= DodgeDefaults.SlideEpsilonM)
            {
                Active = false;
                return;
            }

            float remaining = want;
            int iters = Math.Max(1, cfg.SlideMaxIter);
            for (int i = 0; i < iters && remaining > DodgeDefaults.SlideEpsilonM; i++)
            {
                if (contact != null && contact(x, z, DodgeDefaults.SlideProbeM, out float cnx, out float cnz, out float cx, out float cz))
                {
                    NormalX = cnx;
                    NormalZ = cnz;
                    x = cx;
                    z = cz;
                }

                float tx = Side * NormalZ;
                float tz = -Side * NormalX;
                bool hit = move(x, z, x + tx * remaining, z + tz * remaining,
                    out float rx, out float rz, out float hnx, out float hnz);
                float dx = rx - x;
                float dz = rz - z;
                remaining -= MathF.Sqrt(dx * dx + dz * dz);
                x = rx;
                z = rz;
                if (!hit)
                {
                    // Teğet adım dışarı sürüklenir (≈ adım²/2r): yüzeye geri çek, kenar payı korunur.
                    float movedLen = MathF.Sqrt(dx * dx + dz * dz);
                    if (contact != null && contact(x, z, DodgeDefaults.SlideProbeM + movedLen, out float pnx, out float pnz, out float pcx, out float pcz))
                    {
                        NormalX = pnx;
                        NormalZ = pnz;
                        x = pcx;
                        z = pcz;
                    }

                    break;
                }
                NormalX = hnx;
                NormalZ = hnz;
            }

            float progress = want - MathF.Max(0f, remaining);
            BudgetM -= progress;
            if (progress < cfg.SlideStallRatio * want || BudgetM <= DodgeDefaults.SlideEpsilonM)
                Active = false;
        }
    }
}
