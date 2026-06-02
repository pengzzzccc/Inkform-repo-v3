using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A grappling hook anchor point. Passive: it registers itself in a static list so the
/// player's Hook skill can scan nearby anchors (no per-hook trigger / layer setup needed).
/// Builds a small world-space "interact key" prompt at runtime that the skill toggles on
/// the currently selected hook via SetPromptVisible.
/// </summary>
public class S_Hook : MonoBehaviour
{
    /// <summary>All enabled hooks in the loaded scenes. The Hook skill scans this.</summary>
    public static readonly List<S_Hook> All = new List<S_Hook>();

    [Header("Prompt")]
    [Tooltip("Text shown in the world-space prompt (the interact key).")]
    [SerializeField] private string interactKeyLabel = "E";
    [SerializeField] private Vector2 promptWorldOffset = new Vector2(0f, 0.9f);
    [SerializeField] private float promptWorldScale = 0.01f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.6f, 0.2f, 0.85f);

    private Canvas promptCanvas;
    private GameObject promptRoot;
    private TMP_Text promptLabel;
    private bool promptBuilt;

    public Vector2 AnchorPosition => transform.position;

    private void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
        SetPromptVisible(false);
    }

    /// <summary>Show or hide the world-space interact prompt above this hook.</summary>
    public void SetPromptVisible(bool visible)
    {
        if (visible && !promptBuilt)
            BuildPrompt();

        if (promptRoot != null)
            promptRoot.SetActive(visible);
    }

    private void BuildPrompt()
    {
        promptBuilt = true;

        promptRoot = new GameObject("HookPrompt", typeof(RectTransform), typeof(Canvas));
        promptRoot.transform.SetParent(transform, false);
        promptRoot.transform.localPosition = promptWorldOffset;

        promptCanvas = promptRoot.GetComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = promptRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(120f, 120f);
        canvasRect.localScale = Vector3.one * promptWorldScale;

        GameObject bg = new GameObject("Bg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(promptRoot.transform, false);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(96f, 96f);
        Image bgImage = bg.GetComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.55f);
        bgImage.raycastTarget = false;

        GameObject label = new GameObject("Key", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(promptRoot.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(96f, 96f);

        promptLabel = label.GetComponent<TextMeshProUGUI>();
        promptLabel.text = interactKeyLabel;
        promptLabel.alignment = TextAlignmentOptions.Center;
        promptLabel.fontSize = 64f;
        promptLabel.fontStyle = FontStyles.Bold;
        promptLabel.color = new Color(0.94f, 0.98f, 1f, 1f);
        promptLabel.raycastTarget = false;

        promptRoot.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)promptWorldOffset);
#if UNITY_EDITOR
        UnityEditor.Handles.color = gizmoColor;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.4f, "Hook");
#endif
    }
}
