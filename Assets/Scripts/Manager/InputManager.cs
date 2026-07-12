using UnityEngine;

public static class InputManager
{
    public static PlayerInputActions Actions { get; private set; }

    static InputManager()
    {
        Actions = new PlayerInputActions();
        Actions.Enable();
    }
}