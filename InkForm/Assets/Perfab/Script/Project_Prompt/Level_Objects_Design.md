# Level Objects — Design Document

## 1. Overview

Level objects are interactive elements placed in gameplay scenes. They include breakable blocks, checkpoints, pipelines, moveable blocks, and their trigger helpers. Each object is designed as an independent Prefab that can be placed anywhere in any level.

---

## 2. Object Inventory

| Object | Script | Description |
|--------|--------|-------------|
| Breakable Block | S_BreakableBlock | Destructible block (broken by sprinting solid-form player) |
| Checkpoint | S_Checkpoint | Saves respawn position on player contact |
| Pipeline | S_Pipline | Teleports fluid-form player to output position |
| Moveable Block | S_MoveBlock | Slides between two points on trigger |
| Trigger Helper | S_setTrigger | Detects block-tagged objects for S_MoveBlock |
| Collision Events | S_coleve | Ground detection + lava hazard detection |
| JumpPad | S_JumpPad | Launches player upward with configurable force + color feedback |
| Button Door | S_ButtonDoor | Trigger button that activates a linked S_Door |
| Door | S_Door | Opens/closes with animation when activated |
| Hide Spot | S_HideSpot | Allows player to hide (locks movement, triggers suspicion system) |
| Section Goal | S_SectionGoal | Trigger zone that signals section completion |
| Platform Cable | S_PlatformCableVisual | Visual dual-cable rendering for moving platforms |

---

## 3. Script Details

### 3.1 S_BreakableBlock.cs

**Type**: MonoBehaviour (attach to block prefab)

**Behavior**: When the player collides with a breakable block while in **solid form**, the block is destroyed instantly. The block survives fluid-form player contact — only the solid form can break it.

**Collision Flow**:
```
OnCollisionEnter2D(collision)
    |-- CompareTag("Player") ?
    |   |-- YES: Check S_Player.Instance.getForm()
    |   |   |-- false (solid form) -> Destroy(gameObject)
    |   |   `-- true (fluid form)  -> do nothing (pass through visually)
    |   `-- NO: do nothing
    `-- (other objects): do nothing
```

**Setup Instructions**:
1. Create a sprite GameObject for the block visual
2. Add `BoxCollider2D` — **Is Trigger must be OFF** (this uses OnCollisionEnter2D, not trigger)
3. Add `Rigidbody2D` — set **Body Type** = `Kinematic` (so it doesn't fall with gravity)
4. Add `S_BreakableBlock` component
5. Tag the player GameObject as `"Player"` (the script uses `CompareTag` for zero-allocation checks)

**Important Notes**:
- The block requires a non-trigger Collider2D because it uses `OnCollisionEnter2D`
- The Rigidbody2D must be Kinematic — if set to Dynamic, the block will fall due to gravity
- If blocks are placed inside a Section, they will move with the section root automatically

---

### 3.2 S_Checkpoint.cs

**Type**: MonoBehaviour (attach to checkpoint prefab)

**Behavior**: When the player enters the checkpoint's trigger area, it fires the `reNewSpwnPoint` event to update the global spawn position in `S_GameManager`. The player will respawn at the most recently activated checkpoint after death.

**Event Flow**:
```
OnTriggerEnter2D(collision)
    |-- CompareTag("Player") ?
    |   |-- YES: S_GameEvent.ReNewSpwnPoint(transform)
    |   |   `-- S_GameManager receives event -> updates spwnPoint
    |   `-- NO: do nothing
```

**Setup Instructions**:
1. Create a sprite GameObject for the checkpoint visual (e.g., a flag or marker)
2. Add `BoxCollider2D` — set **Is Trigger** = `true`
3. Add `S_Checkpoint` component
4. Position the checkpoint at the desired respawn location in the level

**Important Notes**:
- The trigger Collider2D must have `isTrigger = true` — otherwise `OnTriggerEnter2D` won't fire
- Checkpoints are one-way: once the player passes one, the spawn point is permanently updated until the next checkpoint
- Multiple checkpoints in a level are supported — the last one reached is the respawn point

---

### 3.3 S_Pipline.cs

**Type**: MonoBehaviour (attach to pipeline entrance)

**Behavior**: When the player in **fluid form** enters the pipeline entrance trigger, they are instantly teleported to the Output position. Solid-form players are not affected.

**Teleport Flow**:
```
OnTriggerEnter2D(collision)
    |-- CompareTag("Player") ?
    |   |-- YES: Check S_Player.Instance.getForm()
    |   |   |-- true (fluid form):
    |   |   |   |-- Output != null ?
    |   |   |   |   |-- YES: player.Teleport(Output.position)
    |   |   |   |   `-- NO: Debug.LogWarning("Pipeline Output not assigned!")
    |   |   |   `-- Reset velocity to zero
    |   |   `-- false (solid form): do nothing
    |   `-- NO: do nothing
```

**Setup Instructions**:
1. Create the **entrance** GameObject with a sprite and trigger Collider2D
2. Create the **exit** GameObject with a sprite (positioned at the teleport destination)
3. Add `S_Pipline` component to the **entrance** GameObject
4. In Inspector, drag the **exit** GameObject to the `Output` field
5. Both entrance and exit can have visual indicators (pipes, portals, etc.)

**Pipeline Pair Diagram**:
```
   Entrance                    Exit
   ┌─────┐                    ┌─────┐
   │  ○  │ ──teleport──>      │  ○  │
   │PIPE │                    │PIPE │
   └─────┘                    └─────┘
   [S_Pipline]                [position target]
   [trigger Collider2D]       [no collider needed]
```

**Important Notes**:
- Only fluid-form players can use pipelines (the `getForm()` check returns `true` for fluid)
- The teleport calls `S_Player.Teleport()` which also resets velocity to zero
- The exit point doesn't need a Collider2D — it's just a position marker
- Null-safe: the script checks if Output is assigned before teleporting

---

### 3.4 S_MoveBlock.cs

**Type**: MonoBehaviour (attach to moveable platform)

**Behavior**: A block that oscillates between two positions (side_1 and side_2) when triggered by an S_setTrigger. Uses exponential decay Lerp for frame-rate independent smooth movement.

**Movement Flow**:
```
Update()
    |-- isTriggered ?
    |   |-- true:  Move block toward side_2.position
    |   `-- false: Move block toward side_1.position
    |
    Movement: Lerp with exponential decay (1 - exp(-speed * dt))
    -> Frame-rate independent, converges to target smoothly
```

**Serialized Fields**:
| Field | Type | Description |
|-------|------|-------------|
| side_1 | Transform | Default/return position marker |
| side_2 | Transform | Triggered/target position marker |
| block | Transform | The block that moves (can be a child object) |
| trigger | GameObject | The trigger child object (for reference) |
| MoveSpeed | float | Interpolation speed (default 10) |

**Setup Instructions**:
1. Create the block GameObject with a sprite, Collider2D, and Rigidbody2D (Kinematic)
2. Create two empty GameObjects as position markers: `side_1` (default position) and `side_2` (target position)
3. Create a child GameObject under the block for the trigger area
4. Add `BoxCollider2D` or `CircleCollider2D` to the trigger child — set **Is Trigger** = `true`
5. Add `S_MoveBlock` component to the **block** GameObject
6. Add `S_setTrigger` component to the **trigger child** GameObject
7. Assign all references in Inspector (side_1, side_2, block, trigger)

**Hierarchy Example**:
```
MoveBlock_Parent
├── Block_Body          (S_MoveBlock, Collider2D, Rigidbody2D Kinematic)
│   ├── Block_Sprite   (SpriteRenderer)
│   └── Trigger_Zone   (S_setTrigger, Collider2D isTrigger)
├── Side_1_Marker      (empty, position marker)
└── Side_2_Marker      (empty, position marker)
```

**Important Notes**:
- The block uses exponential decay Lerp — it doesn't snap to the target, it approaches smoothly
- The trigger child must be tagged as `"block"` if using default S_setTrigger setup (it detects objects with "block" tag)
- S_MoveBlock reads `SetTriggered(bool)` externally — it does NOT detect triggers directly

---

### 3.5 S_setTrigger.cs

**Type**: MonoBehaviour (child trigger of S_MoveBlock)

**Behavior**: Detects objects tagged `"block"` entering/exiting its trigger area and calls `SetTriggered(true/false)` on the parent `S_MoveBlock`.

**Trigger Flow**:
```
OnTriggerEnter2D(collision)
    |-- CompareTag("block") ?
    |   |-- YES: GetComponentInParent<S_MoveBlock>().SetTriggered(true)
    |   `-- NO: do nothing

OnTriggerExit2D(collision)
    |-- CompareTag("block") ?
    |   |-- YES: GetComponentInParent<S_MoveBlock>().SetTriggered(false)
    |   |-- NO: do nothing
```

**Setup Instructions**:
1. Create a child GameObject under the MoveBlock parent
2. Add a trigger Collider2D (`CircleCollider2D` recommended for detection area)
3. Set **Is Trigger** = `true`
4. Add `S_setTrigger` component
5. The script auto-finds the parent `S_MoveBlock` via `GetComponentInParent`

**Important Notes**:
- The object entering the trigger must be tagged `"block"`
- `GetComponentInParent` means the S_setTrigger must be a child of S_MoveBlock in the hierarchy
- The trigger area size determines how close the block needs to be to activate

---

### 3.6 S_coleve.cs

**Type**: MonoBehaviour (Singleton, attach to player body)

**Behavior**: Handles ground detection and lava hazard detection via collision events. Attached to the player's body child object (which has the Collider2D and Rigidbody2D).

**Collision Flow**:
```
OnCollisionEnter2D(collision)
    |-- Contact normal check (normal.y > 0.5 from any contact) -> playerOnGround = true
    |-- CompareTag("lava") ?
    |   |-- YES: S_GameEvent.PlayerDied()
    |   `-- NO: do nothing

OnCollisionExit2D(collision)
    |-- Exiting Ground layer object -> playerOnGround = false
```

**Public API**:
| Method | Returns | Description |
|--------|---------|-------------|
| `getPlayerOnGround()` | bool | Whether the player is currently on ground |

**Setup Instructions**:
1. Attach `S_coleve` to the **player body** child GameObject (same one with Rigidbody2D and Collider2D)
2. Ensure the player is tagged `"Player"`
3. Ground objects should be on the `"Ground"` layer
4. Lava objects should be tagged `"lava"`

**Layer/Tag Configuration**:
| Object | Layer | Tag |
|--------|-------|-----|
| Player body | Default | `Player` |
| Ground platforms | `Ground` | (any) |
| Lava hazards | Default | `lava` |
| Breakable blocks | Default | (any) |

**Important Notes**:
- This is a Singleton — `S_coleve.Instance` provides global access to `getPlayerOnGround()`
- Ground detection uses collision contact normals (normal.y > 0.5 means surface is below player)
- Lava detection fires `OnPlayerDied` which triggers respawn via `S_GameManager`

---

## 4. Prefab Setup Guide

### Breakable Block
1. Create sprite GameObject
2. Add `BoxCollider2D` (Is Trigger = **OFF**)
3. Add `Rigidbody2D` (Body Type = **Kinematic**)
4. Add `S_BreakableBlock` component
5. Tag the player as `"Player"`

### Checkpoint
1. Create sprite GameObject
2. Add `BoxCollider2D` (Is Trigger = **ON**)
3. Add `S_Checkpoint` component
4. Position at desired respawn location

### Pipeline
1. Create entrance GameObject with sprite + trigger Collider2D (Is Trigger = **ON**)
2. Create exit GameObject with sprite (position only, no collider needed)
3. Add `S_Pipline` to entrance, set `Output` to exit transform

### Moveable Block
1. Create block GameObject with sprite + Collider2D + Rigidbody2D (Kinematic)
2. Create `Side_1_Marker` and `Side_2_Marker` empty GameObjects for positions
3. Create trigger child with `CircleCollider2D` (Is Trigger = **ON**)
4. Add `S_MoveBlock` to block, assign all references
5. Add `S_setTrigger` to trigger child (auto-finds parent)

### Collision Events (Player Body)
1. Attach `S_coleve` to player body child (same object with Rigidbody2D + Collider2D)
2. Set Ground objects to `"Ground"` layer
3. Set Lava objects to tag `"lava"`

---

## 5. Common Issues

| Issue | Solution |
|-------|----------|
| Breakable block not breaking | Check player is in solid form (`getForm() == false`) AND Collider2D is NOT a trigger |
| Block falls when game starts | Ensure Rigidbody2D Body Type = Kinematic |
| Checkpoint not saving | Ensure trigger Collider2D has `isTrigger = true` |
| Pipeline not teleporting | Check Output is assigned AND player is in fluid form |
| Pipeline teleports but player keeps falling | Check that `Teleport()` resets velocity (built-in) |
| Move block not responding | Verify S_setTrigger is on the trigger child AND child of S_MoveBlock |
| Move block activates without trigger | Check trigger child Collider2D size — reduce if too large |
| Player death not triggering on lava | Check S_coleve is on player body AND lava is tagged `"lava"` |
| Ground detection unreliable | Ensure ground objects are on `"Ground"` layer and check contact normal threshold |

---

## 6. JumpPad Update

`S_JumpPad` now exposes `jumpForce` with an Inspector range from `10` to `1000`.

The JumpPad visual color is driven by that force:

- Low force: green
- High force: red
- Intermediate values: linear gradient between green and red

The color updates in edit mode via `OnValidate()` and at runtime via `Awake()`. The script auto-finds a `SpriteRenderer` on the JumpPad object, so prefab setup does not require an extra serialized renderer reference.

---

## 7. Button Door System

### 7.1 S_ButtonDoor.cs

**Type**: MonoBehaviour (attach to trigger button)

**Behavior**: When the player enters its trigger collider, activates the linked `S_Door`. One-shot activation (only triggers once).

**Serialized Fields**:
| Field | Type | Description |
|-------|------|-------------|
| doorSystem | S_Door | Reference to the door to activate (auto-finds in parent if null) |

**Setup**: Add `BoxCollider2D` with `isTrigger = true`. Assign `doorSystem` or place as child of door.

### 7.2 S_Door.cs

**Type**: MonoBehaviour (attach to door object)

**Behavior**: Opens/closes a door (moves child transform between two positions). Triggered by `S_ButtonDoor` or external calls.

---

## 8. Hide Spot

`S_HideSpot` allows the player to hide in a designated area. When hiding:
- Player movement is locked via `S_Player.SetMovementLocked(true)`
- Suspicion system is notified
- Player can exit by re-pressing input

**Setup**: Add trigger Collider2D (`isTrigger = true`). Assign to appropriate suspicion zone.

---

## 9. Section Goal

`S_SectionGoal` is a trigger zone that signals section completion when the player enters. Fires `S_GameEvent.SectionEnd(sectionIndex)`.

---

## 10. Platform Cable Visual

`S_PlatformCableVisual` renders two visual cables connecting a top anchor point to a moving platform, creating a winch/cable aesthetic.

### 10.1 Behavior

- Two cables rendered via child `LineRenderer` objects (`CableLeft`, `CableRight`)
- Top Y position synced from `topAnchor` (height only, X stays at platform center)
- Bottom position synced from `platformAttachPoint` (platform position)
- Cable length dynamically adjusts as platform moves up/down
- `[DefaultExecutionOrder(100)]` ensures cables update after platform movement

### 10.2 Serialized Fields

| Field | Default | Description |
|-------|---------|-------------|
| topAnchor | - | Transform for cable top anchor (Y position only) |
| platformAttachPoint | - | Transform for cable bottom (usually the platform) |
| cacheTopAnchorOnStart | false | Whether to cache top Y at Start (set false if top moves) |
| cableOffset | 0.3f | Horizontal distance from platform center to each cable |
| cableWidth | 0.05f | LineRenderer width for cables |
| cableMaterial | - | Material override (falls back to Sprites/Default) |
| sortingLayerName | "" | LineRenderer sorting layer |
| sortingOrder | 4 | LineRenderer sorting order |
| cableColor | (0.55, 0.58, 0.6, 1) | Cable color |
| drawGizmos | true | Show gizmo preview in Scene view |

### 10.3 Setup

1. Attach to a moving platform GameObject (child or sibling)
2. Create an empty `topAnchor` GameObject positioned where cables originate (e.g., ceiling)
3. Set `platformAttachPoint` to the platform's Transform
4. Adjust `cableOffset` to control cable spacing
5. Two child objects (`CableLeft`, `CableRight`) are auto-created with LineRenderers
