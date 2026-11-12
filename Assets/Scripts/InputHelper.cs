using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Entrada compatible con el Input System nuevo (activo en este proyecto).
/// </summary>
public static class InputHelper
{
    public static bool Press()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.spaceKey.wasPressedThisFrame ||
                keyboard.upArrowKey.wasPressedThisFrame ||
                keyboard.wKey.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame)
                return true;
        }

        var pointer = Pointer.current;
        if (pointer != null && pointer.press.wasPressedThisFrame) return true;

        var pad = Gamepad.current;
        if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame))
            return true;

        return false;
#else
        return Input.GetKeyDown(KeyCode.Space) ||
               Input.GetKeyDown(KeyCode.UpArrow) ||
               Input.GetKeyDown(KeyCode.W) ||
               Input.GetKeyDown(KeyCode.Return) ||
               Input.GetMouseButtonDown(0);
#endif
    }

    public static bool PausePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif
    }

    public static bool MenuPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.mKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
