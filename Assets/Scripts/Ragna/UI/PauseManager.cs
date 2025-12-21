using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // Required for selecting buttons

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuCanvas;
    [SerializeField] private GameObject resumeButton; // The first button to highlight

    [Header("Input Settings")]
    // Drag your "Pause" action from the Inspector here
    [SerializeField] private InputActionReference pauseAction;

    private bool isPaused = false;

    private void OnEnable()
    {
        // Enable the action and listen for the press
        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePerformed;
        }
    }

    private void OnDisable()
    {
        // Clean up listeners
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            OpenMenu();
        }
        else
        {
            CloseMenu();
        }
    }

    private void OpenMenu()
    {
        pauseMenuCanvas.SetActive(true);
        Time.timeScale = 0f; // Freeze game time

        // --- CRITICAL FOR CONTROLLERS ---
        // You MUST clear the current selection and set a new one.
        // If you don't do this, the D-pad/Joystick won't know what to move to.
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(resumeButton);
    }

    public void CloseMenu()
    {
        pauseMenuCanvas.SetActive(false);
        Time.timeScale = 1f; // Resume game time
        
        // Deselect everything so you don't accidentally click buttons while playing
        EventSystem.current.SetSelectedGameObject(null);
    }
}