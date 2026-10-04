# Game katmanı (ajan notu)

Kök `AGENTS.md` + `docs/MAP.md`. Hedef klasör/namespace düzeni: `docs/ARCHITECTURE-PLAN.md`.

## Klasör → namespace

| Klasör | Namespace | Konu |
|--------|-----------|------|
| `Casting/` | `Dovus.Game.Casting` | Hexagon, joystick, syllable UI |
| `Skills/` | `Dovus.Game.Skills` | ManifestationDirector, LivingEffectView |
| `Skills/Execution/` | `Dovus.Game.Skills.Execution` | Skill executor VFX/mesh |
| `Vfx/` | `Dovus.Game.Vfx` | Particles, flashes, trails |
| `Boss/` | `Dovus.Game.Boss` | Boss encounter, telegraphs |
| `Actors/` | `Dovus.Game.Actors` | Player/ally motion, vitals, grounding |
| `Weapons/` | `Dovus.Game.Weapons` | Weapon SO, grips, visuals |
| `Team/` | `Dovus.Game.Team` | Portal border team hooks |
| `Cameras/` | `Dovus.Game.Cameras` | Follow/orbit camera (not `Camera` — Unity conflict) |
| `Hud/` | `Dovus.Game.Hud` | Combat HUD overlays |
| `Arena/` | `Dovus.Game.Arena` | Arena geometry, ambience (not `Environment`) |
| `Feel/` | `Dovus.Game.Feel` | Combat feel, haptics, freeze |
| `Audio/` | `Dovus.Game.Audio` | SFX director/library |
| `Data/` | `Dovus.Game.Data` | Element/rune loaders, SOs |
| `Config/` | `Dovus.Game.Config` | Tuning presets |
| `Composition/` | `Dovus.Game.Composition` | Bootstrap, clock, factory |
| `DevTools/` | `Dovus.Game.DevTools` | Debug panels and practice modes |
| `Editor/` | `Dovus.Game.Editor` | `Dovus.Game.Editor` — binders, importers (files stay here) |

**Yeni dosya:** uygun konu klasörüne koy; `namespace` = `Dovus.Game.<Klasör>` (alt klasörler nokta ile).

- **Unity katmanı:** `MonoBehaviour`, `Update`, `Resources`, sahne objeleri burada. Core'a bağımlılık var; tersi yok.
- **Partial'lar:** `ManifestationDirector.*.cs`, `HexagonView.*.cs` — tek sınıf, dosya bölünmesi; taşıırken tüm partial'ları ve `.meta` çiftlerini birlikte taşı.
- **Tek hareket sistemi:** Skill sırasında oyuncu/boss konumu yalnız `MotionTemplateRunner` (Core) + `MotionTemplateBodyHost` (Game/Actors). `SkillMotionDriver` / executor konum yazımı yasak; `SkillMotionMotor` yalnız saf plan (tür, i-frame, hedef) — konumu yalnız kalıp yazar.
- **Sahne:** `GameBootstrapHost.Awake` ile kurulur (Composition); `.unity` / `.prefab` YAML elle düzenlenmez.
- **Derleme kontrolü:** `python tools/GameCompile/check.py` (CoreTests içinde de koşar).
- **JSON:** `ElementSystemJsonLoader` → `SkillMotorLoader.Load`; spec kopyası kök `AGENTS.md` §6.
