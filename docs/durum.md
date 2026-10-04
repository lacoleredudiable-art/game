# Durum — açık bilinen sorunlar

> Yalnız **şu an açık** sorunlar. Ekleme yapma; kapattığın satırı PR'ında sil. Geçmiş: git geçmişi (eski arşiv 2B.1b'de silindi) + PR açıklamaları.
> Etiket: **[d]** eefa1b1'de koddan doğrulandı · **[t]** eski durum.md'den taşındı, yeniden doğrulanmadı.

## Altyapı
- [t] Sweep v2: Animator yok (bacak sütunları Play'le kıyaslanmaz); hasar sayıları Play'den birkaç % sapar; editör takılması kaynaklı `sure` hataları başsızda görünmez.
- [d] Play Sweep Unity penceresi önde olmalı: odakta değilken editör ~4 fps'e düşer, 4× taramadaki `sure` kontrolleri anlamsızlaşır (PR #37).

## Gramer / JSON boşlukları
- [d] İşlenmeyen gramer etkileri: `varlik:durum_ekle>dusman` (9-2), `deger:zirh>kendin` (7-2), `varlik:mermi_sil` (23 skill; düşman mermisi yok, hacim boş).
- [d] Hiç okunmayan engine anahtarları: `aoe`, `cleanse_count` (arınma hepsini siler), `shield_absorb` (kalkan tuning'den), `no_global_timescale` (tasarım gereği). `max_targets` hiçbir yerde uygulanmıyor.
- [d] Kısmi: `hasar_buff>dost` dosta binmiyor; `konum:it` yalnız `yukari_firlat`ta sersemletir; yem kopya boss aggro'su çekmez; `team_full_cleanse` yalnız oyuncuda [t].
- [t] Ulti: `afterimage_count`, `taunt_radius_m` yok; ulti `resource_cost` düşülmüyor; `EnforceResourceCost` false (mana/bekleme uygulanmıyor).
- [t] `target_ally` hitbox'ı `prezentasyon-katmani.json`'da yok (6 fiil, validator sessiz atlar). Zone `RadiusM` JSON'da yok (tuning 3,6 m).
- [t] Kritik: JSON %5/×2, canlı vuruş %10/×1,5. Sert boss zırhı (150) için anahtar yok. Ayar paneli sıfırlayınca boss canı 22'ye dönebilir.

## Karar bekleyen (sahip)
- [t] Sınır modu 1-2: can %15'te açılıp aynı skill'in can çalmasıyla aynı karede kapanıyor.
- [t] Yoğunlaştırma pasifi (`hitbox_scale_mult` 0,55) hedef menzilini de daraltıyor: tasarım mı hata mı?
- [t] 1-11 yankı: kalıp sırasında çubuk oyuncuyu yürütmez; kalıp içinde yürüme istenirse ayrı karar.
- [t] Element renkleri: `GameTuning` ile `prezentasyon-katmani.json` `element_colors` uyuşmuyor, otorite açık.
- [t] `read_as_display.duration_ms` 1500 ≠ `FeelTuning.ReadoutHoldMs` 900.
- [t] Görsel boy dolgulu bounds'tan ölçülüyor (şövalye 1,60 m / hedef 1,78).

## Oyun / görsel
- [t] 9-10 yer değişiminde dost Y=0'a iniyor (`Placement` Y 0f).
- [t] Tarama artığı: ardışık 11-1'de minyon bir kare merkeze 0,91 m görünür.
- [t] 3-6 `as` yayı 19,2 m/s: takılan karede 0,6 m eşiğini aşar. `ignore_armor` %50 delme Play'de doğrulanmadı.
- [t] Hitbox VFX yer tutucu küre 1-5 dönüşünü kapatır; 12-7'de kum saati yok; 2-2 akış çizgisi oyuncu→dost.
- [t] Bağ/hacim (`_mechanicLinks`) status temizliğinden sonra da Root/Slow uygulayabiliyor.
- [t] Ally HP billboard çok büyük; `LivingEffectView.EnsureBangBurst` konsol spam'i.
- [t] Karadul faz 2+ (`shadow_cut`, `chain-rift-lock`) yok.
- [t] Co-op ikinci oyuncu ve ikinci düşman yok: sekme/yayılma yalnız boss'a iner. Gerçek cihazda dokunma boyutu hissedilmedi.
