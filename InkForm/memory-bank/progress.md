# InkForm Project Progress

## Current Version
v0.7.0 (Sprint Charge, NPC Jumping & Wave Spawner)

## What Works
- Player Controller (solid/fluid form switching, wall climb, wall jump, paralyze)
- Sprint Charge System (hold-to-charge, buffer, three-stage scaling, stage-based cooldowns)
- Procedural Slime Rendering (body, outline, eye glow, contact-plane fitting, hybrid tail mesh)
- Dynamic Collider (CircleCollider2D default + CapsuleCollider2D for crouch/wall/ceiling)
- Skill System (Sprint charge, FluidClimb, Skill Tree structure)
- Input Binding System (S_InputBindingManager with runtime rebinding + PlayerPrefs persistence)
- Level Objects (breakable blocks, checkpoints, moving platforms, pipelines, jump pads, doors, button doors)
- Moving Platforms (delta displacement transfer)
- Game Event Bus (all major events wired)
- Manager Systems (GameManager, UIManager with runtime controls menu, AudioManager)
- Level Section System (dual triggers, section-level movement)
- NPC Guard System (5-state: Patrol/Chase/Aim/Attack/Arrest/Stunned, Rigidbody2D optional)
- NPC Jumping System (predictive jump with wall/gap/player-above detection)
- NPC Spawner Tool (S_NPCSpawnerTool for inspector-driven spawning)
- NPC Wave Spawner (S_NPCWaveSpawner for runtime camera-edge spawning)
- NPC Camera (S_NPCCamera)
- NPC Dialogue & Story (S_NPCDialogue, S_NPCStory)
- Suspicion System (0-100 meter, 3-tier thresholds)
- Hide Mechanic (S_HideSpot with static PlayerHidden bridge + SetMovementLocked)
- Sprint Stun (Physics2D.OverlapCircleAll on enemy layer)
- Audio System (BGM/SFX via S_AudioManager + S_GameEvent events)
- Narrative System (Characters, Story Outline, World Overview, Willard Protocol)
- Player Movement Lock API (SetMovementLocked for S_HideSpot integration)

## v0.7.0 — Sprint Charge, NPC Jumping & Wave Spawner (2026-05-13)

### Sprint Charge System
- [x] Hold-to-charge sprint with buffer (0.15s quick-tap for instant dash)
- [x] Three-stage size scaling with shake transition effects
- [x] Stage-based cooldowns (0.1s/0.5s/1.0s)
- [x] Sprint direction uses release-time facing
- [x] Eyes follow velocity during charge (not frozen)
- [x] Procedural renderer freezes into perfect circle during charge
- [x] Dynamic collider scales with charge stage
- [x] Low-friction ball material for rolling during charge
- [x] Player can move, jump, grip during charge
- [x] minSprintSpeed guarantee for quick-tap

### NPC Jumping System
- [x] Predictive jump: wall detection via forward raycast
- [x] Gap detection via multi-step ground scanning
- [x] Player-above detection for vertical chase
- [x] Dynamic jump force and horizontal boost
- [x] Air control factor (50%) for natural jump arcs
- [x] Works with both Rigidbody and Transform movement modes

### NPC Wave Spawner
- [x] S_NPCWaveSpawner: spawns NPCs at camera edges every 30s (configurable)
- [x] Inspector camera reference (falls back to Camera.main)
- [x] Ground detection via raycast
- [x] Automatic cleanup of distant NPCs
- [x] Gizmo visualization

### Bug Fixes
- [x] Fixed SetMovementLocked missing from S_Player (S_HideSpot compile error)
- [x] movementLocked properly blocks jumping, gripping, and movement

### Documentation
- [x] Updated CHANGELOG.md with v0.7.0 entry
- [x] Updated Player_Controller_Design.md (Section 9: Sprint Charge)
- [x] Updated Skill_System_Design.md (Section 8: Sprint Charge Parameters)
- [x] Updated NPC_System_Design.md (Sections 8-9: Jumping + Wave Spawner)
- [x] Updated Manager_Systems_Design.md (Section 8: Movement Lock API)
- [x] Updated memory-bank files

## Previous Versions
See CHANGELOG.md for v0.1.0 through v0.6.3 history.