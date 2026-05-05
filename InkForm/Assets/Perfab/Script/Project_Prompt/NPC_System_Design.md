# NPC System Design

## Overview
The NPC system provides guard patrol/chase/attack behavior for Chapter 2 (The Nurserie). Guards detect the player, transition through a 5-state state machine, fire EM projectiles to paralyze the player, and arrest on contact. Guards lose their target when the player hides (via `S_SuspicionSystem.PlayerHidden`).

## Architecture

### Class Hierarchy
```
MonoBehaviour
 └── S_NPCbase          (base: identity, interaction, sprite/Rigidbody2D refs)
      └── S_NPCEnemy    (guard: full state machine, projectile attack)
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
- **Patrol**: Walk between `waypoints[]`, wait `waypointWaitTime` at each
- **Chase**: Move toward player at `chaseSpeed`, activated when player enters `chaseRange`
- **Attack**: Fire `projectilePrefab` at `fireRate` when player within `attackRange`
- **Arrest**: Trigger arrest when player within `arrestRange`
- **Stunned**: S_Soild_sprint sets this; duration `stunDuration`, then returns to Patrol
- **Disabled**: All behavior suspended; guards do not see/react to player

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

## Inspector Configuration

### S_NPCEnemy Fields
| Group | Field | Type | Description |
|-------|-------|------|-------------|
| State Machine | chaseRange | float | Distance to start chasing (default 8) |
| | loseRange | float | Distance to lose target (default 12) |
| | attackRange | float | Distance to start shooting (default 5) |
| | arrestRange | float | Distance to arrest (default 1.5) |
| | stunDuration | float | How long stunned state lasts (default 3) |
| Attack | fireRate | float | Seconds between shots (default 1.5) |
| | projectilePrefab | GameObject | S_EMProjectile prefab |
| | projectileSpeed | float | Projectile travel speed (default 8) |
| | firePoint | Transform | Spawn point for projectiles |
| Patrol | waypoints | Transform[] | Patrol path points |
| | patrolSpeed | float | Patrol movement speed (default 2) |
| | waypointWaitTime | float | Wait time at each waypoint (default 1) |
| Movement Speed | chaseSpeed | float | Chase movement speed (default 5) |

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
Collider2D[] hits = Physics2D.OverlapCircleAll(center, stunRadius, enemyLayer);
foreach (Collider2D hit in hits)
{
    S_NPCEnemy enemy = hit.GetComponent<S_NPCEnemy>();
    if (enemy != null) enemy.Stun();
}
```
- Uses **body Transform position** (NOT root transform) as center
- Uses **LayerMask** (NOT tag comparison) for efficiency
- Only affects `S_NPCEnemy` components (ignores other NPC types)

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