# Project Progress

## Overall Status
Pre-Production / Chapter 2 Development

## Current Version
v0.6.3 (Hybrid Slime Tail + Dynamic CapsuleCollider + Procedural Rendering + Input Binding UI)

## What Works
- Player Controller (solid/fluid form switching, wall climb, wall jump, paralyze)
- Procedural Slime Rendering (body, outline, eye glow, contact-plane fitting, hybrid tail mesh)
- Dynamic Collider (CircleCollider2D default + CapsuleCollider2D for crouch/wall/ceiling)
- Skill System (Sprint stun on NPCs, Skill Tree structure)
- Input Binding System (S_InputBindingManager with runtime rebinding + PlayerPrefs persistence)
- Level Objects (breakable blocks, checkpoints, moving platforms, pipelines, jump pads, doors, button doors)
- Moving Platforms (delta displacement transfer)
- Game Event Bus (all major events wired)
- Manager Systems (GameManager, UIManager with runtime controls menu, AudioManager)
- Level Section System (dual triggers, section-level movement)
- NPC Guard System (5-state: Patrol/Chase/Aim/Attack/Arrest/Stunned, Rigidbody2D optional)
- NPC Spawner Tool (S_NPCSpawnerTool for inspector-driven spawning)
- NPC Camera (S_NPCCamera)
- NPC Dialogue & Story (S_NPCDialogue, S_NPCStory)
- Suspicion System (0-100 meter, 3-tier thresholds)
- Hide Mechanic (S_HideSpot with static PlayerHidden bridge)
- Sprint Stun (Physics2D.OverlapCircleAll on enemy layer)
- Audio System (BGM/SFX via S_AudioManager + S_GameEvent events)
- Narrative System (Characters, Story Outline, World Overview, Willard Protocol)

## 2026-05-12: v0.6.3 — Hybrid Slime Tail

### Completed
- [x] Hybrid tail model: separate circle-tail mesh with external tangent bridge points
- [x] Tail mesh uses body circle + smaller tail circle with bridge geometry
- [x] Direct tail size, distance, bridge, and follow-speed parameters

## 2026-05-12: v0.6.2 — Dynamic Capsule Collider

### Completed
- [x] CapsuleCollider2D support: crouch (horizontal), wall climb (vertical), ceiling (flattened)
- [x] Smooth size and offset interpolation between modes
- [x] S_Player.GetCollider() returns active dynamic collider
- [x] Grip buffer casts, surface classification follow active collider
- [x] Renderer adds colliderShapeFollow for partial capsule proportion following
- [x] Crouch capsule anchors from bottom edge with input/contact smoothing

## 2026-05-12: v0.6.1 — Procedural Slime Rendering

### Completed
- [x] S_PlayerProceduralRenderer: runtime body, outline, eye glow, white eye meshes
- [x] Contact-plane visual fitting (boundary pushed outside floors/walls/ceilings)
- [x] Rounded-triangle contact shaping for smooth weighted silhouette
- [x] Dynamic CircleCollider2D via S_PlayerDynamicCollider
- [x] Grip buffer snapping for fluid climb pre-contact
- [x] Direct Ceiling state entry when gripping + ceiling contact
- [x] JumpPad force range and force-to-color visualization
- [x] Reduced slime tail size, maxTailStretch cap, motionLag tuning
- [x] Contact-fill mesh to cover gaps from contact-plane skin
- [x] Guarded S_UIManager against duplicate DontDestroyOnLoad registration

## 2026-05-10: v0.6.0 — Input Binding UI & Rigidbody-Free NPC

### Completed
- [x] S_InputBindingManager: singleton, shared InputSystem_Actions, binding persistence via PlayerPrefs
- [x] S_UIManager: runtime-generated controls menu for keyboard/mouse/gamepad binding changes
- [x] Gamepad menu support: selected UI state, Cancel/Back behavior
- [x] S_NPCSpawnerTool: inspector-driven NPC spawning, count adjustment, generation, cleanup
- [x] Rigidbody2D optional for NPC enemies — Transform-based movement with collider casts
- [x] Sprint knockback support without Rigidbody2D
- [x] Updated sprint hit detection to resolve S_NPCEnemy via GetComponentInParent
- [x] Fixed no-Rigidbody NPC falling through ground (manual ground collision + vertical resolution)
- [x] New level objects: S_ButtonDoor, S_Door, S_JumpPad
- [x] NPC scripts: S_NPCCamera, S_NPCDialogue, S_NPCStory

## 2026-05-07: v0.5.0 — NPC Physics Movement Refactor

### Completed
- [x] S_NPCbase.Awake(): Rigidbody2D.bodyType = Dynamic + FreezeRotation
- [x] S_NPCEnemy: MoveHorizontally(speedX) helper — grounded velocity vs airborne freeze
- [x] S_NPCEnemy.UpdateGroundCheck() — Collider2D.GetContacts + normal-angle threshold
- [x] S_NPCEnemy: all 6 movement states use Rigidbody2D.velocity (no more transform.Translate)
- [x] S_NPCEnemy idle wandering: random walk direction + wanderRadius boundary constraint
- [x] Arrest flow bug fixed: EnterState pattern + HandleArrest decoupling
- [x] NPC_System_Design.md updated with Physics Movement section
- [x] Suspicion_System_Design.md created

### Pending (Unity Editor)
- [ ] Build Section Prefabs with NPC guards and hide spots
- [ ] Configure NPC spawner tools in scenes
- [ ] Playtest: procedural rendering across form transitions
- [ ] Playtest Chapter 2 with full guard + suspicion + hide mechanics

## Previous Milestones

### v0.4.x — Section System & Audio
- [x] S_LevelSection dual triggers + section-level movement
- [x] S_AudioManager with BGM/SFX events
- [x] Error handling infrastructure

### v0.3.x — Core Mechanics
- [x] Player controller (solid/fluid)
- [x] Skill system
- [x] Level objects
- [x] Moving platforms with delta transfer
- [x] Game event bus