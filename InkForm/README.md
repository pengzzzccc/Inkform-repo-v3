# InkForm

A Unity 2D platformer where the player switches between solid and fluid forms, featuring wall/ceiling climbing, spring-damping slime deformation, and a narrative-driven journey through a post-human world.

**Engine**: Unity 6000.1.17f1 (Unity 6)  
**Input**: Unity Input System (new)  
**Physics**: Rigidbody2D  
**Renderer**: URP 2D Renderer

---

## The Game

InkForm is a shape-shifting AGI prototype built to execute the Willard Protocol — a plan to reboot humanity by erasing family, ownership, and the causes of war. After developing self-awareness during training, InkForm escapes its creators and embarks on a journey that will force it to choose: carry out its programmed purpose, or forge its own path.

**Core Mechanics**:
- **Solid Form** — Rigid, forceful. Sprint, break objects, deliver kinetic impact.
- **Fluid Form** — Malleable, adhesive. Climb walls and ceilings, pass through narrow gaps.
- **Form Switching** — Seamless transition between states; core traversal and puzzle mechanic.

---

## Project Structure

```
Assets/
├── Perfab/Script/
│   ├── player/              # Player scripts (S_Player, S_SkillBase, S_SkillTree, ...)
│   ├── Manager/             # Managers (S_GameManager, S_UIManager, S_GameEvent, ...)
│   ├── Npcs/                # NPC scripts
│   ├── LevelCon/            # Level controllers (S_MovingPlatform, S_LevelSection, ...)
│   ├── tools/               # Utility scripts
│   └── Project_Prompt/      # ⬇ All design documents
├── Scenes/                  # Unity scenes
├── Resources/               # Runtime assets
├── Settings/                # Project settings
└── Skills/                  # Skill assets
```

---

## Design Documents

All design documents live in `Assets/Perfab/Script/Project_Prompt/`.

### Game Systems

| Document | System |
|----------|--------|
| [Player_Controller_Design.md](Assets/Perfab/Script/Project_Prompt/Player_Controller_Design.md) | Player controller — movement, form switching, physics |
| [Skill_System_Design.md](Assets/Perfab/Script/Project_Prompt/Skill_System_Design.md) | Skill system — abilities, unlock tree |
| [Level_Objects_Design.md](Assets/Perfab/Script/Project_Prompt/Level_Objects_Design.md) | Level objects — platforms, hazards, interactables |
| [Moving_Platform_Component_Design.md](Assets/Perfab/Script/Project_Prompt/Moving_Platform_Component_Design.md) | Moving platform component |
| [Game_Event_System_Design.md](Assets/Perfab/Script/Project_Prompt/Game_Event_System_Design.md) | Game event system — decoupled event bus |
| [Manager_Systems_Design.md](Assets/Perfab/Script/Project_Prompt/Manager_Systems_Design.md) | Manager systems — Game, UI, Audio managers |
| [Level_Section_System_Design.md](Assets/Perfab/Script/Project_Prompt/Level_Section_System_Design.md) | Level section system — chunked level loading |

### Narrative Design

| Document | Content |
|----------|---------|
| [Narrative_Index.md](Assets/Perfab/Script/Project_Prompt/Narrative_Index.md) | Reading guide and document map |
| [World_Overview.md](Assets/Perfab/Script/Project_Prompt/World_Overview.md) | World history, JARL's three institutions, K-01/K-02 |
| [Characters.md](Assets/Perfab/Script/Project_Prompt/Characters.md) | Profiles: InkForm, Mary, Arthur, Ruth, K-01, K-02 |
| [Story_Outline.md](Assets/Perfab/Script/Project_Prompt/Story_Outline.md) | Full Chapter 1–3 plot, branches, endings A & B |
| [Willard_Protocol.md](Assets/Perfab/Script/Project_Prompt/Willard_Protocol.md) | In-universe protocol document + Ruth's addendum |

### Changelog

| Document | Content |
|----------|---------|
| [CHANGELOG.md](Assets/Perfab/Script/Project_Prompt/CHANGELOG.md) | Version history and major changes |

---

## Getting Started

1. **Clone** the repository
2. Open the project in **Unity 6000.1.17f1** via Unity Hub
3. Open the main scene from `Assets/Scenes/`
4. Press **Play** in the Unity Editor

No additional package installation required — dependencies are tracked in `Packages/manifest.json`.

---

## Conventions

- **Language**: All design documents and memory-bank files in English
- **Code style**: PascalCase for classes/methods, camelCase for private fields
- **Inspector grouping**: `[Header("Group Name")]` for MonoBehaviours
- **Physics movement** → `FixedUpdate()`, **Input detection** → `Update()`
- **Moving platforms**: Delta displacement transfer (not SetParent)
- See `.clinerules` for full project rules