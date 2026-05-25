# Active Context

## Current Version
v0.8.1 (Gameplay UX, Energy, Scene Flow, ManagerRoot Hardening)

## Recent Changes (2026-05-25)
- **ManagerRoot Single Persistence**: `ManagerRoot.prefab` is now the only `DontDestroyOnLoad` object; child managers no longer self-create, self-reparent, or call `AttachPersistent()` in normal lifecycle.
- **UIManager Migration**: `S_UIManager` now lives under `ManagerRoot.prefab`; standalone scene UIManager instances were removed.
- **Scene Flow**: `S_GameManager` and `S_SceneChangeTrigger` use `S_SceneReference` for Inspector-dragged SceneAssets, with runtime scene path/name fallback and load validation.
- **Scene Transitions**: Scene changes use fade out/load/fade in and optional transition SFX while gameplay input is disabled.
- **Shared Player Energy**: Added `S_PlayerEnergy`, `OnPlayerEnergyChanged`, skill energy thresholds/drain, sprint quick tap cost, and runtime energy UI.
- **Death UI**: Death shows an independent panel with red death count and `back to checkpoint`; checkpoint respawn waits for the button.
- **Gameplay Fixes**: Dropped keys pop out and hover near ground; NPC/player rigidbodies use continuous/interpolated physics; climb grip and moving-platform jump reset were stabilized.

## New Directory Structure
```
Script/
├── Camera/           (S_CameraMove, S_ParallaxLayer)
├── Core/             (S_SceneReference)
│   └── Events/       (S_GameEvent)
├── Input/            (InputSystem_Actions)
├── Level/
│   ├── Interactables/ (BreakableBlock, Checkpoint, ExitGate, HideSpot, Key, SceneCheckpointTracker)
│   ├── Platforms/     (MoveBlock, MovingPlatform, PlatformCableVisual)
│   ├── Resources/     (DroppedResourceItem, DropResourceCounter)
│   ├── Sections/      (LevelSection, LevelSectionController, SectionAlarmEffect, SectionGoal)
│   └── Zones/         (CantClimb)
├── Managers/         (AudioManager, GameManager, InputBindingManager, ManagerRoot, SceneChangeTrigger, StartMenuController, UIManager)
├── NPCs/             (Core, Combat, Dialogue, Sensors, Spawning)
├── Player/
│   ├── Body/          (PlayerDynamicCollider, PlayerProceduralRenderer)
│   ├── Core/          (Player, PlayerContracts)
│   ├── Physics/       (coleve)
│   └── Skills/        (CameraControlSkill, fluid_climb, PlayerEnergy, PlayerSkillController, SkillBase, SkillTree, Soild_sprint)
├── Systems/Suspicion/ (SuspicionSystem)
└── Tools/            (NPCSpawnerTool, PerformanceMonitor, setTrigger)
```

## Key Design Decisions
- **IPlayerActor interface** abstracts player for NPC/Level systems; `S_PlayerLookup.TryGet` resolves player actors from colliders or active instance.
- **S_PlayerSkillController** owns sprint charge and camera control, injected by `S_Player`.
- **ManagerRoot prefab single persistence**: only the root calls `DontDestroyOnLoad`; child managers are authored under the prefab and must not runtime-reparent.
- **S_SceneReference** is the preferred scene configuration path; old string scene names are compatibility fallback only.
- **S_PlayerEnergy** is the shared energy pool for active skills; skill assets configure their own energy costs.
- **S_SceneCheckpointTracker** auto-creates per scene, tracks spawn position, and respawns only on `OnGameRestart` after death UI confirmation.
- Sprint direction uses release-time facing; quick-tap sprint spends `quickTapEnergyCost`.
- NPC/player Rigidbody2D setup uses Continuous + Interpolate for more stable high-speed motion.

## Active Systems
- Player Controller (IPlayerActor, solid/fluid form, wall climb, wall jump, paralyze)
- Sprint Charge (S_PlayerSkillController + S_Soild_sprint + S_PlayerEnergy)
- Camera Control (S_PlayerSkillController + S_CameraControlSkill + S_CameraMove)
- Skill Tree (S_SkillTree under ManagerRoot prefab)
- Shared Player Energy (S_PlayerEnergy + skill asset energy costs + UI energy bar)
- Manager Root (single persistent ManagerRoot.prefab; AttachPersistent compatibility-only)
- Scene Flow (S_SceneReference, transition fade/SFX, validated runtime scene keys)
- Scene Checkpoint (S_SceneCheckpointTracker per-scene auto-creation, respawn on GameReStart)
- Death UI (independent death panel + back to checkpoint flow)
- NPC System (S_NPCEnemy 5-state FSM, continuous/interpolated Rigidbody2D, wave spawner)
- Suspicion System (event-driven via S_GameEvent)
- Level Sections (dual-trigger, section-level movement)
- Key & Exit Gate System (dropped key pop-out/hover + scene progression)

## Pending
- Unity Editor testing of v0.8.1 scene flow and ManagerRoot duplicate behavior
- Balance tuning for energy drain/regen, sprint quick tap cost, NPC jump parameters
- Verify Build Settings / Build Profiles include Start, Playtest1, NPCPlayTestScene, and END

## Documentation Updated
- [x] memory-bank/activeContext.md
- [x] memory-bank/progress.md
- [x] memory-bank/error-log.md
- [x] CHANGELOG.md (v0.8.1 entry)
- [x] Architecture.md
- [x] Manager_Systems_Design.md
- [x] Game_Event_System_Design.md
- [x] Skill_System_Design.md
- [x] Player_Controller_Design.md
- [x] Level_Objects_Design.md


# 关卡游玩流程
- 玩家进入游戏，屏幕缓慢亮起，进入第一个训练关卡，移动教学。- 关卡教学只包含移动和跳跃。
- 然后一道声音说：“现在熟悉操作”。屏幕的右侧弹出一个ui提示，包含所有移动按键。再检测到玩家按下按键之后进入下一个阶段。
- 然后一道声音说：“请在30秒内到达目标地点”。再说的期间，摄像头会移动到目的地然后再回来。
- 如果玩家成功到达，通关；没有成功到达，死亡，重新加载当前关卡。

- 然后进入下一关，冲刺教学。- 关卡包含移动，跳跃和冲刺技能。
- 然后一道声音说：“现在熟悉操作”。屏幕的右侧弹出一个ui提示，包含技能的描述。再检测到玩家按下按键之后进入下一个阶段。
- 然后一道声音说：“请在30秒内到达目标地点”。再说的期间，摄像头会移动到目的地然后再回来。
- 如果玩家成功到达，通关；没有成功到达，死亡，重新加载当前关卡。

- 然后进入下一关，冲刺熟练关卡。- 关卡包含移动，跳跃和冲刺技能。
- 然后一道声音说：“请在30秒内到达目标地点”。再说的期间，摄像头会移动到目的地然后再回来。
- 如果玩家成功到达，通关；没有成功到达，死亡，重新加载当前关卡。

- 然后进入下一关，攀爬教学。- 关卡包含移动，跳跃，冲刺和攀爬技能。
- 然后一道声音说：“现在熟悉操作”。屏幕的右侧弹出一个ui提示，包含技能的描述。再检测到玩家按下按键之后进入下一个阶段。
- 然后一道声音说：“请在30秒内到达目标地点”。再说的期间，摄像头会移动到目的地然后再回来。
- 如果玩家成功到达，通关；没有成功到达，死亡，重新加载当前关卡。

- 然后进入下一关，攀爬熟练。- 关卡包含移动，跳跃，冲刺和攀爬技能。
- 然后一道声音说：“请在30秒内到达目标地点”。再说的期间，摄像头会移动到目的地然后再回来。
- 如果玩家成功到达，通关；没有成功到达，死亡，重新加载当前关卡。

- 然后进入下一关，混合关卡。- 关卡包含移动，跳跃，冲刺和攀爬技能，中间包含一些npc。
- 然后一道声音说：“请在30秒内到达目标地点”。再说的期间，摄像头会移动到目的地然后再回来。
- 如果玩家成功到达，通关；没有成功到达，死亡，重新加载当前关卡。

- 如果玩家到达，游戏结束。

# 加入新功能
- 相机的控制需要稳定一点，将现在的死区控制变成纯y轴死区，如果玩家超过死区，整体向上或者向下移动一段距离，如果玩家持续移动或者持续下落则稳定持续跟踪。给相机加入移动最大限制。再遇到关卡的最边缘的时候停止。
- 给玩家出场添加一个移动，玩家将从一个机器中走出来。最终离开关卡也是进入这个机器。你来决定可以是什么机器。

# 问题修复
- 现在再复活之后不能重启计时，将死亡ui改为重启当前关卡。
- 再所有关卡切换和关卡配置的间隔时间做成可以控制的参数。
- 再相机移动到目标再移动回来之间的这段时间，冷冻玩家输入。

# 问题修复
- 加入一个快速跳过语音的按键。
- 自适应倒数时间音频，自由组合应有的秒数
- start界面，手柄按不到start
