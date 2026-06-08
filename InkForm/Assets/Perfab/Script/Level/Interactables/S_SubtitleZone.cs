using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Mountable subtitle trigger zone. When the player enters this object's 2D trigger, waits an
/// adjustable delay and then shows a subtitle. Two inspector-authored texts are provided — one
/// for keyboard/mouse, one for gamepad — and the visible one switches live with the player's
/// current input device.
///
/// The subtitle UI is generated at runtime and shared statically across all zones (one overlay
/// canvas reused), so only one subtitle shows at a time. Everything lives in this single class.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class S_SubtitleZone : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("Seconds to wait after the player enters before the subtitle appears.")]
    [SerializeField, Min(0f)] private float delay = 0.5f;
    [Tooltip("Hide the subtitle when the player leaves the zone.")]
    [SerializeField] private bool hideOnExit = true;
    [Tooltip("Auto-hide after this many seconds (0 = stay until the player leaves).")]
    [SerializeField, Min(0f)] private float displayDuration = 0f;
    [Tooltip("Only show once for the whole session.")]
    [SerializeField] private bool fireOnce = false;

    [Header("Subtitle Text")]
    [TextArea(2, 5)]
    [SerializeField] private string keyboardText = "Keyboard / mouse hint";
    [TextArea(2, 5)]
    [SerializeField] private string gamepadText = "Gamepad hint";

    [Header("On Shown")]
    [SerializeField] private S_GameEventInvoker onShown = new S_GameEventInvoker();

    [Header("Style")]
    [SerializeField] private float fontSize = 28f;
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.42f);

    private Coroutine showRoutine;
    private Coroutine autoHideRoutine;
    private bool shown;

    // ── Shared runtime UI (generated once, reused by every zone) ──────────────────────
    private static Canvas s_canvas;
    private static GameObject s_panel;
    private static TMP_Text s_label;
    private static S_SubtitleZone s_owner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_canvas = null;
        s_panel = null;
        s_label = null;
        s_owner = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!S_PlayerLookup.IsPlayer(other))
            return;

        if (fireOnce && shown)
            return;

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowAfterDelay());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!hideOnExit || !S_PlayerLookup.IsPlayer(other))
            return;

        CancelPending();
        if (s_owner == this)
            HideSubtitle();
    }

    private void OnDisable()
    {
        CancelPending();
        if (s_owner == this)
            HideSubtitle();
    }

    private void Update()
    {
        // While we own the subtitle, keep the text in sync with the active input device.
        if (s_owner == this && s_label != null)
            s_label.text = ResolveText();
    }

    private IEnumerator ShowAfterDelay()
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        showRoutine = null;
        ShowSubtitle();
    }

    private void ShowSubtitle()
    {
        EnsureUI();

        s_owner = this;
        s_label.text = ResolveText();
        s_panel.SetActive(true);
        shown = true;

        onShown.Invoke();

        if (autoHideRoutine != null)
            StopCoroutine(autoHideRoutine);

        if (displayDuration > 0f)
            autoHideRoutine = StartCoroutine(AutoHideAfterDuration());
    }

    private IEnumerator AutoHideAfterDuration()
    {
        yield return new WaitForSeconds(displayDuration);
        autoHideRoutine = null;
        if (s_owner == this)
            HideSubtitle();
    }

    private void HideSubtitle()
    {
        if (s_panel != null)
            s_panel.SetActive(false);

        s_owner = null;
    }

    private void CancelPending()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        if (autoHideRoutine != null)
        {
            StopCoroutine(autoHideRoutine);
            autoHideRoutine = null;
        }
    }

    private string ResolveText()
    {
        if (IsUsingGamepad())
            return string.IsNullOrEmpty(gamepadText) ? keyboardText : gamepadText;

        return string.IsNullOrEmpty(keyboardText) ? gamepadText : keyboardText;
    }

    /// <summary>True when a gamepad was used more recently than the keyboard/mouse.</summary>
    private static bool IsUsingGamepad()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null)
            return false;

        double keyboardTime = Keyboard.current != null ? Keyboard.current.lastUpdateTime : 0d;
        double mouseTime = Mouse.current != null ? Mouse.current.lastUpdateTime : 0d;
        return gamepad.lastUpdateTime > Math.Max(keyboardTime, mouseTime);
    }

    // ── Runtime UI generation ─────────────────────────────────────────────────────────
    private void EnsureUI()
    {
        if (s_canvas != null && s_panel != null && s_label != null)
            return;

        GameObject canvasObject = new GameObject("SubtitleCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);

        s_canvas = canvasObject.GetComponent<Canvas>();
        s_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        s_canvas.sortingOrder = 820;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366f, 768f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        s_panel = new GameObject("SubtitlePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        s_panel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = s_panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 42f);
        panelRect.sizeDelta = new Vector2(860f, 92f);

        Image panelImage = s_panel.GetComponent<Image>();
        panelImage.color = panelColor;
        panelImage.raycastTarget = false;

        GameObject textObject = new GameObject("SubtitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(s_panel.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(28f, 12f);
        textRect.offsetMax = new Vector2(-28f, -12f);

        s_label = textObject.GetComponent<TextMeshProUGUI>();
        s_label.alignment = TextAlignmentOptions.Center;
        s_label.fontSize = fontSize;
        s_label.textWrappingMode = TextWrappingModes.Normal;
        s_label.color = Color.white;
        s_label.raycastTarget = false;

        s_panel.SetActive(false);
    }
}
