using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuInputHandler : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private InputActionReference startButtonAction; // Start button (typically Gamepad Start)
    [SerializeField] private InputActionReference selectButtonAction; // Select button (typically Gamepad Select/Back)

    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "Game"; // Name of your game scene

    [Header("Menu Actions")]
    [SerializeField] private GameObject startGameButton; // Reference to your "Start Game" button
    [SerializeField] private GameObject storeButton; // Reference to your "Store" button

    private void OnEnable()
    {
        // Enable Start Button (for starting the game)
        if (startButtonAction != null)
        {
            startButtonAction.action.Enable();
            startButtonAction.action.performed += OnStartButtonPressed;
        }

        // Enable Select Button (for opening store)
        if (selectButtonAction != null)
        {
            selectButtonAction.action.Enable();
            selectButtonAction.action.performed += OnSelectButtonPressed;
        }
    }

    private void OnDisable()
    {
        // Cleanup Start Button
        if (startButtonAction != null)
        {
            startButtonAction.action.performed -= OnStartButtonPressed;
            startButtonAction.action.Disable();
        }

        // Cleanup Select Button
        if (selectButtonAction != null)
        {
            selectButtonAction.action.performed -= OnSelectButtonPressed;
            selectButtonAction.action.Disable();
        }
    }

    private void OnStartButtonPressed(InputAction.CallbackContext context)
    {
        //Debug.Log("[MenuInput] Start button pressed - Starting game");
        StartGame();
    }

    private void OnSelectButtonPressed(InputAction.CallbackContext context)
    {
        //Debug.Log("[MenuInput] Select button pressed - Opening store");
        OpenStore();
    }

    /// <summary>
    /// Starts the game by loading the game scene
    /// </summary>
    public void StartGame()
    {
        // Try to invoke the button if it exists (for consistency with UI)
        if (startGameButton != null)
        {
            var button = startGameButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
                return;
            }
        }

        // Fallback: Load the game scene directly
        //Debug.Log($"[MenuInput] Loading game scene: {gameSceneName}");
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonSound();
        }
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Opens the store menu
    /// </summary>
    public void OpenStore()
    {
        // Try to invoke the store button
        if (storeButton != null)
        {
            var button = storeButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
                return;
            }
        }

        // Fallback: Log warning
        //Debug.LogWarning("[MenuInput] Store button not assigned or not interactable!");
    }
}