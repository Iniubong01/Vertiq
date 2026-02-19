using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Add this component to any ScrollRect GameObject to enable gamepad scrolling.
/// Reads the left stick (vertical axis) and D-pad up/down to scroll the list.
/// Works alongside mouse drag — no conflict.
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class GamepadScrollRect : MonoBehaviour
{
    [Tooltip("How fast the list scrolls per second (0–1 normalised scroll units).")]
    [SerializeField] private float scrollSpeed = 0.5f;

    [Tooltip("Only scroll when this panel is active and focused. " +
             "Leave empty to always scroll when the GameObject is active.")]
    [SerializeField] private GameObject focusGuard;

    private ScrollRect _scrollRect;

    private void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();
    }

    private void Update()
    {
        // Optional focus guard — skip if the guard object is inactive
        if (focusGuard != null && !focusGuard.activeInHierarchy) return;

        float input = 0f;

        // --- New Input System (Gamepad) ---
        if (Gamepad.current != null)
        {
            // Left stick vertical
            float stick = Gamepad.current.leftStick.y.ReadValue();
            if (Mathf.Abs(stick) > 0.15f) // dead-zone
                input = stick;

            // D-pad up / down (override stick if pressed)
            if (Gamepad.current.dpad.up.isPressed)    input =  1f;
            if (Gamepad.current.dpad.down.isPressed)  input = -1f;
        }

        if (input == 0f) return;

        // Scroll: positive input = scroll UP (increase normalised position)
        _scrollRect.verticalNormalizedPosition =
            Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + input * scrollSpeed * Time.deltaTime);
    }
}
