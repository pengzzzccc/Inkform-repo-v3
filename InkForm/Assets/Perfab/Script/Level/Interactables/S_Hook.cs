using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A grappling hook anchor point. Passive: it registers itself in a static list so the
/// player's Hook skill can scan nearby anchors (no per-hook trigger / layer setup needed).
/// Its own authored world-space prompt (an inactive child object) is toggled by the skill via
/// SetPromptVisible, so the prompt doubles as the "this hook is selected" highlight. The key
/// label follows the current input mapping.
/// </summary>
public class S_Hook : MonoBehaviour
{
    /// <summary>All enabled hooks in the loaded scenes. The Hook skill scans this.</summary>
    public static readonly List<S_Hook> All = new List<S_Hook>();

    [Header("Prompt")]
    [Tooltip("Input action whose mapped key is shown. Leave empty to always use Fallback Label.")]
    [SerializeField] private string actionName;
    [SerializeField] private string bindingGroup = "Keyboard&Mouse";
    [SerializeField] private string partName;
    [SerializeField] private string devicePath;
    [Tooltip("Shown when no action is set or the binding can't be resolved.")]
    [SerializeField] private string fallbackLabel = "E";
    [SerializeField] private Vector2 promptWorldOffset = new Vector2(0f, 0.9f);

    [Header("Prompt Refs")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptLabel;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.6f, 0.2f, 0.85f);

    public Vector2 AnchorPosition => transform.position;

    private void Awake()
    {
        if (promptRoot != null)
            promptRoot.SetActive(false);

        RefreshLabel();
    }

    private void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);

        if (S_InputBindingManager.HasInstance)
            S_InputBindingManager.Instance.BindingsChanged += RefreshLabel;
    }

    private void OnDisable()
    {
        All.Remove(this);

        if (S_InputBindingManager.HasInstance)
            S_InputBindingManager.Instance.BindingsChanged -= RefreshLabel;

        SetPromptVisible(false);
    }

    /// <summary>Show or hide the world-space interact prompt above this hook.</summary>
    public void SetPromptVisible(bool visible)
    {
        if (promptRoot != null)
            promptRoot.SetActive(visible);

        if (visible)
            RefreshLabel();
    }

    /// <summary>Re-resolve the mapped key and update the prompt label.</summary>
    private void RefreshLabel()
    {
        if (promptLabel == null)
            return;

        promptLabel.text = ResolveDisplay();
    }

    private string ResolveDisplay()
    {
        if (!string.IsNullOrWhiteSpace(actionName) && S_InputBindingManager.HasInstance)
        {
            string display = S_InputBindingManager.Instance.GetBindingDisplayString(actionName, bindingGroup, partName, devicePath);
            if (!string.IsNullOrWhiteSpace(display) && display != "-")
                return display;
        }

        return fallbackLabel;
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
