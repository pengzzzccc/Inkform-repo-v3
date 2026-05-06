# Active Context

## Active Work
- NPC Guard System implemented: S_NPCbase, S_NPCEnemy, S_EMProjectile
- Suspicion System implemented: S_SuspicionSystem singleton with 3-tier thresholds
- Hide Mechanic implemented: S_HideSpot with static PlayerHidden bridge
- Sprint Stun: S_Soild_sprint now uses OverlapCircleAll on enemy layer
- 4 bugs fixed in v0.5.0 (root vs body transform, HandleGameRestart, OverlapCircle center)
- Arrest flow bug fixed: NPC stuck red + death UI now shows (EnterState pattern fix + HandleArrest decoupling)

## Recent Decisions
- NPC system: 5-state state machine (Patrol / Chase / Attack / Arrest / Stunned)
- S_NPCEnemy: ChaseRange / AttackRange fields, EM projectile attack, waypoint patrol
- S_EMProjectile: fired during Attack state, applies paralyze on player contact
- S_SuspicionSystem: 0-100 meter, AddSuspicion/SetSuspicion/CompleteMission API
- Suspicion tiers: Normal 0-33 / Elevated 34-66 / Critical 67-99 + Arrest at 100
- Arrest triggers: suspicion=100 OR all 3 missions complete
- Hide: press E to toggle hide in cabinets/pillars; reset gravity, hide sprite/collider
- Static bridge pattern: S_HideSpot.PlayerHidden ↔ S_NPCEnemy reads it
- All position calculations must use body Transform, not root Transform
- **State machine rule**: All state transitions MUST go through `EnterState()` — never set `currentState` directly. EnterState handles color update, timer reset, and state-specific initialization.
- **Arrest flow**: `TriggerArrest()` → `EnterState(Disabled)` + `PlayerDied()` → death UI shown. `HandleArrest` only resets suspicion, does NOT auto-restart (avoids race with ShowUI/HideUI event chain).
<task_progress>
- [x] 分析根因
- [x] TriggerArrest 改为 EnterState + PlayerDied
- [x] HandleGameStart/HandleGameRestart 统一走 EnterState(State.Patrol)
- [x] HandleArrest 移除 GameReStart 避免覆盖死亡UI
- [x] 提交并推送
- [x] 更新 error-log.md
- [x] 更新 activeContext.md
- [ ] 更新 progress.md
</task_progress>

## Blocked
- None

## Next Steps
- Unity Editor: assign NPC prefabs to Enemy layer (User Layer 9)
- Unity Editor: assign EMProjectile to Projectile layer (User Layer 10)
- Unity Editor: configure Physics2D Layer Collision Matrix
- Unity Editor: build Section Prefabs with NPC guards and hide spots
- Unity Editor: assign audio clips to Inspector fields
- Playtest Chapter 2 with full guard + suspicion + hide mechanics
- Claw animation system
- Dialogue system integration

---

## Audio System — Usage Quick Reference
```
S_AudioManager (attached to any GameObject, one per scene)
  ├── Inspector: bgmClip, bgmVolume (0-1), sfxVolume (0-1)
  ├── S_GameEvent.PlaySFX(clip) → sfxSource.PlayOneShot()
  ├── S_GameEvent.BGMChange(clip) → bgmSource.Play() + loop
  ├── StopBGM() — public, call directly S_AudioManager.Instance.StopBGM()
  └── Startup: auto-plays bgmClip if assigned in Inspector

S_Player (SFX trigger points):
  ├── jumpClip → fired on Jump() successful AddForce
  ├── formSwitchClip → fired on SetForm() when form actually changes
  └── Both fields need clips assigned in Inspector
```

---

## Completed (Previous Sessions)
- [x] NPC Guard System (S_NPCbase + S_NPCEnemy + S_EMProjectile)
- [x] Suspicion System (S_SuspicionSystem singleton)
- [x] Hide Mechanic (S_HideSpot with static bridge)
- [x] Sprint Stun (OverlapCircleAll on enemy layer)
- [x] 4 root-vs-body-transform bugs fixed
- [x] NPC_System_Design.md created
- [x] Suspicion_System_Design.md created
- [x] CHANGELOG updated to v0.5.0
- [x] Audio system integration (S_AudioManager + S_GameEvent events + S_Player hooks)
- [x] Section system overhaul (v0.4.0): dual triggers, section-level movement
- [x] World Position Anchor for S_SectionGoal triggers (v0.4.1)
- [x] Error handling infrastructure (error-log.md, cross-reference workflow)
- [x] S_LevelSection horizontal drift fix
- [x] S_LevelSection initialized guard fix
- [x] All design documents created (8 docs + CHANGELOG)
- [x] Code review and bug fixes (5 critical + quality improvements)
- [x] Moving platform component with delta transfer
- [x] Player controller, skill system, game event bus
- [x] Manager systems, level objects, utility scripts