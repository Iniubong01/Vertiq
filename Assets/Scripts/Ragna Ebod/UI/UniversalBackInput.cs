using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// A global listener that detects the "Cancel" input (e.g., Gamepad East Button / B button or South Button / A button depending on layout) 
/// and automatically invokes the onClick event of the active BackButtonIdentifier.
/// Place this script on a persistent manager object in your scene (e.g., GameManager or Canvas).
/// </summary>
public class UniversalBackInput : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("Reference to the Cancel action in your Input Map (e.g., UI/Cancel)")]
    [SerializeField] private InputActionReference cancelAction;
    
    [Tooltip("If true, it will also explicitly check for the Gamepad A button (South Button) just in case the Cancel mapping is not what you want.")]
    [SerializeField] private bool explicitlyCheckGamepadAButton = true;

    private void OnEnable()
    {
        if (cancelAction != null)
        {
            cancelAction.action.Enable();
            cancelAction.action.performed += OnCancelPerformed;
        }
    }

    private void OnDisable()
    {
        if (cancelAction != null)
        {
            cancelAction.action.performed -= OnCancelPerformed;
            cancelAction.action.Disable();
        }
    }

    private void Update()
    {
        // Explicit fallback for Gamepad A button (buttonSouth) if the user specifically wanted 'A'
        if (explicitlyCheckGamepadAButton && Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            TryExecuteBackAction();
        }
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        TryExecuteBackAction();
    }

    private void TryExecuteBackAction()
    {
        // Find all BackButtonIdentifiers currently active in the scene hierarchy
        BackButtonIdentifier[] activeBackButtons = FindObjectsByType<BackButtonIdentifier>(FindObjectsSortMode.None);
        
        if (activeBackButtons.Length == 0) return;

        // In a complex UI, there might be multiple active canvases. 
        // A simple approach is to find the last one in the hierarchy or the one whose canvas is on top.
        // For simple setups, usually only one panel is active at a time.
        
        // We will try finding the "deepest" or just grab the first one if there's only one.
        // If there are multiple, you might need a sorting logic based on Canvas sorting order or hierarchy index.
        // For mostly additive menus, we'll invoke the first one we find that is actually active and interactable in hierarchy.

        foreach (var backBtn in activeBackButtons)
        {
            if (backBtn != null && backBtn.gameObject.activeInHierarchy && backBtn.Button.interactable)
            {
                //Debug.Log("[UniversalBackInput] Found back button, simulating click: " + backBtn.gameObject.name);
                backBtn.Button.onClick.Invoke();
                
                // If you want sound, usually the button itself handles it, 
                // but you could add a SoundManager call here if needed.
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayButtonSound();
                }

                return; // Stop after clicking the first valid back button
            }
        }
    }
}
