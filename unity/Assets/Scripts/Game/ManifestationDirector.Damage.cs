using System;
using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using UnityEngine;

namespace Dovus.Game
{
    public sealed partial class ManifestationDirector
    {
        WeaponArmorCatalog _weaponArmor;
        int _damageRoll;

        void RefreshDefenderArmor()
        {
            if (_weaponArmor == null && ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design))
                _weaponArmor = WeaponArmorCatalog.FromJson(design.Json);
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
            _bossStatus.Armor.Base = BossCombatProfile.FromJson(design.Json).Armor;
            _weaponArmor ??= WeaponArmorCatalog.FromJson(design.Json);
        }

        DamageOutcome ComputeOutgoingHit(
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
            outMult *= _modeDirector?.DamageMult ?? 1f;
            outMult *= _passiveDirector?.DamageMult ?? 1f;
            outMult *= _slotPassives?.DamageMultFor(_slotQueryCastId) ?? 1f;
            outMult *= PortalBorderTeamHooks.DamageMult;
            outMult *= SelfDamageBuffMult();
            outMult *= chainBonusOverride ?? _closingChainBonus;
            float eqMult = 1f;
            if (_equipmentBonus != null && !isBasicStrike && !skill.IsEmpty)
            {
                WeaponSkillCompatibility compatibility = WeaponCompatibilityFor(skill);
                eqMult = compatibility.DamageMult;
                LastWeaponCompatible = compatibility.Compatible;
                LastWeaponPassiveEnabled = compatibility.PassiveEnabled;
                LastWeaponUiLabel = compatibility.UiLabel;
            }
            outMult *= eqMult;
            outMult *= WeaponOutgoingDamageMult(skill, isBasicStrike);
            LastEquipmentMatchMult = eqMult;

            float per = _combat != null ? _combat.ClosingDamagePerEffect : 1f;
            float reference = _skillNumbers != null ? _skillNumbers.VerbDamageReference : 0f;
            bool formula = _combat != null && _combat.UseFormulaDamage
                && !isBasicStrike && !skill.IsEmpty && skill.BaseDamage > 0f;

            float skillPower;
            float runeMult = 1f;
            if (formula)
            {
                skillPower = skill.BaseDamage;
                runeMult = skill.DamageMult > 0f ? skill.DamageMult : 1f;
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

            if (!skill.IsEmpty && !skill.EngineModifiers.IsNull && skill.EngineModifiers.Has("element_mult"))
            {
                float element = skill.EngineModifiers["element_mult"].AsFloat(1f);
                if (element > 0f)
                    runeMult *= element;
            }

            float penPct = _passiveDirector?.ArmorPenPercent ?? 0f;
            float penFlat = _passiveDirector?.ArmorPenFlat ?? 0f;
            bool ignoreArmor = WeaponIgnoresArmor
                || (!skill.IsEmpty && !skill.EngineModifiers.IsNull
                    && skill.EngineModifiers["ignore_armor"].AsBool(false));
            float slotPen = _slotPassives?.ArmorPenPercentFor(_slotQueryCastId) ?? 0f;
            penPct = SlotPassiveCombat.CombineArmorPen(penPct, ignoreArmor, slotPen);

            double now = _clock != null ? _clock.Director.WorldTimeMs : 0;
            float armor = 0f;
            float taken = 1f;
            float shield = 0f;
            if (_bossStatus != null)
            {
                _bossStatus.Armor.Passive = 0f;
                armor = _bossStatus.Armor.Effective(now);
                taken = _bossStatus.Board.IncomingDamageMult * PortalBorderTeamHooks.BossIncomingMult;
                shield = _bossStatus.Board.ShieldRemaining;
            }

            float poise = 0f;
            if (!skill.IsEmpty)
            {
                float weaponPoise = _equippedWeapon != null && _equippedWeapon.PoiseMult > 0f
                    ? _equippedWeapon.PoiseMult
                    : 1f;
                float bonusPoise = HitMods(skill, isBasicStrike, false).PoiseMult;
                float slotPoise = _slotPassives?.PoiseDamageMultFor(_slotQueryCastId) ?? 1f;
                if (slotPoise <= 0f)
                    slotPoise = 1f;
                poise = WeaponPassiveRules.OutgoingPoise(
                    skill.BasePoise,
                    skill.PoiseDamageMult,
                    weaponPoise,
                    bonusPoise * slotPoise);
            }

            bool canCrit = formula || (!isBasicStrike && !skill.IsEmpty && skill.BaseDamage > 0f);
            float extraCrit = ExtraCritChanceAdd(skill) + WeaponCritAdd(skill, isBasicStrike);
            int seed = _damageRoll++;

            var outcome = DamagePipeline.Resolve(new DamageQuery
            {
                SkillPower = skillPower * Mathf.Max(0f, effectScale),
                AttackPower = _passiveDirector?.AttackPower ?? 1f,
                Multiplier = outMult * runeMult,
                LandMultiplier = () => _player != null ? PlayerDodgeRig.ConsumeNextHit(_player) : 1f,
                CanCrit = canCrit && skillPower > 0f,
                CritChance = DamagePipeline.DefaultCritChance + Mathf.Max(0f, extraCrit),
                CritMultiplier = DamagePipeline.DefaultCritMultiplier,
                Armor = armor,
                ArmorPenFlat = penFlat,
                ArmorPenPercent = penPct,
                DamageTakenFactor = taken,
                Shield = shield,
                ApplyVariance = true,
                VarianceSeed = seed,
                Poise = poise,
                ThreatMultiplier = _passiveDirector?.ThreatMultiplier ?? 1f,
                ScaleMagnitudes = true
            });

            if (outcome.Amount > 0f && ignoreArmor)
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

        float HealBuffMultiplier(in SkillResolution skill)
        {
            float healMult = _playerStatus != null ? _playerStatus.Board.HealEffectivenessMult : 1f;
            healMult *= _passiveDirector?.HealMult ?? 1f;
            healMult *= _closingChainBonus;
            return healMult;
        }

        void ApplyArmorShred(in SkillResolution skill, ActorStatus target)
        {
            if (target == null || skill.IsEmpty || skill.EngineModifiers.IsNull)
                return;
            JsonValue engine = skill.EngineModifiers;
            float debuff = engine["debuff_armor"].AsFloat(0f);
            double now = _clock != null ? _clock.Director.WorldTimeMs : 0;
            if (debuff < 0f)
            {
                float sec = engine["debuff_duration_sec"].AsFloat(4f);
                target.Armor.ApplyShred(-debuff, now, now + Math.Max(0.05f, sec) * 1000.0);
            }

            float buff = engine["armor_add"].AsFloat(0f);
            if (Mathf.Abs(buff) < 0.001f)
                buff = engine["buff_armor"].AsFloat(0f);
            if (buff > 0f && _playerStatus != null)
            {
                buff = WeaponPassiveRules.ScaleFriendlyMagnitude(buff, WeaponFriendlyScale());
                float sec = engine["buff_duration_sec"].AsFloat(engine["debuff_duration_sec"].AsFloat(3f));
                _playerStatus.Armor.GrantBuff(buff, now + Math.Max(0.05f, sec) * 1000.0);
            }
        }
    }
}
