# Manager Systems — Design Document

## 1. Overview

The manager systems handle game flow, UI management, audio playback, and global state. They consist of `S_GameManager` (game flow control), `S_UIManager` (menu UI), and `S_AudioManager` (BGM + SFX). All systems communicate through `S_GameEvent` — they never reference each other directly.

---

## 2. Architecture

### 2.1 System Relationships

```
S_UIManager (DontDestroyOnLoad)
|-- Buttons: Start, Restart, Exit
|-- Toggle: OpenMenu input
|-- Listens: OnGameStart, OnGameRestart, OnPlayerDied
`-- Controls: Time.timeScale, background visibility

S_GameManager (per-scene)
|-- Player respawn management
|-- Scene loading
|-- Listens: OnPlayerDied, OnGameStart, OnGameRestart, OnExit, reNewSpwnPoint
`-- Spawn point tracking

S_AudioManager (per-scene)
|-- BGM playback (loop)
|-- SFX playback (one-shot)
|-- Listens: OnPlaySFX, OnBGMChange
`-- Volume control via Inspector Range sliders
```

### 2.2 Lifecycle

| System | Persistence | Instantiation |
|--------|-------------|---------------|
| S_UIManager | DontDestroyOnLoad (survives scene loads) | Create once in initial scene |
| S_GameManager | Per-scene (destroyed on scene load) | Create in each gameplay scene |
| S_AudioManager | Per-scene (destroyed on scene load) | Create in each gameplay scene |

### 2.3 Game Flow Diagram

```
Game Start:
  S_UIManager.StartButton --> S_GameEvent.GameStart()
  --> S_GameManager.GameStart() --> SceneManager.LoadScene()
  --> S_UIManager.HideUI() + Time.timeScale = 1

Player Death:
  S_coleve (lava contact) --> S_GameEvent.PlayerDied()
  --> S_GameManager.PlayerDied() --> Reset player position to spawn
  --> S_UIManager.ShowUI() --> Menu appears

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

**Type**: MonoBehaviour (Singleton, per-scene)

**Singleton Pattern**: Uses `Instance` static property. If a new instance is created while one already exists, the duplicate is destroyed.

**Serialized Fields**:
| Field | Type | Description |
|-------|------|-------------|
| scene | string | Scene name to load on game start |

**Runtime State**:
| Field | Type | Description |
|-------|------|-------------|
| player | S_Player | Reference to S_Player (found via `FindAnyObjectByType` in Start) |
| spwnPoint | Transform | Current spawn point (starts at player position) |

**Event Subscriptions**:
| Event | Handler | Action |
|-------|---------|--------|
| OnPlayerDied | `PlayerDied()` | Teleport player to spawn point |
| OnGameStart | `GameStart()` | Load scene, reset time scale |
| OnGameRestart | `GameReStart()` | Reset player position, reset time scale |
| OnExit | `ExitGame()` | Quit application |
| reNewSpwnPoint | `newSpwn(Transform)` | Update spawn point to new checkpoint |

**Key Methods**:
| Method | Description |
|--------|-------------|
| `Start()` | Find S_Player instance, initialize spawn point |
| `PlayerDied()` | Teleport player back to spawn point |
| `GameStart()` | Load the target scene via `SceneManager.LoadScene(scene)` |
| `GameReStart()` | Reset player to spawn point, set `Time.timeScale = 1` |
| `ExitGame()` | Call `Application.Quit()` |
| `newSpwn(Transform)` | Update `spwnPoint` to new checkpoint position |

**Important Notes**:
- `GameStart()` uses `SceneManager.LoadScene()` which destroys the current scene (including this GameManager)
- The new scene must have its own S_GameManager instance
- `GameReStart()` does NOT reload the scene — it just resets the player position
- `Time.timeScale` must be reset to 1 after being set to 0 by the menu

---

### 3.2 S_UIManager.cs

**Type**: MonoBehaviour (Singleton, DontDestroyOnLoad)

**Persistence**: This manager survives scene loads. Create it once in the initial scene and it persists throughout the game session.

**Serialized Fields**:
| Field | Type | Description |
|-------|------|-------------|
| background | GameObject | Root menu panel (contains all buttons) |
| StartButton | Button | Start game button |
| ReStartButton | Button | Restart level button |
| ExitButton | Button | Exit game button |

**Menu Toggle System**:
- Uses `InputSystem_Actions.UI.OpenMenu` input action
- Parity counter (`menuCount`) alternates between show/hide
- Even count (0, 2, 4...): Show menu + pause (`Time.timeScale = 0`)
- Odd count (1, 3, 5...): Hide menu + resume (`Time.timeScale = 1`)

```
OpenMenu input pressed:
    menuCount++
    if (menuCount % 2 == 0)
        ShowUI() + Time.timeScale = 0   (pause)
    else
        HideUI() + Time.timeScale = 1   (resume)
```

**Event Subscriptions**:
| Event | Handler | Action |
|-------|---------|--------|
| OnGameStart | `HideUI()` | Hide menu when game starts |
| OnGameRestart | `HideUI()` | Hide menu on restart |
| OnPlayerDied | `ShowUI()` | Show menu on death |

**Key Methods**:
| Method | Description |
|--------|-------------|
| `Start()` | Subscribe to events, set up button listeners |
| `Update()` | Check OpenMenu input for toggle |
| `ShowUI()` | Set background active, fire OnGameStart/OnGameRestart |
| `HideUI()` | Set background inactive |
| `OnStartButton()` | Fire `S_GameEvent.GameStart()` |
| `OnReStartButton()` | Fire `S_GameEvent.GameReStart()` |
| `OnExitButton()` | Fire `S_GameEvent.ExitGame()` |

---

## 4. Unity Setup

### S_GameManager (per-scene)
1. Create a GameManager GameObject in each gameplay scene
2. Add `S_GameManager` component
3. Set the `scene` field to the target scene name for the Start button
4. Ensure there is only one per scene (Singleton enforces this, but clean setup is better)

### S_UIManager (global)
1. Create a UIManager GameObject in the initial scene (will persist via DontDestroyOnLoad)
2. Add `S_UIManager` component
3. Create a Canvas with:
   - A background panel (initially inactive)
   - Start/Restart/Exit buttons as children of the background
4. Assign all UI references in Inspector
5. Set up button OnClick events to call `OnStartButton()`, `OnReStartButton()`, `OnExitButton()`

### S_AudioManager (per-scene)
1. Create an AudioManager GameObject in each gameplay scene
2. Add `S_AudioManager` component
3. Assign `bgmClip` in Inspector (optional — auto-plays on Start)
4. Adjust `bgmVolume` and `sfxVolume` sliders (0-1)
5. AudioSources are created programmatically — no manual setup needed

### Scene Setup Checklist
```
[ ] S_UIManager exists in initial scene (DontDestroyOnLoad)
[ ] S_GameManager exists in each gameplay scene
[ ] S_AudioManager exists in each gameplay scene (with bgmClip assigned)
[ ] S_GameManager.scene is set to the correct scene name
[ ] Player is tagged "Player"
[ ] S_coleve is on the player body for death detection
[ ] S_Checkpoint exists in the level for spawn point updates
[ ] Player has jumpClip / formSwitchClip assigned for SFX
```

---

## 5. Event Wiring

This section shows how all events connect the manager systems to other game systems.

### 5.1 Complete Event Map

```
S_coleve ──(PlayerDied)──> S_GameEvent ──> S_GameManager.PlayerDied()
                                         ──> S_UIManager.ShowUI()

S_UIManager ──(GameStart)──> S_GameEvent ──> S_GameManager.GameStart()
                                          ──> S_UIManager.HideUI()

S_UIManager ──(GameReStart)──> S_GameEvent ──> S_GameManager.GameReStart()
                                           ──> S_UIManager.HideUI()

S_UIManager ──(ExitGame)──> S_GameEvent ──> S_GameManager.ExitGame()

S_Checkpoint ──(ReNewSpwnPoint)──> S_GameEvent ──> S_GameManager.newSpwn()

S_SectionGoal ──(SectionStart)──> S_GameEvent ──> S_LevelSectionController

S_SectionGoal ──(SectionEnd)──> S_GameEvent ──> S_LevelSectionController

S_Player ──(PlaySFX)──> S_GameEvent ──> S_AudioManager.PlaySFX()

Any System ──(BGMChange)──> S_GameEvent ──> S_AudioManager.PlayBGM()
```

---

## 6. Common Issues

| Issue | Solution |
|-------|----------|
| Menu not showing on death | Check S_UIManager is subscribed to OnPlayerDied in OnEnable() |
| Scene not loading | Verify scene name in S_GameManager.scene field matches Build Settings |
| Player not respawning | Check S_Player.Instance and spwnPoint are not null |
| Menu toggle not working | Ensure InputSystem_Actions.UI.OpenMenu is bound in Input Actions |
| Time scale stuck at 0 | Check GameReStart properly sets Time.timeScale = 1 |
| Duplicate managers | Ensure only one S_UIManager and one S_GameManager per context |
| Button clicks not responding | Verify OnClick events are wired in the Button Inspector |