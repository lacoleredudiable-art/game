using Dovus.Core.Motion;
using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// O-anim(c): yön zaten blend.Strafe'de ucuzca bilindiğinden "Sidestep" tek klibi sağa
    /// giderken Animator'ın humanoid mirror'ıyla "SidestepRight"e döner; state yoksa (ör.
    /// eski override controller) solda kalır — <see cref="ActorView.PlayAction"/> HasState ile korur.
    /// </summary>
    public static class SidestepMirror
    {
        public static MotionAnimClip Apply(MotionAnimClip clip, float strafe, Animator animator)
        {
            if (strafe <= ActorViewDefaults.SidestepMirrorStrafeMin || !string.Equals(clip.State, "Sidestep", System.StringComparison.Ordinal))
                return clip;
            if (animator == null || !animator.HasState(0, Animator.StringToHash("SidestepRight")))
                return clip;
            return new MotionAnimClip(clip.Key, "SidestepRight", clip.Trigger, clip.Fallback);
        }
    }
}
