# Player Controller — Design Document

## 1. Overview

The player controller (`S_Player`) is the core gameplay component managing movement, jumping, form switching, and integration with the skill system. It uses Unity's new Input System for input handling and Rigidbody2D for physics.

`S_CameraMove` provides smooth camera tracking that follows the player.

### Core Gameplay Loop

```
Player starts in SOLID form
    |-- Move horizontally (WASD/Left Stick)
    |-- Jump (Space) with cooldown + max jump count
    |-- Sprint skill (Left Shift) — burst of forward momentum
    |-- Break breakable blocks on contact
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
S_Player (Singleton)
|-- Input System: S_InputBindingManager shared InputSystem_Actions (Move, Jump, Sprint, Grip)
|-- Physics: Rigidbody2D, CircleCollider2D, PhysicsMaterial2D
|-- Rendering: SpriteRenderer, Sprite[]
|-- Skills: S_fluid_climb (wall climbing integration)
|-- Audio: S_GameEvent.PlaySFX() (jump, form switch SFX)
`-- Events: S_SkillTree (sprint activation)
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

**Type**: MonoBehaviour (Singleton)

**Singleton Pattern**: Uses `Instance` static property. Set in `Awake()`.

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

**Public Methods**:
| Method | Parameters | Description |
|--------|------------|-------------|
| `SetForm(form)` | bool (false=solid, true=fluid) | Switch between forms, swap physics material. Fires `S_GameEvent.PlaySFX(formSwitchClip)` only when form actually changes |
| `getForm()` | none → bool | Returns false=solid, true=fluid |
| `SetSprinting(bool)` | bool | Lock/unlock movement during sprint |
| `SetSprintMomentum(bool)` | bool | Track sprint momentum state |
| `GetRigidbody()` | none → Rigidbody2D | Returns player's Rigidbody2D |
| `GetCollider()` | none → Collider2D | Returns player's Collider2D |
| `GetMoveInput()` | none → float | Returns horizontal input (-1 to 1) |
| `GetClimbInput()` | none → float | Returns vertical input (-1 to 1) |
| `GetMoveSpeed()` | none → float | Returns current move speed |
| `GetBodyTransform()` | none → Transform | Returns body child Transform |
| `SetFacingRight(bool)` | bool | Force facing direction |
| `GetFaceRight()` | none → bool | Returns facing direction |
| `KickOut()` | none | Apply knockback impulse in velocity direction |
| `Teleport(Vector2)` | Vector2 position | Instantly move to position, reset velocity |

**Private Methods**:
| Method | Description |
|--------|-------------|
| `SolidMovement()` | Horizontal movement + vertical input + surface classification |
| `FluidMovement()` | Checks grip → delegates to S_fluid_climb or falls back to SolidMovement |
| `UpdateSprite()` | Flips sprite based on facing direction |
| `Jump()` | Applies jump impulse with cooldown and max jump count. Fires `S_GameEvent.PlaySFX(jumpClip)` |

### 3.2 S_CameraMove.cs

**Type**: MonoBehaviour

**Serialized Fields**:
| Field | Default | Description |
|-------|---------|-------------|
| target | - | GameObject to follow (player body) |
| minMoveSpeed | 50f | Interpolation speed |

**Movement**: Uses exponential decay interpolation `1 - exp(-speed * dt)` for frame-rate independent smooth following. Preserves camera Z position.

**Setup**: Attach to the Main Camera. Set `target` to the player's Body child GameObject.

---

## 4. Input System

`S_Player` reads input actions through `S_InputBindingManager.Instance.Actions` so runtime UI rebinding and saved binding overrides apply immediately to gameplay input.

| Action | Binding | Usage |
|--------|---------|-------|
| Move | WASD / Left Stick | Horizontal movement, vertical climb input |
| Jump | Space / South Button | Jump with cooldown |
| Sprint | Left Shift / East Button | Activate sprint skill |
| Grip | G / West Button | Wall climbing in fluid form |

### Input Flow

```
Update()
    |-- Read Move input (WASD/Left Stick)
    |-- Read Jump input (Space) -> Call Jump()
    |-- Read Sprint input (Left Shift) -> S_SkillTree.ActivateSkill("Sprint")
    `-- Read Grip input (G) -> Set gripping flag for S_fluid_climb

FixedUpdate()
    |-- Apply movement based on form
    `-- Update physics
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
| useDynamicCollider | Enables the current dynamic circle collider path |
| dynamicCollider | Reference to the Body `S_PlayerDynamicCollider` |

When enabled, `S_PlayerProceduralRenderer` hides the fallback `SpriteRenderer` and generates mesh children for body, outline, eyes, and eye glow. The fallback `sprites[]` are still kept on `Pre_MainChar.prefab` so the player can fall back to sprite rendering if the procedural path is disabled.

`S_PlayerDynamicCollider` currently adjusts the existing `CircleCollider2D` radius and offset for crouch/slick input, wall attachment, ceiling attachment, speed shrink, and impact compression. The next planned phase is a dynamic `CapsuleCollider2D` that better matches the slime silhouette.
