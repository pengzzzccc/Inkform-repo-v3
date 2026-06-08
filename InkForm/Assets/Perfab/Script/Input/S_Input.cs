using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Static input service. Owns the single InputSystem_Actions instance and the gameplay
/// input-lock stack (disables the Player action map while any lock is held). Replaces the
/// old S_InputBindingManager; there is no runtime rebinding — all bindings live in the
/// InputSystem_Actions asset.
/// </summary>
public static class S_Input
{
    private const string LegacyGameplayInputLockId = "LegacyGameplayInputRequest";

    private static InputSystem_Actions actions;
    private static readonly HashSet<string> gameplayInputLocks = new HashSet<string>();
    private static bool eventsHooked;

    /// <summary>The shared InputSystem actions. Lazily created and enabled on first use.</summary>
    public static InputSystem_Actions Actions
    {
        get
        {
            EnsureInitialized();
            return actions;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        UnhookEvents();

        if (actions != null)
        {
            actions.Disable();
            actions.Dispose();
            actions = null;
        }

        gameplayInputLocks.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInitialized();
    }

    private static void EnsureInitialized()
    {
        EnsureActions();
        HookEvents();
        ApplyGameplayInputLockState();
    }

    private static void EnsureActions()
    {
        if (actions != null)
            return;

        actions = new InputSystem_Actions();
        actions.Enable();
    }

    private static void HookEvents()
    {
        if (eventsHooked)
            return;

        S_GameEvent.OnGameplayInputEnabledRequested += SetGameplayInputEnabled;
        S_GameEvent.OnGameplayInputLockPushed += PushGameplayInputLock;
        S_GameEvent.OnGameplayInputLockPopped += PopGameplayInputLock;
        eventsHooked = true;
    }

    private static void UnhookEvents()
    {
        if (!eventsHooked)
            return;

        S_GameEvent.OnGameplayInputEnabledRequested -= SetGameplayInputEnabled;
        S_GameEvent.OnGameplayInputLockPushed -= PushGameplayInputLock;
        S_GameEvent.OnGameplayInputLockPopped -= PopGameplayInputLock;
        eventsHooked = false;
    }

    public static void SetGameplayInputEnabled(bool enabled)
    {
        if (enabled)
            PopGameplayInputLock(LegacyGameplayInputLockId);
        else
            PushGameplayInputLock(LegacyGameplayInputLockId);
    }

    public static void PushGameplayInputLock(string lockId)
    {
        EnsureActions();
        gameplayInputLocks.Add(Normalize(lockId));
        ApplyGameplayInputLockState();
    }

    public static void PopGameplayInputLock(string lockId)
    {
        EnsureActions();
        gameplayInputLocks.Remove(Normalize(lockId));
        ApplyGameplayInputLockState();
    }

    private static void ApplyGameplayInputLockState()
    {
        if (actions == null)
            return;

        if (gameplayInputLocks.Count == 0)
            actions.Player.Enable();
        else
            actions.Player.Disable();
    }

    private static string Normalize(string lockId)
    {
        return string.IsNullOrWhiteSpace(lockId) ? "AnonymousGameplayInputLock" : lockId;
    }

    // ── Binding display (read-only; used by Hook to show the mapped key) ───────────────

    public static string GetBindingDisplayString(string actionName, string bindingGroup, string partName = null, string devicePath = null)
    {
        InputAction action = Actions.asset.FindAction(actionName);
        if (action == null) return "-";

        int bindingIndex = FindBindingIndex(action, bindingGroup, partName, devicePath);
        if (bindingIndex < 0) return "-";

        string display = action.GetBindingDisplayString(bindingIndex);
        return string.IsNullOrWhiteSpace(display) ? action.bindings[bindingIndex].effectivePath : display;
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
