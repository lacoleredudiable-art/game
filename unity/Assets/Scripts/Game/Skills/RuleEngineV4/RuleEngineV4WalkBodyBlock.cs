using Dovus.Core.Motion;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>
    /// Yürüme boss'un ve dostların gövdesine girmez (O-4.9, O-6.10, O-1.12). Gövdeler tetik çarpışıcılıdır;
    /// dünya fiziği yarıçaplı dikey silindir sayılır (boss <see cref="RuleEngineV4BossBody.RadiusM"/>,
    /// dost <c>world_physics.body_radius_player_m</c>). Merkezler arası mesafe yürüyen + gövde yarıçapının altına
    /// inmez; değince kalan yol gövdenin teğeti boyunca kayar. Zaten o mesafenin içindeyse yalnız uzaklaşmaya izin var.
    /// </summary>
    public sealed class RuleEngineV4WalkBodyBlock
    {
        const string Where = "KinematicMotorController.yürüme";

        readonly Collider[] _near = new Collider[32];

        /// <param name="bottom">Yürüyenin gövde kapsülünün alt uç küresi, varılacak yerde.</param>
        /// <param name="top">Üst uç küresi, varılacak yerde.</param>
        public Vector3 Resolve(Vector3 from, Vector3 to, Vector3 bottom, Vector3 top, float radiusM, Transform self)
        {
            float bossR = RuleEngineV4BossBody.RadiusM(Where);
            float allyR = RuleEngineV4WorldPhysicsRuntime.Active.BodyRadiusPlayerM;
            float step = Flat(to - from).magnitude;
            float reach = radiusM + step + Mathf.Max(bossR, allyR);
            int n = Physics.OverlapCapsuleNonAlloc(bottom, top, reach, _near, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                Collider col = _near[i];
                if (col == null)
                    continue;
                RuleEngineV4PhysicsBodyHost body = col.GetComponentInParent<RuleEngineV4PhysicsBodyHost>();
                if (body == null || (self != null && (body.transform == self || body.transform.IsChildOf(self))))
                    continue;
                if (body.TryGetComponent(out TargetableHost mark) && !mark.IsAvailable)
                    continue;
                float bodyR;
                if (body.WeightTier == RuleEngineV4WeightTier.Boss)
                    bodyR = bossR;
                else if (body.TryGetComponent(out AllyDummyController _))
                    bodyR = allyR;
                else
                    continue;
                to = KeepOut(from, to, body.transform.position, radiusM + bodyR);
            }
            return to;
        }

        /// <summary>Düz yol gövdeye girdiği yerde durur, kalan yol teğete izdüşer; son konum gövde dışına itilmez, yalnız kenarda tutulur.</summary>
        static Vector3 KeepOut(Vector3 from, Vector3 to, Vector3 center, float separation)
        {
            float floor = Mathf.Min(separation, Flat(from - center).magnitude);
            Vector3 delta = Flat(to - from);
            float len = delta.magnitude;
            if (len >= PositionOwnershipDefaults.MinDistM)
            {
                Vector3 dir = delta / len;
                float t = RuleEngineV4MotionSweep.CircleEntry(from, dir, len, center, floor);
                if (t < len)
                {
                    Vector3 hit = from + dir * t;
                    Vector3 normal = Flat(hit - center);
                    Vector3 slide = normal.sqrMagnitude >= PositionOwnershipDefaults.MinDistM * PositionOwnershipDefaults.MinDistM
                        ? Vector3.ProjectOnPlane(dir * (len - t), normal.normalized)
                        : Vector3.zero;
                    to = new Vector3(hit.x + slide.x, to.y, hit.z + slide.z);
                }
            }
            Vector3 off = Flat(to - center);
            float d = off.magnitude;
            if (d >= floor || d < PositionOwnershipDefaults.MinDistM)
                return to;
            off *= floor / d;
            return new Vector3(center.x + off.x, to.y, center.z + off.z);
        }

        static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
