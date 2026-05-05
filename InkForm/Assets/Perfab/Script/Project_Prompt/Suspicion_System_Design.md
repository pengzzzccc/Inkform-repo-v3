# Suspicion System Design

## Overview
The Suspicion System tracks a 0-100 suspicion meter throughout Chapter 2 (The Nurserie). Guards, cameras, and story triggers add suspicion. When suspicion reaches 100 or all 3 story missions are complete, the Arrest Event fires (EMP storm). The player can reduce suspicion by hiding (via `S_HideSpot`) or staying in safe zones.

## Architecture

### Singleton + Static Bridge Pattern
```
S_SuspicionSystem (Singleton MonoBehaviour)
├── static PlayerHidden          ← bridge: S_HideSpot → S_NPCEnemy
├── currentSuspicion (float)
├── completedMissions (int)
└── Event out: SuspicionChanged / ArrestTriggered
```

### PlayerHidden Static Field
The `PlayerHidden` static property bridges `S_HideSpot` (which sets it) and `S_NPCEnemy` (which reads it). This allows guards to lose their target when the player hides, without requiring a direct reference between the two systems.

```
S_HideSpot.EnterHide()
  └→ S_SuspicionSystem.PlayerHidden = true
  
S_NPCEnemy.Update()
  └→ if (PlayerHidden) → return to Patrol
```

### Suspicion Sources
| Source | Amount | Trigger |
|--------|--------|---------|
| Guard alert | `increasePerAlert` (default 20) | S_GameEvent.AlertTriggered |
| Level trigger | Variable | S_SuspicionSystem.AddSuspicion(value) |
| Camera drone detect | Variable | AddSuspicion(value) |
| Story action | Variable | AddSuspicion(value) |

### Suspicion Decay
| Type | Rate | Condition |
|------|------|-----------|
| Passive decay | `decayRate` per second | Only if `decayOnlyInSafeZone = false` |
| Safe zone decay | `decayRate` per second | Player in safe zone trigger area |
| Hidden decay | `hiddenDecayRate` per second | Player hiding in S_HideSpot |

### Arrest Triggers
1. **suspicion_at_max** — `currentSuspicion >= maxSuspicion` (100)
2. **all_missions_complete** — `completedMissions >= missionsToTriggerArrest` (3)
3. Once triggered, `arrestTriggered = true` locks out all further suspicion changes

## Suspicion Thresholds
| Range | State | Effect |
|-------|-------|--------|
| 0–33 | Normal | Guards follow standard patrols |
| 34–66 | Elevated | Additional patrol spawns, faster guards |
| 67–99 | Critical | Guards actively search, alarm pre-warning |
| 100 | Arrest | EMP storm event fires |

(Threshold effects are handled by S_GameManager / level scripts reading `SuspicionPercent`.)

## Inspector Configuration

| Group | Field | Type | Description |
|-------|-------|------|-------------|
| Suspicion Settings | maxSuspicion | float | Maximum suspicion value (default 100) |
| | decayRate | float | Passive decay per second (default 0 = off) |
| | decayOnlyInSafeZone | bool | If true, passive decay only works in safe zones |
| | increasePerAlert | float | Suspicion added per guard alert (default 20) |
| | hiddenDecayRate | float | Decay per second while hidden (default 5) |
| Mission Completion | missionsToTriggerArrest | int | Missions needed to force arrest (default 3) |

## Usage

### Setup in Scene
1. Create a GameObject named "SuspicionSystem"
2. Attach `S_SuspicionSystem` component
3. Configure suspicion settings in Inspector
4. Place **exactly one** instance per scene

### API
```csharp
// Add/subtract suspicion (decay uses negative amounts)
S_SuspicionSystem.Instance.AddSuspicion(10f);

// Set exact value
S_SuspicionSystem.Instance.SetSuspicion(50f);

// Complete a story mission
S_SuspicionSystem.Instance.CompleteMission();

// Decay while in safe zone (call from trigger)
S_SuspicionSystem.Instance.SafeZoneDecay();

// Decay while hidden (called automatically by S_HideSpot)
S_SuspicionSystem.Instance.HideDecay();

// Read current values
float suspicion = S_SuspicionSystem.Instance.CurrentSuspicion;
float percent = S_SuspicionSystem.Instance.SuspicionPercent; // 0-1
```

### Events Published
- `S_GameEvent.SuspicionChanged(float currentValue)` — every value change
- `S_GameEvent.ArrestTriggered()` — when arrest fires

## Game Restart Safety
The `HandleGameRestart()` method resets ALL state:
- `currentSuspicion = 0`
- `completedMissions = 0`
- `arrestTriggered = false`
- **`PlayerHidden = false`** ← critical: static field must be reset!

Failure to reset `PlayerHidden` causes guards to ignore the player on subsequent runs because the static field retains its value across scene reloads.

## S_HideSpot Integration
`S_HideSpot` is a trigger component placed on cabinets, pillars, or machines:
- **EnterHide()**: Sets `S_SuspicionSystem.PlayerHidden = true`, hides sprite/collider, zeroes gravity
- **ExitHide()**: Sets `S_SuspicionSystem.PlayerHidden = false`, restores visibility, applies `exitOffset`
- Calls `S_SuspicionSystem.Instance.HideDecay()` each frame while hiding

Player presses `E` within trigger to toggle hide.

## Common Errors

### 1. PlayerHidden not reset on game restart
- **Symptom**: After restarting, guards ignore player (stuck in Patrol); player can't be detected
- **Root Cause**: `PlayerHidden` is a **static field** — it survives scene reload. If the player was hiding when restart occurred, `PlayerHidden = true` persists
- **Fix**: `HandleGameRestart()` includes `PlayerHidden = false`

### 2. Suspicion meter not updating
- **Symptom**: `SuspicionChanged` event fires but UI doesn't update
- **Cause**: UI not subscribed to `S_GameEvent.OnSuspicionChanged`
- **Fix**: S_UIManager's `OnEnable` must subscribe to the event

### 3. Arrest triggers twice
- **Symptom**: `ArrestTriggered()` called, then suspicion changes trigger it again
- **Cause**: Missing `arrestTriggered` guard in `AddSuspicion()` method
- **Fix**: All mutation methods check `if (arrestTriggered) return;` at top