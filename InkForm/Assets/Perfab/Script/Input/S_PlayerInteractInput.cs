using UnityEngine.InputSystem;

public static class S_PlayerInteractInput
{
    public static bool WasPressedThisFrame()
    {
        InputAction interactAction = S_Input.Actions.Player.Interact;
        return interactAction != null
            && interactAction.enabled
            && interactAction.WasPressedThisFrame();
    }
}
