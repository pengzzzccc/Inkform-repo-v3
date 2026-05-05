---
name: unity-dev
description: Unity C# development best practices for InkForm project. Use when writing MonoBehaviour scripts, setting up GameObjects, configuring audio, animations, physics, or tilemaps in Unity 2D.
---

# Unity Development Skill

## Tech Stack
- Unity 6000.1.17f1 (Unity 6)
- Unity Input System (new) — InputSystem_Actions auto-generated class
- Rigidbody2D (2D Physics)
- URP 2D Renderer
- ScriptableObject (Skill Data)

## Code Templates

### State Machine Pattern
```csharp
public enum StateType { Idle, Active, Transitioning }

private StateType _currentState = StateType.Idle;

void Update()
{
    switch (_currentState)
    {
        case StateType.Idle: HandleIdle(); break;
        case StateType.Active: HandleActive(); break;
        case StateType.Transitioning: HandleTransitioning(); break;
    }
}
```

### Inspector Grouping
```csharp
[Header("Movement Settings")]
public float moveSpeed = 2f;
public Transform targetPoint;

[Header("Audio")]
public AudioSource motorSource;
public AudioClip[] sfxClips;

[Header("Debug")]
[SerializeField] private StateType _debugState;
```

### Audio Triggers
```csharp
// One-shot SFX (does not interrupt other sounds)
sfxSource.PlayOneShot(clipName);

// Looping audio
motorSource.clip = loopClip;
motorSource.loop = true;
motorSource.Play();

// Stop looping audio
motorSource.Stop();
motorSource.loop = false;
```

### Moving Platform Delta Displacement Transfer
```csharp
// DO NOT use SetParent to carry the player (causes physics drag)
// Use per-frame delta displacement transfer instead
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

### Child Object Coordinate Snapshot
```csharp
// When top/bottom are child objects of MovingPlatform, snapshot world coordinates
// Important: top/bottom markers should be children of the platform itself,
// NOT placed under the section root (coordinates will shift after section moves)
private Vector3 topWorldPos;
private Vector3 bottomWorldPos;

void Start()
{
    topWorldPos = topPoint != null ? topPoint.position : transform.position;
    bottomWorldPos = bottomPoint != null ? bottomPoint.position : transform.position;
    transform.position = bottomWorldPos;
}
```

### World Position Anchor (Child Object Fixing)
```csharp
// Use when a child object needs to stay at a fixed world position (not follow parent)
// Typical scenario: Trigger/collision areas as Prefab children that should not move with parent at runtime
private Vector3 fixedWorldPos;

void Start()
{
    fixedWorldPos = transform.position; // Snapshot initial world position
}

void LateUpdate()
{
    // Force back to fixed position every frame (LateUpdate ensures it runs after parent movement)
    if (transform.position != fixedWorldPos)
        transform.position = fixedWorldPos;
}
```

### OverlapCircle Stun (LayerMask Physics Query)
```csharp
// Use for area-of-effect stun/sense checks against specific layers
// Requires enemyLayer field serialized in Inspector (e.g., User Layer 9 "Enemy")
[Header("Stun Settings")]
[SerializeField] private float stunRadius = 2f;
[SerializeField] private LayerMask enemyLayer;

void ApplyStun()
{
    Vector2 center = player.GetBodyTransform().position; // Body transform, NOT root
    Collider2D[] hits = Physics2D.OverlapCircleAll(center, stunRadius, enemyLayer);
    Debug.Log($"[Stun] OverlapCircle hits = {hits.Length}, center = {center}, layer = {enemyLayer.value}");

    foreach (Collider2D hit in hits)
    {
        S_NPCEnemy enemy = hit.GetComponent<S_NPCEnemy>();
        if (enemy != null) enemy.Stun();
    }
}
```
Key points:
- Use **LayerMask** (not `CompareTag`) — faster, zero GC allocation
- Use **body Transform.position** (not root Transform) as search center
- Add **diagnostic log** showing hits count and center position for debugging

## Error Handling Rules

### Error Logging
When a Unity runtime bug is found, you **must** log it to `memory-bank/error-log.md`:

```markdown
## [Date] | [Short Description]
- **Symptom**: [Abnormal behavior the player sees]
- **Root Cause**: [Code-level reason]
- **Fix**: [What was specifically changed]
- **Related Scripts**: [Affected .cs files]
- **Lesson**: [How to avoid similar issues in the future]
```

### Cross-Reference Bug Fix
When fixing a logic bug, you **must** follow this workflow:

1. **Identify root cause type**: e.g., "child object following parent movement", "coordinate system confusion", "event timing issue", etc.
2. **Extract keywords**: Pick 2-3 keywords from the root cause
3. **Scan design documents**: Search for keywords in `Assets/Perfab/Script/Project_Prompt/`, check if other systems have the same pattern
4. **List affected scripts**: Based on the design document architecture descriptions, find scripts that may have the same problem
5. **Check and fix each one**: Apply the fix to every affected script
6. **Log everything**: Record all cross-reference fixes to error-log.md

**Example**:
```
Root cause: Child object follows parent Transform movement
Keywords: child object, follow, Transform
Scan results:
- S_SectionGoal (trigger follows section) → needs fix
- S_MovingPlatform top/bottom (marker follows platform) → already has snapshot mechanism, safe
- S_MoveBlock trigger child → needs checking
```

## Important Notes
- **GameObject Root vs Body Transform**: When a player GameObject has a root parent (fixed y=0) and a body child (Rigidbody2D), always use the body Transform for position/distance calculations. Use `S_Player.Instance.GetBodyTransform()` or expose a `GetBodyTransform()` method — never rely on `transform.position` of the root
- Physics movement goes in `FixedUpdate()`, input detection goes in `Update()`
- Use `Rigidbody2D.linearVelocity` instead of `velocity` (Unity 6 API)
- Enable `Rigidbody2D` interpolation = `RigidbodyInterpolation2D.Interpolate` to avoid visual stuttering
- Moving platforms use delta displacement transfer — do NOT use SetParent
- Use 2D physics: `Rigidbody2D` + `Collider2D` — do NOT use 3D physics components
- Use `CompareTag()` instead of `tag ==` (zero GC allocation)
- Child objects needing fixed world positions (e.g., trigger areas): use `fixedWorldPos` + `LateUpdate` pattern