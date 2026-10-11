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
        int _frame;

        public void Hold(string stateName)
        {
            _stateHash = string.IsNullOrEmpty(stateName) ? 0 : Animator.StringToHash(stateName);
            _frame = Time.frameCount;
        }

        public void Release() => _stateHash = 0;

        public bool IsHeld(Animator animator)
        {
            if (_stateHash == 0 || animator == null)
                return false;
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
