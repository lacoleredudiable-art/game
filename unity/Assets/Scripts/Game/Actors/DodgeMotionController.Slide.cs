using Dovus.Game.Diagnostics;
using Dovus.Core.Dodge;
using Dovus.Game.Boss;
using UnityEngine;
using Dovus.Core.Shared;

namespace Dovus.Game.Actors
{
    /// <summary>O-4.9: dodge engele çarpınca yüzey boyunca kayar (boss gövdesi, duvar, sütun, koza).</summary>
    public sealed partial class DodgeMotionController
    {
        // ---- O-4.9: engelde yüzey boyunca kayma ----

        void BeginSlideOnHit(int worldMs, float distanceM, float ratio, Vector3 at, Vector3 normal)
        {
            // R = çarpma anında kalan yol (toplam yoldan kat edilen düşülür; süzülme payı sayılmaz).
            Vector3 moved = at - _startPos;
            float traveled = moved.x * _dir.x + moved.z * _dir.z;
            float remaining = Mathf.Max(0f, distanceM * (1f - _rollRatioBase) - traveled);
            if (traveled < DodgeMotionControllerDefaults.NoTravelRefundM)
            {
                // Hiç yol alamadan çarptı: hak geri verilir (i-frame sürer).
                _inputCtl?.Charges?.Refund(1f);
                DebugConfig.DevLog($"[DODGE] engelde-hak-iade yol={traveled:F3}m");
            }

            bool started = _slide.Begin(
                _dir.x, _dir.z, normal.x, normal.z, at.x, at.z, remaining, _tuning, PointFull);
            // Başlamasa da serbest yola dönülmez: yol engelde bitti (yol alındıysa hak yanar, i-frame sürer).
            _slideMode = true;
            _simMs = worldMs;
            _pathM = distanceM * (ratio - _rollRatioBase);
            // Motor engelde kısa bir payla (MoveStopInsetM) durdurur; kayma EdgeGapM payıyla başlar.
            if (started && SlideContact(at.x, at.z, DodgeDefaults.SlideProbeM, out float cnx, out float cnz, out float cx, out float cz))
            {
                transform.position = new Vector3(cx, transform.position.y, cz);
                at = transform.position;
                _slide.Rebase(cnx, cnz);
            }

            if (started)
                SlideStarted?.Invoke(at, new Vector3(_slide.Side * normal.z, 0f, -_slide.Side * normal.x));
        }

        void StepSlide(int worldMs, float distanceM)
        {
            if (!_slide.Active)
                return;
            const int StepMs = DodgeDefaults.SlideStepMs;
            Vector3 pos = transform.position;
            float x = pos.x;
            float z = pos.z;
            float glideSpeed = GlideSpeedMps();
            int guard = 0;
            // Sabit adım: sonuç kare hızından bağımsız (T ölçütü).
            while (_slide.Active && _simMs + StepMs <= worldMs && guard++ < 64)
            {
                int next = _simMs + StepMs;
                float curve = distanceM * (_dodge.GetDisplacementRatio(next) - _rollRatioBase);
                float glide = glideSpeed * _dodge.GetGlideVelocityRatio(next) * (StepMs / (float)Units.SecToMs);
                float stepM = Mathf.Max(0f, curve - _pathM) + glide;
                _pathM = curve;
                _simMs = next;
                _slide.Advance(stepM, ref x, ref z, _tuning, SlideMove, SlideContact);
            }

            transform.position = new Vector3(x, pos.y, z);
        }

        bool SlideMove(float fx, float fz, float tx, float tz, out float rx, out float rz, out float nx, out float nz)
        {
            float y = transform.position.y;
            var from = new Vector3(fx, y, fz);
            var to = new Vector3(tx, y, tz);
            Vector3 normal = Vector3.zero;
            bool hit = false;

            Vector3 p = ClampArena(to);
            if (PlanarDist2(p, to) > 1e-8f)
            {
                hit = true;
                normal = ArenaNormal(p);
            }

            if (_boss != null)
            {
                float x = p.x;
                float z = p.z;
                DodgeEdge.KeepOutside(ref x, ref z, _boss.position.x, _boss.position.z, BossSeparation(), tx - fx, tz - fz);
                if (PlanarDist2(p, new Vector3(x, p.y, z)) > 1e-8f)
                {
                    hit = true;
                    p.x = x;
                    p.z = z;
                    normal = BossNormal(p);
                }
            }

            if (_motor != null)
            {
                Vector3 q = _motor.SweepStop(from, p, out Vector3 motorNormal, out bool motorClipped);
                if (motorClipped)
                {
                    hit = true;
                    normal = motorNormal;
                }

                p = q;
            }

            rx = p.x;
            rz = p.z;
            nx = normal.x;
            nz = normal.z;
            return hit && normal.sqrMagnitude > 0.0001f;
        }

        bool SlideContact(float x, float z, float reachM, out float nx, out float nz, out float cx, out float cz)
        {
            nx = nz = 0f;
            cx = x;
            cz = z;
            if (_boss != null)
            {
                float sep = BossSeparation();
                float dx = x - _boss.position.x;
                float dz = z - _boss.position.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d > 0.0001f && d < sep + reachM)
                {
                    nx = dx / d;
                    nz = dz / d;
                    cx = _boss.position.x + nx * sep;
                    cz = _boss.position.z + nz * sep;
                    return true;
                }
            }

            if (_motor != null
                && _motor.ProbeContact(new Vector3(x, transform.position.y, z), EdgeGapM, reachM, out Vector3 n, out Vector3 c))
            {
                nx = n.x;
                nz = n.z;
                cx = c.x;
                cz = c.z;
                return true;
            }

            return false;
        }

        /// <summary>Taraf seçiminde nokta dolu mu: engel gövdesi, boss gövdesi ya da aktif alan telegraph'ı.</summary>
        bool PointFull(float x, float z)
        {
            if (_boss != null)
            {
                float dx = x - _boss.position.x;
                float dz = z - _boss.position.z;
                float sep = BossSeparation();
                if (dx * dx + dz * dz < sep * sep)
                    return true;
                if (_telegraphOwner != _boss || _telegraphCache == null)
                {
                    _telegraphOwner = _boss;
                    _telegraphCache = _boss.GetComponent<BossTelegraphView>();
                }

                if (_telegraphCache != null && _telegraphCache.CoversPoint(x, z))
                    return true;
            }

            return _motor != null && _motor.PointBlocked(new Vector3(x, transform.position.y, z));
        }

        float BossSeparation()
        {
            float body = _motor != null ? _motor.BodyRadiusM : 0.5f;
            float bossR = CombatFallbacks.BossBodyRadiusFallbackM;
            if (_reactorOwner != _boss || _reactorCache == null)
            {
                _reactorOwner = _boss;
                _reactorCache = _boss.GetComponent<BossReactorController>();
            }

            if (_reactorCache != null && _reactorCache.BodyRadiusM > DodgeMotionControllerDefaults.MinBodyRadiusM)
                bossR = _reactorCache.BodyRadiusM;
            return body + bossR + EdgeGapM;
        }
    }
}
