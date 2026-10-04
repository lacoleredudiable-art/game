# PLAN.md — Dövüş oyunu tek iş listesi
Kurallar: aynı anda tek iş · "bitti" = PR merge + testler geçti + kullanıcı gördü · yeni sorunlar "Sonra"ya yazılır, ancak mevcut işi engelliyorsa sorulur · her iş ≤1–2 saatlik PR · model: Composer (composer-2.5); Sonnet/Opus sadece o an açık onayla · git checkout/restore/reset/stash/clean yasak.
Sıra: 0 → 2 → 3 → 4 → 5 → 6 (Aşama 1 yok; numaralar korunmadı). Kullanıcı 2026-10-04 00:34: onay beklemeden sırayla bitir.
Durum etiketleri: [ ] bekliyor · [~] sürüyor · [x] bitti

## Aşama 0 — Düzen (ajanlar için temel)
- [x] 0.1 docs/PLAN.md + docs/ARCHITECTURE.md + docs/MAP.md + docs/OYUN.md + klasör AGENTS.md + verify.ps1
- [x] 0.2 Integration testler: JSON→skill eşleme (144), asset referans testi (VfxLibrary/WeaponVisualRegistry/proplar), kayıt/ayar round-trip; CI'ya bağla
- [x] 0.3 Git dışı asset yedeği (Synty/Mixamo/VFX → yedek klasör veya Git LFS kararı)
- [x] 0.4 Ölü kod temizliği: tek seferlik capture script'leri, 85 eski branch, eski taslak PR'lar (#6 #8 #9 #10 #43 — kullanıcı onayıyla)

## Aşama 2 — Mimari (kuzen önerisi) (davranış değişmeden, adım adım)
- [x] 2.1 Game'i konu klasörlerine ayır + namespace = klasör *(yüzeysel: Game tek SCC — A7 açık)*
- [x] 2.2 Güçlü ID tipleri + JSON mapper *(yüzeysel: kalan string skillId — A28)*
- [x] 2.3 Application assembly + IClock / IRng *(kapandı: A10, A11)*
- [x] 2.4 CastPipeline *(yüzeysel: yürütme Game'de — A9)*
- [x] 2.5 Oyuncu can/diriliş → Application *(kapandı: A10)*
- [x] 2.6 Boss AI → Application *(yüzeysel: BossDirector hâlâ büyük — A3)*
- [x] 2.7 Statik çarpanlar; portal/takım JSON etiketleri *(yüzeysel → A13 kısmi (#139, kalan önbellek statikleri), A14 kapandı (#142, ID listeleri JSON etiketlerine))*
- [x] 2.8 GameTuning bölme; element JSON tek parse *(kapandı: A6)*

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
- [x] 2B.11 ~2.900 sabit sayı → ayar/JSON (A17) — `*Defaults`; literal ratchet `docs/ARCHITECTURE.md`
  - [x] 2B.11b–g tüm oynanış/ayar sayıları `*Defaults` const'larına (1615 → 0 hedef); `MagicNumberRatchetTests` tavanı her PR'da iner — 2B.11c Core+App; 2B.11d Game/Skills+Team; 2B.11e Game/Boss+Actors+Composition+Cameras+Feel+Platform; [x] 2B.11f Game/Weapons+Audio+Casting; [x] 2B.11g Game/Vfx+Arena+Hud bitti
- [x] 2B.12 İsimlendirme (A21–A23); kurallar `docs/ARCHITECTURE.md`
- [x] 2B.12b MonoBehaviour sonek standardı: 54 Game bileşeni + 4 iç içe yardımcı yeniden adlandırıldı (GUID aynı), 79/79 standart sonek, `Game_MonoBehaviours_UseStandardSuffix` kapısı (A21)
- [x] 2B.13 Tek dil + dosya düzeni (A24, A25, C2) — Core/App/Game tek tip/dosya, FileLayoutTests, ≤500 satır kapıları
- [x] 2B.14 DDD: Rune/Element, SkillResolution grupları, wire enum'lar (A26–A30) — tek Skill birleşmesi açık (A27)
- [x] 2B.15 Actor/ActorId, CastPipeline olayları, repository ayrımı (A29, A31, A32) — kısmi iskelet bağlama 2B.23
- [x] 2B.16 Ajan dostu: sabit açılı otomatik ekran görüntüsü aracı (C5); Unity derlemesi CI'da değilse not
- [x] 2B.18a Game katman döngüleri (A7): Platform/Diagnostics yaprakları, `ISentenceDebugSink`, `GameLayeringTests`
- [x] 2B.18b skill ID sabitleri + tipli SkillId (A14/A28)
- [x] 2B.18c SweepV2 shim budaması (A12)
- [x] 2B.19 test ağı: normalize sweep hash kapısı, 144/144, Play fark listesi, PlaySweep tipli erişim
- [x] 2B.20 Game katmanları + DevTools asmdef
- [x] 2B.21 girdi/kombo davranış testleri
- [x] 2B.22 global state kaldırma: `GameSceneRuntime` + builder enjeksiyonu; kalan bilinçli statikler — `AssetCatalog`/`VfxLibraryStandalone`/`ElementSystemRuntimeCache`, `PresentationParticleMaterials` Kenney null tuning, `UiJuiceRuntime.Pulse01`, DevTools `DebugPanelsChrome` iç statikleri
- [x] 2B.23 iskelet bağla/sil
- [x] 2B.24 MD gerçek bölünme: nested host'lar `Game/Skills/Hosts/` (MdCastPort, MdClosingHost, MdCoreServicesHost, MdLaunch/Motion/Skill/Weapon/Projectile hosts), `_deliverySkill` kopyası kaldırıldı, MD partial toplamı ≤3500 (ratchet 3411); kalan: ≤1500 için Execution çıkarma
- [x] 2B.25 skill ID'leri veriye
- [x] 2B.26 sabit kopyaları
- [x] 2B.27 doküman gerçeği (OYUN.md, ARCHITECTURE.md, DocsTruthTests, durum.md → PLAN)
- [x] 2B.28 küçükler (ölü MD sarmalayıcıları, Link.Broken, SeededRng, saat yedeği uyarısı, dil kapısı, GameCompile notu)
- Kural: davranış değişmez (sweep hash + test sayıları), Composer only, her madde 1–3 PR.

## Aşama 2C — Oyun hissi — beklemede (yeni skill yapısı)
*Kullanıcı kararı 10-04: yeni skill sistemi tasarımı geliyor; 2C maddeleri o gelince yeniden yazılır.*

## Açık hatalar / Karar bekleyen
- Sweep: Animator yok; Play odak dışıyken `sure` anlamsız (PR #37).
- Gramer boşlukları: bazı `engine` etkileri hâlâ no-op; `target_ally` prezentasyon validator'da yok.
- Karar: sınır modu 1-2; yoğunlaştırma `hitbox_scale_mult` menzil daraltması; 1-11 yankı + kalıp içi yürüme; element renk otoritesi (`GameTuning` vs prezentasyon JSON); görsel boy ölçümü.
- Oyun: co-op ikinci oyuncu yok; aktif boss Kraliçe — `karadul` `shadow_cut` hâlâ yok; girdi/boss/dodge otomatik testleri sınırlı (Sweep boss hasarı 0).
- Test: otomatik davranış güvencesi sweep hash + yapı testleri; CI dışı Play kıyası bilinen 11 fark listesinde.

## Aşama 4 — Oyun sistemleri
- [ ] 3.1 Diriliş sadece skill ile (2-12 Akan Şifa, koza, takım başına 2) — otomatik 2 sn dirilme kalkar
- [ ] 3.2 4 element etkisi: Su (yenileme), Hava (hız), Toprak (kalkan), Aydınlık (arınma); haste skill/cast hızına da etki
- [ ] 3.3 Test boss PR 3: küçük canavarlar, dalgalar, çok hedef, sekme (8 skill)
- [ ] 3.4 Ağların Kraliçesi: kalan fazlar/mekanikler (koza, ağ, zehir birikimi, Odak/aggro, parça kırma)

## Aşama 5 — Görsel / his
- [ ] 4.1 Silah tutuşları (Ayar sahnesi kalibrasyon sonuçları → kalıcı): Top, Kitap, Tılsım, Küre, Kalkan+Hançer yuvarlak kalkan
- [ ] 4.2 Ortam süsleme: heykeller, uçurumlar, harabeler, savaş kalıntıları, kuru ağaçlar (arena ~50 m yarıçap — sahne `ArenaHalfSizeM`)
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
