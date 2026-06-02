using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal reusable dialogue box. Builds its own screen-space canvas at runtime
/// (bottom panel with a speaker name and a typed-out body line) and advances on the
/// player's Interact input. Gameplay input is locked for the duration via the
/// shared input-lock events.
///
/// Lazy singleton: callers use S_DialogueUI.EnsureExists().Begin(...). Driven by
/// S_NPCDialogue, but any system can show a linear dialogue through Begin().
/// </summary>
public class S_DialogueUI : MonoBehaviour
{
    private const string InputLockId = "Dialogue";

    public static S_DialogueUI Instance { get; private set; }

    [Header("Layout")]
    [SerializeField] private int canvasSortingOrder = 840;
    [SerializeField] private float defaultTextSpeed = 0.04f;

    private Canvas canvas;
    private GameObject panel;
    private TMP_Text speakerLabel;
    private TMP_Text bodyLabel;

    private Coroutine dialogueRoutine;
    private bool inputLockHeld;

    public bool IsActive { get; private set; }

    public static S_DialogueUI EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameObject host = new GameObject("DialogueUI");
        return host.AddComponent<S_DialogueUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildUI();
        HidePanel();
    }

    private void OnDisable()
    {
        StopActiveDialogue();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Show a linear dialogue. Ignored if a dialogue is already running.</summary>
    public void Begin(string speaker, IReadOnlyList<string> lines, float textSpeed, Action onFinished = null)
    {
        if (IsActive || lines == null || lines.Count == 0)
            return;

        float speed = textSpeed > 0f ? textSpeed : defaultTextSpeed;
        dialogueRoutine = StartCoroutine(RunDialogue(speaker, lines, speed, onFinished));
    }

    private IEnumerator RunDialogue(string speaker, IReadOnlyList<string> lines, float textSpeed, Action onFinished)
    {
        IsActive = true;
        PushInputLock();

        if (speakerLabel != null)
            speakerLabel.text = speaker;

        if (panel != null)
            panel.SetActive(true);

        try
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i] ?? string.Empty;

                // Type the line out; pressing Interact mid-type reveals it instantly.
                bool revealed = false;
                if (bodyLabel != null)
                    bodyLabel.text = string.Empty;

                for (int c = 0; c < line.Length; c++)
                {
                    if (S_PlayerInteractInput.WasPressedThisFrame())
                    {
                        revealed = true;
                        break;
                    }

                    if (bodyLabel != null)
                        bodyLabel.text += line[c];

                    yield return new WaitForSecondsRealtime(textSpeed);
                }

                if (revealed && bodyLabel != null)
                    bodyLabel.text = line;

                // Wait for an Interact press to advance to the next line.
                // Skip the frame the line was just revealed on so the same press
                // does not both reveal and advance.
                if (revealed)
                    yield return null;

                while (!S_PlayerInteractInput.WasPressedThisFrame())
                    yield return null;
            }
        }
        finally
        {
            HidePanel();
            PopInputLock();
            IsActive = false;
            dialogueRoutine = null;
            onFinished?.Invoke();
        }
    }

    private void StopActiveDialogue()
    {
        if (dialogueRoutine != null)
        {
            StopCoroutine(dialogueRoutine);
            dialogueRoutine = null;
        }

        HidePanel();
        PopInputLock();
        IsActive = false;
    }

    private void PushInputLock()
    {
        if (inputLockHeld)
            return;

        inputLockHeld = true;
        S_GameEvent.PushGameplayInputLock(InputLockId);
    }

    private void PopInputLock()
    {
        if (!inputLockHeld)
            return;

        inputLockHeld = false;
        S_GameEvent.PopGameplayInputLock(InputLockId);
    }

    private void HidePanel()
    {
        if (panel != null)
            panel.SetActive(false);

        if (bodyLabel != null)
            bodyLabel.text = string.Empty;
    }

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject(
            "DialogueCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366f, 768f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 48f);
        panelRect.sizeDelta = new Vector2(1040f, 170f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.42f);
        panelImage.raycastTarget = false;

        speakerLabel = CreateLabel("SpeakerText", panel.transform, 26f, FontStyles.Bold);
        RectTransform speakerRect = speakerLabel.rectTransform;
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(1f, 1f);
        speakerRect.pivot = new Vector2(0f, 1f);
        speakerRect.offsetMin = new Vector2(28f, -46f);
        speakerRect.offsetMax = new Vector2(-28f, -12f);
        speakerLabel.alignment = TextAlignmentOptions.Left;
        speakerLabel.color = new Color(0.7f, 0.88f, 1f, 1f);

        bodyLabel = CreateLabel("BodyText", panel.transform, 28f, FontStyles.Normal);
        RectTransform bodyRect = bodyLabel.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(28f, 16f);
        bodyRect.offsetMax = new Vector2(-28f, -52f);
        bodyLabel.alignment = TextAlignmentOptions.TopLeft;
        bodyLabel.textWrappingMode = TextWrappingModes.Normal;
    }

    private TMP_Text CreateLabel(string objectName, Transform parent, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }
}
