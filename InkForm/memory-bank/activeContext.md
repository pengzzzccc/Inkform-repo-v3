# Active Context

## Current Version
v0.7.0 (Sprint Charge, NPC Jumping & Wave Spawner)

## Recent Changes (2026-05-13)
- **Sprint Charge System**: Hold-to-charge sprint with buffer, three-stage scaling, stage-based cooldowns
- **NPC Jumping**: Predictive jump with wall/gap/player-above detection, air control
- **NPC Wave Spawner**: Runtime NPC generation at camera edges
- **S_Player Movement Lock**: SetMovementLocked API for S_HideSpot

## Active Systems
- Sprint Charge (S_Player + S_Soild_sprint + S_PlayerProceduralRenderer + S_PlayerDynamicCollider)
- NPC Jump (S_NPCEnemy: EvaluateJump + FindLandingSpot + CalculateJumpParameters + ExecuteJump)
- NPC Wave Spawner (S_NPCWaveSpawner)
- Movement Lock (S_Player.SetMovementLocked)

## Key Design Decisions
- Sprint direction uses release-time facing (no initial direction lock)
- Eyes follow velocity during charge (not frozen)
- Quick-tap buffer (0.15s) for instant dash
- Stage-based cooldowns (0.1s/0.5s/1.0s) instead of flat cooldown
- NPC jump uses predictive landing spot calculation
- NPC wave spawner uses Inspector camera reference

## Pending
- Unity Editor testing of all new systems
- Balance tuning for sprint charge parameters
- Balance tuning for NPC jump parameters
- Balance tuning for wave spawner intervals

## Documentation Updated
- [x] CHANGELOG.md (v0.7.0)
- [x] Player_Controller_Design.md (Section 9: Sprint Charge)
- [x] Skill_System_Design.md (Section 8: Sprint Charge Parameters)
- [x] NPC_System_Design.md (Sections 8-9: Jumping + Wave Spawner)
- [x] Manager_Systems_Design.md (Section 8: Movement Lock API)
- [x] memory-bank/activeContext.md
- [x] memory-bank/progress.md