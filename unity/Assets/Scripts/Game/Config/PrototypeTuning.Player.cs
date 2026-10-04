using UnityEngine;

namespace Dovus.Game.Config
{
    public sealed partial class PrototypeTuning
    {
        [Header("Karakter görsel boyu (metre)")]
        /// <summary>Prefab kaynağından bağımsız, renderer bounds ile ölçülen oyuncu boyu.</summary>
        public float PlayerVisualHeightM = 1.78f;
        /// <summary>Oyuncunun yaklaşık 2.8 katı; renderer bounds ile ölçülen boss boyu.</summary>
        public float BossVisualHeightM = 5.0f;
        /// <summary>Animator hızı — locomotion 1; idle freeze blend tree’de.</summary>
        public float CharacterAnimSpeed = 1.0f;

        [Header("Oyuncu")]
        /// <summary>Tam çubuk koşu hızı; Axe run klibinin 5.52 m/sn doğal hızına yakın.</summary>
        public float WalkSpeedMps = 6.4f;
        public float MoveAccelMps2 = 52f;
        public float MoveDecelMps2 = 64f;
        public float TurnRateDegPerSec = 720f;
        /// <summary>Çubuk ölü bölgenin hemen üstündeyken hız oranı (yürüme); tam çubuk = 1 (koşu). Önerilen.</summary>
        public float MinStickSpeedFrac = 0.42f;
        /// <summary>Animator Speed parametresi sönümü. Önerilen.</summary>
        public float AnimSpeedDampSec = 0.08f;
        /// <summary>Koşu klibi doğal hızını aşınca en çok bu kat hızlanır (üstü ayak kayması kabul). Önerilen.</summary>
        public float LocoMaxPlaybackMult = 1.5f;
        /// <summary>Aksiyon state'ine giriş crossfade süresi (eskisi sert kesim). Önerilen.</summary>
        public float AnimCrossFadeSec = 0.06f;
        /// <summary>Düz vuruş görsel döngüsü (A→B→C) bu süre vuruşsuz geçince A'ya döner. Önerilen.</summary>
        public float BasicStrikeComboResetSec = 1.2f;
        /// <summary>Animator Speed bu eşiğin üstündeyken cast üst gövde katmanında oynar (bacaklar koşar). Önerilen.</summary>
        public float UpperBodyCastMinSpeed = 0.15f;
        /// <summary>Ayak sesi + tozu bu kadar yatay yolda bir (7.5 m/sn koşuda ~3.4 adım/sn). Önerilen.</summary>
        public float FootstepStrideM = 2.2f;
        /// <summary>Boss ağır adımı (ses + büyük toz). Önerilen.</summary>
        public float BossFootstepStrideM = 2.4f;

        // Spec §11 hasar 22; oyuncu tavanı belgede yok. Bir çakma = ölüm — respawn ≤2 sn
        // (§11) döngüsü böyle denenebiliyor. T11 his turunda ayarlanacak.
        [Header("Oyuncu can (T8)")]
        public int PlayerMaxHp = 22;
        // §6 "sönen artık hız". Spec büyüklük vermiyor; T8'de ana hareketin ORTALAMA hızı
        // (3.8/0.26 = 14.6 m/s) kullanılıyordu — eğri u=1'de hızı sıfıra indirdiği için bu
        // ikinci bir atılım gibi okunuyordu. Yürüme hızı mertebesi seçildi (T8.1).
        [Header("Dodge kayma kuyruğu (T8.1, §6)")]
        public float DodgeGlideSpeedMps = 3.5f;
    }
}
