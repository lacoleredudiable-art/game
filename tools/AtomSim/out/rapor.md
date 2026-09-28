# AtomSim raporu — 1440 skill

Kurallar: `docs/element-sistemi.json` → `mechanic_grammar`. Motor: `Core/Mechanic/MechanicGrammar` (oyunla aynı kod).

## Özet

- Plan: 1440 (12 fiil × 12 sıfat × 10 silah) + 120 sıfatsız taban
- A — sıfat nitel imzayı değiştirmiyor: **0**
- A2 — sıfat hiçbir etki atomunu değiştirmiyor (yalnız gövde): **19**
- B — aynı silahta aynı imzayı veren grup: **0**
- B2 — Kılıç'ta yol hariç aynı çekirdek: **0**
- C — bir skill 10 silahta ort. **10.00** farklı imza; yol sınıfına indirgeyince **10.00** (7 sınıf: yakın itiş, yay, dikey, uçan, hat, belirme, gövde)
- C (dürüst) — yol adı imzadan çıkarılınca bir skill 10 silahta ort. **9.90** farklı davranış
- D — çelişkili plan: **0**

### Silah çiftleri: yol adı dışında aynı davranan skill sayısı (144 üzerinden)

- Top = Tılsım: 9
- Çekiç = Tılsım: 6
- Yumruk = Hançer: 0
- Yumruk = Mızrak: 0
- Yumruk = Kılıç: 0
- Yumruk = Balta: 0
- Yumruk = Çekiç: 0
- Yumruk = Top: 0
- Yumruk = Asa: 0
- Yumruk = Tılsım: 0

- Özel mekanik etiketi olmayan (yalnız Delici/Seken/Dalga gibi genel desen): **463**

## Etiket sıklığı (1440 içinde)

| Etiket | Adet |
|---|---|
| Girdap | 120 |
| Seken | 120 |
| Dalga | 120 |
| Bağ | 120 |
| Sis | 120 |
| Yükselen | 120 |
| Güdümlü | 120 |
| Ayna eşli | 120 |
| Çift | 120 |
| Akış | 120 |
| Uzak yansıtıcı | 96 |
| Delici | 50 |
| Havaya atma | 40 |
| Kıskaç | 40 |
| Tuzak | 36 |
| Sersemletme | 36 |
| Gecikmeli an | 32 |
| Taşma | 30 |
| Emme/çalma | 30 |
| Totem | 30 |
| Can bağı | 30 |
| Koruyucu tetik | 30 |
| Ters kopya | 30 |
| Hedefi sana çekme | 22 |
| Sıçrayıp çakılma | 21 |
| Sert | 21 |
| Tasma | 12 |
| Ters kontrol | 12 |
| Bataklık | 12 |
| Dosta yansıtma | 12 |
| İnen akış alanı | 12 |
| Fırlatılma | 12 |
| Çit | 12 |
| Işınlanma | 12 |
| Kalkan hücumu | 12 |
| Faz geçişi | 10 |
| Sekmeli zıplama | 10 |
| İşaretle-geri dön | 10 |
| İniş şok dalgası | 10 |
| Yer değiştirme | 10 |
| Görünmez geçiş | 10 |
| Arkaya ışınlanma | 10 |
| Portal | 10 |
| Yem kopya | 10 |
| Süzülme | 10 |
| İçe çöküş | 10 |
| Bağla çekme | 10 |
| Akıntı | 10 |
| Durum aktarma | 10 |
| Mermi kesen engel | 10 |
| Arınma alanı | 10 |
| Bağışıklık bağı | 10 |
| Arınıp güçlenme | 10 |
| Tam arındırma | 10 |
| Buff silme | 10 |
| Mermi geri gönderme | 10 |
| Hasar emme | 10 |
| Ayna yüzey | 10 |
| Hasar yönlendirme | 10 |
| Kusursuz savuşturma | 10 |
| Bölünen yansıma | 10 |
| Can emen minyon | 10 |
| Zıplayan minyon | 10 |
| Taret | 10 |
| Minyon halkası | 10 |
| Muhafız | 10 |
| Görünmez minyon | 10 |
| Suikastçı minyon | 10 |
| Ayna klon | 10 |
| Klon | 10 |
| Zaman çalma | 10 |
| Dondurma | 10 |
| Zaman senkronu | 10 |
| Titreyen zaman | 10 |
| Hızlanma | 10 |
| Geri sarma | 10 |
| Yankı | 10 |
| Zaman alanı | 10 |
| Duvar | 8 |
| Mayın | 4 |
| Yerden taş duvar | 1 |
| Hat duvar | 1 |

## 144 skill — Kılıç (yay)

### 1 Saldırı

- *sıfatsız*: kılıç yayı → kapsül 1.8m, önünde yay. Etkiler: can→düşman -40
- **1-1 Yoğun Saldırı** — _Delici_ — kılıç yayı → kapsül 1m, önünde yay (delip geçer). Etkiler: can→düşman -54
- **1-2 Emici Saldırı** — _Girdap_ — kılıç yayı → kapsül 1.8m, önünde yay (içine çeker). Etkiler: can→düşman -38; can→sen 11.4 [aktarim]; mermi_sil→düşman mermisi [yut]
- **1-3 Sıçrayan Saldırı** — _Seken_ — kılıç yayı → kapsül 2m, önünde yay (2 kez seker). Etkiler: can→düşman -40 [seker]
- **1-4 Sabit Saldırı** — _Tuzak_ — kılıç yayı → kapsül 1.8m, önünde yay (yerinde çapalı, içine girene). Etkiler: can→düşman -40 [tuzak]
- **1-5 Yayılan Saldırı** — _Dalga_ — kılıç yayı → kapsül 3.2m, önünde yay (dışa büyür, cephe geçerken). Etkiler: can→düşman -34 [alan]
- **1-6 Bağlayıcı Saldırı** — _Bağ_ — kılıç yayı → bağ 1.8m, önünde yay (sen↔hedef bağı, tik tik). Etkiler: can→düşman -40 [bag_akisi]; hareket→düşman 1.5sn [bag_ucu]; hareket→düşman 0.7 1.5sn [bag_ucu,yavas]
- **1-7 Bulandırıcı Saldırı** — _Sis_ — kılıç yayı → bulut 1.8m, önünde yay (sis hacmi, tik tik). Etkiler: can→düşman -40 [bulut_tik]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **1-8 Yükselen Saldırı** — _Havaya atma, Yükselen_ — kılıç yayı → kapsül 1.8m, önünde yay (yerden yükselir, gücü artar). Etkiler: can→düşman -40 [havaya_at]; hareket→düşman 0.8sn [havada]; hasar_buff→sen 0.1 3sn [yukselen]
- **1-9 Odaklı Saldırı** — _Güdümlü_ — kılıç yayı → kapsül 1.8m, önünde yay (hedefe kilitli, tek hedef). Etkiler: can→düşman -46 [iskalamaz,zirh_yoksay]
- **1-10 Aynalı Saldırı** — _Kıskaç, Ayna eşli_ — kılıç yayı → kapsül 1.8m, önünde yay (karşı noktada eşi). Etkiler: can→düşman -40 [kiskac]; yansit→sen 0.3 2sn [ayna_sifati]
- **1-11 Kopya Saldırı** — _Çift_ — kılıç yayı → kapsül 1.8m, önünde yay (2 kez (kopya)). Etkiler: can→düşman -40 [iki_kez]
- **1-12 Akan Saldırı** — _Akış_ — kılıç yayı → kapsül 1.8m, önünde yay (sürekli akar, tik tik). Etkiler: can→düşman -28 3sn [akis]

### 2 İyileştirme

- *sıfatsız*: kılıç yayı → küre 2m, önünde yay (uyumsuz silah). Etkiler: can→dost 28
- **2-1 Yoğun İyileştirme** — _Taşma_ — kılıç yayı → küre 1.1m, önünde yay (tek hedef, uyumsuz silah). Etkiler: can→dost 37.8 [tasar]
- **2-2 Emici İyileştirme** — _Girdap, Emme/çalma_ — kılıç yayı → küre 2m, önünde yay (içine çeker, uyumsuz silah). Etkiler: can→düşman -26.6 [emme]; can→sen 26.6 [aktarim]; mermi_sil→düşman mermisi [yut]
- **2-3 Sıçrayan İyileştirme** — _Seken_ — kılıç yayı → küre 2.2m, önünde yay (2 kez seker, uyumsuz silah). Etkiler: can→dost 28 [dosttan_dosta]
- **2-4 Sabit İyileştirme** — _Totem_ — kılıç yayı → küre 2m, önünde yay (yerinde çapalı, tik tik, uyumsuz silah). Etkiler: can→dost 28 [totem]
- **2-5 Yayılan İyileştirme** — _Dalga_ — kılıç yayı → küre 3.6m, önünde yay (dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: can→dost 23.8 [alan]
- **2-6 Bağlayıcı İyileştirme** — _Can bağı, Bağ_ — kılıç yayı → bağ 2m, önünde yay (sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: can→dost 28 [can_bagi]; hasar_paylasimi→dost 0.5 1.5sn [bag]
- **2-7 Bulandırıcı İyileştirme** — _Sis_ — kılıç yayı → bulut 2m, önünde yay (sis hacmi, tik tik, uyumsuz silah). Etkiler: can→dost 28 [bulut_tik]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **2-8 Yükselen İyileştirme** — _Yükselen_ — kılıç yayı → küre 2m, önünde yay (yerden yükselir, gücü artar, uyumsuz silah). Etkiler: can→dost 28 [buyuyen]; hasar_buff→sen 0.1 3sn [yukselen]
- **2-9 Odaklı İyileştirme** — _Koruyucu tetik, Güdümlü_ — kılıç yayı → küre 2m, önünde yay (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: can→dost 32.2 [koruyucu_tetik]
- **2-10 Aynalı İyileştirme** — _Ters kopya, Ayna eşli_ — kılıç yayı → küre 2m, önünde yay (karşı noktada eşi, uyumsuz silah). Etkiler: can→dost 28; can→düşman -8.4 [ters_kopya]; yansit→sen 0.3 2sn [ayna_sifati]
- **2-11 Kopya İyileştirme** — _Çift_ — kılıç yayı → küre 2m, önünde yay (2 kez (kopya), uyumsuz silah). Etkiler: can→dost 28 [iki_kez]
- **2-12 Akan İyileştirme** — _Akış_ — kılıç yayı → küre 2m, önünde yay (sürekli akar, tik tik, uyumsuz silah). Etkiler: can→dost 19.6 3sn [akis]

### 3 Hareket

- *sıfatsız*: kılıç yayı → çizgi 3.6m, sende. Etkiler: kendini_tasi→sen 3 [yol:yay_kayma]
- **3-1 Yoğun Hareket** — _Faz geçişi_ — kılıç yayı → çizgi 2m, sende (tek hedef). Etkiler: kendini_tasi→sen 3 [faz,yol:yay_kayma]
- **3-2 Emici Hareket** — _Hedefi sana çekme, Girdap_ — kılıç yayı → çizgi 3.6m, sende (içine çeker). Etkiler: cek→düşman 3 [sana_dogru,yol:yay_kayma]; mermi_sil→düşman mermisi [yut]
- **3-3 Sıçrayan Hareket** — _Sekmeli zıplama, Seken_ — kılıç yayı → çizgi 4m, sende (2 kez seker). Etkiler: kendini_tasi→sen 3 [sekmeli,yol:yay_kayma]
- **3-4 Sabit Hareket** — _İşaretle-geri dön_ — kılıç yayı → çizgi 3.6m, sende (yerinde çapalı). Etkiler: isaret_geri_don→sen 3 2.2sn [geri_donus,yol:yay_kayma]
- **3-5 Yayılan Hareket** — _İniş şok dalgası, Dalga_ — kılıç yayı → çizgi 6.5m, sende (dışa büyür, cephe geçerken). Etkiler: kendini_tasi→sen 3 [inis_dalgasi,yol:yay_kayma]; it→düşman [dalga]
- **3-6 Bağlayıcı Hareket** — _Yer değiştirme, Bağ_ — kılıç yayı → bağ 3.6m, sende (sen↔hedef bağı, tik tik). Etkiler: yer_degistir→düşman 3 [iki_uc,yol:yay_kayma]; hareket→düşman 1.5sn [bag_ucu]; hareket→düşman 0.7 1.5sn [bag_ucu,yavas]
- **3-7 Bulandırıcı Hareket** — _Görünmez geçiş, Sis_ — kılıç yayı → bulut 3.6m, sende (sis hacmi, tik tik). Etkiler: kendini_tasi→sen 3 [gorunmez_gecis,yol:yay_kayma]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **3-8 Yükselen Hareket** — _Sıçrayıp çakılma, Yükselen_ — kılıç yayı → çizgi 3.6m, sende (yerden yükselir, gücü artar). Etkiler: kendini_tasi→sen 3 [sicrayip_cakil,yol:yay_kayma]; hasar_buff→sen 0.1 3sn [yukselen]
- **3-9 Odaklı Hareket** — _Arkaya ışınlanma, Güdümlü_ — kılıç yayı → çizgi 3.6m, sende (hedefe kilitli, tek hedef). Etkiler: hedefin_arkasina→sen 3 [isinlanma,yol:yay_kayma]
- **3-10 Aynalı Hareket** — _Portal, Ayna eşli_ — kılıç yayı → çizgi 3.6m, sende (karşı noktada eşi). Etkiler: portal→sen 3 4sn [portal_cifti,yol:yay_kayma]; yansit→sen 0.3 2sn [ayna_sifati]
- **3-11 Kopya Hareket** — _Yem kopya, Çift_ — kılıç yayı → çizgi 3.6m, sende (2 kez (kopya)). Etkiler: kendini_tasi→sen 3 [ardinda_kopya,yol:yay_kayma]; yem_kopya→sen 1 2sn [dikkat_ceker]
- **3-12 Akan Hareket** — _Süzülme, Akış_ — kılıç yayı → çizgi 3.6m, sende (sürekli akar, tik tik). Etkiler: kendini_tasi→sen 3 3sn [suzulme,yol:yay_kayma]

### 4 Savunma

- *sıfatsız*: kılıç yayı → küre 1.5m, önünde yay (katı, uyumsuz silah). Etkiler: kalkan→dost 40
- **4-1 Yoğun Savunma** — _Taşma_ — kılıç yayı → küre 0.8m, önünde yay (katı, tek hedef, uyumsuz silah). Etkiler: kalkan→dost 54 [tasar]
- **4-2 Emici Savunma** — _Girdap, Emme/çalma_ — kılıç yayı → küre 1.5m, önünde yay (katı, içine çeker, uyumsuz silah). Etkiler: can→düşman -38 [emme]; kalkan→sen 38 [aktarim]; mermi_sil→düşman mermisi [yut]
- **4-3 Sıçrayan Savunma** — _Seken_ — kılıç yayı → küre 1.7m, önünde yay (katı, 2 kez seker, uyumsuz silah). Etkiler: kalkan→dost 40 [dosttan_dosta]
- **4-4 Sabit Savunma** — _Duvar, Totem_ — kılıç yayı → küre 1.5m, önünde yay (yerinde çapalı, katı, tik tik, uyumsuz silah). Etkiler: kalkan→dost 40 [totem]
- **4-5 Yayılan Savunma** — _Dalga_ — kılıç yayı → küre 2.7m, önünde yay (katı, dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: kalkan→dost 34 [alan]
- **4-6 Bağlayıcı Savunma** — _Can bağı, Bağ_ — kılıç yayı → bağ 1.5m, önünde yay (katı, sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: kalkan→dost 40 [can_bagi]; hasar_paylasimi→dost 0.5 1.5sn [bag]
- **4-7 Bulandırıcı Savunma** — _Sis_ — kılıç yayı → bulut 1.5m, önünde yay (katı, sis hacmi, tik tik, uyumsuz silah). Etkiler: kalkan→dost 40 [bulut_tik]; gizlen→dost 3.5sn [bulut_ici]; kor→düşman 0.3 3.5sn [bulut_ici]
- **4-8 Yükselen Savunma** — _Yükselen_ — kılıç yayı → küre 1.5m, önünde yay (katı, yerden yükselir, gücü artar, uyumsuz silah). Etkiler: kalkan→dost 40 [buyuyen]; hasar_buff→sen 0.1 3sn [yukselen]
- **4-9 Odaklı Savunma** — _Koruyucu tetik, Güdümlü_ — kılıç yayı → küre 1.5m, önünde yay (katı, hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: kalkan→dost 46 [koruyucu_tetik]
- **4-10 Aynalı Savunma** — _Ters kopya, Ayna eşli_ — kılıç yayı → küre 1.5m, önünde yay (katı, karşı noktada eşi, uyumsuz silah). Etkiler: kalkan→dost 40; can→düşman -12 [ters_kopya]; yansit→sen 0.3 2sn [ayna_sifati]
- **4-11 Kopya Savunma** — _Çift_ — kılıç yayı → küre 1.5m, önünde yay (katı, 2 kez (kopya), uyumsuz silah). Etkiler: kalkan→dost 40 [iki_kez]
- **4-12 Akan Savunma** — _Akış_ — kılıç yayı → küre 1.5m, önünde yay (katı, sürekli akar, tik tik, uyumsuz silah). Etkiler: kalkan→dost 28 3sn [akis]

### 5 Patlama

- *sıfatsız*: kılıç yayı → küre 3.6m, önünde yay. Etkiler: can→düşman -28; it→düşman
- **5-1 Yoğun Patlama** — _Delici_ — kılıç yayı → küre 2m, önünde yay (delip geçer). Etkiler: can→düşman -37.8; it→düşman [sert]
- **5-2 Emici Patlama** — _İçe çöküş, Girdap_ — kılıç yayı → küre 3.6m, önünde yay (içine çeker). Etkiler: can→düşman -26.6; cek→düşman [merkeze]; can→sen 7.98 [aktarim]; mermi_sil→düşman mermisi [yut]
- **5-3 Sıçrayan Patlama** — _Seken_ — kılıç yayı → küre 4m, önünde yay (2 kez seker). Etkiler: can→düşman -28 [seker]; it→düşman [seker]
- **5-4 Sabit Patlama** — _Tuzak_ — kılıç yayı → küre 3.6m, önünde yay (yerinde çapalı, içine girene). Etkiler: can→düşman -28 [tuzak]; it→düşman [tuzak]
- **5-5 Yayılan Patlama** — _Dalga_ — kılıç yayı → küre 6.5m, önünde yay (dışa büyür, cephe geçerken). Etkiler: can→düşman -23.8 [alan]; it→düşman [dalga]
- **5-6 Bağlayıcı Patlama** — _Bağla çekme, Bağ_ — kılıç yayı → bağ 3.6m, önünde yay (sen↔hedef bağı, tik tik). Etkiler: can→düşman -28 [bag_akisi]; cek→düşman [bag_boyunca]; hareket→düşman 1.5sn [bag_ucu]; hareket→düşman 0.7 1.5sn [bag_ucu,yavas]
- **5-7 Bulandırıcı Patlama** — _Sis_ — kılıç yayı → bulut 3.6m, önünde yay (sis hacmi, tik tik). Etkiler: can→düşman -28 [bulut_tik]; it→düşman [sis_patlamasi]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **5-8 Yükselen Patlama** — _Havaya atma, Yükselen_ — kılıç yayı → küre 3.6m, önünde yay (yerden yükselir, gücü artar). Etkiler: can→düşman -28 [havaya_at]; it→düşman [yukari_firlat]; hareket→düşman 0.8sn [havada]; hasar_buff→sen 0.1 3sn [yukselen]
- **5-9 Odaklı Patlama** — _Güdümlü_ — kılıç yayı → küre 3.6m, önünde yay (hedefe kilitli, tek hedef). Etkiler: can→düşman -32.2 [iskalamaz,zirh_yoksay]; it→düşman [tek_hedef]
- **5-10 Aynalı Patlama** — _Kıskaç, Ayna eşli_ — kılıç yayı → küre 3.6m, önünde yay (karşı noktada eşi). Etkiler: can→düşman -28 [kiskac]; it→düşman [iki_uctan]; yansit→sen 0.3 2sn [ayna_sifati]
- **5-11 Kopya Patlama** — _Çift_ — kılıç yayı → küre 3.6m, önünde yay (2 kez (kopya)). Etkiler: can→düşman -28 [iki_kez]; it→düşman [iki_kez]
- **5-12 Akan Patlama** — _Akıntı, Akış_ — kılıç yayı → küre 3.6m, önünde yay (sürekli akar, tik tik). Etkiler: can→düşman -19.6 3sn [akis]; it→düşman [akinti]

### 6 Kontrol

- *sıfatsız*: kılıç yayı → koni 4m, önünde yay (uyumsuz silah). Etkiler: hareket→düşman 1.5sn; can→düşman -6.4
- **6-1 Yoğun Kontrol** — _Delici_ — kılıç yayı → koni 2.2m, önünde yay (delip geçer, uyumsuz silah). Etkiler: hareket→düşman 0.8sn [sert]; can→düşman -8.64
- **6-2 Emici Kontrol** — _Hedefi sana çekme, Girdap_ — kılıç yayı → koni 4m, önünde yay (içine çeker, uyumsuz silah). Etkiler: hareket→düşman 1.5sn; can→düşman -6.08; cek→düşman [sana_dogru]; can→sen 1.82 [aktarim]; mermi_sil→düşman mermisi [yut]
- **6-3 Sıçrayan Kontrol** — _Seken_ — kılıç yayı → koni 4.4m, önünde yay (2 kez seker, uyumsuz silah). Etkiler: hareket→düşman 1.5sn [seker]; can→düşman -6.4 [seker]
- **6-4 Sabit Kontrol** — _Tuzak_ — kılıç yayı → koni 4m, önünde yay (yerinde çapalı, içine girene, uyumsuz silah). Etkiler: hareket→düşman 3.5sn [uzun]; can→düşman -6.4 [tuzak]
- **6-5 Yayılan Kontrol** — _Dalga_ — kılıç yayı → koni 7.2m, önünde yay (dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: hareket→düşman 1.5sn [dalga]; can→düşman -5.44 [alan]
- **6-6 Bağlayıcı Kontrol** — _Tasma, Bağ_ — kılıç yayı → bağ 4m, önünde yay (sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: hareket→düşman 1.5sn [tasma]; can→düşman -6.4 [bag_akisi]; hareket→düşman 1.5sn [bag_ucu]; hareket→düşman 0.7 1.5sn [bag_ucu,yavas]
- **6-7 Bulandırıcı Kontrol** — _Sis_ — kılıç yayı → bulut 4m, önünde yay (sis hacmi, tik tik, uyumsuz silah). Etkiler: hareket→düşman 1.5sn [bulut_tik]; can→düşman -6.4 [bulut_tik]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **6-8 Yükselen Kontrol** — _Havaya atma, Yükselen_ — kılıç yayı → koni 4m, önünde yay (yerden yükselir, gücü artar, uyumsuz silah). Etkiler: hareket→düşman 1.5sn [havaya_at]; can→düşman -6.4 [havaya_at]; hasar_buff→sen 0.1 3sn [yukselen]
- **6-9 Odaklı Kontrol** — _Gecikmeli an, Güdümlü_ — kılıç yayı → koni 4m, önünde yay (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: hareket→düşman 1.5sn [isaretli_an]; can→düşman -7.36 [iskalamaz,zirh_yoksay]
- **6-10 Aynalı Kontrol** — _Kıskaç, Ters kontrol, Ayna eşli_ — kılıç yayı → koni 4m, önünde yay (karşı noktada eşi, uyumsuz silah). Etkiler: hareket→düşman 1.5sn [ters_kontrol]; can→düşman -6.4 [kiskac]; yansit→sen 0.3 2sn [ayna_sifati]
- **6-11 Kopya Kontrol** — _Çift_ — kılıç yayı → koni 4m, önünde yay (2 kez (kopya), uyumsuz silah). Etkiler: hareket→düşman 1.5sn [iki_kez]; can→düşman -6.4 [iki_kez]
- **6-12 Akan Kontrol** — _Bataklık, Akış_ — kılıç yayı → koni 4m, önünde yay (sürekli akar, tik tik, uyumsuz silah). Etkiler: hareket→düşman 3sn [bataklik]; can→düşman -4.48 3sn [akis]

### 7 Zayıflatma

- *sıfatsız*: kılıç yayı → kapsül 3m, önünde yay (uyumsuz silah). Etkiler: zirh→düşman -0.16 4sn; can→düşman -4
- **7-1 Yoğun Zayıflatma** — _Delici_ — kılıç yayı → kapsül 1.7m, önünde yay (delip geçer, uyumsuz silah). Etkiler: zirh→düşman -0.22 2sn; can→düşman -5.4
- **7-2 Emici Zayıflatma** — _Girdap_ — kılıç yayı → kapsül 3m, önünde yay (içine çeker, uyumsuz silah). Etkiler: zirh→düşman -0.15 4sn; can→düşman -3.8; zirh→sen 0.05 4sn [aktarim]; can→sen 1.14 [aktarim]; mermi_sil→düşman mermisi [yut]
- **7-3 Sıçrayan Zayıflatma** — _Seken_ — kılıç yayı → kapsül 3.3m, önünde yay (2 kez seker, uyumsuz silah). Etkiler: zirh→düşman -0.16 4sn [seker]; can→düşman -4 [seker]
- **7-4 Sabit Zayıflatma** — _Tuzak_ — kılıç yayı → kapsül 3m, önünde yay (yerinde çapalı, içine girene, uyumsuz silah). Etkiler: zirh→düşman -0.16 4sn [tuzak]; can→düşman -4 [tuzak]
- **7-5 Yayılan Zayıflatma** — _Dalga_ — kılıç yayı → kapsül 5.4m, önünde yay (dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: zirh→düşman -0.14 4sn [alan]; can→düşman -3.4 [alan]
- **7-6 Bağlayıcı Zayıflatma** — _Bağ_ — kılıç yayı → bağ 3m, önünde yay (sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: zirh→düşman -0.16 4sn [bag_akisi]; can→düşman -4 [bag_akisi]; hareket→düşman 1.5sn [bag_ucu]; hareket→düşman 0.7 1.5sn [bag_ucu,yavas]
- **7-7 Bulandırıcı Zayıflatma** — _Sis_ — kılıç yayı → bulut 3m, önünde yay (sis hacmi, tik tik, uyumsuz silah). Etkiler: zirh→düşman -0.16 4sn [bulut_tik]; can→düşman -4 [bulut_tik]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **7-8 Yükselen Zayıflatma** — _Havaya atma, Yükselen_ — kılıç yayı → kapsül 3m, önünde yay (yerden yükselir, gücü artar, uyumsuz silah). Etkiler: zirh→düşman -0.16 4sn [havaya_at]; can→düşman -4 [havaya_at]; hareket→düşman 0.8sn [havada]; hasar_buff→sen 0.1 3sn [yukselen]
- **7-9 Odaklı Zayıflatma** — _Gecikmeli an, Güdümlü_ — kılıç yayı → kapsül 3m, önünde yay (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: zirh→düşman -0.18 4sn [isaretli_an]; can→düşman -4.6 [iskalamaz,zirh_yoksay]
- **7-10 Aynalı Zayıflatma** — _Kıskaç, Ayna eşli_ — kılıç yayı → kapsül 3m, önünde yay (karşı noktada eşi, uyumsuz silah). Etkiler: zirh→düşman -0.16 4sn [kiskac]; can→düşman -4 [kiskac]; yansit→sen 0.3 2sn [ayna_sifati]
- **7-11 Kopya Zayıflatma** — _Çift_ — kılıç yayı → kapsül 3m, önünde yay (2 kez (kopya), uyumsuz silah). Etkiler: zirh→düşman -0.16 4sn [iki_kez]; can→düşman -4 [iki_kez]
- **7-12 Akan Zayıflatma** — _Akış_ — kılıç yayı → kapsül 3m, önünde yay (sürekli akar, tik tik, uyumsuz silah). Etkiler: zirh→düşman -0.11 3sn [akis]; can→düşman -2.8 3sn [akis]

### 8 Güçlendirme

- *sıfatsız*: kılıç yayı → küre 2m, önünde yay (uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn
- **8-1 Yoğun Güçlendirme** — _Taşma_ — kılıç yayı → küre 1.1m, önünde yay (tek hedef, uyumsuz silah). Etkiler: hasar_buff→dost 0.22 1.5sn [tasar]
- **8-2 Emici Güçlendirme** — _Girdap, Emme/çalma_ — kılıç yayı → küre 2m, önünde yay (içine çeker, uyumsuz silah). Etkiler: hasar_buff→düşman -0.15 3sn [emme]; hasar_buff→sen 0.15 3sn [aktarim]; mermi_sil→düşman mermisi [yut]
- **8-3 Sıçrayan Güçlendirme** — _Seken_ — kılıç yayı → küre 2.2m, önünde yay (2 kez seker, uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn [dosttan_dosta]
- **8-4 Sabit Güçlendirme** — _Totem_ — kılıç yayı → küre 2m, önünde yay (yerinde çapalı, tik tik, uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn [totem]
- **8-5 Yayılan Güçlendirme** — _Dalga_ — kılıç yayı → küre 3.6m, önünde yay (dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: hasar_buff→dost 0.14 3sn [alan]
- **8-6 Bağlayıcı Güçlendirme** — _Can bağı, Bağ_ — kılıç yayı → bağ 2m, önünde yay (sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn [can_bagi]; hasar_paylasimi→dost 0.5 1.5sn [bag]
- **8-7 Bulandırıcı Güçlendirme** — _Sis_ — kılıç yayı → bulut 2m, önünde yay (sis hacmi, tik tik, uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn [bulut_tik]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **8-8 Yükselen Güçlendirme** — _Yükselen_ — kılıç yayı → küre 2m, önünde yay (yerden yükselir, gücü artar, uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn [buyuyen]; hasar_buff→sen 0.1 3sn [yukselen]
- **8-9 Odaklı Güçlendirme** — _Koruyucu tetik, Güdümlü_ — kılıç yayı → küre 2m, önünde yay (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: hasar_buff→dost 0.18 3sn [koruyucu_tetik]
- **8-10 Aynalı Güçlendirme** — _Ters kopya, Ayna eşli_ — kılıç yayı → küre 2m, önünde yay (karşı noktada eşi, uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn; hasar_buff→düşman -0.05 3sn [ters_kopya]; yansit→sen 0.3 2sn [ayna_sifati]
- **8-11 Kopya Güçlendirme** — _Çift_ — kılıç yayı → küre 2m, önünde yay (2 kez (kopya), uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn [iki_kez]
- **8-12 Akan Güçlendirme** — _Akış_ — kılıç yayı → küre 2m, önünde yay (sürekli akar, tik tik, uyumsuz silah). Etkiler: hasar_buff→dost 0.11 3sn [akis]

### 9 Arındırma

- *sıfatsız*: kılıç yayı → küre 3m, önünde yay. Etkiler: durum_sil→dost 1; mermi_sil→düşman mermisi
- **9-1 Yoğun Arındırma** — _Sert_ — kılıç yayı → küre 1.7m, önünde yay (tek hedef). Etkiler: durum_sil→dost 1 [guclu]; mermi_sil→düşman mermisi [delici]
- **9-2 Emici Arındırma** — _Girdap, Durum aktarma_ — kılıç yayı → küre 3m, önünde yay (içine çeker). Etkiler: durum_aktar→dost 1; mermi_sil→düşman mermisi [yut]; durum_ekle→düşman 1 [aktarim]
- **9-3 Sıçrayan Arındırma** — _Seken_ — kılıç yayı → küre 3.3m, önünde yay (2 kez seker). Etkiler: durum_sil→dost 1 [dosttan_dosta]; mermi_sil→düşman mermisi [seker]
- **9-4 Sabit Arındırma** — _Mermi kesen engel, Arınma alanı_ — kılıç yayı → küre 3m, önünde yay (yerinde çapalı). Etkiler: durum_sil→dost 1 [arinma_alani]; mermi_sil→düşman mermisi [engel]
- **9-5 Yayılan Arındırma** — _Dalga_ — kılıç yayı → küre 5.4m, önünde yay (dışa büyür, cephe geçerken). Etkiler: durum_sil→dost 1 [alan]; mermi_sil→düşman mermisi [alan]
- **9-6 Bağlayıcı Arındırma** — _Bağışıklık bağı, Bağ_ — kılıç yayı → bağ 3m, önünde yay (sen↔hedef bağı, tik tik). Etkiler: durum_sil→dost 1 [bag_bagisiklik]; mermi_sil→düşman mermisi [bag_hatti]
- **9-7 Bulandırıcı Arındırma** — _Sis_ — kılıç yayı → bulut 3m, önünde yay (sis hacmi, tik tik). Etkiler: durum_sil→dost 1 [gizli]; mermi_sil→düşman mermisi [sis_perdesi]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **9-8 Yükselen Arındırma** — _Arınıp güçlenme, Yükselen_ — kılıç yayı → küre 3m, önünde yay (yerden yükselir, gücü artar). Etkiler: durum_sil→dost 1 [guce_cevir]; mermi_sil→düşman mermisi [yukselen_perde]; hasar_buff→sen 0.1 3sn [yukselen]
- **9-9 Odaklı Arındırma** — _Tam arındırma, Güdümlü_ — kılıç yayı → küre 3m, önünde yay (hedefe kilitli, tek hedef). Etkiler: durum_sil→dost 99 [tumunu_sil]; mermi_sil→düşman mermisi [hedefli]
- **9-10 Aynalı Arındırma** — _Buff silme, Mermi geri gönderme, Ayna eşli_ — kılıç yayı → küre 3m, önünde yay (karşı noktada eşi). Etkiler: iyi_durum_sil→düşman 1 [ters_hedef]; mermi_sil→düşman mermisi [geri_gonder]; yansit→sen 0.3 2sn [ayna_sifati]
- **9-11 Kopya Arındırma** — _Çift_ — kılıç yayı → küre 3m, önünde yay (2 kez (kopya)). Etkiler: durum_sil→dost 1 [iki_kez]; mermi_sil→düşman mermisi [iki_kez]
- **9-12 Akan Arındırma** — _Akış_ — kılıç yayı → küre 3m, önünde yay (sürekli akar, tik tik). Etkiler: durum_sil→dost 1 [aura]; mermi_sil→düşman mermisi [surekli_perde]

### 10 Yansıma

- *sıfatsız*: kılıç yayı → küre 1.5m, önünde yay (uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [dunyada,silahla:yay]
- **10-1 Yoğun Yansıma** — _Uzak yansıtıcı_ — kılıç yayı → küre 0.8m, önünde yay (tek hedef, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.68 1sn [dunyada,sert,silahla:yay]
- **10-2 Emici Yansıma** — _Girdap, Uzak yansıtıcı, Hasar emme_ — kılıç yayı → küre 1.5m, önünde yay (içine çeker, uyumsuz silah). Etkiler: em→gövdenin olduğu yer 0.5 2sn [cana_cevir,dunyada,silahla:yay]; mermi_sil→düşman mermisi [yut]
- **10-3 Sıçrayan Yansıma** — _Uzak yansıtıcı, Seken_ — kılıç yayı → küre 1.7m, önünde yay (2 kez seker, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [dunyada,seker,silahla:yay]
- **10-4 Sabit Yansıma** — _Ayna yüzey, Uzak yansıtıcı_ — kılıç yayı → küre 1.5m, önünde yay (yerinde çapalı, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [ayna_yuzey,dunyada,silahla:yay]
- **10-5 Yayılan Yansıma** — _Uzak yansıtıcı, Dalga_ — kılıç yayı → küre 2.7m, önünde yay (dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [aura,dunyada,silahla:yay]
- **10-6 Bağlayıcı Yansıma** — _Uzak yansıtıcı, Hasar yönlendirme, Bağ_ — kılıç yayı → bağ 1.5m, önünde yay (sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: yonlendir→düşman 0.5 2sn [bag,dunyada,silahla:yay]; hareket→düşman 1.5sn [bag_ucu]; hareket→düşman 0.7 2sn [bag_ucu,yavas]
- **10-7 Bulandırıcı Yansıma** — _Sis, Uzak yansıtıcı_ — kılıç yayı → bulut 1.5m, önünde yay (sis hacmi, tik tik, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [dunyada,gizli,silahla:yay]; gizlen→dost 5sn [bulut_ici]; kor→düşman 0.3 5sn [bulut_ici]
- **10-8 Yükselen Yansıma** — _Uzak yansıtıcı, Yükselen_ — kılıç yayı → küre 1.5m, önünde yay (yerden yükselir, gücü artar, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [artan_oran,dunyada,silahla:yay]; hasar_buff→sen 0.1 3sn [yukselen]
- **10-9 Odaklı Yansıma** — _Uzak yansıtıcı, Kusursuz savuşturma, Güdümlü_ — kılıç yayı → küre 1.5m, önünde yay (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 1 2sn [dunyada,savusturma,silahla:yay]
- **10-10 Aynalı Yansıma** — _Uzak yansıtıcı, Bölünen yansıma, Ayna eşli_ — kılıç yayı → küre 1.5m, önünde yay (karşı noktada eşi, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [bolunen,dunyada,silahla:yay]; yansit→sen 0.3 2sn [ayna_sifati]
- **10-11 Kopya Yansıma** — _Uzak yansıtıcı, Çift_ — kılıç yayı → küre 1.5m, önünde yay (2 kez (kopya), uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [dunyada,iki_kez,silahla:yay]
- **10-12 Akan Yansıma** — _Uzak yansıtıcı, Akış_ — kılıç yayı → küre 1.5m, önünde yay (sürekli akar, tik tik, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 3sn [dunyada,silahla:yay,surekli]

### 11 Çağırma

- *sıfatsız*: kılıç yayı → nokta 1m, önünde yay (uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [silahla:yay]
- **11-1 Yoğun Çağırma** — _Sert_ — kılıç yayı → nokta 0.6m, önünde yay (tek hedef, uyumsuz silah). Etkiler: aktor_yarat→sen 1 2.5sn [guclu,silahla:yay]
- **11-2 Emici Çağırma** — _Can emen minyon, Girdap_ — kılıç yayı → nokta 1m, önünde yay (içine çeker, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [can_emen,silahla:yay]; mermi_sil→düşman mermisi [yut]
- **11-3 Sıçrayan Çağırma** — _Zıplayan minyon, Seken_ — kılıç yayı → nokta 1.1m, önünde yay (2 kez seker, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [silahla:yay,ziplayan]
- **11-4 Sabit Çağırma** — _Taret_ — kılıç yayı → nokta 1m, önünde yay (yerinde çapalı, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [silahla:yay,taret]
- **11-5 Yayılan Çağırma** — _Minyon halkası, Dalga_ — kılıç yayı → nokta 1.8m, önünde yay (dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: aktor_yarat→sen 3 5sn [halka,silahla:yay]
- **11-6 Bağlayıcı Çağırma** — _Muhafız, Bağ_ — kılıç yayı → bağ 1m, önünde yay (sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [bagli_muhafiz,silahla:yay]
- **11-7 Bulandırıcı Çağırma** — _Görünmez minyon, Sis_ — kılıç yayı → bulut 1m, önünde yay (sis hacmi, tik tik, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [gorunmez,silahla:yay]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **11-8 Yükselen Çağırma** — _Yükselen_ — kılıç yayı → nokta 1m, önünde yay (yerden yükselir, gücü artar, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [buyuyen,silahla:yay]; hasar_buff→sen 0.1 3sn [yukselen]
- **11-9 Odaklı Çağırma** — _Suikastçı minyon, Güdümlü_ — kılıç yayı → nokta 1m, önünde yay (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [silahla:yay,suikastci]
- **11-10 Aynalı Çağırma** — _Ayna klon, Ayna eşli_ — kılıç yayı → nokta 1m, önünde yay (karşı noktada eşi, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [ayna_klon,silahla:yay]; yansit→sen 0.3 2sn [ayna_sifati]
- **11-11 Kopya Çağırma** — _Klon, Çift_ — kılıç yayı → nokta 1m, önünde yay (2 kez (kopya), uyumsuz silah). Etkiler: klon→sen 1 5sn [senin_kopyan,silahla:yay]
- **11-12 Akan Çağırma** — _Akış_ — kılıç yayı → nokta 1m, önünde yay (sürekli akar, tik tik, uyumsuz silah). Etkiler: aktor_yarat→sen 3 5sn [akis,silahla:yay]

### 12 Zaman

- *sıfatsız*: kılıç yayı → silindir 4m, önünde yay (uyumsuz silah). Etkiler: tempo→düşman 0.7 1sn
- **12-1 Yoğun Zaman** — _Delici_ — kılıç yayı → silindir 2.2m, önünde yay (delip geçer, uyumsuz silah). Etkiler: tempo→düşman 0.3 0.5sn [sert]
- **12-2 Emici Zaman** — _Zaman çalma, Girdap_ — kılıç yayı → silindir 4m, önünde yay (içine çeker, uyumsuz silah). Etkiler: tempo→düşman 0.7 1sn; tempo→sen 1.3 1sn [aktarim]; mermi_sil→düşman mermisi [yut]
- **12-3 Sıçrayan Zaman** — _Seken_ — kılıç yayı → silindir 4.4m, önünde yay (2 kez seker, uyumsuz silah). Etkiler: tempo→düşman 0.7 1sn [seker]
- **12-4 Sabit Zaman** — _Dondurma_ — kılıç yayı → silindir 4m, önünde yay (yerinde çapalı, uyumsuz silah). Etkiler: tempo→düşman 1sn [dondur]
- **12-5 Yayılan Zaman** — _Dalga_ — kılıç yayı → silindir 7.2m, önünde yay (dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: tempo→düşman 0.7 1sn [dalga]
- **12-6 Bağlayıcı Zaman** — _Zaman senkronu, Bağ_ — kılıç yayı → bağ 4m, önünde yay (sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: tempo→düşman 0.7 1sn [senkron]; hareket→düşman 1.5sn [bag_ucu]; hareket→düşman 0.7 1.5sn [bag_ucu,yavas]
- **12-7 Bulandırıcı Zaman** — _Titreyen zaman, Sis_ — kılıç yayı → bulut 4m, önünde yay (sis hacmi, tik tik, uyumsuz silah). Etkiler: tempo→düşman 0.7 1sn [titrer]; gizlen→dost 4sn [bulut_ici]; kor→düşman 0.3 4sn [bulut_ici]
- **12-8 Yükselen Zaman** — _Hızlanma, Yükselen_ — kılıç yayı → silindir 4m, önünde yay (yerden yükselir, gücü artar, uyumsuz silah). Etkiler: tempo→sen 1.3 1sn [hizlanma]; hasar_buff→sen 0.1 3sn [yukselen]
- **12-9 Odaklı Zaman** — _Gecikmeli an, Güdümlü_ — kılıç yayı → silindir 4m, önünde yay (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: tempo→düşman 0.7 1sn [isaretli_an]
- **12-10 Aynalı Zaman** — _Geri sarma, Ayna eşli_ — kılıç yayı → silindir 4m, önünde yay (karşı noktada eşi, uyumsuz silah). Etkiler: geri_sar→düşman 2 1sn [geri_sarma]; yansit→sen 0.3 2sn [ayna_sifati]
- **12-11 Kopya Zaman** — _Yankı, Çift_ — kılıç yayı → silindir 4m, önünde yay (2 kez (kopya), uyumsuz silah). Etkiler: tempo→düşman 0.7 1sn [iki_kez]; onceki_skill_tekrar→sen 1 [yanki]
- **12-12 Akan Zaman** — _Zaman alanı, Akış_ — kılıç yayı → silindir 4m, önünde yay (sürekli akar, tik tik, uyumsuz silah). Etkiler: tempo→düşman 0.7 3sn [zaman_alani]; tempo→dost 1.3 3sn [zaman_alani]

## 144 skill — Top (balistik)

### 1 Saldırı

- *sıfatsız*: top mermisi (yay çizip düşer) → kapsül 1.5m, hedef noktada (uyumsuz silah). Etkiler: can→düşman -57.6
- **1-1 Yoğun Saldırı** — _Delici_ — top mermisi (yay çizip düşer) → kapsül 0.8m, hedef noktada (delip geçer, uyumsuz silah). Etkiler: can→düşman -77.76
- **1-2 Emici Saldırı** — _Girdap_ — top mermisi (yay çizip düşer) → kapsül 1.5m, hedef noktada (içine çeker, uyumsuz silah). Etkiler: can→düşman -54.72; can→sen 16.42 [aktarim]; mermi_sil→düşman mermisi [yut]
- **1-3 Sıçrayan Saldırı** — _Seken_ — top mermisi (yay çizip düşer) → kapsül 1.7m, hedef noktada (2 kez seker, uyumsuz silah). Etkiler: can→düşman -57.6 [seker]
- **1-4 Sabit Saldırı** — _Mayın_ — top mermisi (yay çizip düşer) → kapsül 1.5m, hedef noktada (yerinde çapalı, içine girene, uyumsuz silah). Etkiler: can→düşman -57.6 [tuzak]
- **1-5 Yayılan Saldırı** — _Dalga_ — top mermisi (yay çizip düşer) → kapsül 2.7m, hedef noktada (dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: can→düşman -48.96 [alan]
- **1-6 Bağlayıcı Saldırı** — _Bağ_ — top mermisi (yay çizip düşer) → bağ 1.5m, hedef noktada (sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: can→düşman -57.6 [bag_akisi]; hareket→düşman 1.5sn [bag_ucu]; hareket→düşman 0.7 1.5sn [bag_ucu,yavas]
- **1-7 Bulandırıcı Saldırı** — _Sis_ — top mermisi (yay çizip düşer) → bulut 1.5m, hedef noktada (sis hacmi, tik tik, uyumsuz silah). Etkiler: can→düşman -57.6 [bulut_tik]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **1-8 Yükselen Saldırı** — _Havaya atma, Yükselen_ — top mermisi (yay çizip düşer) → kapsül 1.5m, hedef noktada (yerden yükselir, gücü artar, uyumsuz silah). Etkiler: can→düşman -57.6 [havaya_at]; hareket→düşman 0.8sn [havada]; hasar_buff→sen 0.1 3sn [yukselen]
- **1-9 Odaklı Saldırı** — _Güdümlü_ — top mermisi (yay çizip düşer) → kapsül 1.5m, hedef noktada (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: can→düşman -66.24 [iskalamaz,zirh_yoksay]
- **1-10 Aynalı Saldırı** — _Kıskaç, Ayna eşli_ — top mermisi (yay çizip düşer) → kapsül 1.5m, hedef noktada (karşı noktada eşi, uyumsuz silah). Etkiler: can→düşman -57.6 [kiskac]; yansit→sen 0.3 2sn [ayna_sifati]
- **1-11 Kopya Saldırı** — _Çift_ — top mermisi (yay çizip düşer) → kapsül 1.5m, hedef noktada (2 kez (kopya), uyumsuz silah). Etkiler: can→düşman -57.6 [iki_kez]
- **1-12 Akan Saldırı** — _İnen akış alanı, Akış_ — top mermisi (yay çizip düşer) → kapsül 1.5m, hedef noktada (sürekli akar, tik tik, uyumsuz silah). Etkiler: can→düşman -40.32 3sn [akis]

### 2 İyileştirme

- *sıfatsız*: top mermisi (yay çizip düşer) → küre 2m, hedef noktada (uyumsuz silah). Etkiler: can→dost 28
- **2-1 Yoğun İyileştirme** — _Taşma_ — top mermisi (yay çizip düşer) → küre 1.1m, hedef noktada (tek hedef, uyumsuz silah). Etkiler: can→dost 37.8 [tasar]
- **2-2 Emici İyileştirme** — _Girdap, Emme/çalma_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (içine çeker, uyumsuz silah). Etkiler: can→düşman -26.6 [emme]; can→sen 26.6 [aktarim]; mermi_sil→düşman mermisi [yut]
- **2-3 Sıçrayan İyileştirme** — _Seken_ — top mermisi (yay çizip düşer) → küre 2.2m, hedef noktada (2 kez seker, uyumsuz silah). Etkiler: can→dost 28 [dosttan_dosta]
- **2-4 Sabit İyileştirme** — _Totem_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (yerinde çapalı, tik tik, uyumsuz silah). Etkiler: can→dost 28 [totem]
- **2-5 Yayılan İyileştirme** — _Dalga_ — top mermisi (yay çizip düşer) → küre 3.6m, hedef noktada (dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: can→dost 23.8 [alan]
- **2-6 Bağlayıcı İyileştirme** — _Can bağı, Bağ_ — top mermisi (yay çizip düşer) → bağ 2m, hedef noktada (sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: can→dost 28 [can_bagi]; hasar_paylasimi→dost 0.5 1.5sn [bag]
- **2-7 Bulandırıcı İyileştirme** — _Sis_ — top mermisi (yay çizip düşer) → bulut 2m, hedef noktada (sis hacmi, tik tik, uyumsuz silah). Etkiler: can→dost 28 [bulut_tik]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **2-8 Yükselen İyileştirme** — _Yükselen_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (yerden yükselir, gücü artar, uyumsuz silah). Etkiler: can→dost 28 [buyuyen]; hasar_buff→sen 0.1 3sn [yukselen]
- **2-9 Odaklı İyileştirme** — _Koruyucu tetik, Güdümlü_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: can→dost 32.2 [koruyucu_tetik]
- **2-10 Aynalı İyileştirme** — _Ters kopya, Ayna eşli_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (karşı noktada eşi, uyumsuz silah). Etkiler: can→dost 28; can→düşman -8.4 [ters_kopya]; yansit→sen 0.3 2sn [ayna_sifati]
- **2-11 Kopya İyileştirme** — _Çift_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (2 kez (kopya), uyumsuz silah). Etkiler: can→dost 28 [iki_kez]
- **2-12 Akan İyileştirme** — _İnen akış alanı, Akış_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (sürekli akar, tik tik, uyumsuz silah). Etkiler: can→dost 19.6 3sn [akis]

### 3 Hareket

- *sıfatsız*: top mermisi (yay çizip düşer) → çizgi 3m, sende (uyumsuz silah). Etkiler: kendini_tasi→sen 9 [yol:firlatilma]
- **3-1 Yoğun Hareket** — _Fırlatılma, Faz geçişi_ — top mermisi (yay çizip düşer) → çizgi 1.7m, sende (tek hedef, uyumsuz silah). Etkiler: kendini_tasi→sen 9 [faz,yol:firlatilma]
- **3-2 Emici Hareket** — _Hedefi sana çekme, Fırlatılma, Girdap_ — top mermisi (yay çizip düşer) → çizgi 3m, sende (içine çeker, uyumsuz silah). Etkiler: cek→düşman 9 [sana_dogru,yol:firlatilma]; mermi_sil→düşman mermisi [yut]
- **3-3 Sıçrayan Hareket** — _Fırlatılma, Sekmeli zıplama, Seken_ — top mermisi (yay çizip düşer) → çizgi 3.3m, sende (2 kez seker, uyumsuz silah). Etkiler: kendini_tasi→sen 9 [sekmeli,yol:firlatilma]
- **3-4 Sabit Hareket** — _İşaretle-geri dön, Fırlatılma_ — top mermisi (yay çizip düşer) → çizgi 3m, sende (yerinde çapalı, uyumsuz silah). Etkiler: isaret_geri_don→sen 9 2.2sn [geri_donus,yol:firlatilma]
- **3-5 Yayılan Hareket** — _Fırlatılma, İniş şok dalgası, Dalga_ — top mermisi (yay çizip düşer) → çizgi 5.4m, sende (dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: kendini_tasi→sen 9 [inis_dalgasi,yol:firlatilma]; it→düşman [dalga]
- **3-6 Bağlayıcı Hareket** — _Yer değiştirme, Fırlatılma, Bağ_ — top mermisi (yay çizip düşer) → bağ 3m, sende (sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: yer_degistir→düşman 9 [iki_uc,yol:firlatilma]; hareket→düşman 1.5sn [bag_ucu]; hareket→düşman 0.7 1.5sn [bag_ucu,yavas]
- **3-7 Bulandırıcı Hareket** — _Görünmez geçiş, Fırlatılma, Sis_ — top mermisi (yay çizip düşer) → bulut 3m, sende (sis hacmi, tik tik, uyumsuz silah). Etkiler: kendini_tasi→sen 9 [gorunmez_gecis,yol:firlatilma]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **3-8 Yükselen Hareket** — _Sıçrayıp çakılma, Fırlatılma, Yükselen_ — top mermisi (yay çizip düşer) → çizgi 3m, sende (yerden yükselir, gücü artar, uyumsuz silah). Etkiler: kendini_tasi→sen 9 [sicrayip_cakil,yol:firlatilma]; hasar_buff→sen 0.1 3sn [yukselen]
- **3-9 Odaklı Hareket** — _Arkaya ışınlanma, Fırlatılma, Güdümlü_ — top mermisi (yay çizip düşer) → çizgi 3m, sende (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: hedefin_arkasina→sen 9 [isinlanma,yol:firlatilma]
- **3-10 Aynalı Hareket** — _Portal, Fırlatılma, Ayna eşli_ — top mermisi (yay çizip düşer) → çizgi 3m, sende (karşı noktada eşi, uyumsuz silah). Etkiler: portal→sen 9 4sn [portal_cifti,yol:firlatilma]; yansit→sen 0.3 2sn [ayna_sifati]
- **3-11 Kopya Hareket** — _Fırlatılma, Yem kopya, Çift_ — top mermisi (yay çizip düşer) → çizgi 3m, sende (2 kez (kopya), uyumsuz silah). Etkiler: kendini_tasi→sen 9 [ardinda_kopya,yol:firlatilma]; yem_kopya→sen 1 2sn [dikkat_ceker]
- **3-12 Akan Hareket** — _Fırlatılma, Süzülme, İnen akış alanı, Akış_ — top mermisi (yay çizip düşer) → çizgi 3m, sende (sürekli akar, tik tik, uyumsuz silah). Etkiler: kendini_tasi→sen 9 3sn [suzulme,yol:firlatilma]

### 4 Savunma

- *sıfatsız*: top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (katı, uyumsuz silah). Etkiler: kalkan→dost 40
- **4-1 Yoğun Savunma** — _Taşma_ — top mermisi (yay çizip düşer) → küre 0.8m, hedef noktada (katı, tek hedef, uyumsuz silah). Etkiler: kalkan→dost 54 [tasar]
- **4-2 Emici Savunma** — _Girdap, Emme/çalma_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (katı, içine çeker, uyumsuz silah). Etkiler: can→düşman -38 [emme]; kalkan→sen 38 [aktarim]; mermi_sil→düşman mermisi [yut]
- **4-3 Sıçrayan Savunma** — _Seken_ — top mermisi (yay çizip düşer) → küre 1.7m, hedef noktada (katı, 2 kez seker, uyumsuz silah). Etkiler: kalkan→dost 40 [dosttan_dosta]
- **4-4 Sabit Savunma** — _Duvar, Totem_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (yerinde çapalı, katı, tik tik, uyumsuz silah). Etkiler: kalkan→dost 40 [totem]
- **4-5 Yayılan Savunma** — _Dalga_ — top mermisi (yay çizip düşer) → küre 2.7m, hedef noktada (katı, dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: kalkan→dost 34 [alan]
- **4-6 Bağlayıcı Savunma** — _Can bağı, Bağ_ — top mermisi (yay çizip düşer) → bağ 1.5m, hedef noktada (katı, sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: kalkan→dost 40 [can_bagi]; hasar_paylasimi→dost 0.5 1.5sn [bag]
- **4-7 Bulandırıcı Savunma** — _Sis_ — top mermisi (yay çizip düşer) → bulut 1.5m, hedef noktada (katı, sis hacmi, tik tik, uyumsuz silah). Etkiler: kalkan→dost 40 [bulut_tik]; gizlen→dost 3.5sn [bulut_ici]; kor→düşman 0.3 3.5sn [bulut_ici]
- **4-8 Yükselen Savunma** — _Yükselen_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (katı, yerden yükselir, gücü artar, uyumsuz silah). Etkiler: kalkan→dost 40 [buyuyen]; hasar_buff→sen 0.1 3sn [yukselen]
- **4-9 Odaklı Savunma** — _Koruyucu tetik, Güdümlü_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (katı, hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: kalkan→dost 46 [koruyucu_tetik]
- **4-10 Aynalı Savunma** — _Ters kopya, Ayna eşli_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (katı, karşı noktada eşi, uyumsuz silah). Etkiler: kalkan→dost 40; can→düşman -12 [ters_kopya]; yansit→sen 0.3 2sn [ayna_sifati]
- **4-11 Kopya Savunma** — _Çift_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (katı, 2 kez (kopya), uyumsuz silah). Etkiler: kalkan→dost 40 [iki_kez]
- **4-12 Akan Savunma** — _İnen akış alanı, Akış_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (katı, sürekli akar, tik tik, uyumsuz silah). Etkiler: kalkan→dost 28 3sn [akis]

### 5 Patlama

- *sıfatsız*: top mermisi (yay çizip düşer) → küre 3m, hedef noktada. Etkiler: can→düşman -50.4; it→düşman
- **5-1 Yoğun Patlama** — _Delici_ — top mermisi (yay çizip düşer) → küre 1.7m, hedef noktada (delip geçer). Etkiler: can→düşman -68.04; it→düşman [sert]
- **5-2 Emici Patlama** — _İçe çöküş, Girdap_ — top mermisi (yay çizip düşer) → küre 3m, hedef noktada (içine çeker). Etkiler: can→düşman -47.88; cek→düşman [merkeze]; can→sen 14.36 [aktarim]; mermi_sil→düşman mermisi [yut]
- **5-3 Sıçrayan Patlama** — _Seken_ — top mermisi (yay çizip düşer) → küre 3.3m, hedef noktada (2 kez seker). Etkiler: can→düşman -50.4 [seker]; it→düşman [seker]
- **5-4 Sabit Patlama** — _Mayın_ — top mermisi (yay çizip düşer) → küre 3m, hedef noktada (yerinde çapalı, içine girene). Etkiler: can→düşman -50.4 [tuzak]; it→düşman [tuzak]
- **5-5 Yayılan Patlama** — _Dalga_ — top mermisi (yay çizip düşer) → küre 5.4m, hedef noktada (dışa büyür, cephe geçerken). Etkiler: can→düşman -42.84 [alan]; it→düşman [dalga]
- **5-6 Bağlayıcı Patlama** — _Bağla çekme, Bağ_ — top mermisi (yay çizip düşer) → bağ 3m, hedef noktada (sen↔hedef bağı, tik tik). Etkiler: can→düşman -50.4 [bag_akisi]; cek→düşman [bag_boyunca]; hareket→düşman 1.5sn [bag_ucu]; hareket→düşman 0.7 1.5sn [bag_ucu,yavas]
- **5-7 Bulandırıcı Patlama** — _Sis_ — top mermisi (yay çizip düşer) → bulut 3m, hedef noktada (sis hacmi, tik tik). Etkiler: can→düşman -50.4 [bulut_tik]; it→düşman [sis_patlamasi]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **5-8 Yükselen Patlama** — _Havaya atma, Yükselen_ — top mermisi (yay çizip düşer) → küre 3m, hedef noktada (yerden yükselir, gücü artar). Etkiler: can→düşman -50.4 [havaya_at]; it→düşman [yukari_firlat]; hareket→düşman 0.8sn [havada]; hasar_buff→sen 0.1 3sn [yukselen]
- **5-9 Odaklı Patlama** — _Güdümlü_ — top mermisi (yay çizip düşer) → küre 3m, hedef noktada (hedefe kilitli, tek hedef). Etkiler: can→düşman -57.96 [iskalamaz,zirh_yoksay]; it→düşman [tek_hedef]
- **5-10 Aynalı Patlama** — _Kıskaç, Ayna eşli_ — top mermisi (yay çizip düşer) → küre 3m, hedef noktada (karşı noktada eşi). Etkiler: can→düşman -50.4 [kiskac]; it→düşman [iki_uctan]; yansit→sen 0.3 2sn [ayna_sifati]
- **5-11 Kopya Patlama** — _Çift_ — top mermisi (yay çizip düşer) → küre 3m, hedef noktada (2 kez (kopya)). Etkiler: can→düşman -50.4 [iki_kez]; it→düşman [iki_kez]
- **5-12 Akan Patlama** — _Akıntı, İnen akış alanı, Akış_ — top mermisi (yay çizip düşer) → küre 3m, hedef noktada (sürekli akar, tik tik). Etkiler: can→düşman -35.28 3sn [akis]; it→düşman [akinti]

### 6 Kontrol

- *sıfatsız*: top mermisi (yay çizip düşer) → koni 4m, hedef noktada. Etkiler: hareket→düşman 1.5sn; can→düşman -14.4
- **6-1 Yoğun Kontrol** — _Delici_ — top mermisi (yay çizip düşer) → koni 2.2m, hedef noktada (delip geçer). Etkiler: hareket→düşman 0.8sn [sert]; can→düşman -19.44
- **6-2 Emici Kontrol** — _Hedefi sana çekme, Girdap_ — top mermisi (yay çizip düşer) → koni 4m, hedef noktada (içine çeker). Etkiler: hareket→düşman 1.5sn; can→düşman -13.68; cek→düşman [sana_dogru]; can→sen 4.1 [aktarim]; mermi_sil→düşman mermisi [yut]
- **6-3 Sıçrayan Kontrol** — _Seken_ — top mermisi (yay çizip düşer) → koni 4.4m, hedef noktada (2 kez seker). Etkiler: hareket→düşman 1.5sn [seker]; can→düşman -14.4 [seker]
- **6-4 Sabit Kontrol** — _Mayın_ — top mermisi (yay çizip düşer) → koni 4m, hedef noktada (yerinde çapalı, içine girene). Etkiler: hareket→düşman 3.5sn [uzun]; can→düşman -14.4 [tuzak]
- **6-5 Yayılan Kontrol** — _Dalga_ — top mermisi (yay çizip düşer) → koni 7.2m, hedef noktada (dışa büyür, cephe geçerken). Etkiler: hareket→düşman 1.5sn [dalga]; can→düşman -12.24 [alan]
- **6-6 Bağlayıcı Kontrol** — _Tasma, Bağ_ — top mermisi (yay çizip düşer) → bağ 4m, hedef noktada (sen↔hedef bağı, tik tik). Etkiler: hareket→düşman 1.5sn [tasma]; can→düşman -14.4 [bag_akisi]; hareket→düşman 1.5sn [bag_ucu]; hareket→düşman 0.7 1.5sn [bag_ucu,yavas]
- **6-7 Bulandırıcı Kontrol** — _Sis_ — top mermisi (yay çizip düşer) → bulut 4m, hedef noktada (sis hacmi, tik tik). Etkiler: hareket→düşman 1.5sn [bulut_tik]; can→düşman -14.4 [bulut_tik]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **6-8 Yükselen Kontrol** — _Havaya atma, Yükselen_ — top mermisi (yay çizip düşer) → koni 4m, hedef noktada (yerden yükselir, gücü artar). Etkiler: hareket→düşman 1.5sn [havaya_at]; can→düşman -14.4 [havaya_at]; hasar_buff→sen 0.1 3sn [yukselen]
- **6-9 Odaklı Kontrol** — _Gecikmeli an, Güdümlü_ — top mermisi (yay çizip düşer) → koni 4m, hedef noktada (hedefe kilitli, tek hedef). Etkiler: hareket→düşman 1.5sn [isaretli_an]; can→düşman -16.56 [iskalamaz,zirh_yoksay]
- **6-10 Aynalı Kontrol** — _Kıskaç, Ters kontrol, Ayna eşli_ — top mermisi (yay çizip düşer) → koni 4m, hedef noktada (karşı noktada eşi). Etkiler: hareket→düşman 1.5sn [ters_kontrol]; can→düşman -14.4 [kiskac]; yansit→sen 0.3 2sn [ayna_sifati]
- **6-11 Kopya Kontrol** — _Çift_ — top mermisi (yay çizip düşer) → koni 4m, hedef noktada (2 kez (kopya)). Etkiler: hareket→düşman 1.5sn [iki_kez]; can→düşman -14.4 [iki_kez]
- **6-12 Akan Kontrol** — _Bataklık, İnen akış alanı, Akış_ — top mermisi (yay çizip düşer) → koni 4m, hedef noktada (sürekli akar, tik tik). Etkiler: hareket→düşman 3sn [bataklik]; can→düşman -10.08 3sn [akis]

### 7 Zayıflatma

- *sıfatsız*: top mermisi (yay çizip düşer) → kapsül 3m, hedef noktada. Etkiler: zirh→düşman -0.2 4sn; can→düşman -9
- **7-1 Yoğun Zayıflatma** — _Delici_ — top mermisi (yay çizip düşer) → kapsül 1.7m, hedef noktada (delip geçer). Etkiler: zirh→düşman -0.27 2sn; can→düşman -12.15
- **7-2 Emici Zayıflatma** — _Girdap_ — top mermisi (yay çizip düşer) → kapsül 3m, hedef noktada (içine çeker). Etkiler: zirh→düşman -0.19 4sn; can→düşman -8.55; zirh→sen 0.06 4sn [aktarim]; can→sen 2.57 [aktarim]; mermi_sil→düşman mermisi [yut]
- **7-3 Sıçrayan Zayıflatma** — _Seken_ — top mermisi (yay çizip düşer) → kapsül 3.3m, hedef noktada (2 kez seker). Etkiler: zirh→düşman -0.2 4sn [seker]; can→düşman -9 [seker]
- **7-4 Sabit Zayıflatma** — _Mayın_ — top mermisi (yay çizip düşer) → kapsül 3m, hedef noktada (yerinde çapalı, içine girene). Etkiler: zirh→düşman -0.2 4sn [tuzak]; can→düşman -9 [tuzak]
- **7-5 Yayılan Zayıflatma** — _Dalga_ — top mermisi (yay çizip düşer) → kapsül 5.4m, hedef noktada (dışa büyür, cephe geçerken). Etkiler: zirh→düşman -0.17 4sn [alan]; can→düşman -7.65 [alan]
- **7-6 Bağlayıcı Zayıflatma** — _Bağ_ — top mermisi (yay çizip düşer) → bağ 3m, hedef noktada (sen↔hedef bağı, tik tik). Etkiler: zirh→düşman -0.2 4sn [bag_akisi]; can→düşman -9 [bag_akisi]; hareket→düşman 1.5sn [bag_ucu]; hareket→düşman 0.7 1.5sn [bag_ucu,yavas]
- **7-7 Bulandırıcı Zayıflatma** — _Sis_ — top mermisi (yay çizip düşer) → bulut 3m, hedef noktada (sis hacmi, tik tik). Etkiler: zirh→düşman -0.2 4sn [bulut_tik]; can→düşman -9 [bulut_tik]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **7-8 Yükselen Zayıflatma** — _Havaya atma, Yükselen_ — top mermisi (yay çizip düşer) → kapsül 3m, hedef noktada (yerden yükselir, gücü artar). Etkiler: zirh→düşman -0.2 4sn [havaya_at]; can→düşman -9 [havaya_at]; hareket→düşman 0.8sn [havada]; hasar_buff→sen 0.1 3sn [yukselen]
- **7-9 Odaklı Zayıflatma** — _Gecikmeli an, Güdümlü_ — top mermisi (yay çizip düşer) → kapsül 3m, hedef noktada (hedefe kilitli, tek hedef). Etkiler: zirh→düşman -0.23 4sn [isaretli_an]; can→düşman -10.35 [iskalamaz,zirh_yoksay]
- **7-10 Aynalı Zayıflatma** — _Kıskaç, Ayna eşli_ — top mermisi (yay çizip düşer) → kapsül 3m, hedef noktada (karşı noktada eşi). Etkiler: zirh→düşman -0.2 4sn [kiskac]; can→düşman -9 [kiskac]; yansit→sen 0.3 2sn [ayna_sifati]
- **7-11 Kopya Zayıflatma** — _Çift_ — top mermisi (yay çizip düşer) → kapsül 3m, hedef noktada (2 kez (kopya)). Etkiler: zirh→düşman -0.2 4sn [iki_kez]; can→düşman -9 [iki_kez]
- **7-12 Akan Zayıflatma** — _İnen akış alanı, Akış_ — top mermisi (yay çizip düşer) → kapsül 3m, hedef noktada (sürekli akar, tik tik). Etkiler: zirh→düşman -0.14 3sn [akis]; can→düşman -6.3 3sn [akis]

### 8 Güçlendirme

- *sıfatsız*: top mermisi (yay çizip düşer) → küre 2m, hedef noktada (uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn
- **8-1 Yoğun Güçlendirme** — _Taşma_ — top mermisi (yay çizip düşer) → küre 1.1m, hedef noktada (tek hedef, uyumsuz silah). Etkiler: hasar_buff→dost 0.22 1.5sn [tasar]
- **8-2 Emici Güçlendirme** — _Girdap, Emme/çalma_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (içine çeker, uyumsuz silah). Etkiler: hasar_buff→düşman -0.15 3sn [emme]; hasar_buff→sen 0.15 3sn [aktarim]; mermi_sil→düşman mermisi [yut]
- **8-3 Sıçrayan Güçlendirme** — _Seken_ — top mermisi (yay çizip düşer) → küre 2.2m, hedef noktada (2 kez seker, uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn [dosttan_dosta]
- **8-4 Sabit Güçlendirme** — _Totem_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (yerinde çapalı, tik tik, uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn [totem]
- **8-5 Yayılan Güçlendirme** — _Dalga_ — top mermisi (yay çizip düşer) → küre 3.6m, hedef noktada (dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: hasar_buff→dost 0.14 3sn [alan]
- **8-6 Bağlayıcı Güçlendirme** — _Can bağı, Bağ_ — top mermisi (yay çizip düşer) → bağ 2m, hedef noktada (sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn [can_bagi]; hasar_paylasimi→dost 0.5 1.5sn [bag]
- **8-7 Bulandırıcı Güçlendirme** — _Sis_ — top mermisi (yay çizip düşer) → bulut 2m, hedef noktada (sis hacmi, tik tik, uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn [bulut_tik]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **8-8 Yükselen Güçlendirme** — _Yükselen_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (yerden yükselir, gücü artar, uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn [buyuyen]; hasar_buff→sen 0.1 3sn [yukselen]
- **8-9 Odaklı Güçlendirme** — _Koruyucu tetik, Güdümlü_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: hasar_buff→dost 0.18 3sn [koruyucu_tetik]
- **8-10 Aynalı Güçlendirme** — _Ters kopya, Ayna eşli_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (karşı noktada eşi, uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn; hasar_buff→düşman -0.05 3sn [ters_kopya]; yansit→sen 0.3 2sn [ayna_sifati]
- **8-11 Kopya Güçlendirme** — _Çift_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (2 kez (kopya), uyumsuz silah). Etkiler: hasar_buff→dost 0.16 3sn [iki_kez]
- **8-12 Akan Güçlendirme** — _İnen akış alanı, Akış_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (sürekli akar, tik tik, uyumsuz silah). Etkiler: hasar_buff→dost 0.11 3sn [akis]

### 9 Arındırma

- *sıfatsız*: top mermisi (yay çizip düşer) → küre 2.5m, hedef noktada (uyumsuz silah). Etkiler: durum_sil→dost 1; mermi_sil→düşman mermisi
- **9-1 Yoğun Arındırma** — _Sert_ — top mermisi (yay çizip düşer) → küre 1.4m, hedef noktada (tek hedef, uyumsuz silah). Etkiler: durum_sil→dost 1 [guclu]; mermi_sil→düşman mermisi [delici]
- **9-2 Emici Arındırma** — _Girdap, Durum aktarma_ — top mermisi (yay çizip düşer) → küre 2.5m, hedef noktada (içine çeker, uyumsuz silah). Etkiler: durum_aktar→dost 1; mermi_sil→düşman mermisi [yut]; durum_ekle→düşman 1 [aktarim]
- **9-3 Sıçrayan Arındırma** — _Seken_ — top mermisi (yay çizip düşer) → küre 2.8m, hedef noktada (2 kez seker, uyumsuz silah). Etkiler: durum_sil→dost 1 [dosttan_dosta]; mermi_sil→düşman mermisi [seker]
- **9-4 Sabit Arındırma** — _Mermi kesen engel, Arınma alanı_ — top mermisi (yay çizip düşer) → küre 2.5m, hedef noktada (yerinde çapalı, uyumsuz silah). Etkiler: durum_sil→dost 1 [arinma_alani]; mermi_sil→düşman mermisi [engel]
- **9-5 Yayılan Arındırma** — _Dalga_ — top mermisi (yay çizip düşer) → küre 4.5m, hedef noktada (dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: durum_sil→dost 1 [alan]; mermi_sil→düşman mermisi [alan]
- **9-6 Bağlayıcı Arındırma** — _Bağışıklık bağı, Bağ_ — top mermisi (yay çizip düşer) → bağ 2.5m, hedef noktada (sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: durum_sil→dost 1 [bag_bagisiklik]; mermi_sil→düşman mermisi [bag_hatti]
- **9-7 Bulandırıcı Arındırma** — _Sis_ — top mermisi (yay çizip düşer) → bulut 2.5m, hedef noktada (sis hacmi, tik tik, uyumsuz silah). Etkiler: durum_sil→dost 1 [gizli]; mermi_sil→düşman mermisi [sis_perdesi]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **9-8 Yükselen Arındırma** — _Arınıp güçlenme, Yükselen_ — top mermisi (yay çizip düşer) → küre 2.5m, hedef noktada (yerden yükselir, gücü artar, uyumsuz silah). Etkiler: durum_sil→dost 1 [guce_cevir]; mermi_sil→düşman mermisi [yukselen_perde]; hasar_buff→sen 0.1 3sn [yukselen]
- **9-9 Odaklı Arındırma** — _Tam arındırma, Güdümlü_ — top mermisi (yay çizip düşer) → küre 2.5m, hedef noktada (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: durum_sil→dost 99 [tumunu_sil]; mermi_sil→düşman mermisi [hedefli]
- **9-10 Aynalı Arındırma** — _Buff silme, Mermi geri gönderme, Ayna eşli_ — top mermisi (yay çizip düşer) → küre 2.5m, hedef noktada (karşı noktada eşi, uyumsuz silah). Etkiler: iyi_durum_sil→düşman 1 [ters_hedef]; mermi_sil→düşman mermisi [geri_gonder]; yansit→sen 0.3 2sn [ayna_sifati]
- **9-11 Kopya Arındırma** — _Çift_ — top mermisi (yay çizip düşer) → küre 2.5m, hedef noktada (2 kez (kopya), uyumsuz silah). Etkiler: durum_sil→dost 1 [iki_kez]; mermi_sil→düşman mermisi [iki_kez]
- **9-12 Akan Arındırma** — _İnen akış alanı, Akış_ — top mermisi (yay çizip düşer) → küre 2.5m, hedef noktada (sürekli akar, tik tik, uyumsuz silah). Etkiler: durum_sil→dost 1 [aura]; mermi_sil→düşman mermisi [surekli_perde]

### 10 Yansıma

- *sıfatsız*: top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [dunyada,silahla:ucan]
- **10-1 Yoğun Yansıma** — _Uzak yansıtıcı_ — top mermisi (yay çizip düşer) → küre 0.8m, hedef noktada (tek hedef, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.68 1sn [dunyada,sert,silahla:ucan]
- **10-2 Emici Yansıma** — _Girdap, Uzak yansıtıcı, Hasar emme_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (içine çeker, uyumsuz silah). Etkiler: em→gövdenin olduğu yer 0.5 2sn [cana_cevir,dunyada,silahla:ucan]; mermi_sil→düşman mermisi [yut]
- **10-3 Sıçrayan Yansıma** — _Uzak yansıtıcı, Seken_ — top mermisi (yay çizip düşer) → küre 1.7m, hedef noktada (2 kez seker, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [dunyada,seker,silahla:ucan]
- **10-4 Sabit Yansıma** — _Ayna yüzey, Uzak yansıtıcı_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (yerinde çapalı, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [ayna_yuzey,dunyada,silahla:ucan]
- **10-5 Yayılan Yansıma** — _Uzak yansıtıcı, Dalga_ — top mermisi (yay çizip düşer) → küre 2.7m, hedef noktada (dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [aura,dunyada,silahla:ucan]
- **10-6 Bağlayıcı Yansıma** — _Uzak yansıtıcı, Hasar yönlendirme, Bağ_ — top mermisi (yay çizip düşer) → bağ 1.5m, hedef noktada (sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: yonlendir→düşman 0.5 2sn [bag,dunyada,silahla:ucan]; hareket→düşman 1.5sn [bag_ucu]; hareket→düşman 0.7 2sn [bag_ucu,yavas]
- **10-7 Bulandırıcı Yansıma** — _Sis, Uzak yansıtıcı_ — top mermisi (yay çizip düşer) → bulut 1.5m, hedef noktada (sis hacmi, tik tik, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [dunyada,gizli,silahla:ucan]; gizlen→dost 5sn [bulut_ici]; kor→düşman 0.3 5sn [bulut_ici]
- **10-8 Yükselen Yansıma** — _Uzak yansıtıcı, Yükselen_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (yerden yükselir, gücü artar, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [artan_oran,dunyada,silahla:ucan]; hasar_buff→sen 0.1 3sn [yukselen]
- **10-9 Odaklı Yansıma** — _Uzak yansıtıcı, Kusursuz savuşturma, Güdümlü_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 1 2sn [dunyada,savusturma,silahla:ucan]
- **10-10 Aynalı Yansıma** — _Uzak yansıtıcı, Bölünen yansıma, Ayna eşli_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (karşı noktada eşi, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [bolunen,dunyada,silahla:ucan]; yansit→sen 0.3 2sn [ayna_sifati]
- **10-11 Kopya Yansıma** — _Uzak yansıtıcı, Çift_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (2 kez (kopya), uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 2sn [dunyada,iki_kez,silahla:ucan]
- **10-12 Akan Yansıma** — _Uzak yansıtıcı, İnen akış alanı, Akış_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (sürekli akar, tik tik, uyumsuz silah). Etkiler: yansit→gövdenin olduğu yer 0.5 3sn [dunyada,silahla:ucan,surekli]

### 11 Çağırma

- *sıfatsız*: top mermisi (yay çizip düşer) → nokta 1m, hedef noktada (uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [silahla:ucan]
- **11-1 Yoğun Çağırma** — _Sert_ — top mermisi (yay çizip düşer) → nokta 0.6m, hedef noktada (tek hedef, uyumsuz silah). Etkiler: aktor_yarat→sen 1 2.5sn [guclu,silahla:ucan]
- **11-2 Emici Çağırma** — _Can emen minyon, Girdap_ — top mermisi (yay çizip düşer) → nokta 1m, hedef noktada (içine çeker, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [can_emen,silahla:ucan]; mermi_sil→düşman mermisi [yut]
- **11-3 Sıçrayan Çağırma** — _Zıplayan minyon, Seken_ — top mermisi (yay çizip düşer) → nokta 1.1m, hedef noktada (2 kez seker, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [silahla:ucan,ziplayan]
- **11-4 Sabit Çağırma** — _Taret_ — top mermisi (yay çizip düşer) → nokta 1m, hedef noktada (yerinde çapalı, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [silahla:ucan,taret]
- **11-5 Yayılan Çağırma** — _Minyon halkası, Dalga_ — top mermisi (yay çizip düşer) → nokta 1.8m, hedef noktada (dışa büyür, cephe geçerken, uyumsuz silah). Etkiler: aktor_yarat→sen 3 5sn [halka,silahla:ucan]
- **11-6 Bağlayıcı Çağırma** — _Muhafız, Bağ_ — top mermisi (yay çizip düşer) → bağ 1m, hedef noktada (sen↔hedef bağı, tik tik, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [bagli_muhafiz,silahla:ucan]
- **11-7 Bulandırıcı Çağırma** — _Görünmez minyon, Sis_ — top mermisi (yay çizip düşer) → bulut 1m, hedef noktada (sis hacmi, tik tik, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [gorunmez,silahla:ucan]; gizlen→dost 3.2sn [bulut_ici]; kor→düşman 0.3 3.2sn [bulut_ici]
- **11-8 Yükselen Çağırma** — _Yükselen_ — top mermisi (yay çizip düşer) → nokta 1m, hedef noktada (yerden yükselir, gücü artar, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [buyuyen,silahla:ucan]; hasar_buff→sen 0.1 3sn [yukselen]
- **11-9 Odaklı Çağırma** — _Suikastçı minyon, Güdümlü_ — top mermisi (yay çizip düşer) → nokta 1m, hedef noktada (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [silahla:ucan,suikastci]
- **11-10 Aynalı Çağırma** — _Ayna klon, Ayna eşli_ — top mermisi (yay çizip düşer) → nokta 1m, hedef noktada (karşı noktada eşi, uyumsuz silah). Etkiler: aktor_yarat→sen 1 5sn [ayna_klon,silahla:ucan]; yansit→sen 0.3 2sn [ayna_sifati]
- **11-11 Kopya Çağırma** — _Klon, Çift_ — top mermisi (yay çizip düşer) → nokta 1m, hedef noktada (2 kez (kopya), uyumsuz silah). Etkiler: klon→sen 1 5sn [senin_kopyan,silahla:ucan]
- **11-12 Akan Çağırma** — _İnen akış alanı, Akış_ — top mermisi (yay çizip düşer) → nokta 1m, hedef noktada (sürekli akar, tik tik, uyumsuz silah). Etkiler: aktor_yarat→sen 3 5sn [akis,silahla:ucan]

### 12 Zaman

- *sıfatsız*: top mermisi (yay çizip düşer) → silindir 4m, hedef noktada. Etkiler: tempo→düşman 0.7 1sn
- **12-1 Yoğun Zaman** — _Delici_ — top mermisi (yay çizip düşer) → silindir 2.2m, hedef noktada (delip geçer). Etkiler: tempo→düşman 0.3 0.5sn [sert]
- **12-2 Emici Zaman** — _Zaman çalma, Girdap_ — top mermisi (yay çizip düşer) → silindir 4m, hedef noktada (içine çeker). Etkiler: tempo→düşman 0.7 1sn; tempo→sen 1.3 1sn [aktarim]; mermi_sil→düşman mermisi [yut]
- **12-3 Sıçrayan Zaman** — _Seken_ — top mermisi (yay çizip düşer) → silindir 4.4m, hedef noktada (2 kez seker). Etkiler: tempo→düşman 0.7 1sn [seker]
- **12-4 Sabit Zaman** — _Dondurma_ — top mermisi (yay çizip düşer) → silindir 4m, hedef noktada (yerinde çapalı). Etkiler: tempo→düşman 1sn [dondur]
- **12-5 Yayılan Zaman** — _Dalga_ — top mermisi (yay çizip düşer) → silindir 7.2m, hedef noktada (dışa büyür, cephe geçerken). Etkiler: tempo→düşman 0.7 1sn [dalga]
- **12-6 Bağlayıcı Zaman** — _Zaman senkronu, Bağ_ — top mermisi (yay çizip düşer) → bağ 4m, hedef noktada (sen↔hedef bağı, tik tik). Etkiler: tempo→düşman 0.7 1sn [senkron]; hareket→düşman 1.5sn [bag_ucu]; hareket→düşman 0.7 1.5sn [bag_ucu,yavas]
- **12-7 Bulandırıcı Zaman** — _Titreyen zaman, Sis_ — top mermisi (yay çizip düşer) → bulut 4m, hedef noktada (sis hacmi, tik tik). Etkiler: tempo→düşman 0.7 1sn [titrer]; gizlen→dost 4sn [bulut_ici]; kor→düşman 0.3 4sn [bulut_ici]
- **12-8 Yükselen Zaman** — _Hızlanma, Yükselen_ — top mermisi (yay çizip düşer) → silindir 4m, hedef noktada (yerden yükselir, gücü artar). Etkiler: tempo→sen 1.3 1sn [hizlanma]; hasar_buff→sen 0.1 3sn [yukselen]
- **12-9 Odaklı Zaman** — _Gecikmeli an, Güdümlü_ — top mermisi (yay çizip düşer) → silindir 4m, hedef noktada (hedefe kilitli, tek hedef). Etkiler: tempo→düşman 0.7 1sn [isaretli_an]
- **12-10 Aynalı Zaman** — _Geri sarma, Ayna eşli_ — top mermisi (yay çizip düşer) → silindir 4m, hedef noktada (karşı noktada eşi). Etkiler: geri_sar→düşman 2 1sn [geri_sarma]; yansit→sen 0.3 2sn [ayna_sifati]
- **12-11 Kopya Zaman** — _Yankı, Çift_ — top mermisi (yay çizip düşer) → silindir 4m, hedef noktada (2 kez (kopya)). Etkiler: tempo→düşman 0.7 1sn [iki_kez]; onceki_skill_tekrar→sen 1 [yanki]
- **12-12 Akan Zaman** — _Zaman alanı, İnen akış alanı, Akış_ — top mermisi (yay çizip düşer) → silindir 4m, hedef noktada (sürekli akar, tik tik). Etkiler: tempo→düşman 0.7 3sn [zaman_alani]; tempo→dost 1.3 3sn [zaman_alani]

## Aynı skill, 10 silah

### 3-10 Aynalı Hareket

- Yumruk ✓: _Portal, Ayna eşli_ — yumruk teması → çizgi 3m, sende (karşı noktada eşi). Etkiler: portal→sen 1.5 4sn [portal_cifti,yol:kisa_hamle]; yansit→sen 0.3 2sn [ayna_sifati]
- Hançer ✓: _Portal, Ayna eşli_ — hançer dürtüşü → çizgi 3m, sende (karşı noktada eşi). Etkiler: portal→sen 1.8 4sn [portal_cifti,yol:hamle]; yansit→sen 0.3 2sn [ayna_sifati]
- Mızrak: _Portal, Ayna eşli_ — mızrak saplaması → çizgi 3m, sende (karşı noktada eşi, uyumsuz silah). Etkiler: portal→sen 6 4sn [portal_cifti,yol:uzun_hamle]; yansit→sen 0.3 2sn [ayna_sifati]
- Kılıç ✓: _Portal, Ayna eşli_ — kılıç yayı → çizgi 3.6m, sende (karşı noktada eşi). Etkiler: portal→sen 3 4sn [portal_cifti,yol:yay_kayma]; yansit→sen 0.3 2sn [ayna_sifati]
- Balta: _Portal, Ayna eşli_ — ağır balta yayı → çizgi 3m, sende (karşı noktada eşi, sarsılmaz, uyumsuz silah). Etkiler: portal→sen 2.7 4sn [portal_cifti,yol:agir_hamle]; yansit→sen 0.3 2sn [ayna_sifati]
- Çekiç: _Portal, Sıçrayıp çakılma, Ayna eşli_ — yere vuruş → çizgi 3m, sende (yerden yükselir, karşı noktada eşi, uyumsuz silah). Etkiler: portal→sen 3 4sn [portal_cifti,yol:sicrayip_cakilma]; yansit→sen 0.3 2sn [ayna_sifati]
- Top: _Portal, Fırlatılma, Ayna eşli_ — top mermisi (yay çizip düşer) → çizgi 3m, sende (karşı noktada eşi, uyumsuz silah). Etkiler: portal→sen 9 4sn [portal_cifti,yol:firlatilma]; yansit→sen 0.3 2sn [ayna_sifati]
- Asa: _Portal, Ayna eşli_ — asa ışını (hat) → çizgi 3m, sende (karşı noktada eşi, tik tik, uyumsuz silah). Etkiler: portal→sen 7.5 4sn [portal_cifti,yol:hat_kayma]; yansit→sen 0.3 2sn [ayna_sifati]
- Tılsım: _Portal, Işınlanma, Ayna eşli_ — tılsım (hedefte belirir) → çizgi 3m, sende (karşı noktada eşi, uyumsuz silah). Etkiler: portal→sen 8.4 4sn [portal_cifti,yol:isarete_isinlanma]; yansit→sen 0.3 2sn [ayna_sifati]
- Kalkan: _Portal, Kalkan hücumu, Ayna eşli_ — kalkan gövdesi → çizgi 3m, sende (karşı noktada eşi, gövdene bağlı, uyumsuz silah). Etkiler: portal→sen 1.5 4sn [portal_cifti,yol:kalkan_hucumu]; yansit→sen 0.3 2sn [ayna_sifati]

### 4-4 Sabit Savunma

- Yumruk: _Duvar, Totem_ — yumruk teması → küre 1.5m, dokunduğun ilk kişide (yerinde çapalı, katı, tek hedef, tik tik, uyumsuz silah). Etkiler: kalkan→dost 40 [totem]
- Hançer: _Duvar, Totem_ — hançer dürtüşü → küre 1.5m, önünde (yerinde çapalı, katı, tik tik, uyumsuz silah). Etkiler: kalkan→dost 40 [totem]
- Mızrak: _Duvar, Totem_ — mızrak saplaması → küre 1.5m, önünde uzun hat (yerinde çapalı, katı, tik tik, uyumsuz silah). Etkiler: kalkan→dost 40 [totem]
- Kılıç: _Duvar, Totem_ — kılıç yayı → küre 1.5m, önünde yay (yerinde çapalı, katı, tik tik, uyumsuz silah). Etkiler: kalkan→dost 40 [totem]
- Balta: _Duvar, Totem_ — ağır balta yayı → küre 1.5m, önünde yay (yerinde çapalı, katı, sarsılmaz, tik tik, uyumsuz silah). Etkiler: kalkan→dost 40 [totem]
- Çekiç ✓: _Yerden taş duvar, Totem_ — yere vuruş → küre 1.5m, hedef noktada (yerinde çapalı, katı, yerden yükselir, tik tik). Etkiler: kalkan→dost 50 [totem]
- Top: _Duvar, Totem_ — top mermisi (yay çizip düşer) → küre 1.5m, hedef noktada (yerinde çapalı, katı, tik tik, uyumsuz silah). Etkiler: kalkan→dost 40 [totem]
- Asa: _Hat duvar, Totem, Çit_ — asa ışını (hat) → küre 1.5m, senden hedefe hat (yerinde çapalı, katı, tik tik, uyumsuz silah). Etkiler: kalkan→dost 40 [totem]
- Tılsım: _Duvar, Totem_ — tılsım (hedefte belirir) → küre 1.5m, hedef noktada (yerinde çapalı, katı, tik tik, uyumsuz silah). Etkiler: kalkan→dost 40 [totem]
- Kalkan ✓: _Duvar, Totem_ — kalkan gövdesi → küre 1.5m, sende (yerinde çapalı, katı, tik tik). Etkiler: kalkan→dost 50 [totem]

### 12-12 Akan Zaman

- Yumruk: _Zaman alanı, Akış_ — yumruk teması → silindir 4m, dokunduğun ilk kişide (sürekli akar, tek hedef, tik tik, uyumsuz silah). Etkiler: tempo→düşman 0.7 3sn [zaman_alani]; tempo→dost 1.3 3sn [zaman_alani]
- Hançer: _Zaman alanı, Akış_ — hançer dürtüşü → silindir 4m, önünde (sürekli akar, tik tik, uyumsuz silah). Etkiler: tempo→düşman 0.7 3sn [zaman_alani]; tempo→dost 1.3 3sn [zaman_alani]
- Mızrak ✓: _Zaman alanı, Akış_ — mızrak saplaması → silindir 4m, önünde uzun hat (sürekli akar, tik tik). Etkiler: tempo→düşman 0.7 3sn [zaman_alani]; tempo→dost 1.3 3sn [zaman_alani]
- Kılıç: _Zaman alanı, Akış_ — kılıç yayı → silindir 4m, önünde yay (sürekli akar, tik tik, uyumsuz silah). Etkiler: tempo→düşman 0.7 3sn [zaman_alani]; tempo→dost 1.3 3sn [zaman_alani]
- Balta: _Zaman alanı, Akış_ — ağır balta yayı → silindir 4m, önünde yay (sürekli akar, sarsılmaz, tik tik, uyumsuz silah). Etkiler: tempo→düşman 0.7 3sn [zaman_alani]; tempo→dost 1.3 3sn [zaman_alani]
- Çekiç: _Zaman alanı, Akış_ — yere vuruş → silindir 4m, hedef noktada (yerden yükselir, sürekli akar, tik tik, uyumsuz silah). Etkiler: tempo→düşman 0.7 3sn [zaman_alani]; tempo→dost 1.3 3sn [zaman_alani]
- Top ✓: _Zaman alanı, İnen akış alanı, Akış_ — top mermisi (yay çizip düşer) → silindir 4m, hedef noktada (sürekli akar, tik tik). Etkiler: tempo→düşman 0.7 3sn [zaman_alani]; tempo→dost 1.3 3sn [zaman_alani]
- Asa: _Zaman alanı, Akış_ — asa ışını (hat) → silindir 4m, senden hedefe hat (sürekli akar, tik tik, uyumsuz silah). Etkiler: tempo→düşman 0.7 3sn [zaman_alani]; tempo→dost 1.3 3sn [zaman_alani]
- Tılsım: _Zaman alanı, Akış_ — tılsım (hedefte belirir) → silindir 4m, hedef noktada (sürekli akar, tik tik, uyumsuz silah). Etkiler: tempo→düşman 0.7 3sn [zaman_alani]; tempo→dost 1.3 3sn [zaman_alani]
- Kalkan: _Zaman alanı, Akış_ — kalkan gövdesi → silindir 4m, sende (gövdene bağlı, sürekli akar, tik tik, uyumsuz silah). Etkiler: tempo→düşman 0.7 3sn [zaman_alani]; tempo→dost 1.3 3sn [zaman_alani]

### 2-9 Odaklı İyileştirme

- Yumruk: _Koruyucu tetik, Güdümlü_ — yumruk teması → küre 2m, dokunduğun ilk kişide (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: can→dost 32.2 [koruyucu_tetik]
- Hançer: _Koruyucu tetik, Güdümlü_ — hançer dürtüşü → küre 2m, önünde (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: can→dost 32.2 [koruyucu_tetik]
- Mızrak: _Koruyucu tetik, Güdümlü_ — mızrak saplaması → küre 2m, önünde uzun hat (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: can→dost 32.2 [koruyucu_tetik]
- Kılıç: _Koruyucu tetik, Güdümlü_ — kılıç yayı → küre 2m, önünde yay (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: can→dost 32.2 [koruyucu_tetik]
- Balta: _Koruyucu tetik, Güdümlü_ — ağır balta yayı → küre 2m, önünde yay (hedefe kilitli, sarsılmaz, tek hedef, uyumsuz silah). Etkiler: can→dost 32.2 [koruyucu_tetik]
- Çekiç: _Koruyucu tetik, Güdümlü_ — yere vuruş → küre 2m, hedef noktada (yerden yükselir, hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: can→dost 32.2 [koruyucu_tetik]
- Top: _Koruyucu tetik, Güdümlü_ — top mermisi (yay çizip düşer) → küre 2m, hedef noktada (hedefe kilitli, tek hedef, uyumsuz silah). Etkiler: can→dost 32.2 [koruyucu_tetik]
- Asa ✓: _Koruyucu tetik, Güdümlü_ — asa ışını (hat) → küre 2m, senden hedefe hat (hedefe kilitli, tek hedef, tik tik). Etkiler: can→dost 40.25 [koruyucu_tetik]
- Tılsım ✓: _Koruyucu tetik, Güdümlü_ — tılsım (hedefte belirir) → küre 2m, hedef noktada (hedefe kilitli, tek hedef). Etkiler: can→dost 48.3 [koruyucu_tetik]
- Kalkan: _Koruyucu tetik, Güdümlü_ — kalkan gövdesi → küre 2m, sende (hedefe kilitli, gövdene bağlı, tek hedef, uyumsuz silah). Etkiler: can→dost 32.2 [koruyucu_tetik]

### 1-3 Sıçrayan Saldırı

- Yumruk ✓: _Seken_ — yumruk teması → kapsül 1.7m, dokunduğun ilk kişide (tek hedef, 2 kez seker). Etkiler: can→düşman -32 [seker]
- Hançer ✓: _Seken_ — hançer dürtüşü → kapsül 1.7m, önünde (2 kez seker). Etkiler: can→düşman -28 [seker]
- Mızrak ✓: _Seken_ — mızrak saplaması → kapsül 1.7m, önünde uzun hat (2 kez seker). Etkiler: can→düşman -44 [seker,zirh_delen]
- Kılıç ✓: _Seken_ — kılıç yayı → kapsül 2m, önünde yay (2 kez seker). Etkiler: can→düşman -40 [seker]
- Balta ✓: _Seken_ — ağır balta yayı → kapsül 1.7m, önünde yay (sarsılmaz, 2 kez seker). Etkiler: can→düşman -52 [seker]
- Çekiç ✓: _Sersemletme, Seken_ — yere vuruş → kapsül 1.7m, hedef noktada (yerden yükselir, 2 kez seker). Etkiler: can→düşman -60 [seker]; hareket→düşman 0.5sn [seker,sersem]
- Top: _Seken_ — top mermisi (yay çizip düşer) → kapsül 1.7m, hedef noktada (2 kez seker, uyumsuz silah). Etkiler: can→düşman -57.6 [seker]
- Asa: _Seken_ — asa ışını (hat) → kapsül 1.7m, senden hedefe hat (2 kez seker, tik tik, uyumsuz silah). Etkiler: can→düşman -28.8 [seker]
- Tılsım: _Seken_ — tılsım (hedefte belirir) → kapsül 1.7m, hedef noktada (2 kez seker, uyumsuz silah). Etkiler: can→düşman -19.2 [seker]
- Kalkan: _Seken_ — kalkan gövdesi → kapsül 1.7m, sende (gövdene bağlı, 2 kez seker, uyumsuz silah). Etkiler: can→düşman -16 [seker]

### 11-11 Kopya Çağırma

- Yumruk: _Klon, Çift_ — yumruk teması → nokta 1m, dokunduğun ilk kişide (tek hedef, 2 kez (kopya), uyumsuz silah). Etkiler: klon→sen 1 5sn [senin_kopyan,silahla:yakin_itis]
- Hançer: _Klon, Çift_ — hançer dürtüşü → nokta 1m, önünde (2 kez (kopya), uyumsuz silah). Etkiler: klon→sen 1 5sn [senin_kopyan,silahla:yakin_itis]
- Mızrak: _Klon, Çift_ — mızrak saplaması → nokta 1m, önünde uzun hat (2 kez (kopya), uyumsuz silah). Etkiler: klon→sen 1 5sn [senin_kopyan,silahla:yakin_itis]
- Kılıç: _Klon, Çift_ — kılıç yayı → nokta 1m, önünde yay (2 kez (kopya), uyumsuz silah). Etkiler: klon→sen 1 5sn [senin_kopyan,silahla:yay]
- Balta: _Klon, Çift_ — ağır balta yayı → nokta 1m, önünde yay (sarsılmaz, 2 kez (kopya), uyumsuz silah). Etkiler: klon→sen 1 5sn [senin_kopyan,silahla:yay]
- Çekiç: _Klon, Çift_ — yere vuruş → nokta 1m, hedef noktada (yerden yükselir, 2 kez (kopya), uyumsuz silah). Etkiler: klon→sen 1 5sn [senin_kopyan,silahla:dikey]
- Top: _Klon, Çift_ — top mermisi (yay çizip düşer) → nokta 1m, hedef noktada (2 kez (kopya), uyumsuz silah). Etkiler: klon→sen 1 5sn [senin_kopyan,silahla:ucan]
- Asa ✓: _Klon, Çift_ — asa ışını (hat) → nokta 1m, senden hedefe hat (2 kez (kopya), tik tik). Etkiler: klon→sen 1 5sn [senin_kopyan,silahla:hat]
- Tılsım ✓: _Klon, Çift_ — tılsım (hedefte belirir) → nokta 1m, hedef noktada (2 kez (kopya)). Etkiler: klon→sen 1 5sn [senin_kopyan,silahla:belirme]
- Kalkan: _Klon, Çift_ — kalkan gövdesi → nokta 1m, sende (gövdene bağlı, 2 kez (kopya), uyumsuz silah). Etkiler: klon→sen 1 5sn [senin_kopyan,silahla:govde]

## A — sıfatın nitel etkisi olmayanlar


## A2 — sıfat yalnız gövdeyi değiştiriyor (skill id, silah fark etmeksizin)

- 1-1 Yoğun Saldırı: 9 silahta
- 7-1 Yoğun Zayıflatma: 10 silahta

## B2 — Kılıç'ta yol hariç aynı davranış çekirdeği


## C — silahla en az ayrışan skill'ler (yol sınıfı imzası)

- 1-4 Sabit Saldırı: 9 davranış / 10 sınıf / 10 tam
- 2-4 Sabit İyileştirme: 9 davranış / 10 sınıf / 10 tam
- 2-8 Yükselen İyileştirme: 9 davranış / 10 sınıf / 10 tam
- 4-4 Sabit Savunma: 9 davranış / 10 sınıf / 10 tam
- 4-8 Yükselen Savunma: 9 davranış / 10 sınıf / 10 tam
- 5-4 Sabit Patlama: 9 davranış / 10 sınıf / 10 tam
- 6-4 Sabit Kontrol: 9 davranış / 10 sınıf / 10 tam
- 7-4 Sabit Zayıflatma: 9 davranış / 10 sınıf / 10 tam
- 7-8 Yükselen Zayıflatma: 9 davranış / 10 sınıf / 10 tam
- 8-4 Sabit Güçlendirme: 9 davranış / 10 sınıf / 10 tam
- 8-8 Yükselen Güçlendirme: 9 davranış / 10 sınıf / 10 tam
- 9-4 Sabit Arındırma: 9 davranış / 10 sınıf / 10 tam
- 9-8 Yükselen Arındırma: 9 davranış / 10 sınıf / 10 tam
- 12-4 Sabit Zaman: 9 davranış / 10 sınıf / 10 tam
- 12-8 Yükselen Zaman: 9 davranış / 10 sınıf / 10 tam

## D — çelişkiler


## Özel etiketsiz planlar (skill id → silah sayısı)

- 1-1 Yoğun Saldırı: 9 silah — _Delici_
- 1-11 Kopya Saldırı: 9 silah — _Çift_
- 1-12 Akan Saldırı: 8 silah — _Akış_
- 1-3 Sıçrayan Saldırı: 9 silah — _Seken_
- 1-5 Yayılan Saldırı: 9 silah — _Dalga_
- 1-6 Bağlayıcı Saldırı: 9 silah — _Bağ_
- 1-9 Odaklı Saldırı: 9 silah — _Güdümlü_
- 10-1 Yoğun Yansıma: 1 silah — _Sert_
- 10-11 Kopya Yansıma: 1 silah — _Çift_
- 10-12 Akan Yansıma: 1 silah — _Akış_
- 10-3 Sıçrayan Yansıma: 1 silah — _Seken_
- 10-5 Yayılan Yansıma: 1 silah — _Dalga_
- 10-8 Yükselen Yansıma: 1 silah — _Yükselen_
- 11-1 Yoğun Çağırma: 10 silah — _Sert_
- 11-12 Akan Çağırma: 9 silah — _Akış_
- 11-8 Yükselen Çağırma: 10 silah — _Yükselen_
- 12-1 Yoğun Zaman: 10 silah — _Delici_
- 12-3 Sıçrayan Zaman: 10 silah — _Seken_
- 12-5 Yayılan Zaman: 10 silah — _Dalga_
- 2-11 Kopya İyileştirme: 10 silah — _Çift_
- 2-12 Akan İyileştirme: 9 silah — _Akış_
- 2-3 Sıçrayan İyileştirme: 10 silah — _Seken_
- 2-5 Yayılan İyileştirme: 10 silah — _Dalga_
- 2-8 Yükselen İyileştirme: 10 silah — _Yükselen_
- 4-11 Kopya Savunma: 10 silah — _Çift_
- 4-12 Akan Savunma: 9 silah — _Akış_
- 4-3 Sıçrayan Savunma: 10 silah — _Seken_
- 4-5 Yayılan Savunma: 10 silah — _Dalga_
- 4-8 Yükselen Savunma: 10 silah — _Yükselen_
- 5-1 Yoğun Patlama: 9 silah — _Delici_
- 5-11 Kopya Patlama: 9 silah — _Çift_
- 5-3 Sıçrayan Patlama: 9 silah — _Seken_
- 5-5 Yayılan Patlama: 9 silah — _Dalga_
- 5-9 Odaklı Patlama: 9 silah — _Güdümlü_
- 6-1 Yoğun Kontrol: 9 silah — _Delici_
- 6-11 Kopya Kontrol: 9 silah — _Çift_
- 6-3 Sıçrayan Kontrol: 9 silah — _Seken_
- 6-5 Yayılan Kontrol: 9 silah — _Dalga_
- 7-1 Yoğun Zayıflatma: 10 silah — _Delici_
- 7-11 Kopya Zayıflatma: 10 silah — _Çift_
- 7-12 Akan Zayıflatma: 9 silah — _Akış_
- 7-3 Sıçrayan Zayıflatma: 10 silah — _Seken_
- 7-5 Yayılan Zayıflatma: 10 silah — _Dalga_
- 7-6 Bağlayıcı Zayıflatma: 10 silah — _Bağ_
- 8-11 Kopya Güçlendirme: 10 silah — _Çift_
- 8-12 Akan Güçlendirme: 9 silah — _Akış_
- 8-3 Sıçrayan Güçlendirme: 10 silah — _Seken_
- 8-5 Yayılan Güçlendirme: 10 silah — _Dalga_
- 8-8 Yükselen Güçlendirme: 10 silah — _Yükselen_
- 9-1 Yoğun Arındırma: 10 silah — _Sert_
- 9-11 Kopya Arındırma: 10 silah — _Çift_
- 9-12 Akan Arındırma: 9 silah — _Akış_
- 9-3 Sıçrayan Arındırma: 10 silah — _Seken_
- 9-5 Yayılan Arındırma: 10 silah — _Dalga_
