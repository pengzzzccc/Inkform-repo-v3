using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class S_EndThanksUI
{
    private const string EndSceneName = "END";
    private const string CanvasName = "ThanksForPlayingCanvas";
    private static bool isHooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        if (isHooked)
            SceneManager.sceneLoaded -= HandleSceneLoaded;

        isHooked = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (!isHooked)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            isHooked = true;
        }

        BuildIfEndScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BuildIfEndScene(scene);
    }

    private static void BuildIfEndScene(Scene scene)
    {
        if (!IsEndScene(scene) || GameObject.Find(CanvasName) != null)
            return;

        GameObject canvasObject = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366f, 768f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textObject = new GameObject("ThanksForPlayingText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Shadow));
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(900f, 160f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "Thanks for playing";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 72f;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(0.94f, 0.98f, 1f, 1f);
        text.raycastTarget = false;

        Shadow shadow = textObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        shadow.effectDistance = new Vector2(3f, -3f);
    }

    private static bool IsEndScene(Scene scene)
    {
        return string.Equals(scene.name, EndSceneName, System.StringComparison.OrdinalIgnoreCase)
            || scene.path.EndsWith("/END.unity", System.StringComparison.OrdinalIgnoreCase)
            || scene.path.EndsWith("\\END.unity", System.StringComparison.OrdinalIgnoreCase);
    }
}
