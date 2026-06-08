using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Drives the END scene outro: keeps the screen black on entry, plays an ending
/// audio clip, then slowly fades a centered subtitle in, holds it, fades it out,
/// and finally reveals the "Thanks for playing" text.
///
/// The overlay (black mask + subtitle + thanks text) is an authored prefab placed
/// directly in the END scene. Drag the CanvasGroup/label references onto this
/// component in the inspector.
/// </summary>
public class S_EndingSequence : MonoBehaviour
{
    public static S_EndingSequence Instance { get; private set; }

    [Header("Audio")]
    [Tooltip("Stop the background music when the ending begins so the END scene stays silent except for the ending clip.")]
    [SerializeField] private bool stopBackgroundMusic = true;
    [Tooltip("Ending audio clip played first. Leave empty to fall back to a timed delay.")]
    [SerializeField] private AudioClip endingClip;
    [SerializeField, Min(0f)] private float fallbackAudioDuration = 4f;

    [Header("Subtitle")]
    [TextArea]
    [SerializeField] private string subtitleText = "[ Subtitle placeholder ]";
    [SerializeField, Min(0f)] private float subtitleFadeInTime = 1.5f;
    [SerializeField, Min(0f)] private float subtitleHoldTime = 3f;
    [SerializeField, Min(0f)] private float subtitleFadeOutTime = 1f;

    [Header("Thanks")]
    [SerializeField] private string thanksText = "Thanks for playing";
    [SerializeField, Min(0f)] private float thanksFadeInTime = 1f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float delayBeforeAudio = 0.5f;
    [SerializeField, Min(0f)] private float gapBeforeThanks = 0.5f;

    [Header("UI Refs")]
    [SerializeField] private CanvasGroup subtitleGroup;
    [SerializeField] private TextMeshProUGUI subtitleLabel;
    [SerializeField] private CanvasGroup thanksGroup;
    [SerializeField] private TextMeshProUGUI thanksLabel;

    private AudioSource audioSource;
    private Coroutine sequenceRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Silence the BGM carried over from the previous scene the instant END loads.
        // Goes through the event bus so the AudioManager also suppresses its Start() auto-play.
        if (stopBackgroundMusic)
            S_GameEvent.StopBgmRequested();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Start()
    {
        sequenceRoutine = StartCoroutine(RunEndingSequence());
    }

    private void OnDisable()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        if (audioSource != null)
            audioSource.Stop();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private IEnumerator RunEndingSequence()
    {
        // Keep the screen black: the mask Image starts opaque, subtitle/thanks hidden.
        SetGroupAlpha(subtitleGroup, 0f);
        SetGroupAlpha(thanksGroup, 0f);

        if (delayBeforeAudio > 0f)
            yield return new WaitForSecondsRealtime(delayBeforeAudio);

        // Play the ending audio (or wait the fallback duration if none is assigned).
        if (endingClip != null && audioSource != null)
        {
            audioSource.clip = endingClip;
            audioSource.Play();
            while (audioSource.isPlaying)
                yield return null;
        }
        else if (fallbackAudioDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(fallbackAudioDuration);
        }

        // Subtitle: slow fade in, hold, fade out.
        if (subtitleLabel != null)
            subtitleLabel.text = subtitleText;

        yield return FadeCanvasGroup(subtitleGroup, 0f, 1f, subtitleFadeInTime);

        if (subtitleHoldTime > 0f)
            yield return new WaitForSecondsRealtime(subtitleHoldTime);

        yield return FadeCanvasGroup(subtitleGroup, 1f, 0f, subtitleFadeOutTime);

        if (gapBeforeThanks > 0f)
            yield return new WaitForSecondsRealtime(gapBeforeThanks);

        // Thanks for playing.
        if (thanksLabel != null)
            thanksLabel.text = thanksText;

        yield return FadeCanvasGroup(thanksGroup, 0f, 1f, thanksFadeInTime);

        sequenceRoutine = null;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }

    private static void SetGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
            group.alpha = alpha;
    }
}
