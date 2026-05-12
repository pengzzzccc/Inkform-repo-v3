# Active Context

## Active Work
- **v0.6.3**: Hybrid Slime Tail — separate circle-tail mesh with external tangent bridge points
- **v0.6.2**: Dynamic CapsuleCollider2D — crouch horizontal capsule, wall climb vertical capsule, ceiling flatten
- **v0.6.1**: Procedural Slime Rendering — runtime body/outline/eye meshes, contact-plane fitting, grip buffer snapping
- **v0.6.0**: Input Binding UI (`S_InputBindingManager`) + Rigidbody-Free NPC Knockback + `S_NPCSpawnerTool`
- NPC Guard System: 5-state state machine with optional Rigidbody2D (Transform-based fallback)
- Suspicion System: 0-100 meter with 3-tier thresholds
- Hide Mechanic: `S_HideSpot` with static `PlayerHidden` bridge
- Narrative system: Story outline, character profiles, world overview, Willard Protocol

## Recent Decisions
- **Input Architecture** (v0.6.0): `S_InputBindingManager` singleton replaces per-script `new InputSystem_Actions()`. S_Player now gets actions via `S_InputBindingManager.Instance.Actions`. Binding overrides persisted via PlayerPrefs.
- **NPC Rigidbody Optional** (v0.6.0): RigidBody2D made optional for NPC enemies. Transform-based movement with collider casts for horizontal movement, falling, ground snapping, and obstacle blocking. Sprint knockback works without Rigidbody.
- **Procedural Rendering** (v0.6.1): `S_PlayerProceduralRenderer` generates body, outline, eye glow, and white eye meshes at runtime. Contact-plane fitting pushes slime boundary outside floors/walls/ceilings. Rounded-triangle contact shaping.
- **Dynamic Collider** (v0.6.2): `S_PlayerDynamicCollider` switches between CircleCollider2D (default) and CapsuleCollider2D (crouch/wall/ceiling). Smooth size and offset interpolation.
- **Hybrid Tail** (v0.6.3): Separate circle-tail mesh with external tangent bridge points between body circle and tail circle. Light body lag preserved.
- **Player root+body pattern**: All Transform references must use `GetBodyTransform()`. Root is an anchor with no movement.
- **State machine rule**: All state transitions MUST go through `EnterState()` — never set `currentState` directly.
- **Event chain safety**: `HandleArrest` does NOT auto-restart; death UI shown via `PlayerDied → ShowUI()`.

## Key Architecture Patterns
- **S_InputBindingManager**: Singleton, DontDestroyOnLoad. Shares `InputSystem_Actions`, saves/loads binding overrides, coordinates interactive rebinding. Events: `BindingsChanged`.
- **S_PlayerProceduralRenderer**: Attached to body child. Generates mesh geometry (body, outline, eye, contact fill). Contact-plane fitting with rounded-triangle shaping.
- **S_PlayerDynamicCollider**: Attached to body child. Switches between Circle/Capsule colliders based on movement state. `GetCollider()` returns the active one.
- **S_NPCEnemy**: Rigidbody2D is optional. Transform-based movement with `Physics2D.BoxCast` for ground detection and obstacle blocking.
- **Audio System**: `S_AudioManager` singleton with BGM/SFX. Events via `S_GameEvent.PlaySFX()`/`BGMChange()`.

## Blocked
- None

## Next Steps
- Playtest Chapter 2 with full guard + suspicion + hide mechanics
- Playtest procedural rendering across different form transitions
- Unity Editor: build Section Prefabs with NPC guards and hide spots
- Unity Editor: configure NPC spawner tools in scenes
- Claw animation system
- Dialogue system integration (S_NPCDialogue exists)
- Level design: design new levels with S_ButtonDoor, S_Door, S_JumpPad

## Completed (v0.6.x)
- [x] v0.6.3: Hybrid slime tail rendering (external tangent bridge mesh)
- [x] v0.6.2: Dynamic CapsuleCollider2D (crouch/wall/ceiling modes + smooth interpolation)
- [x] v0.6.1: Procedural slime rendering (body/outline/eye/contact-fill meshes)
- [x] v0.6.1: Dynamic CircleCollider2D support
- [x] v0.6.1: Grip buffer snapping for fluid climb
- [x] v0.6.0: S_InputBindingManager singleton (shared actions + persistence)
- [x] v0.6.0: Runtime-generated controls menu in S_UIManager
- [x] v0.6.0: Gamepad menu support (selected state, Cancel/Back)
- [x] v0.6.0: S_NPCSpawnerTool for inspector-driven NPC spawning
- [x] v0.6.0: Rigidbody2D optional for NPC enemies (Transform-based fallback)
- [x] v0.6.0: Sprint knockback without Rigidbody
- [x] New level objects: S_ButtonDoor, S_Door, S_JumpPad
- [x] NPC scripts: S_NPCCamera, S_NPCDialogue, S_NPCStory
- [x] Player: S_PlayerDynamicCollider, S_PlayerProceduralRenderer
- [x] Design docs: Characters, Narrative, Story Outline, Willard Protocol, World Overview, Procedural Rendering, Narrative Index

## Completed (Previous Versions)
- [x] v0.5.0: NPC Guard System (S_NPCbase + S_NPCEnemy + S_EMProjectile)
- [x] v0.5.0: Suspicion System (S_SuspicionSystem singleton)
- [x] v0.5.0: Hide Mechanic (S_HideSpot with static bridge)
- [x] v0.5.0: Sprint Stun (OverlapCircleAll on enemy layer)
- [x] v0.5.0: 4 root-vs-body-transform bugs fixed
- [x] v0.5.0: Arrest flow bug fixed (EnterState pattern)
- [x] v0.4.1: World Position Anchor for S_SectionGoal triggers
- [x] v0.4.0: Section System overhaul (dual triggers, section-level movement)
- [x] v0.3.x: Player controller, skill system, level objects, moving platforms, game event bus