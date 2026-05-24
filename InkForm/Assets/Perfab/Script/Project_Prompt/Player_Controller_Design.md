# Player Controller — Design Document

## 1. Overview

The player controller (`S_Player`) is the core gameplay component managing movement, jumping, form switching, paralyze effects, sprint charging, camera control, and integration with the skill system. It uses Unity's new Input System for input handling and Rigidbody2D for physics. `S_Player` implements the `IPlayerActor` interface, which decouples player access from concrete class references throughout the codebase.

`S_PlayerSkillController` owns the sprint charge state machine and camera control logic, extracted from `S_Player` in v0.8.0 to reduce monolithic complexity.

`S_CameraMove` provides smooth camera tracking that follows the player, with support for manual camera control via the Camera Control skill.

### Core Gameplay Loop

```
Player starts in SOLID form
    |-- Move horizontally (WASD/Left Stick)
    |-- Jump (Space) with cooldown + max jump count
    |-- Sprint skill (Left Shift) — hold-to-charge, release to dash
    |-- Break breakable blocks on contact
    |-- Camera Control (C) — bullet time + manual camera pan
    |
Form Switch (toggle)
    |
Player switches to FLUID form
    |-- Same horizontal movement
    |-- Jump with reduced gravity
    |-- Wall climb (Grip input) — stick to surfaces, climb along them
    |-- Enter pipelines for teleportation
```

---

## 2. Architecture

### 2.1 Script Dependencies

```
S_Player (Singleton, implements IPlayerActor)
|-- Input System: S_InputBindingManager shared InputSystem_Actions (Move, Jump, Sprint, Grip, CameraControl)
|-- Physics: Rigidbody2D, CircleCollider2D, PhysicsMaterial2D
|-- Rendering: SpriteRenderer, Sprite[], S_PlayerProceduralRenderer, S_PlayerDynamicCollider
|-- Skills: S_fluid_climb (wall climbing), S_Soild_sprint (sprint charge), S_CameraControlSkill (camera control)
|-- Skill Controller: S_PlayerSkillController (sprint charge + camera control logic, injected at Awake)
|-- Contracts: IPlayerActor interface (decouples player from NPC/Level systems)
|-- Player Lookup: S_PlayerLookup (static utility resolving IPlayerActor from colliders)
|-- Audio: S_GameEvent.PlaySFX() (jump, form switch, sprint charge SFX)
|-- Events: S_SkillTree (sprint/camera skill access)
`-- Camera: S_CameraMove (follow + manual control)
```

### 2.2 Form System

The player has two forms with different physics properties and abilities:

```
Form: solid                          Form: fluid
|-- SolidMat physics material        |-- FluidMat physics material
|-- Standard gravity (4x)            |-- Reduced gravity
|-- Direct velocity movement         |-- Delegates to S_fluid_climb when gripping
|-- Can break breakable blocks       |-- Can enter pipelines
`-- Sprint skill available           `-- Wall climb skill available
```

**Form Switching**: Toggled via `SetForm(bool)` — `false` = solid, `true` = fluid. The physics material is swapped instantly, changing friction and bounce properties.

### 2.3 Movement Flow

```
FixedUpdate()
    |
    |-- solid form? --> SolidMovement()
    |                    |-- Read horizontal input
    |                    |-- Set velocity directly
    |                    |-- Classify surface contacts
    |                    `-- Update facing direction
    |
    `-- fluid form? --> FluidMovement()
                         |-- gripping? --> S_fluid_climb.FluidMovementTick()
                         `-- not gripping? --> SolidMovement()
```

### 2.4 Hierarchy

```
Player (S_Player component)
├── Body (child)
│   ├── Rigidbody2D
│   ├── CircleCollider2D
│   ├── PhysicsMaterial2D (SolidMat or FluidMat, swapped at runtime)
│   ├── SpriteRenderer
│   └── S_coleve (ground/lava detection)
├── S_SkillTree (skill management)
```

**Important**: `S_Player` manages the root GameObject. Physics components (Rigidbody2D, Collider2D) are on the **Body** child. `S_Player.body` holds the reference.

---

## 3. Script Details

### 3.1 S_Player.cs

**Type**: MonoBehaviour (Singleton, implements `IPlayerActor`)

**Singleton Pattern**: Uses `Instance` static property. Set in `Awake()`.

**Interface**: `S_Player` implements `IPlayerActor` which exposes `Rigidbody`, `Collider`, `BodyTransform`, `IsFluidForm`, `IsParalyzed`, `IsSprintMomentumActive`, `FacingRight`, `Teleport`, `SetMovementLocked`, `ApplyParalyze`, `ForceSprintBreakthrough`, and `CancelSprintCharge`. Other systems access the player through `S_PlayerLookup.TryGet()` instead of directly referencing `S_Player.Instance`.

**Serialized Fields**:
| Field | Default | Description |
|-------|---------|-------------|
| MoveSpeed | 10f | Horizontal movement speed |
| JumpSpeed | 10f | Jump impulse force |
| MaxJump | 1 | Maximum jumps before landing (2 = double jump) |
| JumpCoolDownTime | 0.1s | Cooldown between jumps |
| body | - | Child GameObject with Rigidbody2D/SpriteRenderer/Collider |
| sprites | - | Sprite array: [0]=solid-right, [1]=solid-left, [2]=fluid-right, [3]=fluid-left |
| SolidMat | - | Physics material for solid form |
| FluidMat | - | Physics material for fluid form |
| fluidClimbSkill | - | Reference to S_fluid_climb ScriptableObject |
| solidGravityScale | 4f | Gravity scale in solid form |
| kickForceMultiplier | 10f | Force multiplier for KickOut() |
| jumpClip | - | SFX played on jump |
| formSwitchClip | - | SFX played when form changes |
| paralyzeSlowMultiplier | 0.5f | Movement speed multiplier while paralyzed |
| defaultParalyzeDuration | 3f | Default duration of paralyze effect (seconds) |
| useProceduralRenderer | true | Enable procedural slime rendering path |
| proceduralRenderer | - | Reference to Body S_PlayerProceduralRenderer |
| useDynamicCollider | true | Enable dynamic circle/capsule collider path |
| dynamicCollider | - | Reference to Body S_PlayerDynamicCollider |
| cameraController | - | Reference to S_CameraMove for manual camera control |

**Public Methods**:
| Method | Parameters | Description |
|--------|------------|-------------|
| `SetForm(form)` | `form` enum (solid/fluid) | Switch between forms, swap physics material. Fires `S_GameEvent.PlaySFX(formSwitchClip)` only when form actually changes |
| `getForm()` | none → bool | Returns false=solid, true=fluid |
| `SetSprinting(bool)` | bool | Set sprinting state (blocked while paralyzed) |
| `SetSprintMomentum(bool)` | bool | Track sprint momentum state |
| `IsSprintMomentumActive` | property → bool | True if sprinting or sprint momentum is active |
| `IsSprintCharging` | property → bool | True if currently charging sprint |
| `IsParalyzed` | property → bool | True if player is paralyzed |
| `IsMovementLocked` | property → bool | True if movement is locked |
| `ForceSprintBreakthrough(float direction, float minSpeed, float duration)` | float, float, float | Preserve sprint momentum through obstacles |
| `ApplyParalyze(float duration, float slowMultiplier)` | float, float | Apply paralyze effect with custom duration/multiplier |
| `SetMovementLocked(bool locked)` | bool | Lock/unlock all movement (resets velocity on lock) |
| `BeginCameraControl(S_CameraControlSkill)` | S_CameraControlSkill | Enter camera control mode (bullet time + manual pan) |
| `ReleaseSprintCharge()` | none | Release charged sprint dash |
| `GetRigidbody()` | none → Rigidbody2D | Returns player's Rigidbody2D |
| `GetCollider()` | none → Collider2D | Returns active Collider2D (dynamic or fallback) |
| `GetMoveInput()` | none → float | Returns horizontal input (-1 to 1) |
| `GetClimbInput()` | none → float | Returns vertical input (-1 to 1) |
| `GetMoveVector()` | none → Vector2 | Returns full move input vector |
| `GetMoveSpeed()` | none → float | Returns current move speed |
| `GetBodyTransform()` | none → Transform | Returns body child Transform |
| `SetFacingRight(bool)` | bool | Force facing direction |
| `GetFaceRight()` | none → bool | Returns facing direction |
| `KickOut()` | none | Apply knockback impulse in velocity direction |
| `Teleport(Vector2)` | Vector2 position | Instantly move to position, reset velocity |

**Private Methods**:
| Method | Description |
|--------|-------------|
| `SolidMovement()` | Horizontal movement + surface classification |
| `FluidMovement()` | Checks grip → delegates to S_fluid_climb or falls back to SolidMovement |
| `UpdateSprite()` | Delegates to procedural renderer or flips sprite |
| `Jump()` | Applies jump impulse with cooldown and max jump count. Fires `S_GameEvent.PlaySFX(jumpClip)` |
| `BeginSprintCharge()` | Start sprint charge (buffer → charge → release flow) |
| `UpdateSprintCharge()` | Per-frame charge update (stage progression, visuals, shake) |
| `ReleaseSprintCharge()` | Release charge → apply sprint impulse + cooldown |
| `UpdateSprintBreakthrough()` | Preserve minimum horizontal speed during breakthrough |
| `HandleCameraControlInput()` | Detect CameraControl input and activate/deactivate |
| `CameraControlTick()` | Feed move input to camera during manual control |
| `EndCameraControl()` | Restore time scale and camera state |
| `ParalyzeRoutine(float, float)` | Coroutine: apply slow + jump reduction, restore after duration |

### 3.2 S_CameraMove.cs

**Type**: MonoBehaviour

**Serialized Fields**:
| Field | Default | Description |
|-------|---------|-------------|
| target | - | GameObject to follow (player body) |
| minMoveSpeed | 50f | Base interpolation speed |
| manualMoveSpeed | 8f | Camera pan speed during manual control |
| manualMaxDistanceFromTarget | 8f | Maximum camera offset from target during manual control |
| returnSmoothSpeed | 12f | Speed at which camera returns to target after manual control |
| followDeadZoneRadius | 1.5f | Minimum distance before camera starts following target |
| drawDeadZoneGizmo | true | Show dead zone gizmo in Scene view |

**Public Methods**:
| Method | Description |
|--------|-------------|
| `BeginManualControl()` | Switch to manual camera control mode |
| `ManualControlTick(Vector2)` | Feed move input for camera panning (clamped to max distance) |
| `EndManualControl()` | Return to automatic follow mode |

**Movement**: In follow mode, uses exponential decay interpolation `1 - exp(-speed * dt)` with dead zone. In manual mode, moves camera via `Time.unscaledDeltaTime` (works during bullet time) and clamps distance from target. Preserves camera Z position.

**Setup**: Attach to the Main Camera. Set `target` to the player's Body child GameObject.

---

## 4. Input System

`S_Player` reads input actions through `S_InputBindingManager.Instance.Actions` so runtime UI rebinding and saved binding overrides apply immediately to gameplay input.

| Action | Binding | Usage |
|--------|---------|-------|
| Move | WASD / Left Stick | Horizontal movement, vertical climb input |
| Jump | Space / South Button | Jump with cooldown |
| Sprint | Left Shift / East Button | Hold to charge sprint, release to dash |
| Grip | G / West Button | Wall climbing in fluid form |
| CameraControl | C / - | Hold for bullet time + manual camera pan |

### Input Flow

```
Update()
    |-- Handle CameraControl input (hold → activate, release → deactivate)
    |-- Read Jump input (Space) -> Call Jump()
    |-- Sprint: WasPerformedThisFrame() -> BeginSprintCharge()
    |           WasReleasedThisFrame() -> ReleaseSprintCharge()
    `-- Read Grip input (G) -> Set gripping flag for S_fluid_climb

FixedUpdate()
    |-- Apply movement based on form
    |-- Update sprint charge visuals/physics
    |-- Update sprint breakthrough
    `-- Update dynamic collider
```

---

## 5. Unity Setup

### 5.1 Player GameObject

1. Create a root GameObject named "Player"
2. Add `S_Player` component to the root
3. Create a child GameObject named "Body"
4. On **Body**, add:
   - `Rigidbody2D` (Gravity Scale = 4, Freeze Rotation Z = true, Interpolation = Interpolate)
   - `CircleCollider2D`
   - `SpriteRenderer`
   - `S_coleve` component
5. Create 4 sprites (solid-right, solid-left, fluid-right, fluid-left) and assign to `sprites[]`
6. Create PhysicsMaterial2D assets:
   - **SolidMat**: Friction = 0.4, Bounce = 0
   - **FluidMat**: Friction = 0, Bounce = 0

### 5.2 Camera Setup

1. Select Main Camera
2. Add `S_CameraMove` component
3. Set `target` to the player's Body GameObject
4. Adjust `minMoveSpeed` for smoothness (50 = responsive, lower = smoother)

### 5.3 Required Tags and Layers

| GameObject | Tag | Layer |
|------------|-----|-------|
| Player root | `Player` | Default |
| Body child | (none) | Default |
| Ground platforms | (any) | `Ground` |

### 5.4 Input Actions Setup

1. Open `InputSystem_Actions` Input Actions asset
2. Ensure the following Action Maps exist:
   - **Player**: Move, Jump, Sprint, Grip
   - **UI**: OpenMenu

---

## 6. Physics Configuration

### 6.1 Rigidbody2D (on Body)

| Setting | Value | Reason |
|---------|-------|--------|
| Body Type | Dynamic | Affected by gravity and physics |
| Gravity Scale | 4 (solid) / varies (fluid) | Heavier feel in solid form |
| Linear Drag | 0 | Movement handled by script, not drag |
| Angular Drag | 0 | Freeze rotation prevents spin |
| Freeze Rotation Z | true | Player doesn't rotate from physics |
| Interpolation | Interpolate | Smooth visual movement |

### 6.2 PhysicsMaterial2D

| Material | Friction | Bounce | Use Case |
|----------|----------|--------|----------|
| SolidMat | 0.4 | 0 | Solid form — some friction for ground control |
| FluidMat | 0 | 0 | Fluid form — zero friction for sliding/climbing |

### 6.3 Surface Classification

The player classifies surface contacts to determine movement behavior:

```
Surface Types:
- Floor: normal.y > 0.5 (surface below player)
- Wall Left: normal.x > 0.5 (surface to the left)
- Wall Right: normal.x < -0.5 (surface to the right)
- Ceiling: normal.y < -0.5 (surface above player)
```

---

## 7. Common Issues

| Issue | Solution |
|-------|----------|
| Player not moving | Check S_InputBindingManager exists and its shared InputSystem_Actions is enabled |
| Jumping feels floaty | Increase solidGravityScale |
| Player sliding on walls | Check SolidMat friction value |
| Camera jitter | Ensure Rigidbody2D has interpolation enabled |
| Form switching not working | Verify SolidMat/FluidMat are assigned |
| Player falls through platform | Check Rigidbody2D Collision Detection = Continuous |
| Sprite not flipping | Ensure sprites[] array has correct ordering [right, left] |
| Double jump not working | Set MaxJump >= 2 in Inspector |

---

## 8. Procedural Slime Rendering Baseline

The player body can now use procedural slime rendering through `S_PlayerProceduralRenderer` on the Body child. `S_Player` owns two serialized toggles/references:

| Field | Description |
|-------|-------------|
| useProceduralRenderer | Enables the procedural rendering path in `UpdateSprite()` |
| proceduralRenderer | Reference to the Body `S_PlayerProceduralRenderer` |
| useDynamicCollider | Enables the dynamic circle/capsule collider path |
| dynamicCollider | Reference to the Body `S_PlayerDynamicCollider` |

When enabled, `S_PlayerProceduralRenderer` hides the fallback `SpriteRenderer` and generates mesh children for body, outline, eyes, and eye glow. The fallback `sprites[]` are still kept on `Pre_MainChar.prefab` so the player can fall back to sprite rendering if the procedural path is disabled.

`S_PlayerDynamicCollider` keeps normal movement on `CircleCollider2D`, then switches to a dynamic `CapsuleCollider2D` for crouch/slick, wall climbing, and ceiling climbing. `S_Player.GetCollider()` returns the active collider, so climb classification, grip buffer casts, and procedural contact rendering all follow the current physical shape.

---

## 9. Sprint Charge System

The sprint skill has been replaced with a hold-to-charge sprint system. Instead of `WasPerformedThisFrame()` triggering an instant dash, the player now holds the sprint key to charge, and releases to dash.

**v0.8.0 Note**: Sprint charge logic has been extracted from `S_Player` into `S_PlayerSkillController`. `S_Player` delegates `BeginSprintCharge()`, `ReleaseSprintCharge()`, and `CancelSprintCharge()` to the skill controller. The charge state machine, visual updates, and audio management all live in `S_PlayerSkillController`.

### 9.1 Behavior Overview

```
Sprint key pressed
    |-- Buffer phase (0 ~ bufferTime, default 0.15s)
    |   |-- No visual/physics changes
    |   |-- Player can still move, jump, grip normally
    |   `-- If released during buffer → instant minSprintSpeed dash (quick-tap)
    |
    |-- Charge phase (bufferTime ~ maxChargeTime)
    |   |-- Visual: procedural renderer → perfect circle, tail hidden, eyes follow velocity
    |   |-- Physics: low-friction ball material, rotation unlocked (rolls on ground)
    |   |-- Collider: scales with charge stage (1.0x → 1.3x → 1.6x)
    |   |-- Player can still move, jump, grip during charge
    |   |-- Three stages with shake transition effects:
    |   |   Stage 1 (0s): 1.0x scale, 0.1s cooldown
    |   |   Stage 2 (stage2Time): 1.3x scale, 0.5s cooldown
    |   |   Stage 3 (stage3Time): 1.6x scale, 1.0s cooldown
    |   `-- Direction locked at charge start (visual only)
    |
    Sprint key released
    |-- Release direction = current facing at release moment
    |-- Sprint speed = Lerp(minSprintSpeed, maxSprintSpeed, charge01)
    |-- Stun nearby enemies via OverlapCircleAll
    |-- SprintLock coroutine for form/physics restore
    `-- Cooldown set based on release stage
```

### 9.2 S_PlayerSkillController Sprint Charge Fields

These fields now live in `S_PlayerSkillController` (previously in `S_Player`):

| Field | Type | Description |
|-------|------|-------------|
| `isSprintCharging` | bool | Whether charge is active |
| `sprintChargeTimer` | float | Accumulated charge time |
| `sprintChargeStage` | int | Current stage (0, 1, 2) |
| `chargeScaleMultiplier` | float | Current visual/collider scale |
| `chargeVisualsActive` | bool | Whether buffer has passed and visuals are active |
| `sprintCooldownRemaining` | float | Remaining cooldown seconds |

### 9.3 S_Soild_sprint Charge Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `maxChargeTime` | 2f | Maximum charge duration for full speed |
| `maxSprintSpeed` | 200f | Maximum sprint impulse at full charge |
| `minSprintSpeed` | 20f | Minimum sprint impulse (quick-tap) |
| `stage1Scale` | 1.0 | Scale multiplier for stage 1 |
| `stage2Scale` | 1.3 | Scale multiplier for stage 2 |
| `stage3Scale` | 1.6 | Scale multiplier for stage 3 |
| `stage2Time` | 0.5s | Time to enter stage 2 |
| `stage3Time` | 1.2s | Time to enter stage 3 |
| `shakeFrequency` | 25 | Stage transition shake frequency |
| `shakeAmplitude` | 0.15 | Stage transition shake amplitude |
| `shakeDecay` | 5 | Shake exponential decay rate |
| `stage1Cooldown` | 0.1s | Cooldown after stage 1 release |
| `stage2Cooldown` | 0.5s | Cooldown after stage 2 release |
| `stage3Cooldown` | 1.0s | Cooldown after stage 3 release |
| `bufferTime` | 0.15s | Quick-tap buffer threshold |
| `chargeBallMaterial` | - | Low-friction physics material for rolling |

### 9.4 Integration Points

- **S_PlayerProceduralRenderer**: `SetChargeOverride(bool)` toggles perfect-circle rendering during charge
- **S_PlayerDynamicCollider**: `SetChargeOverride(bool, float)` scales collider with charge stage
- **S_SkillTree**: `GetSprintSkill()` returns the S_Soild_sprint instance for charge parameter access

---

## 10. Camera Control System

The player can hold the CameraControl input to enter a bullet-time state with manual camera panning. This allows the player to survey the level ahead while slowing down time.

**v0.8.0 Note**: Camera control logic has been extracted from `S_Player` into `S_PlayerSkillController`. `S_Player` delegates `HandleCameraControlInput()`, `BeginCameraControl()`, `EndCameraControl()`, and `CameraControlTick()` to the skill controller.

### 10.1 Behavior

```
CameraControl pressed (hold)
    |-- Time.timeScale scaled by bulletTimeScale (default 0.2x)
    |-- Camera switches to manual control mode
    |-- Player can pan camera with Move input (WASD/Left Stick)
    |-- Camera clamped to manualMaxDistanceFromTarget from player
    `-- Player movement still works (at reduced time scale)

CameraControl released
    |-- Time.timeScale restored to original value
    |-- Camera smoothly returns to follow mode
```

### 10.2 S_CameraControlSkill Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `bulletTimeScale` | 0.2f | Time scale multiplier during camera control (0.01–1.0) |

### 10.3 Blocking Conditions

Camera control is blocked when:
- Player is already in camera control mode
- Player is paralyzed
- Movement is locked
- Sprint is charging
- Time scale is already 0 (paused)

---

## 11. Movement Lock System

`S_Player.SetMovementLocked(bool)` provides a way to freeze/unfreeze player movement from external systems (e.g., hiding spots, cutscenes).

### 11.1 Behavior

| Action | Effect |
|--------|--------|
| `SetMovementLocked(true)` | Sets `movementLocked = true`, zeroes velocity and angular velocity |
| `SetMovementLocked(false)` | Sets `movementLocked = false`, player can move again |

### 11.2 Blocked Actions While Locked

- All horizontal movement (`SolidMovement`/`FluidMovement` skipped)
- Jumping (early return in `Jump()`)
- Grip/climb input (cleared in `StateRunner()`)
- Sprint charge cannot begin (movement locked check in `BeginSprintCharge`)

### 11.3 Callers

- `S_HideSpot`: Locks movement when player enters a hide spot, unlocks when exiting (via `IPlayerActor.SetMovementLocked`)
- `S_SceneCheckpointTracker`: Uses `IPlayerActor.Teleport()` for respawn after death
- Any system requiring cutscene/pause behavior
