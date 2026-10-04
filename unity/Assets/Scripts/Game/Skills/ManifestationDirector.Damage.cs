using Dovus.App.Casting;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Game.Actors;
using Dovus.Game.Data;
using Dovus.Game.Team;
using System;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        WeaponArmorCatalog _weaponArmor;
        /// <summary>O7: tek kalıcı savaş zarı. Oyunda oturum tohumu; tarama/test <see cref="ReseedCombatRng"/> ile sabit tohum.</summary>
        readonly CombatRng _combatRng = new CombatRng(CombatRng.SessionSeed());
        CritSystem? _critSystem;

        /// <summary>Play Sweep / testler: deterministik kritik ve sapma.</summary>
        public void ReseedCombatRng(int seed) => _combatRng.Reseed(seed);

        public int CombatRngSeed => _combatRng.Seed;

        /// <summary>element-sistemi.json crit_system (base 0.05, ×2.0, tavan 0.75).</summary>
        CritSystem Crits
        {
            get
            {
                if (_critSystem == null)
                    _critSystem = ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design)
                        ? CritSystem.FromDocument(design.Document)
                        : CritSystem.Default;
                return _critSystem.Value;
            }
        }

        internal void RefreshDefenderArmor()
        {
            if (_weaponArmor == null && ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design))
                _weaponArmor = WeaponArmorCatalog.FromDocument(design.Document);
            if (_playerStatus == null)
                return;
            float weapon = _weaponArmor != null && _equippedWeapon != null
                ? _weaponArmor.ArmorOf(_equippedWeapon.Id)
                : 0f;
            _playerStatus.Armor.Base = weapon;
        }

        void EnsureBossArmor()
        {
            if (_bossStatus == null)
                return;
            if (!ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design))
                return;
            _bossStatus.Armor.Base = BossCombatProfile.FromDocument(design.Document).Armor;
            _weaponArmor ??= WeaponArmorCatalog.FromDocument(design.Document);
        }

        internal DamageOutcome ComputeOutgoingHit(
            ClosingHit closing,
            SkillResolution skill,
            bool isBasicStrike,
            float slashCommitMult,
            float effectScale,
            float? chainBonusOverride)
        {
            EnsureBossArmor();
          RefreshDefenderArmor();

            float outMult = 1f;
            if (_playerStatus != null)
                outMult *= _playerStatus.Board.OutgoingDamageMult;
            outMult *= _slotPassives?.DamageMultFor(CastSession.SlotQueryCastId) ?? 1f;
            outMult *= TeamHub.DamageMult;
            outMult *= SelfDamageBuffMult();
            outMult *= ConsumeOverflowBonus(isBasicStrike);
            outMult *= chainBonusOverride ?? CastSession.ClosingChainBonus;
            float eqMult = 1f;
            if (_equipmentBonus != null && !isBasicStrike && !skill.IsEmpty)
            {
                WeaponSkillCompatibility compatibility = WeaponCompatibilityFor(skill);
                eqMult = compatibility.DamageMult;
            }
            outMult *= eqMult;
            outMult *= WeaponOutgoingDamageMult(skill, isBasicStrike);
            LastEquipmentMatchMult = eqMult;

            float per = _combat != null ? _combat.ClosingDamagePerEffect : 1f;
            float reference = _skillNumbers != null ? _skillNumbers.VerbDamageReference : 0f;
            bool formula = _combat != null && _combat.UseFormulaDamage
                && !isBasicStrike && !skill.IsEmpty && skill.Combat.BaseDamage > 0f;

            float skillPower;
            float runeMult = 1f;
            if (formula)
            {
                skillPower = skill.Combat.BaseDamage;
                runeMult = skill.Scaling.DamageMult > 0f ? skill.Scaling.DamageMult : 1f;
            }
            else
            {
                skillPower = ClosingDamageMath.Compute(
                    closing.TotalEffect,
                    per,
                    skill,
                    isBasicStrike,
                    1f,
                    reference);
            }

            if (skillPower <= 0f && slashCommitMult > 0f && closing.TotalEffect > 0f)
                skillPower = closing.TotalEffect * per * slashCommitMult;

            skillPower = DamagePipeline.TuneOutgoingPower(
                isBasicStrike,
                skillPower,
                _combat != null ? _combat.BasicStrikePower : 0f,
                _combat != null ? _combat.SkillPreArmorScale : 1f);

            if (!skill.IsEmpty && !skill.Engine.IsNull && skill.Engine.HasElementMult)
            {
                float element = skill.Engine.ElementMult(1f);
                if (element > 0f)
                    runeMult *= element;
            }

            bool skillIgnoresArmor = !skill.IsEmpty && !skill.Engine.IsNull
                && skill.Engine.IgnoreArmor(false);
            // O8: Yay'ın "sonraki vuruş zırh yok" bonusu yalnız gerçekten işe yaradığında (skill zaten delmiyorsa) tüketilir.
            bool weaponArmorBonus = WeaponIgnoresArmor && !skillIgnoresArmor;
            bool ignoreArmor = WeaponIgnoresArmor || skillIgnoresArmor;
            float slotPen = _slotPassives?.ArmorPenPercentFor(CastSession.SlotQueryCastId) ?? 0f;
            float penPct = SlotPassiveCombat.CombineArmorPen(0f, ignoreArmor, slotPen);

            double now = _clock != null ? _clock.Director.WorldTimeMs : 0;
            float armor = 0f;
            float taken = 1f;
            float shield = 0f;
            if (_bossStatus != null)
            {
                _bossStatus.Armor.Passive = 0f;
                armor = _bossStatus.Armor.Effective(now);
                taken = _bossStatus.Board.IncomingDamageMult * TeamHub.BossIncomingMult;
                shield = _bossStatus.Board.ShieldRemaining;
            }
            if (!skill.IsEmpty && JsonEffectRules.PiercesDefenses(MechanicPlanFor(skill)?.Body))
            {
                shield = 0f;                  // delici: boss kalkanını deler
                taken = Mathf.Max(taken, 1f); // ve hasar azaltmasını yok sayar
            }

            float poise = 0f;
            if (!skill.IsEmpty)
            {
                float weaponPoise = _equippedWeapon != null && _equippedWeapon.PoiseMult > 0f
                    ? _equippedWeapon.PoiseMult
                    : 1f;
                float bonusPoise = HitMods(skill, isBasicStrike, false).PoiseMult;
                float slotPoise = _slotPassives?.PoiseDamageMultFor(CastSession.SlotQueryCastId) ?? 1f;
                if (slotPoise <= 0f)
                    slotPoise = 1f;
                poise = WeaponPassiveRules.OutgoingPoise(
                    skill.Combat.BasePoise,
                    skill.Scaling.PoiseDamageMult,
                    weaponPoise,
                    bonusPoise * slotPoise);
            }

            bool canCrit = formula || (!isBasicStrike && !skill.IsEmpty && skill.Combat.BaseDamage > 0f);
            float extraCrit = ClosingHealRules.ExtraCritChanceAdd(skill) + WeaponCritAdd(skill, isBasicStrike);

            var outcome = DamagePipeline.Resolve(new DamageQuery
            {
                SkillPower = skillPower * Mathf.Max(0f, effectScale),
                Multiplier = outMult * runeMult,
                LandMultiplier = () => _player != null ? PlayerDodgeController.ConsumeNextHit(_player) : 1f,
                CanCrit = canCrit && skillPower > 0f,
                CritChance = Crits.ChanceWith(extraCrit),
                CritMultiplier = Crits.Multiplier,
                CritRoll01 = _combatRng.NextRoll01(),
                Armor = armor,
                ArmorPenPercent = penPct,
                DamageTakenFactor = taken,
                Shield = shield,
                ApplyVariance = true,
                VarianceRoll01 = _combatRng.NextRoll01(),
                Poise = poise,
                ScaleMagnitudes = true
            });

            if (outcome.Amount > 0f && weaponArmorBonus)
                WeaponIgnoresArmor = false;
            if (outcome.ShieldAbsorbed > 0f && _bossStatus != null)
                _bossStatus.Board.ConsumeShield(outcome.ShieldAbsorbed);
            if (_bossStatus != null)
            {
                _bossStatus.LastHitWasCrit = outcome.WasCrit;
                _bossStatus.LastThreat = outcome.Threat;
                _bossStatus.LastPoise = outcome.Poise;
            }
            return outcome;
        }

        internal float HealBuffMultiplier(in SkillResolution skill)
        {
            float healMult = _playerStatus != null ? _playerStatus.Board.HealEffectivenessMult : 1f;
            healMult *= CastSession.ClosingChainBonus;
            return healMult;
        }

        internal void ApplyArmorShred(in SkillResolution skill, ActorStatusHost target)
        {
            if (skill.IsEmpty || skill.Engine.IsNull)
                return;
            var engine = skill.Engine;
            float debuff = engine.DebuffArmor(0f);
            double now = _clock != null ? _clock.Director.WorldTimeMs : 0;
            if (debuff < 0f && target != null)
            {
                float sec = engine.DebuffDurationSec(SkillsManifestationDefaults.DefaultDebuffDurationSec);
                target.Armor.ApplyShred(-debuff, now, now + Math.Max(SkillsManifestationDefaults.ShredMinDurationSec, sec) * SkillsTimeDefaults.SecToMs);
            }

            float buff = engine.ArmorAdd(0f);
            if (Mathf.Abs(buff) < 0.001f)
                buff = engine.BuffArmor(0f);
            if (buff > 0f && _playerStatus != null)
            {
                buff = WeaponPassiveRules.ScaleFriendlyMagnitude(buff, WeaponFriendlyScale());
                float sec = engine.BuffDurationSec(engine.DebuffDurationSec(SkillsManifestationDefaults.DefaultBuffDurationSec));
                _playerStatus.Armor.GrantBuff(buff, now + Math.Max(SkillsManifestationDefaults.ArmorBuffMinDurationSec, sec) * SkillsTimeDefaults.SecToMs);
            }
        }
    }
}
