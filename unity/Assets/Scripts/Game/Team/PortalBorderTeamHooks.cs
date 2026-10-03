using System;
using Dovus.App.Team;

namespace Dovus.Game.Team
{
    /// <summary>
    /// Sınır / portal / takım çarpanları. Hasar hesabı burada yazılmaz;
    /// mevcut çarpan satırları bu değerleri okur.
    /// </summary>
    public static class PortalBorderTeamHooks
    {
        public static TeamModifierTable Table { get; } = new();

        public static float AttackSpeedMult
        {
            get => Table.For(PlayerActorId).AttackSpeedMult;
            set => SetPlayerModifiers(Table.For(PlayerActorId), value, null, null, null, null);
        }

        public static float DamageMult
        {
            get => Table.For(PlayerActorId).DamageMult;
            set => SetPlayerModifiers(Table.For(PlayerActorId), null, value, null, null, null);
        }

        public static float LifestealAdd
        {
            get => Table.For(PlayerActorId).LifestealAdd;
            set => SetPlayerModifiers(Table.For(PlayerActorId), null, null, value, null, null);
        }

        public static float BossIncomingMult
        {
            get => Table.BossIncomingMult;
            set => Table.BossIncomingMult = value;
        }

        public static float BossStrikeScale
        {
            get => Table.BossStrikeScale;
            set => Table.BossStrikeScale = value;
        }

        public static float MoveSpeedMult
        {
            get => Table.For(PlayerActorId).MoveSpeedMult;
            set => SetPlayerModifiers(Table.For(PlayerActorId), null, null, null, value, null);
        }

        public static bool IntentionalTeleport;
        public static float PlayerDamageTakenMult
        {
            get => Table.For(PlayerActorId).DamageTakenMult;
            set => SetPlayerModifiers(Table.For(PlayerActorId), null, null, null, null, value);
        }

        public static int PlayerActorId = 1;

        public static event Action<string> Cast;

        public static Func<float> Roll
        {
            get => Table.Roll;
            set => Table.Roll = value;
        }

        public static void ResetModifiers()
        {
            Table.Reset();
            IntentionalTeleport = false;
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

        public static void SetMiss(int actorId, float chance) => Table.SetMiss(actorId, chance);

        public static void SetTaken(int actorId, float mult) => Table.SetTaken(actorId, mult);

        public static float DamageTakenMult(int actorId) => Table.DamageTakenMult(actorId);

        public static bool TryMiss(int actorId) => Table.TryMiss(actorId);

        static void SetPlayerModifiers(
            ActorModifiers current,
            float? attackSpeedMult,
            float? damageMult,
            float? lifestealAdd,
            float? moveSpeedMult,
            float? damageTakenMult)
        {
            Table.Set(
                PlayerActorId,
                new ActorModifiers(
                    attackSpeedMult ?? current.AttackSpeedMult,
                    damageMult ?? current.DamageMult,
                    lifestealAdd ?? current.LifestealAdd,
                    moveSpeedMult ?? current.MoveSpeedMult,
                    damageTakenMult ?? current.DamageTakenMult,
                    current.MissChance));
        }
    }
}
