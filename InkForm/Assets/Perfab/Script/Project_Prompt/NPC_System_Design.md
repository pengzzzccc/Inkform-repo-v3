# NPC System Design — InkForm

## Overview
The NPC system provides all non-player characters across InkForm's three chapters. It is built on a common base class (`S_NPCbase`) with four specialized subclasses, plus a standalone `S_SuspicionSystem` manager for Chapter 2's core mechanic.

## File Structure
```
Assets/Perfab/Script/Npcs/
  ├── S_NPCbase.cs          — Base class (identity, interaction, lifecycle)
  ├── S_NPCEnemy.cs         — Guard NPC (patrol + chase + arrest state machine)
  ├── S_NPCDialogue.cs      — Dialogue NPC (linear and branching modes)
  ├── S_NPCStory.cs         — K-01 labourer NPC (fixed patrol, mimic target)
  └── S_NPCCamera.cs       — Surveillance drone (detection cone + alerts)

Assets/Perfab/Script/Manager/
  └── S_SuspicionSystem.cs  — Suspicion meter (Ch2 core mechanic)
```

## Class Hierarchy
```
MonoBehaviour
  └── S_NPCbase
        ├── S_NPCEnemy   (guard patrol/chase/arrest)
        ├── S_NPCDialogue (Ruth, Arthur, story NPCs)
        ├── S_NPCStory   (K-01 workers, fixed routes)
        └── S_NPCCamera  (surveillance drones)
```

## S_NPCbase — Common Functionality

### Inspector Fields
| Group | Field | Type | Description |
|-------|-------|------|-------------|
| Identity | `npcName` | string | Display name |
| Identity | `npcID` | string | Unique identifier |
| Interaction | `canInteract` | bool | Player can interact |
| Interaction | `interactRange` | float | Detection distance |
| Dialogue (Optional) | `dialogueAsset` | TextAsset | Dialogue data |

### Core References
Cached in `Awake()`:
- `SpriteRenderer npcSprite`
- `Rigidbody2D npcRig`
- `Collider2D npcCol`

### Lifecycle
- `OnEnable()` subscribes to `OnGameStart`, `OnGameRestart`
- `OnDisable()` unsubscribes
- Subclasses MUST call `base.OnEnable()` / `base.OnDisable()`

### Public API
| Method | Description |
|--------|-------------|
| `OnInteract()` | Called when player interacts; fires `NPCInteract(npcID)` |
| `SetActive(bool)` | Toggle NPC visibility and collision |
| `FlipSprite(dirX)` | Flip sprite to face movement direction |

## S_NPCEnemy — Guard NPC

### State Machine
```
Idle → Patrol → Chase → Arrest → Disabled
         ↑        ↓
         └────────┘ (lose player)
```

### States
| State | Behaviour |
|-------|-----------|
| **Idle** | Stand still (TODO) |
| **Patrol** | Follow waypoint sequence at `patrolSpeed` |
| **Chase** | Pursue player at `chaseSpeed` when within `chaseRange`, stop at `loseRange` |
| **Arrest** | Rush to player at `arrestSpeed`; triggered by `OnArrestTriggered` |
| **Disabled** | Inactive |

### Suspicion Integration
- Listens to `OnSuspicionChanged(value)` to adjust patrol speed and aggression
- Transitions to Arrest state on `OnArrestTriggered()`

### Inspector Fields
| Group | Field | Type | Default |
|-------|-------|------|---------|
| Patrol | `waypoints` | Transform[] | — |
| Patrol | `patrolSpeed` | float | 3 |
| Patrol | `waypointWaitTime` | float | 1 |
| Chase | `chaseSpeed` | float | 6 |
| Chase | `chaseRange` | float | 8 |
| Chase | `loseRange` | float | 12 |
| Arrest | `arrestSpeed` | float | 9 |

## S_NPCDialogue — Dialogue NPC

### Modes
| Mode | Use Case |
|------|----------|
| **Linear** | Ruth (Ch1 Branch1), simple NPCs |
| **Branching** | Arthur (Ch3) — dialogue changes based on collected documents and branch history |

### Dialogue Flow
1. `OnInteract()` → `StartDialogue()` → fires `StoryTrigger("Dialogue_Start_{npcID}")`
2. Player advances → `AdvanceDialogue()` → next line
3. Last line → `EndDialogue()` → fires `StoryTrigger("Dialogue_End_{npcID}"`

### Inspector Fields
| Group | Field | Type | Description |
|-------|-------|------|-------------|
| Dialogue Mode | `mode` | enum | Linear or Branching |
| Linear | `linearLines` | string[] | Dialogue array |
| Branching | `branchA_Lines` | string[] | Dialogue for branch A |
| Branching | `branchB_Lines` | string[] | Dialogue for branch B |
| Dialogue UI | `textSpeed` | float | Characters per second |

### Future: TextAsset Parser
`dialogueAsset` field is inherited from `S_NPCbase`. When implemented, the parser will load dialogue from structured text files instead of inspector arrays.

## S_NPCStory — K-01 Labourer NPC

### Purpose
Fixed-route NPCs with no chase behaviour. Used for:
- **Chapter 1 Branch 2**: K-01 workers on the factory floor (player mimics their movement)
- **Chapter 2**: Nurserie K-01 workers (suspicion increases when player separates from groups)

### Movement
- Follows `waypoints[]` sequence at `moveSpeed`
- Waits `waypointWaitTime` at each waypoint
- Loops if `loopRoute = true`

### Mimicry System (Future)
- `isMimicTarget`: Flag for K-01 units the player can mimic
- `mimicDetectionRange`: Range at which player must match movement patterns

## S_NPCCamera — Surveillance Drone

### Detection System
- Cone-shaped detection zone (range + angle)
- Cooldown between detections prevents spam
- On detection: fires `SuspicionChanged(+30)` and `AlertTriggered(transform)`

### Visual Feedback (TODO)
- `idleLight` (green) → normal state
- `alertLight` (red) → during detection cooldown

### Inspector Fields
| Group | Field | Type | Default |
|-------|-------|------|---------|
| Patrol | `waypoints` | Transform[] | — |
| Patrol | `patrolSpeed` | float | 2 |
| Patrol | `waypointWaitTime` | float | 1 |
| Detection | `detectionRange` | float | 8 |
| Detection | `detectionAngle` | float | 60° |
| Detection | `detectionCooldown` | float | 2 |
| Detection | `suspicionOnDetect` | int | 30 |
| Visual | `idleLight` | Color | Green |
| Visual | `alertLight` | Color | Red |

## S_SuspicionSystem — Suspicion Meter

### Location
`Assets/Perfab/Script/Manager/S_SuspicionSystem.cs`

### Thresholds (from Story_Outline.md)
| Range | Level | Guard Behaviour |
|-------|-------|-----------------|
| 0–33 | Normal | Standard patrols |
| 34–66 | Elevated | Additional patrols, faster guards |
| 67–99 | Critical | Active search, alarm pre-warning |
| 100 | **Arrest** | EMP storm deployed |

### Suspicion Sources (from Story_Outline.md)
| Trigger | Suspicion |
|---------|-----------|
| Separated from K-01 groups (per second) | +10 |
| Caught on camera in restricted zone | +30 (one-time) |
| Violating worker protocol | +20 per incident |
| Entering archive vault | +50 |
| Entering observation deck | +40 |
| Accessing communications tower | +60 |

### Public API
| Method | Description |
|--------|-------------|
| `AddSuspicion(amount)` | Modify meter; clamps 0–100; fires `OnSuspicionChanged` |
| `CompleteMission()` | Increment completed missions; triggers arrest at 3/3 |
| `SafeZoneDecay()` | Call from safe zone triggers for timed suspicion decay |
| `CurrentSuspicion` | Read current value |
| `SuspicionPercent` | 0.0–1.0 normalized |

### Arrest Triggers
1. Suspicion reaches 100/100
2. All 3 story missions completed (canon path)

## GameEvent Integration

### New Events (added to S_GameEvent.cs)
| Event | Signature | Fired By |
|-------|-----------|----------|
| `OnNPCInteract` | `Action<string>` (npcID) | `S_NPCbase.OnInteract()` |
| `OnSuspicionChanged` | `Action<float>` (0–100) | `S_SuspicionSystem.AddSuspicion()` |
| `OnAlertTriggered` | `Action<Transform>` (npc) | `S_NPCCamera.OnPlayerDetected()` |
| `OnArrestTriggered` | `Action` | `S_SuspicionSystem.TriggerArrest()` |
| `OnStoryTrigger` | `Action<string>` (triggerID) | `S_NPCDialogue`, level triggers |

### Event Subscribers
| Event | Subscribers |
|-------|-------------|
| `OnGameStart` | All NPC subclasses (via base) |
| `OnGameRestart` | All NPC subclasses (via base), `S_SuspicionSystem` |
| `OnSuspicionChanged` | `S_NPCEnemy` (adjust behaviour) |
| `OnArrestTriggered` | `S_NPCEnemy` (enter Arrest state) |
| `OnStoryTrigger` | `S_NPCDialogue` (auto-start dialogue) |

## Story Mapping

| Chapter | NPC Type | File | Notes |
|---------|----------|------|-------|
| Ch1 Escape | `S_NPCEnemy` | Guard patrols | Avoid or break through with solid form |
| Ch1 Escape | `S_NPCCamera` | Drones | Trigger alerts |
| Ch1 Branch1 | `S_NPCDialogue` | Ruth | Linear dialogue, hidden room |
| Ch1 Branch2 | `S_NPCStory` | K-01 workers | Mimicry sequence |
| Ch1 Branch2 | `S_NPCDialogue` | Terminal | K-01 cluster manager terminal |
| Ch2 Nurserie | `S_NPCEnemy` | Guards | Behaviour changes with suspicion |
| Ch2 Nurserie | `S_NPCStory` | K-01 workers | Group separation triggers suspicion |
| Ch2 Nurserie | `S_NPCCamera` | Drones | Restricted zone detection |
| Ch3 Control | `S_NPCDialogue` | Arthur | Branching dialogue |
| Ch3 Control | `S_NPCDialogue` | Ruth | Silent presence during final choice |

## Implementation Status
- [x] `S_NPCbase` — complete base class
- [x] `S_NPCEnemy` — state machine skeleton
- [x] `S_NPCDialogue` — dialogue flow skeleton
- [x] `S_NPCStory` — K-01 patrol complete
- [x] `S_NPCCamera` — detection + patrol complete
- [x] `S_SuspicionSystem` — meter + arrest logic complete
- [ ] `S_NPCEnemy` — full patrol/chase/arrest movement logic
- [ ] `S_NPCDialogue` — dialogue UI integration
- [ ] `S_NPCStory` — mimicry system
- [ ] `S_NPCCamera` — light colour visual feedback
- [ ] `S_DialogueUI` — dialogue UI manager (separate system)
- [ ] `S_InventorySystem` — document/key item collection (separate system)

## Dependencies
- `S_Player` — Player singleton (distance checks)
- `S_GameEvent` — Event bus (all NPC events)
- `S_GameManager` — Game restart handling
- `S_UIManager` — Future: dialogue UI display