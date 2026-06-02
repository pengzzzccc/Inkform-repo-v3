# Active Context

## Current Version
v0.9.0 (Run Flow Configuration Refactor)

## Recent Changes (2026-06-01)
- **Run Flow Config**: Added `S_RunFlowConfig` as the single authoring entry point for fixed training, random training, facility rooms, and ending.
- **Run Flow Controller**: Replaced the old progression controller with `S_RunFlowController` under `Assets/Perfab/Script/Level/Flow`.
- **GameManager Simplification**: `S_GameManager` is now a scene service only: scene loading, transition fade, input lock, frame-rate settings, and exit.
- **Event Refresh**: Runtime flow now uses `RunStartRequested`, `RespawnRequested`, `LevelCompleted`, `RoomEnterRequested(RoomId)`, and `EndingRequested`.
- **Training Config Rename**: `S_LevelConfig` became `S_TrainingLevelConfig`; training assets moved to `Assets/Perfab/Configs/Levels/Training/`.
- **Facility Config Move**: `RoomGraph.asset` moved to `Assets/Perfab/Configs/Levels/Facility/`.
- **Configuration Guide**: Added `Assets/Perfab/Script/Project_Prompt/Level_Flow_Configuration_Guide.md`.

## Recent Changes (2026-05-25)
- **ManagerRoot Single Persistence**: `ManagerRoot.prefab` is now the only `DontDestroyOnLoad` object; child managers no longer self-create, self-reparent, or call `AttachPersistent()` in normal lifecycle.
- **UIManager Migration**: `S_UIManager` now lives under `ManagerRoot.prefab`; standalone scene UIManager instances were removed.
- **Scene Flow**: `S_GameManager` and `S_SceneChangeTrigger` use `S_SceneReference` for Inspector-dragged SceneAssets, with runtime scene path/name fallback and load validation.
- **Scene Transitions**: Scene changes use fade out/load/fade in and optional transition SFX while gameplay input is disabled.
- **Shared Player Energy**: Added `S_PlayerEnergy`, `OnPlayerEnergyChanged`, skill energy thresholds/drain, sprint quick tap cost, and runtime energy UI.
- **Death UI**: Death shows an independent panel with red death count and `back to checkpoint`; checkpoint respawn waits for the button.
- **Gameplay Fixes**: Dropped keys pop out and hover near ground; NPC/player rigidbodies use continuous/interpolated physics; climb grip and moving-platform jump reset were stabilized.

## New Directory Structure
```
Script/
├── Camera/           (S_CameraMove, S_ParallaxLayer)
├── Core/             (S_SceneReference)
│   └── Events/       (S_GameEvent)
├── Input/            (InputSystem_Actions)
├── Level/
│   ├── Flow/          (RunFlowConfig, RunFlowController, RunFlowTypes)
│   ├── Interactables/ (BreakableBlock, Checkpoint, ExitGate, HideSpot, Key, SceneCheckpointTracker)
│   ├── Platforms/     (MoveBlock, MovingPlatform, PlatformCableVisual)
│   ├── Resources/     (DroppedResourceItem, DropResourceCounter)
│   ├── Sections/      (LevelSection, LevelSectionController, SectionAlarmEffect, SectionGoal)
│   └── Zones/         (CantClimb)
├── Managers/         (AudioManager, GameManager, InputBindingManager, ManagerRoot, SceneChangeTrigger, StartMenuController, UIManager)
├── NPCs/             (Core, Combat, Dialogue, Sensors, Spawning)
├── Player/
│   ├── Body/          (PlayerDynamicCollider, PlayerProceduralRenderer)
│   ├── Core/          (Player, PlayerContracts)
│   ├── Physics/       (coleve)
│   └── Skills/        (CameraControlSkill, fluid_climb, PlayerEnergy, PlayerSkillController, SkillBase, SkillTree, Soild_sprint)
├── Systems/Suspicion/ (SuspicionSystem)
└── Tools/            (NPCSpawnerTool, PerformanceMonitor, setTrigger)
```

## Key Design Decisions
- **IPlayerActor interface** abstracts player for NPC/Level systems; `S_PlayerLookup.TryGet` resolves player actors from colliders or active instance.
- **S_PlayerSkillController** owns sprint charge and camera control, injected by `S_Player`.
- **ManagerRoot prefab single persistence**: only the root calls `DontDestroyOnLoad`; child managers are authored under the prefab and must not runtime-reparent.
- **S_RunFlowConfig** is the only place to configure authored level order and facility flow.
- **S_SceneReference** is the preferred scene configuration path for all flow and scene-load assets.
- **S_PlayerEnergy** is the shared energy pool for active skills; skill assets configure their own energy costs.
- **S_SceneCheckpointTracker** auto-creates per scene, tracks spawn position, and respawns only on `RespawnRequested` after death UI confirmation.
- Sprint direction uses release-time facing; quick-tap sprint spends `quickTapEnergyCost`.
- NPC/player Rigidbody2D setup uses Continuous + Interpolate for more stable high-speed motion.

## Active Systems
- Player Controller (IPlayerActor, solid/fluid form, wall climb, wall jump, paralyze)
- Sprint Charge (S_PlayerSkillController + S_Soild_sprint + S_PlayerEnergy)
- Camera Control (S_PlayerSkillController + S_CameraControlSkill + S_CameraMove)
- Skill Tree (S_SkillTree under ManagerRoot prefab)
- Shared Player Energy (S_PlayerEnergy + skill asset energy costs + UI energy bar)
- Manager Root (single persistent ManagerRoot.prefab; AttachPersistent compatibility-only)
- Scene Flow (S_RunFlowConfig + S_RunFlowController + S_SceneReference)
- Scene Loading (S_GameManager transition fade/SFX, validated runtime scene keys)
- Scene Checkpoint (S_SceneCheckpointTracker per-scene auto-creation, respawn on RespawnRequested)
- Death UI (independent death panel + back to checkpoint flow)
- NPC System (S_NPCEnemy 5-state FSM, continuous/interpolated Rigidbody2D, wave spawner)
- Suspicion System (event-driven via S_GameEvent)
- Level Sections (dual-trigger, section-level movement)
- Key & Exit Gate System (dropped key pop-out/hover + scene progression)

## Pending
- Unity Editor testing of v0.8.1 scene flow and ManagerRoot duplicate behavior
- Balance tuning for energy drain/regen, sprint quick tap cost, NPC jump parameters
- Verify Build Settings / Build Profiles include Start, Train 1-6, ComR, PS, BF, LivA, For, and END

## Documentation Updated
- [x] memory-bank/activeContext.md
- [x] memory-bank/progress.md
- [x] memory-bank/error-log.md
- [x] CHANGELOG.md (v0.8.1 entry)
- [x] Architecture.md
- [x] Manager_Systems_Design.md
- [x] Game_Event_System_Design.md
- [x] Skill_System_Design.md
- [x] Player_Controller_Design.md
- [x] Level_Objects_Design.md


## 增加新功能
- hook技能，当玩家处于流体模式的时候，检测周围的一个数额的范围内有没有hook，如果有的话再hook上显示与hook交互的按键（使用当前的交互按键）。再玩家按下hook交互键之后，玩家需要射出一条触手钩住hook（好好研究一下如果程序化渲染这个部分）。玩家可以再hook上通过移动键左右摇摆（也要有程序化渲染展示）。玩家只能一次性钩住一个钩子，再范围内同时又多个钩子的时候选择朝向方向最近的hook进行连接。