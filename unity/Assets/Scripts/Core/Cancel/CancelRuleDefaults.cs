using System.Collections.Generic;

namespace Dovus.Core.Cancel
{
    /// <summary>
    /// İptal tablosunun satırları. Kaynak: 13 §9, 15 §4, 10 O-2.19 / O-3.5 / O-3.11 / O-4.12 / O-4.14, 12 Y24.
    /// Satır eklemek/çıkarmak bu dosyada tek satırdır; karar kodu (<see cref="CancelRuleTable"/>) değişmez.
    /// </summary>
    public static class CancelRuleDefaults
    {
        const string S13 = "13 §9";

        public static CancelRuleTable Build(CancelOptions? o = null)
        {
            o ??= new CancelOptions();
            var r = new List<CancelRule>();
            const ActionPhase any = ActionPhase.Any;

            // --- Skill çalışırken -------------------------------------------------------------
            // "Hazırlık yalnız dodge ile kesilir; dodge hakkı yanar, CD yanmaz."
            r.Add(Cancel("S1", ActionKind.Skill, ActionPhase.Windup, ActionKind.Dodge, true, o.WindupDodgeRefundsCooldown,
                RuleStatus.Proposed, S13 + ", 15 §4 (O-3.5 çelişir: CD harcanır)"));
            r.Add(Deny("S2", ActionKind.Skill, ActionPhase.Windup, ActionKind.Skill, o.BufferMs, RuleStatus.Proposed, S13));
            r.Add(Deny("S3", ActionKind.Skill, ActionPhase.Windup, ActionKind.WeaponSwap, o.BufferMs, RuleStatus.Proposed, S13));
            r.Add(Deny("S4", ActionKind.Skill, ActionPhase.Windup, ActionKind.BasicStrike, o.BufferMs, RuleStatus.Proposed, S13));

            // "Etki kesilmez."
            r.Add(o.DodgeCancelsSkillActive
                ? Cancel("S5", ActionKind.Skill, ActionPhase.Active, ActionKind.Dodge, false, false, RuleStatus.Gap, "O-3.11 (13 §9 çelişir)")
                : Deny("S5", ActionKind.Skill, ActionPhase.Active, ActionKind.Dodge, o.BufferMs, RuleStatus.Proposed, S13 + ", 15 §4 (O-3.11 çelişir)"));
            r.Add(Deny("S6", ActionKind.Skill, ActionPhase.Active, ActionKind.Skill, o.BufferMs, RuleStatus.Proposed, S13));
            r.Add(Deny("S7", ActionKind.Skill, ActionPhase.Active, ActionKind.WeaponSwap, o.BufferMs, RuleStatus.Proposed, S13));
            r.Add(Deny("S8", ActionKind.Skill, ActionPhase.Active, ActionKind.BasicStrike, o.BufferMs, RuleStatus.Proposed, S13));

            // "Toparlanma kesilebilir; öncelik dodge > silah değişimi > düz vuruş sıfırlama."
            r.Add(Cancel("S9", ActionKind.Skill, ActionPhase.Recovery, ActionKind.Dodge, false, false, RuleStatus.Decided, S13 + ", O-3.5"));
            r.Add(o.SwapCancelsSkillRecovery
                ? Cancel("S10", ActionKind.Skill, ActionPhase.Recovery, ActionKind.WeaponSwap, false, false, RuleStatus.Proposed, S13)
                : Deny("S10", ActionKind.Skill, ActionPhase.Recovery, ActionKind.WeaponSwap, o.BufferMs, RuleStatus.Proposed, S13));
            r.Add(o.StrikeCancelsSkillRecovery
                ? Cancel("S11", ActionKind.Skill, ActionPhase.Recovery, ActionKind.BasicStrike, false, false, RuleStatus.Proposed, S13 + " (düz vuruş sıfırlama)")
                : Deny("S11", ActionKind.Skill, ActionPhase.Recovery, ActionKind.BasicStrike, o.BufferMs, RuleStatus.Proposed, S13));
            // Skill → skill doğrudan kesme yok: sıradaki skill toparlanma bitince başlar (tamponla).
            r.Add(Deny("S12", ActionKind.Skill, ActionPhase.Recovery, ActionKind.Skill, o.BufferMs, RuleStatus.Proposed, S13 + ", O-4.12"));

            // --- Silah değişimi: kendi toparlanması iptal edilemez (sonsuz zincir olmasın) -------
            r.Add(Deny("W1", ActionKind.WeaponSwap, any, ActionKind.Dodge, o.BufferMs, RuleStatus.Decided, "O-2.19, " + S13));
            r.Add(Deny("W2", ActionKind.WeaponSwap, any, ActionKind.Skill, o.BufferMs, RuleStatus.Decided, "O-2.19, " + S13));
            r.Add(Deny("W3", ActionKind.WeaponSwap, any, ActionKind.WeaponSwap, o.BufferMs, RuleStatus.Proposed, S13));
            r.Add(Deny("W4", ActionKind.WeaponSwap, any, ActionKind.BasicStrike, o.BufferMs, RuleStatus.Gap, "belgede yok"));

            // --- Düz vuruş ---------------------------------------------------------------------
            // Dodge her düz vuruş fazını keser (bugünkü DodgeCancelRules: "skill ve düz vuruş kesilir").
            r.Add(Cancel("B1", ActionKind.BasicStrike, any, ActionKind.Dodge, false, false, RuleStatus.Decided, "O-3.11, DodgeCancelRules"));
            // Y24: klip sürerken gelen dokunuş sıraya alınır.
            r.Add(Deny("B2", ActionKind.BasicStrike, any, ActionKind.BasicStrike, o.StrikeQueueMs, RuleStatus.Decided, "Y24"));
            // Belgede yok: düz vuruş sırasında skill/değişim → klip bitince başlasın (tampon).
            r.Add(Deny("B3", ActionKind.BasicStrike, any, ActionKind.Skill, o.BufferMs, RuleStatus.Gap, "belgede yok"));
            r.Add(Deny("B4", ActionKind.BasicStrike, any, ActionKind.WeaponSwap, o.BufferMs, RuleStatus.Gap, "belgede yok"));

            // --- Dodge çalışırken ----------------------------------------------------------------
            // Hak ve zincir kararı DodgeChain/DodgeChargeBank'ta; bu tablo yalnız "başka eylem keser mi"yi söyler.
            r.Add(Cancel("D1", ActionKind.Dodge, any, ActionKind.Dodge, false, false, RuleStatus.Decided, "15 §3 (DodgeChain karar verir)"));
            r.Add(Deny("D2", ActionKind.Dodge, any, ActionKind.Skill, o.BufferMs, RuleStatus.Gap, "belgede yok"));
            r.Add(Deny("D3", ActionKind.Dodge, any, ActionKind.WeaponSwap, o.BufferMs, RuleStatus.Decided, "O-2.19"));
            r.Add(Deny("D4", ActionKind.Dodge, any, ActionKind.BasicStrike, o.BufferMs, RuleStatus.Gap, "belgede yok"));

            return new CancelRuleTable(r, new[]
            {
                o.PriorityDodge, o.PriorityWeaponSwap, o.PrioritySkill, o.PriorityBasicStrike,
            });
        }

        static CancelRule Cancel(string id, ActionKind running, ActionPhase phases, ActionKind incoming,
            bool fizzles, bool refundsCd, RuleStatus status, string source) =>
            new CancelRule(id, running, phases, incoming, true, 0, fizzles, refundsCd, status, source);

        static CancelRule Deny(string id, ActionKind running, ActionPhase phases, ActionKind incoming,
            int bufferMs, RuleStatus status, string source) =>
            new CancelRule(id, running, phases, incoming, false, bufferMs, false, false, status, source);
    }
}
