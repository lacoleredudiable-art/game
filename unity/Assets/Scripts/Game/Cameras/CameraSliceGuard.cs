using UnityEngine;

namespace Dovus.Game.Cameras
{
    /// <summary>
    /// Dilimde boss çevresi kamera kuralları (<see cref="CameraSliceRules"/>): dönüş tavanı (O-5.6), boss'a yakın yaw
    /// dondurma (O-5.7), omuz değişimi aralığı (O-5.9), boss arada kalınca saydamlık (O-5.11). Bağlanmamışsa
    /// (dilim dışı) <see cref="FollowCameraController"/> eski davranışla kalır.
    /// </summary>
    public sealed class CameraSliceGuard
    {
        readonly CameraSliceRules _rules;
        readonly CameraBossBody _body;
        readonly CameraBossFade _fade;
        float _heldOffsetDeg;
        float _lastOffsetDeg;
        bool _hasOffset;
        float _lastShoulderSwitchSec = float.NegativeInfinity;

        public CameraSliceGuard(CameraSliceRules rules, CameraBossBody body, CameraBossFade fade)
        {
            _rules = rules;
            _body = body;
            _fade = fade;
        }

        public CameraSliceRules Rules => _rules;

        /// <summary>O-5.11: son karede boss gövdesi kamera ile karakter arasındaydı.</summary>
        public bool BossOccluded => _fade != null && _fade.IsFaded;

        /// <summary>
        /// O-5.7: yaw'ın boss'tan gelen payı (yumuşak kilit / kilit) gövde kenarına 2 m'den yakınken donar; 2–5 m
        /// arasında boss yönündeki değişimin yalnız (1 − ağırlık) kadarını izler; 5 m'den uzakta normal. Sebep: boss
        /// merkezine yakınken hedef yönü her adımda hızla döner. Elle döndürme (<paramref name="orbitYawDeg"/>) donmaz.
        /// </summary>
        public float HoldBossYaw(float orbitYawDeg, float desiredYawDeg, Transform bossTarget, Vector3 player)
        {
            float live = Mathf.DeltaAngle(orbitYawDeg, desiredYawDeg);
            float hold = BossYawHold01(bossTarget, player);
            if (hold <= 0f || !_hasOffset)
                _heldOffsetDeg = live;
            else
                _heldOffsetDeg = Mathf.DeltaAngle(0f, _heldOffsetDeg + (1f - hold) * Mathf.DeltaAngle(_lastOffsetDeg, live));
            _lastOffsetDeg = live;
            _hasOffset = true;
            return orbitYawDeg + _heldOffsetDeg;
        }

        /// <summary>0 = normal takip (kenardan 5 m ve ötesi), 1 = donmuş (2 m ve berisi); arası yumuşak.</summary>
        float BossYawHold01(Transform bossTarget, Vector3 player)
        {
            if (_body == null || bossTarget == null || !_body.Is(bossTarget))
                return 0f;
            float t = Mathf.InverseLerp(_rules.BossYawFreeEdgeM, _rules.BossYawFreezeEdgeM, _body.EdgeDistanceM(player));
            return Mathf.SmoothStep(0f, 1f, t);
        }

        /// <summary>O-5.6: kameranın yörüngedeki yaw'ı da saniyede tavandan hızlı dönmez.</summary>
        public float LimitYaw(float currentDeg, float nextDeg, float dt) =>
            Mathf.MoveTowardsAngle(currentDeg, nextDeg, _rules.TurnRateMaxDps * dt);

        /// <summary>
        /// O-5.6: kamera bir karede tavan × kare süresinden fazla dönemez (yaw, eğim, yuvarlanma birlikte). Pay, Gözcü'nün
        /// aynı karede ölçtüğü açının float yuvarlamasıyla eşiği geçmemesi için.
        /// </summary>
        public Quaternion LimitTurn(Quaternion current, Quaternion wanted, float dt) =>
            Quaternion.RotateTowards(current, wanted,
                _rules.TurnRateMaxDps * FollowCameraControllerDefaults.TurnRateFloatMargin * dt);

        /// <summary>O-5.9: kilitliyken omuz en çok <see cref="CameraSliceRules.ShoulderSwitchMinSec"/>'de bir değişir.</summary>
        public float HoldShoulder(float currentSign, float pick, float nowSec)
        {
            if (Mathf.Abs(pick - currentSign) <= FollowCameraControllerDefaults.ShoulderSignEpsilon)
                return pick;
            if (nowSec - _lastShoulderSwitchSec < _rules.ShoulderSwitchMinSec)
                return currentSign;
            _lastShoulderSwitchSec = nowSec;
            return pick;
        }

        /// <summary>O-5.11: gövde kamera–karakter arasındaysa boss saydam, değilse opak.</summary>
        public void UpdateFade(Vector3 character, Vector3 camPos)
        {
            if (_fade != null && _body != null)
                _fade.Apply(_body.Occludes(character, camPos));
        }
    }
}
