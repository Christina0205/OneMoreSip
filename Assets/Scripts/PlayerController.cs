using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads player input and routes it to the SipMechanic.
/// Supports both the new Input System (tap/click) and a keyboard fallback.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField] private SipMechanic sipMechanic;

    private void Update()
    {
        if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing)
            return;

        // Keyboard fallback
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            TrySip();

        // Touch / mouse click
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            TrySip();
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TrySip();
    }

    private void TrySip()
    {
        sipMechanic?.TakeSip();
    }
}
