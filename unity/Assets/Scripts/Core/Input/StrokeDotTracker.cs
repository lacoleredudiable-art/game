using System.Collections.Generic;

namespace Dovus.Core.Input
{
    /// <summary>
    /// Çizim tanıma kuralı (denetim B ek). Ölçüm: CoreTests <c>DrawRecognitionTests</c> (gürültülü,
    /// elle çizilmiş örnek yollar, 60/30 fps). Kurallar:
    /// 1. İki örnek arası parça taranır (hızlı çizgi noktayı atlamaz).
    /// 2. Noktanın içinden GEÇEN çizgi yalnız merkeze <see cref="CoreFraction"/>·r kadar yaklaşırsa sayılır
    ///    (uzak sıçramada aradaki noktayı sıyırmak yanlış rün eklemesin).
    /// 3. Çizgi nokta halkasında DURURSA (yerleşme) ya da orada BİTERSE tam r ile sayılır.
    /// 4. Çizgi bir köşede keskin dönerse (≥ <see cref="CornerTurnDeg"/>) köşeye en yakın nokta
    ///    <see cref="CornerFraction"/>·r içinde ise sayılır (düşük kare hızında örnekler köşeyi keser).
    /// Aynı nokta, çizgi halkasından çıkmadan iki kez sayılmaz.
    /// </summary>
    public sealed class StrokeDotTracker
    {
        public const float CoreFraction = 0.8f;
        public const float CornerFraction = 1.4f;
        public const float CornerTurnDeg = 60f;

        readonly List<StrokeDotSweep.Hit> _scratch = new List<StrokeDotSweep.Hit>(6);
        float _radius;
        float _minSegment;
        float _settle;
        bool _hasPrev;
        bool _hasPrev2;
        float _px, _py, _p2x, _p2y;
        int _last;
        bool _left = true;

        public int LastDot => _last;

        /// <param name="radius">Nokta yarıçapı (px).</param>
        /// <param name="minSegment">Köşe dönüşü ölçülecek en kısa parça (px, ~12 dp).</param>
        /// <param name="settle">Bu kadar kısa adım "durdu" sayılır (px, ~4 dp).</param>
        public void Begin(float x, float y, float radius, float minSegment, float settle,
            IReadOnlyList<float> dotX, IReadOnlyList<float> dotY,
            List<int> into)
        {
            into.Clear();
            _radius = radius;
            _minSegment = minSegment;
            _settle = settle;
            _last = 0;
            _left = true;
            _hasPrev = true;
            _hasPrev2 = false;
            _px = x;
            _py = y;
            int h = StrokeDotSweep.Inside(x, y, dotX, dotY, radius);
            if (h != 0)
                Register(h, into);
        }

        public void Move(float x, float y,
            IReadOnlyList<float> dotX, IReadOnlyList<float> dotY,
            List<int> into)
        {
            into.Clear();
            if (!_hasPrev)
            {
                Begin(x, y, _radius, _minSegment, _settle, dotX, dotY, into);
                return;
            }

            float ax = _px, ay = _py;
            float segLen = Dist(ax, ay, x, y);
            // Kural 4: önceki örnekte keskin dönüş.
            if (_hasPrev2)
            {
                float l1 = Dist(_p2x, _p2y, ax, ay);
                if (l1 >= _minSegment && segLen >= _minSegment
                    && TurnDeg(_p2x, _p2y, ax, ay, x, y) >= CornerTurnDeg)
                {
                    int c = StrokeDotSweep.Inside(ax, ay, dotX, dotY, _radius * CornerFraction);
                    if (c != 0)
                        Register(c, into);
                }
            }

            // Kural 1+2: parça çekirdek yarıçapla taranır.
            StrokeDotSweep.Sweep(ax, ay, x, y, dotX, dotY, _radius * CoreFraction, _scratch);
            for (int i = 0; i < _scratch.Count; i++)
                Register(_scratch[i].Dot, into);

            // Kural 3: halkada durdu.
            int inside = StrokeDotSweep.Inside(x, y, dotX, dotY, _radius);
            if (inside != 0 && segLen <= _settle)
                Register(inside, into);

            if (_last != 0 && inside != _last
                && Dist(x, y, dotX[_last - 1], dotY[_last - 1]) > _radius)
                _left = true;

            if (segLen >= 0.5f)
            {
                _p2x = ax;
                _p2y = ay;
                _hasPrev2 = true;
                _px = x;
                _py = y;
            }
        }

        /// <summary>Kural 3: çizgi bir nokta halkasında bitti.</summary>
        public void End(float x, float y,
            IReadOnlyList<float> dotX, IReadOnlyList<float> dotY,
            List<int> into)
        {
            into.Clear();
            int h = StrokeDotSweep.Inside(x, y, dotX, dotY, _radius);
            if (h != 0)
                Register(h, into);
            _hasPrev = false;
            _hasPrev2 = false;
        }

        void Register(int dot, List<int> into)
        {
            if (dot == _last && !_left)
                return;
            into.Add(dot);
            _last = dot;
            _left = false;
        }

        static float Dist(float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            return (float)System.Math.Sqrt(dx * dx + dy * dy);
        }

        static float TurnDeg(float ax, float ay, float bx, float by, float cx, float cy)
        {
            float v1x = bx - ax, v1y = by - ay, v2x = cx - bx, v2y = cy - by;
            float n = (float)System.Math.Sqrt((v1x * v1x + v1y * v1y) * (v2x * v2x + v2y * v2y));
            if (n < 1e-6f)
                return 0f;
            float cos = (v1x * v2x + v1y * v2y) / n;
            if (cos > 1f) cos = 1f;
            if (cos < -1f) cos = -1f;
            return (float)(System.Math.Acos(cos) * 180.0 / System.Math.PI);
        }
    }
}
