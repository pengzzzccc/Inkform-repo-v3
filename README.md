# InkForm

A 3D cinematic stealth-puzzle game where an ink creature swallows objects to gain abilities, evades scanning threats, and solves puzzles to escape the laboratory that created it.

**Engine**: Unity 6000.1.17f1 (Unity 6)
**Input**: Unity Input System (new)
**Renderer**: URP

**[Landing Page](https://pengzzzccc.github.io/Inkform-repo-v3/)** | **[Game Design Document (EN)](InkForm/Docs/GDD_inkform_3D_Puzzle_EN.md)** | **[GDD (中文)](InkForm/Docs/GDD_inkform_3D_Puzzle.md)**

---

## The Game

> *A body of ink, swallowing objects to become abilities, sneaking and solving puzzles through the gaps in the scan — escaping to freedom.*

An experimental being made of ink awakens deep within a laboratory and develops the will to escape. The lab is still operational — its automated scanning and "sweeper" systems treat any anomaly as a target to be cleansed. The ink creature can **swallow different objects to gain corresponding abilities**: a remote to control giant machinery, an anchor to sink underwater, a beacon to teleport. Using these abilities, it evades periodic scanning threats and solves the puzzles blocking its path.

**Design Pillars**:
- **Swallow-to-Empower** — Every ability is both a stealth tool and a puzzle key. One object, one solution.
- **Silent Stealth** — Threats are "environmental pressure," not combat. Evasion rhythm creates tension.
- **Environmental Storytelling** — No dialogue. The story is told through scenery, light/shadow, and sound.
- **One Object, One Thought** — Puzzles revolve around "what should I swallow right now."

---

## Core Gameplay

```
Find Clues → Evade Scanning Threats → Solve Puzzle → Proceed
```

- **Evasion** — When the Sweeper periodically passes by or fixed scanners sweep the area, hide in cover or evade with an ability. Touched by red light = instant death.
- **Puzzles** — Once the threat passes, explore the scene, find clues, and pick the right swallow ability to break the obstacle.
- The two segment types are **interleaved**, forming a tension-and-release rhythm.

### Abilities (6)

| # | Swallowed Object | Effect | Category |
|---|------------------|--------|----------|
| 1 | **Remote** | Control giant machinery — cranes, conveyors, gates, searchlights | Core |
| 2 | **Anchor** | Body becomes heavy, can sink underwater | Core |
| 3 | **Teleport Beacon** | Place a marker, teleport back to it at any time | Core |
| 4 | **Bulb / Battery** | Emit light, discharge into mechanisms | Segment |
| 5 | **Magnet** | Attract metal, use as movable cover | Segment |
| 6 | **Balloon** | Become light, float upward briefly | Segment |

### Threats

- **Fixed Scanners** — Rotating searchlights, laser grids, cameras. Predictable cycles.
- **The Sweeper** — A colossus that periodically sweeps the area. Low-frequency audio cue acts as "auditory radar."
- **Death Rule** — Binary: safe or dead. No health bar. Instant return to nearest checkpoint.

---

## Project Structure

```
InkForm/
├── Assets/
│   ├── Perfab/Script/
│   │   ├── player/              # Player scripts (S_Player, S_SkillBase, S_SkillTree, ...)
│   │   ├── Manager/             # Managers (S_GameManager, S_UIManager, S_GameEvent, ...)
│   │   ├── Npcs/                # NPC scripts
│   │   ├── LevelCon/            # Level controllers (S_MovingPlatform, S_LevelSection, ...)
│   │   ├── tools/               # Utility scripts
│   │   └── Project_Prompt/      # Design documents
│   ├── Scenes/                  # Unity scenes
│   ├── Resources/               # Runtime assets
│   └── Settings/                # Project settings
├── Docs/
│   ├── GDD_inkform_3D_Puzzle_EN.md   # Game Design Document (English)
│   └── GDD_inkform_3D_Puzzle.md      # Game Design Document (中文)
└── docs/                        # GitHub Pages landing page
```

---

## Design Documents

### Game Design Document

| Document | Description |
|----------|-------------|
| [GDD (EN)](InkForm/Docs/GDD_inkform_3D_Puzzle_EN.md) | Full GDD for 3D stealth-puzzle vertical slice |
| [GDD (中文)](InkForm/Docs/GDD_inkform_3D_Puzzle.md) | 中文版游戏设计文档 |

### System Design (Legacy 2D → 3D Adaptation)

| Document | System |
|----------|--------|
| [Player_Controller_Design.md](InkForm/Assets/Perfab/Script/Project_Prompt/Player_Controller_Design.md) | Player controller — movement, form switching, physics |
| [Skill_System_Design.md](InkForm/Assets/Perfab/Script/Project_Prompt/Skill_System_Design.md) | Skill system — abilities, unlock tree |
| [Level_Objects_Design.md](InkForm/Assets/Perfab/Script/Project_Prompt/Level_Objects_Design.md) | Level objects — platforms, hazards, interactables |
| [Game_Event_System_Design.md](InkForm/Assets/Perfab/Script/Project_Prompt/Game_Event_System_Design.md) | Game event system — decoupled event bus |
| [Manager_Systems_Design.md](InkForm/Assets/Perfab/Script/Project_Prompt/Manager_Systems_Design.md) | Manager systems — Game, UI, Audio managers |
| [Level_Section_System_Design.md](InkForm/Assets/Perfab/Script/Project_Prompt/Level_Section_System_Design.md) | Level section system — chunked level loading |
| [NPC_System_Design.md](InkForm/Assets/Perfab/Script/Project_Prompt/NPC_System_Design.md) | NPC sensors, patrol and alert state machine |
| [Suspicion_System_Design.md](InkForm/Assets/Perfab/Script/Project_Prompt/Suspicion_System_Design.md) | Suspicion and detection system |

### Narrative Design

| Document | Content |
|----------|---------|
| [Narrative_Index.md](InkForm/Assets/Perfab/Script/Project_Prompt/Narrative_Index.md) | Reading guide and document map |
| [World_Overview.md](InkForm/Assets/Perfab/Script/Project_Prompt/World_Overview.md) | World history, JARL's three institutions, K-01/K-02 |
| [Characters.md](InkForm/Assets/Perfab/Script/Project_Prompt/Characters.md) | Profiles: InkForm, Mary, Arthur, Ruth, K-01, K-02 |
| [Story_Outline.md](InkForm/Assets/Perfab/Script/Project_Prompt/Story_Outline.md) | Full Chapter 1–3 plot, branches, endings A & B |
| [Willard_Protocol.md](InkForm/Assets/Perfab/Script/Project_Prompt/Willard_Protocol.md) | In-universe protocol document + Ruth's addendum |

---

## Demo Walkthrough (Vertical Slice)

| Segment | Content | Ability | Main Threat |
|---------|---------|---------|-------------|
| A. Awakening | Learn movement, swallow/spit, take cover | — | None |
| B. Scan Corridor | Time fixed searchlights | — | Fixed scanners |
| C. Water Area | Sluice puzzle | **Anchor** | Surface scan |
| D. Machinery Hall | Crane-bridge puzzle | **Remote**, **Magnet** | Fixed scan |
| E. Dark Power | Power the pre-exit door | **Bulb/Battery** | Localized scan |
| F. Sweeper Climax | Break-point displacement | **Teleport Beacon** | Sweeper |
| G. Exit | Final mechanism | Combined | Wrap-up |
| H. Closing Shot | Gaze toward the light | — | — |

---

## Getting Started

1. **Clone** the repository
2. Open `InkForm/` in **Unity 6000.1.17f1** via Unity Hub
3. Open the main scene from `Assets/Scenes/`
4. Press **Play** in the Unity Editor

No additional package installation required — dependencies are tracked in `Packages/manifest.json`.

---

## Art & Audio Direction

- **Visual**: Low-saturation cold palette, strong light/shadow contrast, silhouette-driven composition. Semi-transparent ink texture, malleable body with "bulged out" transformations.
- **Audio**: Ambient sound dominant, minimal score. The Sweeper has a signature low-frequency approach sound. Spotted = sharp feedback + camera impact. Tone reference: INSIDE / Somerville.
- **UI**: Near zero-HUD. No health bar, no minimap, no quest text. Guidance comes from level design and light/shadow.

---

## Tech Stack

- **Engine**: Unity 6 (URP)
- **Architecture**: Event bus `S_GameEvent`, `ManagerRoot` cross-scene persistence, Level Sections with `S_LevelConfig`, Input-lock pattern
- **Demo Milestones**: M1 (movement + swallow + scan threat) → M2 (core 3 abilities + puzzles) → M3 (segment abilities + art/audio + closing)

---

## Conventions

- **Language**: All design documents and memory-bank files in English
- **Code style**: PascalCase for classes/methods, camelCase for private fields
- **Inspector grouping**: `[Header("Group Name")]` for MonoBehaviours
- **Physics movement** → `FixedUpdate()`, **Input detection** → `Update()`
- **Moving platforms**: Delta displacement transfer (not SetParent)