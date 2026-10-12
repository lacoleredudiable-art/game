# Kül Ejderi — kaynak modeller (v0)

Tasarım: `docs/tasarim/14-kul-ejderi.md`, yerleşim: `15-krater-arenasi.md` §3, iş kartı ve poz listesi:
`16-asset-listesi.md` §1. Bu klasör Unity dışındadır; Unity'ye alma gri kutu / uygulama görevinin işidir.

| Dosya | Ne | Üçgen | Durum |
|---|---|---|---|
| `tripo/ejder_kaba.glb` | Tripo ham bütün ejderha (gövde kaynağı) | 31 708 | Kaynak |
| `tripo/ejder_kafa.glb` | Tripo ham baş + boyun | 14 834 | Kaynak |
| `ejder_govde.fbx` (+ `_albedo.png`, `.blend`) | Gövde + kanatlar, baş ve bacaklar kesik, arena koordinatlarında | 10 000 | v0: iskelet (Kok, Gogus, Kanat_L/R) yok |
| `ejder_bas.fbx` (+ `_albedo.png`) | Baş + boyun, iskeletli (Boyun_1–5, Bas), yatış pozu dinlenme pozu | 14 000 | v0: çene ayrı değil, doku taş tonuna çekilmedi, kor maskesi yok |
| `ejder_bas_pozlar.fbx` (+ `.blend`) | Aynı model + 11 baş/boyun pozu, her biri tek karelik klip `Poz_<Ad>` | — | v0 |

**Koordinat:** bütün dosyalar arena koordinatlarındadır (X doğu, Z kuzey, Y yukarı; pivot arena merkezi). Unity'de
konum ve dönüş sıfır bırakılır; baş güneye bakar. FBX: ölçek 1, Apply Transform açık, Y yukarı, -Z ileri.

**Yeniden üretme** (`tools/blender-ejder/`, Blender Python modülü `bpy` 5.x ile, arayüzsüz):
```
python govde.py   -- tripo/ejder_kaba.glb <çıkış>
python kafa.py    -- tripo/ejder_kafa.glb <çıkış>
python pozlar.py  -- <çıkış>/ejder_bas.blend <çıkış>
python onizleme.py -- <çıkış>/ejder_govde.blend <render> <çıkış>/ejder_bas.fbx
python sis.py <render> oyun_kamerasi_kilitli genel yandan
```

**Sıradaki (v1):** ön bacak + pençe (Tripo), çene kesimi ve Cene kemiği, doku rengi (SI-5.3–5.5), emission maskesi
(gözler, çatlaklar, kalp), gövdeye Gogus ve kanat kemikleri, `ejder_on` birleşimi, kalan pozlar ve 4 sinema klibi.
