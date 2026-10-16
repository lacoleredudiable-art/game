using System.Collections.Generic;

namespace Dovus.Core.Targeting
{
    /// <summary>
    /// Oyuncunun hedef kilidi (Core, sahne yok): kilitli hedef + hedef üstündeki kilit noktası.
    /// Alma, hedef değiştirme, nokta değiştirme ve otomatik düşme burada; kamera davranışı, model üstündeki
    /// nokta konumları ve işaret görseli Game'de. Kilit eski "en yakın düşman" seçimini yalnız geçersiz kılar:
    /// <see cref="ToManualTarget"/> çıktısı RuleEngineV4AutoTargetPicker.Pick'in manuel hedef girdisidir.
    /// Güdümlü skill kilidi (8 s tavan, K 22:53) ayrı bir kuraldır, bununla karışmaz.
    /// </summary>
    public sealed class TargetLock
    {
        public const int NoPoint = -1;

        bool _lockedHostile;

        public bool IsLocked { get; private set; }
        public int TargetId { get; private set; }
        public int PointId { get; private set; } = NoPoint;
        public LockReleaseReason LastRelease { get; private set; }

        /// <summary>Hedef seçme yolu: bu kimliğe kilitlen. Aday uygun ve alma mesafesi içinde olmalı.</summary>
        public bool TryAcquire(int id, IReadOnlyList<LockOnCandidate> candidates, in LockOnRange range)
        {
            if (!Find(candidates, id, out LockOnCandidate c) || !c.Available || c.DistanceM > range.AcquireM)
                return false;
            if (IsLocked && TargetId == id)
                return true;
            Lock(c);
            return true;
        }

        /// <summary>Otomatik: alma mesafesindeki en yakın düşman (eşitte küçük kimlik).</summary>
        public bool TryAcquireNearestHostile(IReadOnlyList<LockOnCandidate> candidates, in LockOnRange range)
        {
            bool found = false;
            LockOnCandidate best = default;
            for (int i = 0; i < candidates.Count; i++)
            {
                LockOnCandidate c = candidates[i];
                if (!c.Available || !c.IsHostile || c.DistanceM > range.AcquireM)
                    continue;
                if (!found || c.DistanceM < best.DistanceM
                    || (c.DistanceM == best.DistanceM && c.Id < best.Id))
                {
                    best = c;
                    found = true;
                }
            }
            if (!found)
                return false;
            Lock(best);
            return true;
        }

        public void Release()
        {
            if (!IsLocked)
                return;
            Drop(LockReleaseReason.Manual);
        }

        /// <summary>
        /// Her karede çağrılır. Hedef yok / uygun değil ise kilit düşer (mesafe bakılmaz, G-10);
        /// seçili nokta kullanılamaz olduysa varsayılan noktaya döner. Hâlâ kilitliyse true.
        /// </summary>
        public bool Update(IReadOnlyList<LockOnCandidate> candidates)
        {
            if (!IsLocked)
                return false;
            if (!Find(candidates, TargetId, out LockOnCandidate c) || !c.Available)
            {
                Drop(LockReleaseReason.TargetGone);
                return false;
            }
            if (!LockPointRules.TryFind(c.Points, PointId, out LockPoint p) || !p.Available)
                PointId = LockPointRules.DefaultPointId(c.Points);
            return true;
        }

        /// <summary>
        /// Aynı taraftaki (düşman/dost) uygun komşuya geç. direction &gt; 0 sağa (bakış açısı artar), &lt; 0 sola;
        /// o yönde kimse kalmadıysa öbür uçtan başlar (döngü). Tek aday varsa değişmez.
        /// </summary>
        public bool SwitchTarget(int direction, IReadOnlyList<LockOnCandidate> candidates, in LockOnRange range)
        {
            if (!IsLocked || direction == 0 || !Find(candidates, TargetId, out LockOnCandidate current))
                return false;

            bool found = false;
            LockOnCandidate next = default;
            bool wrapFound = false;
            LockOnCandidate wrap = default;
            for (int i = 0; i < candidates.Count; i++)
            {
                LockOnCandidate c = candidates[i];
                if (c.Id == TargetId || !c.Available || c.IsHostile != _lockedHostile || c.DistanceM > range.AcquireM)
                    continue;
                // direction yönünde "ileri" sayılan adaylardan current'a en yakın olanı seç.
                if (Order(c, current, direction) > 0)
                {
                    if (!found || Order(c, next, direction) < 0)
                    {
                        next = c;
                        found = true;
                    }
                }
                // Döngü için direction yönünün en ucundan öbür uca sarma adayı: en "geri" olan.
                if (!wrapFound || Order(c, wrap, direction) < 0)
                {
                    wrap = c;
                    wrapFound = true;
                }
            }

            if (!found)
            {
                if (!wrapFound)
                    return false;
                next = wrap;
            }
            Lock(next);
            return true;
        }

        /// <summary>Kilitli hedefin sıradaki erişilebilir noktasına geç (direction işareti yönü). Değiştiyse true.</summary>
        public bool CyclePoint(int direction, IReadOnlyList<LockOnCandidate> candidates)
        {
            if (!IsLocked || direction == 0 || !Find(candidates, TargetId, out LockOnCandidate c))
                return false;
            IReadOnlyList<LockPoint> pts = c.Points;
            int n = pts.Count;
            if (n == 0)
                return false;
            int start = -1;
            for (int i = 0; i < n; i++)
            {
                if (pts[i].Id == PointId)
                {
                    start = i;
                    break;
                }
            }
            int step = direction > 0 ? 1 : -1;
            for (int k = 1; k <= n; k++)
            {
                int idx = (((start < 0 ? (step > 0 ? -1 : 0) : start) + step * k) % n + n) % n;
                if (!pts[idx].Available)
                    continue;
                if (pts[idx].Id == PointId)
                    return false;
                PointId = pts[idx].Id;
                return true;
            }
            return false;
        }

        /// <summary>Belirli bir noktaya geç (dokunarak seçme). Nokta kilitli hedefte olmalı ve erişilebilir olmalı.</summary>
        public bool TrySelectPoint(int pointId, IReadOnlyList<LockOnCandidate> candidates)
        {
            if (!IsLocked || !Find(candidates, TargetId, out LockOnCandidate c))
                return false;
            if (!LockPointRules.TryFind(c.Points, pointId, out LockPoint p) || !p.Available)
                return false;
            PointId = pointId;
            return true;
        }

        /// <summary>RuleEngineV4AutoTargetPicker.Pick için manuel hedef girdisi.</summary>
        public void ToManualTarget(out bool manualSelected, out int? manualId)
        {
            manualSelected = IsLocked;
            manualId = IsLocked ? TargetId : null;
        }

        void Lock(in LockOnCandidate c)
        {
            IsLocked = true;
            TargetId = c.Id;
            _lockedHostile = c.IsHostile;
            PointId = LockPointRules.DefaultPointId(c.Points);
            LastRelease = LockReleaseReason.None;
        }

        void Drop(LockReleaseReason reason)
        {
            IsLocked = false;
            TargetId = 0;
            PointId = NoPoint;
            LastRelease = reason;
        }

        /// <summary>direction yönünde a, b'den ileri ise &gt; 0, geri ise &lt; 0 (bakış açısı, eşitte kimlik).</summary>
        static int Order(in LockOnCandidate a, in LockOnCandidate b, int direction)
        {
            int cmp = a.BearingDeg.CompareTo(b.BearingDeg);
            if (cmp == 0)
                cmp = a.Id.CompareTo(b.Id);
            return direction > 0 ? cmp : -cmp;
        }

        static bool Find(IReadOnlyList<LockOnCandidate> candidates, int id, out LockOnCandidate found)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Id != id)
                    continue;
                found = candidates[i];
                return true;
            }
            found = default;
            return false;
        }
    }
}
