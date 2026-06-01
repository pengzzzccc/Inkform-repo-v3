using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs the tutorial flow for a training level. Attach to a GameObject in each training scene.
/// Reads S_TrainingLevelConfig to determine the tutorial type and available skills.
///
/// Flow for TeachAndPractice:
///   1. Fade in (handled by GameManager scene transition)
///   2. Apply configured skill availability
///   3. Voice line: "Now get familiar with the controls."
///   4. Show tutorial prompt, wait for any key
///   5. Voice line: "Reach the goal within 30 seconds."
///   6. Camera pan to goal and back
///   7. Start countdown
///   8. Success (player reaches goal) or Fail (countdown expires)
///
/// Flow for PracticeOnly (skip steps 3-5):
///   1. Fade in
///   2. Apply configured skill availability
///   3. Voice line: "Reach the goal within 30 seconds."
///   4. Camera pan to goal and back
///   5. Start countdown
///   6. Success or Fail
///
/// Flow for None:
///   Same as PracticeOnly but skips voice lines entirely.
/// </summary>
public class S_TutorialController : MonoBehaviour
{
    private const string TutorialInputLockId = "Tutorial";

    [Header("Config")]
    [SerializeField] private S_TrainingLevelConfig levelConfig;

    [Header("References")]
    [SerializeField] private S_CameraMove cameraMove;
    [SerializeField] private S_VoiceLinePlayer voiceLinePlayer;
    [SerializeField] private S_UITutorialPrompt tutorialPrompt;
    [SerializeField] private S_CountdownTimer countdownTimer;
    [SerializeField] private Transform cameraPanTarget;

    [Header("Camera Pan Settings")]
    [SerializeField] private float panToTargetDuration = 1.5f;
    [SerializeField] private float panHoldDuration = 1.5f;
    [SerializeField, Min(0f)] private float panReturnDuration = 1.0f;

    [Header("Startup Delay")]
    [SerializeField] private float startupDelay = 0.5f;
    [SerializeField, Min(0f)] private float preCountdownDelay = 0f;
    [SerializeField, Min(0f)] private float timeoutDeathDelay = 0.5f;

    private Coroutine flowRoutine;
    private Coroutine countdownRoutine;
    private bool goalReached;
    private bool countdownExpired;
    private bool tutorialInputLocked;
    private readonly List<AudioClip> digitCountdownClips = new List<AudioClip>(8);

    void Awake()
    {
        // Auto-find references if not assigned
        if (cameraMove == null)
            cameraMove = FindAnyObjectByType<S_CameraMove>();
        if (voiceLinePlayer == null)
            voiceLinePlayer = FindAnyObjectByType<S_VoiceLinePlayer>();
        if (tutorialPrompt == null)
            tutorialPrompt = FindAnyObjectByType<S_UITutorialPrompt>();
        if (countdownTimer == null)
            countdownTimer = FindAnyObjectByType<S_CountdownTimer>();
    }

    void OnEnable()
    {
        S_GameEvent.OnCountdownFinished += HandleCountdownFinished;
        S_GameEvent.OnLevelCompleted += HandleGoalReached;
    }

    void OnDisable()
    {
        S_GameEvent.OnCountdownFinished -= HandleCountdownFinished;
        S_GameEvent.OnLevelCompleted -= HandleGoalReached;

        CleanupActiveTutorialFlow();
    }

    void Start()
    {
        // Disable gameplay input during tutorial
        PushTutorialInputLock();

        // Apply the exact skill set available in this training level.
        ApplyConfiguredSkillSet();

        // Disable NPCs initially if needed
        if (levelConfig != null && !levelConfig.hasNPC)
        {
            // NPCs won't interfere during tutorial phases
        }

        flowRoutine = StartCoroutine(RunFlow());
    }

    private IEnumerator RunFlow()
    {
        // Wait for scene to settle
        yield return new WaitForSecondsRealtime(GetStartupDelay());

        if (levelConfig == null)
        {
            Debug.LogWarning("[TutorialController] No LevelConfig assigned. Skipping tutorial flow.");
            PopTutorialInputLock();
            yield break;
        }

        S_TrainingIntroMode type = levelConfig.tutorialType;

        // --- TEACH PHASE (TeachAndPractice only) ---
        if (type == S_TrainingIntroMode.TeachAndPractice)
        {
            // Voice: "Now get familiar with the controls."
            if (voiceLinePlayer != null && !string.IsNullOrEmpty(levelConfig.familiarizeSubtitle))
            {
                yield return voiceLinePlayer.PlayVoiceLine(
                    levelConfig.familiarizeVoiceClip,
                    levelConfig.familiarizeSubtitle);
            }

            // Unlock input so player can practice while prompt is showing
            PopTutorialInputLock();

            // Show prompt and wait for all required inputs
            if (tutorialPrompt != null)
            {
                if (levelConfig.requiredActions != null && levelConfig.requiredActions.Length > 0)
                {
                    yield return tutorialPrompt.ShowAndWaitForAllInputs(
                        levelConfig.promptTitle,
                        levelConfig.promptDescription,
                        levelConfig.requiredActions);
                }
                else
                {
                    yield return tutorialPrompt.ShowAndWaitForInput(
                        levelConfig.promptTitle,
                        levelConfig.promptDescription);
                }
            }

            // Re-lock input before countdown announcement
            PushTutorialInputLock();
        }

        // --- COUNTDOWN ANNOUNCEMENT PHASE ---
        if (type != S_TrainingIntroMode.None)
        {
            string countdownSubtitle = levelConfig.GetCountdownSubtitle();
            if (voiceLinePlayer != null && !string.IsNullOrEmpty(countdownSubtitle))
            {
                if (levelConfig.TryBuildCountdownVoiceClips(digitCountdownClips, out float clipGap))
                {
                    yield return voiceLinePlayer.PlayVoiceSequence(
                        digitCountdownClips,
                        countdownSubtitle,
                        clipGap);
                }
                else
                {
                    Debug.LogWarning($"[TutorialController] Countdown digit voice library is missing or incomplete for {levelConfig.name}. Showing countdown subtitle without legacy audio fallback.");
                    yield return voiceLinePlayer.PlayVoiceSequence(
                        null,
                        countdownSubtitle,
                        0f);
                }
            }
        }

        // --- CAMERA PAN PHASE ---
        if (cameraMove != null && cameraPanTarget != null)
        {
            yield return cameraMove.PanToTarget(
                cameraPanTarget.position,
                GetPanToTargetDuration(),
                GetPanHoldDuration(),
                GetPanReturnDuration(),
                S_TutorialSkipInput.WasSkipPressedThisFrame);
        }

        float delayBeforeCountdown = GetPreCountdownDelay();
        if (delayBeforeCountdown > 0f)
            yield return new WaitForSecondsRealtime(delayBeforeCountdown);

        // --- COUNTDOWN PHASE ---
        // Enable gameplay input
        PopTutorialInputLock();

        goalReached = false;
        countdownExpired = false;

        if (countdownTimer != null && levelConfig.timeLimit > 0f)
        {
            // Start countdown in parallel
            countdownRoutine = StartCoroutine(countdownTimer.StartCountdown(levelConfig.timeLimit));

            // Wait for either success or timeout
            yield return new WaitUntil(() => goalReached || countdownExpired);
        }
        else
        {
            // No countdown - just wait for goal
            yield return new WaitUntil(() => goalReached);
        }

        // --- RESULT ---
        if (goalReached)
        {
            // Success - run flow advances from the LevelCompleted event.
            StopCountdown();
            yield break;
        }

        if (countdownExpired)
        {
            // Fail - player dies
            PushTutorialInputLock();

            if (voiceLinePlayer != null)
                voiceLinePlayer.ClearSubtitle();

            // Short delay before death
            yield return new WaitForSecondsRealtime(GetTimeoutDeathDelay());
            S_GameEvent.PlayerDied();
        }
    }

    private void HandleCountdownFinished()
    {
        if (!goalReached)
            countdownExpired = true;
    }

    private void HandleGoalReached(S_LevelCompletionReason reason)
    {
        goalReached = true;

        // Stop countdown if still running
        if (countdownTimer != null && countdownTimer.IsRunning)
            StopCountdown();
    }

    private void ApplyConfiguredSkillSet()
    {
        if (levelConfig == null)
            return;

        S_SkillTree skillTree = S_SkillTree.Instance != null
            ? S_SkillTree.Instance
            : FindAnyObjectByType<S_SkillTree>();

        if (skillTree == null)
        {
            Debug.LogWarning("[TutorialController] No S_SkillTree found in scene.");
            return;
        }

        skillTree.ApplyTutorialSkillSet(levelConfig.skillsToUnlock);
    }

    private void StopCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        if (countdownTimer != null)
            countdownTimer.StopCountdown();
    }

    private void CleanupActiveTutorialFlow()
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }

        StopCountdown();

        if (voiceLinePlayer != null)
            voiceLinePlayer.ClearSubtitle();

        if (tutorialPrompt != null)
            tutorialPrompt.Hide();

        PopTutorialInputLock();
    }

    private void PushTutorialInputLock()
    {
        if (tutorialInputLocked)
            return;

        S_GameEvent.PushGameplayInputLock(TutorialInputLockId);
        tutorialInputLocked = true;
    }

    private void PopTutorialInputLock()
    {
        if (!tutorialInputLocked)
            return;

        S_GameEvent.PopGameplayInputLock(TutorialInputLockId);
        tutorialInputLocked = false;
    }

    private float GetStartupDelay()
    {
        return Mathf.Max(0f, levelConfig != null ? levelConfig.startupDelay : startupDelay);
    }

    private float GetPanToTargetDuration()
    {
        return Mathf.Max(0f, levelConfig != null ? levelConfig.panToTargetDuration : panToTargetDuration);
    }

    private float GetPanHoldDuration()
    {
        return Mathf.Max(0f, levelConfig != null ? levelConfig.panHoldDuration : panHoldDuration);
    }

    private float GetPanReturnDuration()
    {
        return Mathf.Max(0f, levelConfig != null ? levelConfig.panReturnDuration : panReturnDuration);
    }

    private float GetPreCountdownDelay()
    {
        return Mathf.Max(0f, levelConfig != null ? levelConfig.preCountdownDelay : preCountdownDelay);
    }

    private float GetTimeoutDeathDelay()
    {
        return Mathf.Max(0f, levelConfig != null ? levelConfig.timeoutDeathDelay : timeoutDeathDelay);
    }
}
