# Game Event System 闂?Design Document

## 1. Overview

The game event system (`S_GameEvent`) is a static event bus that provides centralized, decoupled communication between all game systems. Any script can invoke or listen to events without direct references to other components. This is the backbone for inter-system communication in InkForm.

### Why a Static Event Bus?

In a game with player controller, skill system, level sections, UI, and managers, direct references between systems create tight coupling. The event bus pattern allows:
- **Decoupling**: Producers don't know about consumers, and vice versa
- **Flexibility**: New systems can listen to existing events without modifying the producer
- **Single responsibility**: Each system only fires events; it doesn't manage who responds

---

## 2. Architecture

### 2.1 Communication Pattern

```
Producer (invokes event)          Consumer (subscribes to event)
    |                                     |
    |   S_GameEvent.SectionStart(0)       |
    +----> S_GameEvent (static bus) <-----+
           OnSectionStart += handler
```

### 2.2 Event Inventory (30+ events as of v0.8.1)

| Event | Parameter | Producer | Consumer |
|-------|-----------|----------|----------|
| **Game Lifecycle** | | | |
| OnPlayerDied | none | S_coleve (lava), hazards, NPC arrest | S_UIManager |
| OnGameStart | none | S_UIManager (Start button) | S_GameManager |
| OnGameRestart | none | S_UIManager (Restart button) | S_GameManager, S_SceneCheckpointTracker |
| OnExit | none | S_UIManager (Exit button) | S_GameManager |
| OnStartFreshGameRequested | none | S_StartMenuController | S_GameManager |
| OnReturnToStartMenuRequested | none | S_StartMenuController | S_GameManager |
| OnSceneLoadRequested | string runtimeKey | S_SceneChangeTrigger, S_GameManager | S_GameManager |
| OnGameplayInputEnabledRequested | bool enabled | S_GameManager, S_UIManager | S_Player |
| OnLevelExitRequested | none | S_ExitGate | S_GameManager |
| **Data** | | | |
| OnScoreChanged | int score | (future use) | (future UI) |
| OnSkillUsed | string name | (future use) | (future UI) |
| OnPlayerEnergyChanged | float current, float max | S_PlayerEnergy | S_UIManager |
| reNewSpwnPoint | Transform | S_Checkpoint | S_SceneCheckpointTracker (legacy bridge) |
| OnSpawnPointChanged | Transform | S_Checkpoint | S_SceneCheckpointTracker |
| **Section** | | | |
| OnSectionStart | int index | S_SectionGoal (Start trigger) | S_LevelSectionController |
| OnSectionEnd | int index | S_SectionGoal (End trigger) | S_LevelSectionController |
| OnSectionDescentStarted | int index | S_LevelSectionController | S_AudioManager, S_SectionAlarmEffect |
| OnSectionDescentCompleted | int index | S_LevelSectionController | S_AudioManager, S_SectionAlarmEffect |
| **Audio** | | | |
| OnPlaySFX | AudioClip clip | S_Player, S_PlayerSkillController, any system | S_AudioManager |
| OnPlaySFXPitched | AudioClip, float pitch, float vol | Any system | S_AudioManager |
| OnBGMChange | AudioClip clip | Any system | S_AudioManager |
| OnBgmVolumeChangeRequested | float value | S_GameManager | S_AudioManager |
| OnSfxVolumeChangeRequested | float value | S_GameManager | S_AudioManager |
| **NPC & Story** | | | |
| OnNPCInteract | string npcID | S_NPCEnemy, S_NPCDialogue | (future UI) |
| OnSuspicionChanged | float value | S_SuspicionSystem | S_UIManager, S_NPCCamera |
| OnSuspicionValueChanged | float current, float max | S_SuspicionSystem | S_UIManager |
| OnSuspicionChangeRequested | float amount, Transform source | S_NPCEnemy | S_SuspicionSystem |
| OnHiddenSuspicionDecayRequested | float deltaTime | S_HideSpot | S_SuspicionSystem |
| OnPlayerHiddenChangeRequested | bool hidden | S_HideSpot | S_SuspicionSystem |
| OnPlayerHiddenChanged | bool hidden | S_SuspicionSystem | S_HideSpot |
| OnAlertTriggered | Transform npc | S_SuspicionSystem | S_NPCCamera |
| OnArrestTriggered | none | S_SuspicionSystem | S_GameManager |
| OnSuspicionResetRequested | none | S_GameManager | S_SuspicionSystem |
| OnStoryTrigger | string triggerID | S_NPCDialogue | S_NPCStory |
| **Key & Gate** | | | |
| OnKeyCollected | none | S_Key | S_ExitGate |
| OnKeyCountChanged | int collected, int total | S_Key | S_ExitGate, S_UIManager |

### 2.3 Event Flow Diagrams

#### Player Death Flow
```
Player touches lava or NPC arrest occurs
    -> S_GameEvent.PlayerDied()
    -> S_UIManager.ShowDeathUI()
        -> Time.timeScale = 0
        -> Death panel appears with back to checkpoint
    -> Player presses back to checkpoint
    -> S_GameEvent.GameReStart()
    -> S_SceneCheckpointTracker respawns player
```

#### Section Progression Flow
```
Player enters Section 0 StartTrigger
    闂?S_SectionGoal.OnTriggerEnter2D() (triggerType = Start)
    闂?S_GameEvent.SectionStart(0)
    闂?S_LevelSectionController.HandleSectionStart(0)
        闂?sections[0].RevealSection() (section descends)

Player enters Section 0 EndTrigger
    闂?S_SectionGoal.OnTriggerEnter2D() (triggerType = End)
    闂?S_GameEvent.SectionEnd(0)
    闂?S_LevelSectionController.HandleSectionEnd(0)
        闂?sections[0].HideSection() (section ascends)
        闂?sections[0].MarkCompleted()
        闂?sections[1].RevealSection() (next section descends)
```

---

## 3. Script Details

### 3.1 S_GameEvent.cs

**Type**: Static class (no MonoBehaviour 闂?does NOT need a GameObject in scene)

**Event Declarations** (30+ events):
```csharp
// Game lifecycle events
public static event Action OnPlayerDied;
public static event Action OnGameStart;
public static event Action OnGameRestart;
public static event Action OnExit;
public static event Action OnStartFreshGameRequested;
public static event Action OnReturnToStartMenuRequested;
public static event Action<string> OnSceneLoadRequested;
public static event Action<bool> OnGameplayInputEnabledRequested;
public static event Action OnLevelExitRequested;

// Data events
public static event Action<int> OnScoreChanged;
public static event Action<string> OnSkillUsed;
public static event Action<float, float> OnPlayerEnergyChanged; // v0.8.1
public static event Action<Transform> reNewSpwnPoint;       // legacy bridge
public static event Action<Transform> OnSpawnPointChanged;

// Section events
public static event Action<int> OnSectionStart;
public static event Action<int> OnSectionEnd;
public static event Action<int> OnSectionDescentStarted;
public static event Action<int> OnSectionDescentCompleted;

// Audio events
public static event Action<AudioClip> OnPlaySFX;
public static event Action<AudioClip, float, float> OnPlaySFXPitched;
public static event Action<AudioClip> OnBGMChange;
public static event Action<float> OnBgmVolumeChangeRequested;
public static event Action<float> OnSfxVolumeChangeRequested;

// NPC & Story events
public static event Action<string> OnNPCInteract;
public static event Action<float> OnSuspicionChanged;
public static event Action<float, float> OnSuspicionValueChanged;
public static event Action<float, Transform> OnSuspicionChangeRequested;
public static event Action<float> OnHiddenSuspicionDecayRequested;
public static event Action<bool> OnPlayerHiddenChangeRequested;
public static event Action<bool> OnPlayerHiddenChanged;
public static event Action<Transform> OnAlertTriggered;
public static event Action OnArrestTriggered;
public static event Action OnSuspicionResetRequested;
public static event Action<string> OnStoryTrigger;

// Key & Gate events
public static event Action OnKeyCollected;
public static event Action<int, int> OnKeyCountChanged;
```

**Invocation Methods**:
| Method | Parameters | Invokes | Description |
|--------|------------|---------|-------------|
| `PlayerDied()` | none | `OnPlayerDied` | Player has died (lava, hazard, NPC arrest) |
| `GameStart()` | none | `OnGameStart` | Start button pressed |
| `GameReStart()` | none | `OnGameRestart` | Restart button pressed |
| `ExitGame()` | none | `OnExit` | Exit button pressed |
| `ReNewSpwnPoint(Transform)` | Transform | `reNewSpwnPoint` | Legacy bridge 闂?also calls `SpawnPointChanged` |
| `SpawnPointChanged(Transform)` | Transform | `OnSpawnPointChanged`, `reNewSpwnPoint` | New checkpoint reached (v0.8.0) |
| `ScoreChanged(int)` | int | `OnScoreChanged` | Score updated (future) |
| `SkillUsed(string)` | string | `OnSkillUsed` | Skill activated (future) |
| `PlayerEnergyChanged(float, float)` | current, max | `OnPlayerEnergyChanged` | Shared player energy changed (v0.8.1) |
| `StartFreshGameRequested()` | none | `OnStartFreshGameRequested` | New game from start menu (v0.8.0) |
| `ReturnToStartMenuRequested()` | none | `OnReturnToStartMenuRequested` | Return to start menu (v0.8.0) |
| `SceneLoadRequested(string)` | runtime scene key | `OnSceneLoadRequested` | Request scene load by `S_SceneReference.RuntimeKey` (v0.8.1) |
| `GameplayInputEnabledRequested(bool)` | bool | `OnGameplayInputEnabledRequested` | Toggle gameplay input (v0.8.0) |
| `LevelExitRequested()` | none | `OnLevelExitRequested` | Player exits level (v0.8.0) |
| `SectionStart(int)` | int sectionIndex | `OnSectionStart` | Player entered section Start trigger |
| `SectionEnd(int)` | int sectionIndex | `OnSectionEnd` | Player entered section End trigger |
| `SectionDescentStarted(int)` | int sectionIndex | `OnSectionDescentStarted` | Section platform started descending |
| `SectionDescentCompleted(int)` | int sectionIndex | `OnSectionDescentCompleted` | Section platform finished descending |
| `PlaySFX(AudioClip)` | AudioClip clip | `OnPlaySFX` | Play a one-shot sound effect |
| `PlaySFX(AudioClip, float, float)` | clip, pitch, vol | `OnPlaySFXPitched` | Play SFX with pitch/volume |
| `BGMChange(AudioClip)` | AudioClip clip | `OnBGMChange` | Switch background music clip |
| `BgmVolumeChangeRequested(float)` | float | `OnBgmVolumeChangeRequested` | Change BGM volume (v0.8.0) |
| `SfxVolumeChangeRequested(float)` | float | `OnSfxVolumeChangeRequested` | Change SFX volume (v0.8.0) |
| `NPCInteract(string)` | string npcID | `OnNPCInteract` | Player interacted with NPC |
| `SuspicionChanged(float)` | float value | `OnSuspicionChanged` | Suspicion meter value changed |
| `SuspicionValueChanged(float, float)` | current, max | `OnSuspicionValueChanged` | Suspicion value + max changed (v0.8.0) |
| `SuspicionChangeRequested(float, Transform)` | amount, source | `OnSuspicionChangeRequested` | Request suspicion change (v0.8.0) |
| `HiddenSuspicionDecayRequested(float)` | deltaTime | `OnHiddenSuspicionDecayRequested` | Hidden decay tick (v0.8.0) |
| `PlayerHiddenChangeRequested(bool)` | hidden | `OnPlayerHiddenChangeRequested` | Request hide state change (v0.8.0) |
| `PlayerHiddenChanged(bool)` | hidden | `OnPlayerHiddenChanged` | Hide state changed (v0.8.0) |
| `SuspicionResetRequested()` | none | `OnSuspicionResetRequested` | Reset suspicion on restart (v0.8.0) |
| `AlertTriggered(Transform)` | Transform npc | `OnAlertTriggered` | NPC triggered alert |
| `ArrestTriggered()` | none | `OnArrestTriggered` | Player was arrested |
| `StoryTrigger(string)` | string triggerID | `OnStoryTrigger` | Story event triggered |
| `KeyCollected()` | none | `OnKeyCollected` | Player collected a key |
| `KeyCountChanged(int, int)` | int collected, int total | `OnKeyCountChanged` | Key count updated |

All methods use null-conditional invocation (`?.Invoke()`) for safety 闂?no null reference exceptions if no subscribers.

---

## 4. Usage Guide

### 4.1 Subscribing to an Event (Consumer)

Always subscribe in `OnEnable()` and unsubscribe in `OnDisable()` to prevent memory leaks and null reference errors.

```csharp
public class S_UIManager : MonoBehaviour
{
    void OnEnable()
    {
        S_GameEvent.OnPlayerDied += ShowDeathUI;
        S_GameEvent.OnPlayerEnergyChanged += UpdateEnergyBar;
        S_GameEvent.OnKeyCountChanged += UpdateKeyCount;
    }

    void OnDisable()
    {
        S_GameEvent.OnPlayerDied -= ShowDeathUI;
        S_GameEvent.OnPlayerEnergyChanged -= UpdateEnergyBar;
        S_GameEvent.OnKeyCountChanged -= UpdateKeyCount;
    }

    private void UpdateEnergyBar(float current, float max)
    {
        // Update shared skill energy UI.
    }
}
```

### 4.2 Invoking an Event (Producer)

```csharp
// Any script can invoke events 闂?no reference needed
S_GameEvent.PlayerDied();
S_GameEvent.SectionStart(0);
S_GameEvent.SpawnPointChanged(checkpointTransform);
S_GameEvent.PlayerEnergyChanged(currentEnergy, maxEnergy);
```

### 4.3 Adding a New Event

1. Add the event declaration to `S_GameEvent`:
   ```csharp
   public static event Action<ParamType> OnNewEvent;
   ```

2. Add the invocation method:
   ```csharp
   public static void NewEvent(ParamType param) => OnNewEvent?.Invoke(param);
   ```

3. Subscribe in consumers using `OnEnable()`/`OnDisable()` lifecycle

---

## 5. Event Category Guide

### 5.1 Game Lifecycle Events
These events control the overall game flow (start, death, restart, exit). They are consumed by `S_GameManager`, `S_UIManager`, and `S_SceneCheckpointTracker`.

| When to use | Event to fire |
|-------------|---------------|
| Player dies (lava, hazard) | `S_GameEvent.PlayerDied()` |
| Start button pressed | `S_GameEvent.GameStart()` |
| Restart button pressed | `S_GameEvent.GameReStart()` |
| Exit button pressed | `S_GameEvent.ExitGame()` |

### 5.2 Data Events
These events carry data (score, skill name, transform). They are consumed by UI or manager systems.

| When to use | Event to fire |
|-------------|---------------|
| Score changes | `S_GameEvent.ScoreChanged(newScore)` |
| Skill activated | `S_GameEvent.SkillUsed(skillName)` |
| New checkpoint reached | `S_GameEvent.ReNewSpwnPoint(checkpointTransform)` |

### 5.3 Audio Events
These events manage sound effects and background music. They are consumed by `S_AudioManager`.

| When to use | Event to fire |
|-------------|---------------|
| Player jumps | `S_GameEvent.PlaySFX(jumpClip)` |
| Player switches form | `S_GameEvent.PlaySFX(formSwitchClip)` |
| Any one-shot SFX needed | `S_GameEvent.PlaySFX(clip)` |
| Change background music | `S_GameEvent.BGMChange(newBGMClip)` |

### 5.4 Section Events
These events manage level section progression. They are consumed by `S_LevelSectionController`.

| When to use | Event to fire |
|-------------|---------------|
| Player enters section StartTrigger | `S_GameEvent.SectionStart(sectionIndex)` |
| Player enters section EndTrigger | `S_GameEvent.SectionEnd(sectionIndex)` |

### 5.5 Key & Gate Events
These events manage the key collection and exit gate system. Keys are produced by `S_Key`, consumed by `S_ExitGate` and `S_UIManager`.

| When to use | Event to fire |
|-------------|---------------|
| Player collects a key | `S_GameEvent.KeyCollected()` |
| Key count UI needs update | `S_GameEvent.KeyCountChanged(collected, total)` |

---

## 6. Best Practices

1. **Always unsubscribe in OnDisable()** 闂?prevents memory leaks and stale references
2. **Use specific event types** (`Action<int>`) over generic (`Action<object>`)
3. **Keep events focused on game-wide state changes** 闂?for local communication, use direct method calls
4. **Events are fire-and-forget** 闂?producers don't wait for consumers to finish
5. **Thread safety**: All events must be invoked from the main thread (Unity's Update/FixedUpdate)
6. **Event naming**: Use `On` prefix for events (e.g., `OnPlayerDied`, `OnSectionStart`)

---

## 7. Common Issues

| Issue | Solution |
|-------|----------|
| Event not firing | Check `?.Invoke()` is used and at least one subscriber exists |
| NullReferenceException on event | Ensure all subscriptions have matching unsubscriptions in OnDisable() |
| Event fires but nothing happens | Check the consumer's OnEnable is being called (object must be active) |
| Multiple handlers conflict | Events support multiple subscribers 闂?order is not guaranteed, design accordingly |
| Event fires twice | Check for duplicate subscriptions (OnEnable called multiple times without OnDisable) |
