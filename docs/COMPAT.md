# Element sistemi v6.1.1 uyumluluğu

`docs/element-sistemi.json` artık bağlayıcı v6.1.1 şemasıdır. Önceki v5.3 şeması
`docs/archive/element-sistemi-v5.3.json` altında saklanır.

Mevcut `SkillMotor` hâlâ v5.3 `elements` / `verbs` / `adjectives` alanlarını bekler;
v6.1.1 ise `runes`, `verb_base`, `adjective_mods` ve `skills.by_verb` kullanır.
Unity `SkillMotorLoader` parse hatasını zaten yakalar, açık bir uyarı yazar ve gömülü
v5 varsayılana döner; dolayısıyla edit-time yükleme çökmez. Bu fallback v6.1.1 davranışı
değildir.

Sonraki kod işi, `SkillMotor` ve ona bağlı Core testlerini v6.1.1 şemasına uyarlamaktır.
Bu tasarım kilidi değişikliğinde 144 skill motoru uygulanmadı ve Core test projeleri
silinmedi.
