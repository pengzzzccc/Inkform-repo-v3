# InkForm Project Progress

## Current Version
v0.8.1 (Gameplay UX, Energy, Scene Flow, ManagerRoot Hardening)

## What Works
- Player Controller (IPlayerActor interface, solid/fluid form switching, wall climb, wall jump, paralyze)
- Sprint Charge System (hold-to-charge, buffer, three-stage scaling, shared energy drain)
- Camera Control System (bullet-time manual camera, now in S_PlayerSkillController)
- Procedural Slime Rendering (body, outline, eye glow, contact-plane fitting, hybrid tail mesh)
- Dynamic Collider (CircleCollider2D default + CapsuleCollider2D for crouch/wall/ceiling)
- Skill System (Sprint charge, FluidClimb, CameraControl, Skill Tree structure)
- Input Binding System (S_InputBindingManager with runtime rebinding + PlayerPrefs persistence)
- Level Objects (breakable blocks, checkpoints, moving platforms, pipelines, jump pads, doors, button doors, keys, exit gates)
- Moving Platforms (delta displacement transfer)
- Game Event Bus (30+ events including scene management, volume control, suspicion refinement)
- Manager Systems (S_ManagerRoot persistent container, GameManager, UIManager, AudioManager, InputBindingManager)
- Level Section System (dual triggers, section-level movement, alarm effects)
- NPC Guard System (5-state: Patrol/Chase/Aim/Attack/Arrest/Stunned, Rigidbody2D optional)
- NPC Jumping System (predictive jump with wall/gap/player-above detection)
- NPC Spawner Tool (S_NPCSpawnerTool for inspector-driven spawning)
- NPC Wave Spawner (S_NPCWaveSpawner for runtime camera-edge spawning)
- NPC Camera (S_NPCCamera)
- NPC Dialogue & Story (S_NPCDialogue, S_NPCStory)
- Suspicion System (0-100 meter, 3-tier thresholds, event-driven)
- Hide Mechanic (S_HideSpot with event-driven PlayerHidden bridge)
- Sprint Stun (Physics2D.OverlapCircleAll on enemy layer)
- Audio System (BGM/SFX via S_AudioManager + S_GameEvent events)
- Narrative System (Characters, Story Outline, World Overview, Willard Protocol)
- Player Movement Lock API (SetMovementLocked for S_HideSpot integration)
- Player Contracts (IPlayerActor interface + S_PlayerLookup utility)
- Manager Root (single persistent ManagerRoot.prefab)
- Scene Checkpoint Tracker (S_SceneCheckpointTracker per-scene auto-creation)
- Key & Exit Gate System (collect keys to unlock exit, load next level)

## v0.8.1 - Gameplay UX, Energy, Scene Flow, ManagerRoot Hardening (2026-05-25)

### ManagerRoot Single Persistence
- [x] `ManagerRoot.prefab` is the only object that calls `DontDestroyOnLoad`
- [x] `S_UIManager` moved under `ManagerRoot.prefab`; standalone scene UIManager instances removed
- [x] Child managers no longer self-create, self-reparent, or call `AttachPersistent()` during normal lifecycle
- [x] `S_StartMenuController` validates full ManagerRoot setup instead of creating partial managers

### Scene Flow & UI
- [x] `S_SceneReference` supports Inspector SceneAsset drag references with runtime path/name fallback
- [x] `S_GameManager` transition fade/SFX flow disables gameplay input while loading
- [x] Scene loading validates Build Settings / Build Profiles before transition starts
- [x] Death UI uses an independent panel with red death counter and `back to checkpoint` button

### Shared Energy & Gameplay Fixes
- [x] `S_PlayerEnergy` shared pool, regen delay, reset behavior, and `OnPlayerEnergyChanged` event
- [x] Skill assets expose `minEnergyToStart` and `energyDrainPerSecond`; sprint exposes `quickTapEnergyCost`
- [x] Energy UI reflects shared skill usage and recovery
- [x] Dropped keys pop out from breakable blocks and hover near ground before pickup
- [x] NPC/player Rigidbody2D stability improved with Continuous + Interpolate
- [x] FluidClimb grip detection and moving-platform jump reset fixed

### Documentation
- [x] CHANGELOG.md v0.8.1 entry
- [x] Manager, event, skill, player, level object, architecture, active context, progress, and error-log docs updated

---

## v0.8.0 闁?Architecture Refactor (2026-05-25)

### Architecture Overhaul
- [x] Major directory restructure: flat structure 闁?modular directory tree
- [x] Player: `player/` 闁?`Player/Core/`, `Player/Skills/`, `Player/Body/`, `Player/Physics/`
- [x] Managers: `Manager/` 闁?`Managers/` (plus new S_ManagerRoot)
- [x] NPCs: `Npcs/` 闁?`NPCs/Core/`, `NPCs/Combat/`, `NPCs/Dialogue/`, `NPCs/Sensors/`, `NPCs/Spawning/`
- [x] Level: `LevelCon/` 闁?`Level/Interactables/`, `Level/Platforms/`, `Level/Resources/`, `Level/Sections/`, `Level/Zones/`
- [x] Tools: `tools/` 闁?`Tools/`
- [x] New: `Core/Events/`, `Camera/`, `Input/`, `Systems/Suspicion/`

### Interface Abstraction
- [x] IPlayerActor interface (Rigidbody, Collider, BodyTransform, IsFluidForm, IsParalyzed, Teleport, SetMovementLocked, etc.)
- [x] S_PlayerLookup static utility (TryGet from Collider2D/Collision2D, TryGetActive, IsPlayer)
- [x] S_Player implements IPlayerActor
- [x] NPC/Level systems now use IPlayerActor instead of direct S_Player references

### Skill Controller Extraction
- [x] S_PlayerSkillController extracted from S_Player
- [x] Owns sprint charge state machine (BeginSprintCharge, FixedTickSprintCharge, ReleaseSprintCharge, CancelSprintCharge)
- [x] Owns camera control (BeginCameraControl, EndCameraControl, CameraControlTick)
- [x] Initialized via injection from S_Player

### Manager Root
- [x] S_ManagerRoot: single persistent ManagerRoot.prefab root
- [x] AttachPersistent retained only as compatibility path; manager lifecycle now uses prefab children
- [x] GetOrCreateChild / GetOrCreateComponent helpers
- [x] RuntimeInitializeOnLoadMethod reset for editor domain reload

### Scene Checkpoint
- [x] S_SceneCheckpointTracker: per-scene checkpoint/respawn tracker
- [x] Auto-creates via [RuntimeInitializeOnLoadMethod]
- [x] Listens to OnSpawnPointChanged, OnPlayerDied, OnGameRestart
- [x] Uses IPlayerActor.Teleport for respawn

### S_GameEvent Expansion
- [x] Scene management: OnStartFreshGameRequested, OnReturnToStartMenuRequested, OnSceneLoadRequested, OnGameplayInputEnabledRequested, OnLevelExitRequested
- [x] Spawn point: OnSpawnPointChanged (replaces reNewSpwnPoint)
- [x] Volume: OnBgmVolumeChangeRequested, OnSfxVolumeChangeRequested
- [x] Suspicion refinement: OnSuspicionValueChanged, OnSuspicionChangeRequested, OnHiddenSuspicionDecayRequested, OnPlayerHiddenChangeRequested, OnPlayerHiddenChanged, OnSuspicionResetRequested

### Documentation
- [x] Updated memory-bank/activeContext.md
- [x] Updated memory-bank/progress.md
- [x] Updated Architecture.md (v0.8.1 diagrams/notes)
- [x] Updated CHANGELOG.md

## v0.7.2 闁?Key & Exit Gate System, UI Fixes (2026-05-15)

### New Features
- [x] Key & Exit Gate System (S_Key + S_ExitGate + S_GameEvent events)
- [x] S_UIManager key count HUD

### Bug Fixes
- [x] Controls Mapping Panel layout fix (anchor-based positioning)

### Documentation
- [x] Level_Objects_Design.md: 閹?1 Key & Exit Gate System
- [x] Game_Event_System_Design.md: Key & Gate events

## v0.7.1 闁?Documentation Sync & Platform Cable (2026-05-15)

### New Features
- [x] Dual Platform Cable (S_PlatformCableVisual)

### Documentation
- [x] Full sync of all 9 design documents against 40+ source files

## v0.7.0 闁?Sprint Charge, NPC Jumping & Wave Spawner (2026-05-13)

### Sprint Charge System
- [x] Hold-to-charge sprint with buffer (0.15s quick-tap for instant dash)
- [x] Three-stage size scaling with shake transition effects
- [x] Shared energy drain is now the main sprint limiter; old stage cooldowns remain only as legacy tuning data

### NPC Jumping System
- [x] Predictive jump: wall detection, gap detection, player-above detection
- [x] Dynamic jump force and horizontal boost
- [x] Air control factor (50%)

### NPC Wave Spawner
- [x] S_NPCWaveSpawner: camera-edge spawning, 30s interval, cleanup

## Previous Versions
See CHANGELOG.md for v0.1.0 through v0.6.3 history.
