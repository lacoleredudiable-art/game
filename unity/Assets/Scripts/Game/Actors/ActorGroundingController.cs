using Dovus.Core.Motion;
using Dovus.Game.Platform;
using UnityEngine;
using Dovus.Core.Shared;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// Kök Y ve görsel çocuk ofseti. Zemin yüksekliği bir kez çakılır.
    /// Havada değilken kök zemine oturur, görsel local Y spawn değerine döner
    /// (cast'ler arasında birikmez), sonra en alçak ayak o zemine çekilir.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class ActorGroundingController : MonoBehaviour
    {
        readonly Grounding _logic = new();
        GameClockHost _clock;
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

        /// <summary>
        /// Silah yürüme seti değişince ayak tabanı yeniden örneklenir; eski silahın
        /// <see cref="_footGroundY"/> değeri yeni Loko kliplerinde gövdeyi havaya çekerdi.
        /// </summary>
        public void ResetFootPlant()
        {
            _footReady = false;
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

        public void BindClock(GameClockHost clock) => _clock = clock;

        void LateUpdate()
        {
            if (!_claimed)
                _logic.Release();
            _claimed = false;
            float dt = _clock != null ? (float)(_clock.WorldDeltaMs / Units.SecToMs) : Time.deltaTime;
            _logic.Tick(dt);
            ApplyRoot();
            ResetVisual();
            Animator anim = VisualAnimator();
            bool dodgeOwns = DodgeClipPlayback.ClipOwnsFeet(anim);
            // Loko_Yay_Run kalçayı kök XZ'den metrelerce öne kaçırır; kamera kökü takip eder →
            // mesh uçar, sonra geri düşer (ping). Kalçayı köke çek.
            if (_logic.OnGround && !dodgeOwns)
                LockHipsXZ(anim);
            // Koşu Loko_*_Run ayak eğrisini kendi taşır; kilitlemek görseli her kare savurur.
            if (_logic.OnGround && FootLock && !SkipFootLock && !dodgeOwns && !LocoRunOwnsFeet(anim))
                LockFeet();
        }

        /// <summary>Kalça dünya XZ'sini köke çeker (görsel local XZ). Y'ye dokunmaz.</summary>
        void LockHipsXZ(Animator anim)
        {
            if (_visual == null || anim == null || !anim.isHuman)
                return;
            Transform hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null)
                return;
            Vector3 err = hips.position - transform.position;
            err.y = 0f;
            if (err.sqrMagnitude < ActorGroundingControllerDefaults.HipDriftCorrectMinM
                * ActorGroundingControllerDefaults.HipDriftCorrectMinM)
                return;
            Transform parent = _visual.parent;
            Vector3 localErr = parent != null ? parent.InverseTransformVector(err) : err;
            localErr.y = 0f;
            Vector3 lp = _visual.localPosition;
            lp.x -= localErr.x;
            lp.z -= localErr.z;
            _visual.localPosition = lp;
        }

        const float LocoRunWeight = 0.5f;

        static bool LocoRunOwnsFeet(Animator anim)
        {
            if (anim == null || !anim.isActiveAndEnabled || anim.runtimeAnimatorController == null)
                return false;
            foreach (AnimatorClipInfo ci in anim.GetCurrentAnimatorClipInfo(0))
            {
                if (ci.weight > LocoRunWeight && ci.clip != null
                    && ci.clip.name.EndsWith("_Run", System.StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        Animator _animator;

        Animator VisualAnimator()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            return _animator;
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
            // Y her kare plant'a; XZ'yi LockHipsXZ yazar — burada sıfırlamak kalça kilidini bozar.
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
            // Yay Loko ayak salınımı sınırsız düzeltilince görsel her kare uçuyor / duvara giriyordu (O-1.23).
            float step = Mathf.Clamp(error, -ActorGroundingControllerDefaults.MaxFootLockStepM,
                ActorGroundingControllerDefaults.MaxFootLockStepM);
            Transform parent = _visual.parent;
            float parentScale = parent != null ? parent.lossyScale.y : 1f;
            Vector3 lp = _visual.localPosition;
            lp.y -= Grounding.LocalOffsetForWorldError(step, parentScale);
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
