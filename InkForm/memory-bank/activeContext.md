# Active Context

## Current Version
v0.8.0 (Architecture Refactor — Modular Directory + Interface Abstraction)

## Recent Changes (2026-05-25)
- **Major Directory Restructure**: Flat structure → modular directory tree
- **IPlayerActor Interface**: S_Player now implements `IPlayerActor`, decoupling player references
- **S_PlayerLookup**: Static utility for finding player via Collider2D/Collision2D/active instance
- **S_PlayerSkillController**: Extracted sprint charge + camera control logic from S_Player
- **S_ManagerRoot**: Persistent manager root node (DontDestroyOnLoad) with `AttachPersistent` API
- **S_SceneCheckpointTracker**: Per-scene checkpoint/respawn tracker using `[RuntimeInitializeOnLoadMethod]`
- **S_GameEvent Expansion**: 12+ new events for scene management, volume control, suspicion refinement

## New Directory Structure
```
Script/
├── Camera/           (S_CameraMove, S_ParallaxLayer)
├── Core/Events/      (S_GameEvent)
├── Input/            (InputSystem_Actions)
├── Level/
│   ├── Interactables/ (BreakableBlock, ButtonDoor, Checkpoint, Door, ExitGate, HideSpot, JumpPad, Key, Pipline, SceneCheckpointTracker)
│   ├── Platforms/     (MoveBlock, MovingPlatform, PlatformCableVisual)
│   ├── Resources/     (DroppedResourceItem, DropResourceCounter)
│   ├── Sections/      (LevelSection, LevelSectionController, SectionAlarmEffect, SectionGoal)
│   └── Zones/         (CantClimb)
├── Managers/         (AudioManager, GameManager, InputBindingManager, ManagerRoot, SceneChangeTrigger, StartMenuController, UIManager)
├── MCTS/             (MCTSBotController, MCTSGameState, MCTSNode, LevelTestMetrics)
├── NPCs/
│   ├── Combat/       (EMProjectile, NPCEnemy)
│   ├── Core/         (NPCbase)
│   ├── Dialogue/     (NPCDialogue, NPCStory)
│   ├── Sensors/      (NPCCamera)
│   └── Spawning/     (NPCWaveSpawner)
├── Player/
│   ├── Body/         (PlayerDynamicCollider, PlayerProceduralRenderer)
│   ├── Core/         (Player, PlayerContracts)
│   ├── Physics/      (coleve)
│   └── Skills/       (CameraControlSkill, fluid_climb, PlayerSkillController, SkillBase, SkillTree, Soild_sprint)
├── Systems/Suspicion/ (SuspicionSystem)
└── Tools/            (NPCSpawnerTool, PerformanceMonitor, setTrigger)
```

## Key Design Decisions
- **IPlayerActor interface** abstracts player for NPC/Level systems (no direct S_Player dependency)
- **S_PlayerLookup.TryGet** resolves IPlayerActor from any collider (component hierarchy + tag fallback)
- **S_PlayerSkillController** owns sprint charge state machine + camera control, injected by S_Player
- **S_ManagerRoot** is the single DontDestroyOnLoad container; managers attach via `AttachPersistent`
- **S_SceneCheckpointTracker** auto-creates per scene via `[RuntimeInitializeOnLoadMethod]`, tracks spawn position, handles respawn via `IPlayerActor.Teleport`
- Sprint direction uses release-time facing (no initial direction lock)
- Quick-tap buffer (0.15s) for instant dash
- Stage-based cooldowns (0.1s/0.5s/1.0s) instead of flat cooldown
- NPC jump uses predictive landing spot calculation

## Active Systems
- Player Controller (IPlayerActor, solid/fluid form, wall climb, wall jump, paralyze)
- Sprint Charge (S_PlayerSkillController + S_Soild_sprint + S_PlayerProceduralRenderer + S_PlayerDynamicCollider)
- Camera Control (S_PlayerSkillController + S_CameraControlSkill + S_CameraMove)
- Skill Tree (S_SkillTree → S_SkillBase subclasses)
- Manager Root (S_ManagerRoot with AttachPersistent lifecycle)
- Scene Checkpoint (S_SceneCheckpointTracker per-scene auto-creation)
- NPC System (S_NPCEnemy 5-state FSM, jump, wave spawner)
- Suspicion System (event-driven via S_GameEvent)
- Level Sections (dual-trigger, section-level movement)

## Pending
- Unity Editor testing of refactored systems
- Balance tuning for sprint charge / NPC jump parameters
- Verify all cross-references survive the directory restructure

## Documentation Updated
- [x] memory-bank/activeContext.md
- [ ] memory-bank/progress.md
- [ ] Architecture.md (full Mermaid rewrite)
- [ ] CHANGELOG.md (v0.8.0 entry)