using Dovus.Core.Motion;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>
    /// v4 Hareket komutlarının (KendiniTasi, dash, It) süpürmesi (O-1.13, O-4.9).
    /// Süpüren kendi kapsülüyle taranır: oyuncu motorun gövde kapsülü (ayak tabanı – 1,8 m);
    /// diğerleri dünya fiziği yarıçapı ile kendi çarpışıcısının boyu (boss: <c>body_radius_boss_m</c>).
    /// Yol bir canlıya ya da yapıya değince gövde kenarları arasında kenar payı kala durur.
    /// Canlılar ve koza tetik çarpışıcılıdır, dünya fiziği yarıçaplı dikey silindir sayılır;
    /// katı çarpışıcılar (sütun, duvar, sabit yapı) kendi geometrileriyle süpürülür.
    /// </summary>
    public sealed class RuleEngineV4MotionSweep
    {
        readonly RaycastHit[] _hits = new RaycastHit[32];
        readonly Collider[] _near = new Collider[64];

        readonly struct Capsule
        {
            /// <summary>Uç kürelerin merkezleri, kökün Y'sine göre.</summary>
            public readonly float BottomY;
            public readonly float TopY;
            public readonly float Radius;

            public Capsule(float bottomY, float topY, float radius)
            {
                BottomY = bottomY;
                TopY = Mathf.Max(bottomY, topY);
                Radius = radius;
            }

            public float SoleY => BottomY - Radius;
            public float HeadY => TopY + Radius;
            public float MidY => (BottomY + TopY) * 0.5f;
        }

        /// <summary>Düz çizgide gidilebilecek mesafe; dir yatay ve birim.</summary>
        public float ClearDistance(Vector3 from, Vector3 dir, float meters, float radiusM, Transform self) =>
            Stop(from, dir, meters, radiusM, self, out _);

        /// <summary>Durduğu yerden sonra kalan yol, değdiği yüzey boyunca kayar (kayma ayrıca süpürülmez).</summary>
        public Vector3 SweepAndSlide(Vector3 from, Vector3 to, float radiusM, Transform self)
        {
            Vector3 delta = to - from;
            delta.y = 0;
            float dist = delta.magnitude;
            if (dist < PositionOwnershipDefaults.MinDistM)
                return from;
            Vector3 dir = delta / dist;
            float clear = Stop(from, dir, dist, radiusM, self, out Vector3 normal);
            if (clear >= dist)
                return to;
            Vector3 stop = from + dir * clear;
            normal.y = 0;
            float remain = dist - clear;
            if (normal.sqrMagnitude < PositionOwnershipDefaults.MinDistM * PositionOwnershipDefaults.MinDistM
                || remain <= PositionOwnershipDefaults.MinDistM)
                return stop;
            return stop + Vector3.ProjectOnPlane(dir * remain, normal.normalized);
        }

        /// <summary>O-4.9 kenar payı: süpürenin dodge ayarı; dodge'u olmayan gövdede aynı belgelenmiş değer.</summary>
        public static float EdgeGapM(Transform self) =>
            self != null && self.TryGetComponent(out DodgeMotionController dodge)
                ? dodge.EdgeGapM
                : DodgeMotionControllerDefaults.FallbackEdgeGapM;

        float Stop(Vector3 from, Vector3 dir, float meters, float radiusM, Transform self, out Vector3 normal)
        {
            normal = Vector3.zero;
            if (meters <= 0)
                return 0;
            if (!TryCapsule(self, radiusM, out Capsule cap))
                return 0;
            float gap = EdgeGapM(self);
            float best = meters;
            SolidStop(from, dir, cap, 0, self, ref best, ref normal, KinematicMotorControllerDefaults.MoveStopInsetM);
            SolidStop(from, dir, cap, gap, self, ref best, ref normal, 0);
            BodyStop(from, dir, cap, gap, self, ref best, ref normal);
            return Mathf.Max(0, best);
        }

        static bool TryCapsule(Transform self, float radiusM, out Capsule cap)
        {
            cap = default;
            if (self == null)
            {
                Debug.LogError("[V4-FALLBACK] RuleEngineV4MotionSweep: süpüren gövde yok");
                return false;
            }
            if (self.TryGetComponent(out KinematicMotorController motor))
            {
                motor.BodyCapsule(Vector3.zero, out Vector3 bottom, out Vector3 top, out float r);
                cap = new Capsule(bottom.y, top.y, r);
                return true;
            }
            if (!self.TryGetComponent(out Collider col))
            {
                Debug.LogError($"[V4-FALLBACK] RuleEngineV4MotionSweep: {self.name} gövde kapsülü yok (motor ve çarpışıcı yok)");
                return false;
            }
            Bounds b = col.bounds;
            float radius = Mathf.Max(KinematicMotorControllerDefaults.CapsuleRadiusFloorM, radiusM);
            float y = self.position.y;
            cap = new Capsule(b.min.y - y + radius, b.max.y - y - radius, radius);
            if (cap.TopY <= cap.BottomY)
                cap = new Capsule(cap.MidY, cap.MidY, radius);
            return true;
        }

        /// <summary>
        /// Katı çarpışıcı: kapsül <paramref name="inflate"/> kadar yanlara şişirilir, dikey uçları aynı kalır.
        /// Başlangıçta değen çarpışıcı (zemin, şişirilmiş payın içindeki duvar) atlanır; şişirilmemiş tarama yine durdurur.
        /// </summary>
        void SolidStop(
            Vector3 from, Vector3 dir, in Capsule cap, float inflate, Transform self,
            ref float best, ref Vector3 normal, float inset)
        {
            float r = cap.Radius + inflate;
            float bottom = Mathf.Min(cap.BottomY + inflate, cap.MidY);
            float top = Mathf.Max(cap.TopY - inflate, cap.MidY);
            Vector3 p1 = from + Vector3.up * bottom;
            Vector3 p2 = from + Vector3.up * top;
            int n = Physics.CapsuleCastNonAlloc(p1, p2, r, dir, _hits, best + inset, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = _hits[i];
                if (h.collider == null || IsSelf(h.collider.transform, self))
                    continue;
                if (h.distance <= 0 && h.point == Vector3.zero)
                    continue;
                float at = Mathf.Max(0, h.distance - inset);
                if (at < best)
                {
                    best = at;
                    normal = h.normal;
                }
            }
        }

        /// <summary>
        /// Canlı ve koza: dünya fiziği yarıçaplı dikey silindir. Merkezler arası
        /// süpüren + gövde + pay olduğu anda durulur. Zaten o mesafenin içindeyse yalnız uzaklaşmaya izin var.
        /// </summary>
        void BodyStop(
            Vector3 from, Vector3 dir, in Capsule cap, float gap, Transform self,
            ref float best, ref Vector3 normal)
        {
            float reach = cap.Radius + gap + LargestBodyM();
            Vector3 a = from + Vector3.up * cap.MidY;
            Vector3 b = a + dir * best;
            int n = Physics.OverlapCapsuleNonAlloc(a, b, reach, _near, ~0, QueryTriggerInteraction.Collide);
            float sole = from.y + cap.SoleY;
            float head = from.y + cap.HeadY;
            for (int i = 0; i < n; i++)
            {
                Collider col = _near[i];
                if (col == null || !col.isTrigger)
                    continue;
                RuleEngineV4PhysicsBodyHost body = col.GetComponentInParent<RuleEngineV4PhysicsBodyHost>();
                if (body == null || body.BodyRadiusM <= 0 || IsSelf(body.transform, self))
                    continue;
                if (body.TryGetComponent(out TargetableHost mark) && !mark.IsAvailable)
                    continue;
                Bounds bb = col.bounds;
                if (bb.max.y <= sole || bb.min.y >= head)
                    continue;
                Vector3 c = body.transform.position;
                float at = CircleEntry(from, dir, best, c, cap.Radius + body.BodyRadiusM + gap);
                if (at < best)
                {
                    best = at;
                    normal = from + dir * at - c;
                }
            }
        }

        /// <summary>Yatay doğrultuda merkezler arası <paramref name="separation"/>'a inilen ilk mesafe; yoksa <paramref name="meters"/>.</summary>
        internal static float CircleEntry(Vector3 from, Vector3 dir, float meters, Vector3 center, float separation)
        {
            float sx = from.x - center.x;
            float sz = from.z - center.z;
            float b = sx * dir.x + sz * dir.z;
            float c = sx * sx + sz * sz - separation * separation;
            if (c <= 0)
                return b < 0 ? 0 : meters;
            if (b >= 0)
                return meters;
            float disc = b * b - c;
            if (disc < 0)
                return meters;
            float t = -b - Mathf.Sqrt(disc);
            return t < meters ? t : meters;
        }

        static bool IsSelf(Transform t, Transform self) =>
            self != null && (t == self || t.IsChildOf(self));

        /// <summary>Tarama yarıçapı: dünya fiziğindeki en büyük gövde (boss).</summary>
        static float LargestBodyM()
        {
            RuleEngineV4WorldPhysics wp = RuleEngineV4WorldPhysicsRuntime.Active;
            return Mathf.Max(
                Mathf.Max(RuleEngineV4BossBody.RadiusM("RuleEngineV4MotionSweep"), wp.BodyRadiusStructureM),
                Mathf.Max(wp.BodyRadiusCreatureM, wp.BodyRadiusPlayerM));
        }
    }
}
