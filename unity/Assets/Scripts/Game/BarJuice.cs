using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Can barı juice'u: gecikmeli (ghost) hasar izi, hasarda beyaz / iyileşmede yeşil flaş.
    /// Oran değişimini kendisi algılar; çağıran yalnız güncel oranı verir.
    /// </summary>
    public sealed class BarJuice
    {
        readonly Image _ghost;
        readonly Image _flash;
        float _last = -1f;
        float _ghostRatio;
        float _holdUntil;
        float _flashAt = -10f;
        bool _healFlash;

        public BarJuice(Image ghost, Image flash)
        {
            _ghost = ghost;
            _flash = flash;
        }

        public float GhostRatio => _ghostRatio;

        public void Tick(float ratio, HudTheme theme)
        {
            float now = Time.unscaledTime;
            if (_last < 0f)
            {
                _last = ratio;
                _ghostRatio = ratio;
            }

            if (ratio < _last - 1e-4f)
            {
                _holdUntil = now + theme.GhostHoldSec;
                _flashAt = now;
                _healFlash = false;
            }
            else if (ratio > _last + 1e-4f)
            {
                _flashAt = now;
                _healFlash = true;
            }

            if (now >= _holdUntil)
                _ghostRatio = Mathf.MoveTowards(_ghostRatio, ratio, theme.GhostDrainPerSec * Time.unscaledDeltaTime);
            if (_ghostRatio < ratio)
                _ghostRatio = ratio;
            _last = ratio;

            if (_ghost != null)
            {
                _ghost.fillAmount = _ghostRatio;
                _ghost.color = theme.GhostColor;
            }

            if (_flash != null)
            {
                float k = 1f - Mathf.Clamp01((now - _flashAt) / Mathf.Max(0.01f, theme.FlashSec));
                Color c = _healFlash ? theme.HealColor : theme.FlashColor;
                c.a = k * (_healFlash ? theme.HealFlashAlpha : theme.FlashAlpha);
                _flash.color = c;
                _flash.enabled = k > 0f;
            }
        }
    }
}
