# Manager Systems Design

## Purpose

The manager layer keeps global services stable across scene loads. It does not author level order directly; level order is configured in `S_RunFlowConfig` and interpreted by `S_RunFlowController`.

## ManagerRoot

`S_ManagerRoot` is the only object allowed to call `DontDestroyOnLoad`.

Required children:

- `GameManager`
- `RunFlowController`
- `InputBindingManager`
- `AudioManager`
- `SuspicionSystem`
- `SkillTree`
- `UIManager`

Scenes that can be opened directly for testing should contain the full `ManagerRoot.prefab` at scene root.

## GameManager

`S_GameManager` responsibilities:

- Listen to `S_GameEvent.OnSceneLoadRequested`.
- Validate scene keys through `S_SceneReference.CanLoadScene`.
- Run fade-out/load/fade-in transitions.
- Lock gameplay input while transitions run.
- Reset time scale and fixed delta time before scene changes.
- Cancel active player skills before scene changes.
- Handle frame-rate lock/unlock.
- Handle application exit.

It does not know fixed training order, random training pools, or facility topology.

## RunFlowController

`S_RunFlowController` responsibilities:

- Listen to `RunStartRequested`, `LevelCompleted`, `RoomEnterRequested`, `EndingRequested`, and `ReturnToStartMenuRequested`.
- Load fixed training entries in order.
- Draw random training entries without repetition for the configured count.
- Enter a random `RoomGraph` facility entry room.
- Validate facility room adjacency.
- Route ending requests to the configured ending scene.

It requests scene loads through `S_GameEvent.SceneLoadRequested(sceneKey)`.

## UIManager

`S_UIManager` responsibilities:

- Pause menu display and input locking.
- Death UI display.
- Restart/back-to-checkpoint button sends `RespawnRequested`.
- Start button sends `RunStartRequested`.
- Key count, energy, and suspicion UI.

`S_UIManager` does not decide which scene comes next.

## StartMenuController

`S_StartMenuController` builds the runtime start menu for `Start.unity`.

- start -> `RunStartRequested`
- settings -> input rebinding/volume controls
- exit -> `ExitGame`

It validates that `ManagerRoot.prefab` contains required manager children, including `RunFlowController`.

## AudioManager

`S_AudioManager` remains event-driven:

- `PlaySFX`
- `PlaySFX` with pitch/volume
- `BGMChange`
- volume change requests
- section descent alarm events

## Respawn Flow

```text
Player dies
  -> S_GameEvent.PlayerDied()
  -> S_UIManager shows death UI and pauses time

Death UI restart button
  -> S_GameEvent.RespawnRequested()
  -> S_SceneCheckpointTracker teleports to checkpoint
     or requests current scene reload through SceneLoadRequested
```

## Scene Flow

```text
Gameplay trigger
  -> S_GameEvent.LevelCompleted(reason)
  -> S_RunFlowController chooses next scene
  -> S_GameEvent.SceneLoadRequested(sceneKey)
  -> S_GameManager loads scene with transition
```

## Configuration

The manager prefab should only bind:

- `S_RunFlowController.runFlowConfig = Assets/Perfab/Configs/Levels/RunFlowConfig.asset`
- transition settings on `S_GameManager`
- service-specific settings on UI/audio/input/suspicion/skill managers

Do not configure level order on `S_GameManager`.
