# Kül Ejderi — kaynak modeller (v0)

Tasarım: `docs/tasarim/18-kul-ejderi.md`, yerleşim: `19-krater-arenasi.md` §3, iş kartı ve poz listesi:
`20-asset-listesi.md` §1. Bu klasör Unity dışındadır; Unity'ye alma gri kutu / uygulama görevinin işidir.

| Dosya | Ne | Üçgen | Durum |
|---|---|---|---|
| `tripo/ejder_kaba.glb` | Tripo ham bütün ejderha (gövde kaynağı) | 31 708 | Kaynak |
| `tripo/ejder_kafa.glb` | Tripo ham baş + boyun | 14 834 | Kaynak |
| `tripo/ejder_pence.glb` | Tripo ham bacak çifti (biri kullanıldı, aynalandı) | 5 256 | Kaynak |
| `ejder_govde.fbx` (+ `_albedo.png`, `.blend`) | Gövde + kanatlar, baş ve bacaklar kesik | 10 000 | v0: iskelet (Kok, Gogus, Kanat_L/R) yok |
| `ejder_on.fbx` (+ `ejder_bas_albedo.png`, `ejder_bacak_albedo.png`, `.blend`) | Baş + boyun + iki ön bacak, tek iskelet, 19 poz klibi `Poz_<Ad>` | 18 574 | v0 |

**İskelet (`ejder_on`):** Gogus → Boyun_1–5 → Bas; Gogus → Omuz_L/R → Kol_Ust → Kol_Alt → Pence. L = batı, R = doğu.
Dinlenme pozu = `Poz_Yatis` (baş yerde, burun Z 7,5).

**Pozlar (19):** Yatis, Nefes_Cekme, Bas_Kalkik, Bas_Yere_Vurmus, Bas_Sola_Cekik, Bas_Saga_Cekik, Bas_Goge, Dogrulmus,
Dogrulmus_Nefes, Gogus_Yerde, Sendeleme; Pence_L/R_ Havada, Yerde, Acik, Supurme_Sonu. Her klip tek karedir ve
yalnız kendi kemiklerini taşır (baş pozları bacağa, bacak pozları başa dokunmaz).

**Koordinat:** bütün dosyalar arena koordinatlarındadır, pivot arena merkezi (0, 0, 0); Unity'de konum ve dönüş
sıfır, baş güneye (−Z) bakar. Unity sol el (X doğu, Z kuzey), Blender sağ el: Blender'da **doğu = −X, kuzey = −Y**.
FBX: ölçek 1, Apply Transform açık, ileri −Z, yukarı Y. Bütün parçaların yüzeyleri dışa bakar (işaretli hacim > 0, 12 Ekim kontrolü).

**Yeniden üretme** (`tools/blender-ejder/`, Blender Python modülü `bpy` 5.x, arayüzsüz):
```
python govde.py    -- tripo/ejder_kaba.glb  <out>
python kafa.py     -- tripo/ejder_kafa.glb  <out_bas>
python pozlar.py   -- <out_bas>/ejder_bas.blend <pozlar>
python bacak.py    -- tripo/ejder_pence.glb <pozlar>/ejder_bas_pozlar.blend <out_on>
python dokular.py  -- <out_on>/ejder_on.blend <out_on>
python onizleme.py -- <out>/ejder_govde.blend <render> <out_on>/ejder_on.fbx [poz=Pence_L_Havada]
python sis.py <render> oyun_kamerasi_kilitli genel yandan
```

**Sıradaki (v1):** çene kesimi ve Cene kemiği, göz kapağı şekil anahtarı, doku rengi (SI-5.3–5.5), emission maskesi
(gözler, çatlaklar, kalp), gövdeye Kok/Gogus ve kanat kemikleri + kanat pozları, göğüs plakaları, doğrulmuş bacak
pozları, 4 sinema klibi.
