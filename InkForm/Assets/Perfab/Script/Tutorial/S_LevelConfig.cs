using System;
using UnityEngine;

[Serializable]
public struct TutorialRequiredAction
{
    [Tooltip("Display name shown in UI, e.g. 'Move', 'Jump'")]
    public string actionName;

    [Tooltip("Keyboard key names that fulfill this action, e.g. ['W','A','S','D','Up','Down','Left','Right']")]
    public string[] keyboardKeys;

    [Tooltip("Gamepad button names that fulfill this action, e.g. ['LeftStick','ButtonSouth']")]
    public string[] gamepadKeys;
}

public enum TutorialType
{
    None,
    TeachAndPractice,
    PracticeOnly
}

[CreateAssetMenu(fileName = "LevelConfig", menuName = "InkForm/Level Config")]
public class S_LevelConfig : ScriptableObject
{
    [Header("Tutorial")]
    [Tooltip("TeachAndPractice = show voice + prompt + wait for input. PracticeOnly = skip to camera pan + countdown.")]
    public TutorialType tutorialType = TutorialType.None;

    [Tooltip("Skill names available in this training level, e.g. 'Sprint', 'FluidClimb'")]
    public string[] skillsToUnlock;

    [Header("Voice")]
    [Tooltip("Voice clip for the familiarization prompt. Can be null if audio not ready.")]
    public AudioClip familiarizeVoiceClip;

    [Tooltip("Subtitle text shown during familiarization voice")]
    [TextArea]
    public string familiarizeSubtitle = "Now get familiar with the controls.";

    [Tooltip("Voice clip for the countdown prompt. Can be null.")]
    public AudioClip countdownVoiceClip;

    [Tooltip("Subtitle text shown during countdown voice")]
    [TextArea]
    public string countdownSubtitle = "Reach the goal within 30 seconds.";

    [Header("Required Actions")]
    [Tooltip("Actions the player must perform before the prompt dismisses. Leave empty to dismiss on any key.")]
    public TutorialRequiredAction[] requiredActions;

    [Header("Prompt UI")]
    [Tooltip("Title shown in the tutorial prompt panel, e.g. 'Movement Controls'")]
    public string promptTitle = "Controls";

    [Tooltip("Description text listing controls, e.g. 'WASD / Arrow Keys - Move\\nSpace - Jump'")]
    [TextArea(3, 10)]
    public string promptDescription = "WASD / Arrow Keys - Move\nSpace - Jump";

    [Header("Countdown")]
    [Tooltip("Time limit in seconds")]
    public float timeLimit = 30f;

    [Header("Timing")]
    [Min(0f)] public float startupDelay = 0.5f;
    [Min(0f)] public float panToTargetDuration = 1.5f;
    [Min(0f)] public float panHoldDuration = 1.5f;
    [Min(0f)] public float panReturnDuration = 1.0f;
    [Min(0f)] public float preCountdownDelay = 0f;
    [Min(0f)] public float timeoutDeathDelay = 0.5f;

    [Header("NPC")]
    [Tooltip("Whether this level contains NPCs")]
    public bool hasNPC;
}
