# Project Progress

## Overall Status
Pre-Production / Chapter 2 Development

## Current Version
v0.5.0 (NPC Guard System + Suspicion + Hide Mechanics)

## What Works
- Player Controller (solid/fluid form switching, wall climb, wall jump)
- Skill System (Sprint stun on NPCs, Skill Tree structure)
- Level Objects (breakable blocks, checkpoints, moving platforms, pipelines)
- Moving Platforms (delta displacement transfer)
- Game Event Bus (all major events wired)
- Manager Systems (GameManager, UIManager, AudioManager)
- Level Section System (dual triggers, section-level movement)
- NPC Guard System (5-state state machine: Patrol/Chase/Aim/Attack/Arrest/Stunned)
- Suspiccion System (0-100 meter, 3-tier thresholds)
- Hide Mechanic (S_HideSpot with static PlayerHidden bridge)
- Sprint Stun (Physics2D.OverlapCircleAll on enemy layer)
- Audio System (BGM/SFX via S_AudioManager + S_GameEvent events)

## 2026-05-07: NPC Physics Movement Refactor

### Completed
- [x] S_NPCbase.Awake(): Rigidbody2D.bodyType = Dynamic + FreezeRotation
- [x] S_NPCEnemy: MoveHorizontally(speedX) helper — grounded velocity vs airborne freeze
- [x] S_NPCEnemy.UpdateGroundCheck() — Collider2D.GetContacts + normal-angle threshold
- [x] S_NPCEnemy: all 6 movement states use Rigidbody2D.velocity (no more transform.Translate)
- [x] S_NPCEnemy idle wandering: random walk direction + wanderRadius boundary constraint
- [x] New Inspector fields: gravityScale, groundLayer, groundNormalThreshold, wanderRadius, wanderWalkTimeMin/Max, wanderPauseTimeMin/Max
- [x] NPC_System_Design.md updated with Physics Movement section + new Inspector fields + error #7
- [x] memory-bank/activeContext.md updated
- [x] memory-bank/progress.md updated

### Pending (Unity Editor)
- [ ] Assign Rigidbody2D and Collider2D to all NPC prefabs
- [ ] Configure gravityScale, groundLayer, wander fields in Inspector for each NPC
- [ ] Assign NPC prefabs to Enemy layer (User Layer 9)
- [ ] Configure Physics2D Layer Collision Matrix
- [ ] Playtest: verify NPC physics movement, ground detection, idle wandering

## Previous Milestones

### v0.5.0 — NPC Guard System
- [x] S_NPCbase: identity, interaction, sprite/Rigidbody2D refs
- [x] S_NPCEnemy: 5-state state machine, waypoint patrol, chase/attack/arrest
- [x] S_EMProjectile: paralyze on player contact
- [x] S_SuspicionSystem: 0-100 meter, 3-tier thresholds
- [x] S_HideSpot: static PlayerHidden bridge to NPC state machine
- [x] Sprint Stun: Physics2D.OverlapCircleAll on enemy layer
- [x] NPC_System_Design.md, Suspicion_System_Design.md created
- [x] 4 root-vs-body-transform bugs fixed
- [x] Arrest flow bug fixed (EnterState pattern)

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