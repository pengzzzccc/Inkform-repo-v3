# Game Event System Design

## Purpose

`S_GameEvent` is the static event bus used by gameplay objects, UI, managers, and flow systems. Producers publish intent; consumers decide what to do.

## Current Event Categories

### Run And Scene Flow

| Invoker | Event | Meaning | Main consumer |
| --- | --- | --- | --- |
| `RunStartRequested()` | `OnRunStartRequested` | Start a fresh run from menu/UI | `S_RunFlowController`, reset listeners |
| `RespawnRequested()` | `OnRespawnRequested` | Restart the current level after death UI confirmation | `S_SceneCheckpointTracker`, reset listeners |
| `ReturnToStartMenuRequested()` | `OnReturnToStartMenuRequested` | Go back to start menu | `S_RunFlowController` |
| `SceneLoadRequested(string)` | `OnSceneLoadRequested` | Load a scene by runtime key | `S_GameManager` |
| `LevelCompleted(S_LevelCompletionReason)` | `OnLevelCompleted` | Current training/random level is complete | `S_RunFlowController` |
| `RoomEnterRequested(RoomId)` | `OnRoomEnterRequested` | Facility room exit requests target room | `S_RunFlowController` |
| `EndingRequested()` | `OnEndingRequested` | Route to ending scene | `S_RunFlowController` |
| `FacilityEntered()` | `OnFacilityEntered` | Facility phase began | Optional listeners |

### UI And Input

| Invoker | Event | Meaning |
| --- | --- | --- |
| `PlayerDied()` | `OnPlayerDied` | Show death UI |
| `GameplayInputEnabledRequested(bool)` | `OnGameplayInputEnabledRequested` | Explicit input enable/disable request |
| `PushGameplayInputLock(string)` | `OnGameplayInputLockPushed` | Push named input lock |
| `PopGameplayInputLock(string)` | `OnGameplayInputLockPopped` | Pop named input lock |

### Player Data

| Invoker | Event | Meaning |
| --- | --- | --- |
| `ScoreChanged(int)` | `OnScoreChanged` | Score display changed |
| `SkillUsed(string)` | `OnSkillUsed` | Skill used |
| `PlayerEnergyChanged(float, float)` | `OnPlayerEnergyChanged` | Energy UI update |
| `SpawnPointChanged(Transform)` | `OnSpawnPointChanged` | Checkpoint changed |

### Level Sections

| Invoker | Event | Meaning |
| --- | --- | --- |
| `SectionStart(int)` | `OnSectionStart` | Player entered section start trigger |
| `SectionEnd(int)` | `OnSectionEnd` | Player entered section end trigger |
| `SectionDescentStarted(int)` | `OnSectionDescentStarted` | Section starts moving down |
| `SectionDescentCompleted(int)` | `OnSectionDescentCompleted` | Section reached playable position |

### Audio

| Invoker | Event | Meaning |
| --- | --- | --- |
| `PlaySFX(AudioClip)` | `OnPlaySFX` | One-shot SFX |
| `PlaySFX(AudioClip, float, float)` | `OnPlaySFXPitched` | Pitched/scaled SFX |
| `BGMChange(AudioClip)` | `OnBGMChange` | Change background music |
| `BgmVolumeChangeRequested(float)` | `OnBgmVolumeChangeRequested` | Change BGM volume |
| `SfxVolumeChangeRequested(float)` | `OnSfxVolumeChangeRequested` | Change SFX volume |

### Keys And Gates

| Invoker | Event | Meaning |
| --- | --- | --- |
| `KeyCollected()` | `OnKeyCollected` | A key was collected |
| `KeyCountChanged(int, int)` | `OnKeyCountChanged` | Key UI/gate unlock update |

### Suspicion, NPC, Story

| Invoker | Event | Meaning |
| --- | --- | --- |
| `NPCInteract(string)` | `OnNPCInteract` | Player interacted with NPC |
| `SuspicionChanged(float)` | `OnSuspicionChanged` | Legacy suspicion UI value |
| `SuspicionValueChanged(float, float)` | `OnSuspicionValueChanged` | Suspicion current/max value |
| `SuspicionChangeRequested(float, Transform)` | `OnSuspicionChangeRequested` | Add/subtract suspicion |
| `HiddenSuspicionDecayRequested(float)` | `OnHiddenSuspicionDecayRequested` | Hidden player suspicion decay |
| `PlayerHiddenChangeRequested(bool)` | `OnPlayerHiddenChangeRequested` | Request hidden state |
| `PlayerHiddenChanged(bool)` | `OnPlayerHiddenChanged` | Hidden state changed |
| `AlertTriggered(Transform)` | `OnAlertTriggered` | NPC alert |
| `ArrestTriggered()` | `OnArrestTriggered` | Player caught/arrested |
| `SuspicionResetRequested()` | `OnSuspicionResetRequested` | Reset suspicion |
| `StoryTrigger(string)` | `OnStoryTrigger` | Narrative trigger id |

### Tutorial

| Invoker | Event | Meaning |
| --- | --- | --- |
| `CameraPanStarted()` | `OnCameraPanStarted` | Tutorial camera pan started |
| `CameraPanEnded()` | `OnCameraPanEnded` | Tutorial camera pan ended |
| `CountdownStarted()` | `OnCountdownStarted` | Countdown began |
| `CountdownTick(float)` | `OnCountdownTick` | Countdown value changed |
| `CountdownFinished()` | `OnCountdownFinished` | Countdown reached zero |
| `TutorialPromptDismissed()` | `OnTutorialPromptDismissed` | Prompt closed |
| `VoiceLineFinished()` | `OnVoiceLineFinished` | Voice line ended |
| `TutorialPhaseChanged()` | `OnTutorialPhaseChanged` | Tutorial state changed |

## Rules

- Producers should not call manager methods directly when an event exists.
- Scene loading must go through `SceneLoadRequested`.
- Level progression must go through `LevelCompleted`, `RoomEnterRequested`, or `EndingRequested`.
- Use named input locks so overlapping systems can safely push/pop their own lock.
- Subscribe in `OnEnable` and unsubscribe in `OnDisable`.

## Common Examples

Training level exit:

```csharp
S_GameEvent.LevelCompleted(S_LevelCompletionReason.Goal);
```

Facility room exit:

```csharp
S_GameEvent.RoomEnterRequested(RoomId.PS);
```

Death UI restart:

```csharp
S_GameEvent.RespawnRequested();
```

Direct ending:

```csharp
S_GameEvent.EndingRequested();
```
