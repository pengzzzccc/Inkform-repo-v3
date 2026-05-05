# Current Focus

## Active Work
- NPC system implemented: S_NPCbase + 4 subclasses + S_SuspicionSystem
- Audio system integrated: S_AudioManager (BGM + SFX), S_GameEvent audio events, S_Player SFX hooks
- Section system runtime bug fixes complete
- Known fixes: initialized guard (Start order), Y-axis movement (horizontal drift), World Position Anchor (trigger fix)

## Recent Decisions
- NPC system: S_NPCbase base class with OnInteract(), SetActive(), FlipSprite(), DistanceToPlayer()
- S_NPCEnemy: Patrol/Chase/Arrest state machine (skeleton), listens to SuspicionChanged + ArrestTriggered
- S_NPCDialogue: Linear + Branching modes, StoryTrigger events for dialogue lifecycle
- S_NPCStory: K-01 fixed patrol (complete), mimic system placeholder
- S_NPCCamera: Cone detection + cooldown (complete), triggers SuspicionChanged + AlertTriggered
- S_SuspicionSystem: 0–100 meter, 3-tier thresholds, 2 arrest triggers (suspicion max / all missions complete)
- 5 new S_GameEvent events: OnNPCInteract, OnSuspicionChanged, OnAlertTriggered, OnArrestTriggered, OnStoryTrigger
- Audio: SFX routed through S_GameEvent for decoupling; BGM/SFX volume controlled via Inspector Range sliders
- Section only moves on Y axis, X/Z keeps section's own position
- Initialized guard: Reveal/Hide returns early when !initialized
- Design documents and CHANGELOG only updated before major version pushes
- Errors logged to memory-bank/error-log.md, not modifying design docs directly

## Blocked
- None

## Next Steps
- S_NPCEnemy full patrol/chase/arrest movement logic
- S_NPCDialogue dialogue UI integration (need S_DialogueUI system)
- S_NPCStory mimicry system
- S_NPCCamera light colour visual feedback
- Import audio assets (wav/mp3/ogg) and assign to Player (jumpClip, formSwitchClip) and AudioManager (bgmClip)
- Unity test audio playback
- Build Section Prefab (Unity Editor assembly)
- Claw animation system

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
- [x] NPC system implemented (S_NPCbase + 4 subclasses + S_SuspicionSystem + GameEvent extensions)
- [x] NPC_System_Design.md created
- [x] Audio system integration (S_AudioManager + S_GameEvent events + S_Player hooks)
- [x] Code review and bug fixes (5 critical + quality improvements)
- [x] Documentation comments added and removed
- [x] Moving platform component created
- [x] Level section system implemented
- [x] Section system overhaul (v0.4.0): dual triggers, section-level movement
- [x] World Position Anchor for S_SectionGoal triggers (v0.4.1)
- [x] Error handling infrastructure (error-log.md, cross-reference workflow)
- [x] S_LevelSection horizontal drift fix (Y-axis only movement)
- [x] S_LevelSection initialized guard fix
- [x] .clinerules → English conversion
- [x] Skill docs → English conversion
- [x] Unity-dev skill → English conversion
- [x] Project_Prompt design documents enhanced with detail
- [x] Language rule: all communication in English
