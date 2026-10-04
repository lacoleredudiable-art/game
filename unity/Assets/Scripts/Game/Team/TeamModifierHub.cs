using System;
using Dovus.App.Actors;
using Dovus.App.Team;
using Dovus.Core.Actors;
using Dovus.Core.Shared;

namespace Dovus.Game.Team
{
    /// <summary>
    /// Sınır / portal / takım çarpanları. Hasar hesabı burada yazılmaz;
    /// mevcut çarpan satırları bu değerleri okur.
    /// </summary>
    public sealed class TeamModifierHub
    {
        public static TeamModifierHub Neutral { get; } = new TeamModifierHub();

        public TeamModifierTable Table { get; } = new();

        public int PlayerActorId { get; set; } = 1;

        /// <summary>Oyuncu kimliğini kayıt defterinden çöz (sayısal değer ActorDefaults ile aynı kalır).</summary>
        public void BindActorRegistry(ActorRegistry registry)
        {
            if (registry == null)
                return;
            if (registry.Get(ActorDefaults.PlayerId) is PlayerActor)
                PlayerActorId = ActorTargetKey.FromActorId(ActorDefaults.PlayerId);
        }

        public float AttackSpeedMult
        {
            get => Table.For(PlayerActorId).AttackSpeedMult;
            set => SetPlayerModifiers(Table.For(PlayerActorId), value, null, null, null, null);
        }

        public float DamageMult
        {
            get => Table.For(PlayerActorId).DamageMult;
            set => SetPlayerModifiers(Table.For(PlayerActorId), null, value, null, null, null);
        }

        public float LifestealAdd
        {
            get => Table.For(PlayerActorId).LifestealAdd;
            set => SetPlayerModifiers(Table.For(PlayerActorId), null, null, value, null, null);
        }

        public float BossIncomingMult
        {
            get => Table.BossIncomingMult;
            set => Table.BossIncomingMult = value;
        }

        public float BossStrikeScale
        {
            get => Table.BossStrikeScale;
            set => Table.BossStrikeScale = value;
        }

        public float MoveSpeedMult
        {
            get => Table.For(PlayerActorId).MoveSpeedMult;
            set => SetPlayerModifiers(Table.For(PlayerActorId), null, null, null, value, null);
        }

        public bool IntentionalTeleport;

        public float PlayerDamageTakenMult
        {
            get => Table.For(PlayerActorId).DamageTakenMult;
            set => SetPlayerModifiers(Table.For(PlayerActorId), null, null, null, null, value);
        }

        public event Action<string> Cast;

        public Func<float> Roll
        {
            get => Table.Roll;
            set => Table.Roll = value;
        }

        public void ResetModifiers()
        {
            Table.Reset();
            IntentionalTeleport = false;
        }

        public void MarkIntentionalTeleport() => IntentionalTeleport = true;

        public bool ConsumeIntentionalTeleport()
        {
            bool flagged = IntentionalTeleport;
            IntentionalTeleport = false;
            return flagged;
        }

        public void NotifyCast(SkillId skillId)
        {
            if (!skillId.IsEmpty)
                Cast?.Invoke(skillId.Value);
        }

        public void SetMiss(int actorId, float chance) => Table.SetMiss(actorId, chance);

        public void SetTaken(int actorId, float mult) => Table.SetTaken(actorId, mult);

        public float DamageTakenMult(int actorId) => Table.DamageTakenMult(actorId);

        public bool TryMiss(int actorId) => Table.TryMiss(actorId);

        void SetPlayerModifiers(
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
                    damageTakenMult ?? current.DamageTakenMult));
        }
    }
}
