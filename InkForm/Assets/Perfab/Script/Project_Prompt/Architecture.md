# InkForm Architecture

> Last updated: v0.8.0 (2026-05-25) — Architecture Refactor

## 1. System Overview (C4-Style Context Diagram)

```mermaid
graph TB
    subgraph Input["🎮 Input Layer"]
        IBM[S_InputBindingManager<br/>Singleton]
        IA[InputSystem_Actions]
    end

    subgraph Player["🧍 Player System"]
        P[S_Player<br/>Singleton<br/>implements IPlayerActor]
        PContracts[IPlayerActor interface]
        PLookup[S_PlayerLookup<br/>static utility]
        PSC[S_PlayerSkillController]
    end

    subgraph PlayerBody["🫠 Player Body"]
        PDR[S_PlayerDynamicCollider]
        SPPR[S_PlayerProceduralRenderer]
    end

    subgraph Skills["⚡ Skill System"]
        ST[S_SkillTree<br/>Singleton]
        SB[S_SkillBase<br/>ScriptableObject]
        SSprint[S_Soild_sprint]
        SClimb[S_fluid_climb]
        SCCam[S_CameraControlSkill]
    end

    subgraph Camera["📷 Camera System"]
        CM[S_CameraMove]
        NPCCam[S_NPCCamera]
        PL[S_ParallaxLayer]
    end

    subgraph NPC["🤖 NPC System"]
        NPCBase[S_NPCbase]
        NPCE[S_NPCEnemy]
        EMProj[S_EMProjectile]
        NPCWave[S_NPCWaveSpawner]
        NPCDial[S_NPCDialogue]
        NPCStory[S_NPCStory]
    end

    subgraph Suspicion["🔍 Suspicion System"]
        SUS[S_SuspicionSystem]
        HS[S_HideSpot]
    end

    subgraph Level["🗺️ Level System"]
        subgraph Section["Section System"]
            LS[S_LevelSection]
            LSCtrl[S_LevelSectionController]
            SG[S_SectionGoal]
            SAE[S_SectionAlarmEffect]
        end
        subgraph LevelObj["Level Objects"]
            MP[S_MovingPlatform]
            BB[S_BreakableBlock]
            Door[S_Door]
            BDoor[S_ButtonDoor]
            Key[S_Key]
            EG[S_ExitGate]
            JP[S_JumpPad]
            MB[S_MoveBlock]
            Pipe[S_Pipline]
            CP[S_Checkpoint]
            SCT[S_SceneCheckpointTracker]
            DRI[S_DroppedResourceItem]
            DRC[S_DropResourceCounter]
            CC[S_CantClimb]
            PCV[S_PlatformCableVisual]
        end
    end

    subgraph Managers["🏢 Manager Layer"]
        MR[S_ManagerRoot<br/>Singleton<br/>DontDestroyOnLoad]
        GM[S_GameManager<br/>Singleton]
        UI[S_UIManager<br/>Singleton]
        AM[S_AudioManager<br/>Singleton]
    end

    subgraph Events["📡 Event Bus"]
        GE[S_GameEvent<br/>Static<br/>30+ events]
    end

    subgraph Tools["🔧 Tools"]
        NST[S_NPCSpawnerTool]
        PM[S_PerformanceMonitor]
        STR[S_setTrigger]
    end

    subgraph MCTS["🧪 MCTS Testing"]
        MCTSBot[MCTSBotController]
        MCTSGS[MCTSGameState]
        MCTSN[MCTSNode]
        LTM[LevelTestMetrics]
    end

    subgraph SceneCtrl["🎬 Scene Control"]
        SC[S_SceneChangeTrigger]
        SMC[S_StartMenuController]
    end

    %% Manager Root
    MR -->|"AttachPersistent"| GM
    MR -->|"AttachPersistent"| UI
    MR -->|"AttachPersistent"| AM
    MR -->|"AttachPersistent"| IBM

    %% Player architecture
    P -->|"implements"| PContracts
    P -->|"creates"| PSC
    P -->|"manages"| SPPR
    P -->|"manages"| PDR
    P -->|"reads"| IBM
    P -->|"uses"| ST
    ST -->|"contains"| SB
    SB -->|"subclasses"| SSprint
    SB -->|"subclasses"| SClimb
    SB -->|"subclasses"| SCCam
    PSC -->|"delegates"| SSprint
    PSC -->|"delegates"| SCCam
    PSC -->|"modifies"| SPPR
    PSC -->|"modifies"| PDR
    P -->|"controls"| CM

    %% IPlayerActor decoupling
    PLookup -->|"resolves"| PContracts
    NPCE -->|"uses"| PLookup
    HS -->|"uses"| PLookup
    SCT -->|"uses"| PLookup
    SUS -->|"uses"| PLookup

    %% Player ↔ Level
    P -->|"collides"| MP
    P -->|"triggers"| JP
    P -->|"uses"| Door
    P -->|"collects"| Key
    P -->|"enters"| HS
    P -->|"triggers"| CP
    P -->|"pushes"| MB

    %% NPC System
    NPCBase -->|"inherits"| NPCE
    NPCE -->|"spawns"| NPCWave
    NPCE -->|"fires"| EMProj
    NPCDial -->|"triggers"| NPCStory

    %% Events (all systems publish/subscribe through GE)
    P -.->|"publishes"| GE
    NPCE -.->|"publishes"| GE
    SUS -.->|"publishes"| GE
    LS -.->|"publishes"| GE
    Key -.->|"publishes"| GE
    SCT -.->|"subscribes"| GE
    GM -.->|"subscribes"| GE
    UI -.->|"subscribes"| GE
    AM -.->|"subscribes"| GE

    %% MCTS
    MCTSBot -->|"simulates"| P
    MCTSBot -->|"uses"| MCTSGS
    MCTSGS -->|"builds"| MCTSN
    MCTSBot -->|"measures"| LTM

    %% Tools
    NST -->|"spawns"| NPCE
```

## 2. Class Inheritance & Interface Diagram

```mermaid
classDiagram
    direction TB

    class MonoBehaviour {
        <<Unity>>
    }

    class ScriptableObject {
        <<Unity>>
    }

    %% === Interfaces ===
    class IPlayerActor {
        <<interface>>
        +Rigidbody2D Rigidbody
        +Collider2D Collider
        +Transform BodyTransform
        +bool IsFluidForm
        +bool IsParalyzed
        +bool IsSprintMomentumActive
        +bool FacingRight
        +Teleport(Vector2)
        +SetMovementLocked(bool)
        +ApplyParalyze(float, float)
        +ForceSprintBreakthrough(float, float, float)
        +CancelSprintCharge()
    }

    %% === Player ===
    class S_Player {
        +static Instance
        +bool isFluid
        +bool movementLocked
        +SwitchForm()
        +Die()
        +ResetPlayer()
    }

    class S_PlayerLookup {
        <<static>>
        +TryGet(Collider2D, out IPlayerActor)$
        +TryGet(Collision2D, out IPlayerActor)$
        +TryGetActive(out IPlayerActor)$
        +IsPlayer(Collider2D)$
    }

    class S_PlayerSkillController {
        +bool IsSprintCharging
        +bool IsCameraControlActive
        +Initialize(...)
        +BeginSprintCharge()
        +FixedTickSprintCharge()
        +ReleaseSprintCharge()
        +CancelSprintCharge()
        +BeginCameraControl()
        +EndCameraControl()
        +CameraControlTick()
        +HandleCameraControlInput()
    }

    class S_PlayerProceduralRenderer {
        +UpdateBodyShape()
        +UpdateEyeGlow()
        +SetChargeOverride(bool)
    }

    class S_PlayerDynamicCollider {
        +SwitchToCircle()
        +SwitchToCapsule()
        +SetChargeOverride(bool, float)
    }

    %% === Skills ===
    class S_SkillBase {
        <<ScriptableObject>>
        +string skillName
        +bool unlocked
        +Activate()
        +Deactivate()
    }

    class S_SkillTree {
        +static Instance
        +S_SkillBase[] skills
        +GetSprintSkill()
        +GetCameraControlSkill()
        +UnlockSkill()
    }

    class S_Soild_sprint {
        +float MaxChargeTime
        +float BufferTime
        +GetStage(float)
        +GetStageScale(float)
        +ActivateCharge()
        +GetCooldown(int)
    }

    class S_fluid_climb {
        +float climbSpeed
        +StartClimb()
        +StopClimb()
    }

    class S_CameraControlSkill {
        +float BulletTimeScale
        +Activate(S_Player)
    }

    %% === Camera ===
    class S_CameraMove {
        +Transform target
        +BeginManualControl()
        +EndManualControl()
        +ManualControlTick()
    }

    class S_ParallaxLayer {
        +float parallaxFactor
        +UpdatePosition()
    }

    %% === NPC ===
    class S_NPCbase {
        +float health
        +float moveSpeed
        +TakeDamage()
        +Die()
    }

    class S_NPCEnemy {
        +enum State
        +State currentState
        +EvaluateJump()
        +FindLandingSpot()
        +ExecuteJump()
    }

    class S_EMProjectile {
        +float speed
        +OnTriggerEnter2D()
    }

    class S_NPCWaveSpawner {
        +float spawnInterval
        +SpawnWave()
        +CleanupDistant()
    }

    class S_NPCCamera {
        +FollowTarget()
    }

    class S_NPCDialogue {
        +string npcID
        +StartDialogue()
    }

    class S_NPCStory {
        +string triggerID
        +OnStoryTrigger()
    }

    %% === Level Section ===
    class S_LevelSection {
        +int sectionIndex
        +OnPlayerEnter()
        +OnPlayerExit()
    }

    class S_LevelSectionController {
        +S_LevelSection[] sections
        +ActivateSection()
    }

    class S_SectionGoal {
        +int goalIndex
        +CheckGoal()
    }

    class S_SectionAlarmEffect {
        +StartAlarm()
        +StopAlarm()
    }

    %% === Level Objects ===
    class S_MovingPlatform {
        +Vector3[] waypoints
        +MovePlatform()
    }

    class S_BreakableBlock {
        +int hp
        +Break()
    }

    class S_Door {
        +bool isOpen
        +Open()
        +Close()
    }

    class S_ButtonDoor {
        +OnButtonPress()
    }

    class S_Key {
        +int keyID
        +Collect()
    }

    class S_ExitGate {
        +OpenGate()
    }

    class S_HideSpot {
        +EnterHide()
        +ExitHide()
    }

    class S_JumpPad {
        +float jumpForce
        +Bounce()
    }

    class S_MoveBlock {
        +Push()
    }

    class S_Pipline {
        +Transport()
    }

    class S_Checkpoint {
        +Activate()
    }

    class S_SceneCheckpointTracker {
        +trackedScene
        +HandleSpawnPointChanged()
        +HandleRespawnRequested()
    }

    class S_DroppedResourceItem {
        +Collect()
    }

    class S_DropResourceCounter {
        +CheckCount()
    }

    %% === Managers ===
    class S_ManagerRoot {
        +static Instance
        +EnsureExists()$
        +AttachPersistent(Transform)$
        +DestroyDuplicate(MonoBehaviour)$
        +GetOrCreateChild(string)
        +GetOrCreateComponent~T~(string)
    }

    class S_GameManager {
        +static Instance
        +RestartGame()
        +ExitGame()
        +LoadNextLevel()
    }

    class S_UIManager {
        +static Instance
        +ShowControlsMenu()
        +HideControlsMenu()
    }

    class S_AudioManager {
        +static Instance
        +PlaySFX()
        +PlayBGM()
        +StopBGM()
    }

    class S_InputBindingManager {
        +static Instance
        +RebindAction()
        +SaveBindings()
    }

    %% === Systems ===
    class S_SuspicionSystem {
        +float suspicion
        +AddSuspicion()
        +CheckThresholds()
    }

    %% === Events ===
    class S_GameEvent {
        <<static 30+ events>>
        +OnPlayerDied
        +OnGameStart / OnGameRestart
        +OnStartFreshGameRequested
        +OnReturnToStartMenuRequested
        +OnSceneLoadRequested
        +OnGameplayInputEnabledRequested
        +OnLevelExitRequested
        +OnSpawnPointChanged
        +OnSectionStart / OnSectionEnd
        +OnSectionDescentStarted / Completed
        +OnPlaySFX / OnPlaySFXPitched
        +OnBGMChange
        +OnBgmVolumeChangeRequested
        +OnSfxVolumeChangeRequested
        +OnSuspicionChanged
        +OnSuspicionValueChanged
        +OnSuspicionChangeRequested
        +OnHiddenSuspicionDecayRequested
        +OnPlayerHiddenChangeRequested
        +OnPlayerHiddenChanged
        +OnSuspicionResetRequested
        +OnAlertTriggered
        +OnArrestTriggered
        +OnStoryTrigger
        +OnNPCInteract
        +OnKeyCollected
        +OnKeyCountChanged
    }

    %% === Scene ===
    class S_SceneChangeTrigger {
        +string sceneName
        +ChangeScene()
    }

    class S_StartMenuController {
        +StartGame()
        +ExitGame()
    }

    %% === Tools ===
    class S_NPCSpawnerTool {
        +SpawnNPC()
    }

    class S_PerformanceMonitor {
        +LogFPS()
    }

    class S_setTrigger {
        +SetTriggerActive()
    }

    %% === MCTS ===
    class MCTSBotController {
        +Simulate()
        +SelectBestAction()
    }

    class MCTSGameState {
        +Clone()
        +GetLegalActions()
        +SimulateRandom()
    }

    class MCTSNode {
        +int visits
        +float value
        +SelectChild()
        +Expand()
    }

    class LevelTestMetrics {
        +float completionTime
        +int deathCount
        +Record()
    }

    %% === Inheritance ===
    MonoBehaviour <|-- S_Player
    MonoBehaviour <|-- S_PlayerSkillController
    MonoBehaviour <|-- S_SkillTree
    MonoBehaviour <|-- S_PlayerProceduralRenderer
    MonoBehaviour <|-- S_PlayerDynamicCollider
    MonoBehaviour <|-- S_CameraMove
    MonoBehaviour <|-- S_NPCbase
    MonoBehaviour <|-- S_NPCWaveSpawner
    MonoBehaviour <|-- S_NPCCamera
    MonoBehaviour <|-- S_NPCDialogue
    MonoBehaviour <|-- S_NPCStory
    MonoBehaviour <|-- S_EMProjectile
    MonoBehaviour <|-- S_LevelSection
    MonoBehaviour <|-- S_LevelSectionController
    MonoBehaviour <|-- S_SectionGoal
    MonoBehaviour <|-- S_SectionAlarmEffect
    MonoBehaviour <|-- S_MovingPlatform
    MonoBehaviour <|-- S_BreakableBlock
    MonoBehaviour <|-- S_Door
    MonoBehaviour <|-- S_ButtonDoor
    MonoBehaviour <|-- S_Key
    MonoBehaviour <|-- S_ExitGate
    MonoBehaviour <|-- S_HideSpot
    MonoBehaviour <|-- S_JumpPad
    MonoBehaviour <|-- S_MoveBlock
    MonoBehaviour <|-- S_Pipline
    MonoBehaviour <|-- S_Checkpoint
    MonoBehaviour <|-- S_SceneCheckpointTracker
    MonoBehaviour <|-- S_DroppedResourceItem
    MonoBehaviour <|-- S_DropResourceCounter
    MonoBehaviour <|-- S_CantClimb
    MonoBehaviour <|-- S_ParallaxLayer
    MonoBehaviour <|-- S_PlatformCableVisual
    MonoBehaviour <|-- S_ManagerRoot
    MonoBehaviour <|-- S_GameManager
    MonoBehaviour <|-- S_UIManager
    MonoBehaviour <|-- S_AudioManager
    MonoBehaviour <|-- S_InputBindingManager
    MonoBehaviour <|-- S_SuspicionSystem
    MonoBehaviour <|-- S_SceneChangeTrigger
    MonoBehaviour <|-- S_StartMenuController
    MonoBehaviour <|-- S_setTrigger
    MonoBehaviour <|-- S_NPCSpawnerTool
    MonoBehaviour <|-- S_PerformanceMonitor
    MonoBehaviour <|-- MCTSBotController
    MonoBehaviour <|-- MCTSGameState
    MonoBehaviour <|-- LevelTestMetrics

    ScriptableObject <|-- S_SkillBase
    S_SkillBase <|-- S_Soild_sprint
    S_SkillBase <|-- S_fluid_climb
    S_SkillBase <|-- S_CameraControlSkill

    S_NPCbase <|-- S_NPCEnemy
    MCTSNode <|-- MCTSNode : children

    IPlayerActor <|.. S_Player
```

## 3. Event Bus Dependency Diagram (30+ Events)

```mermaid
graph LR
    subgraph Publishers["📤 Event Publishers"]
        P["S_Player"]
        PSC["S_PlayerSkillController"]
        NPCE["S_NPCEnemy"]
        SUS["S_SuspicionSystem"]
        LS["S_LevelSection"]
        LSCtrl["S_LevelSectionController"]
        CP["S_Checkpoint"]
        BB["S_BreakableBlock"]
        Key["S_Key"]
        GM["S_GameManager"]
        NPCDial["S_NPCDialogue"]
        HS["S_HideSpot"]
        SCT["S_SceneCheckpointTracker"]
        SMC["S_StartMenuController"]
    end

    subgraph EventBus["📡 S_GameEvent"]
        direction TB
        E1["OnPlayerDied"]
        E2["OnGameStart / OnGameRestart"]
        E3["OnScoreChanged"]
        E4["OnSkillUsed"]
        E5["OnSpawnPointChanged"]
        E6["OnSectionStart / OnSectionEnd"]
        E7["OnSectionDescentStarted / Completed"]
        E8["OnPlaySFX / OnPlaySFXPitched"]
        E9["OnBGMChange"]
        E10["OnBgmVolumeChangeRequested"]
        E11["OnSfxVolumeChangeRequested"]
        E12["OnStartFreshGameRequested"]
        E13["OnReturnToStartMenuRequested"]
        E14["OnSceneLoadRequested"]
        E15["OnGameplayInputEnabledRequested"]
        E16["OnLevelExitRequested"]
        E17["OnNPCInteract"]
        E18["OnSuspicionChanged"]
        E19["OnSuspicionValueChanged"]
        E20["OnSuspicionChangeRequested"]
        E21["OnHiddenSuspicionDecayRequested"]
        E22["OnPlayerHiddenChangeRequested"]
        E23["OnPlayerHiddenChanged"]
        E24["OnAlertTriggered"]
        E25["OnArrestTriggered"]
        E26["OnSuspicionResetRequested"]
        E27["OnStoryTrigger"]
        E28["OnKeyCollected"]
        E29["OnKeyCountChanged"]
    end

    subgraph Subscribers["📥 Event Subscribers"]
        GM2["S_GameManager"]
        UI["S_UIManager"]
        AM["S_AudioManager"]
        NPCDial2["S_NPCDialogue"]
        NPCStory["S_NPCStory"]
        SAE["S_SectionAlarmEffect"]
        NPCCam["S_NPCCamera"]
        LS2["S_LevelSection"]
        LSCtrl2["S_LevelSectionController"]
        EG["S_ExitGate"]
        DRC["S_DropResourceCounter"]
        SCT2["S_SceneCheckpointTracker"]
        SUS2["S_SuspicionSystem"]
        HS2["S_HideSpot"]
    end

    P -->|publishes| E1
    P -->|publishes| E2
    P -->|publishes| E4
    P -->|publishes| E8
    CP -->|publishes| E5
    GM -->|publishes| E3
    GM -->|publishes| E9
    GM -->|publishes| E10
    GM -->|publishes| E11
    GM -->|publishes| E14
    LS -->|publishes| E6
    LSCtrl -->|publishes| E7
    BB -->|publishes| E8
    NPCDial -->|publishes| E17
    NPCDial -->|publishes| E27
    SUS -->|publishes| E18
    SUS -->|publishes| E19
    NPCE -->|publishes| E24
    NPCE -->|publishes| E25
    HS -->|publishes| E23
    Key -->|publishes| E28
    Key -->|publishes| E29
    SMC -->|publishes| E12
    SMC -->|publishes| E13

    E1 -->|subscribes| GM2
    E1 -->|subscribes| SCT2
    E2 -->|subscribes| GM2
    E2 -->|subscribes| SCT2
    E3 -->|subscribes| UI
    E5 -->|subscribes| SCT2
    E6 -->|subscribes| LS2
    E6 -->|subscribes| LSCtrl2
    E7 -->|subscribes| AM
    E7 -->|subscribes| SAE
    E8 -->|subscribes| AM
    E9 -->|subscribes| AM
    E10 -->|subscribes| AM
    E11 -->|subscribes| AM
    E12 -->|subscribes| GM2
    E13 -->|subscribes| GM2
    E14 -->|subscribes| GM2
    E17 -->|subscribes| NPCDial2
    E18 -->|subscribes| UI
    E18 -->|subscribes| NPCCam
    E19 -->|subscribes| UI
    E20 -->|subscribes| SUS2
    E21 -->|subscribes| SUS2
    E22 -->|subscribes| HS2
    E23 -->|subscribes| HS2
    E24 -->|subscribes| NPCCam
    E25 -->|subscribes| GM2
    E26 -->|subscribes| SUS2
    E27 -->|subscribes| NPCStory
    E28 -->|subscribes| EG
    E29 -->|subscribes| UI
    E29 -->|subscribes| DRC
```

## 4. Player System Internal Architecture

```mermaid
graph TB
    subgraph PlayerCore["S_Player (Singleton, IPlayerActor)"]
        PV["Physics: Rigidbody2D"]
        PI["Input: InputSystem_Actions"]
        PF["State: isFluid / isGrounded / isGripping / movementLocked"]
    end

    subgraph SkillCtrl["S_PlayerSkillController"]
        SC_Sprint["Sprint Charge State Machine<br/>BeginSprintCharge → FixedTick<br/>→ ReleaseSprintCharge"]
        SC_Cam["Camera Control<br/>BeginCameraControl → CameraControlTick<br/>(bullet-time)"]
    end

    subgraph Components["Player Components"]
        SPR["S_PlayerProceduralRenderer<br/>Body, Outline, Eye Glow,<br/>Contact-Plane Fitting, Tail Mesh"]
        PDC["S_PlayerDynamicCollider<br/>CircleCollider2D (default)<br/>CapsuleCollider2D (crouch/wall/ceiling)"]
    end

    subgraph SkillTree["S_SkillTree → S_SkillBase[]"]
        SS["S_Soild_sprint<br/>Hold-to-charge, 3 stages,<br/>Buffer (0.15s), Stage cooldowns"]
        SC["S_fluid_climb<br/>Wall/Ceiling climb,<br/>Fluid form gravity"]
        SCC["S_CameraControlSkill<br/>Bullet-time camera"]
    end

    subgraph Camera["S_CameraMove"]
        CF["Follow Target"]
        CB["Bounds Clamp"]
        CM["Manual Control Mode"]
    end

    subgraph Contracts["IPlayerActor + S_PlayerLookup"]
        IPA["IPlayerActor interface"]
        PLU["S_PlayerLookup.TryGet()"]
    end

    PlayerCore -->|"creates & injects"| SkillCtrl
    PlayerCore --> SPR
    PlayerCore --> PDC
    SkillCtrl -->|"delegates sprint"| SS
    SkillCtrl -->|"delegates camera"| SCC
    SkillCtrl -->|"modifies"| SPR
    SkillCtrl -->|"modifies"| PDC
    PlayerCore -->|"reads"| SkillTree
    PlayerCore --> Camera

    SS -->|"scale body"| SPR
    SS -->|"scale collider"| PDC
    SC -->|"change gravity"| PV
    SCC -->|"bullet-time"| Camera

    PlayerCore -->|"implements"| IPA
    PLU -->|"resolves"| IPA
```

## 5. Manager Root Architecture

```mermaid
graph TB
    subgraph Persistent["DontDestroyOnLoad"]
        MR["S_ManagerRoot<br/>(Singleton)"]
        GM["S_GameManager"]
        UI["S_UIManager"]
        AM["S_AudioManager"]
        IBM["S_InputBindingManager"]
    end

    subgraph Scene["Per-Scene"]
        SCT["S_SceneCheckpointTracker<br/>(auto-created)"]
        LS["S_LevelSection"]
        LSCtrl["S_LevelSectionController"]
        NPCE["S_NPCEnemy"]
    end

    MR -->|"AttachPersistent"| GM
    MR -->|"AttachPersistent"| UI
    MR -->|"AttachPersistent"| AM
    MR -->|"AttachPersistent"| IBM
    MR -->|"GetOrCreateChild"| GM
    MR -->|"GetOrCreateChild"| UI
    MR -->|"GetOrCreateChild"| AM

    SCT -->|"spawned per scene<br/>RuntimeInitializeOnLoadMethod"| SCT
    SCT -->|"OnSpawnPointChanged"| GE["S_GameEvent"]
    SCT -->|"IPlayerActor.Teleport"| PLU["S_PlayerLookup"]
```

## 6. NPC System Internal Architecture

```mermaid
graph TB
    subgraph NPCBase["S_NPCbase (Base Class)"]
        NB_Health["Health System"]
        NB_Move["Movement"]
        NB_Damage["TakeDamage / Die"]
    end

    subgraph NPCEnemy["S_NPCEnemy (inherits S_NPCbase)"]
        NE_State["5-State FSM:<br/>Patrol → Chase → Aim → Attack → Arrest"]
        NE_Stunned["Stunned State"]
        NE_Detection["Player Detection<br/>(IPlayerActor via S_PlayerLookup)"]
        NE_Jump["Jump System:<br/>EvaluateJump → FindLandingSpot<br/>→ CalculateJumpParameters → ExecuteJump"]
        NE_AirControl["Air Control (50%)"]
    end

    subgraph NPCSupport["NPC Support Systems"]
        NW["S_NPCWaveSpawner<br/>Camera-edge spawning,<br/>30s interval, Cleanup"]
        NC["S_NPCCamera<br/>Follow NPC Alert"]
        ND["S_NPCDialogue<br/>Dialogue Trigger"]
        NS["S_NPCStory<br/>Story Progression"]
        EP["S_EMProjectile<br/>Projectile Attack"]
    end

    subgraph NPCTools["NPC Tools"]
        NST["S_NPCSpawnerTool<br/>Inspector-driven spawning"]
    end

    NPCBase --> NPCEnemy
    NPCEnemy -->|"fires"| EP
    NPCEnemy -->|"spawns"| NW
    NPCEnemy -->|"triggers"| ND
    ND -->|"triggers"| NS
    NPCEnemy -->|"alerts"| NC
    NPCEnemy -->|"uses"| NE_Detection
    NST -->|"creates"| NPCEnemy
```

## 7. Level Section System Architecture

```mermaid
graph TB
    subgraph SectionSystem["Level Section System"]
        LSC["S_LevelSectionController"]
        LS1["S_LevelSection #1"]
        LS2["S_LevelSection #2"]
        LS3["S_LevelSection #n"]
        SG1["S_SectionGoal #1"]
        SG2["S_SectionGoal #2"]
    end

    subgraph SectionEffects["Section Effects"]
        SAE["S_SectionAlarmEffect<br/>Alarm Visual/Audio"]
        AM["S_AudioManager<br/>Platform Alarm Sound"]
    end

    subgraph SectionObjects["Section Level Objects"]
        MP["S_MovingPlatform"]
        BB["S_BreakableBlock"]
        Door["S_Door / S_ButtonDoor"]
        Key["S_Key"]
        EG["S_ExitGate"]
        JP["S_JumpPad"]
        MB["S_MoveBlock"]
        Pipe["S_Pipline"]
        CP["S_Checkpoint"]
        SCT["S_SceneCheckpointTracker"]
    end

    LSC -->|"manages"| LS1
    LSC -->|"manages"| LS2
    LSC -->|"manages"| LS3
    LS1 -->|"contains"| SG1
    LS2 -->|"contains"| SG2

    LSC -->|"OnSectionDescentStarted"| SAE
    LSC -->|"OnSectionDescentStarted"| AM

    LS1 -->|"contains"| SectionObjects
    LS2 -->|"contains"| SectionObjects
```

## 8. Data Flow Summary

```mermaid
flowchart TD
    Input["🎮 Input System"] -->|"Move/Jump/Skill"| Player["🧍 S_Player"]
    Player -->|"Update()"| Physics["Rigidbody2D<br/>(FixedUpdate)"]
    Player -->|"Form Switch"| SkillTree["S_SkillTree"]
    SkillTree -->|"Solid"| Sprint["S_Soild_sprint"]
    SkillTree -->|"Fluid"| Climb["S_fluid_climb"]
    Player -->|"creates"| SkillCtrl["S_PlayerSkillController"]
    SkillCtrl -->|"Sprint Charge"| Sprint
    SkillCtrl -->|"Camera Control"| CamSkill["S_CameraControlSkill"]
    SkillCtrl -->|"modifies"| ProceduralRenderer["S_PlayerProceduralRenderer"]
    SkillCtrl -->|"modifies"| DynCollider["S_PlayerDynamicCollider"]
    Player -->|"controls"| Camera["S_CameraMove"]

    Player -->|"Trigger Enter"| Level["🗺️ Level Objects"]
    Player -->|"Section Enter"| Section["S_LevelSection"]
    Section -->|"Events"| EventBus["📡 S_GameEvent<br/>(30+ events)"]
    Level -->|"Events"| EventBus

    NPC["🤖 S_NPCEnemy"] -->|"S_PlayerLookup.TryGet()"| IPlayer["IPlayerActor"]
    NPC -->|"Attack"| Projectile["S_EMProjectile"]
    NPC -->|"Alert"| EventBus

    Suspicion["🔍 S_SuspicionSystem"] -->|"Reads NPC"| NPC
    Suspicion -->|"Threshold"| EventBus
    HideSpot["S_HideSpot"] -->|"Event"| EventBus

    Checkpoint["S_SceneCheckpointTracker"] -->|"subscribes"| EventBus
    Checkpoint -->|"IPlayerActor.Teleport"| IPlayer

    EventBus -->|"Subscribe"| Managers["🏢 Managers"]
    Managers -->|"Audio"| AudioManager["S_AudioManager"]
    Managers -->|"UI"| UIManager["S_UIManager"]
    Managers -->|"Scene"| SceneControl["S_SceneChangeTrigger"]

    MR["S_ManagerRoot"] -->|"AttachPersistent"| Managers
```

## 9. Singleton Dependency Map

```mermaid
graph LR
    subgraph Singletons["Singleton Instances"]
        MR["S_ManagerRoot.Instance"]
        P["S_Player.Instance"]
        ST["S_SkillTree.Instance"]
        GM["S_GameManager.Instance"]
        UI["S_UIManager.Instance"]
        AM["S_AudioManager.Instance"]
        IBM["S_InputBindingManager.Instance"]
    end

    subgraph Dependents["Systems That Reference Singletons"]
        P2["S_Player → S_InputBindingManager, S_SkillTree"]
        PSC["S_PlayerSkillController → S_Player, S_SkillTree, S_CameraMove"]
        SPR["S_PlayerProceduralRenderer → S_Player"]
        PDC["S_PlayerDynamicCollider → S_Player"]
        NPCE["S_NPCEnemy → IPlayerActor via S_PlayerLookup"]
        SUS["S_SuspicionSystem → IPlayerActor via S_PlayerLookup"]
        HS["S_HideSpot → IPlayerActor via S_PlayerLookup"]
        SCT["S_SceneCheckpointTracker → IPlayerActor via S_PlayerLookup"]
        CM["S_CameraMove → S_Player"]
        LS["S_LevelSection → IPlayerActor via S_PlayerLookup"]
        LSC["S_LevelSectionController → S_GameManager"]
        PM["S_PerformanceMonitor → S_ManagerRoot"]
    end

    MR -->|"manages"| GM
    MR -->|"manages"| UI
    MR -->|"manages"| AM
    MR -->|"manages"| IBM
    P --> PSC
    P --> SPR
    P --> PDC
    P --> P2
    PSC --> ST
    PSC --> CM
    NPCE -->|"IPlayerActor"| P
    SUS -->|"IPlayerActor"| P
    HS -->|"IPlayerActor"| P
    SCT -->|"IPlayerActor"| P
    LS -->|"IPlayerActor"| P
    GM --> LSC
```

## 10. Directory Structure

```
Assets/Perfab/Script/
├── Camera/                          # Camera systems
│   ├── S_CameraMove.cs              # Camera follow + manual control
│   └── S_ParallaxLayer.cs           # Parallax scrolling
├── Core/
│   └── Events/
│       └── S_GameEvent.cs           # Static event bus (30+ events)
├── Input/
│   ├── InputSystem_Actions.cs       # Generated input actions
│   └── InputSystem_Actions.inputactions
├── Level/
│   ├── Interactables/
│   │   ├── S_BreakableBlock.cs
│   │   ├── S_ButtonDoor.cs
│   │   ├── S_Checkpoint.cs
│   │   ├── S_Door.cs
│   │   ├── S_ExitGate.cs
│   │   ├── S_HideSpot.cs
│   │   ├── S_JumpPad.cs
│   │   ├── S_Key.cs
│   │   ├── S_Pipline.cs
│   │   └── S_SceneCheckpointTracker.cs  # NEW: per-scene respawn
│   ├── Platforms/
│   │   ├── S_MoveBlock.cs
│   │   ├── S_MovingPlatform.cs
│   │   └── S_PlatformCableVisual.cs
│   ├── Resources/
│   │   ├── S_DroppedResourceItem.cs
│   │   └── S_DropResourceCounter.cs
│   ├── Sections/
│   │   ├── S_LevelSection.cs
│   │   ├── S_LevelSectionController.cs
│   │   ├── S_SectionAlarmEffect.cs
│   │   └── S_SectionGoal.cs
│   └── Zones/
│       └── S_CantClimb.cs
├── Managers/
│   ├── S_AudioManager.cs            # Audio (BGM/SFX/alarm)
│   ├── S_GameManager.cs             # Game state + scene loading
│   ├── S_InputBindingManager.cs     # Runtime rebinding
│   ├── S_ManagerRoot.cs             # NEW: persistent root
│   ├── S_SceneChangeTrigger.cs      # Scene transitions
│   ├── S_StartMenuController.cs     # Start menu
│   └── S_UIManager.cs               # UI overlay + controls menu
├── MCTS/
│   ├── LevelTestMetrics.cs
│   ├── MCTSBotController.cs
│   ├── MCTSGameState.cs
│   └── MCTSNode.cs
├── NPCs/
│   ├── Combat/
│   │   ├── S_EMProjectile.cs
│   │   └── S_NPCEnemy.cs
│   ├── Core/
│   │   └── S_NPCbase.cs
│   ├── Dialogue/
│   │   ├── S_NPCDialogue.cs
│   │   └── S_NPCStory.cs
│   ├── Sensors/
│   │   └── S_NPCCamera.cs
│   └── Spawning/
│       └── S_NPCWaveSpawner.cs
├── Player/
│   ├── Body/
│   │   ├── S_PlayerDynamicCollider.cs
│   │   └── S_PlayerProceduralRenderer.cs
│   ├── Core/
│   │   ├── S_Player.cs              # Main player (IPlayerActor)
│   │   └── S_PlayerContracts.cs     # NEW: IPlayerActor + S_PlayerLookup
│   ├── Physics/
│   │   └── S_coleve.cs
│   └── Skills/
│       ├── S_CameraControlSkill.cs
│       ├── S_fluid_climb.cs
│       ├── S_PlayerSkillController.cs  # NEW: sprint + camera control
│       ├── S_SkillBase.cs
│       ├── S_SkillTree.cs
│       └── S_Soild_sprint.cs
├── Systems/
│   └── Suspicion/
│       └── S_SuspicionSystem.cs
├── Tools/
│   ├── S_NPCSpawnerTool.cs
│   ├── S_PerformanceMonitor.cs
│   └── S_setTrigger.cs
└── Project_Prompt/                  # Design documents
    ├── Architecture.md              # This file
    ├── CHANGELOG.md
    └── ...