namespace Dovus.Core.RuleEngineV4
{
    public enum RuleEngineV4TargetSide
    {
        Self,
        Hostile,
        Friendly,
    }

    public sealed class RuleEngineV4TargetResolution
    {
        public RuleEngineV4TargetResolution(
            RuleEngineV4TargetSide side,
            bool requiresLivingTarget,
            string autoProfileKey)
        {
            Side = side;
            RequiresLivingTarget = requiresLivingTarget;
            AutoProfileKey = autoProfileKey;
        }

        public RuleEngineV4TargetSide Side { get; }
        public bool RequiresLivingTarget { get; }
        /// <summary>Sıfat/fiil ölçüsü (Odaklı, İşaretli, …) — yalnız plan meta.</summary>
        public string AutoProfileKey { get; }
    }

    /// <summary>Taraf kuralı + sıfatın otomatik hedef kolu (kural-kitabi-v4 §1.2, §3.2).</summary>
    public static class RuleEngineV4TargetResolver
    {
        public static RuleEngineV4TargetResolution Resolve(RuleEngineV4Verb verb, RuleEngineV4Adjective adjective)
        {
            if (adjective.Id == 9 && verb.Id == 9)
                return new RuleEngineV4TargetResolution(RuleEngineV4TargetSide.Friendly, true, "isaretli_arindirma_place");

            if (adjective.Id == 9)
                return new RuleEngineV4TargetResolution(RuleEngineV4TargetSide.Friendly, false, "isaretli_nearest_mark");

            if (adjective.Id == 2)
                return new RuleEngineV4TargetResolution(VerbSide(verb), true, "odakli_need");

            if (adjective.Id == 7)
                return new RuleEngineV4TargetResolution(VerbSide(verb), true, "tetikli_nearest");

            return new RuleEngineV4TargetResolution(VerbSide(verb), RequiresTarget(verb), DefaultAutoKey(verb));
        }

        static RuleEngineV4TargetSide VerbSide(RuleEngineV4Verb verb)
        {
            if (verb.Id == 3 || verb.Id == 4 || verb.Id == 10)
                return RuleEngineV4TargetSide.Self;
            return verb.Hostile ? RuleEngineV4TargetSide.Hostile : RuleEngineV4TargetSide.Friendly;
        }

        static bool RequiresTarget(RuleEngineV4Verb verb) =>
            verb.Id switch
            {
                3 => true,
                4 => false,
                10 => false,
                _ => verb.Hostile || verb.Id == 2 || verb.Id == 8 || verb.Id == 9 || verb.Id == 12,
            };

        static string DefaultAutoKey(RuleEngineV4Verb verb) =>
            verb.Hostile ? "hostile_nearest" : verb.Id == 3 ? "hostile_nearest_for_move" : "friendly_nearest_or_self";
    }
}
