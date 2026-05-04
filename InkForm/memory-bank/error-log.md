# InkForm — Error Log

Known bugs and their fixes. Updated whenever a runtime or logic bug is discovered.

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