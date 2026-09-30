using Dovus.Core.Motion;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Kök Y ve görsel çocuk ofseti. Zemin yüksekliği bir kez çakılır.
    /// Havada değilken kök zemine oturur, görsel local Y spawn değerine döner
    /// (cast'ler arasında birikmez), sonra en alçak ayak o zemine çekilir.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class ActorGrounding : MonoBehaviour
    {
        readonly Grounding _logic = new();
        GameClock _clock;
        Transform _visual;
        float _plantedVisualY;
        bool _visualReady;
        bool _claimed;
        bool _footReady;
        float _footGroundY;

        /// <summary>İnsanoid ayak kiliti. Boss çökmesinde kapatılır.</summary>
        public bool FootLock = true;

        public bool SkipFootLock { get; set; }

        public float PlantedRootY => _logic.GroundY;
        public float RootY => _logic.RootY;
        public float FootGroundY => _footGroundY;
        public bool HasFootGround => _footReady;
        public bool Airborne => _logic.Holding;
        public bool Landing => _logic.Landing;

        void Awake()
        {
            _logic.Plant(transform.position.y);
            CaptureVisual();
        }

        public void Follow(float rootY)
        {
            _logic.Follow(rootY);
            _claimed = true;
        }

        public void Release()
        {
            _logic.Release();
            _claimed = true;
        }

        /// <summary>Kalıp fazı: havadaysa eğri, yatay fazdaysa o karede zemin.</summary>
        public void ApplyMotion(bool airborne, float rootY)
        {
            _logic.ApplyMotion(airborne, rootY);
            _claimed = true;
        }

        /// <summary>Kaldırma bitti. İnişi yeniden başlatmadan zemine yapışır; ayak kilidi bu kare çalışır.</summary>
        public void LandNow()
        {
            _logic.Snap();
            _claimed = true;
        }

        /// <summary>Cast arası süpürme: inişi beklemeden zemine yapıştır.</summary>
        public void SnapPlanted()
        {
            _logic.Snap();
            _claimed = true;
            ApplyRoot();
            ResetVisual();
            if (FootLock && !SkipFootLock)
                LockFeet();
        }

        void LateUpdate()
        {
            if (!_claimed)
                _logic.Release();
            _claimed = false;
            if (_clock == null)
                _clock = FindAnyObjectByType<GameClock>();
            float dt = _clock != null ? (float)(_clock.WorldDeltaMs / 1000.0) : Time.deltaTime;
            _logic.Tick(dt);
            ApplyRoot();
            ResetVisual();
            if (_logic.OnGround && FootLock && !SkipFootLock)
                LockFeet();
        }

        void ApplyRoot()
        {
            Vector3 p = transform.position;
            p.y = _logic.RootY;
            transform.position = p;
        }

        void CaptureVisual()
        {
            _visual = transform.Find("Visual");
            if (_visual == null)
            {
                Animator anim = GetComponentInChildren<Animator>();
                if (anim != null && anim.transform != transform)
                    _visual = anim.transform;
            }
            if (_visual == null)
                return;
            _plantedVisualY = _visual.localPosition.y;
            _visualReady = true;
        }

        void ResetVisual()
        {
            if (!_visualReady)
                CaptureVisual();
            if (_visual == null)
                return;
            Vector3 lp = _visual.localPosition;
            if (Mathf.Abs(lp.y - _plantedVisualY) < 0.0001f)
                return;
            lp.y = _plantedVisualY;
            _visual.localPosition = lp;
        }

        void LockFeet()
        {
            float feet = MeasureFeet(transform);
            if (float.IsPositiveInfinity(feet))
                return;
            if (!_footReady)
            {
                _footGroundY = feet;
                _footReady = true;
                return;
            }
            if (_visual == null)
                return;
            float error = feet - _footGroundY;
            if (Mathf.Abs(error) < 0.001f)
                return;
            Transform parent = _visual.parent;
            float parentScale = parent != null ? parent.lossyScale.y : 1f;
            Vector3 lp = _visual.localPosition;
            lp.y -= Grounding.LocalOffsetForWorldError(error, parentScale);
            _visual.localPosition = lp;
        }

        /// <summary>En alçak ayak (parmak, yoksa bilek). Yoksa görsel tabanı.</summary>
        public static float MeasureFeet(Transform actor)
        {
            if (actor == null)
                return float.PositiveInfinity;
            Animator anim = actor.GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman)
            {
                float y = float.PositiveInfinity;
                y = LowerBone(y, anim, HumanBodyBones.LeftToes, HumanBodyBones.LeftFoot);
                y = LowerBone(y, anim, HumanBodyBones.RightToes, HumanBodyBones.RightFoot);
                if (y < float.PositiveInfinity)
                    return y;
            }

            float min = float.PositiveInfinity;
            Renderer[] rends = actor.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rends.Length; i++)
            {
                Renderer r = rends[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                    continue;
                min = Mathf.Min(min, r.bounds.min.y);
            }
            return min;
        }

        static float LowerBone(float y, Animator anim, HumanBodyBones prefer, HumanBodyBones fallback)
        {
            Transform bone = anim.GetBoneTransform(prefer) ?? anim.GetBoneTransform(fallback);
            if (bone == null)
                return y;
            return Mathf.Min(y, bone.position.y);
        }
    }
}
