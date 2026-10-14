@AGENTS.md

# Claude Code için oturum kuralları

- Her oturum tek görev. Görev kullanıcının verdiği tek satırdır; bitince "telefonda şunu görmeliyim" cümlesi
  doğrulanır. Görev dışı dosyaya dokunulmaz, repo baştan taranmaz.
- Oturum başında yalnız şunlar okunur: bu dosya, görevin adlandırdığı dosyalar, gerekiyorsa
  `docs/tasarim/11-onarim-plani.md`. Tasarım sorusu çıkarsa `docs/tasarim/13-vizyon-ve-cerceve.md` ve
  `docs/tasarim/00-OKU-BENI.md`.
- Değişiklik yapmadan önce plan yazılır ve onay beklenir. Plan: dokunulacak dosyalar, eklenecek/değiştirilecek
  test, telefonda beklenen sonuç.
- "Refactor", "temizle", "iyileştir" türü görev kabul edilmez; kullanıcıdan adresli tek satır istenir.
- Aynı hatada üçüncü denemeden sonra durulur; AGENTS.md'deki a/b/c (kod hatası / sayı / kural) kanıtı yazılır,
  kullanıcıya sorulur.
- Oturum sonunda: commit + push + PR açıklaması (AGENTS.md formatı); `13-vizyon-ve-cerceve.md`'deki bir [AÇIK]
  maddeye dokunulduysa PR'da yazılır.
