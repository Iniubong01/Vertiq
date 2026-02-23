using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuCanvas;
    [SerializeField] private GameObject resumeButton; 
    [SerializeField] private GameObject powerUpsButton;
    [SerializeField] private GameObject pauseButton; 

    [Header("Input Settings")]
    [SerializeField] private InputActionReference pauseAction; // Start button in game scene

    private bool isPaused = false;
    private float sceneLoadTime;
    private float lastPauseToggleTime;
    private const float PAUSE_COOLDOWN = 0.3f; // Prevent rapid toggling

    private void Start()
    {
        // Ensure game starts in unpaused state
        Time.timeScale = 1f;
        isPaused = false;
        sceneLoadTime = Time.unscaledTime;
        lastPauseToggleTime = -999f; // Allow immediate first pause
        
        // Ensure pause menu is hidden
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
        
        Debug.Log("[PauseManager] Game scene initialized - Time.timeScale = 1.0");
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePerformed;
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        // Ignore input for 0.5 seconds after scene load to prevent accidental pause
        if (Time.unscaledTime - sceneLoadTime < 0.5f)
        {
            Debug.Log("[PauseManager] Ignoring pause - scene just loaded");
            return;
        }
        
        // Cooldown to prevent rapid toggling
        if (Time.unscaledTime - lastPauseToggleTime < PAUSE_COOLDOWN)
        {
            Debug.Log($"[PauseManager] Ignoring pause - cooldown active ({Time.unscaledTime - lastPauseToggleTime:F2}s since last toggle)");
            return;
        }
        
        // Don't pause if a power-up was just activated
        if (PowerUpManager.Instance != null && PowerUpManager.Instance.WasPowerUpJustActivated())
        {
            Debug.Log("[PauseManager] Ignoring pause - power-up just activated");
            return;
        }
        
        TogglePause();
    }

    public void TogglePause()
    {
        // Additional check: Don't pause if power-up just activated
        if (PowerUpManager.Instance != null && PowerUpManager.Instance.WasPowerUpJustActivated())
        {
            Debug.Log("[PauseManager] Ignoring pause toggle - power-up just activated");
            return;
        }
        
        // Update cooldown timer
        lastPauseToggleTime = Time.unscaledTime;
        
        isPaused = !isPaused;
        
        Debug.Log($"[PauseManager] Toggling pause - New state: {(isPaused ? "PAUSED" : "UNPAUSED")}");

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
        
        if (powerUpsButton != null)
        {
            powerUpsButton.SetActive(false);
        }
        
        if (pauseButton != null)
        {
            pauseButton.SetActive(false);
        }
        
        Time.timeScale = 0f; 

        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.SetPausedState(true);
        }

        // Select the resume button for gamepad navigation
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            
            if (resumeButton != null)
            {
                EventSystem.current.SetSelectedGameObject(resumeButton);
            }
        }
        
        Debug.Log("[PauseManager] Pause menu opened - Time.timeScale = 0");
    }

    public void CloseMenu()
    {
        pauseMenuCanvas.SetActive(false);
        
        if (powerUpsButton != null)
        {
            powerUpsButton.SetActive(true);
        }
        
        if (pauseButton != null)
        {
            pauseButton.SetActive(true);
        }
        
        Time.timeScale = 1f; 
        
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.SetPausedState(false);
        }

        EventSystem.current.SetSelectedGameObject(null);
        isPaused = false;
        
        Debug.Log("[PauseManager] Pause menu closed - Time.timeScale = 1");
    }

    public void LoadHomeScene()
    {
        // Reset time scale before leaving
        Time.timeScale = 1f;
        isPaused = false;
        UIManager.LoadMainMenu();
    }
}