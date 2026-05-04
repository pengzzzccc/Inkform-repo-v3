# Level Section System — Architecture & Implementation Plan

## 1. Overview

The Level Section System manages a level composed of multiple vertical moving platform sections. Each section is a self-contained Prefab that can be independently edited and reused across levels. Sections are revealed (descend) and hidden (ascend) sequentially as the player progresses through gameplay challenges.

### Core Concept

```
Section 1 (Jump)       Section 2 (Sprint+Jump)    Section 3 (Jump+Sprint+Climb)
[Platform A][B]        [Platform C][D]            [Platform E][F][Wall]

Initial State:  ALL sections hidden at top

Player enters Section 1 StartTrigger -> Section 1 descends
Player enters Section 1 EndTrigger   -> Section 1 rises + Section 2 descends simultaneously
Player enters Section 2 EndTrigger   -> Section 2 rises + Section 3 descends simultaneously
```

---

## 2. Game Mechanics Reference

| Mechanic | Script | Trigger |
|----------|--------|---------|
| Jump | `S_Player.Jump()` | Jump input, cooldown + max jump count |
| Double Jump | `S_Player.Jump()` | Automatic when MaxJump >= 2 |
| Sprint | `S_Soild_sprint.Activate()` | Sprint input, cooldown, applies Impulse force |
| Wall Climb | `S_fluid_climb.FluidMovementTick()` | Grip input + fluid form + surface contact |

---

## 3. Architecture

### 3.1 Scene Hierarchy

```
LevelSectionController (attached to scene root)
|-- Section_Jump (Prefab Instance)
|   |-- Platform_A  (S_MovingPlatform)
|   |-- Platform_B  (S_MovingPlatform)
|   |-- StartTrigger (S_SectionGoal, type=Start)
|   `-- EndTrigger   (S_SectionGoal, type=End)
|-- Section_Sprint (Prefab Instance)
|   |-- Platform_C  (S_MovingPlatform)
|   |-- Platform_D  (S_MovingPlatform)
|   |-- StartTrigger (S_SectionGoal, type=Start)
|   `-- EndTrigger   (S_SectionGoal, type=End)
`-- Section_Climb (Prefab Instance)
    |-- Platform_E  (S_MovingPlatform)
    |-- Platform_F  (S_MovingPlatform)
    |-- Wall        (climbable surface)
    |-- StartTrigger (S_SectionGoal, type=Start)
    `-- EndTrigger   (S_SectionGoal, type=End)
```

### 3.2 Prefab Structure

Each Section Prefab is a self-contained GameObject:

```
Section_Jump (Prefab)
|-- [S_LevelSection] component
|   |-- sectionIndex = 0
|   |-- sectionTopPoint -> SectionTop (child marker)
|   `-- sectionBottomPoint -> SectionBottom (child marker)
|-- SectionTop       (empty GameObject, top position marker)
|-- SectionBottom    (empty GameObject, bottom/game-area position marker)
|-- Platform_A
|   `-- [S_MovingPlatform] (optional independent movement)
|-- Platform_B
|   `-- [S_MovingPlatform]
|-- StartTrigger
|   |-- [S_SectionGoal] -> sectionIndex = 0, triggerType = Start
|   `-- BoxCollider2D (isTrigger)
`-- EndTrigger
    |-- [S_SectionGoal] -> sectionIndex = 0, triggerType = End
    `-- BoxCollider2D (isTrigger)
```

### 3.3 Script Inventory

| Script | Type | Description |
|--------|------|-------------|
| `S_MovingPlatform.cs` | Unchanged | Independent platform with Reveal/Hide, states, audio, claw animation, delta transfer |
| `S_LevelSection.cs` | Refactored | Section Prefab root. Handles section-level movement (descend/ascend as a whole) |
| `S_SectionGoal.cs` | Refactored | Trigger with Start/End type. Fires SectionStart or SectionEnd event |
| `S_LevelSectionController.cs` | Refactored | Scene-level controller. Manages section sequence via Start/End events |
| `S_GameEvent.cs` | Refactored | Replaced OnSectionCompleted with OnSectionStart + OnSectionEnd |

### 3.4 Data Flow

```
Player enters Section 0 StartTrigger
    |
S_SectionGoal.OnTriggerEnter2D()  (triggerType = Start)
    |
S_GameEvent.SectionStart(0)
    |
S_LevelSectionController.HandleSectionStart(0)
    |
sections[0].RevealSection()   <- Section 0 root moves down from top to game area

Player completes Section 0, enters Section 0 EndTrigger
    |
S_SectionGoal.OnTriggerEnter2D()  (triggerType = End)
    |
S_GameEvent.SectionEnd(0)
    |
S_LevelSectionController.HandleSectionEnd(0)
    |
sections[0].HideSection()     <- Section 0 root moves up back to top
sections[0].MarkCompleted()
sections[1].RevealSection()   <- Section 1 root moves down to game area (simultaneously)
```

---

## 4. Script Details

### 4.1 S_LevelSection — Section-Level Movement (New)

S_LevelSection now handles the entire section's vertical movement as a Transform move:

```csharp
[Header("Section Movement")]
[SerializeField] private Transform sectionTopPoint;    // top position marker (child)
[SerializeField] private Transform sectionBottomPoint;  // bottom/game-area position marker (child)
[SerializeField] private float sectionMoveSpeed = 3f;
```

Key behavior:
- `Start()`: Snapshots sectionTopPoint and sectionBottomPoint **Y coordinates only**. X/Z are kept from the section root's own position. Starts at topWorldPos.
- `RevealSection()`: Moves entire section root **vertically only** from topWorldPos to bottomWorldPos (descend)
- `HideSection()`: Moves entire section root **vertically only** back to topWorldPos (ascend)
- Uses `Mathf.MoveTowards` on Y axis only — section never drifts horizontally
- Child objects (platforms, triggers) automatically follow the root Transform
- `isMoving` flag tracks active movement state

### 4.2 PlatformState Enum (S_MovingPlatform, unchanged)

```csharp
public enum PlatformState
{
    HiddenAtTop,      // Platform at top, inactive
    Descending,       // Platform moving down (being revealed)
    VisibleAtBottom,  // Platform at bottom, active
    Ascending         // Platform moving up (being hidden)
}
```

### 4.3 S_MovingPlatform — Unchanged

S_MovingPlatform retains all its original functionality:
- Independent Reveal/Hide state machine
- Audio system (motor start/loop/stop, landing, claw sounds)
- Claw animation
- Player delta transfer (no SetParent)
- Coordinate snapshot for top/bottom points

When used inside a Section, the platform's own movement is optional — the section moves as a whole. When used standalone, the platform works exactly as before.

### 4.3.1 Coordinate System (Important)

topPoint and bottomPoint can be child GameObjects of the MovingPlatform. At Start(), their world positions are snapshotted into `topWorldPos` and `bottomWorldPos`. All movement uses these fixed world coordinates — not the live Transform positions.

```
Start():
  topWorldPos = topPoint.position    // snapshot once
  bottomWorldPos = bottomPoint.position
  transform.position = bottomWorldPos

Update():
  MoveTowards(transform.position, topWorldPos/bottomWorldPos, ...)
```

### 4.3.2 Player Delta Transfer (Important)

The platform does NOT use `SetParent` to carry the player. Instead, it tracks position delta each frame and applies it to the player's Rigidbody2D:

```
Update():
  if (playerOnPlatform)
      playerRb.position += (currentPos - lastPos)
  lastPos = currentPos
```

This approach correctly handles both the platform's own movement AND the parent section's movement, since `transform.position` includes parent delta.

### 4.4 S_SectionGoal — Start/End Trigger

```csharp
public enum SectionTriggerType { Start, End }

public class S_SectionGoal : MonoBehaviour
{
    [SerializeField] private int sectionIndex = 0;
    [SerializeField] private SectionTriggerType triggerType = SectionTriggerType.Start;

    private Vector3 fixedWorldPos;

    void Start()
    {
        fixedWorldPos = transform.position;
    }

    void LateUpdate()
    {
        if (transform.position != fixedWorldPos)
            transform.position = fixedWorldPos;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (triggerType == SectionTriggerType.Start)
            S_GameEvent.SectionStart(sectionIndex);
        else
            S_GameEvent.SectionEnd(sectionIndex);
    }
}
```

Each section has two triggers:
- **StartTrigger**: Placed at section entrance. Fires `SectionStart` when player enters.
- **EndTrigger**: Placed at section exit. Fires `SectionEnd` when player enters.

### 4.4.1 World Position Anchor (Important)

S_SectionGoal is a child of the Section Prefab, but triggers must remain at fixed world positions — they should NOT move with the section when it ascends/descends. This is achieved by:
1. `Start()`: Snapshots the initial world position into `fixedWorldPos`
2. `LateUpdate()`: Forces the Transform back to `fixedWorldPos` if it has moved (due to parent movement)

This pattern ensures triggers are part of the Prefab hierarchy (convenient for editing) but behave as fixed world-space objects at runtime.

### 4.5 S_LevelSectionController

- Scene-level controller
- Holds S_LevelSection[] sections array (assigned in Inspector)
- `Start()`: All sections hidden (at top). Player walks to trigger the first section.
- Listens to `S_GameEvent.OnSectionStart` and `S_GameEvent.OnSectionEnd`
- `HandleSectionStart(index)`: Validates index matches currentSectionIndex, then reveals section
- `HandleSectionEnd(index)`: Validates index, hides current section, marks completed, advances index, reveals next section

### 4.6 S_GameEvent Extensions

```csharp
public static event Action<int> OnSectionStart;
public static event Action<int> OnSectionEnd;

public static void SectionStart(int index) => OnSectionStart?.Invoke(index);
public static void SectionEnd(int index) => OnSectionEnd?.Invoke(index);
```

---

## 5. Inspector Configuration

### S_LevelSectionController
- Sections: S_LevelSection[] (drag Prefab instances in order)

### S_LevelSection (on Section Prefab)
- Section Index: int (0, 1, 2...)
- Section Top Point: Transform (SectionTop child marker)
- Section Bottom Point: Transform (SectionBottom child marker)
- Section Move Speed: float (default 3)

### S_MovingPlatform (on each platform)
- Movement Settings: topPoint, bottomPoint, moveSpeed
- Claw Settings: clawAnimator, clawDelay, clawTransitionTime
- Audio: motorSource, sfxSource, all AudioClips
- Debug: currentState (read-only in play mode)

### S_SectionGoal
- Section Index: int
- Trigger Type: SectionTriggerType (Start or End)
- BoxCollider2D (isTrigger = true)

---

## 6. Detailed Usage Guide

### 6.1 Creating a Section Prefab (Step-by-Step)

#### Step 1: Create the section root
1. In Hierarchy, right-click → **Create Empty** → name it `Section_01`
2. Add component: `S_LevelSection`
3. In Inspector, set **Section Index** = `0`

#### Step 2: Create position markers
1. Right-click on `Section_01` → **Create Empty** → name it `SectionTop`
2. Move it to the desired **top position** (where the section hides, above the camera)
3. Right-click on `Section_01` → **Create Empty** → name it `SectionBottom`
4. Move it to the desired **bottom position** (where the section appears in the game area)
5. Select `Section_01`, drag `SectionTop` → **Section Top Point** field
6. Drag `SectionBottom` → **Section Bottom Point** field
7. Set **Section Move Speed** (default 3, adjust as needed)

#### Step 3: Add platforms
1. Create or drag platform Prefabs as children of `Section_01`
2. Each platform has `S_MovingPlatform` component (optional — leave top/bottom unassigned if section moves as a whole)
3. Position platforms relative to `SectionBottom` — this is where they'll appear when the section descends

#### Step 4: Create StartTrigger
1. Right-click on `Section_01` → **Create Empty** → name it `StartTrigger`
2. Position it at the **entrance** of the section (where the player walks in)
3. Add component: `S_SectionGoal`
4. In Inspector:
   - **Section Index** = `0` (same as parent section)
   - **Trigger Type** = `Start`
5. Add component: `BoxCollider2D`
   - Check **Is Trigger** = `true`
   - Adjust Size to cover the trigger area

#### Step 5: Create EndTrigger
1. Right-click on `Section_01` → **Create Empty** → name it `EndTrigger`
2. Position it at the **exit** of the section (where the player completes the challenge)
3. Add component: `S_SectionGoal`
4. In Inspector:
   - **Section Index** = `0` (same as parent section)
   - **Trigger Type** = `End`
5. Add component: `BoxCollider2D`
   - Check **Is Trigger** = `true`
   - Adjust Size to cover the trigger area

#### Step 6: Save as Prefab
1. Drag the entire `Section_01` GameObject from Hierarchy to `Assets/Perfab/` in Project
2. This creates a Prefab you can reuse across levels

### 6.2 Setting Up a Level (Step-by-Step)

#### Step 1: Add the controller
1. In your scene Hierarchy, right-click → **Create Empty** → name it `LevelController`
2. Add component: `S_LevelSectionController`

#### Step 2: Place section Prefabs
1. Drag Section Prefabs from Project into the scene Hierarchy
2. Each section should be a direct child of the scene (or under a common parent)
3. Position each section — the SectionTop/SectionBottom markers define their vertical range

#### Step 3: Configure the controller
1. Select `LevelController`
2. In Inspector, expand **Sections** array
3. Set **Size** = number of sections
4. Drag each section Prefab instance into the array slots **in order** (Section_01 → [0], Section_02 → [1], ...)

#### Step 4: Play test
1. Press Play → All sections start hidden at top
2. Walk player to Section_01's StartTrigger → Section_01 descends
3. Complete the challenge → Walk to EndTrigger → Section_01 rises + Section_02 descends
4. Continue through all sections

### 6.3 S_LevelSection Inspector Reference

| Field | Type | Description |
|-------|------|-------------|
| Section Index | int | Unique index for this section (0, 1, 2...) |
| Section Top Point | Transform | Child marker defining the top (hidden) position |
| Section Bottom Point | Transform | Child marker defining the bottom (visible) position |
| Section Move Speed | float | Speed of section movement (units/second, default 3) |

### 6.4 S_SectionGoal Inspector Reference

| Field | Type | Description |
|-------|------|-------------|
| Section Index | int | Must match parent S_LevelSection's index |
| Trigger Type | enum | `Start` or `End` |

**Important**: BoxCollider2D must have **Is Trigger** checked.

### 6.5 S_LevelSectionController Inspector Reference

| Field | Type | Description |
|-------|------|-------------|
| Sections | S_LevelSection[] | Array of section Prefab instances in play order |

### 6.6 Common Configuration Patterns

#### Pattern A: Section with only section-level movement (no individual platform movement)
- S_MovingPlatform: Leave topPoint/bottomPoint **unassigned**
- The platform will stay fixed relative to the section root
- Section root moves everything together

#### Pattern B: Section with both section-level AND individual platform movement
- S_MovingPlatform: Assign topPoint/bottomPoint **relative to the section root**
- When section descends, platforms move with it
- After section lands, individual platforms can still move on their own

#### Pattern C: Standalone S_MovingPlatform (no section)
- Use S_MovingPlatform independently without S_LevelSection
- Assign topPoint/bottomPoint as usual
- Platform works exactly as before (Reveal/Hide API)

### 6.7 Reusability

Sections can be reused across different levels:
- Level 1: Section_Jump + Section_Sprint + Section_Climb
- Level 2: Section_Jump + Section_DoubleJump + Section_Sprint_Wall

To reuse: drag the Prefab into a new scene, position it, and add to the new LevelSectionController's sections[] array.

---

## 7. Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| Player falls off when section moves | Delta transfer handles parent movement automatically via transform.position |
| Section doesn't descend | Check sectionTopPoint/sectionBottomPoint are assigned in Inspector |
| Section not triggering | Verify sectionIndex matches order in Controller sections[] |
| Trigger not firing | Ensure BoxCollider2D **Is Trigger** = true and player tag is "Player" |
| Two triggers mixed up | Check S_SectionGoal triggerType is set correctly (Start vs End) |
| Platform jitter | MoveTowards in Update() + Rigidbody2D interpolation |
| Section moves but platforms don't follow | Ensure platforms are children of section root in hierarchy |
| Section moves too fast/slow | Adjust Section Move Speed on S_LevelSection |
| Player slides off platform during section movement | This is normal — delta transfer is frame-based, ensure Rigidbody2D interpolation is enabled |
| EndTrigger fires before player completes challenge | Reposition EndTrigger further along the challenge path |
| Multiple sections activate at once | Ensure each section has a unique sectionIndex, and triggers have correct indices |
