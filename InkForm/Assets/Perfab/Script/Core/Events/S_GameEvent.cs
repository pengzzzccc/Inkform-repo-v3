using System;
using UnityEngine;

public static class S_GameEvent
{
    public static event Action OnPlayerDied;
    public static event Action OnRunStartRequested;
    public static event Action OnRespawnRequested;
    public static event Action OnExit;
    public static event Action OnReturnToStartMenuRequested;
    public static event Action<string> OnSceneLoadRequested;
    public static event Action<bool> OnGameplayInputEnabledRequested;
    public static event Action<string> OnGameplayInputLockPushed;
    public static event Action<string> OnGameplayInputLockPopped;
    public static event Action<int> OnScoreChanged;
    public static event Action<string> OnSkillUsed;
    public static event Action<float, float> OnPlayerEnergyChanged;
    public static event Action<Transform> reNewSpwnPoint;
    public static event Action<Transform> OnSpawnPointChanged;
    public static event Action<S_LevelCompletionReason> OnLevelCompleted;
    public static event Action<int> OnSectionStart;
    public static event Action<int> OnSectionEnd;
    public static event Action<int> OnSectionDescentStarted;
    public static event Action<int> OnSectionDescentCompleted;
    public static event Action<AudioClip> OnPlaySFX;
    public static event Action<AudioClip, float, float> OnPlaySFXPitched;
    public static event Action<AudioClip> OnBGMChange;
    public static event Action<float> OnBgmVolumeChangeRequested;
    public static event Action<float> OnSfxVolumeChangeRequested;

    // NPC & Story Events
    public static event Action<string> OnNPCInteract;
    public static event Action<float> OnSuspicionChanged;
    public static event Action<float, float> OnSuspicionValueChanged;
    public static event Action<float, Transform> OnSuspicionChangeRequested;
    public static event Action<float> OnHiddenSuspicionDecayRequested;
    public static event Action<bool> OnPlayerHiddenChangeRequested;
    public static event Action<bool> OnPlayerHiddenChanged;
    public static event Action<Transform> OnAlertTriggered;
    public static event Action OnArrestTriggered;
    public static event Action OnSuspicionResetRequested;
    public static event Action<string> OnStoryTrigger;
    public static event Action<S_RoomTransitionRequest> OnRoomEnterRequested;
    public static event Action OnFacilityEntered;
    public static event Action OnEndingRequested;

    public static void PlayerDied() => OnPlayerDied?.Invoke();
    public static void RunStartRequested() => OnRunStartRequested?.Invoke();
    public static void RespawnRequested() => OnRespawnRequested?.Invoke();
    public static void ExitGame() => OnExit?.Invoke();
    public static void ReturnToStartMenuRequested() => OnReturnToStartMenuRequested?.Invoke();
    public static void SceneLoadRequested(string sceneName) => OnSceneLoadRequested?.Invoke(sceneName);
    public static void GameplayInputEnabledRequested(bool enabled) => OnGameplayInputEnabledRequested?.Invoke(enabled);
    public static void PushGameplayInputLock(string lockId) => OnGameplayInputLockPushed?.Invoke(lockId);
    public static void PopGameplayInputLock(string lockId) => OnGameplayInputLockPopped?.Invoke(lockId);
    public static void PlayerEnergyChanged(float current, float max) => OnPlayerEnergyChanged?.Invoke(current, max);
    public static void ReNewSpwnPoint(Transform spwnPoint) => SpawnPointChanged(spwnPoint);
    public static void SpawnPointChanged(Transform spwnPoint)
    {
        OnSpawnPointChanged?.Invoke(spwnPoint);
        reNewSpwnPoint?.Invoke(spwnPoint);
    }
    public static void LevelCompleted(S_LevelCompletionReason reason = S_LevelCompletionReason.Goal) => OnLevelCompleted?.Invoke(reason);
    public static void ScoreChanged(int score) => OnScoreChanged?.Invoke(score);
    public static void SkillUsed(string skillName) => OnSkillUsed?.Invoke(skillName);
    public static void SectionStart(int index) => OnSectionStart?.Invoke(index);
    public static void SectionEnd(int index) => OnSectionEnd?.Invoke(index);
    public static void SectionDescentStarted(int index) => OnSectionDescentStarted?.Invoke(index);
    public static void SectionDescentCompleted(int index) => OnSectionDescentCompleted?.Invoke(index);
    public static void PlaySFX(AudioClip clip) => OnPlaySFX?.Invoke(clip);
    public static void PlaySFX(AudioClip clip, float pitch, float volumeMultiplier = 1f) => OnPlaySFXPitched?.Invoke(clip, pitch, volumeMultiplier);
    public static void BGMChange(AudioClip clip) => OnBGMChange?.Invoke(clip);
    public static void BgmVolumeChangeRequested(float value) => OnBgmVolumeChangeRequested?.Invoke(value);
    public static void SfxVolumeChangeRequested(float value) => OnSfxVolumeChangeRequested?.Invoke(value);

    // Key & Gate Events
    public static event Action OnKeyCollected;
    public static event Action<int, int> OnKeyCountChanged;

    // NPC & Story Invokers
    public static void NPCInteract(string npcID) => OnNPCInteract?.Invoke(npcID);
    public static void SuspicionChanged(float value) => OnSuspicionChanged?.Invoke(value);
    public static void SuspicionValueChanged(float current, float max) => OnSuspicionValueChanged?.Invoke(current, max);
    public static void SuspicionChangeRequested(float amount, Transform source = null) => OnSuspicionChangeRequested?.Invoke(amount, source);
    public static void HiddenSuspicionDecayRequested(float deltaTime) => OnHiddenSuspicionDecayRequested?.Invoke(deltaTime);
    public static void PlayerHiddenChangeRequested(bool hidden) => OnPlayerHiddenChangeRequested?.Invoke(hidden);
    public static void PlayerHiddenChanged(bool hidden) => OnPlayerHiddenChanged?.Invoke(hidden);
    public static void AlertTriggered(Transform npc) => OnAlertTriggered?.Invoke(npc);
    public static void ArrestTriggered() => OnArrestTriggered?.Invoke();
    public static void SuspicionResetRequested() => OnSuspicionResetRequested?.Invoke();
    public static void StoryTrigger(string triggerID) => OnStoryTrigger?.Invoke(triggerID);
    public static void RoomEnterRequested(RoomId roomId) => RoomEnterRequested(new S_RoomTransitionRequest(roomId, S_RoomEntryMode.Door));
    public static void RoomEnterRequested(S_RoomTransitionRequest request) => OnRoomEnterRequested?.Invoke(request);
    public static void FacilityEntered() => OnFacilityEntered?.Invoke();
    public static void EndingRequested() => OnEndingRequested?.Invoke();

    // Key & Gate Invokers
    public static void KeyCollected() => OnKeyCollected?.Invoke();
    public static void KeyCountChanged(int collected, int total) => OnKeyCountChanged?.Invoke(collected, total);

    // Tutorial & Camera Pan Events
    public static event Action OnCameraPanStarted;
    public static event Action OnCameraPanEnded;
    public static event Action<float> OnCountdownTick;
    public static event Action OnCountdownStarted;
    public static event Action OnCountdownFinished;
    public static event Action OnTutorialPromptDismissed;
    public static event Action OnVoiceLineFinished;
    public static event Action OnTutorialPhaseChanged;

    // Tutorial & Camera Pan Invokers
    public static void CameraPanStarted() => OnCameraPanStarted?.Invoke();
    public static void CameraPanEnded() => OnCameraPanEnded?.Invoke();
    public static void CountdownStarted() => OnCountdownStarted?.Invoke();
    public static void CountdownTick(float remaining) => OnCountdownTick?.Invoke(remaining);
    public static void CountdownFinished() => OnCountdownFinished?.Invoke();
    public static void TutorialPromptDismissed() => OnTutorialPromptDismissed?.Invoke();
    public static void VoiceLineFinished() => OnVoiceLineFinished?.Invoke();
    public static void TutorialPhaseChanged() => OnTutorialPhaseChanged?.Invoke();
}
