using Dovus.Core.Combat;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// SkillMotionPlan → oyuncu transform. DodgeMotion'dan bağımsız; dünya saatiyle lerp.
    /// </summary>
    [DefaultExecutionOrder(11)]
    public sealed class SkillMotionDriver : MonoBehaviour
    {
        GameClock _clock;
        PrototypeTuning _proto;
        AfterimageTrail _afterimage;
        ActorVisual _visual;
        ActorStatus _status;
        PlayerVitals _vitals;

        bool _active;
        Vector3 _from;
        Vector3 _to;
        Vector3 _face;
        double _startMs;
        double _endMs;
        float _lastEmit;

        public bool IsDisplacing => _active;

        public void Bind(GameClock clock, PrototypeTuning proto)
        {
            _clock = clock;
            _proto = proto;
            _afterimage = GetComponent<AfterimageTrail>();
            _visual = GetComponent<ActorVisual>();
            _status = GetComponent<ActorStatus>();
            _vitals = GetComponent<PlayerVitals>();
        }

        public void Play(in SkillMotionPlan plan, double worldMs)
        {
            if (plan.IsEmpty || plan.Kind == SkillMotionKind.PlaceMark)
                return;
            if (_vitals != null && _vitals.IsDown)
                return;

            _from = transform.position;
            _to = new Vector3(plan.DestX, _from.y, plan.DestZ);
            _face = new Vector3(plan.FaceX, 0f, plan.FaceZ);
            if (_face.sqrMagnitude < 0.0001f)
                _face = transform.forward;

            float dur = Mathf.Max(0.01f, plan.DurationSec);
            _startMs = worldMs;
            _endMs = worldMs + dur * 1000.0;
            _lastEmit = -1f;
            _active = true;
            _afterimage?.Clear();

            if (plan.IframeMs > 0 && _status != null)
                _status.Board.Apply(Dovus.Core.Status.StatusKind.Stasis, plan.IframeMs, 1f);

            if (plan.Kind == SkillMotionKind.ZenitsuPass)
                _visual?.Trigger(ActorVisual.TriggerDodge);
            else
                _visual?.Trigger(ActorVisual.TriggerDodge);
        }

        public void WarpInstant(float x, float z)
        {
            Vector3 p = transform.position;
            p.x = x;
            p.z = z;
            float half = _proto != null ? _proto.ArenaHalfSizeM : 12f;
            p.x = Mathf.Clamp(p.x, -half, half);
            p.z = Mathf.Clamp(p.z, -half, half);
            transform.position = p;
            _active = false;
        }

        void Update()
        {
            if (!_active || _clock == null)
                return;

            if (_vitals != null && _vitals.IsDown)
            {
                _active = false;
                return;
            }

            double worldMs = _clock.Director.WorldTimeMs;
            float t = _endMs <= _startMs
                ? 1f
                : Mathf.Clamp01((float)((worldMs - _startMs) / (_endMs - _startMs)));

            // Ease-out: hızlı gidiş, yumuşak varış.
            float eased = 1f - (1f - t) * (1f - t);
            Vector3 pos = Vector3.Lerp(_from, _to, eased);
            float half = _proto != null ? _proto.ArenaHalfSizeM : 12f;
            pos.x = Mathf.Clamp(pos.x, -half, half);
            pos.z = Mathf.Clamp(pos.z, -half, half);
            transform.position = pos;

            if (_face.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(_face, Vector3.up);

            MaybeEmit(eased);

            if (t >= 1f)
                _active = false;
        }

        void MaybeEmit(float ratio)
        {
            if (_afterimage == null)
                return;
            int count = Mathf.Max(1, _afterimage.Count);
            float step = 1f / count;
            if (ratio + 0.001f < _lastEmit + step && _lastEmit >= 0f)
                return;
            _lastEmit = ratio;
            _afterimage.Emit(transform.position, transform.rotation, transform.localScale);
        }
    }
}
