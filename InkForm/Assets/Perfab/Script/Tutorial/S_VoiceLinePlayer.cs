using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class S_VoiceLinePlayer : MonoBehaviour
{
    [Header("Subtitle UI")]
    [SerializeField] private TMP_Text subtitleText;

    [Header("Settings")]
    [SerializeField] private float fallbackDisplayDuration = 3f;
    [SerializeField] private float typeSpeed = 0.04f;

    private AudioSource audioSource;
    private Coroutine activeRoutine;

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
        bool hasAudio = clip != null;
        if (hasAudio)
            audioSource.PlayOneShot(clip);

        if (subtitleText != null)
        {
            subtitleText.gameObject.SetActive(true);
            yield return TypeSubtitle(subtitle);
        }

        if (hasAudio)
            yield return new WaitWhile(() => audioSource.isPlaying);
        else
        {
            // No audio: display subtitle for fallback duration
            yield return new WaitForSecondsRealtime(fallbackDisplayDuration);
        }

        // Clear subtitle
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
            subtitleText.text += text[i];
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
    }

    public void ClearSubtitle()
    {
        if (subtitleText != null)
            subtitleText.text = string.Empty;
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
