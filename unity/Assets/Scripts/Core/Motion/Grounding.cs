using System;

namespace Dovus.Core.Motion
{
    /// <summary>Yere basma ölçüsü. Play Sweep aynı eşikleri okur.</summary>
    public readonly struct GroundSample
    {
        public GroundSample(float rootY, float visualOffsetY, bool airborne, bool landing)
        {
            RootY = rootY;
            VisualOffsetY = visualOffsetY;
            Airborne = airborne;
            Landing = landing;
        }

        public float RootY { get; }
        /// <summary>Kalan görsel ofset. Çözücü bunu her örnekte sıfırlar; cast'ler arası birikmez.</summary>
        public float VisualOffsetY { get; }
        public bool Airborne { get; }
        public bool Landing { get; }
    }

    /// <summary>
    /// Kök yüksekliği. Zemin bir kez çakılır; havadaki eğri onun üstüne biner.
    /// Eğri bitince ya da cast iptal olunca kısa smoothstep ile zemine iner. Zeminin altına geçmez,
    /// iptal anında zıplamaz (u=0 hâlâ mevcut yükseklik).
    /// </summary>
    public sealed class Grounding
    {
        public const float LandSec = 0.18f;
        public const float LiveSlackM = 0.05f;
        public const float SettleSlackM = 0.03f;
        public const float SettleAfterSec = 0.3f;

        float _groundY;
        float _y;
        float _landFrom;
        float _landT;
        bool _holding;
        bool _landing;

        public float GroundY => _groundY;
        public float RootY => _y;
        public bool Holding => _holding;
        public bool Landing => _landing;
        public bool OnGround => !_holding && !_landing;

        public void Plant(float rootY)
        {
            _groundY = rootY;
            _y = rootY;
            _holding = false;
            _landing = false;
            _landT = 0f;
        }

        /// <summary>Havadaki fazın kökü. Eğri değeri olduğu gibi yazılır.</summary>
        public void Follow(float rootY)
        {
            _holding = true;
            _landing = false;
            _landT = 0f;
            _y = rootY;
        }

        /// <summary>Hava bitti ya da cast iptal. Zemindeyse olduğu yerde kalır.</summary>
        public void Release()
        {
            if (_landing)
                return;
            _holding = false;
            if (MathF.Abs(_y - _groundY) <= MotionDefaults.GroundSnapEpsilonM)
            {
                _y = _groundY;
                return;
            }
            _landFrom = _y;
            _landT = 0f;
            _landing = true;
        }

        public void Cancel() => Release();

        public void Snap()
        {
            _holding = false;
            _landing = false;
            _landT = 0f;
            _y = _groundY;
        }

        /// <summary>
        /// Havada kalan faz eğriyi yazar. Yatay faz (geri tepme dahil) o karede zemine yapışır;
        /// yumuşak iniş yalnız eğrisi hâlâ yerden yüksek olan faza kalır. Skill kimliği aranmaz.
        /// </summary>
        public void ApplyMotion(bool airborne, float rootY)
        {
            if (airborne)
            {
                Follow(rootY);
                return;
            }

            if (MathF.Abs(rootY - _groundY) > MotionDefaults.GroundReleaseDeltaM)
            {
                Follow(rootY);
                Release();
                return;
            }

            Snap();
        }

        public void Tick(float dt)
        {
            if (!_landing)
                return;
            _landT += MathF.Max(0f, dt);
            float u = LandSec <= 0.0001f ? 1f : Math.Clamp(_landT / LandSec, 0f, 1f);
            float s = u * u * (MotionDefaults.SmoothStepThree - 2f * u);
            _y = _landFrom + (_groundY - _landFrom) * s;
            if (u >= 1f)
            {
                _y = _groundY;
                _landing = false;
            }
        }

        /// <summary>Ölçülen görsel ofseti atar. Kök eğriyi taşır; ofset cast'ler arasında birikmez.</summary>
        public GroundSample Sample(float visualLeftover)
        {
            // Ölçülen ofset kök eğrisine eklenmez; her örnekte sıfırlanır.
            _ = visualLeftover;
            return new GroundSample(_y, 0f, _holding, _landing);
        }

        public static bool WithinSlack(float feetY, float groundY, bool settled) =>
            MathF.Abs(feetY - groundY) <= (settled ? SettleSlackM : LiveSlackM);

        /// <summary>
        /// Ayak hatasını görselin local Y kaymasına çevirir.
        /// Görselin kendi localScale'i kendi konumunu ölçeklemez; dünya kayması yalnız
        /// ebeveynin Y ölçeğidir. Ebeveyn ölçeği yerine görselin lossyScale'i kullanılırsa
        /// (boss kapsülü 1,3 ve görsel fit'i büyük) düzeltme eksik kalır, ayak ~5 cm havada durur.
        /// </summary>
        public static float LocalOffsetForWorldError(float worldError, float parentLossyScaleY)
        {
            float scale = MathF.Abs(parentLossyScaleY) < 0.0001f ? 1f : parentLossyScaleY;
            return worldError / scale;
        }
    }
}
