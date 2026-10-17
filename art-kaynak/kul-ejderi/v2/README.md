# Kül Ejderi v2 — çalışma kaynağı

Durum (17 Ekim 2026): görsel tasarım kesinleşti (`docs/tasarim/18-kul-ejderi.md` sürüm 1.1). Unity'ye alınmadı;
`unity/Assets` içine koymak ayrı bir aşama işidir.

## İçerik
- `ejder.blend` — ana dosya: Tripo v2 gövde (sfenks pozu, kuyruk ve arka bacaklar görünür), v1 baş + 34 diş,
  tek iskelet `ejder_on`, kalp + 4 plaka (geçici biçim), kanatlar, 4 kırılan pul (`ejder_pul_*`), 33 poz/klip.
- `doku/` — albedo + kor (emisyon RGBA: R faz 1, G gözler, B faz 2+). Kanat koru yalnız B'de (SI-5.3).
  Baş dokuları (`ejder_bas_*_v2.png`) 17 Ekim'de Tripo kafa kaynağından `doku_palet.py` ile yeniden üretildi
  (eski v1 dosyaları başka makinenin geçici klasöründe kalmıştı, repoda yoktu).
- `teslim/ejder.fbx` — Unity'ye gidecek tek dosya: LOD0 35 580 üçgen (≤ 36 000), LOD1 18 592, ≤ 4 etki,
  bütün pozlar anahtarlı. `teslim/kabul_raporu.txt` ölçümler.
- `onizleme/kontrol_v2_1.png` — 8 kare: faz 1/2/3, çökme, kanat hazırlık ve süpürme, pul. `siluet_64px.png` (SI-6.2).
- `kemik.json`, `kanat3.json`, `tripo/` — üretim verisi ve ham kaynaklar.

## Betikler (`tools/blender-ejder/v2/`, Blender 5.2 `bpy` modülü, `python <betik>.py -- <argümanlar>`)
İlk üretim: govde2 → govde2_birlestir → pozlar2 → kapak → kalp_yer → omurga → omurga_kemik → kanat3 →
kanat3_birlestir → doku_palet / malzeme2.

17 Ekim kesinleşme (sırayla; hepsi tekrar çalıştırılabilir):
1. `kanat_poz.py -- ejder.blend 30` — dinlenme kanatları 30° indirilir; `Poz_Kanat_*_{Acik,Supurme_Basi,Supurme_Sonu}`.
2. `yuzey_disa.py` + `doku_palet.py` (ad `ejder_bas`, kaynak `tripo/ejder_kafa.glb` içindeki jpg) → baş dokuları.
3. `doku_yol.py -- ejder.blend` — bütün dokular `//doku/` göreli yolunda.
4. `pul.py -- ejder.blend` — 4 pul parçası.
5. `teslim.py -- ejder.blend teslim <çalışma.blend>` — bütçe, LOD1, ağırlık temizliği, FBX, rapor (kaynağa dokunmaz).
Önizleme: `poz_render.py` + `sis.py` (OpenEXR, Pillow, numpy; doku_palet için scipy).

## Açık işler
- Plaka ve kalp biçimi geçici.
- Kanat Savuruşu'nun pençe yanındaki Z 10–18 şeridi kanat tarafından alçaktan örtülmüyor (`18` KE-6.6 [AÇIK]).
- Çökme (Kor Mührü ödülü) için ayrı poz yok; `Poz_Sendeleme` kullanılır.
