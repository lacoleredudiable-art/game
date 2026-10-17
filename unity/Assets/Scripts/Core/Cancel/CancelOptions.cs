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
        /// [K] Mustafa 2026-10-16 Q2: hazırlıkta dodge ile kesilen skill'in bekleme süresi yanar, iade yok (false).
        /// Oyunda genel kural: iade yok. true yapmak 13 §9 / 15 §4'ün eski önerisine döner.
        /// </summary>
        public bool WindupDodgeRefundsCooldown = false;

        /// <summary>
        /// [K] Mustafa 2026-10-16 Q1: dodge skill'i etki anında da hemen keser (true). 10 O-3.11 ile uyumlu.
        /// false: etki kesilmez, dodge tampona girer (13 §9 / 15 §4'ün eski önerisi).
        /// </summary>
        public bool DodgeCancelsSkillActive = true;

        /// <summary>13 §9 [Ö]: skill toparlanması silah değişimiyle kesilir.</summary>
        public bool SwapCancelsSkillRecovery = true;

        /// <summary>
        /// [K] Mustafa 2026-10-16 Q3: skill toparlanması düz vuruşla KESİLMEZ (false); düz vuruş tampona girer.
        /// </summary>
        public bool StrikeCancelsSkillRecovery = false;

        /// <summary>
        /// [K] Mustafa 2026-10-16 Q3: düz vuruş sürerken skill basılırsa skill düz vuruşun animasyonunu keser (true).
        /// Vuruş karesinden önce basılırsa düz vuruş etki etmez, sonra basılırsa etki eder, animasyon skill'inkiyle değişir.
        /// </summary>
        public bool SkillCancelsStrike = true;

        /// <summary>
        /// [K] Mustafa 2026-10-16 Q4: düz vuruş sırasında zamanında (vuruş karesinden sonra, toparlanmada) silah değişimi
        /// düz vuruşu keser ve zinciri sıfırlar (true). Daha erken basılırsa reddedilir, <see cref="BufferMs"/> kadar saklanır.
        /// "Zamanında" penceresi = vuruş karesinden sonrası (basit okuma; kullanıcı ayrı süre verirse buraya girer).
        /// </summary>
        public bool SwapCancelsStrikeRecovery = true;

        /// <summary>
        /// [K] Mustafa 2026-10-16 Q4: dodge ya da silah değişimi sürerken gelen skill / düz vuruş "çalışmaz".
        /// Basit okuma: girdi atılır, saklanmaz (0 ms). Sıraya alınsın istenirse tek bu sayı yükseltilir.
        /// </summary>
        public int BlockedInputBufferMs = 0;

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
