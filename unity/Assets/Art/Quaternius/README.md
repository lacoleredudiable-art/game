# Quaternius deneme seti (CC0)

Fab kilidi yüzünden geçici. Kaynak: OpenGameArt / Quaternius.

| Klasör | Rol | Öneri |
|---|---|---|
| `Characters/.../Warrior.fbx` | Oyuncu | Prefab: `Prefabs/PlayerVisual_Quaternius` |
| `Dungeon/ModularDungeon/` | Arena | Prefab: `Prefabs/ArenaVisual_Quaternius` |
| `BossCandidates/.../Demon.fbx` | Boss | Prefab: `Prefabs/BossVisual_Quaternius` |

## Anim

Controller: `Animators/Player_Quaternius` · `Animators/Boss_Quaternius`

| Oyun | Quaternius clip |
|---|---|
| Idle/Walk/Run | Idle_Weapon / Walk / Run (loop kopya) |
| Dodge | Roll |
| **Düz vuruş** | **Sword_AttackFast** @1.55 (`BasicStrike`) |
| Hit / Death | RecieveHit / Death |
| CastPierce… | Sword_AttackFast / Sword_Attack / Punch… |
| Boss Windup / Slam | Jump / Bite_Front |
| Boss Stagger / Death | HitRecieve / Death |

Kancalar: `ActorVisual`, `BossVisual` (Speed, trigger’lar).
