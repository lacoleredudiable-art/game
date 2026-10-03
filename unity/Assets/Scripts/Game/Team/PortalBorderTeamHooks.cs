using System;
using System.Collections.Generic;

namespace Dovus.Game.Team
{
    /// <summary>
    /// Sınır / portal / takım çarpanları. Hasar hesabı burada yazılmaz;
    /// mevcut çarpan satırları bu değerleri okur.
    /// </summary>
    public static class PortalBorderTeamHooks
    {
        public static float AttackSpeedMult = 1f;
        public static float DamageMult = 1f;
        public static float LifestealAdd;
        public static float BossIncomingMult = 1f;
        public static float BossStrikeScale = 1f;
        public static float MoveSpeedMult = 1f;
        public static bool IntentionalTeleport;
        public static float PlayerDamageTakenMult = 1f;
        public static int PlayerActorId = 1;

        static readonly Dictionary<int, float> Miss = new();
        static readonly Dictionary<int, float> Taken = new();

        public static event Action<string> Cast;

        public static Func<float> Roll;

        public static void ResetModifiers()
        {
            AttackSpeedMult = 1f;
            DamageMult = 1f;
            LifestealAdd = 0f;
            BossIncomingMult = 1f;
            BossStrikeScale = 1f;
            MoveSpeedMult = 1f;
            PlayerDamageTakenMult = 1f;
            IntentionalTeleport = false;
            Miss.Clear();
            Taken.Clear();
        }

        public static void MarkIntentionalTeleport() => IntentionalTeleport = true;

        public static bool ConsumeIntentionalTeleport()
        {
            bool flagged = IntentionalTeleport;
            IntentionalTeleport = false;
            return flagged;
        }

        public static void NotifyCast(string skillId)
        {
            if (!string.IsNullOrEmpty(skillId))
                Cast?.Invoke(skillId);
        }

        public static void SetMiss(int actorId, float chance)
        {
            if (chance <= 0f)
                Miss.Remove(actorId);
            else
                Miss[actorId] = chance;
        }

        public static void SetTaken(int actorId, float mult)
        {
            if (mult <= 0f)
                mult = 1f;
            if (Math.Abs(mult - 1f) < 0.0001f)
                Taken.Remove(actorId);
            else
                Taken[actorId] = mult;
        }

        public static float DamageTakenMult(int actorId) =>
            Taken.TryGetValue(actorId, out float mult) ? mult : 1f;

        public static bool TryMiss(int actorId)
        {
            if (!Miss.TryGetValue(actorId, out float chance) || chance <= 0f)
                return false;
            float roll = Roll != null ? Roll() : 1f;
            return roll < chance;
        }
    }
}
