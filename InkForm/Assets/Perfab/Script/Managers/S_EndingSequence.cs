using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the END scene outro: keeps the screen black on entry, plays an ending
/// audio clip, then slowly fades a centered subtitle in, holds it, fades it out,
/// and finally reveals the "Thanks for playing" text.
///
/// Builds its own overlay canvas at runtime (black mask + subtitle + thanks text)
/// so the END scene needs no manual setup. Auto-injects itself into the END scene
/// via scene-load hooks, the same way the retired S_EndThanksUI did. It can also
/// be placed manually on a scene object.
/// </summary>
public class S_EndingSequence : MonoBehaviour
{
    private const string EndSceneName = "END";
    private const string SequenceObjectName = "EndingSequence";

    private static bool sceneHookRegistered;

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

    private AudioSource audioSource;
    private CanvasGroup subtitleGroup;
    private TextMeshProUGUI subtitleLabel;
    private CanvasGroup thanksGroup;
    private TextMeshProUGUI thanksLabel;
    private Coroutine sequenceRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        if (sceneHookRegistered)
            SceneManager.sceneLoaded -= HandleSceneLoaded;

        sceneHookRegistered = false;
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (!sceneHookRegistered)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneHookRegistered = true;
        }

        EnsureForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    private static void EnsureForScene(Scene scene)
    {
        if (!Application.isPlaying || !IsEndScene(scene) || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].GetComponentInChildren<S_EndingSequence>(true) != null)
                return;
        }

        GameObject sequenceObject = new GameObject(SequenceObjectName);
        SceneManager.MoveGameObjectToScene(sequenceObject, scene);
        sequenceObject.AddComponent<S_EndingSequence>();
    }

    private static bool IsEndScene(Scene scene)
    {
        return string.Equals(scene.name, EndSceneName, System.StringComparison.OrdinalIgnoreCase)
            || scene.path.EndsWith("/END.unity", System.StringComparison.OrdinalIgnoreCase)
            || scene.path.EndsWith("\\END.unity", System.StringComparison.OrdinalIgnoreCase);
    }

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

        BuildUI();
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

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject(
            "EndingSequenceCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Above the END text legacy value (900), below the scene-transition overlay (1000).
        canvas.sortingOrder = 950;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366f, 768f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Full-screen black mask that keeps the scene hidden for the whole sequence.
        GameObject maskObject = new GameObject("BlackMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        maskObject.transform.SetParent(canvasObject.transform, false);

        RectTransform maskRect = maskObject.GetComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = Vector2.zero;
        maskRect.offsetMax = Vector2.zero;

        Image maskImage = maskObject.GetComponent<Image>();
        maskImage.color = Color.black;
        maskImage.raycastTarget = true;

        subtitleGroup = BuildCenteredLabel(canvasObject.transform, "SubtitleText", 48f, FontStyles.Normal, out subtitleLabel);
        thanksGroup = BuildCenteredLabel(canvasObject.transform, "ThanksForPlayingText", 72f, FontStyles.Bold, out thanksLabel);

        subtitleGroup.alpha = 0f;
        thanksGroup.alpha = 0f;
    }

    private CanvasGroup BuildCenteredLabel(Transform parent, string objectName, float fontSize, FontStyles fontStyle, out TextMeshProUGUI label)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(TextMeshProUGUI), typeof(Shadow));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(1100f, 240f);

        label = textObject.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.color = new Color(0.94f, 0.98f, 1f, 1f);
        label.raycastTarget = false;

        Shadow shadow = textObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        shadow.effectDistance = new Vector2(3f, -3f);

        return textObject.GetComponent<CanvasGroup>();
    }
}
