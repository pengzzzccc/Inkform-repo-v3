# Skill System 閳?Design Document

## 1. Overview

The skill system allows players to unlock and activate abilities using skill points. It uses ScriptableObject-based skill definitions for data-driven design, with a centralized `S_SkillTree` manager for unlocking and activation. Active skill use is gated by the player's shared `S_PlayerEnergy` pool.

### Supported Skills
- **Sprint** (`S_Soild_sprint`) - Hold-to-charge sprint with buffer, stage scaling, quick-tap cost, and shared energy drain
- **Fluid Climb** (`S_fluid_climb`) 閳?Wall/ceiling climbing in fluid form
- **Camera Control** (`S_CameraControlSkill`) 閳?Bullet time + manual camera pan

---

## 2. Architecture

### 2.1 Class Hierarchy

```
S_SkillBase (ScriptableObject, abstract)
|-- S_Soild_sprint (concrete: sprint charge skill)
|-- S_fluid_climb  (concrete: wall climb skill)
|-- S_CameraControlSkill (concrete: camera control skill)
`-- (future skills extend this base)

S_SkillTree (MonoBehaviour, Singleton)
|-- S_SkillBase[] allSkills (all registered skill assets)
|-- Dictionary<string, S_SkillBase> unlockedMap
`-- int skillPoints

S_PlayerSkillController (MonoBehaviour, v0.8.0) 閳?extracted from S_Player
|-- Owns sprint charge state machine (BeginSprintCharge, FixedTick, Release, Cancel)
|-- Owns camera control logic (BeginCameraControl, EndCameraControl, CameraControlTick)
|-- Initialized via injection from S_Player.Initialize()
`-- Delegates to S_SkillTree for skill parameter access

S_PlayerEnergy (MonoBehaviour, v0.8.1)
|-- Single shared energy pool on the player
|-- Broadcasts OnPlayerEnergyChanged(current, max)
`-- Skills consume energy using their own Inspector-configured costs
```

### 2.2 Why ScriptableObjects?

ScriptableObjects are used because:
- **Data-driven**: Skill parameters (speed, energy drain, force) are defined in assets, not hardcoded
- **Energy-driven**: Each skill asset configures its own energy threshold and drain while sharing one player energy bar
- **Reusability**: The same skill asset can be referenced by multiple systems
- **Editor-friendly**: Designers can tweak values in the Inspector without touching code
- **Runtime persistence**: Skill state (unlocked/locked) is managed by S_SkillTree, not the SO itself

### 2.3 Skill Lifecycle

```
Unlock Flow:
1. Player earns skill points (AddSkillPoints)
2. Player calls TryUnlock("SkillName")
3. System checks: exists? already unlocked? prerequisites met? enough points?
4. If all pass: deduct points, mark unlocked, call OnUnlocked()

Activation Flow:
1. Player presses skill input (e.g., Sprint button)
2. S_Player calls S_SkillTree.ActivateSkill("Sprint")
3. S_SkillTree looks up skill in unlockedMap
4. Calls skill.Activate(player)
```

---

## 3. Script Details

### 3.1 S_SkillBase.cs (Abstract ScriptableObject)

**Base class for all skills.** Create new skills by extending this class.

**Serialized Fields**:
| Field | Type | Description |
|-------|------|-------------|
| skillName | string | Unique name identifier (used for lookup) |
| description | string | Human-readable description |
| icon | Sprite | UI icon sprite |
| requiredPoints | int | Skill points to unlock |
| prerequisites | S_SkillBase[] | Array of prerequisite skills that must be unlocked first |
| availableSolid | bool | Whether usable in solid form |
| availableFluid | bool | Whether usable in fluid form |
| minEnergyToStart | float | Minimum shared player energy required before a skill can begin |
| energyDrainPerSecond | float | Shared player energy drained while the skill remains active |

**Methods**:
| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `CanUnlock()` | none | bool | Checks all prerequisites are unlocked |
| `Activate(S_Player)` | S_Player | void | **Abstract** 閳?implement the skill effect in subclasses |
| `OnUnlocked(S_Player)` | S_Player | void | **Virtual** 閳?optional hook called once when skill is unlocked |

**Creating a New Skill**:
1. Create a new C# script extending `S_SkillBase`
2. Add `[CreateAssetMenu]` attribute
3. Implement `Activate(S_Player player)` for the skill effect
4. Optionally override `OnUnlocked(S_Player player)` for unlock effects
5. Create asset via Assets > Create > InkForm > Skills
6. Drag the asset into `S_SkillTree.allSkills[]`
7. Call `TryUnlock("SkillName")` to unlock
8. Call `ActivateSkill("SkillName")` to trigger

### 3.2 S_SkillTree.cs (Singleton Manager)

**Type**: MonoBehaviour (Singleton, persistent child of `ManagerRoot.prefab`)

**Initialization**: On first `Awake()`, grants 5 skill points and auto-unlocks Sprint, FluidClimb, and CameraControl. Uses an `initialized` flag to prevent double initialization across scene loads.

**Key Methods**:
| Method | Parameters | Description |
|--------|------------|-------------|
| `AddSkillPoints(int)` | int count | Add points to the pool |
| `TryUnlock(string)` | string skillName | Attempt to unlock a skill by name. Checks prerequisites, deducts points |
| `ActivateSkill(string)` | string skillName | Activate an unlocked skill. Calls `skill.Activate(player)` |
| `IsUnlocked(string)` | string skillName 閳?bool | Check if a skill is unlocked |
| `GetSkillPoints()` | none 閳?int | Get current point count |

**Internal State**:
| Field | Type | Description |
|-------|------|-------------|
| allSkills | S_SkillBase[] | All registered skill assets (assigned in Inspector) |
| unlockedMap | Dictionary<string, S_SkillBase> | Skills that have been unlocked |
| skillPoints | int | Current skill point pool |

---

### 3.3 S_Soild_sprint.cs (Sprint Skill)

**Type**: ScriptableObject (extends S_SkillBase)

**Configuration**:
| Field | Default | Description |
|-------|---------|-------------|
| sprintSpeed | 20f | Impulse force magnitude |
| quickTapEnergyCost | 12f | Extra shared energy cost for quick-tap sprint |
| cooldown | 1.0s | Legacy instant `Activate()` fallback cooldown; energy is the main active-use limiter |
| SprintLockTime | 0.1s | Duration locked in solid form during sprint |

**Activation Flow**:
```
Activate(S_Player player)
    |-- Check shared player energy
    |-- Check cooldown for legacy fallback activation
    |-- Check form availability (availableSolid / availableFluid)
    |-- Calculate impulse direction based on facing
    |-- Apply impulse: player.GetRigidbody().AddForce(direction * sprintSpeed, ForceMode2D.Impulse)
    |-- Set sprint momentum flag: player.SetSprintMomentum(true)
    |-- Start SprintLock coroutine:
    |   |-- Lock player in solid form for SprintLockTime
    |   |-- After lock expires, restore original form
    |   `-- Reset sprint momentum flag
```

**SprintLock Behavior**:
- During sprint, the player is temporarily locked in solid form
- This prevents form switching mid-sprint (which could break physics)
- After SprintLockTime expires, the original form is restored
- SprintLock is implemented as a coroutine in S_Player

**Setup**:
1. Create Sprint ScriptableObject asset (Assets > Create > InkForm > Skills)
2. Set `skillName` = "Sprint"
3. Set `requiredPoints` = 0 (free to unlock at start)
4. Set `availableSolid` = true, `availableFluid` = false
5. Add to `S_SkillTree.allSkills[]`

---

### 3.4 S_fluid_climb.cs (Wall Climb Skill)

**Type**: ScriptableObject (extends S_SkillBase)

**Configuration**:
| Field | Default | Description |
|-------|---------|-------------|
| stickyForce | 3f | Force pulling player toward surface |
| climbSpeed | 3f | Movement speed along surfaces |
| fluidGravityScale | 4f | Gravity when not attached to surface |
| activeTime | 4.0s | Legacy max climb duration; shared energy drain is the primary limiter |
| surfaceLayer | ~0 | Layer mask for climbable surfaces |
| floorDotThreshold | 0.5 | Dot product threshold for floor detection |
| ceilingDotThreshold | 0.5 | Dot product threshold for ceiling detection |

**Surface State Machine**:
```
None --(contact floor)--> Floor
Floor --(push toward wall)--> WallLeft / WallRight
WallLeft/WallRight --(contact ceiling)--> Ceiling
Any --(exhaustion / no contact)--> None
```

**State Descriptions**:
| State | Description |
|-------|-------------|
| None | Not attached to any surface. Gravity = fluidGravityScale, 60% air control |
| Floor | Standing on a surface. Can move horizontally, push toward wall to transition |
| WallLeft | Attached to left wall. Climb up/down with vertical input |
| WallRight | Attached to right wall. Climb up/down with vertical input |
| Ceiling | Attached to ceiling. Move horizontally along ceiling |

**Physics When Attached**:
- Gravity = 0 (sticky force holds player to surface)
- Movement driven by input along the surface normal
- `stickyForce` applies continuous pull toward the surface

**Physics When Free-Floating**:
- Gravity = fluidGravityScale
- 60% air control (reduced horizontal input)
- Can re-attach to surfaces on contact

**Setup**:
1. Create FluidClimb ScriptableObject asset (Assets > Create > InkForm > Skills)
2. Set `skillName` = "FluidClimb"
3. Set `requiredPoints` = 0 (free to unlock at start)
4. Set `availableSolid` = false, `availableFluid` = true
5. Add to `S_SkillTree.allSkills[]`
6. In S_Player, assign to `fluidClimbSkill` field

---

## 4. Shared Player Energy (v0.8.1)

`S_PlayerEnergy` is a player component shared by Sprint, FluidClimb, and CameraControl. It starts full, regenerates after `regenDelay`, and broadcasts `S_GameEvent.PlayerEnergyChanged(current, max)` whenever the value changes.

### 4.1 S_PlayerEnergy API

| Member | Description |
|--------|-------------|
| `CurrentEnergy` | Current energy value |
| `MaxEnergy` | Maximum energy value |
| `NormalizedEnergy` | Current/max normalized value |
| `CanStartSkill(S_SkillBase)` | Checks `skill.MinEnergyToStart` |
| `TryConsumeSkillEnergy(S_SkillBase, float)` | Drains `skill.EnergyDrainPerSecond * deltaTime` |
| `TrySpendAmount(float)` | One-shot energy spend used by sprint quick tap |
| `ResetEnergy()` | Restores full energy and broadcasts UI update |

### 4.2 Skill Asset Energy Fields

Every `S_SkillBase` asset exposes:

| Field | Description |
|-------|-------------|
| `minEnergyToStart` | Minimum player energy required before activation |
| `energyDrainPerSecond` | Energy drain while active |

`S_Soild_sprint` also exposes `quickTapEnergyCost` for short press sprint release.

### 4.3 Runtime Behavior

```
Skill input starts
    -> S_PlayerEnergy.CanStartSkill(skill)
    -> active skill drains shared energy over time
    -> if energy reaches zero, active skill stops
Skill input stops
    -> regeneration delay starts
    -> energy recovers over time
```

---

## 5. Creating a New Skill

### Step-by-Step

1. **Create the script**:
   ```csharp
   [CreateAssetMenu(menuName = "InkForm/Skills/YourSkillName")]
   public class S_YourSkill : S_SkillBase
   {
       [Header("Skill Settings")]
       public float yourParam = 1f;

       public override void Activate(S_Player player)
       {
           // Implement skill effect here
       }

       // Optional: override OnUnlocked for unlock effects
       public override void OnUnlocked(S_Player player)
       {
           Debug.Log($"Skill {skillName} unlocked!");
       }
   }
   ```

2. **Create the ScriptableObject asset**: Right-click in Project > Create > InkForm > Skills > YourSkillName

3. **Configure the asset**:
   - Set `skillName` to a unique string
   - Set `description` for UI display
   - Set `requiredPoints` cost
   - Set `availableSolid` / `availableFluid`
   - Set any prerequisites if needed

4. **Register in S_SkillTree**: Drag the asset into `allSkills[]`

5. **Trigger activation**: Call `S_SkillTree.ActivateSkill("YourSkillName")` from input or game logic

---

## 6. Unity Setup

### S_SkillTree
1. Create a GameObject named "SkillTree"
2. Add `S_SkillTree` component
3. The object is a direct child of `ManagerRoot.prefab`
4. Drag all skill ScriptableObject assets into `allSkills[]`

### S_Player Integration
1. In the Player Inspector, assign the `fluidClimbSkill` reference
2. Sprint is triggered from `S_Player.Jump()` when sprint input is detected
3. Wall climb is triggered from `S_Player.FluidMovement()` when grip input is active

---

## 7. Common Issues

| Issue | Solution |
|-------|----------|
| Skill not activating | Check `IsUnlocked()` returns true and correct form availability |
| Sprint feels wrong | Verify SprintLockTime and sprintSpeed values |
| Wall climb not sticky enough | Increase stickyForce |
| Energy drains too fast | Lower the skill asset `energyDrainPerSecond` or increase `S_PlayerEnergy.regenPerSecond` |
| SkillTree missing | Confirm the full `ManagerRoot.prefab` is present in the scene |
| Energy bar not changing | Confirm player has `S_PlayerEnergy` and skill assets have nonzero energy costs |
| Prerequisites not working | Ensure prerequisite skill names match exactly |
| Skill asset not found | Check `skillName` string matches what's passed to TryUnlock/ActivateSkill |

---

## 8. Fluid Climb Updates

`S_fluid_climb` now supports a Grip buffer for fluid climbing. When the player is in fluid form and holding Grip, the skill can cast the player's collider horizontally within `gripBufferDistance`. If a valid wall is found, the player is snapped toward the wall and the state machine enters `WallLeft` or `WallRight` without requiring perfect contact first.

Additional serialized fields:

| Field | Description |
|-------|-------------|
| gripBufferDistance | Maximum distance from a wall where Grip can still attach |
| gripSnapSkin | Small safe distance kept when snapping toward the wall |
| gripInputThreshold | Horizontal input threshold used to choose left/right grip direction |
| drawGripBufferGizmos | Shows left/right buffer regions when the player is selected |
| gripBufferGizmoColor | Gizmo color for buffer visualization |

The surface state machine also allows direct `None/Floor -> Ceiling` entry while Grip movement is active. This lets the player attach to ceilings directly instead of requiring a wall-to-ceiling transition first.

---

## 9. Sprint Charge System

The sprint skill (`S_Soild_sprint`) now supports a hold-to-charge sprint mechanism. The player holds the sprint key to accumulate charge, and releases to dash.

### 8.1 New Parameters (Sprint Charge section)

| Parameter | Default | Description |
|-----------|---------|-------------|
| maxChargeTime | 2f | Maximum charge duration for full sprint speed |
| maxSprintSpeed | 200f | Maximum sprint impulse at full charge |
| minSprintSpeed | 20f | Minimum sprint impulse (quick-tap) |
| stage1Scale | 1.0 | Visual/collider scale during stage 1 |
| stage2Scale | 1.3 | Visual/collider scale during stage 2 |
| stage3Scale | 1.6 | Visual/collider scale during stage 3 |
| stage2Time | 0.5s | Time threshold to enter stage 2 |
| stage3Time | 1.2s | Time threshold to enter stage 3 |
| shakeFrequency | 25 | Stage transition shake frequency |
| shakeAmplitude | 0.15 | Stage transition shake amplitude |
| shakeDecay | 5 | Shake exponential decay rate |
| stage1Cooldown | 0.1s | Cooldown after stage 1 release |
| stage2Cooldown | 0.5s | Cooldown after stage 2 release |
| stage3Cooldown | 1.0s | Cooldown after stage 3 release |
| bufferTime | 0.15s | Quick-tap buffer threshold |
| chargeBallMaterial | - | Low-friction PhysicsMaterial2D for rolling |
| chargeStartClip | - | SFX played when charge begins (after buffer exits) |
| chargeStageClip | - | SFX played when entering a new charge stage |

### 8.2 Original Sprint Parameters (preserved)

| Parameter | Default | Description |
|-----------|---------|-------------|
| sprintSpeed | 20f | Original instant-sprint impulse (used by legacy `Activate()` fallback) |
| cooldown | 1.0s | Cooldown between instant `Activate()` calls |
| SprintLockTime | 0.1s | Duration player is locked in solid form during sprint |
| stunRadius | 2f | OverlapCircle radius for detecting enemies on sprint hit |
| enemyLayer | ~0 | LayerMask filtering which colliders count as enemies |

### 8.3 New Methods

| Method | Description |
|--------|-------------|
| `GetStage(float timer)` | Returns stage index (0, 1, 2) based on charge time |
| `GetStageScale(float timer)` | Returns scale multiplier for current stage |
| `GetCooldown(int stage)` | Returns cooldown duration for given stage |
| `GetShakeOffset(float shakeTimer)` | Returns damped sine shake offset for stage transitions |
| `ActivateCharge(player, speed, direction)` | Performs charged sprint with stun hit detection |

### 8.4 Buffer System

Quick-tap sprint (press and release within `bufferTime`) bypasses all visual/physics changes and immediately performs a `minSprintSpeed` dash. This ensures responsive instant-dash for skilled players.

### 8.5 S_SkillTree Integration

`S_SkillTree.GetSprintSkill()` returns the `S_Soild_sprint` instance so `S_Player` can access charge parameters without holding a direct reference.

---

## 10. Camera Control Skill

The camera control skill (`S_CameraControlSkill`) allows the player to enter a bullet-time state and manually pan the camera to survey the level.

### 9.1 Behavior

```
Activate(S_Player player)
    |-- Calls player.BeginCameraControl(this)
    |-- S_Player enters camera control mode:
    |   |-- Time.timeScale *= bulletTimeScale (default 0.2x)
    |   |-- S_CameraMove.BeginManualControl()
    |   `-- Player movement continues at reduced time scale
    |
Player holds CameraControl input
    |-- Move input (WASD) feeds to S_CameraMove.ManualControlTick()
    |-- Camera pans around player (clamped to manualMaxDistanceFromTarget)
    `-- Uses Time.unscaledDeltaTime so panning is smooth during bullet time
    |
Player releases CameraControl input
    |-- Time.timeScale restored to original
    |-- S_CameraMove.EndManualControl()
    `-- Camera smoothly returns to follow mode
```

### 9.2 Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `bulletTimeScale` | 0.2f | Time scale multiplier (0.01閳?.0). Lower = slower time |

### 9.3 Form Availability

- Available in both solid and fluid forms (`availableSolid = true`, `availableFluid = true`)
- Set automatically in `OnEnable()`

### 9.4 Blocking Conditions

Camera control is blocked when:
- Player is already in camera control mode
- Player is paralyzed
- Movement is locked
- Sprint is charging
- Time scale is 0 (paused)

### 9.5 S_SkillTree Integration

`S_SkillTree.GetCameraControlSkill()` returns the `S_CameraControlSkill` instance so `S_PlayerSkillController` can access camera control parameters.

---

## 11. S_PlayerSkillController (v0.8.0)

`S_PlayerSkillController` is a MonoBehaviour created and injected by `S_Player` during `Awake()`. It owns the sprint charge state machine and camera control logic, previously embedded directly in `S_Player`.

### 10.1 Initialization

```csharp
// S_Player creates S_PlayerSkillController in Awake()
skillController = gameObject.AddComponent<S_PlayerSkillController>();
skillController.Initialize(this, moveAction, sprintAction, cameraControlAction,
    cameraController, proceduralRenderer, dynamicCollider, body, bodyRigidbody,
    solidGravityScale, useDynamicCollider);
```

### 10.2 Key Methods

| Method | Description |
|--------|-------------|
| `BeginSprintCharge()` | Start sprint charge (buffer 閳?charge 閳?release flow) |
| `FixedTickSprintCharge()` | Per-frame charge update (stage progression, visuals, shake) |
| `ReleaseSprintCharge()` | Release charge 閳?apply sprint impulse + cooldown |
| `CancelSprintCharge()` | Cancel active sprint charge and restore visuals |
| `HandleCameraControlInput()` | Detect CameraControl input and activate/deactivate |
| `CameraControlTick()` | Feed move input to camera during manual control |
| `BeginCameraControl(skill)` | Enter bullet-time + manual camera mode |
| `EndCameraControl()` | Restore time scale and camera state |
| `TickCooldown()` | Decrease sprint cooldown timer |

### 10.3 Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsSprintCharging` | bool | Whether sprint charge is active |
| `IsCameraControlActive` | bool | Whether camera control is active |

### 10.4 Integration

- `S_Player.Update()` 閳?calls `skillController.HandleCameraControlInput()`, `skillController.TickCooldown()`
- `S_Player.FixedUpdate()` 閳?calls `skillController.FixedTickSprintCharge()`
- `S_Player.BeginSprintCharge()` 閳?delegates to `skillController.BeginSprintCharge()`
- `S_Player.ReleaseSprintCharge()` 閳?delegates to `skillController.ReleaseSprintCharge()`
- Sprint/Camera skill parameters are fetched from `S_SkillTree` at runtime
