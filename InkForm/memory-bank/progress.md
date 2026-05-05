# Project Progress

## Completed
- [x] Player Controller (S_Player, S_CameraMove)
- [x] Skill System (S_SkillBase, S_SkillTree, S_Soild_sprint, S_fluid_climb)
- [x] Game Event Bus (S_GameEvent)
- [x] Manager Systems (S_GameManager, S_UIManager)
- [x] Level Objects (S_BreakableBlock, S_Checkpoint, S_Pipline, S_MoveBlock)
- [x] Utility Scripts (S_coleve, S_setTrigger)
- [x] Moving Platform Component (S_MovingPlatform) — Reveal/Hide API with delta transfer
- [x] Level Section System (S_LevelSection, S_LevelSectionController, S_SectionGoal) — v0.4.0 dual triggers
- [x] Section system bug fixes (initialized guard, Y-only movement, World Position Anchor)
- [x] NPC Guard System (S_NPCbase, S_NPCEnemy, S_EMProjectile) — v0.5.0
- [x] Suspicion System (S_SuspicionSystem) — v0.5.0
- [x] Hide Mechanic (S_HideSpot with static bridge) — v0.5.0
- [x] Sprint Stun (OverlapCircleAll on enemy layer) — v0.5.0
- [x] Audio System (S_AudioManager + GameEvent SFX events + Player SFX hooks)
- [x] Error handling infrastructure (error-log.md, cross-reference workflow)
- [x] All 9 system design documents + CHANGELOG
- [x] Project context management (.clinerules, memory-bank, .cline/skills)

## In Progress
- [ ] Unity Editor: layer setup (Enemy Layer 9, Projectile Layer 10, Collision Matrix)
- [ ] Unity Editor: Section Prefab assembly with NPC guards and hide spots

## TODO
- [ ] Chapter 2 playtest (guard patrol, suspicion meter, hide, arrest flow)
- [ ] Claw animation system
- [ ] Dialogue system (S_DialogueUI integration)
- [ ] Level design and balancing
- [ ] Audio asset import and assignment

---
*Last updated: 2026-05-06 (v0.5.0)*