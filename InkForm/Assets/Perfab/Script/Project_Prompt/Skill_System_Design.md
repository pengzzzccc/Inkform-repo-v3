# Skill System — Design Document

## 1. Overview

The skill system allows players to unlock and activate abilities using skill points. It uses ScriptableObject-based skill definitions for data-driven design, with a centralized `S_SkillTree` manager for unlocking and activation.

### Supported Skills
- **Sprint** (`S_Soild_sprint`) — Burst of forward momentum with cooldown
- **Fluid Climb** (`S_fluid_climb`) — Wall/ceiling climbing in fluid form

---

## 2. Architecture

### 2.1 Class Hierarchy

```
S_SkillBase (ScriptableObject, abstract)
|-- S_Soild_sprint (concrete: sprint skill)
|-- S_fluid_climb  (concrete: wall climb skill)
`-- (future skills extend this base)

S_SkillTree (MonoBehaviour, Singleton)
|-- S_SkillBase[] allSkills (all registered skill assets)
|-- Dictionary<string, S_SkillBase> unlockedMap
`-- int skillPoints
```

### 2.2 Why ScriptableObjects?

ScriptableObjects are used because:
- **Data-driven**: Skill parameters (speed, cooldown, force) are defined in assets, not hardcoded
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

**Methods**:
| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `CanUnlock()` | none | bool | Checks all prerequisites are unlocked |
| `Activate(S_Player)` | S_Player | void | **Abstract** — implement the skill effect in subclasses |
| `OnUnlocked(S_Player)` | S_Player | void | **Virtual** — optional hook called once when skill is unlocked |

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

**Type**: MonoBehaviour (Singleton, DontDestroyOnLoad)

**Initialization**: On first `Awake()`, grants 5 skill points and auto-unlocks Sprint and FluidClimb. Uses an `initialized` flag to prevent double initialization across scene loads.

**Key Methods**:
| Method | Parameters | Description |
|--------|------------|-------------|
| `AddSkillPoints(int)` | int count | Add points to the pool |
| `TryUnlock(string)` | string skillName | Attempt to unlock a skill by name. Checks prerequisites, deducts points |
| `ActivateSkill(string)` | string skillName | Activate an unlocked skill. Calls `skill.Activate(player)` |
| `IsUnlocked(string)` | string skillName → bool | Check if a skill is unlocked |
| `GetSkillPoints()` | none → int | Get current point count |

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
| cooldown | 1.0s | Time between activations |
| SprintLockTime | 0.1s | Duration locked in solid form during sprint |

**Activation Flow**:
```
Activate(S_Player player)
    |-- Check cooldown (if cooldown active, return)
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
| activeTime | 4.0s | Max climb duration before exhaustion |
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

## 4. Creating a New Skill

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

## 5. Unity Setup

### S_SkillTree
1. Create a GameObject named "SkillTree"
2. Add `S_SkillTree` component
3. The object uses `DontDestroyOnLoad` — create it once in the initial scene
4. Drag all skill ScriptableObject assets into `allSkills[]`

### S_Player Integration
1. In the Player Inspector, assign the `fluidClimbSkill` reference
2. Sprint is triggered from `S_Player.Jump()` when sprint input is detected
3. Wall climb is triggered from `S_Player.FluidMovement()` when grip input is active

---

## 6. Common Issues

| Issue | Solution |
|-------|----------|
| Skill not activating | Check `IsUnlocked()` returns true and correct form availability |
| Sprint feels wrong | Verify SprintLockTime and sprintSpeed values |
| Wall climb not sticky enough | Increase stickyForce |
| Exhaustion too fast | Increase activeTime |
| Skills reset on scene load | S_SkillTree uses DontDestroyOnLoad + initialized flag |
| Prerequisites not working | Ensure prerequisite skill names match exactly |
| Skill asset not found | Check `skillName` string matches what's passed to TryUnlock/ActivateSkill |

---

## 7. Fluid Climb Updates

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
