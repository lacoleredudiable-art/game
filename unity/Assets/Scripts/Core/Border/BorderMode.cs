using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Shared;

namespace Dovus.Core.Border
{
    /// <summary>
    /// Sınır modu (144 kombo). Skill, can eşiğin altındayken atılırsa açılır.
    /// Can eşiğin üstüne çıkınca biter. Süre sayacı yok.
    /// %20: saldırı hızı +%30, can çalma +%25, hasar +%20.
    /// %10: saldırı hızı +%50, can çalma +%40, hasar +%35.
    /// 12-8'in kendi sütunu ayrıdır: 5 sn boyunca hız +%20'den +%50'ye çıkar.
    /// </summary>
    public sealed class BorderMode
    {
        public const float Tier20 = 0.20f;
        public const float Tier10 = 0.10f;
        public const float Tier20Attack = 0.30f;
        public const float Tier20Life = 0.25f;
        public const float Tier20Damage = 0.20f;
        public const float Tier10Attack = 0.50f;
        public const float Tier10Life = 0.40f;
        public const float Tier10Damage = 0.35f;
        public const float ColumnSec = 5f;
        public const float ColumnFrom = 0.20f;
        public const float ColumnTo = 0.50f;

        readonly Dictionary<int, Slot> _slots = new();

        public void Clear() => _slots.Clear();

        public static bool TryTier(in SkillEngineModifiers engine, out float threshold, out float attack, out float life, out float damage) =>
            BorderModeTier.TryFromEngine(engine, out threshold, out attack, out life, out damage);

        /// <summary>
        /// Eşik altı açar (eşit değil). Üstünde açmaz ve açık modu da kapatmaz.
        /// 12-8 sütunu cana bakmadan başlar.
        /// </summary>
        public bool OnSkill(int actorId, SkillId skillId, in SkillEngineModifiers engine, float hpRatio)
        {
            Slot slot = Get(actorId);
            if (BorderModeTier.OpensColumn(engine))
            {
                slot.ColumnOn = true;
                slot.ColumnLeft = ColumnSec;
                slot.ColumnAge = 0f;
            }

            if (!TryTier(engine, out float threshold, out float attack, out float life, out float damage))
                return slot.Active;

            if (hpRatio >= threshold)
                return false;

            slot.Active = true;
            slot.HoldCast = true;
            slot.Threshold = threshold;
            slot.Attack = attack;
            slot.Life = life;
            slot.Damage = damage;
            slot.Source = skillId.Value;
            return true;
        }

        /// <summary>
        /// Skill bitti. Eşik bir sonraki Tick'te yeniden bakılır.
        /// Aynı skill'in can çalması bitene kadar aura kapanmaz.
        /// </summary>
        public void EndCast(int actorId)
        {
            Get(actorId).HoldCast = false;
        }

        /// <summary>
        /// Can eşiğin üstündeyse modu kapatır. Eşitken açık kalır.
        /// Skill sürerken (HoldCast) kapanmaz; eşik cast anında bakıldı.
        /// </summary>
        public bool Tick(int actorId, float hpRatio, float dtSec)
        {
            Slot slot = Get(actorId);
            bool ended = false;
            if (slot.Active && !slot.HoldCast && hpRatio > slot.Threshold)
            {
                slot.Active = false;
                ended = true;
            }

            if (slot.ColumnOn)
            {
                float dt = dtSec > 0f ? dtSec : 0f;
                slot.ColumnAge += dt;
                slot.ColumnLeft = Math.Max(0f, ColumnSec - slot.ColumnAge);
                if (slot.ColumnAge > ColumnSec)
                    slot.ColumnOn = false;
            }

            return ended;
        }

        public bool Active(int actorId) => Get(actorId).Active;

        public float Threshold(int actorId) => Get(actorId).Active ? Get(actorId).Threshold : 0f;

        public string Source(int actorId) => Get(actorId).Active ? Get(actorId).Source : string.Empty;

        public float AttackSpeedMult(int actorId)
        {
            Slot slot = Get(actorId);
            float m = slot.Active ? 1f + slot.Attack : 1f;
            return m * ColumnAttack(slot);
        }

        public float LifestealAdd(int actorId)
        {
            Slot slot = Get(actorId);
            return slot.Active ? slot.Life : 0f;
        }

        public float DamageMult(int actorId)
        {
            Slot slot = Get(actorId);
            return slot.Active ? 1f + slot.Damage : 1f;
        }

        public float ColumnMoveSpeedMult(int actorId) => ColumnAttack(Get(actorId));

        public string AuraLabel(int actorId)
        {
            Slot slot = Get(actorId);
            if (!slot.Active)
                return string.Empty;
            int pct = (int)Math.Round(slot.Threshold * 100f);
            return "Sınır %" + pct;
        }

        static float ColumnAttack(Slot slot)
        {
            if (!slot.ColumnOn)
                return 1f;
            float u = ColumnSec <= 0f ? 1f : Math.Min(1f, slot.ColumnAge / ColumnSec);
            float add = ColumnFrom + (ColumnTo - ColumnFrom) * u;
            return 1f + add;
        }

        Slot Get(int actorId)
        {
            if (!_slots.TryGetValue(actorId, out Slot slot))
            {
                slot = new Slot();
                _slots[actorId] = slot;
            }
            return slot;
        }

        sealed class Slot
        {
            public bool Active;
            public bool HoldCast;
            public float Threshold;
            public float Attack;
            public float Life;
            public float Damage;
            public string Source = string.Empty;
            public bool ColumnOn;
            public float ColumnLeft;
            public float ColumnAge;
        }
    }
}
