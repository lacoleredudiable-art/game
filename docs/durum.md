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

**Son güncelleme:** 16 Eylül 2026 (Bağlama 1 · UseFormulaDamage) · **Sıradaki:**
Bağlama 2+ (Resource/Cooldown/…) +
Pentagon→Hexagon isim borcu + element/sınıf seçimi + co-op (bkz. `docs/element-sistemi.md` §10)
+ **Faz 6 bağlama** kalanı (Zone/Passive/TimeEffect/RealityEffect/Equipment/
space_layer/state machine / Presentation henüz Game'e bağlı değil)

> **16 Eylül — Bağlama 1: DamageCalculator bayraklı.** `CombatTuning.UseFormulaDamage`
> varsayılan **false** (ClosingDamageMath birebir). `true` → `DamageCalculator`
> (resistance=0, weakness=1; boss direnci ayrı görev). Crit → `DamageNumberHud.ShowDamage(amount, isCrit)`
> sarı+#58 / normal beyaz+#42. ClosingDamageMath **silinmedi**.
> MCP Play: flag=false → 13.09 = ClosingDamageMath; flag=true+crit → 68 (40×0.85×2),
> HUD `-68` #FFEB33 size 58. `dotnet test` 224 yeşil.

> **16 Eylül — Görev 15: AnimationBridge.** `Game/AnimationBridge.cs` — `AnimationFrameNode`
> + `Animator`: `animator_state` → `Animator.Play` (Controller'da yoksa
> `Debug.LogWarning`, fırlatmaz — SafeSetFloat deseni). worldMs frame-timer:
> `frame * (total_duration_ms / total_frames)` → `DamageFrameReached` /
> `SpawnVfxFrameReached` (`every_tick` → active_frames aralığında her kare;
> null → hasar event yok). Bootstrap/ManifestationDirector **bağlanmadı**.
> Unity MCP Play: Quaternius `Player_Quaternius` + `CastPierce`; startMs=1000,
> damage frame 12 → **1200** ms (PASSED). JSON `animator_state` isimleri
> (`Spell_Cast_Projectile` vb.) Quaternius controller'da yok — Faz 6 eşleme
> (Bilinen açıklar).

> **16 Eylül — Görev 16: PlaceholderFactory.** `Game/PlaceholderFactory.cs` —
> `vfx_binding.trail_vfx` / `impact_vfx` için `Resources/Vfx/{Trail|Impact}/{style}`
> yoksa (şu an hiç yok) `element_colors.primary` ile LineRenderer veya küre
> (`PrimitiveMesh`, collider yok). Uyarı stil başına bir kez
> (`asset_missing_handling.log_warning`). Katalog:
> `Resources/Presentation/prezentasyon-katmani.json` (+ gömülü hex yedek).
> Play doğrulama: `PlaceholderFactoryProbe` — Ateş `#c45c26` impact küre +
> straight trail çizgi (MCP Play mode). LivingEffectView dokunulmadı; Bootstrap'e
> bağlı değil. ManifestationDirector VFX spawn yok (Faz 6). `dotnet test` 224 yeşil.

> **16 Eylül — Görev 14: PresentationValidator.** `Core/Presentation/PresentationValidator.cs`
> — `SkillResolution.Hitbox` → `PresentationCatalog.Hitboxes`, `AnimationType` →
> `Animations` (trajectory eşleştirme **yok**, Görev 16). Eşleşmeyen id sessizce
> `HitboxFound`/`AnimationFound=false` (fırlatmaz). **Eksik hitbox:**
> `SkillResolution.Hitbox` değeri **`target_ally`** prezentasyon katmanında yok —
> fiiller: `cc_arindirma`, `hiz_buff`, `kalkan_transferi`, `arindirma`, `kutsal_kalkan`,
> `dirilis` (6/42). Tüm `animation_type` değerleri katalogda. ManifestationDirector
> bağlama yok. `PresentationValidatorTests` 3; `dotnet test` 224 yeşil.

> **16 Eylül — Görev 13: PresentationCatalog.** `Core/Presentation/PresentationCatalog.cs`
> — MiniJson ile `docs/prezentasyon-katmani.json` (binding:false, SkillMotor'a karışmaz).
> `TrajectoryNode` / `HitboxNode` (Raw + GetFloat/GetBool/GetString) +
> `AnimationFrameNode` (`DamageAppliedAtFrame` JsonValue; CancelWindow nullable) +
> `CompatibilityResult` enum (✓/⚠/✗ → Compatible/Special/Incompatible).
> **Sayılar JSON'dan:** Trajectories=16, Hitboxes=16, Animations=10 (changelog "15"/"17"
> yanlış). `PresentationCatalogTests` 3; `dotnet test` 221 yeşil. Game bağlama yok.

> **16 Eylül — Görev 11: EquipmentCatalog + ElementMatchBonus.** `Core/Equipment/` —
> `EquipmentSlot` / `EquipmentItem` / `EquipmentCatalog` (MiniJson,
> `equipment_system.examples` → 18 item, Id=`{slot}:{element}`) +
> `EquipmentBonusResolver` (`rules.element_match_bonus` "+%10 etki" → 1.1; silah×skill
> element eşleşince çarpan, yoksa 1.0; yüzde ParseDefenseDropMult deseni). Envanter UI /
> PlayerVitals / ManifestationDirector **bağlanmadı** (sabit tek ekipman varsayımı).
> `EquipmentCatalogTests` 2; `dotnet test` 218 yeşil.

> **16 Eylül — Görev 12: UI Rules hizalaması (Game).** `PentagonView` + `ManifestationDirector`:
> her skill cast'te fiil rünü etrafında kozmetik radial cooldown (`base_cooldown_sec`) + kalan sn
> (`ui_rules.cooldown_display`). **CooldownTracker / cast engeli yok** (Faz 6).
>
> **Sayı karşılaştırması (değiştirilmedi — sahibi karar verir):**
> | kaynak | alan | değer | |
> |---|---|---|---|
> | `ui_rules.read_as_display.duration_ms` | JSON | **1500** | |
> | `FeelTuning.ReadoutHoldMs` | Core | **900** | **farklı** |
>
> **zone_display notu (kod yok):** `ui_rules.zone_display` = `in_world`, transparency **0.6**.
> Görev 7 `ZoneDirector` henüz Game'e bağlı değil — dünya alanı görseli Faz 6 / Zone Game
> bağlama turunda.

> **16 Eylül — Görev 4: PassiveDirector.** `Core/Combat/PassiveDirector.cs`: `SkillMotor.Passives`
> listesini alır; `TryTrigger(dot[], worldMs)` ile `trigger_combo` birebir eşleşen pasifi açar;
> `DurationSec` dolunca `Tick` düşürür. Cooldown yok; birden fazla pasif aynı anda aktif
> (ActiveModeDirector tek-mod farkı). Effects `JsonValue` + birleşik çarpan/ek accessors.
> **JSON sayısı 10** (gorev-listesi "12" yazıyordu — yanlış; test `EqualTo(10)`).
> ManifestationDirector'a bağlanmadı. 6 yeni test; `dotnet test` 216 yeşil.

> **16 Eylül — Görev 6: StatusReactionTable canlı JSON.** Elle 14 kural listesi kalktı;
> `Rebuild(motor.StatusInteractions)` effect metninden magnitude/süre alanlarını okur.
> `SkillMotorLoader` yüklerken Rebuild çağırır. 3 satır tabloda yok (özel yol): burn+poison
> (Tick), shield+burn (Tick), stun+knockback (Applicator) — StatusKind id'leri eşleşiyor ama
> genellenebilir kural değil. Eşleşmeyen mechanic id bu turda yok. StatusBoard/Applicator
> dokunulmadı. `dotnet test` + StatusBoardTests regresyon yeşil.

> **16 Eylül — Görev 10: RealityEffectDirector.** `Core/Combat/RealityEffectDirector.cs`
> (`manipulation_layers.reality_layer`): `ApplyReviveBlock` / `IsReviveBlocked` (Cehennem,
> worldMs bayrağı — PlayerVitals respawn'a **bağlanmadı**); `ApplyPartialErase` (Karabasan:
> shield/haste/damage_reduction); `ApplyFullErase` (Hiçlik: yalnızca shields).
> `StatusBoard.RemoveKinds` eklendi (`CleanseHostile` değiştirilmedi). **full_erase
> minions/summons:** oyunda minion/summon sistemi yok — uygulanamaz (aşağıdaki açık).
> 7 yeni test. ManifestationDirector'a bağlanmadı (Faz 6).

> **16 Eylül — Görev 9: TimeEffectDirector.** Core/Combat/TimeEffectDirector.cs —
> manipulation_layers.time_layer 4 tipi (worldMs): delayed_detonation / echo
> (damage_ratio) / extend_lifetime (RemainingSec × multiplier, alan yuvası tutmaz) /
> death_delay (beyan anı + delay). Soft-cap max_active_fields=2. CollectDue tetik
> listesini çıkarır; PlayerVitals/BossVitals/Core/Time/* **dokunulmadı** (Faz 6 bağlama).
> 7 yeni test; dotnet test yeşil (TimeEffectDirectorTests 7/7).

> **16 Eylül (Görev 8) — space_layer okuma (SkillMotor), SkillMotionMotor dokunulmadı.**
> SpaceEffectNode + ParseSpaceEffects → SkillMotor.SpaceEffects (JSON'da 5) /
> MaxActiveLinks (3). Opsiyonel alanlar Has* + değer. **Uygulama yok** —
> SkillMotionMotor / SkillMotionTuning hâlâ sabit sayılar; davranış değişmedi.
>
> **Sayı karşılaştırması (otorite sahibi karar verir — bu turda bağlama yok):**
> | JSON effect | alan | JSON | Tuning/kod | |
> |---|---|---|---|---|
> | lev_isinlanma short_blink | distance_m | 3 | ShortBlinkDistanceM=4 | **farklı** |
> | lev_isinlanma | i_frame_ms | 300 | ShortBlink iframeMs=0 (çağrıda) | **farklı** |
> | yildirim_zenitsu phase_blink | distance_m | 8 | ZenitsuEngageRangeM=9 | **farklı** |
> | yildirim_zenitsu | damage_on_pass | true | ZenitsuSlashCommitMult=1 (kesi var) | kavramsal yakın, birim farklı |
> | yildirim_zenitsu | (i_frame yok) | — | ZenitsuIframeMs=220 | JSON'da yok |
> | pus_gecisi stealth_shift | distance_m | 5 | ShortBlink yolu → 4 | **farklı** |
>
> dotnet test yeşil (SpaceEffects.Count == JSON).


> **16 Eylül (Görev 3) — PlayerStateMachine + state_machine okuma.**
> `SkillMotor.ParseStateMachine` → `PlayerStates` (9) / `BossStates` (6).
> `Core/Combat/PlayerStateMachine.cs` anlık durum + `can_draw`/`can_move`/`can_dodge`/
> `i_frames` okur (bool veya string capability → metin: `"true"`/`"false"`/`"partial"`/
> `"based_on_cast_mobility"`/`"limited"`/`"dodge_direction"`). **SentencePhase'e bağlama
> YOK** — ayrı karar. `dotnet test` yeşil.
>
> **SentencePhase ↔ state_machine.player_states fark raporu (bağlama yapılmadı):**
>
> | SentencePhase (cümle) | player_states (dövüş) | Not |
> |---|---|---|
> | `Idle` | `idle` | İsim örtüşür; ikisi de "boşta". |
> | `Building` | `drawing` | **Uyuşmazlık:** JSON `can_draw="partial"`, `can_move=false`; `Building`'de çizim tam açık, hareket SentencePhase ile kilitlenmez. |
> | `Recovering` | `recovering` | İsim örtüşür; JSON `can_draw=true`, `can_move="limited"`, `can_dodge=true`. `SentencePhase.Recovering` girdi kilidi (`IsRecovering` / RemainingRecoveryMs) — dodge/düz vuruş/yeni fiille kesilir; capability metinleri SentenceEngine'de yok. |
> | `Resolved` / `Aborted` | — | Cümle kaydı fazları; dövüş state_machine'de karşılık yok. |
> | — | `casting` | SentencePhase'te yok. `can_move="based_on_cast_mobility"`. |
> | — | `dodging` | SentencePhase'te yok. `i_frames=true`, `can_move="dodge_direction"`; JSON'da `can_dodge` anahtarı yok → parse `"false"`. |
> | — | `stunned` / `rooted` / `channeling` / `dead` | SentencePhase'te yok (CC / kanal / ölüm). |
>
> Örnek görev notu doğrulandı: `drawing` → `can_draw="partial"`; `Building` tam açık.

> **16 Eylül — Görev 7 (ZoneDirector).** `Core/Layers/IZoneDirector.cs` (`ZoneInstance` +
> movement sabitleri) + `Core/Combat/ZoneDirector.cs` (saf C#: TrySpawn/Tick/Remove/MoveZone/
> SetFollowTarget). Soft-cap = `max_active_zones` (JSON 5), dolunca en eski düşer
> (StateBridgeBoard deseni). `RemainingSec` Tick ile azalır, ≤0 silinir. Movement ayrımı:
> `player_directed`→MoveZone, `follow_target`→SetFollowTarget, `static`/boş→ikisi de no-op.
> Görsel spawn yok. **RadiusM JSON'da yok** — spawn parametresi (çağıran verir). 9 yeni test;
> ManifestationDirector'a bağlanmadı.

> **16 Eylül (Görev 2) — ResourceTracker + CooldownTracker (Core only).**
> `Core/Combat/ResourceTracker.cs`: JSON `global_rules.resource_system` (max_mana=100,
> regen_per_sec=8, regen_delay_after_cast_sec=1.5) — `CanAfford`/`Spend`/`Tick`.
> `Core/Combat/CooldownTracker.cs`: JSON `cooldown_rules` (global_cooldown_sec=0.3,
> max_concurrent_casts=1) — verb id → soğuma bitiş (worldMs), `TryStart`/`CompleteCast`.
> **Oyuna bağlanmadı** (PlayerVitals / ManifestationDirector kasıtlı dışı).
> Testler: `ResourceTrackerTests` + `CooldownTrackerTests`. `dotnet test` 177 yeşil (163+14).
>
> **PlayerVitals MaxHp vs JSON:** `PlayerVitals.MaxHp` `PrototypeTuning.PlayerMaxHp` (=**22**)
> üzerinden `Bind` edilir — `global_rules.player_stats.max_hp` (**100**) değil. Boss:
> `CombatTuning.Boss.MaxHp`=120 vs `boss_stats_default.max_hp`=22000 (prototip his sayıları).
>
> **StatusTuning ↔ `global_rules.status_durations`** (StatusTuning.cs **değiştirilmedi** —
> sahibi otorite seçer):
>
> | status (JSON) | JSON duration | StatusTuning | eşleşme |
> |---|---|---|---|
> | stun | 2s → 2000ms | StunMs=800 | **fark** |
> | root | 3s → 3000ms | RootMs=1200 | **fark** |
> | silence | 3s → 3000ms | SilenceMs=1000 | **fark** |
> | slow | 4s → 4000ms | SlowMs=1500 | **fark** |
> | blind | 3s → 3000ms | BlindMs=1400 | **fark** |
> | fear | 2s → 2000ms | FearMs=900 | **fark** |
> | taunt | 3s → 3000ms | TauntMs=1200 | **fark** |
> | burn | 4s → 4000ms | BurnMs=2400 | **fark** |
> | armor_break | 5s → 5000ms | ArmorBreakMs=3000 | **fark** |
> | weaken | 4s → 4000ms | WeakenMs=2500 | **fark** |
> | grievous_wounds | 6s → 6000ms | GrievousMs=2500 | **fark** |
> | poison | 6s → 6000ms | PoisonMs=3000 | **fark** |
> | shield | 5s → 5000ms | ShieldMs=2500 | **fark** |
> | invulnerability | 0.5s → 500ms | StasisMs=700 (isim varsayımı) | **fark** |
> | confuse | 2s → 2000ms | StatusTuning'de yok | **yalnız JSON** |
>
> Magnitude:
>
> | alan | JSON | StatusTuning | not |
> |---|---|---|---|
> | grievous heal | heal_reduction=0.5 | GrievousHealMult=0.5 | süre hariç **aynı** |
> | slow speed | reduction=0.4 → 0.6 | SlowSpeedMult=0.55 | **fark** |
> | weaken | damage_reduction=0.25 → 0.75 | WeakenOutgoingMult=0.85 | **fark** |
> | armor_break | armor_reduction=0.3 | ArmorBreakDamageTakenMult=1.2 | semantik farklı |
> | burn/poison tick | mult 0.08 / 0.05 | DamagePerSec 6 / 4 | birim farklı |
> | shield absorb | 50 | ShieldAbsorb=25 | **fark** |
> | blind accuracy | 0.5 | yok | yalnız JSON |
>
> StatusTuning'de olup JSON'da olmayan: DisarmMs, Haste*, DamageReduction*, Regen*,
> Knockback*, BurnPoisonComboBonusPerSec, ShieldBurnDrainRatio, StunKnockbackDurationAddMs.

> **16 Eylül (6. tur) — DamageCalculator (formulas.damage + crit_system), paralel sınıf.**
> `Core/Combat/DamageCalculator.cs`: `base_damage_value × adj.damage_mult × length.damage_mult ×
> (1-resistance) × weakness_bonus`, sonra `crit_system` (base + `adjective_crit_bonus[id]`,
> `max_crit_chance` tavanı, `crit_multiplier`); ctor seed'li `Random` (deterministik test).
> `FromElementSystemJson` MiniJson ile `crit_system` okur. `SkillResolution` overload'u
> `CritEligible`/`AdjectiveId`/`BaseDamage`/`DamageMult` kullanır.
>
> **Çağrılmıyor:** ManifestationDirector hâlâ `ClosingDamageMath` kullanıyor. İki yol nasıl
> birleşir (ClosingDamageMath'i değiştir / DamageCalculator'a geç / hibrit) **sahibine sorulacak**
> — bu görevin kapsamı değil; ClosingDamageMath ve ManifestationDirector'a dokunulmadı.
> `dotnet test` 163 yeşil (156+7 `DamageCalculatorTests`).

> **16 Eylül (5. tur) — SkillMotor okuma katmanı (passives/chains/zones/status + verb alanları).**
> `docs/gorev-listesi` motor tam uyum backlog'unun parse adımı: `VerbNode`/`SkillResolution`'a
> `CritEligible`/`ElementOrigin`/`DamageType`; yeni `PassiveNode`/`ChainNode`/
> `StatusInteractionNode`/`ZoneNode` + `ParsePassives`/`ParseChains`/`ParseStatusInteractions`/
> `ParseZones` (MiniJson, ActiveMode deseni). `SkillMotor.Passives`/`Chains`/
> `StatusInteractions`/`Zones`/`MaxActiveZones`. **Uygulama yok** — yalnızca okuma.
> `dotnet test` 156 yeşil (155+1). JSON sayıları: passives 10, chains 6, status satırları 17,
> zones 11, max_active_zones 5.

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

- **Görev 17 (element renk karşılaştırması) — sahibi için tamamlandı, RENK DEĞİŞTİRİLMEDİ.**
  `docs/prezentasyon-katmani.json` `vfx_binding.element_colors` ile `PrototypeTuning`'in
  mevcut `ElementFire/Water/Air/Earth/Light/Dark` alanları **hiç eşleşmiyor** — sadece
  "biraz farklı" değil, **muhtemelen karışmış**: mevcut `ElementEarth` (#9EC76B, yeşilimsi)
  yeni JSON'un `Hava`sına (#87a96b) neredeyse birebir yakın; mevcut `ElementDark` (#7A47C7,
  mor) yeni JSON'un `Toprak`ına (#877dd9) neredeyse birebir yakın. Bu, Rune enum'unun 16
  Eylül 4. turda yeniden adlandırılmasından (Igne/Suru/Kabuk/Zehir/Sarsinti/Toprak →
  Ateş/Su/Hava/Toprak/Aydınlık/Karanlık) ÖNCE atanmış renklerin, rename SIRASINDA doğru
  elemente taşınmamış olabileceğini düşündürüyor — ama bu bir varsayım, kanıtlanmadı.
  **Tam karşılaştırma tablosu:**

  | Element | Mevcut kod (`PrototypeTuning`) | Yeni JSON (`prezentasyon-katmani.json`) |
  |---|---|---|
  | Ateş | `ElementFire` #FF6B9E (sıcak magenta) | `primary` #C45C26 / `light` #FF9A3C (turuncu) |
  | Su | `ElementWater` #47B8FF (parlak mavi) | `primary` #39646A / `light` #5FB5D0 (koyu petrol) |
  | Hava | `ElementAir` #B8D1FF (açık mavi) | `primary` #87A96B / `light` #C5E0A8 (yeşilimsi) |
  | Toprak | `ElementEarth` #9EC76B (yeşil) | `primary` #877DD9 / `light` #B8AFEF (mor) |
  | Aydınlık | `ElementLight` #FFF5D1 (krem) | `primary` #C9A227 / `light` #FFE082 (altın sarısı) |
  | Karanlık | `ElementDark` #7A47C7 (mor) | `primary` #5C8A7D / `light` #7FB3A0 (yeşilimsi-gri) |

  **Karar sahibine kaldı:** hangisi otorite olacak? Mevcut kod zaten oynanışta kullanılıyor
  (skill tint, HUD), yeni JSON ise VFX/asset paketleriyle eşleşmek için tasarlanmış
  (`asset_example` alanlarına bakılırsa). İkisini birden tutmak (`primary` = kod rengi kalsın,
  `light` = parlak varyant olarak eklensin gibi) da bir seçenek. Değiştirilmedi, sadece
  rapor edildi (görev kuralı).

- **Ulti (`active_modes`) efektlerinin bir kısmı henüz dünyaya işlemiyor** (4. tur): JSON'daki
  `cast_time_mult`/`attack_speed_mult` okunuyor (`ActiveModeNode.GetEffect`) ama hiçbir yere
  uygulanmıyor. `dash_cooldown_mult`/`afterimage_count` (Fırtına Akışı) ve `taunt_radius_m`
  (Aşılmaz Duvar) hiç uygulanmıyor. `resource_cost` hiçbir modda düşülmüyor —
  `ResourceTracker`/`CooldownTracker` Core'da var (Görev 2) ama oyuna bağlanmadı; verb
  `base_resource_cost` hâlâ düşülmüyor.
  `visual.aura`/`screen_edges` okunmuyor — `ActiveModeHud` banner'ı var, ekran kenarı VFX yok.
- **"team_has_debuffs"/"team_has_wounded" basitleştirilmiş okunuyor** (4. tur): `AllyDummy`'nin
  `StatusBoard`'u yok, takım debuff sayısı yalnızca oyuncudan okunuyor. `team_full_cleanse` de
  yalnızca oyuncuyu temizliyor, ally'yi değil. "team_has_wounded" `min(oyuncu, ally)` can
  oranını doğru okuyor.
- **`docs/element-sistemi.json`'ın büyük kısmı motor tarafından hâlâ uygulanmıyor** —
  `verbs`/`adjectives`/`elements`/`scaling_economy.lengths`/`active_modes` dünyaya işliyor.
  **Okuma katmanı (5. tur) eklendi:** `passives`/`chain_mechanics`/
  `manipulation_layers.zone_layer` + `status_interaction_table` `SkillMotor`'da listeleniyor;
  verb `crit_eligible`/`element_origin`/`damage_type` parse+Resolve'da.
  **Görev 8:** `manipulation_layers.space_layer` de okunuyor (`SpaceEffects`/`MaxActiveLinks`);
  SkillMotionMotor hâlâ bağlanmadı (sayı sapmaları yukarıda — otorite sahibi).
  **`formulas`/`crit_system` kodu var ama bağlı değil (6. tur):** `DamageCalculator` yazıldı;
  canlı hasar hâlâ `ClosingDamageMath`. **ChainDirector (Görev 5) Core'da** ama
  ManifestationDirector'a bağlı değil.
  **Zone yaşam döngüsü Core'da var (Görev 7)** — `ZoneDirector` soft-cap/süre/movement;
  Game/Manifestation'a bağlı değil (görsel/hasar yok).
  **TimeEffectDirector (Görev 9) Core'da** — zamanlama hesabı var, cast/ölüm/zone'a bağlı değil.
  **RealityEffectDirector (Görev 10) Core'da** — revive_block/partial_erase/
  full_erase(shields); Game'e bağlı değil; revive_block PlayerVitals respawn'a bağlı değil
  (Faz 6). **PassiveDirector (Görev 4) Core'da** — tetik/süre/çoklu-aktif; Manifestation'a
  bağlı değil (Faz 6).
  `global_rules.resource_system`/`cooldown_rules` → Core sınıflar var, bağ yok;
  `status_durations` ↔ StatusTuning fark listesi (Görev 2, yukarıda; otorite açık).
  **Görev 3:** `state_machine` okunuyor (`PlayerStates`/`BossStates` + `PlayerStateMachine`;
  SentencePhase bağlanmadı).   **Görev 11:** `equipment_system` Core'da okunuyor
  (`EquipmentCatalog` 18 item + `EquipmentBonusResolver`); Game/hasar yoluna bağlı değil,
  envanter seçimi yok.   **Görev 13:** `prezentasyon-katmani.json` Core'da okunuyor
  (`PresentationCatalog` 16×16×10). **Görev 14:** `PresentationValidator` hitbox+animation
  doğrular; **`target_ally` hitbox prezentasyonda yok** (6 fiil). **Görev 16:**
  `PlaceholderFactory` trail/impact placeholder üretir; ManifestationDirector spawn
  bağlama yok (Faz 6). **Görev 15:** `AnimationBridge` frame-timer + Play hazır;
  Bootstrap'e bağlı değil. JSON `animator_state` (`Spell_Cast_*` / `Melee_*` / …)
  Quaternius controller state isimleriyle (`CastPierce` / `BasicStrike` / …)
  örtüşmüyor — eşleme Faz 6. Görev 17 (element_colors audit) sırada.
- **`SkillResolution.Hitbox` = `target_ally` prezentasyon katmanında yok (Görev 14):**
  `prezentasyon-katmani.json` hitbox_library'de `target_ally` id'si yok. Element fiilleri:
  `cc_arindirma`, `hiz_buff`, `kalkan_transferi`, `arindirma`, `kutsal_kalkan`, `dirilis`.
  Validator `HitboxFound=false` döner; sessiz atlama — hitbox ekleme / eşleme kararı açık.
  `status_interaction_table`
  uygulama artık `StatusReactionTable.Rebuild(motor.StatusInteractions)` — 3 özel satır
  (burn+poison / shield+burn / stun+knockback) hâlâ StatusBoard.Tick / Applicator'da.
  `atoms_catalog` /
  `all_verbs_atoms` / `atom_kombinasyonlari` kasıtlı okunmuyor.
- **Zone `RadiusM` JSON'da yok (Görev 7)** — `TrySpawn` çağıranı vermek zorunda; Game bağlama
  turunda tuning/config kararı lazım.
- **`ui_rules.zone_display` (Görev 12 notu)** — `in_world`, transparency 0.6; ZoneDirector henüz
  Game'e bağlı değil, görsel yazılmadı (Faz 6 / Zone Game bağlama).
- **`read_as_display.duration_ms` (1500) ≠ `FeelTuning.ReadoutHoldMs` (900)** — Görev 12'de
  not düşüldü, değiştirilmedi (sahibi otorite seçer).
- **space_layer'da 2 effect JSON'da var, oyunda karşılığı yok (Görev 8):**
  `karabasan_hat` (`invisible_link`) ve `hiclik_yarik` (`tear`) — yalnızca
  `SkillMotor.SpaceEffects` listesinde; yeni mekanik yazılmadı. `pus_gecisi`
  (`stealth_shift`) de ayrı tip olarak okunuyor ama gameplay hâlâ Pus→ShortBlink/
  Zenitsu rün yolundan gidiyor (space_layer'a bağlı değil).
- **`reality_layer` full_erase minions/summons (Görev 10):** oyunda minion/summon
  sistemi yok — uygulanamaz. Yalnızca `shields` uygulandı; uydurma minion sistemi
  kurulmadı.
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
