using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class S_CountdownTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private GameObject countdownPanel;

    [Header("Settings")]
    [SerializeField] private float pulseThreshold = 5f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.3f, 0.3f, 1f);

    private float currentTime;
    private bool isRunning;
    private Coroutine countdownRoutine;

    public bool IsRunning => isRunning;
    public float CurrentTime => currentTime;

    void Awake()
    {
        EnsureUIBuilt();
        if (countdownPanel != null)
            countdownPanel.SetActive(false);
    }

    /// <summary>
    /// Start countdown from given seconds. Yields until finished (reached zero).
    /// </summary>
    public IEnumerator StartCountdown(float seconds)
    {
        StopCountdown();

        currentTime = seconds;
        isRunning = true;
        countdownRoutine = null;

        if (countdownPanel != null)
            countdownPanel.SetActive(true);

        UpdateDisplay();
        S_GameEvent.CountdownStarted();

        while (isRunning && currentTime > 0f)
        {
            yield return null;
            currentTime -= Time.deltaTime;

            if (currentTime < 0f)
                currentTime = 0f;

            UpdateDisplay();
            S_GameEvent.CountdownTick(currentTime);
        }

        if (!isRunning)
        {
            if (countdownPanel != null)
                countdownPanel.SetActive(false);

            countdownRoutine = null;
            yield break;
        }

        isRunning = false;
        countdownRoutine = null;

        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        S_GameEvent.CountdownFinished();
    }

    public void StopCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        isRunning = false;

        if (countdownPanel != null)
            countdownPanel.SetActive(false);
    }

    private void UpdateDisplay()
    {
        if (countdownText == null)
            return;

        int displaySeconds = Mathf.CeilToInt(currentTime);
        countdownText.text = displaySeconds.ToString();

        // Color change for warning
        if (currentTime <= pulseThreshold)
        {
            countdownText.color = warningColor;
            // Simple pulse effect
            float pulse = 1f + 0.2f * Mathf.Sin(Time.time * 6f);
            countdownText.transform.localScale = Vector3.one * pulse;
        }
        else
        {
            countdownText.color = normalColor;
            countdownText.transform.localScale = Vector3.one;
        }
    }

    private void EnsureUIBuilt()
    {
        if (countdownText != null && countdownPanel != null)
            return;

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("TutorialCountdownCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 850;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1366f, 768f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (countdownPanel == null)
        {
            GameObject panelObject = new GameObject("CountdownPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(canvas.transform, false);
            countdownPanel = panelObject;

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -34f);
            panelRect.sizeDelta = new Vector2(160f, 72f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.35f);
            panelImage.raycastTarget = false;
        }

        if (countdownText == null)
        {
            GameObject textObject = new GameObject("CountdownText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(countdownPanel.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            countdownText = textObject.GetComponent<TMP_Text>();
            countdownText.alignment = TextAlignmentOptions.Center;
            countdownText.fontSize = 42f;
            countdownText.fontStyle = FontStyles.Bold;
            countdownText.raycastTarget = false;
        }
    }
}
