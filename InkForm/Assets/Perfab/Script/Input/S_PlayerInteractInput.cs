using UnityEngine.InputSystem;

public static class S_PlayerInteractInput
{
    public static bool WasPressedThisFrame()
    {
        if (!S_InputBindingManager.TryGetExisting(out S_InputBindingManager inputManager))
            return false;

        if (inputManager.IsRebinding)
            return false;

        InputAction interactAction = inputManager.Actions.Player.Interact;
        return interactAction != null
            && interactAction.enabled
            && interactAction.WasPressedThisFrame();
    }
}
