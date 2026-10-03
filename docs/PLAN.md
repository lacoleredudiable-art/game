# PLAN.md — Dövüş oyunu tek iş listesi
Kurallar: aynı anda tek iş · "bitti" = PR merge + testler geçti + kullanıcı gördü · yeni sorunlar "Sonra"ya yazılır, ancak mevcut işi engelliyorsa sorulur · her iş ≤1–2 saatlik PR · model: Composer (composer-2.5); Sonnet/Opus sadece o an açık onayla · git checkout/restore/reset/stash/clean yasak.
Sıra: 0 → 2 → 3 → 4 → 5 → 6 (Aşama 1 yok; numaralar korunmadı). Kullanıcı 2026-10-04 00:34: onay beklemeden sırayla bitir.
Durum etiketleri: [ ] bekliyor · [~] sürüyor · [x] bitti

## Aşama 0 — Düzen (ajanlar için temel)
- [ ] 0.1 docs/PLAN.md (bu dosya) + docs/ARCHITECTURE-PLAN.md + docs/MAP.md (konu → dosya → giriş noktası) + klasör başına kısa AGENTS.md + Composer görev şablonu + verify.ps1 (derle + Core test + 1440 kapı + gramer → tek özet)
- [ ] 0.2 Integration testler: JSON→skill eşleme (144), asset referans testi (VfxLibrary/WeaponVisualRegistry/proplar), kayıt/ayar round-trip; CI'ya bağla
- [ ] 0.3 Git dışı asset yedeği (Synty/Mixamo/VFX → yedek klasör veya Git LFS kararı)
- [ ] 0.4 Ölü kod temizliği: tek seferlik capture script'leri, 85 eski branch, eski taslak PR'lar (#6 #8 #9 #10 #43 — kullanıcı onayıyla)

## Aşama 2 — Mimari (kuzen önerisi) (davranış değişmeden, adım adım)
- [ ] 2.1 Game'i konu klasörlerine ayır + namespace = klasör (Dovus.<Katman>.<Konu>)
- [ ] 2.2 Güçlü ID tipleri (SkillId, WeaponId, RuneId, ElementId, ActorId) + JSON mapper (ham JsonValue Game'e sızmaz)
- [ ] 2.3 Application assembly + IClock / IRng; mana/diriliş oyun saatine, tohumsuz Random'lar CombatRng'ye
- [ ] 2.4 CastPipeline (ManifestationDirector.cs:1316–1332 taşınır; komut al → olay yay)
- [ ] 2.5 Oyuncu can/diriliş → Application (Player aggregate)
- [ ] 2.6 Boss AI → Application (BossBrain)
- [ ] 2.7 Statik çarpanlar oyuncu başına; PortalSystem/TeamComboSystem switch'leri → JSON etiketleri; pasifler enum
- [ ] 2.8 PrototypeTuning bölme; element-sistemi.json'dan lore/changelog ayırma, tek parse

## Aşama 3 — Oynanış doğruluğu (küçük, somut hatalar) [mimariden sonra]
- [ ] 1.1 Cooldown + mana açık (EnforceCooldown/EnforceResourceCost) — kombo bazlı CD kararı
- [ ] 1.2 Dünya donması kalksın (dodge 90 ms / vurulma 130 ms → sadece görsel)
- [ ] 1.3 Dodge: 6 sn dolum + çift basış birleşik dodge (uzun i-frame)
- [ ] 1.5 Yanlış silah ikonları (Yay/Kitap/Küre) + lock-on düğmesi rün paneliyle çakışma
- [ ] 1.6 Kullanıcı kararı + iş: skill metni ↔ sayı (Denetim D) — doğru kaynak JSON mu metin mi?

## Aşama 4 — Oyun sistemleri
- [ ] 3.1 Diriliş sadece skill ile (2-12 Akan Şifa, koza, takım başına 2) — otomatik 2 sn dirilme kalkar
- [ ] 3.2 4 element etkisi: Su (yenileme), Hava (hız), Toprak (kalkan), Aydınlık (arınma); haste skill/cast hızına da etki
- [ ] 3.3 Test boss PR 3: küçük canavarlar, dalgalar, çok hedef, sekme (8 skill)
- [ ] 3.4 Ağların Kraliçesi: kalan fazlar/mekanikler (koza, ağ, zehir birikimi, Odak/aggro, parça kırma)

## Aşama 5 — Görsel / his
- [ ] 4.1 Silah tutuşları (Ayar sahnesi kalibrasyon sonuçları → kalıcı): Top, Kitap, Tılsım, Küre, Kalkan+Hançer yuvarlak kalkan
- [ ] 4.2 Ortam süsleme: heykeller, uçurumlar, harabeler, savaş kalıntıları, kuru ağaçlar (arena 25 m üstüne)
- [ ] 4.3 Skill animasyonları: silah başı 6–8 temel hareket, vuruş karesi efektle senkron
- [ ] 4.4 Skill efektleri: ~50 şablon (8 şekil × 6 element) JSON eşleme
- [ ] 4.5 10–15 imza efekt (ör. Zenitsu tarzı Kılıç+Elektrik, Riven Q tarzı 3 aşamalı kombo)
- [ ] 4.6 Boss giriş sahnesi + müzik

## Aşama 6 — Roadmap
- [ ] 5.1 İlerleme (kayıt, rün açılımı, Skill Book)
- [ ] 5.2 Denge (5–6 build karşılaştırma, Delirme/Zaman Akıntısı)
- [ ] 5.3 Photon co-op (Application sınırından; host otoriteli)
- [ ] 5.4 Mobil UI: ana menü, lobi, duraklatma, ayarlar, kombo CD göstergesi
- [ ] 5.5 Dağıtım: release keystore, versionCode artışı

## Sonra (araya giren fikirler buraya)
- Zone/"alev alma" + Outplay ödülü · Ejderha Efendisi sınıfı · ejderha lore · iOS
