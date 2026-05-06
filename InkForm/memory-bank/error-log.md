# InkForm — Error Log

Known bugs and their fixes. Updated whenever a runtime or logic bug is discovered.

---

## 2026-05-06 | S_NPCEnemy chases root instead of body
- **Symptom**: Guards chase the player's root GameObject (which doesn't move) instead of the actual moving body, so guards never reach the player.
- **Root Cause**: `ValidatePlayerReference()` used `S_Player.Instance.transform` (root) instead of `GetBodyTransform()`. The root stays at y=0 while the body is the actual Rigidbody2D-tagged child that moves.
- **Fix**: Changed `S_Player.Instance.transform` → `S_Player.Instance.GetBodyTransform()` in `ValidatePlayerReference()`.
- **Related Scripts**: `S_NPCEnemy.cs`
- **Lesson**: When the player uses root + body separation pattern, all Transform references must go through `GetBodyTransform()`. The root Transform is an anchor — no movement, no physics. Any script that calls `S_Player.Instance.transform` for distance/position is likely wrong.

### Cross-Reference Scan Results
- **S_NPCbase.DistanceToPlayer()**: Same bug — uses `S_Player.Instance.transform`. Fixed to use `GetBodyTransform()`.
- **S_Soild_sprint OverlapCircle**: Uses player root transform as search center. Fixed to use body position.
- **S_SuspicionSystem**: Does not reference Transform directly — safe.

---

## 2026-05-06 | S_SuspicionSystem.HandleGameRestart() leaves PlayerHidden stale
- **Symptom**: After game restart, guards do not detect the player even when the player is visible, because `PlayerHidden` static field remains `true` from previous session.
- **Root Cause**: `HandleGameRestart()` resets `currentSuspicion` and `missionsCompleted` but did not reset the static bridge field `PlayerHidden`.
- **Fix**: Added `S_HideSpot.PlayerHidden = false;` to `HandleGameRestart()`.
- **Related Scripts**: `S_SuspicionSystem.cs`, `S_HideSpot.cs`
- **Lesson**: Static bridge fields used for cross-system communication must be included in all reset paths (game restart, scene load, checkpoint reload). Any static field not reset will carry state across scene loads.

### Cross-Reference Scan Results
- **Other static fields in project**: `S_Player.Instance` (rebuilt on scene load), `S_AudioManager.Instance` (rebuilt on scene load), `S_GameEvent` events (cleared by C# event losing subscribers on scene reload). All safe.
- **S_SkillTree initialized flag**: Already has reset guard. Safe.

---

## 2026-05-06 | S_Soild_sprint OverlapCircle uses wrong search center
- **Symptom**: Sprint stun never hits guards because OverlapCircle searches around root Transform (y=0), not the player's actual position.
- **Root Cause**: `Physics2D.OverlapCircleAll(transform.position, ...)` uses root Transform. Root is at y=0, player body could be at y=10+.
- **Fix**: Changed to `playerBodyTransform.position` (body Transform reference). Added diagnostic `Debug.Log` showing hits count and center position.
- **Related Scripts**: `S_Soild_sprint.cs`
- **Lesson**: Any Physics2D query (OverlapCircle, OverlapBox, Raycast) that uses the player's position must use body Transform, not root Transform.

### Cross-Reference Scan Results
- **S_fluid_climb**: Uses `transform.position` for climbing checks. But fluid climb is on the body child component — safe (not root).
- **S_NPCEnemy chase**: Fixed above in entry #9. Safe now.

---

## 2026-05-03 | S_LevelSection starts moving on game start without trigger
- **Symptom**: Sections spontaneously move and disappear on game start. They reappear when the previous section's EndTrigger fires.
- **Root Cause**: `S_LevelSectionController.Start()` calls `HideSection()` on all sections, but `S_LevelSection.Start()` may not have executed yet (execution order is not guaranteed). When `HideSection()` runs before `S_LevelSection.Start()`, `topWorldPos` is `Vector3.zero` (default), so `moveTarget = Vector3.zero` — sections move toward world origin and disappear.
- **Fix**: Added `initialized` flag in `S_LevelSection`. `Start()` sets `initialized = true` after snapshotting positions. `RevealSection()` and `HideSection()` return early if `!initialized`, preventing movement before coordinates are snapshotted.
- **Related Scripts**: `S_LevelSection.cs`, `S_LevelSectionController.cs`
- **Lesson**: When one MonoBehaviour calls another's methods in `Start()`, execution order is undefined in Unity. Always add initialization guards (`initialized` flag) to protect methods that depend on `Start()` having completed. This is the same pattern used in `S_SkillTree`.

### Cross-Reference Scan Results
- **S_MovingPlatform**: Safe — its `Reveal()`/`Hide()` methods are called externally, not from another script's `Start()`.
- **S_SkillTree**: Already has `initialized` flag for the same reason (DontDestroyOnLoad + scene reload).

---

## 2026-05-03 | S_LevelSection drifts horizontally during ascent/descent
- **Symptom**: All sections drift left when moving up/down after game restart.
- **Root Cause**: `S_LevelSection.Start()` snapshot full XYZ world positions from sectionTopPoint/sectionBottomPoint. `Vector3.MoveTowards()` then moved the section on all 3 axes. If Top/Bottom markers had different X coordinates (common when placed at different positions in the scene), the section would drift horizontally during vertical movement.
- **Fix**: Only snapshot Y coordinates from sectionTopPoint/sectionBottomPoint. X/Z always use the section root's own position. Movement now uses `Mathf.MoveTowards` on Y axis only.
- **Related Scripts**: `S_LevelSection.cs`
- **Lesson**: Vertical movement systems should only modify the Y axis. Use `Mathf.MoveTowards` for single-axis movement instead of `Vector3.MoveTowards` which moves on all axes. Marker objects' X/Z coordinates should be ignored.

### Cross-Reference Scan Results
- **S_MovingPlatform**: Uses `Vector3.MoveTowards` for full XYZ movement — this is correct because platforms can move in any direction based on topPoint/bottomPoint placement.
- **S_SectionGoal**: Uses `fixedWorldPos` anchor — unaffected, position is fixed.
- **S_CameraMove**: Uses Lerp on X/Y — unaffected.

---

## 2026-05-03 | S_SectionGoal follows section movement
- **Symptom**: StartTrigger and EndTrigger move with the section when it ascends/descends, making them unreachable by the player after the section moves to the top.
- **Root Cause**: S_SectionGoal is a child of S_LevelSection. When the section root Transform moves, all children (including triggers) follow.
- **Fix**: Added `fixedWorldPos` snapshot in `Start()`, enforced position in `LateUpdate()` to keep triggers at their original world coordinates.
- **Related Scripts**: `S_SectionGoal.cs`
- **Lesson**: When a child object needs to stay at a fixed world position (not follow parent), use the `fixedWorldPos + LateUpdate` pattern. Common for triggers, collision zones, and UI anchors that are logically part of a Prefab but physically fixed.

### Cross-Reference Scan Results
- **S_MovingPlatform topPoint/bottomPoint**: Safe — already uses world pos snapshot in `Start()`. Note: top/bottom markers should be children of the platform itself, not of the section root.
- **S_MoveBlock triggers**: To be checked if S_MoveBlock ever becomes a child of a moving parent.
- **Other triggers in project**: Safe — not nested under moving parents.

---

## 2026-05-03 | S_MovingPlatform child point tracking
- **Symptom**: topPoint and bottomPoint are children of the platform, so when the platform moves, the target positions move with it, causing the platform to never reach its destination.
- **Root Cause**: `topPoint.position` and `bottomPoint.position` were read live each frame instead of being snapshotted.
- **Fix**: Snapshot world positions in `Start()` into `topWorldPos` and `bottomWorldPos`, use these fixed values for movement.
- **Related Scripts**: `S_MovingPlatform.cs`
- **Lesson**: When using child objects as position markers for movement targets, always snapshot their world positions at initialization. Never read live Transform positions of children that move with the parent.

---

## 2026-05-03 | Player sluggish on moving platforms
- **Symptom**: Player movement feels sluggish and resistant when standing on a moving platform (especially ascending).
- **Root Cause**: Using `SetParent` to attach the player to the platform. The physics engine detects overlap between the player and platform colliders and fights the parent-forced position.
- **Fix**: Replaced `SetParent` with per-frame delta transfer: `playerRb.position += (currentPos - lastPos)`.
- **Related Scripts**: `S_MovingPlatform.cs`
- **Lesson**: Never use `SetParent` for physics-driven objects on moving platforms. Use delta position transfer to keep the player's Rigidbody2D fully physics-driven.

---

## 2026-04-29 | S_Checkpoint dangling statement
- **Symptom**: Checkpoint logic executes only the first line after the if-statement, ignoring the rest.
- **Root Cause**: Missing braces `{}` around multi-line if-block.
- **Fix**: Added braces to the if-block.
- **Related Scripts**: `S_Checkpoint.cs`
- **Lesson**: Always use braces for if-blocks, even for single-line bodies. Prevents accidental dangling statements.

---

## 2026-04-29 | S_Player physics material never applied
- **Symptom**: Player's physics material (solid/fluid friction) never takes effect.
- **Root Cause**: Private fields `solidMat`/`fluidMat` shadowed the serialized fields `SolidMat`/`FluidMat` due to naming mismatch.
- **Fix**: Removed the private copies, used the serialized fields directly.
- **Related Scripts**: `S_Player.cs`
- **Lesson**: Be careful with field naming conventions. Private fields with `camelCase` can shadow serialized fields with `PascalCase` if not careful.

---

## 2026-04-29 | S_fluid_climb performance drain
- **Symptom**: Frame rate drops when player is near climbable surfaces.
- **Root Cause**: `Debug.Log()` called every frame in `FixedUpdate()`.
- **Fix**: Removed the debug log.
- **Related Scripts**: `S_fluid_climb.cs`
- **Lesson**: Never leave `Debug.Log()` in hot paths (Update/FixedUpdate). Use conditional compilation `#if UNITY_EDITOR` or remove after debugging.

---

## 2026-04-29 | S_SkillTree duplicate initialization
- **Symptom**: Skills initialize twice on scene reload, causing duplicate event subscriptions.
- **Root Cause**: `DontDestroyOnLoad` persists the singleton, but `Start()` runs again on scene load.
- **Fix**: Added `initialized` flag to prevent re-initialization.
- **Related Scripts**: `S_SkillTree.cs`
- **Lesson**: Singletons with `DontDestroyOnLoad` need initialization guards.

---

## 2026-04-29 | S_Soild_sprint SprintLock wrong form
- **Symptom**: Sprint lock coroutine restores the wrong form after sprint ends.
- **Root Cause**: Coroutine captures form state at coroutine start, not at actual sprint end.
- **Fix**: Store the form to restore when the lock actually triggers, not at coroutine creation.
- **Related Scripts**: `S_Soild_sprint.cs`
- **Lesson**: Be careful with coroutine state capture. Capture values at the point of use, not at coroutine creation.

---

## 2026-05-07 | NPC arrest stuck red + death UI never shows
- **Symptom**: After arrest, NPC stays red (Discovered color). Cannot arrest again. Game restart doesn't reset NPC color. Death UI never appears.
- **Root Cause**: Three bugs stacked:
  1. `TriggerArrest()` set `currentState = State.Disabled` directly, bypassing `EnterState()` → `UpdateStateColor()` never called → NPC stuck red
  2. `HandleGameStart()`/`HandleGameRestart()` set `currentState = State.Patrol` directly + manual `UpdateStateColor()` — inconsistent with state machine pattern
  3. `HandleArrest()` immediately called `GameReStart()` → scene reload fires `HideUI()` → death UI `ShowUI()` overridden one frame later
- **Fix**:
  - `TriggerArrest()`: use `EnterState(State.Disabled)` to reset color; add `S_GameEvent.PlayerDied()` to trigger death UI
  - `HandleGameStart()`/`HandleGameRestart()`: use `EnterState(State.Patrol)` for consistent state reset
  - `HandleArrest()`: removed `GameReStart()` call; death UI now shown via `PlayerDied → ShowUI()`, player manually clicks Restart
- **Related Scripts**: `S_NPCEnemy.cs`, `S_GameManager.cs`
- **Lesson**: All state transitions MUST go through `EnterState()` to keep side effects (color update, timer reset) in one place. Never set `currentState` directly outside `EnterState()`. Event chains that include UI + scene reload must be carefully ordered — `PlayerDied` and `ArrestTriggered` are separate events; `HandleArrest` should not auto-restart as it races with `ShowUI`.

### Cross-Reference Scan Results
- **S_NPCbase**: `EnterState()` pattern is properly followed after fix. Safe.
- **S_GameManager.PlayerDied()**: Only resets position, does not reload scene — correct.
- **S_UIManager**: Subscribed to both `OnPlayerDied → ShowUI` and `OnGameRestart → HideUI` — ordering now correct since `HandleArrest` no longer calls `GameReStart`.
