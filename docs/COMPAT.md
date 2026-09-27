# Element sistemi v6.1.1 uyumluluğu

`docs/element-sistemi.json` artık bağlayıcı v6.1.1 şemasıdır. Önceki v5.3 şeması
`docs/archive/element-sistemi-v5.3.json` altında saklanır.

`SkillMotor` v6.1.1 `runes`, `verb_base`, `adjective_mods` ve `skills.by_verb`
alanlarını doğrudan okur. Altı ekran noktası rün id'si değildir: `RuneLoadout`,
12 ründen tekrarsız 6 seçimi slotlara eşler. Varsayılan build JSON'daki ilk
`ana_classes_80` satırıdır; Core API 0-2 pasif rünle başka build kurabilir.

Silah çarpanları ve `uyumsuz_cizim` canlıdır. Zaman, hedef `Slow` / oyuncu `Haste`
durumuna gider; global zaman ölçeğine bağlanmaz. Elementler yüklenen isim/VFX boya
verisidir ve prototip hasar hesabını değiştirmez.

## Unity Editor doğrulaması

1. Play'e gir; Console'da `[ElementSystem] v6.1.1 loaded: 12 runes, 144 skills`
   satırını gör.
2. Altıgendeki iki rünü çiz; sonuç iki-rün skill adıyla kapanmalı.
3. Prototip Kılıç ile fiil 1/3/5/9 yeşil “Uyumlu”, diğer fiiller sarı
   “Uyumsuz — %80 etki” göstermeli.
4. Zaman için sahnedeki `PrototypeBootstrap` üzerinde `Prototype Main Class Id = 3`
   seç; tempo yalnız aktör status'larına gitmeli, `Time.timeScale` değişmemeli.

Henüz stub: build/silah/element seçim UI'ı, pasif rün efektlerinin tamamı,
`hitbox_vfx` prefabları ve tam `presentation` animasyon/VFX/ses bağları. Eksik
presentation asset'leri mevcut güvenli fallback yolunu kullanır. Core test projeleri
korundu; v5-only subsistem testleri arşiv v5.3 fixture'ına sabitlendi.
