# Element sistemi v6.1.1 uyumluluğu

`docs/element-sistemi.json` artık bağlayıcı v6.1.1 şemasıdır. Önceki v5.3 şeması
`docs/archive/element-sistemi-v5.3.json` altında saklanır.

`ElementSystemJsonLoader` canonical Resources JSON'un tek sahibidir. Aynı parse
sonucundan `SkillMotor`, `SkillFactory`, `RuneManager`, `AnimationDatabase` ve runtime
12 `RuneSO` / 10 `WeaponSO` / 6 `ElementSO` üretilir. Kalıcı asset için Unity'de
`Dovus → Import Element System v6.1.1 Assets` çalıştırılır.

Altı ekran noktası rün id'si değildir: `RuneManager`/`RuneLoadout`, 12 ründen
tekrarsız 6 seçimi slotlara ve 0-2 seçimi pasif yuvalara eşler. `SkillFactory`
fiil×sıfat ile 144 skill, seçili build için 36 skill üretir.

Silah çarpanları ve `uyumsuz_cizim` canlıdır. Zaman, hedef `Slow` / oyuncu `Haste`
durumuna gider; global zaman ölçeğine bağlanmaz. Elementler yüklenen isim/VFX boya
verisidir ve prototip hasar hesabını değiştirmez.

## Unity Editor doğrulaması

1. İsteğe bağlı Editor importer'ı çalıştır; Console'da
   `12 RuneSO / 10 WeaponSO / 6 ElementSO` tamamlandığını gör.
2. Play'e gir; Console'da `[JSONLoader] v6.1.1: 12 runes, 144 skills`
   satırını gör.
3. F1'e bas (veya `V6` → `TEST 1-1`); aynı normal cast hattından sonra Console'da
   `smoke 1-1 effect applied=True` ve boss hasarı görünmeli.
4. 1-1 cast'inde `AnimationDatabase` Kılıç×Saldırı adını önce mevcut controller
   clip'lerinde arar; özel clip yoksa `CastPierce` state'ine düşer;
   state/clip varsa animasyon oynamalı, yoksa cast sürüp binding uyarısı vermeli.
5. B veya `V6` paneli → listeden tam 6 rün → `SEÇİLİ 6'YI UYGULA`; altı slot etiketi
   değişmeli ve yeni iki-rün skill cast edilebilmeli.
6. E veya `ELEMENT DEĞİŞTİR`; sonraki skill başlığının element öneki değişmeli,
   hasar sayısı yalnız element yüzünden değişmemeli.
7. Prototip Kılıç ile fiil 1/3/5/9 yeşil “Uyumlu”, diğer fiiller sarı
   “Uyumsuz — %80 etki” göstermeli.
8. Zaman için sahnedeki `PrototypeBootstrap` üzerinde `Prototype Main Class Id = 3`
   seç; tempo yalnız aktör status'larına gitmeli, `Time.timeScale` değişmemeli.

Henüz stub: radial build/element menüleri, silah seçimi, pasif rün ve silah
identity-passive efektleri, `hitbox_vfx` prefabları, 120 özel clip/event ve tam
VFX/ses bağları. Eksik presentation asset'leri mevcut güvenli fallback yolunu kullanır.
Core test projeleri korundu; v5-only subsistem testleri arşiv v5.3 fixture'ına sabitlendi.
