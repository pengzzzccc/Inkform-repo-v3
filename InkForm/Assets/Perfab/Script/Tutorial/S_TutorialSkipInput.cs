using UnityEngine;
using UnityEngine.InputSystem;

public static class S_TutorialSkipInput
{
    private static int lastConsumedFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        lastConsumedFrame = -1;
    }

    public static bool WasSkipPressedThisFrame()
    {
        if (Time.frameCount == lastConsumedFrame)
            return false;

        if (!IsSkipPressedThisFrame())
            return false;

        lastConsumedFrame = Time.frameCount;
        return true;
    }

    private static bool IsSkipPressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null
            && (keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame))
        {
            return true;
        }

        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
    }
}
