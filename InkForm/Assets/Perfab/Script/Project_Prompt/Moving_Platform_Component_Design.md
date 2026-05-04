# Moving Platform Component — Design Document

## 1. Overview

The Moving Platform component (`S_MovingPlatform`) is a reusable vertical moving platform with claw animation and audio support. It can be used standalone or inside a Level Section. The platform moves between top and bottom positions, with claw open/close animation at the bottom.

### Usage Modes

| Mode | Description | Setup |
|------|-------------|-------|
| Standalone | Platform moves independently | Assign topPoint/bottomPoint on the platform itself |
| Section Child | Platform moves with section root | Leave topPoint/bottomPoint unassigned; section moves the whole group |

---

## 2. Visual Structure

### 2.1 Core Elements

```
[Top Rail / Track]
       |
    ═══╪═══  ← Slider / Connection Point
       |
    ┌──┴──┐
    │     │  ← Ropes / Chains (x2)
    │     │
    └──┬──┘
    ┌──┴──┐
    │ PLT │  ← Moving Platform Body
    │     │
    └─────┘
```

### 2.2 Claw Structure

```
    ╭─────╮
    │Motor │  ← Top Drive Unit (Mushroom-shaped Shell)
    ╰──┬──╯
       │
    ┌──┴──┐
    │Claws│  ← 3–4 Openable Claw Fingers
    │ ╲ ╱ │
    └──┬──┘
       │
    ╭──┴──╮
    │ Grab │  ← Target Object
    ╰─────╯
```

---

## 3. State Machine

### 3.1 States

```csharp
public enum PlatformState
{
    HiddenAtTop,      // Platform at top, inactive (initial state)
    Descending,       // Platform moving down (being revealed)
    VisibleAtBottom,  // Platform at bottom, active
    Ascending         // Platform moving up (being hidden)
}
```

### 3.2 State Diagram

```
                    ┌──────────────┐
          ┌────────│  HiddenAtTop  │◄────────┐
          │        │  (initial)    │         │
          │        └──────┬───────┘         │
          │               │ Reveal()        │ Reached Top
          │               ▼                 │
          │        ┌──────────────┐         │
          │        │  Descending  │─────────┤
          │        │              │ Reached  │
          │        └──────┬───────┘ Bottom   │
          │               │                 │
          │               ▼                 │
          │        ┌──────────────┐         │
          │        │VisibleAtBottom│         │
          │        │              │         │
          │        └──────┬───────┘         │
          │               │ Hide()          │
          │               ▼                 │
          └────────►┌──────────────│─────────┘
                    │  Ascending   │
                    │              │
                    └──────────────┘
```

### 3.3 State Transitions

| From | To | Trigger | Action |
|------|----|---------|--------|
| HiddenAtTop | Descending | `Reveal()` called | Play motor start sound |
| Descending | VisibleAtBottom | Reach bottomWorldPos | Stop motor, play landing sound, trigger claw open |
| VisibleAtBottom | Ascending | `Hide()` called | Play ascend start sound, play motor loop |
| Ascending | HiddenAtTop | Reach topWorldPos | Stop motor |

---

## 4. Script Details

### 4.1 S_MovingPlatform.cs

**Type**: MonoBehaviour (attach to platform GameObject)

**Serialized Fields**:

#### Movement Settings
| Field | Default | Description |
|-------|---------|-------------|
| topPoint | - | Transform marker for top (hidden) position |
| bottomPoint | - | Transform marker for bottom (visible) position |
| moveSpeed | 2f | Movement speed (units/second) |

#### Claw Settings
| Field | Default | Description |
|-------|---------|-------------|
| clawAnimator | - | Animator component for claw animation |
| clawOpenAngle | 30f | Claw open angle (degrees) |
| clawCloseAngle | 5f | Claw closed angle (degrees) |
| clawTransitionTime | 0.5s | Duration between claw open and close |
| clawDelay | 0.5s | Delay after landing before claw opens |

#### Audio
| Field | Description |
|-------|-------------|
| motorSource | AudioSource for motor loop (continuous sound during movement) |
| sfxSource | AudioSource for one-shot sound effects |
| motorStartClip | Motor startup sound (one-shot, then transitions to loop) |
| motorLoopClip | Continuous motor loop sound |
| motorStopClip | Motor deceleration sound |
| landingClip | Platform landing impact sound |
| clawOpenClip | Mechanical claw opening sound |
| clawCloseClip | Mechanical claw closing sound |
| ascendStartClip | Ascent startup sound |

#### Debug
| Field | Description |
|-------|-------------|
| currentState | Current PlatformState (read-only in play mode) |

**Public API**:
| Method | Description |
|--------|-------------|
| `Reveal()` | Start descending (HiddenAtTop → Descending). No-op if not at top |
| `Hide()` | Start ascending (VisibleAtBottom → Ascending). No-op if not at bottom |
| `IsHidden()` | Returns true if at HiddenAtTop |
| `IsVisible()` | Returns true if at VisibleAtBottom |
| `IsMoving()` | Returns true if Descending or Ascending |
| `GetCurrentState()` | Returns current PlatformState |

### 4.2 Coordinate Snapshot System

At `Start()`, the platform snapshots the world positions of `topPoint` and `bottomPoint`:

```csharp
void Start()
{
    topWorldPos = topPoint != null ? topPoint.position : transform.position;
    bottomWorldPos = bottomPoint != null ? bottomPoint.position : transform.position;
    transform.position = bottomWorldPos;  // Start at bottom (hidden position)
    lastPosition = transform.position;
    currentState = PlatformState.HiddenAtTop;
}
```

**Important**: If `topPoint` or `bottomPoint` are null, the platform falls back to its own position. This allows the platform to work without markers when used inside a Section (the section moves it instead).

### 4.3 Player Delta Transfer

The platform does NOT use `SetParent` to carry the player. Instead, it tracks position delta each frame and applies it to the player's Rigidbody2D:

```csharp
void Update()
{
    if (playerOnPlatform && playerRb != null)
    {
        Vector3 delta = transform.position - lastPosition;
        playerRb.position += new Vector2(delta.x, delta.y);
    }
    lastPosition = transform.position;
}
```

This approach:
- Avoids physics drag issues caused by SetParent
- Correctly handles both the platform's own movement AND parent section's movement
- Works frame-by-frame for smooth player transport

### 4.4 Audio System

#### Audio Timeline

```
        ↓ Descent Phase                  ↑ Ascent Phase
   ┌─────────────────┐          ┌─────────────────┐
   │  Motor_Start    │          │  Ascend_Start   │
   │  (one-shot)     │          │  (one-shot)     │
   │  ↓              │          │  ↓              │
   │  Motor_Loop     │          │  Motor_Loop     │
   │  (continuous)   │          │  (continuous)   │
   │  ↓              │          │  ↓              │
   │  Landing        │          │  Motor_Stop     │
   │  (one-shot)     │          │  (one-shot)     │
   │  ↓              │          │                 │
   │  [clawDelay]    │          │                 │
   │  ↓              │          │                 │
   │  Claw_Open      │          │                 │
   │  (one-shot)     │          │                 │
   │  ↓              │          │                 │
   │  [clawTransit]  │          │                 │
   │  ↓              │          │                 │
   │  Claw_Close     │          │                 │
   │  (one-shot)     │          │                 │
   └─────────────────┘          └─────────────────┘
```

#### Audio Methods
| Method | Description |
|--------|-------------|
| `PlayMotorStart()` | Play motor start one-shot, then transition to motor loop after clip length |
| `PlayMotorLoop()` | Start looping motor sound |
| `StopMotor()` | Stop motor loop, play motor stop one-shot |
| `PlaySfx(clip)` | Play a one-shot sound effect via sfxSource |

---

## 5. Audio System

### 5.1 Sound Effects List

| SFX ID | Name | Type | Trigger | Description |
|--------|------|------|---------|-------------|
| SFX_01 | Motor_Start | One-shot | Movement begins | Motor startup hum |
| SFX_02 | Motor_Loop | Loop | During movement | Continuous motor sound |
| SFX_03 | Motor_Stop | One-shot | Movement stops | Motor deceleration |
| SFX_04 | Landing | One-shot | Reaching bottom | Platform landing impact |
| SFX_05 | Claw_Open | One-shot | Claw opens | Mechanical claw opening |
| SFX_06 | Claw_Close | One-shot | Claw closes | Mechanical claw closing |
| SFX_07 | Ascend_Start | One-shot | Ascent begins | Ascent startup sound |

---

## 6. Unity Setup

### 6.1 Standalone Platform (No Section)

1. Create the platform GameObject with sprite, `BoxCollider2D` (non-trigger), and `Rigidbody2D` (Kinematic)
2. Create `TopPoint` and `BottomPoint` empty GameObjects as position markers
3. Add `S_MovingPlatform` component to the platform
4. Assign `topPoint` and `bottomPoint` in Inspector
5. Set `moveSpeed` (default 2)

### 6.2 Platform Inside a Section

1. Place the platform as a child of a `S_LevelSection` root
2. Leave `topPoint` and `bottomPoint` **unassigned** — the section moves the platform as a whole
3. Position the platform relative to the section's SectionBottom marker
4. The platform's own Reveal/Hide can still be used for independent movement within the section

### 6.3 Audio Setup

1. Add two AudioSource components to the platform:
   - `motorSource`: for motor loop (check **Loop** = false in Inspector, the script controls looping)
   - `sfxSource`: for one-shot effects (check **Loop** = false)
2. Assign all AudioClip references in Inspector
3. Audio is null-safe — missing clips or sources won't cause errors

### 6.4 Claw Animation Setup

1. Create a claw mechanism child GameObject with an Animator
2. Define two Animator triggers: `"Open"` and `"Close"`
3. Create animation states for open and closed claw positions
4. Assign the Animator to `clawAnimator` in Inspector
5. Adjust `clawDelay` (time after landing before claw opens) and `clawTransitionTime` (time between open and close)

---

## 7. Component Hierarchy

```
MovingPlatform (GameObject)
├── Platform_Body (Mesh/Sprite)
├── Rail_Left (Visual, optional)
├── Rail_Right (Visual, optional)
├── Rope_Left (LineRenderer, optional)
├── Rope_Right (LineRenderer, optional)
├── ClawMechanism (GameObject, optional)
│   ├── Claw_Base (Mesh)
│   ├── Claw_Finger_1 (Bone + Animation)
│   ├── Claw_Finger_2 (Bone + Animation)
│   └── Claw_Finger_3 (Bone + Animation)
├── AudioSource_Motor (AudioSource)
├── AudioSource_Effects (AudioSource)
├── BoxCollider2D (non-trigger)
├── Rigidbody2D (Kinematic)
└── S_MovingPlatform (Script)
```

---

## 8. Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| Platform doesn't move | Ensure `Reveal()` is called and `topPoint`/`bottomPoint` are assigned |
| Player slides off platform | Delta transfer handles this — ensure `CompareTag("Player")` works and Rigidbody2D is on the player body |
| Audio out of sync | Use `PlayOneShot()` for precise triggering; the script handles motor loop transitions |
| Motor sound cuts off abruptly | Add `motorStopClip` for a deceleration sound |
| Claw animation not playing | Verify `clawAnimator` is assigned and triggers `"Open"`/`"Close"` exist |
| Platform jitter | Use `Rigidbody2D` Interpolation = Interpolate on the player |
| Platform moves when section moves | This is expected — the platform follows its parent section root |
| Top/bottom points shift after section moves | Ensure topPoint/bottomPoint are children of the platform, NOT the section root |