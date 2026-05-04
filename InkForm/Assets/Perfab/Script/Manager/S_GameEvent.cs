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

    public static void PlayerDied() => OnPlayerDied?.Invoke();
    public static void GameStart() => OnGameStart?.Invoke();
    public static void GameReStart() => OnGameRestart?.Invoke();
    public static void ExitGame() => OnExit?.Invoke();
    public static void ReNewSpwnPoint(Transform spwnPoint) => reNewSpwnPoint?.Invoke(spwnPoint);
    public static void ScoreChanged(int score) => OnScoreChanged?.Invoke(score);
    public static void SkillUsed(string skillName) => OnSkillUsed?.Invoke(skillName);
    public static void SectionStart(int index) => OnSectionStart?.Invoke(index);
    public static void SectionEnd(int index) => OnSectionEnd?.Invoke(index);
}