# NPC System Design

## Overview
The NPC system provides guard patrol/chase/attack behavior for Chapter 2 (The Nurserie). Guards detect the player, transition through a 5-state state machine, fire EM projectiles to paralyze the player, and arrest on contact. Guards lose their target when the player hides (via `S_SuspicionSystem.PlayerHidden`).

Movement supports two modes. NPCs with Rigidbody2D keep the physics-based `Rigidbody2D.linearVelocity` path. Performance-sensitive NPCs can omit Rigidbody2D and use lightweight Transform movement while keeping Collider2D for sprint detection, projectile hits, and range checks.

## Architecture

### Class Hierarchy
```
MonoBehaviour
 └── S_NPCbase          (base: identity, interaction, sprite/Rigidbody2D refs, Dynamic body setup)
      └── S_NPCEnemy    (guard: full state machine, projectile attack, ground check, idle wandering)
```

### State Machine (S_NPCEnemy)
```
          ┌────────────────────────────────────────────┐
          │                                            ▼
Patrol ──► Chase ──► Attack ──► Arrest
  ▲          │          │
  │          ▼          ▼
  └──── Disabled    Stunned ──► Patrol
```
- **Patrol**: Walk between `waypoints[]`, wait `waypointWaitTime` at each.
  - If no waypoints assigned: idle wandering within `wanderRadius` from spawn position.
- **Chase**: Move toward player at `chaseSpeed`, activated when player enters `chaseRange`
- **Attack**: Fire `projectilePrefab` at `fireRate` when player within `attackRange`
- **Arrest**: Trigger arrest when player within `arrestRange`; on arrest, calls `EnterState(State.Disabled)` + fires `S_GameEvent.PlayerDied()` to show death UI
- **Stunned**: S_Soild_sprint sets this; duration `stunDuration`, then returns to Patrol
- **Disabled**: All behavior suspended; guards do not see/react to player

> **Critical Rule**: All state transitions MUST go through `EnterState(State.XXX)` — never set `currentState` directly. `EnterState()` handles sprite color update, timer reset, and state-specific initialization. Setting `currentState` directly bypasses all side effects and causes bugs (e.g. NPC stuck red after arrest).

### Player Reference Cache
`S_NPCEnemy` stores `playerTransform` as a cached reference—NOT serialized, NOT assigned in Inspector. It is resolved at runtime via `ValidatePlayerReference()`:
```csharp
private void ValidatePlayerReference()
{
    if (playerTransform != null) return;
    if (S_Player.Instance != null)
        playerTransform = S_Player.Instance.GetBodyTransform();
}
```
This is called at the top of `Update()` every frame to handle scene reload safely.

### GameObject Root vs Body Transform
The player GameObject has a **root Transform** (the parent object, which remains at `y=0` or fixed position) and a **body Transform** (the child with Rigidbody2D, which actually moves). All NPC distance checks, chase targets, and detection queries MUST use `S_Player.Instance.GetBodyTransform()` — never `S_Player.Instance.transform` or `GameObject.Find("Player").transform`.

## Physics Movement

### Body Configuration (S_NPCbase.Awake)
Current implementation:
- `Rigidbody2D` is optional.
- If present, `Rigidbody2D.bodyType = Dynamic` and `constraints = FreezeRotation` by default.
- If `S_NPCEnemy.useRigidbodyMovement` is false, the Rigidbody2D is changed to Kinematic and movement is driven by Transform.
- If no Rigidbody2D is present, S_NPCEnemy uses Transform movement and Collider2D casts.
- `Collider2D` should still be present so sprint hit detection and projectile interaction work.

- `Rigidbody2D.bodyType = RigidbodyType2D.Dynamic` — gravity and ground collisions apply.
- `Rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation` — NPC stays upright.
- `gravityScale` is set in `S_NPCEnemy.Awake()` from the serialized `gravityScale` field (default 3).

### Ground Detection
Current implementation:
- Rigidbody mode uses `npcCol.GetContacts(groundContacts)` to read contact manifold.
- Transform mode uses a short downward `Collider2D.Cast` controlled by `transformGroundProbeDistance`.

Ground detection in `S_NPCEnemy.UpdateGroundCheck()`:
- Uses `npcCol.GetContacts(groundContacts)` to read contact manifold.
- Filters by `groundLayer` LayerMask (default `~0` = everything).
- Checks `Vector2.Dot(contact.normal, Vector2.up) > groundNormalThreshold` (default 0.5).
- This matches the pattern used in `S_fluid_climb.ClassifySurface()` — reliable for slopes, walls, and ceilings.

### Movement Rules
Current implementation:
- **Rigidbody mode, grounded**: set `npcRig.linearVelocity = new Vector2(moveSpeedX, npcRig.linearVelocity.y)` for horizontal movement.
- **Rigidbody mode, airborne**: set X velocity to 0 and let gravity pull the NPC down.
- **Transform mode**: move with `transform.position += Vector3.right * speedX * Time.deltaTime`.

- **When grounded**: set `npcRig.velocity = new Vector2(moveSpeedX, npcRig.velocity.y)` — horizontal movement with vertical free.
- **When airborne**: set `npcRig.velocity = new Vector2(0f, npcRig.velocity.y)` — freeze X, let gravity pull them down.
- `S_NPCEnemy.MoveHorizontally(speedX)` encapsulates this clamped-velocity pattern for all movement states.
- All movement states (Patrol/Chase/Aim/Attack/Arrest/Stunned) use `MoveHorizontally()` or the same clamped-velocity pattern.

### Sprint Knockback
Sprint hits call `S_NPCEnemy.OnSprintHit(Vector2 hitDirection)`.
- Always enters Stunned state through `EnterState(State.Stunned)`.
- Rigidbody mode applies horizontal knockback through `linearVelocity`.
- Transform mode stores a short-lived knockback velocity and moves manually.
- Transform knockback uses `Collider2D.Cast` against `knockbackObstacleLayer` to avoid passing through walls.

### Airborne Behavior
- If an NPC is knocked off a platform or in mid-air during state transitions, X velocity is frozen to 0.
- Gravity pulls them to the nearest surface below.
- Once grounded again, normal movement resumes.
- Idle wandering is suppressed while airborne (`ExecuteIdleWandering()` returns early if `!isGrounded`).

### Idle Wandering (no waypoints)
When `waypoints` is null or empty, the patrol state uses idle wandering:
- Random direction picked each walk cycle (`Random.Range(0, 360) * Deg2Rad` → unit vector).
- Walk time randomized between `wanderWalkTimeMin` / `wanderWalkTimeMax`.
- Pause time randomized between `wanderPauseTimeMin` / `wanderPauseTimeMax`.
- Boundary constraint: if distance from `spawnPosition` exceeds `wanderRadius`, direction is forced back toward spawn.
- Wandering only runs while grounded.

## Inspector Configuration

### S_NPCEnemy Fields
| Group | Field | Type | Description |
|-------|-------|------|-------------|
| State Machine | chaseRange | float | Distance to start chasing (default 8) |
| | loseRange | float | Distance to lose target (default 12) |
| | attackRange | float | Distance to start shooting (default 5) |
| | arrestRange | float | Distance to arrest (default 1.5) |
| | stunDuration | float | How long stunned state lasts (default 3) |
| Aim | attackWindupTime | float | Windup before firing (default 0.5) |
| | aimCooldownTime | float | Cooldown after aiming (default 2) |
| Arrest | arrestDuration | float | Max chase duration for arrest (default 3) |
| State Colors | aimColor | Color | Sprite tint during Aim (default orange) |
| | arrestColor | Color | Sprite tint during Arrest (default red) |
| | defaultColor | Color | Normal sprite tint (default white) |
| Attack | fireRate | float | Seconds between shots (default 1.5) |
| | projectilePrefab | GameObject | S_EMProjectile prefab |
| | projectileSpeed | float | Projectile travel speed (default 8) |
| | firePoint | Transform | Spawn point for projectiles |
| Patrol | waypoints | Transform[] | Patrol path points (null/empty → idle wandering) |
| | patrolSpeed | float | Patrol movement speed (default 2) |
| | waypointWaitTime | float | Wait time at each waypoint (default 1) |
| Movement Speed | chaseSpeed | float | Chase movement speed (default 5) |
| Movement Mode | useRigidbodyMovement | bool | Use Rigidbody2D movement when a Rigidbody2D is present |
| | requireGroundForTransformMovement | bool | Transform-mode NPCs only patrol/chase while grounded |
| | transformGroundProbeDistance | float | Downward cast distance for Transform-mode ground checks |
| Ground Detection | gravityScale | float | Gravity multiplier (default 3) |
| | groundLayer | LayerMask | Layers considered "ground" (default Everything) |
| | groundNormalThreshold | float | Min dot(normal, up) to count as ground (default 0.5) |
| Sprint Knockback | sprintKnockbackSpeed | float | Horizontal knockback speed from player sprint |
| | sprintKnockbackDuration | float | How long sprint knockback lasts |
| | sprintKnockbackDamping | float | How quickly Transform knockback velocity fades |
| | knockbackObstacleLayer | LayerMask | Layers that stop Transform knockback |
| | knockbackObstacleSkin | float | Extra cast distance used to stop before obstacles |
| Idle Wandering | wanderRadius | float | Max distance from spawn (default 3) |
| | wanderWalkTimeMin | float | Min walk duration (default 1) |
| | wanderWalkTimeMax | float | Max walk duration (default 3) |
| | wanderPauseTimeMin | float | Min pause between walks (default 0.5) |
| | wanderPauseTimeMax | float | Max pause between walks (default 2) |

### S_EMProjectile Fields
| Group | Field | Type | Description |
|-------|-------|------|-------------|
| Paralyze Effect | paralyzeDuration | float | How long player is paralyzed (default 3) |
| | moveSpeedReduction | float | Player speed multiplier while paralyzed (default 0.5) |
| Movement | speed | float | Projectile travel speed (default 8) |
| | maxLifetime | float | Auto-destroy after this many seconds (default 5) |

## Layer & Physics Setup

### Required Layers (TagManager.asset)
Create custom layers in Edit → Project Settings → Tags and Layers:
- **Player** layer (User Layer 8) — assigned to player's body child
- **Enemy** layer (User Layer 9) — assigned to NPC guard GameObjects
- **Projectile** layer (User Layer 10) — assigned to EM projectile prefab

### Enemy Layer Mask (S_Soild_sprint)
```
enemyLayer = Enemy (layer 9)
```
Set this in the Sprint Skill ScriptableObject's Inspector.

### Physics2D Collision Matrix (Physics2DSettings.asset)
Ensure the Layer Collision Matrix has:
- **Enemy × Player**: CHECKED (so guards detect player for chase/arrest)
- **Enemy × Enemy**: UNCHECKED (so guards don't block each other's projectiles)
- **Proj. × Player**: CHECKED (so projectiles hit the player)
- **Proj. × Ground**: CHECKED (so projectiles die on walls)

### Camera Culling Mask
If guards are invisible in Game View:
1. Select the Main Camera
2. In the Camera component, find **Culling Mask**
3. Ensure the **Enemy** layer is CHECKED
4. Also check **Projectile** layer if projectiles are invisible

## S_Soild_sprint Stun Mechanic
When Sprint is activated, `Physics2D.OverlapCircleAll()` searches for all colliders on the `enemyLayer` within `stunRadius`:
```csharp
Vector2 center = player.GetBodyTransform().position;
float dir = player.GetFaceRight() ? 1f : -1f;
Collider2D[] hits = Physics2D.OverlapCircleAll(center, stunRadius, enemyLayer);
foreach (Collider2D hit in hits)
{
    S_NPCEnemy enemy = hit.GetComponent<S_NPCEnemy>();
    if (enemy == null) enemy = hit.GetComponentInParent<S_NPCEnemy>();
    if (enemy != null) enemy.OnSprintHit(new Vector2(dir, 0f));
}
```
- Uses **body Transform position** (NOT root transform) as center
- Uses **LayerMask** (NOT tag comparison) for efficiency
- Only affects `S_NPCEnemy` components (ignores other NPC types)
- `OnSprintHit()` stuns and applies knockback even when the enemy has no Rigidbody2D

## Common Errors

### 1. Guards not visible in Game View
- **Symptom**: Guards exist in Hierarchy but don't render
- **Cause**: Camera Culling Mask doesn't include the Enemy layer
- **Fix**: Select Main Camera → Culling Mask → check Enemy layer

### 2. OverlapCircleAll returns 0 hits
- **Symptom**: Sprint stun never hits enemies; debug log shows `hits=0`
- **Causes** (check all):
  - `enemyLayer` field not set in the Sprint Skill asset Inspector
  - Guards are on Default layer, not Enemy layer
  - Enemy×Player collision unchecked in Physics2D Layer Collision Matrix
  - Camera Culling Mask issue (guards exist but invisible — separate from collision)
- **Fix**: Verify all three conditions above

### 3. Guards don't chase player
- **Symptom**: Guards stay in Patrol state; player walks through chaseRange
- **Cause**: `playerTransform` caching bug — references root GameObject (which doesn't move) instead of body
- **Fix**: Ensure `ValidatePlayerReference()` calls `S_Player.Instance.GetBodyTransform()`

### 4. Guards keep chasing after player hides
- **Symptom**: Player enters S_HideSpot, guards don't lose target
- **Cause**: Scene reload didn't reset `S_SuspicionSystem.PlayerHidden` (static field retained old value)
- **Fix**: `S_SuspicionSystem.HandleGameRestart()` must set `PlayerHidden = false`

### 5. Projectiles not hitting player
- **Symptom**: EMProjectile passes through player
- **Cause**: Projectile layer × Player layer unchecked in Physics2D collision matrix
- **Fix**: Check the collision matrix entry

### 6. NPC stuck red + death UI never shows after arrest
- **Symptom**: After arrest, NPC stays red (Discovered color). Cannot arrest again. Game restart doesn't reset NPC color. Death UI never appears.
- **Cause**: Three bugs stacked:
  1. `TriggerArrest()` set `currentState = State.Disabled` directly, bypassing `EnterState()` — color never reset to white
  2. `HandleGameStart()`/`HandleGameRestart()` set `currentState = State.Patrol` directly + manual `UpdateStateColor()` — inconsistent with state machine pattern
  3. `HandleArrest()` immediately called `GameReStart()` → scene reload → `HideUI()` → death UI `ShowUI()` overridden one frame later
- **Fix**: Use `EnterState()` for all three transitions. Remove auto-restart from `HandleArrest` — let `PlayerDied()` event drive the death UI display. Player manually clicks Restart to reload.
- **Lesson**: Never bypass the state machine. `EnterState()` is the single authority for all state-transition side effects.

### 7. NPC moves off ledge by transform.Translate (pre-physics refactor)
- **Symptom**: NPC drifts off platforms, doesn't fall with gravity
- **Cause**: `transform.Translate()` bypassed physics engine — no gravity, no ground collision
- **Fix**: Use Rigidbody2D.velocity with ground detection (this refactor)

---

## 8. NPC Jumping System (v0.7.0)

NPC enemies now have predictive jump ability to traverse gaps, walls, and reach higher platforms.

### 8.1 Jump Detection

`EvaluateJump()` runs every frame in `Update()` before `ExecuteMovement()`. It checks three conditions:

| Condition | Detection Method |
|-----------|-----------------|
| Wall ahead | Forward raycast from NPC center, distance = `obstacleDetectDistance` |
| Gap ahead | Multi-step forward ground scan with `FindLandingSpot()` |
| Player above | Height difference check (threshold to max height) + horizontal range |

### 8.2 Predictive Jump (FindLandingSpot)

When a gap is detected, the NPC scans forward in `gapScanStep` increments to find the landing platform:

```
Scan direction = facing direction
For each step (up to gapScanMaxSteps):
    Raycast down from probe position
    If ground found after a gap → record landing point
    Calculate jump parameters from distance and height difference
```

### 8.3 Jump Parameters

`CalculateJumpParameters()` adjusts force based on terrain:

| Factor | Adjustment |
|--------|-----------|
| Higher landing point | `jumpForce += heightDiff * 2f` |
| Far horizontal distance | `horizBoost = distance * 1.5f` |

### 8.4 Air Control

`MoveHorizontally()` now preserves `airControlFactor` (default 50%) horizontal velocity when airborne, creating natural jump arcs.

### 8.5 Inspector Parameters (Jump Ability section)

| Parameter | Default | Description |
|-----------|---------|-------------|
| canJump | true | Enable/disable jumping |
| jumpForce | 8f | Base jump impulse force |
| jumpCooldown | 1.0s | Minimum time between jumps |
| obstacleDetectDistance | 1.0f | Forward raycast distance for wall detection |
| gapDetectHeight | 0.5f | Ground scan ray origin height |
| gapScanStep | 0.3f | Distance between scan points |
| gapScanMaxSteps | 10 | Maximum forward scan distance |
| airControlFactor | 0.5f | Horizontal control factor while airborne |
| playerAboveThreshold | 1.0f | Minimum height difference to trigger jump toward player |
| playerAboveMaxHeight | 4.0f | Maximum height difference for player-above jump |
| jumpObstacleLayer | ~0 | Layer mask for obstacle detection |

### 8.6 Jump States

Jumping only activates during Patrol, Chase, and Arrest states. Disabled during Aim and Stunned.

---

## 9. NPC Wave Spawner (v0.7.0)

`S_NPCWaveSpawner` generates NPC waves at runtime, spawning enemies at camera edges.

### 9.1 Behavior

```
Every spawnInterval seconds:
    1. Get camera left/right edge X positions + sideOffset
    2. Raycast down from camera height to find ground
    3. Instantiate npcsPerSide NPCs on each side
    4. Set facing direction toward camera center
    5. Respect maxAliveNpcs limit
    6. Cleanup distant NPCs beyond cleanupDistance
```

### 9.2 Inspector Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| targetCamera | (drag) | Camera reference (falls back to Camera.main) |
| npcPrefab | (drag) | NPC prefab to spawn |
| spawnInterval | 30f | Seconds between waves |
| npcsPerSide | 2 | NPCs spawned per side per wave |
| maxAliveNpcs | 20 | Maximum NPCs alive at once |
| sideOffset | 2f | Extra distance beyond camera edge |
| groundDetectDistance | 10f | Downward raycast distance for ground |
| groundLayer | ~0 | Ground detection layer |
| cleanupDistantNpcs | true | Auto-destroy far NPCs |
| cleanupDistance | 30f | Distance threshold for cleanup |
| cleanupCheckInterval | 5f | Seconds between cleanup checks |
| spawnYFallback | 0f | Y position used when ground raycast finds no surface |
| debugLogs | false | Enable debug logging for spawn/cleanup events |

### 9.3 Setup

1. Create empty GameObject in scene
2. Attach `S_NPCWaveSpawner`
3. Drag NPC prefab into `npcPrefab`
4. Drag scene camera into `targetCamera`
5. Adjust parameters as needed

---

## 10. NPC Story System

`S_NPCStory` extends `S_NPCbase` and provides NPC dialogue/story trigger behavior with waypoint-based movement.

### 10.1 Behavior

NPC walks between waypoints, triggers story events when player enters interact range.

### 10.2 Key Fields

| Field | Type | Description |
|-------|------|-------------|
| waypoints | Transform[] | Waypoint path for NPC movement |
| storyTriggerID | string | ID passed to `S_GameEvent.StoryTrigger()` |
| interactRange | float | Distance to trigger interaction |

---

## 11. NPC Camera System

`S_NPCCamera` extends `S_NPCbase` and provides NPC-driven camera movement (e.g., cutscene cameras).

### 11.1 Behavior

Camera follows waypoints at configurable speed with wait times at each point.

### 11.2 Key Fields

| Field | Type | Description |
|-------|------|-------------|
| waypoints | Transform[] | Camera path points |
| patrolSpeed | float | Movement speed between points |
| waitTime | float | Wait duration at each waypoint |
| loopMode | bool | Whether camera loops back to start |
