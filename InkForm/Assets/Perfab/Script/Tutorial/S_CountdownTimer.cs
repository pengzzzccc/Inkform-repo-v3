using System;
using System.Collections;
using TMPro;
using UnityEngine;

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
}
