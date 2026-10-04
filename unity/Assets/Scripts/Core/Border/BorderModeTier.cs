using Dovus.Core.Grammar;

namespace Dovus.Core.Border
{
    /// <summary>element-sistemi.json engine.border_mode değerleri.</summary>
    public static class BorderModeTier
    {
        public static bool TryFromEngine(
            in SkillEngineModifiers engine,
            out float threshold,
            out float attack,
            out float life,
            out float damage)
        {
            threshold = 0f;
            attack = 0f;
            life = 0f;
            damage = 0f;
            if (engine.IsNull)
                return false;
            string mode = engine.ReadString("border_mode");
            switch (mode)
            {
                case "tier_20":
                    threshold = BorderMode.Tier20;
                    attack = BorderMode.Tier20Attack;
                    life = BorderMode.Tier20Life;
                    damage = BorderMode.Tier20Damage;
                    return true;
                case "tier_10":
                    threshold = BorderMode.Tier10;
                    attack = BorderMode.Tier10Attack;
                    life = BorderMode.Tier10Life;
                    damage = BorderMode.Tier10Damage;
                    return true;
                default:
                    return false;
            }
        }

        public static bool OpensColumn(in SkillEngineModifiers engine) =>
            !engine.IsNull && engine.ReadBool("border_column");
    }
}
