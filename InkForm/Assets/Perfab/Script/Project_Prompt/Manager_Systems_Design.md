# Manager Systems 閳?Design Document

## 1. Overview

The manager systems handle game flow, UI management, audio playback, and global state. They consist of `S_ManagerRoot` (persistent container), `S_GameManager` (game flow control), `S_UIManager` (menu UI), and `S_AudioManager` (BGM + SFX). All systems communicate through `S_GameEvent` 閳?they never reference each other directly.

`S_InputBindingManager` is also part of the manager layer. It owns the shared `InputSystem_Actions` instance, saves runtime binding overrides with `PlayerPrefs`, and is used by both `S_Player` and `S_UIManager`.

`S_ManagerRoot` (v0.8.1) is the only object allowed to call `DontDestroyOnLoad`. All persistent managers are authored as children of `ManagerRoot.prefab`; child managers keep singleton guards but do not self-create, self-reparent, or preserve themselves.

---

## 2. Architecture

### 2.1 System Relationships

```
S_ManagerRoot (Singleton, DontDestroyOnLoad) - v0.8.1
|-- Single persistent prefab root for all manager singletons
|-- Destroys duplicate roots as a whole
|-- AttachPersistent(managerTransform) is compatibility-only; normal code must not use runtime reparenting
`-- RuntimeInitializeOnLoadMethod reset for editor domain reload

S_UIManager (child of ManagerRoot.prefab)
|-- Buttons: Start, Restart, Exit
|-- Toggle: OpenMenu input
|-- Death panel: red count + back to checkpoint button
|-- Energy UI: listens to OnPlayerEnergyChanged
`-- Controls: pause menu, death UI, gameplay input lock/unlock

S_GameManager (child of ManagerRoot.prefab)
|-- Scene flow using S_SceneReference drag references
|-- Ink fade transition + optional transition SFX
|-- Listens: OnGameStart, OnGameRestart, OnExit, OnLevelExitRequested, OnSceneLoadRequested
`-- Disables gameplay input during scene transitions

S_AudioManager (child of ManagerRoot.prefab)
|-- BGM playback (loop)
|-- SFX playback (one-shot)
|-- Platform Alarm (loop, triggered by section descent)
|-- Listens: OnPlaySFX, OnPlaySFXPitched, OnBGMChange, OnBgmVolumeChangeRequested, OnSfxVolumeChangeRequested, OnSectionDescentStarted, OnSectionDescentCompleted
`-- Volume control via events and Inspector Range sliders

S_SectionAlarmEffect (per-scene)
|-- Visual alarm effect during section descent
|-- Listens: OnSectionDescentStarted, OnSectionDescentCompleted
`-- Screen flash + tint effect

S_InputBindingManager (child of ManagerRoot.prefab)
|-- Owns one shared InputSystem_Actions instance
|-- Loads/saves binding overrides via PlayerPrefs
|-- Provides interactive rebinding for keyboard/mouse and gamepad
`-- Used by S_Player and S_UIManager
```

### 2.2 Lifecycle

| System | Persistence | Instantiation |
|--------|-------------|---------------|
| S_ManagerRoot | DontDestroyOnLoad root | Place full `ManagerRoot.prefab` in Start and gameplay scenes |
| S_GameManager | Persistent as root child | Direct child of `ManagerRoot.prefab` |
| S_UIManager | Persistent as root child | Direct child of `ManagerRoot.prefab` |
| S_InputBindingManager | Persistent as root child | Direct child of `ManagerRoot.prefab`; not auto-created |
| S_AudioManager | Persistent as root child | Direct child of `ManagerRoot.prefab` |
| S_SuspicionSystem | Persistent as root child | Direct child of `ManagerRoot.prefab` |
| S_SkillTree | Persistent as root child | Direct child of `ManagerRoot.prefab` |
| S_SectionAlarmEffect | Per-scene (destroyed on scene load) | Create in each gameplay scene |

### 2.3 Game Flow Diagram

```
Game Start:
  S_UIManager.StartButton --> S_GameEvent.GameStart()
  --> S_GameManager.GameStart() --> SceneManager.LoadScene()
  --> S_UIManager.HideUI() + Time.timeScale = 1

Player Death:
  S_coleve (lava contact) --> S_GameEvent.PlayerDied()
  --> S_UIManager.ShowDeathUI() --> Freeze time + show death panel
  --> Player clicks back to checkpoint
  --> S_GameEvent.GameReStart() --> S_SceneCheckpointTracker respawns player

Game Restart:
  S_UIManager.ReStartButton --> S_GameEvent.GameReStart()
  --> S_GameManager.GameReStart() --> Reset player position
  --> S_UIManager.HideUI() + Time.timeScale = 1

Game Exit:
  S_UIManager.ExitButton --> S_GameEvent.ExitGame()
  --> S_GameManager.ExitGame() --> Application.Quit()
```

---

## 3. Script Details

### 3.1 S_GameManager.cs

**Type**: MonoBehaviour (Singleton, persistent child of `ManagerRoot.prefab`)

**Singleton Pattern**: Uses `Instance` static property. If a new instance is created while one already exists, the duplicate is destroyed.

**Serialized Fields**:
| Field | Type | Description |
|-------|------|-------------|
| startMenuScene | S_SceneReference | Start menu scene selected by dragging a SceneAsset in Inspector |
| levelScenes | S_SceneReference[] | Ordered level flow, using scene path first and legacy name fallback |
| transitionFadeOutTime / transitionFadeInTime | float | Ink fade timing for scene transitions |
| transitionClip | AudioClip | Optional SFX played when transition starts |

**Runtime State**:
| Field | Type | Description |
|-------|------|-------------|
| currentLevelIndex | int | Active level index for `LoadNextLevel()` |
| sceneLoadRoutine | Coroutine | Guards against double scene-load triggers |

**Event Subscriptions**:
| Event | Handler | Action |
|-------|---------|--------|
| OnGameStart | `GameStart()` | Load scene, reset time scale |
| OnGameRestart | `GameReStart()` | Reset time scale and suspicion; checkpoint tracker handles respawn |
| OnExit | `ExitGame()` | Quit application |
| OnLevelExitRequested | `LoadNextLevel()` | Load the next configured scene |
| OnSceneLoadRequested | `LoadSceneByKey(string)` | Load scene by path/name runtime key |

**Key Methods**:
| Method | Description |
|--------|-------------|
| `StartFreshGameFromMenu()` | Reset session state and load the first configured level |
| `LoadLevel(int)` | Load a configured level by index |
| `LoadNextLevel()` | Advance through `levelScenes` |
| `ReturnToStartMenu()` | Load the configured start menu scene |
| `GameReStart()` | Reset time/suspicion and let checkpoint tracker respawn |
| `ExitGame()` | Call `Application.Quit()` |
| `CanLoadSceneKey(string)` | Validate scene path/name is loadable before transition starts |

**Important Notes**:
- `S_GameManager` uses `S_SceneReference` runtime keys; dragging SceneAssets in Inspector is preferred over typing names
- `GameReStart()` does NOT reload the scene; the checkpoint tracker respawns only after the death UI button is pressed
- During transitions, gameplay input is disabled and restored after fade-in
- `Time.timeScale` must be reset to 1 after being set to 0 by the menu

---

### 3.2 S_UIManager.cs

**Type**: MonoBehaviour (Singleton, persistent child of `ManagerRoot.prefab`)

**Persistence**: `S_UIManager` is authored under `ManagerRoot.prefab`. It never calls `DontDestroyOnLoad` or `AttachPersistent` during normal lifecycle.

**Serialized Fields**:
| Field | Type | Description |
|-------|------|-------------|
| background | GameObject | Root menu panel (contains all buttons) |
| StartButton | Button | Start game button |
| ReStartButton | Button | Restart level button |
| ExitButton | Button | Exit game button |
| ControlsButton | Button | Optional controls button; created at runtime if unset |
| energySlider / energyUI | Slider / GameObject | Energy HUD, created at runtime if unset |
| deathPanel / backToCheckpointButton | GameObject / Button | Independent death UI |
| deathCountText | TMP_Text | Red death counter displayed on death panel |

**Menu Toggle System**:
- Uses `S_InputBindingManager.Instance.Actions.UI.OpenMenu`
- `menuOpen` tracks show/hide state
- Opening the menu pauses (`Time.timeScale = 0`)
- Hiding the menu resumes (`Time.timeScale = 1`)
- OpenMenu is ignored while an interactive rebind is waiting for input

```
OpenMenu input pressed:
    if (menuOpen)
        HideUI() + Time.timeScale = 1   (resume)
    else
        ShowUI() + Time.timeScale = 0   (pause)
```

**Controls Binding UI**:
- Built at runtime under `background`
- Supports keyboard/mouse and gamepad bindings for Move, Jump, Sprint, Grip, and OpenMenu
- Saves binding overrides through `S_InputBindingManager`
- Provides reset-all and cancel-rebind controls

**Event Subscriptions**:
| Event | Handler | Action |
|-------|---------|--------|
| OnGameStart | `HideAllGameplayBlockingUI()` | Hide pause/death UI when game starts |
| OnGameRestart | `HideAllGameplayBlockingUI()` | Hide pause/death UI on checkpoint return |
| OnPlayerDied | `ShowDeathUI()` | Freeze time and show death panel |
| OnPlayerEnergyChanged | `UpdateEnergyBar(float, float)` | Refresh shared energy UI |
| OnKeyCountChanged | `UpdateKeyCount(int, int)` | Refresh key HUD |

**Key Methods**:
| Method | Description |
|--------|-------------|
| `Start()` | Set up button listeners and build controls UI |
| `Update()` | Check OpenMenu input for toggle |
| `ShowUI()` | Set background active and show pause menu |
| `HideUI()` | Set background inactive, cancel rebinding, resume time |
| `ShowDeathUI()` | Show death panel and select `back to checkpoint` |
| `BackToCheckpoint()` | Hide death UI and fire `GameReStart()` |
| `UpdateEnergyBar(float, float)` | Cache/update energy slider value |
| `OnStartButton()` | Fire `S_GameEvent.GameStart()` |
| `OnReStartButton()` | Fire `S_GameEvent.GameReStart()` |
| `OnExitButton()` | Fire `S_GameEvent.ExitGame()` |

---

## 4. Unity Setup

### ManagerRoot Prefab Setup
1. Place the full `Assets/Perfab/Player/ManagerRoot.prefab` in `Start.unity` and any gameplay scene that can be opened directly.
2. Keep these managers as direct prefab children: `S_GameManager`, `S_UIManager`, `S_InputBindingManager`, `S_AudioManager`, `S_SuspicionSystem`, `S_SkillTree`, and `S_PerformanceMonitor`.
3. Do not place standalone `UIManager.prefab` instances in scenes.
4. Do not create partial managers at runtime; `S_StartMenuController` validates the root and logs an error if it is missing.

### Scene Flow Setup
1. In `S_GameManager`, drag SceneAsset files into `startMenuScene` and `levelScenes`.
2. In `S_SceneChangeTrigger`, drag the target SceneAsset into `targetScene`.
3. Make sure dragged scenes are enabled in Build Settings / Build Profiles; loading validates this before transition starts.
4. Configure transition fade times and optional `transitionClip` in `S_GameManager`.

### UI Setup
1. `S_UIManager` lives under `ManagerRoot.prefab` with its menu Canvas and EventSystem hierarchy.
2. Energy UI and death UI can be assigned in Inspector or built at runtime if unset.
3. Death UI should expose only `back to checkpoint` by default; normal pause menu remains controlled by OpenMenu input.

### Scene Setup Checklist
```
[ ] Full ManagerRoot.prefab exists in the scene
[ ] No standalone UIManager prefab instance exists in the scene
[ ] S_GameManager levelScenes/startMenuScene use dragged SceneAsset references
[ ] Referenced scenes are enabled in Build Settings / Build Profiles
[ ] Player has S_PlayerEnergy and skill assets have energy costs configured
[ ] S_SceneCheckpointTracker can respawn from last checkpoint after GameReStart
```

---

## 5. Event Wiring

This section shows how current manager systems connect to other game systems.

### 5.1 Current Event Map

```
S_coleve / NPC arrest --(PlayerDied)--> S_GameEvent --> S_UIManager.ShowDeathUI()
BackToCheckpoint button --(GameReStart)--> S_GameEvent --> S_SceneCheckpointTracker respawn

S_StartMenuController --(StartFreshGameRequested)--> S_GameEvent --> S_GameManager.StartFreshGameFromMenu()
S_ExitGate --(LevelExitRequested)--> S_GameEvent --> S_GameManager.LoadNextLevel()
S_SceneChangeTrigger --(SceneLoadRequested runtimeKey)--> S_GameEvent --> S_GameManager transition load

S_PlayerEnergy --(PlayerEnergyChanged current,max)--> S_GameEvent --> S_UIManager energy bar
S_Player / systems --(GameplayInputEnabledRequested bool)--> S_GameEvent --> S_Player input lock

S_Checkpoint --(SpawnPointChanged Transform)--> S_GameEvent --> S_SceneCheckpointTracker cache spawn
S_Player / systems --(PlaySFX)--> S_GameEvent --> S_AudioManager.PlaySFX()
```

---

## 6. Common Issues

| Issue | Solution |
|-------|----------|
| Transform.SetParent assertion | Ensure only `S_ManagerRoot` calls `DontDestroyOnLoad`; child managers must not call `AttachPersistent()` during normal lifecycle |
| Start menu cannot build | Add the full `ManagerRoot.prefab` to `Start.unity`; runtime creation of partial managers is intentionally disabled |
| Scene not loading | Drag a valid SceneAsset into `S_GameManager`/`S_SceneChangeTrigger` and enable it in Build Settings / Build Profiles |
| Death opens pause menu | `OnPlayerDied` should call `ShowDeathUI()`, not `ShowUI()` |
| Energy bar missing | Confirm the active player has `S_PlayerEnergy` and `S_UIManager` is under `ManagerRoot.prefab` |
| Duplicate managers | Delete standalone scene manager instances; use the full `ManagerRoot.prefab` |

---

## 7. ManagerRoot Single Persistence Rule

`S_ManagerRoot` is the only persistent object. Duplicate roots are disabled/destroyed as whole roots, and child managers only keep singleton duplicate guards. `AttachPersistent()` remains for compatibility but should not be used by manager `Awake()` methods or new code.

Normal lifecycle:
```
Scene loads full ManagerRoot.prefab
  -> S_ManagerRoot.Awake() calls DontDestroyOnLoad(root)
  -> Child managers initialize in place
  -> Later scenes may include another ManagerRoot prefab
  -> Duplicate root is destroyed, persistent root remains active
```

---
## 8. S_Player Movement Lock API (v0.7.0)

`S_Player` exposes a `SetMovementLocked(bool)` method used by `S_HideSpot` and other systems that need to freeze the player in place.

### 8.1 API

| Method/Property | Type | Description |
|-----------------|------|-------------|
| `SetMovementLocked(bool locked)` | void | Locks/unlocks player movement. When locked: velocity zeroed, angular velocity zeroed, movement/jump/grip input blocked |
| `IsMovementLocked` | bool (read-only) | Returns current lock state |

### 8.2 Behavior When Locked

| System | Behavior |
|--------|----------|
| FixedUpdate | Velocity set to zero every frame, no SolidMovement/FluidMovement |
| Jump() | Returns immediately, no jump or sprint charge |
| StateRunner() | Gripping set to false |
| Rigidbody2D | Velocity and angular velocity zeroed on lock activation |

### 8.3 Integration with S_HideSpot

`S_HideSpot` calls `player.SetMovementLocked(true)` when hiding and `player.SetMovementLocked(false)` when exiting. This replaces the fallback Rigidbody freeze approach.

---

## 9. S_AudioManager Platform Alarm

`S_AudioManager` supports a looping platform alarm sound that plays during section descent events.

### 9.1 Serialized Fields

| Field | Default | Description |
|-------|---------|-------------|
| platformAlarmClip | - | AudioClip for the looping alarm |
| platformAlarmVolumeMultiplier | 1f | Volume multiplier applied on top of sfxVolume |

### 9.2 Behavior

```
OnSectionDescentStarted(int)
    |-- If not already playing: set clip, loop=true, volume = sfxVolume * multiplier, Play()

OnSectionDescentCompleted(int)
    |-- Stop alarm, clear clip
```

### 9.3 Volume

Alarm volume = `sfxVolume * platformAlarmVolumeMultiplier`. Adjusting `sfxVolume` in Inspector affects both SFX and alarm proportionally.

---

## 10. S_SectionAlarmEffect

`S_SectionAlarmEffect` provides a visual alarm effect (screen flash/tint) during section descent.

### 10.1 Event Subscriptions

| Event | Handler |
|-------|---------|
| `OnSectionDescentStarted` | Activate visual alarm effect |
| `OnSectionDescentCompleted` | Deactivate visual alarm effect |

### 10.2 Setup

Attach to a GameObject in the scene. The effect activates automatically when section descent begins.
