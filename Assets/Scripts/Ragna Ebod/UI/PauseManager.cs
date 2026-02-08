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
        powerUpsButton.SetActive(false); 
        pauseButton.SetActive(false); 
        Time.timeScale = 0f; 

        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.SetPausedState(true);
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(resumeButton);
    }

    public void CloseMenu()
    {
        pauseMenuCanvas.SetActive(false);
        powerUpsButton.SetActive(true); 
        pauseButton.SetActive(true); 
        Time.timeScale = 1f; 
        
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.SetPausedState(false);
        }

        EventSystem.current.SetSelectedGameObject(null);
        isPaused = false;
    }

    public void LoadHomeScene()
    {
        // Reset time scale before leaving
        Time.timeScale = 1f;
        isPaused = false;
        UIManager.LoadMainMenu();
    }
}