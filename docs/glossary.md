# Veri sözcükleri sözlüğü (PLAN 2B.13b)

Kod tanımlayıcıları İngilizce; JSON / fiil kimlikleri / tuning panel alanları gibi **veri sözcükleri** bilinçli olarak Türkçe veya snake_case kalabilir. Bu tablo: veri sözcüğü → kodda karşılık (varsa) → anlam.

| Veri sözcüğü | Kod adı | Anlam |
|--------------|---------|--------|
| `PoseZehir` | `PoseZehir` (serileştirilmiş alan) | Zehir duruşu / görsel pose anahtarı (`ActorPoseView`, `VisualSettings`) |
| `bag_hatti` | — (JSON / log string) | Mekanik: bağ şeridi ile mermi silme |
| `iki_kez` | — (JSON / log string) | Mekanik: çift vuruş / kopya mermi |
| `surekli` | `Continuous` (metot) | Mekanik gramer: sürekli etki kipi |
| `genis_yay` | `WeaponPassiveKind.WideArc` | Pasif: geniş yay |
| `karsi_saldiri` | `WeaponPassiveKind.CounterStrike` | Pasif: karşı saldırı |
| `sirt_vurusu` | `WeaponPassiveKind.Backstab` | Pasif: sırt vuruşu |
| `yere_cakma` | `WeaponPassiveKind.GroundSlam` | Pasif: yere çakma |
| `kosu_atisi` | `WeaponPassiveKind.RunShot` | Pasif: koşu atışı |
| `sabit_nisan` | `WeaponPassiveKind.SteadyAim` | Pasif: sabit nişan |
| `uzun_buyu` | `WeaponPassiveKind.LongEnchant` | Pasif: uzun büyü |
| `kutsal_etki` | `WeaponPassiveKind.HolyEffect` | Pasif: kutsal etki |
| `dolu_sayfa` | `WeaponPassiveKind.FullPage` | Pasif: dolu sayfa |
| `capraz_ates` | `WeaponPassiveKind.CrossFire` | Pasif: çapraz ateş |

Rune enum üyeleri (`Ates`, `Hava`, …) PLAN 2B.14a kapsamında; burada listelenmez.
