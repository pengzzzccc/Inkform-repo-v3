using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class S_VoiceLinePlayer : MonoBehaviour
{
    [Header("Subtitle UI")]
    [SerializeField] private TMP_Text subtitleText;

    [Header("Settings")]
    [SerializeField] private float fallbackDisplayDuration = 3f;
    [SerializeField] private float typeSpeed = 0.04f;
    [SerializeField] private bool allowSkip = true;

    private AudioSource audioSource;
    private bool skipRequested;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        EnsureUIBuilt();

        if (subtitleText != null)
            subtitleText.text = string.Empty;
    }

    /// <summary>
    /// Play a voice line and show subtitle text. Yields until playback/display finishes.
    /// </summary>
    public IEnumerator PlayVoiceLine(AudioClip clip, string subtitle)
    {
        if (clip != null)
            yield return PlayVoiceSequence(new[] { clip }, subtitle, 0f);
        else
            yield return PlayVoiceSequence(null, subtitle, 0f);
    }

    public IEnumerator PlayVoiceSequence(IReadOnlyList<AudioClip> clips, string subtitle, float clipGap = 0f)
    {
        StopPlayback();
        skipRequested = false;

        bool hasAudio = HasAnyClip(clips);
        bool audioFinished = !hasAudio;
        Coroutine audioRoutine = null;

        if (hasAudio)
            audioRoutine = StartCoroutine(PlayAudioSequence(clips, Mathf.Max(0f, clipGap), () => audioFinished = true));

        if (subtitleText != null)
        {
            subtitleText.gameObject.SetActive(true);
            yield return TypeSubtitle(subtitle);
        }

        if (hasAudio)
        {
            while (!audioFinished && !skipRequested)
            {
                if (WasSkipPressed())
                    skipRequested = true;

                yield return null;
            }
        }
        else
        {
            float endTime = Time.unscaledTime + fallbackDisplayDuration;
            while (Time.unscaledTime < endTime && !skipRequested)
            {
                if (WasSkipPressed())
                    skipRequested = true;

                yield return null;
            }
        }

        if (skipRequested)
        {
            if (audioRoutine != null)
                StopCoroutine(audioRoutine);

            StopPlayback();
        }

        if (subtitleText != null)
            subtitleText.text = string.Empty;

        S_GameEvent.VoiceLineFinished();
    }

    private IEnumerator TypeSubtitle(string text)
    {
        if (subtitleText == null || string.IsNullOrEmpty(text))
            yield break;

        subtitleText.text = string.Empty;
        for (int i = 0; i < text.Length; i++)
        {
            if (WasSkipPressed())
            {
                skipRequested = true;
                subtitleText.text = text;
                yield break;
            }

            subtitleText.text += text[i];
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
    }

    public void ClearSubtitle()
    {
        StopPlayback();

        if (subtitleText != null)
            subtitleText.text = string.Empty;
    }

    private IEnumerator PlayAudioSequence(IReadOnlyList<AudioClip> clips, float clipGap, System.Action onFinished)
    {
        for (int i = 0; clips != null && i < clips.Count; i++)
        {
            AudioClip clip = clips[i];
            if (clip == null)
                continue;

            audioSource.clip = clip;
            audioSource.Play();

            while (audioSource.isPlaying && !skipRequested)
                yield return null;

            if (skipRequested)
                break;

            if (clipGap > 0f && i < clips.Count - 1)
            {
                float endTime = Time.unscaledTime + clipGap;
                while (Time.unscaledTime < endTime && !skipRequested)
                    yield return null;
            }
        }

        onFinished?.Invoke();
    }

    private bool HasAnyClip(IReadOnlyList<AudioClip> clips)
    {
        if (clips == null)
            return false;

        for (int i = 0; i < clips.Count; i++)
        {
            if (clips[i] != null)
                return true;
        }

        return false;
    }

    private bool WasSkipPressed()
    {
        if (!allowSkip)
            return false;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null
            && (keyboard.spaceKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame
                || keyboard.escapeKey.wasPressedThisFrame))
        {
            return true;
        }

        Gamepad gamepad = Gamepad.current;
        return gamepad != null
            && (gamepad.buttonSouth.wasPressedThisFrame
                || gamepad.startButton.wasPressedThisFrame);
    }

    private void StopPlayback()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
    }

    private void OnDisable()
    {
        StopPlayback();
    }

    private void EnsureUIBuilt()
    {
        if (subtitleText != null)
            return;

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("TutorialSubtitleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 820;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1366f, 768f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject panelObject = new GameObject("SubtitlePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 42f);
        panelRect.sizeDelta = new Vector2(860f, 92f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.42f);
        panelImage.raycastTarget = false;

        GameObject textObject = new GameObject("SubtitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(28f, 12f);
        textRect.offsetMax = new Vector2(-28f, -12f);

        subtitleText = textObject.GetComponent<TMP_Text>();
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.fontSize = 28f;
        subtitleText.textWrappingMode = TextWrappingModes.Normal;
        subtitleText.raycastTarget = false;
    }
}
