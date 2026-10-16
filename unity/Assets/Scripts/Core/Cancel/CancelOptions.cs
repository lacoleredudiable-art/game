namespace Dovus.Core.Cancel
{
    /// <summary>
    /// Onay bekleyen ([Ö]) ve belgeler arası çelişen kuralların anahtarları. Varsayılan = belgenin önerisi.
    /// Anahtar çevirmek tek satırlık veri değişikliğidir; kod değişmez.
    /// </summary>
    public sealed class CancelOptions
    {
        /// <summary>O-4.12 (ÖNERİ).</summary>
        public int BufferMs = CancelDefaults.BufferMs;

        /// <summary>Y24 (kullanıcı kararı): düz vuruş dokunuşunun sırada bekleme süresi.</summary>
        public int StrikeQueueMs = CancelDefaults.StrikeQueueMs;

        /// <summary>
        /// 13 §9 [Ö] + 15 §4: hazırlıkta dodge ile kesilen skill'in CD'si yanmaz (true).
        /// 10 O-3.5: "mana ve cooldown harcanır" (false). Belgeler çelişiyor; 13 üstte olduğu için true.
        /// </summary>
        public bool WindupDodgeRefundsCooldown = true;

        /// <summary>
        /// 13 §9 [Ö] + 15 §4: "Etki kesilmez" (false). 10 O-3.11 ve bugünkü <c>DodgeCancelRules</c>:
        /// dodge her durumdan çıkar (true). Çelişki; 13/15 üstte olduğu için false.
        /// </summary>
        public bool DodgeCancelsSkillActive = false;

        /// <summary>13 §9 [Ö]: skill toparlanması silah değişimiyle kesilir.</summary>
        public bool SwapCancelsSkillRecovery = true;

        /// <summary>13 §9 [Ö]: skill toparlanması düz vuruşla kesilir (düz vuruş sıfırlama / weave).</summary>
        public bool StrikeCancelsSkillRecovery = true;

        /// <summary>13 §9 [Ö]: skill/silah değişimi başlayınca düz vuruş sayacı sıfırlanır.</summary>
        public bool WeaveResetsStrikeCounter = true;

        /// <summary>Tampondaki girdilerin deneme sırası (küçük önce): dodge, silah değişimi, skill, düz vuruş.</summary>
        public int PriorityDodge = 0;
        public int PriorityWeaponSwap = 1;
        /// <summary>Belgede yok (13 §9 yalnız dodge &gt; değişim &gt; düz vuruşu sıralar); skill ortaya konuldu.</summary>
        public int PrioritySkill = 2;
        public int PriorityBasicStrike = 3;
    }
}
