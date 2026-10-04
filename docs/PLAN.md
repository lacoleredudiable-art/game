# PLAN.md — Dövüş oyunu tek iş listesi
Kurallar: aynı anda tek iş · "bitti" = PR merge + testler geçti + kullanıcı gördü · yeni sorunlar "Sonra"ya yazılır, ancak mevcut işi engelliyorsa sorulur · her iş ≤1–2 saatlik PR · model: Composer (composer-2.5); Sonnet/Opus sadece o an açık onayla · git checkout/restore/reset/stash/clean yasak.
Sıra: 0 → 2 → 3 → 4 → 5 → 6 (Aşama 1 yok; numaralar korunmadı). Kullanıcı 2026-10-04 00:34: onay beklemeden sırayla bitir.
Durum etiketleri: [ ] bekliyor · [~] sürüyor · [x] bitti

## Aşama 0 — Düzen (ajanlar için temel)
- [x] 0.1 docs/PLAN.md (bu dosya) + docs/ARCHITECTURE-PLAN.md + docs/MAP.md (konu → dosya → giriş noktası) + klasör başına kısa AGENTS.md + Composer görev şablonu + verify.ps1 (derle + Core test + 1440 kapı + gramer → tek özet)
- [x] 0.2 Integration testler: JSON→skill eşleme (144), asset referans testi (VfxLibrary/WeaponVisualRegistry/proplar), kayıt/ayar round-trip; CI'ya bağla
- [x] 0.3 Git dışı asset yedeği (Synty/Mixamo/VFX → yedek klasör veya Git LFS kararı)
- [x] 0.4 Ölü kod temizliği: tek seferlik capture script'leri, 85 eski branch, eski taslak PR'lar (#6 #8 #9 #10 #43 — kullanıcı onayıyla)

## Aşama 2 — Mimari (kuzen önerisi) (davranış değişmeden, adım adım)
- [x] 2.1 Game'i konu klasörlerine ayır + namespace = klasör (Dovus.<Katman>.<Konu>)
- [x] 2.2 Güçlü ID tipleri (SkillId, WeaponId, RuneId, ElementId, ActorId) + JSON mapper (ham JsonValue Game'e sızmaz)
- [x] 2.3 Application assembly + IClock / IRng; mana/diriliş oyun saatine, tohumsuz Random'lar CombatRng'ye
- [x] 2.4 CastPipeline (ManifestationDirector.cs:1316–1332 taşınır; komut al → olay yay)
- [x] 2.5 Oyuncu can/diriliş → Application (Player aggregate)
- [x] 2.6 Boss AI → Application (BossBrain) — 2.6a BossAttackSelector; 2.6b BossBrain
- [x] 2.7 Statik çarpanlar oyuncu başına; PortalSystem/TeamComboSystem switch'leri → JSON etiketleri; pasifler enum
- [x] 2.8 GameTuning bölme; element-sistemi.json'dan lore/changelog ayırma, tek parse

## Aşama 3 — Oynanış doğruluğu (küçük, somut hatalar) [mimariden sonra]
- [x] 1.1 Cooldown + mana açık (EnforceCooldown/EnforceResourceCost) — kombo bazlı CD kararı
- [x] 1.2 Dünya donması kalksın (dodge 90 ms / vurulma 130 ms → sadece görsel)
- [x] 1.3 Dodge: 6 sn dolum + çift basış birleşik dodge (uzun i-frame)
- [x] 1.5 Yanlış silah ikonları (Yay/Kitap/Küre) + lock-on düğmesi rün paneliyle çakışma
- [x] 1.6 Kullanıcı kararı + iş: skill metni ↔ sayı (Denetim D) — doğru kaynak JSON mu metin mi?

## Aşama 2B — Kalan mimari (kuzenin 32 maddesinin tamamı) [Aşama 3'ten sonra]
- [x] 2B.1 Ölü kod taraması + silme (A17, C8); ActorModifiers.MissChance, kullanılmayan dodge kancası
  - [x] 2B.1b docs temizliği: ilk prototip artıkları silindi (`docs/concept/`, `docs/archive/`, eski play-sweep sonuçları); `durum.md`/`unity-notlari.md` bağlantı ve adları güncellendi
- [x] 2B.2 ManifestationDirector'ı tamamen böl (A1, A9): kalan skill akışı, VFX, HUD, takım → App/Game servisleri; dosya ≤500 satır
- [x] 2B.3 HexagonInput böl (A2): girdi / skill tetik / UI ayrı
- [x] 2B.4 BossDirector kalan Unity mantığı → App (A3)
- [x] 2B.5 GameBootstrap → Composition kökü, küçük kurucular (A4); GameTuning gerçek alt nesneler + sahne değeri göç aracı (A5) — 2B.5a kurucular (#103); 2B.5b tuning bölümleri + sahne göçü (#104)
- [x] 2B.6 Core içi döngüler + Combat konu bölme (A8, A19) — 2B.6a (#106), 2B.6b (#107), 2B.6c döngü kapısı + kalan SCC allowlist
- [x] 2B.7 SweepV2 sahte Unity katmanı bağımlılığını azalt: testler App katmanına (A12)
- [x] 2B.8 7 singleton + kalan statikler → enjeksiyon (A13); 2B.8b: `AssetCatalog`, `TeamComboAccess`, HUD/SFX/VFX/theme enjeksiyonu — (a) team hooks: `TeamModifierHub` örneği, statik `PortalBorderTeamHooks` kaldırıldı
- [x] 2B.9 59 FindAnyObjectByType/Camera.main → referans enjeksiyonu; her kare GetComponent önbellek (A15)
- [x] 2B.10 Eksik asset referansları raporu + güvenli geri dönüş (A16)
- [x] 2B.11 ~2.900 sabit sayı → ayar/JSON (öncelik: oynanış sayıları) (A17) — 5 dosya + `*Defaults`; görsel ~3596 literal `docs/constants-report.md`
  - [x] 2B.11b–g tüm oynanış/ayar sayıları `*Defaults` const'larına (1615 → 0 hedef); `MagicNumberRatchetTests` tavanı her PR'da iner — 2B.11c Core+App; 2B.11d Game/Skills+Team; 2B.11e Game/Boss+Actors+Composition+Cameras+Feel+Platform; [x] 2B.11f Game/Weapons+Audio+Casting; [x] 2B.11g Game/Vfx+Arena+Hud bitti
- [x] 2B.12 İsimlendirme: sonek standardı, TeamCombo*/GameBootstrap/GameTuning/GrammarDebugPanel adları, yorum kodları (A21–A23); `docs/naming.md`
- [x] 2B.12b MonoBehaviour sonek standardı: 54 Game bileşeni + 4 iç içe yardımcı yeniden adlandırıldı (GUID aynı), 79/79 standart sonek, `Game_MonoBehaviours_UseStandardSuffix` kapısı (A21)
- [ ] 2B.13 Tek dil kuralı (kod İngilizce, veri sözcükleri sözlükle) (A24); dosya adı=tip, tek tip/dosya, yanlış yerdeki dosyalar (A25) — (a) Core/App tek tip/dosya bitti; **(b) Game tek tip/dosya + yanlış klasör + A24 kapı testleri**; **(c) Core ≤500**; **[x] (d) Game/tools ≤500 + FileSizeTests**
- [ ] 2B.14 DDD: Rune/Element dili (A26), tek Skill modeli (A27), ID tiplerinin tam benimsenmesi + SkillResolution sadeleşme (A28, A30) — **[x] (a) A26 rün dili + A27 skill-model.md + SkillCatalogEntry**; **[x] (b) A28/A30 SkillResolution grupları + wire enum'lar + Game JsonValue kapısı**
- [ ] 2B.15 DDD: Player/Actor varlıkları, hedef=ActorId (A29); skill yan etkileri → olaylar (A31); repository arayüzleri, katalog = parser/factory/depo ayrımı (A32) — **[x] (a) Actor varlıkları, ActorRegistry/ActorViewRegistry, PlayerTargeting ActorId**; **[x] (b) CastPipeline sunum olayları + ISkill/IMotionTemplate repository/parser ayrımı (Presentation/Equipment katalogları PR dışı)**
- [x] 2B.16 Ajan dostu: sabit açılı otomatik ekran görüntüsü aracı (C5); Unity derlemesi CI'da değilse not
- [x] 2B.18a Game katman döngüleri (A7): Platform/Diagnostics yaprakları, `ISentenceDebugSink`, `GameLayeringTests`
- Kural: davranış değişmez (sweep hash + test sayıları), Composer only, her madde 1–3 PR.

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
