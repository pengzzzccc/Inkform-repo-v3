# InkForm Architecture

> Current architecture after the level-flow refactor.

## Core Principles

- `ManagerRoot.prefab` is the only persistent root. It owns global managers and is preserved with `DontDestroyOnLoad`.
- `S_GameManager` is a scene service. It loads scenes, owns transition fade, resets time scale, cancels active skills, and validates `S_SceneReference` keys.
- `S_RunFlowController` owns gameplay progression. It reads `S_RunFlowConfig` and decides which scene should load next.
- `S_RunFlowConfig` is the single authoring entry point for fixed training, random training, facility rooms, and ending.
- Gameplay objects publish intent through `S_GameEvent`; flow and managers subscribe.

## Runtime Flow

```text
Start menu
  -> S_GameEvent.RunStartRequested()
  -> S_RunFlowController.StartNewRun()
  -> fixed training list
  -> random training pool
  -> facility entry room from RoomGraph
  -> facility room exits / ending trigger
  -> END scene
```

Scene loading is always requested through:

```text
S_GameEvent.SceneLoadRequested(sceneKey)
  -> S_GameManager.LoadScene(sceneKey)
```

## Main Systems

| Area | Main classes | Responsibility |
| --- | --- | --- |
| Persistent root | `S_ManagerRoot` | Owns global managers and prevents duplicate roots |
| Scene service | `S_GameManager`, `S_SceneReference` | Scene loading, transition overlay, frame rate |
| Flow | `S_RunFlowController`, `S_RunFlowConfig`, `S_LevelSceneEntry` | Run phases and level routing |
| Events | `S_GameEvent` | Static event bus for gameplay intent |
| Training | `S_TrainingLevelConfig`, `S_TutorialController`, `S_CountdownTimer` | Per-training intro, prompt, camera pan, countdown |
| Facility | `S_RoomGraph`, `S_RoomExit`, `S_EndingTrigger` | Room topology and facility navigation |
| Respawn | `S_Checkpoint`, `S_SceneCheckpointTracker` | Scene-scoped checkpoint respawn |
| Level objects | `S_ExitGate`, `S_InkPod`, `S_SectionGoal` | Completion triggers and local interactions |
| Player | `S_Player`, `S_PlayerSkillController`, `S_PlayerEnergy` | Movement, skills, shared energy |
| NPC/stealth | `S_NPCEnemy`, `S_SuspicionSystem`, `S_HideSpot` | Detection, suspicion, hiding |

## File Layout

```text
Assets/Perfab/Script/
├── Core/
│   ├── S_SceneReference.cs
│   └── Events/S_GameEvent.cs
├── Level/
│   ├── Flow/
│   │   ├── S_RunFlowConfig.cs
│   │   ├── S_RunFlowController.cs
│   │   └── S_RunFlowTypes.cs
│   ├── Interactables/
│   ├── Platforms/
│   ├── Progression/
│   ├── Resources/
│   ├── Sections/
│   └── Zones/
├── Managers/
├── NPCs/
├── Player/
├── Systems/
├── Tools/
└── Tutorial/
```

Configuration assets live under:

```text
Assets/Perfab/Configs/Levels/
├── RunFlowConfig.asset
├── Facility/RoomGraph.asset
└── Training/*_TrainingLevelConfig.asset
```

## Event Boundaries

- UI requests a new run with `RunStartRequested`.
- Death UI and pause restart request checkpoint return with `RespawnRequested`.
- Level exits request progression with `LevelCompleted(S_LevelCompletionReason)`.
- Facility exits request graph movement with `RoomEnterRequested(RoomId)`.
- Ending triggers request the ending with `EndingRequested`.
- Only `S_GameManager` should consume `SceneLoadRequested` for actual Unity scene loading.

## Authoring Reference

Use `Level_Flow_Configuration_Guide.md` for step-by-step setup and field meanings.
