namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Basılı tutulan 2 noktalı kombo parmak kalkınca ne yapar.</summary>
    public enum RuleEngineV4HeldRelease
    {
        /// <summary>Hedef seçme açılmadan bırakıldı: kilitli hedef, yoksa otomatik (H kuralı).</summary>
        LaunchDefault,
        /// <summary>Hedef seçme açık, parmak bir hedefin/yüzeyin üstünde kalktı.</summary>
        LaunchAtPicked,
        /// <summary>Hedef seçme açık, boşluğa bırakıldı: kilit yok sayılır, otomatik hedef.</summary>
        LaunchAuto,
        /// <summary>Merkeze geri bırakıldı ya da dokunuş iptal edildi: skill çıkmaz, mana/bekleme harcanmaz.</summary>
        Cancel,
    }
}
