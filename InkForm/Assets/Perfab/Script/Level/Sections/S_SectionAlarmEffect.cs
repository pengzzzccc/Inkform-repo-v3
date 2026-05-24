using UnityEngine;

public class S_SectionAlarmEffect : MonoBehaviour
{
    [SerializeField, Min(0f)] private float flashFrequency = 8f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.35f;
    [SerializeField, Min(0f)] private float fadeOutTime = 0.15f;

    private bool flashing;
    private bool fadingOut;
    private float flashTimer;
    private float fadeTimer;
    private float fadeStartAlpha;
    private float currentAlpha;

    void OnEnable()
    {
        S_GameEvent.OnSectionDescentStarted += StartAlarmEffect;
        S_GameEvent.OnSectionDescentCompleted += StopAlarmEffect;
    }

    void OnDisable()
    {
        S_GameEvent.OnSectionDescentStarted -= StartAlarmEffect;
        S_GameEvent.OnSectionDescentCompleted -= StopAlarmEffect;
    }

    void Update()
    {
        if (flashing)
        {
            flashTimer += Time.unscaledDeltaTime;
            float pulse = (Mathf.Sin(flashTimer * flashFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
            currentAlpha = pulse * maxAlpha;
            return;
        }

        if (fadingOut)
        {
            fadeTimer -= Time.unscaledDeltaTime;
            float t = fadeOutTime > 0f ? Mathf.Clamp01(fadeTimer / fadeOutTime) : 0f;
            currentAlpha = fadeStartAlpha * t;

            if (fadeTimer <= 0f)
            {
                fadingOut = false;
                currentAlpha = 0f;
            }
        }
    }

    void OnGUI()
    {
        if (currentAlpha <= 0f)
            return;

        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 0f, 0f, currentAlpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private void StartAlarmEffect(int sectionIndex)
    {
        flashing = true;
        fadingOut = false;
        flashTimer = 0f;
    }

    private void StopAlarmEffect(int sectionIndex)
    {
        flashing = false;
        fadeStartAlpha = currentAlpha;
        fadeTimer = fadeOutTime;
        fadingOut = fadeOutTime > 0f && currentAlpha > 0f;

        if (!fadingOut)
            currentAlpha = 0f;
    }
}
