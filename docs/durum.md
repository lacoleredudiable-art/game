# Durum

> **Görevi bitiren ajan burayı güncellemekle yükümlü.** Bu dosyanın tek amacı, sıradaki
> ajanın repoyu taramadan nerede kaldığımızı anlaması. Kısa tut: ne bitti, ne üretildi,
> nerede sapma var.

> **16 Eylül 2026 — doküman sıfırlaması:** `dovus-sistemi.md`, `tasarim-ozeti.md`,
> `teknoloji-kararlari.md`, `his-kontrol-listesi.md`, `t0-kurulum.md`, `alis-sepeti.md`,
> `animasyon-omurgasi.md` **silindi** (beşgen/3-rün alfa prototipine aitti, altıgen/6-element
> sistemine geçildi, kafa karıştırıyordu). Yeni bağlayıcı doküman: [Element Sistemi](element-sistemi.md)
> + veri kaynağı `element-sistemi.json`. **Aşağıdaki eski oturum kayıtlarında** hâlâ "beşgen",
> "rün", ya da silinen dosyalara link geçebilir — onlar o an doğruydu, güncel mimariyi
> yansıtmazlar; körü körüne referans alma.

**Son güncelleme:** 16 Eylül 2026 (4. tur) · **Sıradaki:** Pentagon→Hexagon isim borcu +
element/sınıf seçimi + co-op (bkz. `docs/element-sistemi.md` §10)

> **16 Eylül (4. tur) — v5.3 JSON merge + ulti (active_modes) uçtan uca.** Sahibi kapsamlı
> v5.3 spec'i + ayrı bir "prezentasyon katmanı" JSON'u yapıştırdı, `element-sistemi.json`'ın
> `verbs`/`adjectives`/`elements`/`status_interaction_table`/`atoms_catalog`/
> `atom_kombinasyonlari`'ını v5.3 ile DEĞİŞTİRDİ (additive merge — v5.3'ün redefine etmediği
> `scaling_economy`/`categories`/`verb_families`/`future_layers` gibi bölümler korundu; v5.3
> tek başına eksikti, sıfırdan yazsaydık motor kırılırdı). **2-5 (Zehir/Pus) reverti bu kez
> KABUL edildi** — v5.3 otorite: `aktif_zehirlenme`→`tam_arinma` (İksir), `kisisel_isinlanma`
> →`hiz_gorunmezlik` geri döndü, ikisi de artık gerçek `base_resource_cost` alıyor (20/15).
> `element_families`'a eksik olan `ates_ailesi` eklendi (v5.3'ün `class`/`lore.arketipler`
> alanlarından). Adjective sayısı 41 (42 değil) — v5.3'ün veri gerçeği, uydurma değil.
> 8 test v5.3'e göre güncellendi (skill adı/skill_id değişiklikleri, `Special`/`ZoneEffect`
> artık verb'lerde yok → `IsNull` bekleniyor).
>
> **Ulti sistemi (`active_modes`) uçtan uca yazıldı** — sahibi "v5.2.1 hiç tam aktif olmadı,
> v5.3'e de olmasın" dedi. Aynı elementin dörtlüsü (X-X-X-X) artık gerçekten bir mod açıyor:
> `SkillMotor.ActiveModes` (yeni `ActiveModeNode` + `ParseActiveModes`, JSON'daki 6 modu okur,
> kombo tablosu YAZILMADI — hepsi JSON'dan) → `Core/Combat/ActiveModeDirector.cs` (saf C#,
> tetik/koşul/soğuma/süre durum makinesi) → `ManifestationDirector` dört-aynı-rün kapanışında
> tetikler, `ActorStatus.ModeDirector` üzerinden `damage_mult`/`damage_taken_mult`/
> `move_speed_mult`/`lifesteal`/hareket engeli/`hp_per_sec_percent` maliyeti dünyaya işliyor;
> tek seferlik efektler (`team_full_cleanse`/`team_invulnerability_sec`→Stasis/
> `enemy_blind_sec`→boss Blind/`team_regen_per_sec`→Regen) aktivasyon anında uygulanıyor.
> Yeni `ActiveModeHud.cs` — mod açıkken üst-orta banner isim+geri sayım gösteriyor (Kan
> Çılgınlığı gibi süresizlerde "AKTİF"), `ReactionReadout`'un aksine kendiliğinden SÖNMÜYOR —
> "gerçekten çalışıyor mu" sorusu bir daha sorulmasın diye kalıcı. 10 yeni test
> (`ActiveModeDirectorTests`) + Unity MCP'de derleme/Play mode hatasız doğrulandı.
> **Kapsam dışı bırakılanlar (bkz. Bilinen açıklar):** `cast_time_mult`/`attack_speed_mult`/
> `dash_cooldown_mult`/`afterimage_count` (Fırtına Akışı), `taunt_radius_m` (boss AI hedefleme
> değişimi), `resource_cost` düşümü (oyunda hiç mana/kaynak sistemi yok — önceden de öyleydi),
> `visual.aura`/`screen_edges` (banner var, ekran kenarı VFX yok). `team_has_debuffs`/
> `team_has_wounded` yalnızca oyuncu+ally can oranını okuyor — ally'nin `StatusBoard`'u yok.
> `dotnet test` 155 yeşil (145→155).

> **16 Eylül — bug turu + durum tablosu + boss 2. saldırı (2. oturum):** Sahibi Play mode'da
> beş bug rapor etti, hepsi teşhis edildi ve düzeltildi (Unity MCP ile Play mode'da
> doğrulandı, `dotnet test` 143 yeşil):
> - **Düz vuruş heal basıyordu** — sahnede `BasicStrikeDot` donmuş `5` (eski pentagon
>   SARSINTI) idi, altıgende 5=Aydınlık/`arindirma` (`cleanse` → `IsHealSkill` true
>   sanıyordu). `PrototypeTuning.EnsureRuntimeDefaults()`'a zorla `1` (Ateş) eklendi.
> - **Hasar sayısı görünmüyordu** — sahnede `ShowDamageNumbers` donmuş `0` idi (kod default'u
>   `true` ama sahne ezmişti); aynı yerde zorla `true` yapıldı.
> - **Sol joystick görünmüyordu** — `MoveInput` fonksiyonel olarak zaten çalışıyordu, hiç
>   görseli yoktu. Yeni `JoystickView.cs` (dinamik taban+kabarcık, `PentagonView`'daki
>   `CreateCircleSprite` deseniyle) eklendi.
> - **Kamera 360° dönemiyordu** — `FollowCamera` tamamen sabitti. `OrbitYawDeg` eklendi,
>   yeni `CameraOrbitInput.cs` üçüncü parmak (veya editörde sağ-tık sürükleme) ile yaw
>   döndürüyor; `MoveInput`/`PentagonInput`'un zaten claim ettiği parmaklara dokunmuyor.
> - **Duvarların içine giriliyordu** — Quaternius dungeon mesh'leri collider'sız geliyordu
>   (`ArenaWalkFit`'in "Fizik collider yok" notu); `KinematicMotor` sadece dış kare clamp
>   yapıyordu. Yeni `WallColliderFit.cs` isim eşleşmesiyle (`wall`/`column`/`arch`/...)
>   `BoxCollider` ekliyor (canlı sahnede 52 adet), `KinematicMotor.PushOutOfObstacles()`
>   küre-itme uyguluyor.
>
> **Durum etkileşim tablosu artık var** (`docs/element-sistemi.json` `status_interaction_table`,
> sahibinin 17 kuralı) — `Core/Status/StatusReactionTable.cs` (14 genellenebilir kural) +
> `StatusBoard.Apply/Tick` (reaksiyon uygulama, artık burn/poison/regen tick'i entry'nin
> kendi magnitude'unu okuyor — eskiden global sabitti) + `StatusApplicator` (3 özel durum:
> burn+poison ekstra tick, shield+burn kalkan aşınması, stun+knockback aynı-vuruş süre
> uzaması). `StatusKind.Poison` + `GrievousWounds`'un artık gerçekten kullanılan
> `HealEffectivenessMult`'ı eklendi. 8 yeni test.
>
> **Boss ikinci saldırı:** `docs/bosses/karadul.json`'da speclenmiş ama `implemented:false`
> olan `fire_cone` (Cehennem Nefesi) artık çalışıyor — `BossAttackKind` (Slam/FireCone),
> `BossAttackKindPicker` (aynı desen: üst üste tekrar sınırlı), boss can %50 altına düşünce
> (Faz 2/Öfke) açılıyor. Dar koni (40° yarım açı, `BossAttack.ArcHalfAngleDeg` + gerçek
> `Vector3.SignedAngle` hesap — Slam'in `angleFromForwardDeg` parametresi eskiden hep `0f`
> geçiliyordu, kullanılmıyordu). Hasar/windup spec'ten (18/800ms); radius/arc uydurma
> (docs/element-sistemi.md §10'da işaretli). His deneyi boss hasarını 0'a çekiyor
> (`combat.Boss.Damage=0`) — `FireConeDamage` de aynı satırda 0'landı, tutarlı kalsın.
>
> **HUD (best-effort):** `VitalsHud` bar'ları düz siyah dikdörtgenden yuvarlak köşeli +
> ince kenarlıklı hâle geldi (`RoundedRectSprite`, 9-slice). Diğer HUD elemanlarına
> dokunulmadı — kapsam "modern" tanımı öznel, geri bildirim istiyorum.
>
> **Yapılmadı (kasıtlı, sahibi "sonra yapalım" dedi):** Fab paketleri (Demon Watcher,
> JustCreate karakterler, Modular Dungeon Lava) henüz satın alınmadı — Mixamo/rig
> entegrasyonu ayrı bir tur.

> **16 Eylül (3. tur) — durum tablosu görünmezdi, artık görünüyor.** Sahibi "skilleri
> attığımda bir etkileşim göremiyorum" dedi — teşhis: mekanik (2. turda yazılan
> `StatusReactionTable`) doğru çalışıyordu, ama tetiklendiğinde ekranda **hiçbir sinyal**
> yoktu. `StatusBoard.ReactionTriggered` event'i eklendi; `StatusApplicator.Result` artık
> `TriggeredReactions` taşıyor; `ManifestationDirector.ApplyClosingStatuses` bunu mevcut
> tepki yazısı kanalına (`ReactionReadout.NoteSkill`, "ally +N" ile aynı yol) yazıyor —
> renk `AcidGreen` (§10: kırmızı-turuncu yasak). Unity MCP'de canlı doğrulandı: Ateş'in
> Kor'u (1-3, `zirh_eritme`, mechanics=[armor_break,burn]) TEK cast'te "Erimiş Zırh"ı
> tetikliyor ve şimdi ekrana yazıyor. 2 yeni test (145 yeşil).

**Önceki:** element-sistemi motor genişletmesi adım 2+ (bkz. `docs/gorev-listesi.md` "Backlog")

> **element-sistemi 4.2.2 + SkillMotor parse genişletmesi (16 Eylül):** Sahibi sohbette v5.2
> (5 ailenin verb/bileşik detayı + `three_runes_examples`) ve v5.2.1 (`atoms_catalog`) verisini
> yapıştırdı; `docs/durum.md`'deki eski açık ("Core/Unity henüz okumuyor") buradan kapandı.
> `docs/element-sistemi.json` (+ `Resources` kopyası) additive merge ile 4.2.2'ye çıktı: 33
> fiile `base_resource_cost`/`special`/`zone_effect`, 28 bileşiğe `identity`/`special_mechanics`,
> `element_families` (5 aile — Ateş kaynakta yoktu), `three_runes_examples`, `atoms_catalog` +
> `all_verbs_atoms` + `atom_kombinasyonlari` (motor OKUMAZ, VFX/animasyon referansı) eklendi.
> **2-5 (Zehir/`aktif_zehirlenme`) ve 3-2 (Pus/`kisisel_isinlanma`) bilerek DEĞİŞTİRİLMEDİ**:
> v5.2 bunları İksir/`tam_arinma` ve hız+görünmezliğe geri almak istiyordu ama bu dosyada zaten
> kasıtlı dönüşüm notu vardı (sahibi kararı: kilitli hâli koru).
> `Core/Grammar/MiniJson.cs` (yeni, bağımsız minimal JSON ağacı) + `SkillMotor.cs` artık
> `animation_type`/`target_mode`/`base_cooldown_sec`/`base_resource_cost`/`target_behaviors`/
> `special`/`zone_effect` (verb) ve `engine_modifiers`'ın TAMAMI (adjective, 20+ alan) okunuyor;
> `SkillResolution` bu alanları taşıyor. `dotnet test` 135 yeşil (127→135, 8 yeni test:
> `SkillMotorTests` + `MiniJsonTests`). Kalan adımlar (`ResourceTracker`/`DamageCalculator`/...)
> `docs/gorev-listesi.md` "Backlog" bölümünde — `formulas`/`global_rules` yok, o sayılar
> gelmeden yazılmayacak.

> **Telefon:** `dovus-prototip.apk` (71 MB, 15 Eyl 20:05) → `adb install -r` Success,
> paket `com.dovus.prototip` açıldı. İçerik: arena×3, boss hasar 0, ally+oyuncu %50,
> Su heal, Ally HUD barı.

> **Heal / HUD:** Sol üstte Ally barı + kafa üstü bar. Mend daha boş olana (eşitse sana).

> **Arena / VFX his:** `ArenaWalkFit` duvar/floor’dan yürüyüş yarım kenarı (inset);
> duvara gömülme soft clamp ile kesilir. Arena-wide kırmızı ember kalktı → `LavaDecor`
> (emissive havuz + ısı partikül + point light). Skill: daha kalın glow çizgi, yassı wisp
> (eski top-sürü değil), kapanış bang burst. Atmosphere sıcak lav tonu + bloom.

> **SkillMotion + StateBridge motoru:** Core `SkillMotionMotor` (Pus blink/Zenitsu,
> Hareket dash, sabitleme→işaret) + `StateBridgeBoard` (iki aynı işaret=portal).
> Game: `SkillMotionDriver`, `StateBridgeView`. Hava+Su yakın boss → Zenitsu kesisi;
> Hava+Su+Toprak → işaret; iki işaret arası caminin içine gir → diğer uç.
> Tuning: `CombatTuning.SkillMotion` (sayılar durum.md sapma — spec yoktu).

> **Karadul his turu:** `docs/bosses/karadul.json` (+ Resources). Raid stub’ları future.
> `ClosingDamageMath`: commit × fiil `base_damage` / 40 × sıfat tax; heal/dash = 0 can.
> `ClosingDamagePerEffect` 1→**3.5** (~5×4-rün jab düşürür). Hasar sayısı **açık**, bar
> turuncu + HP metni. Slam hasarı `ActorStatus` (kalkan/stasis i-frame). Dodge i-frame aynı.

> **element-sistemi 4.2.1 merge:** `docs/` + `Resources` senkron. Fold korundu
> (`secondary_effects.mechanical=true`). Fiil rename (Pus/Zehir/Mühür/Kül/Kum/Obsidiyen),
> her fiile `animation_type` / `target_mode` / `base_cooldown_sec`, `poison`, silüet eksenleri.
> `future_layers.state_bridge` binding:false (motor yok). Core henüz yeni alanları okumuyor.

> **Jab polish:** düz vuruş ayrı `BasicStrike` → `Sword_AttackFast` @1.55 + boss’a bak.
> CastPierce de hızlı kılıç; CastSlam Punch. Skill bang yok (önceki gibi).

> **Anim (Quaternius):** `Player_Quaternius` / `Boss_Quaternius` controller —
> `Assets/Art/Quaternius/Animators/`. Map: Roll→Dodge, Punch/Sword→Cast*,
> Jump→Windup, Bite→Slam, HitRecieve→Stagger. `BossVisual` + motor Speed/Dodge/Hit kancaları.
> Prefab’lara controller atandı. Idle loop için clip kopyaları `Animators/Clips/`.

> **Quaternius bağlandı:** Prefab’lar `Assets/Art/Quaternius/Prefabs/` —
> Player/Boss/Arena → Bootstrap. Warrior ~1.9m, Demon ~2.8m.

> **Sanat deneme:** Fab 24s kilidi → Quaternius CC0 indirildi:
> `unity/Assets/Art/Quaternius/` (Characters + Dungeon + BossCandidates).
> Blend/OBJ/zip → `tools/vendor/Quaternius/`. Öneri: `Warrior.fbx`, `Demon.fbx`.
> Fab sepeti (JustCreate + Lava + Demon Watcher) `docs/alis-sepeti.md`’de bekliyor.

> **SkillMotor katlama:** 1=kök skill; 2=`skill_name` kartı (36); 3=(bileşik)+kök sıfatı
> (`Alev · Yoğunlaştırma`); 4=bileşik+bileşik (`Alev+Alev`). 2'li kart 3/4'e taşınmaz.
> 3/4 hazır `skill_name` yok — formül isim. `secondary_effects` notu güncellendi.

**Önceki omurga:** tek Humanoid + kıyafet; sınıf yok. `ActorVisual` / `CastBodyMapper` hazır.

**Önceki:** 23 Ağustos 2026 · Faz 3.5 bitti — telefonda his turu (soru 3–5) / Faz 4 kapısı

> **T14 kapandı.** Üç fiilin hareket karakteri ayrıldı (İĞNE Zenitsu fırlatış, SÜRÜ
> kademeli bulut, SARSINTI yerden yükselen halka) + düz vuruşun kısa jab silüeti.
> `dotnet test` 92 yeşil. Sırada telefonda §13 soru 3–5 yeniden.
>
> **T13 kapandı.** YERE ÇAKMA üç ritmi: YAKIN / GEÇ / GENİŞ. Tell'ler windup'ta okunur
> (GEÇ ton+poz, GENİŞ disk). `MaxSameVariantStreak=2`.
>
> **T12 kapandı.** Boss canı 120, kapanış ödülü × `ClosingDamagePerEffect` hasar, tür son
> rüne bağlı tepki, ölümde `TriggerSlowmo` + çökme + tam can revive.
>
> **T11.1 kapandı.** Mürekkep cümle sınırında kopuyor, toparlanma kilidi kalıcı HUD'da,
> `BossDirector.TickWindup` null-safe.
>
> **T11 kapandı, his turu kapandı, Faz 3.5 planlandı.** §13'ün 1. ve 2. sorusu telefonda
> **evet** — yani Faz 4'ün eski yasağı kalktı. 3–5. sorular açık ve üçünün tıkandığı yer aynı:
> mekanik **okunuyor** ama temsil soyut, boss tek saldırıyla tekdüze, harcamanın gittiği yer yok.
>
> 5. oturumda (23 Ağustos, masa başı) 4. oturumun "cümle sonucu okunmuyor" teşhisi **düzeltildi**:
> sahibi nokta sayısını ve dwell'i görüyor. Yedi karar alındı, spec güncellendi, dört görev
> yazıldı: **T11.1 → T12 → T13 → T14**, sıra bağlayıcı. Kararların tablosu
> `docs/his-kontrol-listesi.md` "Turda alınan kararlar"da; gerekçeler aşağıda.
>
> **Sanat (Faz 4) hâlâ sırada değil** — ama sebebi artık "§13 cevaplanmadı" değil: T12 ve T13
> bossun kaç saldırısı olduğunu ve nasıl öldüğünü donduruyor, rig'li model/animasyon seti o
> kararlara bağımlı. Faz 3.5'in tamamı primitive.

## Tarihçe (özet)

Beşgen prototip dönemi (T0–T14, 22–23 Ağustos): Core/Unity iskeleti, cümle gramer motoru,
dodge/exchange derecelendirmesi, tezahür (yaşayan etki) katmanı, boss telegrafı + yavaş çekim
+ kamera, HUD, oyun içi ayar paneli, Android build + kare süresi göstergesi, his turu (§13
soru 1–2 evet), silüet keskinleştirme — hepsi bitti. 16 Eylül'de altıgen/element-sistemi
dönemine geçildi (bkz. yukarısı). Görev görev "üretilen API / doğrulama / sapma" detayları
**buradan bilerek silindi** — İĞNE/SÜRÜ/SARSINTI/"beşgen" gibi beşgen terminolojisi güncel
altıgen sistemiyle karışıp kafa karıştırıyordu ve artık işimiz yok. Detay istenirse
`git log --oneline` (T0…T14 commit'leri) veya eski commit'lerdeki bu dosyanın hâli yeterli.

Güncel API yüzeyi için kaynak koddur: `Dovus.Core.*` (saf C#, AGENTS kural 1) +
`Dovus.Game.*` (Unity kabuğu). Element/skill verisi için `docs/element-sistemi.md` +
`docs/element-sistemi.json`. Boss verisi `docs/bosses/*.json`.

## Bilinen açıklar

- **Ulti (`active_modes`) efektlerinin bir kısmı henüz dünyaya işlemiyor** (4. tur): JSON'daki
  `cast_time_mult`/`attack_speed_mult` okunuyor (`ActiveModeNode.GetEffect`) ama hiçbir yere
  uygulanmıyor. `dash_cooldown_mult`/`afterimage_count` (Fırtına Akışı) ve `taunt_radius_m`
  (Aşılmaz Duvar) hiç uygulanmıyor. `resource_cost` hiçbir modda düşülmüyor — oyunda hiçbir
  yerde (verb'lerin `base_resource_cost`'u dahil) mana/kaynak sistemi yok, ulti'ye özgü değil.
  `visual.aura`/`screen_edges` okunmuyor — `ActiveModeHud` banner'ı var, ekran kenarı VFX yok.
- **"team_has_debuffs"/"team_has_wounded" basitleştirilmiş okunuyor** (4. tur): `AllyDummy`'nin
  `StatusBoard`'u yok, takım debuff sayısı yalnızca oyuncudan okunuyor. `team_full_cleanse` de
  yalnızca oyuncuyu temizliyor, ally'yi değil. "team_has_wounded" `min(oyuncu, ally)` can
  oranını doğru okuyor.
- **`docs/element-sistemi.json`'ın büyük kısmı motor tarafından hiç okunmuyor** — yalnızca
  `verbs`/`adjectives`/`elements`/`scaling_economy.lengths`/`active_modes` gerçekten parse
  edilip dünyaya işliyor. `formulas`/`crit_system`/`global_rules`/`state_machine`/`passives`/
  `chain_mechanics`/`manipulation_layers`/`equipment_system` hiç kod karşılığı yok — saf veri.
  `status_interaction_table` çalışıyor ama JSON'dan CANLI okunmuyor, biri (`StatusReactionTable.cs`)
  elle senkron kopyaladı; JSON değişirse kod otomatik güncellenmez. `atoms_catalog` /
  `all_verbs_atoms` / `atom_kombinasyonlari` kasıtlı okunmuyor (VFX/animasyon referansı).
  Verb'lerin yeni `crit_eligible`/`element_origin`/`engine_base_stats.damage_type` alanları da
  henüz parse edilmiyor.
- **His deneme (geçici):** arena `ArenaVisualScale=3`, boss hasar 0, `AllyDummy` %50 —
  kalıcı tasarım değil; his bittiğinde geri alınacak.
- **SkillMotion selective hedef UI yok** — Zenitsu = boss menzildeyse; ally blink yok.
- **Karadul faz 2+:** `shadow_cut`/`chain-rift-lock` JSON'da hâlâ `implemented:false`,
  BossDirector'da yok (Slam + FireCone var). Adaptation spam cezası yok.
- **Ateş ailesi'nin bazı fiillerinde `base_resource_cost` yok** — v5.2/v5.3 kaynağı her fiil
  için doldurmadı; eksik olduğu fiillerde `VerbNode.BaseResourceCost` `0` döner (gerçek değer
  değil, "yok" anlamına gelir).
- **Co-op / ikinci oyuncu açık** (§13 soru 5) — ikinci bir oyuncu gerekiyor, tek başına kodla
  kapatılamaz; bkz. dosya başı "Sıradaki".
- **Rune enum adları artık gerçek element adlarıyla eşleşiyor** (bu turda düzeltildi —
  bkz. yukarıdaki not) — eskiden `Rune.Igne`/`Suru`/`Kabuk`/`Zehir`/`Sarsinti`/`Toprak` gibi
  beşgen kalıntısı adlar gerçek elementle (`DisplayName`) uyuşmuyordu.

### Build notları

Taşındı → `docs/unity-notlari.md` (sahne/derleme/Android build tuzakları — tasarım kararı
yok, hepsi "şunu yapma, çalışmıyor" cinsinden, Unity/cihaza dokunacak görevde okunur).

## master ayrışması (16 Eylül, 4. tur sonu)

`origin/master` 29 Ağustos'tan beri **bu sohbetten habersiz, paralel bir hatta** ilerlemiş:
kendi "proje sadeleştirme"si + 13 Eylül'de kilitlenen ayrı bir "v4.2 element spec" +
tamamen başka bir çözücü (`Core/Elements/ElementCatalog.cs`/`ElementResolver.cs`). Sahibine
soruldu: **bu sohbetteki hat (v5.3/`SkillMotor`/ulti) esas alındı**, `origin/master`'ın
paralel içeriği atıldı (`git merge -s ours` ile tarihçe bağlandı ama dosyalar hiç girmedi).
Tek kurtarılan parça `docs/unity-notlari.md` (operasyonel Unity/build notları, gerçekten
yeni ve kullanışlıydı). **Ders: bundan sonraki her oturum işe `git fetch && git log
origin/master` ile başlamalı** — bu ayrışma günler önce fark edilebilirdi.
