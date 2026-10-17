using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// skill_anim kesin klibi (ör. Kilic_ATIL) oynarken kalıp faz klibi ve sunum darbeleri
    /// (PulseRune / PulseAnimationType) onu ezmesin: klibin
    /// Trail_On / Trail_Off / Impact olayları yalnız klip oynarken gelir. Kilit yalnız klip
    /// state'inden çıkılınca kalkar: cast koşucusu hareketi başlatırken EndMotionAnim çağırır.
    /// </summary>
    public sealed class SkillClipHold
    {
        int _stateHash;
        int _timedStateHash;
        int _frame;
        float _releaseAtUnscaled = -1f;

        /// <param name="maxHoldSec">Düz vuruş: yalnız prefire süresince tut; 0 = klip state'inden çıkana kadar.</param>
        public void Hold(string stateName, float maxHoldSec = 0f)
        {
            _stateHash = string.IsNullOrEmpty(stateName) ? 0 : Animator.StringToHash(stateName);
            _timedStateHash = maxHoldSec > 0f ? _stateHash : 0;
            _frame = Time.frameCount;
            _releaseAtUnscaled = maxHoldSec > 0f ? Time.unscaledTime + maxHoldSec : -1f;
        }

        public void Release()
        {
            _stateHash = 0;
            _releaseAtUnscaled = -1f;
        }

        /// <summary>Prefire kilidi bitip motor hareket ettiğinde düz vuruş klibini bırak ve Locomotion'a dön.</summary>
        public bool ShouldExitTimedHoldOnMove(Animator animator)
        {
            if (_timedStateHash == 0 || animator == null)
                return false;
            int hash = _timedStateHash;
            _timedStateHash = 0;
            Release();
            AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
            if (st.shortNameHash == hash)
                return true;
            return animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).shortNameHash == hash;
        }

        public bool IsHeld(Animator animator)
        {
            if (_stateHash == 0 || animator == null)
                return false;
            if (_releaseAtUnscaled > 0f)
            {
                if (Time.unscaledTime >= _releaseAtUnscaled)
                {
                    Release();
                    return false;
                }

                return true;
            }
            // Geçiş Animator güncellemesinde başlar; tutulduğu karede state bilgisi henüz eskidir.
            if (Time.frameCount == _frame)
                return true;
            for (int layer = 0; layer < animator.layerCount; layer++)
            {
                if (animator.GetCurrentAnimatorStateInfo(layer).shortNameHash == _stateHash
                    || (animator.IsInTransition(layer)
                        && animator.GetNextAnimatorStateInfo(layer).shortNameHash == _stateHash))
                    return true;
            }
            _stateHash = 0;
            return false;
        }
    }
}
