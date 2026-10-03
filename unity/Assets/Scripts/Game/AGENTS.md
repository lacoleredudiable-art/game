# Game katmanı (ajan notu)

Kök `AGENTS.md` + `docs/MAP.md`. Hedef klasör/namespace düzeni: `docs/ARCHITECTURE-PLAN.md`.

- **Unity katmanı:** `MonoBehaviour`, `Update`, `Resources`, sahne objeleri burada. Core'a bağımlılık var; tersi yok.
- **Partial'lar:** `ManifestationDirector.*.cs`, `HexagonView.*.cs` — tek sınıf, dosya bölünmesi; taşıırken tüm partial'ları ve `.meta` çiftlerini birlikte taşı.
- **Tek hareket sistemi:** Skill sırasında oyuncu/boss konumu yalnız `MotionTemplateRunner` (Core) + `MotionTemplateBody` (Game). `SkillMotionDriver` / executor konum yazımı yasak.
- **Sahne:** `PrototypeBootstrap.Awake` ile kurulur; `.unity` / `.prefab` YAML elle düzenlenmez.
- **Derleme kontrolü:** `python tools/GameCompile/check.py` (CoreTests içinde de koşar).
- **JSON:** `ElementSystemJsonLoader` → `SkillMotorLoader.Load`; spec kopyası kök `AGENTS.md` §6.
