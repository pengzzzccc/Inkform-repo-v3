# InkForm — Changelog

## v0.5.0 — NPC Guard System & Suspicion Meter (2026-05-06)

### New Features
- **NPC Guard System**: 5-state state machine (Patrol / Chase / Attack / Arrest / Stunned)
  - `S_NPCbase` base class with identity, interaction, and component caching
  - `S_NPCEnemy` guard with patrol waypoints, chase/attack ranges, and EM projectile attack
  - `S_EMProjectile` fired during Attack state, applies paralyze on contact
- **Suspicion System**: 0-100 meter tracking player visibility in Chapter 2
  - `S_SuspicionSystem` singleton with AddSuspicion/SetSuspicion/CompleteMission API
  - Three suspicion tiers (Normal 0-33 / Elevated 34-66 / Critical 67-99) + Arrest at 100
  - Arrest also triggers on all 3 story missions complete
  - Passive decay, safe zone decay, and hidden decay rates
- **Hide Mechanic**: Player can hide in cabinets/pillars via `S_HideSpot`
  - Static `PlayerHidden` bridge property connecting S_HideSpot ↔ S_NPCEnemy
  - Press E to toggle hide; resets gravity, hides sprite and collider
- **Sprint Stun**: `S_Soild_sprint` now uses `Physics2D.OverlapCircleAll` on enemy layer
  - Stuns all guards within `stunRadius` on sprint activation

### Bug Fixes
- Fix `S_NPCEnemy.ValidatePlayerReference()` referencing root GameObject instead of body Transform
  - Guards now correctly chase the player's moving body, not the static root
- Fix `S_NPCbase.DistanceToPlayer()` using `S_Player.Instance.transform` (root) instead of `GetBodyTransform()`
- Fix `S_SuspicionSystem.HandleGameRestart()` not resetting `PlayerHidden` static field
  - Guards now correctly detect player after scene restart
- Fix `S_Soild_sprint` OverlapCircle using player root transform position instead of body position
- Fix NPC arrest flow: state bypass + death UI race condition (2026-05-07)
  - `TriggerArrest()` now uses `EnterState(State.Disabled)` instead of directly assigning `currentState`, ensuring color reset
  - `HandleGameStart()`/`HandleGameRestart()` now use `EnterState(State.Patrol)` for consistent state reset
  - `HandleArrest()` no longer calls `GameReStart()` immediately — death UI now displays correctly via `PlayerDied → ShowUI()`
  - **Rule established**: All state transitions MUST go through `EnterState()` — never set `currentState` directly

### Layer & Physics Configuration
- Added Enemy layer (User Layer 9) for NPC guards
- Added Projectile layer (User Layer 10) for EM projectile collision
- Configured Physics2D Layer Collision Matrix for Enemy↔Player and Projectile↔Player

### Documentation
- Added NPC System design document (architecture, state machine, layer setup, common errors)
- Added Suspicion System design document (static bridge pattern, thresholds, API, restart safety)

---

## v0.4.1 — Trigger World Position Anchor & Error Handling (2026-05-03)

### Bug Fixes
- Fix S_SectionGoal triggers following section movement
  - Added `fixedWorldPos` snapshot in `Start()` + `LateUpdate()` position enforcement
  - Triggers now stay at fixed world positions regardless of parent section movement
  - Documented as "World Position Anchor" pattern in design doc (section 4.4.1)

### Infrastructure
- Added error handling rules to `.clinerules`:
  - Error logging to `memory-bank/error-log.md` (symptom, root cause, fix, lesson)
  - Cross-reference bug fix: scan Project_Prompt/ design docs for similar patterns when fixing logic bugs
- Updated unity-dev skill with:
  - Error logging template
  - Cross-reference bug fix workflow (6-step process)
  - World Position Anchor code template
  - Note about top/bottom marker placement (should be platform children, not section children)
- Created `memory-bank/error-log.md` with all known bugs (8 entries from v0.1.0 to v0.4.1)

### Documentation
- Updated Level Section System design doc section 4.4.1 (World Position Anchor)

---

## v0.4.0 — Section System Overhaul (2026-05-03)

### New Features
- **Section-Level Movement**: `S_LevelSection` now handles entire section vertical movement
  - Added `sectionTopPoint` / `sectionBottomPoint` / `sectionMoveSpeed` fields
  - Section root moves as a whole, child objects (platforms, triggers) auto-follow
  - S_MovingPlatform independent functionality fully preserved for standalone use
- **Dual Trigger System**: Each section now has Start and End triggers
  - `S_SectionGoal` gained `SectionTriggerType` enum (Start/End)
  - StartTrigger fires when player enters section, EndTrigger fires when player exits
  - StartTrigger placed at section entrance, EndTrigger at section exit
- **Initial State**: All sections start hidden at top (player walks to trigger first section)

### Refactored
- `S_GameEvent`: Replaced `OnSectionCompleted` with `OnSectionStart` + `OnSectionEnd`
- `S_LevelSectionController`: Rewritten to listen to Start/End events instead of single completion event
- `S_LevelSection`: Removed direct child platform Reveal/Hide calls; section movement is now Transform-based
- `S_SectionGoal`: Added `SectionTriggerType` enum and dual event dispatch

### Documentation
- Updated Level Section System design doc with new dual-trigger architecture
- Updated section movement documentation
- Updated Prefab workflow instructions

---

## v0.3.1 — Platform Delta Transfer Fix (2026-05-03)

### Bug Fixes
- Fix player sluggish movement on moving platforms: replaced SetParent with Rigidbody2D delta transfer
  - Platform now tracks position delta each frame and applies it to the player's Rigidbody2D
  - Physics engine remains fully in control, preventing collision-fighting behavior
  - Player movement feels responsive while standing on ascending/descending platforms

### Documentation
- Added section 4.2.2 "Player Delta Transfer" to Level Section System design doc
- Explains why SetParent causes sluggish movement and how delta transfer solves it

---

## v0.3.0 — Level Section System (2026-05-03)

### New Features
- **Level Section System**: Implement section-based level progression with moving platforms
  - `S_MovingPlatform`: Refactored to command-based Reveal/Hide API (HiddenAtTop, Descending, VisibleAtBottom, Ascending)
  - `S_LevelSection`: Self-contained section component for Prefab workflow, auto-collects child platforms
  - `S_SectionGoal`: Trigger-based section completion detection
  - `S_LevelSectionController`: Scene-level controller managing section reveal/hide sequence
- **New Event**: `S_GameEvent.OnSectionCompleted` for section progression communication

### Architecture
- Each section is a self-contained Prefab (platforms + goal trigger)
- Sections are controlled externally by LevelSectionController via event-driven architecture
- Supports Prefab reuse across different levels

### Documentation
- Added Level Section System design document
- Added Player Controller design document
- Added Skill System design document
- Added Game Event System design document
- Added Manager Systems design document
- Added Level Objects design document
- Added Project CHANGELOG

---

## v0.2.0 — Bug Fixes & Code Quality (2026-04-29)

### Bug Fixes
- Fix S_Checkpoint dangling statement (missing braces)
- Fix S_Player physics material never applied (field name mismatch)
- Fix S_fluid_climb performance drain (Debug.Log in FixedUpdate)
- Fix S_SkillTree duplicate initialization on scene reload
- Fix S_Soild_sprint SprintLock restoring wrong form
- Fix character rendering jitter (enable Rigidbody2D interpolation)

### Code Quality
- Replace all `tag ==` with `CompareTag()` (zero GC allocation)
- Add null reference safety to S_GameManager
- Frame-rate independent Lerp in S_CameraMove and S_MoveBlock
- Clean up S_Pipline dead code
- Fix method naming: SoildMovement→SolidMovement, stateRuner→StateRunner, greping→gripping

### Documentation
- Added detailed English XML documentation to all scripts (later removed per request)
- Cleaned up code formatting across all scripts

---

## v0.1.0 — Initial Prototype

### Core Systems
- Player controller with solid/fluid form switching
- Skill system with ScriptableObject-based skills
- Sprint skill with cooldown and momentum
- Fluid climb skill with surface classification and exhaustion timer
- Game event bus for decoupled communication
- Game manager with scene loading and spawn points
- UI manager with pause menu toggle
- Level objects: breakable blocks, checkpoints, pipelines, moveable blocks
- Camera follow with smooth interpolation