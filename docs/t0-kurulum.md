# T0 — Unity kurulumu (insan işi)

Force Text ayarından sonra kalan iki adım: `.gitignore` kontrolü ve commit.

## `.gitignore` ne demek?

GitHub'a **hangi dosyaların gitmemesi gerektiğini** söyleyen bir liste.

Unity projeyi açınca otomatik dev klasörler oluşturur:

| Klasör | Ne | Neden commit etme |
|---|---|---|
| `Library/` | Unity'nin önbelleği | Çok büyük (GB), her makinede yeniden oluşur |
| `Temp/` | Geçici dosyalar | Anlık, gereksiz |
| `Logs/` | Log dosyaları | Gereksiz |
| `obj/` | Derleme artığı | Yeniden oluşur |

Bunları GitHub'a atarsan repo şişer ve birleştirmeler cehennem olur. `.gitignore` "bunları görmezden gel" der.

Repoda zaten `.gitignore` var; `unity/Library/` gibi alt klasörleri de kapsayacak şekilde güncellendi. `git pull` yapınca gelir.

## Commit adımları

Terminali repoyu klonladığın klasörde aç (`game` — `docs` ile `unity` aynı seviyede).

### 1) Son değişiklikleri çek (opsiyonel ama iyi fikir)

```bash
git pull
```

### 2) Ne commit edilecek bak

```bash
git status
```

**Görmen gerekenler (commit edilecek):**
- `unity/Assets/`
- `unity/ProjectSettings/`
- `unity/Packages/manifest.json`
- `.gitignore`

**Görmemen gerekenler (ignore edilmeli):**
- `unity/Library/`
- `unity/Temp/`
- `unity/Logs/`

`unity/Library` listede çıkıyorsa önce `git pull` yap; hâlâ öyleyse `.gitignore` eksiktir.

### 3) Ekle ve commit et

```bash
git add .gitignore unity/
git commit -m "chore: unity 6 urp projesi"
git push
```

### 4) .NET SDK (T1 için)

T1 Unity açmadan çalışır ama bilgisayarında .NET lazım:

```bash
dotnet --version
```

`8.x` veya üstü görünmeli. Yoksa: https://dotnet.microsoft.com/download — **.NET SDK 8** kur.

## T0 bitti mi?

- [x] Unity 6 Universal 3D projesi `unity/` içinde
- [x] Force Text
- [x] MCP bağlı (Accepted, yeşil)
- [x] `.gitignore` + commit + push
- [x] `dotnet --version` çalışıyor

Hepsi tamamsa `docs/gorev-listesi.md` içindeki **T1** prompt'unu ajana ver.
