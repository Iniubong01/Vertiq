using UnityEngine;
using UnityEngine.InputSystem;

public class MenuInputHandler : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private InputActionReference startButtonAction; // Start button (typically Gamepad Start)
    [SerializeField] private InputActionReference selectButtonAction; // Select button (typically Gamepad Select/Back)

    [Header("Menu Actions")]
    [SerializeField] private GameObject startGameButton; // Reference to your "Start Game" button
    [SerializeField] private GameObject storeButton;

    private UIManager uiManager;


    
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
        Debug.Log("[MenuInput] Start button pressed - Starting game");
        StartGame();
    }

    private void OnSelectButtonPressed(InputAction.CallbackContext context)
    {
        Debug.Log("[MenuInput] Select button pressed - Opening store");
        OpenStore();
    }

    // Call your actual start game method here
    public void StartGame()
    {
        // Option 1: Simulate button click if you have a button reference
        if (startGameButton != null)
        {
            var button = startGameButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
                return;
            }
        }

        // Option 2: Direct call to UIManager or your game starter
        // Replace this with your actual start game method
        // UIManager.StartGame(); 
        // or
        // GameManager.Instance.StartNewGame();
        
        Debug.LogWarning("[MenuInput] No start game method assigned. Set up your start game logic here.");
    }

    public void OpenStore()
    {
        if (storeButton != null)
        {
            var button = storeButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
                return;
            }
        }
        // Replace this with your actual store opening method
        // uiManager.MenuToStore();
        // or
        // StoreManager.Instance.ShowStore();
        
        Debug.LogWarning("[MenuInput] No store open method assigned. Set up your store logic here.");
    }
}