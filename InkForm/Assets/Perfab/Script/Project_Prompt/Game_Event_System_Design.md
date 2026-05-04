# Game Event System — Design Document

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

### 2.2 Event Inventory

| Event | Parameter | Producer | Consumer |
|-------|-----------|----------|----------|
| OnPlayerDied | none | S_coleve (lava), hazards | S_GameManager, S_UIManager |
| OnGameStart | none | S_UIManager (Start button) | S_GameManager |
| OnGameRestart | none | S_UIManager (Restart button) | S_GameManager |
| OnExit | none | S_UIManager (Exit button) | S_GameManager |
| OnScoreChanged | int score | (future use) | (future UI) |
| OnSkillUsed | string name | (future use) | (future UI) |
| reNewSpwnPoint | Transform | S_Checkpoint | S_GameManager |
| OnSectionStart | int index | S_SectionGoal (Start trigger) | S_LevelSectionController |
| OnSectionEnd | int index | S_SectionGoal (End trigger) | S_LevelSectionController |

### 2.3 Event Flow Diagrams

#### Player Death Flow
```
Player touches lava
    → S_coleve.OnCollisionEnter2D()
    → S_GameEvent.PlayerDied()
    → S_GameManager.PlayerDied()
        → Teleport player to spwnPoint
    → S_UIManager.ShowUI()
        → Menu appears
```

#### Section Progression Flow
```
Player enters Section 0 StartTrigger
    → S_SectionGoal.OnTriggerEnter2D() (triggerType = Start)
    → S_GameEvent.SectionStart(0)
    → S_LevelSectionController.HandleSectionStart(0)
        → sections[0].RevealSection() (section descends)

Player enters Section 0 EndTrigger
    → S_SectionGoal.OnTriggerEnter2D() (triggerType = End)
    → S_GameEvent.SectionEnd(0)
    → S_LevelSectionController.HandleSectionEnd(0)
        → sections[0].HideSection() (section ascends)
        → sections[0].MarkCompleted()
        → sections[1].RevealSection() (next section descends)
```

---

## 3. Script Details

### 3.1 S_GameEvent.cs

**Type**: Static class (no MonoBehaviour — does NOT need a GameObject in scene)

**Event Declarations**:
```csharp
// Game lifecycle events
public static event Action OnPlayerDied;
public static event Action OnGameStart;
public static event Action OnGameRestart;
public static event Action OnExit;

// Data events
public static event Action<int> OnScoreChanged;
public static event Action<string> OnSkillUsed;
public static event Action<Transform> reNewSpwnPoint;

// Section events
public static event Action<int> OnSectionStart;
public static event Action<int> OnSectionEnd;
```

**Invocation Methods**:
| Method | Parameters | Invokes | Description |
|--------|------------|---------|-------------|
| `PlayerDied()` | none | `OnPlayerDied` | Player has died (lava, hazard) |
| `GameStart()` | none | `OnGameStart` | Start button pressed |
| `GameReStart()` | none | `OnGameRestart` | Restart button pressed |
| `ExitGame()` | none | `OnExit` | Exit button pressed |
| `ReNewSpwnPoint(Transform)` | Transform | `reNewSpwnPoint` | New checkpoint reached |
| `ScoreChanged(int)` | int | `OnScoreChanged` | Score updated (future) |
| `SkillUsed(string)` | string | `OnSkillUsed` | Skill activated (future) |
| `SectionStart(int)` | int sectionIndex | `OnSectionStart` | Player entered section Start trigger |
| `SectionEnd(int)` | int sectionIndex | `OnSectionEnd` | Player entered section End trigger |

All methods use null-conditional invocation (`?.Invoke()`) for safety — no null reference exceptions if no subscribers.

---

## 4. Usage Guide

### 4.1 Subscribing to an Event (Consumer)

Always subscribe in `OnEnable()` and unsubscribe in `OnDisable()` to prevent memory leaks and null reference errors.

```csharp
public class S_GameManager : MonoBehaviour
{
    void OnEnable()
    {
        S_GameEvent.OnPlayerDied += PlayerDied;
        S_GameEvent.reNewSpwnPoint += newSpwn;
        S_GameEvent.OnSectionStart += HandleSectionStart;
        S_GameEvent.OnSectionEnd += HandleSectionEnd;
    }

    void OnDisable()
    {
        S_GameEvent.OnPlayerDied -= PlayerDied;
        S_GameEvent.reNewSpwnPoint -= newSpwn;
        S_GameEvent.OnSectionStart -= HandleSectionStart;
        S_GameEvent.OnSectionEnd -= HandleSectionEnd;
    }

    private void PlayerDied()
    {
        // Teleport player to spawn point
        player.transform.position = spwnPoint.position;
    }

    private void HandleSectionStart(int index)
    {
        sections[index].RevealSection();
    }
}
```

### 4.2 Invoking an Event (Producer)

```csharp
// Any script can invoke events — no reference needed
S_GameEvent.PlayerDied();
S_GameEvent.SectionStart(0);
S_GameEvent.ReNewSpwnPoint(checkpointTransform);
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
These events control the overall game flow (start, death, restart, exit). They are consumed by `S_GameManager` and `S_UIManager`.

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

### 5.3 Section Events
These events manage level section progression. They are consumed by `S_LevelSectionController`.

| When to use | Event to fire |
|-------------|---------------|
| Player enters section StartTrigger | `S_GameEvent.SectionStart(sectionIndex)` |
| Player enters section EndTrigger | `S_GameEvent.SectionEnd(sectionIndex)` |

---

## 6. Best Practices

1. **Always unsubscribe in OnDisable()** — prevents memory leaks and stale references
2. **Use specific event types** (`Action<int>`) over generic (`Action<object>`)
3. **Keep events focused on game-wide state changes** — for local communication, use direct method calls
4. **Events are fire-and-forget** — producers don't wait for consumers to finish
5. **Thread safety**: All events must be invoked from the main thread (Unity's Update/FixedUpdate)
6. **Event naming**: Use `On` prefix for events (e.g., `OnPlayerDied`, `OnSectionStart`)

---

## 7. Common Issues

| Issue | Solution |
|-------|----------|
| Event not firing | Check `?.Invoke()` is used and at least one subscriber exists |
| NullReferenceException on event | Ensure all subscriptions have matching unsubscriptions in OnDisable() |
| Event fires but nothing happens | Check the consumer's OnEnable is being called (object must be active) |
| Multiple handlers conflict | Events support multiple subscribers — order is not guaranteed, design accordingly |
| Event fires twice | Check for duplicate subscriptions (OnEnable called multiple times without OnDisable) |