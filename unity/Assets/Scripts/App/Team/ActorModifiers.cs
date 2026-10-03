namespace Dovus.App.Team
{
    public readonly struct ActorModifiers
    {
        public float AttackSpeedMult { get; }
        public float DamageMult { get; }
        public float LifestealAdd { get; }
        public float MoveSpeedMult { get; }
        public float DamageTakenMult { get; }
        public float MissChance { get; }

        public ActorModifiers(
            float attackSpeedMult,
            float damageMult,
            float lifestealAdd,
            float moveSpeedMult,
            float damageTakenMult,
            float missChance = 0f)
        {
            AttackSpeedMult = attackSpeedMult;
            DamageMult = damageMult;
            LifestealAdd = lifestealAdd;
            MoveSpeedMult = moveSpeedMult;
            DamageTakenMult = damageTakenMult;
            MissChance = missChance;
        }

        public static ActorModifiers Default =>
            new(1f, 1f, 0f, 1f, 1f, 0f);
    }
}
