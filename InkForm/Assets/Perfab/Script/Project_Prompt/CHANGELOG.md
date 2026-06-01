# InkForm — Changelog

## v0.8.1 - Gameplay UX, Energy, Scene Flow, ManagerRoot Hardening (2026-05-25)

### Gameplay UX & Flow
- Added scene transition flow in `S_GameManager`: fade out, optional transition SFX, async load, fade in, and gameplay input lock/unlock during transition.
- Added `S_SceneReference` for Inspector scene drag references. Runtime loading prefers `scenePath` and keeps `sceneName` as a compatibility fallback.
- Updated `S_GameManager` and `S_SceneChangeTrigger` to use scene references instead of hand-typed scene names where possible.
- Added load-time scene validation so missing Build Settings / Build Profile entries produce clear errors and recover transition/input state.
- Added independent death UI in `S_UIManager` with red death count and a single `back to checkpoint` button. Death no longer opens the normal pause menu or immediately respawns.

### Shared Player Energy
- Added `S_PlayerEnergy` on the player with `maxEnergy`, delayed regeneration, reset-on-checkpoint behavior, and `OnPlayerEnergyChanged(current, max)` UI updates.
- Added shared skill energy configuration on `S_SkillBase`: `minEnergyToStart` and `energyDrainPerSecond`.
- Added `quickTapEnergyCost` to `S_Soild_sprint`; sprint, FluidClimb, and CameraControl now consume the same player energy pool.
- Added runtime energy UI generation/update in `S_UIManager`; skill use drains energy and recovery starts after the configured delay.

### Level Objects & Movement Fixes
- Added dropped-key behavior: breakable blocks call `S_Key.InitializeDroppedKey(...)`, keys pop out first, then hover near the ground before pickup.
- Improved NPC physics setup with `Continuous` collision detection and `Interpolate` to reduce wall clipping during chase, jump, and knockback.
- Improved FluidClimb grip detection so holding Grip near/facing a wall can attach more reliably without requiring perfect center overlap.
- Fixed jump reset on moving platforms by using `SampleWalkableGround()` and a grounded reset guard instead of relying on FluidClimb surface state.

### ManagerRoot Single Persistence
- `ManagerRoot.prefab` is now the only object that calls `DontDestroyOnLoad`.
- `S_UIManager` is embedded under `ManagerRoot.prefab`; standalone UIManager scene instances were removed.
- Child managers (`S_GameManager`, `S_UIManager`, `S_InputBindingManager`, `S_AudioManager`, `S_SuspicionSystem`, `S_SkillTree`, `S_PerformanceMonitor`) no longer self-reparent, self-create, or call `AttachPersistent()` during normal lifecycle.
- `S_ManagerRoot.AttachPersistent()` remains only as a compatibility no-op/warning path; normal setup requires managers to be direct children of `ManagerRoot.prefab`.
- `Start.unity` now contains the full `ManagerRoot.prefab`; `S_StartMenuController` validates this setup instead of creating partial manager objects at runtime.

### Documentation
- Updated manager, event, skill, player, level object, architecture, memory-bank, and error-log documentation to match the current v0.8.1 behavior.

---

## v0.8.0 - Architecture Refactor: Modular Directory & Interface Abstraction (2026-05-25)

### Architecture Overhaul
- **Major directory restructure**: Flat `player/`, `Manager/`, `Npcs/`, `LevelCon/`, `tools/` → modular tree with `Player/Core/`, `Player/Skills/`, `Player/Body/`, `Player/Physics/`, `Managers/`, `NPCs/Core/`, `NPCs/Combat/`, `NPCs/Dialogue/`, `NPCs/Sensors/`, `NPCs/Spawning/`, `Level/Interactables/`, `Level/Platforms/`, `Level/Resources/`, `Level/Sections/`, `Level/Zones/`, `Camera/`, `Core/Events/`, `Input/`, `Systems/Suspicion/`, `Tools/`
- All `.cs` and `.meta` files moved to new locations; Unity meta GUIDs preserved

### New Files
- **`S_PlayerContracts.cs`** (`Player/Core/`): `IPlayerActor` interface + `S_PlayerLookup` static utility
  - `IPlayerActor`: abstracts player access (Rigidbody, Collider, BodyTransform, IsFluidForm, IsParalyzed, Teleport, SetMovementLocked, ApplyParalyze, ForceSprintBreakthrough, CancelSprintCharge)
  - `S_PlayerLookup.TryGet(Collider2D/Collision2D, out IPlayerActor)`: resolves player from any collider
  - `S_PlayerLookup.TryGetActive(out IPlayerActor)`: gets current active player
  - `S_PlayerLookup.IsPlayer(Collider2D/Collision2D)`: quick tag + component check
- **`S_PlayerSkillController.cs`** (`Player/Skills/`): Extracted sprint charge + camera control logic from `S_Player`
  - Sprint charge state machine: `BeginSprintCharge`, `FixedTickSprintCharge`, `ReleaseSprintCharge`, `CancelSprintCharge`
  - Camera control: `BeginCameraControl`, `EndCameraControl`, `CameraControlTick`, `HandleCameraControlInput`
  - Initialized via `Initialize()` injection from `S_Player`
- **`S_ManagerRoot.cs`** (`Managers/`): Persistent `DontDestroyOnLoad` manager container
  - `EnsureExists()`: finds or creates the root GameObject
  - `AttachPersistent(Transform)`: parents a manager under the root
  - `GetOrCreateChild(string)` / `GetOrCreateComponent<T>(string)`: lazy child/component creation
  - `RuntimeInitializeOnLoadMethod` reset for editor domain reload
- **`S_SceneCheckpointTracker.cs`** (`Level/Interactables/`): Per-scene checkpoint/respawn tracker
  - Auto-creates per scene via `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]`
  - Listens to `OnSpawnPointChanged`, `OnPlayerDied`, `OnRespawnRequested`
  - Uses `IPlayerActor.Teleport` for respawn; falls back to `SceneManager.LoadScene` if no player
  - Tracks spawn position per scene to support multi-scene workflows

### Interface Abstraction
- `S_Player` now implements `IPlayerActor`
- NPC systems (`S_NPCEnemy`), level systems (`S_HideSpot`, `S_LevelSection`, `S_SceneCheckpointTracker`), and suspicion system now resolve the player via `S_PlayerLookup.TryGet()` / `S_PlayerLookup.TryGetActive()` instead of directly referencing `S_Player.Instance`
- This decouples gameplay systems from the concrete `S_Player` class

### S_GameEvent Expansion (30+ events, up from 19)
- **Scene management**: `OnRunStartRequested`, `OnReturnToStartMenuRequested`, `OnSceneLoadRequested`, `OnGameplayInputEnabledRequested`, `OnLevelCompleted`
- **Spawn point**: `OnSpawnPointChanged` (replaces `reNewSpwnPoint` — old event kept as compatibility bridge)
- **Volume control**: `OnBgmVolumeChangeRequested`, `OnSfxVolumeChangeRequested`
- **Suspicion refinement**: `OnSuspicionValueChanged`, `OnSuspicionChangeRequested`, `OnHiddenSuspicionDecayRequested`, `OnPlayerHiddenChangeRequested`, `OnPlayerHiddenChanged`, `OnSuspicionResetRequested`

### Skill Controller Extraction
- Sprint charge logic moved from `S_Player` to `S_PlayerSkillController`
- Camera control logic moved from `S_Player` to `S_PlayerSkillController`
- `S_Player` now delegates to `S_PlayerSkillController` for sprint/camera operations
- `S_PlayerSkillController` is created and injected by `S_Player` during `Awake()`

### Manager Root
- All persistent managers (`S_GameManager`, `S_UIManager`, `S_AudioManager`, `S_InputBindingManager`) now attach under `S_ManagerRoot` via `AttachPersistent()`
- `S_ManagerRoot` provides `DontDestroyOnLoad` lifecycle management
- `S_PerformanceMonitor` references `S_ManagerRoot` for dependency validation

### Documentation
- **Architecture.md**: Complete rewrite with 10 Mermaid diagrams reflecting new modular structure, IPlayerActor interface, S_ManagerRoot, S_PlayerSkillController, S_SceneCheckpointTracker, and 30+ events
- **memory-bank/activeContext.md**: Updated to v0.8.0 with new directory tree and design decisions
- **memory-bank/progress.md**: Updated to v0.8.0 with architecture refactor details

---

## v0.7.2 - Key & Exit Gate System, UI Fixes (2026-05-15)

### New Features
- **Key & Exit Gate System**: Collect keys to unlock the exit gate and load the next level
  - `S_Key`: Collectible key items with trigger-based collection, persists across deaths within same level, resets on scene load
  - `S_ExitGate`: Locked gate that unlocks when `collectedKeys >= requiredKeys`, player contact loads next level via `S_GameManager.LoadNextLevel()`
  - `S_GameEvent`: Added `OnKeyCollected` and `OnKeyCountChanged` events
  - `S_UIManager`: Added `keyCountText` field for displaying "collected / total" HUD
  - Keys use `HashSet<S_Key>` static tracking, `SceneManager.sceneLoaded` hook for auto-reset

### Bug Fixes
- **Controls Mapping Panel**: Fixed panel not displaying binding rows (only footer buttons visible)
  - Root cause: `VerticalLayoutGroup` on ControlsPanel conflicted with `ScrollRect` height allocation
  - Fix: Replaced VLG with pure anchor-based positioning for Title, ScrollRect, and Footer
  - ScrollRect now uses anchor offsets (top: -46px for Title, bottom: +58px for Footer)
  - FooterContainer uses anchor-bottom + HorizontalLayoutGroup for button arrangement
  - All previous improvements preserved: ScrollRect scroll support, ScrollToSelected, raycastTarget=false, Hide/Camera Control mapping rows

### Documentation
- **Level_Objects_Design.md**: Added §11 Key & Exit Gate System with full S_Key and S_ExitGate documentation
- **Game_Event_System_Design.md**: Added OnKeyCollected and OnKeyCountChanged to event inventory, invocation methods, and §5.5 Key & Gate Events category

---

## v0.7.1 - Documentation Sync & Platform Cable (2026-05-15)

### New Features
- **Dual Platform Cable** (`S_PlatformCableVisual`): Platform cables now render two lines instead of one
  - New `cableOffset` parameter controls horizontal spacing from platform center
  - Cable Y follows `topAnchor` height, X stays at platform center (no horizontal drift)
  - Cable length dynamically adjusts as platform moves up/down
  - Auto-creates `CableLeft` and `CableRight` child objects with LineRenderers
  - Removes legacy single LineRenderer on main object
  - `[DefaultExecutionOrder(100)]` ensures cables update after platform movement

### Documentation (Full Sync)
Comprehensive audit of all 9 design documents against 40+ source files:

- **Player_Controller_Design.md**: Added §10 Camera Control System, §11 Movement Lock System. Updated §1 overview, §2.1 dependencies (CameraControl, procedural renderer, dynamic collider), §3.1 fields (cameraController), §3.2 CameraMove fields (manualMoveSpeed, manualMaxDistanceFromTarget, returnSmoothSpeed, followDeadZoneRadius), §3.1 public/private methods (SetMovementLocked, BeginCameraControl, ReleaseSprintCharge, ApplyParalyze, ForceSprintBreakthrough, etc.), §4 input table (CameraControl action), §4 input flow diagram

- **Skill_System_Design.md**: Added §9 Camera Control Skill. Updated §1 overview (CameraControl skill), §2.1 class hierarchy (S_CameraControlSkill)

- **Level_Objects_Design.md**: Added §7 Button Door System, §8 Hide Spot, §9 Section Goal, §10 Platform Cable Visual. Updated §2 object inventory with 6 new entries

- **Manager_Systems_Design.md**: Added §9 S_AudioManager Platform Alarm, §10 S_SectionAlarmEffect. Updated §2.1 architecture (S_SectionAlarmEffect), §2.2 lifecycle table, §3.1 S_GameManager (loadScene field)

- **Game_Event_System_Design.md**: Expanded from 11 to 19 events. Added OnSectionDescentStarted, OnSectionDescentCompleted, OnNPCInteract, OnSuspicionChanged, OnAlertTriggered, OnArrestTriggered, OnStoryTrigger. Updated §2.2 inventory, §3.1 declarations, §3.1 invocation methods

- **NPC_System_Design.md**: Added §10 NPC Story System, §11 NPC Camera System

- **Moving_Platform_Component_Design.md**: Added §9 S_PlatformCableVisual quick reference

- **Level_Section_System_Design.md**: Added §8 Section Descent Events

- **Suspicion_System_Design.md**: No changes needed (already complete)

---

## v0.7.0 - Sprint Charge, NPC Jumping & Wave Spawner (2026-05-13)

### New Features
- **Sprint Charge System**: Hold sprint key to charge, release to dash
  - Three-stage size scaling (stage1Scale, stage2Scale, stage3Scale) with time thresholds
  - High-frequency shake effect on stage transition (damped sine wave)
  - Sprint direction now uses release-time facing (no more initial direction lock)
  - Eyes follow current velocity direction during charge (no longer frozen)
  - Procedural renderer freezes into a perfect circle during charge (no tail, no deformation)
  - Dynamic collider scales with charge stage
  - Minimum sprint speed guarantee via `minSprintSpeed` parameter
  - Buffer time system: quick-tap immediately dashes at minSprintSpeed with no visual delay
  - Stage-based cooldowns: Stage 1 = 0.1s, Stage 2 = 0.5s, Stage 3 = 1.0s (all adjustable)
  - During charge: player can move, jump, and grip walls normally
  - Low-friction charge ball physics material for rolling behavior

- **NPC Jumping System**: Predictive jump for gap and obstacle traversal
  - Wall detection via forward raycast
  - Gap detection via multi-step ground scanning with landing spot prediction
  - Player-above detection for vertical chase
  - Dynamic jump force and horizontal boost calculated from landing point distance/height
  - Air control factor (50% by default) for natural jump arcs
  - Works with both Rigidbody and Transform movement modes

- **NPC Wave Spawner** (`S_NPCWaveSpawner`): Runtime NPC generation
  - Spawns NPCs at camera edges every configurable interval (default 30s)
  - Configurable NPCs per side, max alive count, ground detection
  - Automatic cleanup of distant NPCs
  - Inspector camera reference (falls back to Camera.main)
  - Gizmo visualization of spawn zones and cleanup radius

### Bug Fixes
- Fixed `SetMovementLocked` missing from `S_Player` causing `S_HideSpot` compile error
- `movementLocked` now properly blocks jumping, gripping, and movement input

### Refactored
- Sprint input changed from `WasPerformedThisFrame()` instant activation to hold-to-charge system
- `S_Player.BeginSprintCharge()` no longer sets visual/physics state immediately (buffer delay)
- `S_PlayerProceduralRenderer.SetChargeOverride()` simplified to `bool active` parameter
- `MoveHorizontally()` in `S_NPCEnemy` now preserves air control during jumps

### Documentation
- Updated Player Controller design document with Sprint Charge system
- Updated Skill System design document with new Sprint parameters
- Updated NPC System design document with jumping ability and wave spawner
- Updated Manager Systems design document with `SetMovementLocked` API
- Updated memory-bank context and progress files

---

## v0.6.3 - Hybrid Slime Tail Pass (2026-05-12)

### Rendering
- Added a hybrid tail model that keeps light body lag while rendering a separate circle-tail mesh.
- Tail mesh uses external tangent bridge points between the body circle and a smaller tail circle.
- Added direct tail size, distance, bridge, and follow-speed parameters for easier tuning.

---

## v0.6.2 - Dynamic Capsule Collider Phase (2026-05-12)

### New Features
- Added dynamic `CapsuleCollider2D` support to the player slime collider.
- Normal movement remains on `CircleCollider2D`.
- Crouch/slick uses a horizontal capsule.
- Wall climb uses a vertical capsule offset toward the wall.
- Ceiling climb uses a flattened horizontal capsule.

### Integration
- `S_Player.GetCollider()` now returns the active dynamic collider.
- Grip buffer casts, surface classification, and procedural contact fitting now follow the active collider.
- `Pre_MainChar.prefab` now includes the disabled capsule component and serialized capsule tuning defaults.

### Tuning
- Capsule shape switches smooth size and offset instead of snapping directly.
- Renderer adds `colliderShapeFollow` so the procedural body can partially follow active capsule proportions.
- Renderer adds tail ground sticking so motion lag stays close to the floor contact plane.
- Crouch capsule now anchors from the bottom edge and uses input/contact smoothing to avoid walking bounce and tail flicker.
- Renderer adds a contact-fill mesh under the slime body to cover tiny visible gaps caused by contact-plane skin.

---

## v0.6.1 - Slime Rendering Baseline & Interaction Polish (2026-05-12)

### New Features
- Added procedural player slime rendering via `S_PlayerProceduralRenderer`.
- Added runtime-generated body, outline, eye glow, and white eye meshes for the player body.
- Added contact-plane visual fitting so the slime boundary is pushed outside floors, walls, and ceilings.
- Added rounded-triangle contact shaping for a smoother weighted slime silhouette.
- Added dynamic `CircleCollider2D` support via `S_PlayerDynamicCollider`.
- Added Grip buffer snapping so fluid climb can start slightly before full wall contact.
- Added direct Ceiling state entry when gripping and contacting ceilings.
- Added JumpPad force range and force-to-color visualization.

### Tuning
- Reduced slime tail size with lower `motionLag`, capped `maxTailStretch`, and lower tail contribution.
- Increased default player slime mesh resolution and added edge smoothing for softer boundaries.
- Synchronized current scene player sprite settings back into `Pre_MainChar.prefab`.

### Bug Fixes
- Guarded `S_UIManager` against duplicate `DontDestroyOnLoad` registration assertions.
- Updated JumpPad color in edit mode with `OnValidate`.

### Documentation
- Added Player Procedural Rendering design document.
- Updated Player Controller, Skill System, Level Objects, and Manager Systems notes for the current baseline.

---

## v0.6.0 - Input Binding UI & Rigidbody-Free NPC Knockback (2026-05-10)

### New Features
- Added `S_InputBindingManager` singleton to share `InputSystem_Actions`, save binding overrides, reset bindings, and coordinate interactive rebinding.
- Expanded `S_UIManager` with runtime-generated controls menu for keyboard, mouse, and gamepad binding changes.
- Added gamepad menu support in `S_UIManager`, including selected UI state and Cancel/Back behavior.
- Added `S_NPCSpawnerTool` for inspector-driven NPC spawning, count adjustment, generation, and cleanup.

### NPC Movement & Combat
- Made `Rigidbody2D` optional for NPC enemies.
- Added Transform-based NPC movement with collider casts for horizontal movement, falling, ground snapping, and obstacle blocking.
- Added sprint knockback support for NPC enemies without requiring a `Rigidbody2D`.
- Updated sprint hit detection to resolve `S_NPCEnemy` from child colliders via `GetComponentInParent`.

### Bug Fixes
- Fixed no-Rigidbody NPC enemies falling through the ground by adding manual ground collision checks and vertical movement resolution.
- Kept current `.inputactions` defaults as source of truth and corrected project documentation around Grip binding behavior.

### Documentation
- Updated Player Controller, Manager Systems, and NPC System design notes for input binding, UI manager behavior, and optional Rigidbody NPC movement.

---

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
- Fix suspicion respawn handling not resetting `PlayerHidden` static field
  - Guards now correctly detect player after scene restart
- Fix `S_Soild_sprint` OverlapCircle using player root transform position instead of body position
- Fix NPC arrest flow: state bypass + death UI race condition (2026-05-07)
  - `TriggerArrest()` now uses `EnterState(State.Disabled)` instead of directly assigning `currentState`, ensuring color reset
  - Run/respawn handlers now use `EnterState(State.Patrol)` for consistent state reset
  - `HandleArrest()` no longer forces an immediate restart; death UI now displays correctly via `PlayerDied -> ShowUI()`
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
