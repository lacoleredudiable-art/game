using System;

namespace Dovus.Core.Elements
{
    /// <summary>
    /// Kilitli element spec çözümleyicisi.
    /// 2 → kimlik skill; 3 → fiil + core sıfat; 4 → fiil + bileşik sıfat.
    /// </summary>
    public sealed class ElementResolver
    {
        readonly ElementCatalog _catalog;

        public ElementResolver(ElementCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public bool TryResolve(ReadOnlySpan<int> coreRunes, out ElementResolveResult result)
        {
            result = default;
            int n = coreRunes.Length;
            if (n < 2 || n > 4)
                return false;

            for (int i = 0; i < n; i++)
            {
                if (coreRunes[i] < 1 || coreRunes[i] > 6)
                    return false;
            }

            if (!_catalog.TryGetLength(n, out LengthEconomy length))
                return false;

            string compoundId = ElementCatalog.CompoundId(coreRunes[0], coreRunes[1]);
            if (!_catalog.TryGetElement(compoundId, out ElementNode compound) || compound.IsCore)
                return false;
            if (!_catalog.TryGetVerb(compound.VerbId, out VerbStats verb))
                return false;

            int[] runesCopy = coreRunes.ToArray();

            if (n == 2)
            {
                if (string.IsNullOrEmpty(compound.SkillId))
                    return false;

                result = new ElementResolveResult(
                    ElementResolveKind.IdentitySkill,
                    runesCopy,
                    compoundId,
                    compound.SkillId,
                    compound.SkillName,
                    compound.SkillJob,
                    compound.VerbUnlocksJob,
                    verb,
                    null,
                    verb.BaseDamage * length.DamageMult,
                    verb.BasePoise * length.PoiseDamageMult,
                    ResolveMobility(length.Mobility, verb),
                    length.CastTimeMult,
                    length.ResourceCostMult,
                    length.Role);
                return true;
            }

            AdjectiveMods adjective;
            if (n == 3)
            {
                if (!_catalog.TryGetCore(coreRunes[2], out ElementNode coreAdj))
                    return false;
                if (!_catalog.TryGetAdjective(coreAdj.AdjectiveId, out adjective))
                    return false;
            }
            else
            {
                string adjCompoundId = ElementCatalog.CompoundId(coreRunes[2], coreRunes[3]);
                if (!_catalog.TryGetElement(adjCompoundId, out ElementNode adjCompound) || adjCompound.IsCore)
                    return false;
                if (!_catalog.TryGetAdjective(adjCompound.AdjectiveId, out adjective))
                    return false;
            }

            result = new ElementResolveResult(
                n == 3
                    ? ElementResolveKind.VerbPlusCoreAdjective
                    : ElementResolveKind.VerbPlusCompoundAdjective,
                runesCopy,
                compoundId,
                null,
                null,
                null,
                compound.VerbUnlocksJob,
                verb,
                adjective,
                verb.BaseDamage * length.DamageMult * adjective.DamageMult,
                verb.BasePoise * length.PoiseDamageMult * adjective.PoiseDamageMult,
                ResolveMobility(length.Mobility, verb),
                length.CastTimeMult,
                length.ResourceCostMult,
                length.Role);
            return true;
        }

        public static CastMobility MostRestrictive(CastMobility a, CastMobility b) =>
            (CastMobility)Math.Max((byte)a, (byte)b);

        static CastMobility ResolveMobility(CastMobility lengthMobility, VerbStats verb)
        {
            if (verb.IsMotionFamily)
                return verb.CastMobility;
            return MostRestrictive(lengthMobility, verb.CastMobility);
        }
    }
}
