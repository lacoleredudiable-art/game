using System;
using System.Collections.Generic;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Katman 1: fiil + sıfat + silah → sıralı komut listesi (fizik-komutlari.md).</summary>
    public sealed class RuleEngineV4Planner
    {
        readonly RuleEngineV4Catalog _catalog;

        public RuleEngineV4Planner(RuleEngineV4Catalog catalog) => _catalog = catalog;

        public CommandPlan Plan(int verbRune, int adjectiveRune, int weaponId, bool targetOutOfRange = false)
        {
            if (!_catalog.TryGetVerb(verbRune, out RuleEngineV4Verb verb)
                || !_catalog.TryGetAdjective(adjectiveRune, out RuleEngineV4Adjective adjective)
                || !_catalog.TryGetWeapon(weaponId, out RuleEngineV4Weapon weapon))
                return new CommandPlan(verbRune, adjectiveRune, weaponId, Array.Empty<PhysicsCommand>());

            var commands = new List<PhysicsCommand>();
            RuleEngineV4Globals g = _catalog.Globals;
            RuleEngineV4Scale scale = _catalog.Scale;
            float rangeM = EffectiveRangeM(verb, weapon);
            float powerMult = AdjectivePowerMult(adjective, g);
            float damageScale = weapon.DamageScale(g.RitmReferenceTotalSec);

            if (targetOutOfRange)
                commands.Add(new MenzileYuruCommand { RangeM = rangeM });

            float charge = 0;
            if (adjective.Id == 1 && (!adjective.ChargeOnHostileOnly || verb.Hostile))
                charge = g.YogunChargeSec;

            float prefire = Math.Min(g.PrefireCapSec, weapon.PrefireSec + charge);
            commands.Add(new OnSureCommand
            {
                PrefireSec = prefire,
                ChargeSec = charge,
                TotalCapSec = g.PrefireCapSec,
                RecoverySec = weapon.RecoverySec,
                DamageScale = damageScale,
                PrefireMoves = weapon.PrefireMoves,
            });

            AppendBodyCommands(commands, adjective, verb, weapon, rangeM, g);

            AppendVerbEffect(commands, verb, weapon, scale, damageScale, powerMult, rangeM, g);

            return new CommandPlan(verbRune, adjectiveRune, weaponId, commands);
        }

        float EffectiveRangeM(RuleEngineV4Verb verb, RuleEngineV4Weapon weapon)
        {
            float range = weapon.RangeM(_catalog.Scale.RangeReferenceM);
            if (!verb.Hostile && verb.Id != 3)
                range = Math.Min(range, _catalog.Globals.FriendlyRangeCapM);
            return range;
        }

        static float AdjectivePowerMult(RuleEngineV4Adjective adjective, RuleEngineV4Globals g)
        {
            if (adjective.Id == 1)
                return g.YogunPowerMult;
            if (adjective.Id == 5)
                return g.YayilanPowerMult;
            return 1f;
        }

        void AppendBodyCommands(
            List<PhysicsCommand> commands,
            RuleEngineV4Adjective adjective,
            RuleEngineV4Verb verb,
            RuleEngineV4Weapon weapon,
            float rangeM,
            RuleEngineV4Globals g)
        {
            switch (adjective.Body)
            {
                case "structure":
                    commands.Add(new YapiKurCommand { LifeSec = g.SabitStructureLifeSec });
                    return;
                case "area_ring":
                {
                    float radius = g.YayilanRadiusM;
                    if (g.ClipInnerMeasuresToRange)
                        radius = Math.Min(radius, rangeM);
                    commands.Add(new AlanAcCommand
                    {
                        Shape = "daire",
                        RadiusM = radius,
                        MaxTargets = g.YayilanTargetCap,
                        PowerMult = g.YayilanPowerMult,
                    });
                    return;
                }
                case "guided_lock":
                    commands.Add(new KilitlenCommand
                    {
                        LockCapSec = g.GudumluLockCapSec,
                        BlockInputSec = g.GudumluBlockSec,
                    });
                    if (verb.Id == 3)
                    {
                        commands.Add(new KendiniTasiCommand
                        {
                            MaxDistanceM = rangeM,
                            SpeedMps = weapon.TravelSpeedMps,
                            SkillWhileMoving = weapon.TravelSkillAllowed,
                        });
                        return;
                    }
                    AppendWeaponDelivery(commands, weapon, rangeM);
                    return;
                case "weapon_delivery_then_bounce":
                    AppendWeaponDelivery(commands, weapon, rangeM);
                    {
                        float search = g.SicrayanSearchM;
                        if (g.ClipInnerMeasuresToRange)
                            search = Math.Min(search, rangeM);
                        commands.Add(new SekCommand
                        {
                            BounceCount = g.SicrayanBounceCount,
                            BounceMult = g.SicrayanBounceMult,
                            SearchRadiusM = search,
                        });
                    }
                    return;
                default:
                    AppendWeaponDelivery(commands, weapon, rangeM);
                    break;
            }
        }

        void AppendWeaponDelivery(List<PhysicsCommand> commands, RuleEngineV4Weapon weapon, float rangeM)
        {
            switch (weapon.Delivery)
            {
                case WeaponDeliveryClass.Missile:
                    commands.Add(new MermiFirlatCommand { RangeM = rangeM, SpeedMps = weapon.TravelSpeedMps });
                    break;
                case WeaponDeliveryClass.Area:
                    commands.Add(new AlanAcCommand
                    {
                        Shape = "nokta",
                        RadiusM = rangeM,
                        MaxTargets = 1,
                        PowerMult = 1f,
                    });
                    break;
                default:
                    commands.Add(new YakinVurusCommand { RangeM = rangeM, HitParts = weapon.HitParts });
                    break;
            }
        }

        void AppendVerbEffect(
            List<PhysicsCommand> commands,
            RuleEngineV4Verb verb,
            RuleEngineV4Weapon weapon,
            RuleEngineV4Scale scale,
            float damageScale,
            float powerMult,
            float rangeM,
            RuleEngineV4Globals g)
        {
            switch (verb.Id)
            {
                case 1:
                    commands.Add(new HasarVerCommand
                    {
                        Amount = scale.BaseDamage * damageScale,
                        Poise = scale.BasePoise * powerMult,
                        PowerMult = powerMult,
                    });
                    commands.Add(new DengeVerCommand { Amount = scale.BasePoise * powerMult });
                    break;
                case 2:
                    commands.Add(new SifaVerCommand
                    {
                        Amount = scale.BaseHeal * damageScale,
                        PowerMult = powerMult,
                    });
                    break;
                case 3:
                    if (commands.Exists(c => c is KendiniTasiCommand))
                        break;
                    commands.Add(new KendiniTasiCommand
                    {
                        MaxDistanceM = rangeM,
                        SpeedMps = weapon.TravelSpeedMps,
                        SkillWhileMoving = weapon.TravelSkillAllowed,
                    });
                    break;
                case 4:
                {
                    float dur = powerMult > 1f ? g.KorumaYogunBlockSec : g.KorumaDefaultBlockSec;
                    if (powerMult > 1f)
                        dur *= g.YogunDurationMult;
                    commands.Add(new DurusAcCommand { DurationSec = dur, BlockRatio = 1f });
                    break;
                }
                case 6:
                {
                    float cc = g.KontrolDurationSec * (powerMult > 1f ? g.YogunDurationMult : 1f);
                    commands.Add(new KontrolUygulaCommand { DurationSec = cc });
                    commands.Add(new TasmaBaglaCommand { DurationSec = cc, MaxLengthM = rangeM });
                    commands.Add(new DengeVerCommand { Amount = scale.BasePoise });
                    break;
                }
                case 9:
                    commands.Add(new EtkiSokCommand { Count = powerMult > 1 ? 2 : 1 });
                    break;
            }
        }

    }
}
