using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class S_UITutorialPrompt : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text checklistText;

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private float offScreenX = 600f;
    [SerializeField] private float onScreenX = -20f;

    private Coroutine activeRoutine;
    private bool waitingForInput;
    private bool waitingForAllInputs;
    private bool allInputsComplete;

    // Tracking state for required actions
    private HashSet<string> keyboardActionsCompleted = new HashSet<string>();
    private HashSet<string> gamepadActionsCompleted = new HashSet<string>();
    private TutorialRequiredAction[] activeRequiredActions;

    void Awake()
    {
        if (panel != null)
            SetPanelOffScreen();
    }

    void Update()
    {
        // Enter / Numpad Enter / gamepad South can skip whichever prompt is showing.
        bool skipPressed = S_TutorialSkipInput.WasSkipPressedThisFrame();

        if (waitingForInput)
        {
            // Dismiss only on the deliberate skip input so practice key presses
            // (movement etc.) don't accidentally close the prompt.
            if (skipPressed)
            {
                waitingForInput = false;
                return;
            }
        }

        if (waitingForAllInputs && activeRequiredActions != null)
        {
            TrackRequiredInputs();
            UpdateChecklistDisplay();

            // Skip closes the checklist immediately without performing every action.
            if (skipPressed)
            {
                allInputsComplete = true;
                waitingForAllInputs = false;
                return;
            }

            // Check completion: keyboard group OR gamepad group fully done
            if (IsKeyboardGroupComplete() || IsGamepadGroupComplete())
            {
                allInputsComplete = true;
                waitingForAllInputs = false;
            }
        }
    }

    /// <summary>
    /// Show prompt, wait for any key, then hide. Yields until dismissed.
    /// </summary>
    public IEnumerator ShowAndWaitForInput(string title, string description)
    {
        if (titleText != null)
            titleText.text = title;
        if (descriptionText != null)
            descriptionText.text = description;

        // Slide in
        yield return SlidePanel(onScreenX);

        // Wait for any input
        waitingForInput = true;
        yield return new WaitUntil(() => !waitingForInput);

        // Slide out
        yield return SlidePanel(offScreenX);

        S_GameEvent.TutorialPromptDismissed();
    }

    /// <summary>
    /// Show prompt with a checklist of required actions. Tracks each key press.
    /// Player can freely move while this is showing.
    /// Dismisses when ALL keyboard actions OR ALL gamepad actions are completed.
    /// If requiredActions is empty, falls back to any-key dismissal.
    /// </summary>
    public IEnumerator ShowAndWaitForAllInputs(string title, string description, TutorialRequiredAction[] requiredActions)
    {
        if (titleText != null)
            titleText.text = title;
        if (descriptionText != null)
            descriptionText.text = description;

        // If no required actions specified, fall back to any-key
        if (requiredActions == null || requiredActions.Length == 0)
        {
            yield return ShowAndWaitForInput(title, description);
            yield break;
        }

        // Reset tracking
        keyboardActionsCompleted.Clear();
        gamepadActionsCompleted.Clear();
        activeRequiredActions = requiredActions;
        allInputsComplete = false;

        // Build checklist text
        if (checklistText != null)
            checklistText.gameObject.SetActive(true);

        yield return SlidePanel(onScreenX);

        // Wait for all inputs
        waitingForAllInputs = true;
        yield return new WaitUntil(() => allInputsComplete);

        // Brief hold so player sees completion
        yield return new WaitForSecondsRealtime(0.5f);

        // Slide out
        activeRequiredActions = null;
        if (checklistText != null)
            checklistText.gameObject.SetActive(false);

        yield return SlidePanel(offScreenX);

        S_GameEvent.TutorialPromptDismissed();
    }

    private void TrackRequiredInputs()
    {
        if (activeRequiredActions == null)
            return;

        for (int i = 0; i < activeRequiredActions.Length; i++)
        {
            var action = activeRequiredActions[i];

            // Check keyboard keys
            if (!IsKeyboardActionComplete(i) && action.keyboardKeys != null)
            {
                foreach (string keyName in action.keyboardKeys)
                {
                    if (IsKeyPressed(keyName))
                    {
                        keyboardActionsCompleted.Add(action.actionName);
                        break;
                    }
                }
            }

            // Check gamepad buttons
            if (!IsGamepadActionComplete(i) && action.gamepadKeys != null)
            {
                foreach (string buttonName in action.gamepadKeys)
                {
                    if (IsGamepadButtonPressed(buttonName))
                    {
                        gamepadActionsCompleted.Add(action.actionName);
                        break;
                    }
                }
            }
        }
    }

    private bool IsKeyboardActionComplete(int actionIndex)
    {
        return activeRequiredActions != null &&
               actionIndex < activeRequiredActions.Length &&
               keyboardActionsCompleted.Contains(activeRequiredActions[actionIndex].actionName);
    }

    private bool IsGamepadActionComplete(int actionIndex)
    {
        return activeRequiredActions != null &&
               actionIndex < activeRequiredActions.Length &&
               gamepadActionsCompleted.Contains(activeRequiredActions[actionIndex].actionName);
    }

    private bool IsKeyboardGroupComplete()
    {
        if (activeRequiredActions == null)
            return false;

        for (int i = 0; i < activeRequiredActions.Length; i++)
        {
            if (activeRequiredActions[i].keyboardKeys != null &&
                activeRequiredActions[i].keyboardKeys.Length > 0 &&
                !keyboardActionsCompleted.Contains(activeRequiredActions[i].actionName))
                return false;
        }
        return true;
    }

    private bool IsGamepadGroupComplete()
    {
        if (activeRequiredActions == null)
            return false;

        for (int i = 0; i < activeRequiredActions.Length; i++)
        {
            if (activeRequiredActions[i].gamepadKeys != null &&
                activeRequiredActions[i].gamepadKeys.Length > 0 &&
                !gamepadActionsCompleted.Contains(activeRequiredActions[i].actionName))
                return false;
        }
        return true;
    }

    private void UpdateChecklistDisplay()
    {
        if (checklistText == null || activeRequiredActions == null)
            return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < activeRequiredActions.Length; i++)
        {
            string name = activeRequiredActions[i].actionName;
            bool kbDone = keyboardActionsCompleted.Contains(name);
            bool gpDone = gamepadActionsCompleted.Contains(name);
            string check = (kbDone || gpDone) ? "<color=#00FF88>[X]</color>" : "[ ]";
            sb.AppendLine($"{check} {name}");
        }

        checklistText.text = sb.ToString();
    }

    private bool IsKeyPressed(string keyName)
    {
        if (Keyboard.current == null)
            return false;

        Key key = ParseKeyName(keyName);
        if (key == Key.None)
            return false;

        return Keyboard.current[key].wasPressedThisFrame;
    }

    private bool IsGamepadButtonPressed(string buttonName)
    {
        if (Gamepad.current == null)
            return false;

        switch (buttonName)
        {
            case "ButtonSouth": return Gamepad.current.buttonSouth.wasPressedThisFrame;
            case "ButtonEast": return Gamepad.current.buttonEast.wasPressedThisFrame;
            case "ButtonNorth": return Gamepad.current.buttonNorth.wasPressedThisFrame;
            case "ButtonWest": return Gamepad.current.buttonWest.wasPressedThisFrame;
            case "LeftStick": return Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.25f;
            case "RightStick": return Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.25f;
            case "LeftShoulder": return Gamepad.current.leftShoulder.wasPressedThisFrame;
            case "RightShoulder": return Gamepad.current.rightShoulder.wasPressedThisFrame;
            case "Start": return Gamepad.current.startButton.wasPressedThisFrame;
            case "Select": return Gamepad.current.selectButton.wasPressedThisFrame;
            case "DpadUp": return Gamepad.current.dpad.up.wasPressedThisFrame;
            case "DpadDown": return Gamepad.current.dpad.down.wasPressedThisFrame;
            case "DpadLeft": return Gamepad.current.dpad.left.wasPressedThisFrame;
            case "DpadRight": return Gamepad.current.dpad.right.wasPressedThisFrame;
            default: return false;
        }
    }

    private Key ParseKeyName(string name)
    {
        switch (name.ToUpperInvariant())
        {
            case "W": return Key.W;
            case "A": return Key.A;
            case "S": return Key.S;
            case "D": return Key.D;
            case "UP": return Key.UpArrow;
            case "DOWN": return Key.DownArrow;
            case "LEFT": return Key.LeftArrow;
            case "RIGHT": return Key.RightArrow;
            case "SPACE": return Key.Space;
            case "LSHIFT":
            case "SHIFT": return Key.LeftShift;
            case "LCTRL":
            case "CTRL": return Key.LeftCtrl;
            case "E": return Key.E;
            case "Q": return Key.Q;
            case "F": return Key.F;
            case "G": return Key.G;
            case "R": return Key.R;
            case "TAB": return Key.Tab;
            case "ENTER": return Key.Enter;
            case "ESCAPE":
            case "ESC": return Key.Escape;
            default:
                // Try parsing as Key enum directly
                if (System.Enum.TryParse<Key>(name, true, out Key result))
                    return result;
                return Key.None;
        }
    }

    private IEnumerator SlidePanel(float targetX)
    {
        if (panel == null)
            yield break;

        Vector2 startPos = panel.anchoredPosition;
        Vector2 endPos = new Vector2(targetX, startPos.y);
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideDuration));
            panel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        panel.anchoredPosition = endPos;
    }

    private void SetPanelOffScreen()
    {
        if (panel != null)
            panel.anchoredPosition = new Vector2(offScreenX, panel.anchoredPosition.y);
    }

    public void Hide()
    {
        waitingForInput = false;
        waitingForAllInputs = false;
        allInputsComplete = false;
        activeRequiredActions = null;
        SetPanelOffScreen();
    }

}