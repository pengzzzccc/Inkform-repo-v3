using System;
using UnityEngine;

public static class S_GameEvent
{
    public static event Action OnPlayerDied;
    public static event Action OnGameStart;
    public static event Action OnGameRestart;
    public static event Action OnExit;
    public static event Action<int> OnScoreChanged;
    public static event Action<string> OnSkillUsed;
    public static event Action<Transform> reNewSpwnPoint;
    public static event Action<int> OnSectionStart;
    public static event Action<int> OnSectionEnd;
    public static event Action<int> OnSectionDescentStarted;
    public static event Action<int> OnSectionDescentCompleted;
    public static event Action<AudioClip> OnPlaySFX;
    public static event Action<AudioClip, float, float> OnPlaySFXPitched;
    public static event Action<AudioClip> OnBGMChange;

    // NPC & Story Events
    public static event Action<string> OnNPCInteract;
    public static event Action<float> OnSuspicionChanged;
    public static event Action<Transform> OnAlertTriggered;
    public static event Action OnArrestTriggered;
    public static event Action<string> OnStoryTrigger;

    public static void PlayerDied() => OnPlayerDied?.Invoke();
    public static void GameStart() => OnGameStart?.Invoke();
    public static void GameReStart() => OnGameRestart?.Invoke();
    public static void ExitGame() => OnExit?.Invoke();
    public static void ReNewSpwnPoint(Transform spwnPoint) => reNewSpwnPoint?.Invoke(spwnPoint);
    public static void ScoreChanged(int score) => OnScoreChanged?.Invoke(score);
    public static void SkillUsed(string skillName) => OnSkillUsed?.Invoke(skillName);
    public static void SectionStart(int index) => OnSectionStart?.Invoke(index);
    public static void SectionEnd(int index) => OnSectionEnd?.Invoke(index);
    public static void SectionDescentStarted(int index) => OnSectionDescentStarted?.Invoke(index);
    public static void SectionDescentCompleted(int index) => OnSectionDescentCompleted?.Invoke(index);
    public static void PlaySFX(AudioClip clip) => OnPlaySFX?.Invoke(clip);
    public static void PlaySFX(AudioClip clip, float pitch, float volumeMultiplier = 1f) => OnPlaySFXPitched?.Invoke(clip, pitch, volumeMultiplier);
    public static void BGMChange(AudioClip clip) => OnBGMChange?.Invoke(clip);

    // Key & Gate Events
    public static event Action OnKeyCollected;
    public static event Action<int, int> OnKeyCountChanged;

    // NPC & Story Invokers
    public static void NPCInteract(string npcID) => OnNPCInteract?.Invoke(npcID);
    public static void SuspicionChanged(float value) => OnSuspicionChanged?.Invoke(value);
    public static void AlertTriggered(Transform npc) => OnAlertTriggered?.Invoke(npc);
    public static void ArrestTriggered() => OnArrestTriggered?.Invoke();
    public static void StoryTrigger(string triggerID) => OnStoryTrigger?.Invoke(triggerID);

    // Key & Gate Invokers
    public static void KeyCollected() => OnKeyCollected?.Invoke();
    public static void KeyCountChanged(int collected, int total) => OnKeyCountChanged?.Invoke(collected, total);
}
