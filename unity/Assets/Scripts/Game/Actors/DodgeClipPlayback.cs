using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// Dodge klibi hızlandırılarak sıkıştırılmaz: klip importer'da takla / kalkış / geri sıçrama
    /// kesitlerine ayrılır (klipler.csv). Her kesit state'inin hızı klip boyudur; burada verilen
    /// çarpan 1 / süre olduğundan kesit tam o sürede biter. Parametre yoksa (başka controller) dokunulmaz.
    /// </summary>
    public static class DodgeClipPlayback
    {
        public const string RollState = ActorView.StateDodge;
        public const string RollClip = "Ortak_Takla";
        public const string RiseState = "DodgeRise";
        public const string RiseClip = "Ortak_Takla_Kalkis";
        public const string BackstepClip = "Ortak_GeriSicrama";
        public const string RollSpeedParam = "DodgeSpeed";
        public const string RiseSpeedParam = "DodgeRiseSpeed";

        static readonly int RollSpeedHash = Animator.StringToHash(RollSpeedParam);
        static readonly int RiseSpeedHash = Animator.StringToHash(RiseSpeedParam);
        static readonly int RollStateHash = Animator.StringToHash(RollState);
        static readonly int[] FeetOwnerStates =
        {
            Animator.StringToHash(RollState),
            Animator.StringToHash(RiseState),
            Animator.StringToHash(RollClip),
            Animator.StringToHash(RiseClip),
            Animator.StringToHash(BackstepClip),
        };

        /// <summary>
        /// Takla kesitlerinde kök yüksekliği klibin kendisinden (pozda, zemine oturur). Ayak kilidi
        /// havadaki ayağı zemine çekerken gövdeyi yere gömüyordu; bu state'lerde ayağı klip taşır.
        /// </summary>
        public static bool ClipOwnsFeet(Animator anim)
        {
            if (anim == null || !anim.isActiveAndEnabled || anim.runtimeAnimatorController == null)
                return false;
            // Kesitten çıkış karışımında (çömelme → duruş) ayak yine kilitlenir.
            AnimatorStateInfo st = anim.IsInTransition(0)
                ? anim.GetNextAnimatorStateInfo(0)
                : anim.GetCurrentAnimatorStateInfo(0);
            return IsFeetOwner(st.shortNameHash);
        }

        static bool IsFeetOwner(int stateHash) => System.Array.IndexOf(FeetOwnerStates, stateHash) >= 0;

        /// <summary>
        /// Takla kaymayla, kalkış toparlanmayla aynı anda biter. Takla kesiti çömelmeden başlar:
        /// ayakta duruşla karıştırılırsa (çapraz geçiş) ayaklar yere girer; kesit doğrudan başlar.
        /// </summary>
        public static void ApplyDodge(Animator anim, float rollSec, float riseSec)
        {
            SetDuration(anim, RollSpeedParam, RollSpeedHash, rollSec);
            SetDuration(anim, RiseSpeedParam, RiseSpeedHash, riseSec);
            if (anim != null && anim.isActiveAndEnabled && anim.runtimeAnimatorController != null
                && anim.HasState(0, RollStateHash))
                anim.Play(RollStateHash, 0, 0f);
        }

        /// <summary>Geri sıçrama kesiti sıçrama hareketiyle aynı anda biter.</summary>
        public static void ApplyBackstep(Animator anim, float hopSec) =>
            SetDuration(anim, RollSpeedParam, RollSpeedHash, hopSec);

        static void SetDuration(Animator anim, string name, int hash, float sec)
        {
            if (anim == null || anim.runtimeAnimatorController == null || sec <= 0f || !HasFloat(anim, name))
                return;
            anim.SetFloat(hash, 1f / sec);
        }

        static bool HasFloat(Animator anim, string name)
        {
            foreach (AnimatorControllerParameter p in anim.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Float && p.name == name)
                    return true;
            }
            return false;
        }
    }
}
