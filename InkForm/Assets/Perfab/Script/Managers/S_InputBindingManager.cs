using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class S_InputBindingManager : MonoBehaviour
{
    public static S_InputBindingManager Instance
    {
        get
        {
            if (instance == null && !TryGetExisting(out _))
            {
                Debug.LogError("[InputBindingManager] Missing S_InputBindingManager. Add the full ManagerRoot prefab to the scene.");
            }

            return instance;
        }
    }

    public static bool HasInstance => TryGetExisting(out _);

    public static bool TryGetExisting(out S_InputBindingManager manager)
    {
        manager = instance;
        if (manager != null)
            return true;

        manager = FindAnyObjectByType<S_InputBindingManager>();
        if (manager == null)
            return false;

        instance = manager;
        return true;
    }

    private const string BindingPrefsKey = "InkForm.InputBindingOverrides";
    private const string LegacyGameplayInputLockId = "LegacyGameplayInputRequest";
    private static S_InputBindingManager instance;

    private InputSystem_Actions actions;
    private readonly HashSet<string> gameplayInputLocks = new HashSet<string>();
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
    private InputAction rebindingAction;
    private bool rebindingActionWasEnabled;

    public event Action BindingsChanged;

    public InputSystem_Actions Actions
    {
        get
        {
            InitializeActions();
            return actions;
        }
    }

    public bool IsRebinding => rebindingOperation != null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            S_ManagerRoot.DestroyDuplicate(this);
            return;
        }

        instance = this;
        InitializeActions();
    }

    private void OnEnable()
    {
        actions?.Enable();
        S_GameEvent.OnGameplayInputEnabledRequested += SetGameplayInputEnabled;
        S_GameEvent.OnGameplayInputLockPushed += PushGameplayInputLock;
        S_GameEvent.OnGameplayInputLockPopped += PopGameplayInputLock;
        ApplyGameplayInputLockState();
    }

    private void OnDisable()
    {
        S_GameEvent.OnGameplayInputEnabledRequested -= SetGameplayInputEnabled;
        S_GameEvent.OnGameplayInputLockPushed -= PushGameplayInputLock;
        S_GameEvent.OnGameplayInputLockPopped -= PopGameplayInputLock;
        CancelRebind();
        actions?.Disable();
    }

    private void OnDestroy()
    {
        if (instance != this) return;

        CancelRebind();
        actions?.Dispose();
        actions = null;
        instance = null;
    }

    public InputAction FindAction(string actionName, bool throwIfNotFound = false)
    {
        return Actions.asset.FindAction(actionName, throwIfNotFound);
    }

    public int FindBindingIndex(string actionName, string bindingGroup, string partName = null, string devicePath = null)
    {
        InputAction action = FindAction(actionName);
        return action == null ? -1 : FindBindingIndex(action, bindingGroup, partName, devicePath);
    }

    public string GetBindingDisplayString(string actionName, string bindingGroup, string partName = null, string devicePath = null)
    {
        InputAction action = FindAction(actionName);
        if (action == null) return "-";

        int bindingIndex = FindBindingIndex(action, bindingGroup, partName, devicePath);
        if (bindingIndex < 0) return "-";

        string display = action.GetBindingDisplayString(bindingIndex);
        return string.IsNullOrWhiteSpace(display) ? action.bindings[bindingIndex].effectivePath : display;
    }

    public bool StartInteractiveRebind(
        string actionName,
        string bindingGroup,
        string partName,
        string devicePath,
        string expectedControlType,
        Action onComplete,
        Action onCancel)
    {
        InputAction action = FindAction(actionName);
        if (action == null) return false;

        int bindingIndex = FindBindingIndex(action, bindingGroup, partName, devicePath);
        if (bindingIndex < 0) return false;

        CancelRebind();

        rebindingAction = action;
        rebindingActionWasEnabled = action.enabled;
        action.Disable();

        rebindingOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithControlsExcluding("<Mouse>/scroll")
            .WithControlsExcluding("<Pointer>/position")
            .WithControlsExcluding("<Pointer>/delta")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => FinishRebind(operation, true, onComplete))
            .OnCancel(operation => FinishRebind(operation, false, onCancel));

        if (!string.IsNullOrWhiteSpace(expectedControlType))
            rebindingOperation.WithExpectedControlType(expectedControlType);

        ApplyDeviceFilter(bindingGroup, devicePath);

        rebindingOperation.Start();
        return true;
    }

    public void CancelRebind()
    {
        if (rebindingOperation == null) return;

        InputActionRebindingExtensions.RebindingOperation operation = rebindingOperation;
        operation.Cancel();

        if (rebindingOperation == operation)
            FinishRebind(operation, false, null);
    }

    public void ResetBinding(string actionName, string bindingGroup, string partName = null, string devicePath = null)
    {
        InputAction action = FindAction(actionName);
        if (action == null) return;

        int bindingIndex = FindBindingIndex(action, bindingGroup, partName, devicePath);
        if (bindingIndex < 0) return;

        action.RemoveBindingOverride(bindingIndex);
        SaveBindingOverrides();
        BindingsChanged?.Invoke();
    }

    public void ResetAllBindings()
    {
        Actions.asset.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(BindingPrefsKey);
        PlayerPrefs.Save();
        BindingsChanged?.Invoke();
    }

    public void SetGameplayInputEnabled(bool enabled)
    {
        if (enabled)
            PopGameplayInputLock(LegacyGameplayInputLockId);
        else
            PushGameplayInputLock(LegacyGameplayInputLockId);
    }

    public void PushGameplayInputLock(string lockId)
    {
        InitializeActions();
        gameplayInputLocks.Add(NormalizeGameplayInputLockId(lockId));
        ApplyGameplayInputLockState();
    }

    public void PopGameplayInputLock(string lockId)
    {
        InitializeActions();
        gameplayInputLocks.Remove(NormalizeGameplayInputLockId(lockId));
        ApplyGameplayInputLockState();
    }

    private void ApplyGameplayInputLockState()
    {
        InitializeActions();

        if (gameplayInputLocks.Count == 0)
            actions.Player.Enable();
        else
            actions.Player.Disable();
    }

    private static string NormalizeGameplayInputLockId(string lockId)
    {
        return string.IsNullOrWhiteSpace(lockId) ? "AnonymousGameplayInputLock" : lockId;
    }

    private void InitializeActions()
    {
        if (actions != null) return;

        actions = new InputSystem_Actions();
        LoadBindingOverrides();

        if (isActiveAndEnabled)
            actions.Enable();
    }

    private void LoadBindingOverrides()
    {
        string json = PlayerPrefs.GetString(BindingPrefsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(json))
            actions.asset.LoadBindingOverridesFromJson(json);
    }

    private void SaveBindingOverrides()
    {
        string json = Actions.asset.SaveBindingOverridesAsJson();

        if (string.IsNullOrWhiteSpace(json) || json == "{}")
            PlayerPrefs.DeleteKey(BindingPrefsKey);
        else
            PlayerPrefs.SetString(BindingPrefsKey, json);

        PlayerPrefs.Save();
    }

    private void ApplyDeviceFilter(string bindingGroup, string devicePath)
    {
        if (bindingGroup == "Keyboard&Mouse")
        {
            rebindingOperation
                .WithControlsHavingToMatchPath("<Keyboard>")
                .WithControlsHavingToMatchPath("<Mouse>");
            return;
        }

        if (bindingGroup == "Gamepad" || devicePath == "<Gamepad>")
        {
            rebindingOperation.WithControlsHavingToMatchPath("<Gamepad>");
        }
    }

    private void FinishRebind(
        InputActionRebindingExtensions.RebindingOperation operation,
        bool save,
        Action callback)
    {
        if (rebindingOperation != operation) return;

        rebindingOperation = null;
        operation.Dispose();

        if (rebindingActionWasEnabled && rebindingAction != null && !rebindingAction.enabled)
            rebindingAction.Enable();

        rebindingAction = null;
        rebindingActionWasEnabled = false;

        if (save)
        {
            SaveBindingOverrides();
            BindingsChanged?.Invoke();
        }

        callback?.Invoke();
    }

    private static int FindBindingIndex(InputAction action, string bindingGroup, string partName, string devicePath)
    {
        int fallbackDeviceMatch = -1;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.isComposite) continue;

            if (!string.IsNullOrWhiteSpace(partName))
            {
                if (!binding.isPartOfComposite) continue;
                if (!string.Equals(binding.name, partName, StringComparison.OrdinalIgnoreCase)) continue;
            }
            else if (binding.isPartOfComposite)
            {
                continue;
            }

            if (MatchesBindingGroup(binding, bindingGroup))
                return i;

            if (MatchesDevicePath(binding.path, devicePath, false))
                return i;

            if (fallbackDeviceMatch < 0 && MatchesDevicePath(binding.path, devicePath, true))
                fallbackDeviceMatch = i;
        }

        return fallbackDeviceMatch;
    }

    private static bool MatchesBindingGroup(InputBinding binding, string bindingGroup)
    {
        if (string.IsNullOrWhiteSpace(bindingGroup) || string.IsNullOrWhiteSpace(binding.groups))
            return false;

        string[] groups = binding.groups.Split(';');
        foreach (string group in groups)
        {
            if (string.Equals(group, bindingGroup, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool MatchesDevicePath(string bindingPath, string devicePath, bool allowGamepadFallback)
    {
        if (string.IsNullOrWhiteSpace(bindingPath) || string.IsNullOrWhiteSpace(devicePath))
            return false;

        if (bindingPath.StartsWith(devicePath, StringComparison.OrdinalIgnoreCase))
            return true;

        return allowGamepadFallback
            && devicePath == "<Gamepad>"
            && bindingPath.IndexOf("Gamepad>", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
