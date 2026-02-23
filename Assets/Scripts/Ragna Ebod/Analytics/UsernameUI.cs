using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UsernameUI : MonoBehaviour
{
    public static UsernameUI Instance { get; private set; }

    [SerializeField] private TMPro.TMP_Text debugPanel; // assign a TMP text in Inspector

    [Header("Input References")]
    public TMP_InputField usernameInput;
    public Button saveButton;
    public TMP_Text statusText;

    // The scene-side text is registered via UsernameDisplayBridge — not Inspector-linked
    private TMP_Text _usernameDisplay;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // This is already DontDestroyOnLoad via ProfileManager parent
    }

    private void Start()
    {
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveClicked);

        RefreshDisplay();
    }

    // Called by UsernameDisplayBridge when the scene text object enables
    public void RegisterDisplay(TMP_Text display)
    {
        _usernameDisplay = display;
        RefreshDisplay(); // Immediately update with the saved name
    }

    // Called by UsernameDisplayBridge when the scene text object disables
    public void UnregisterDisplay(TMP_Text display)
    {
        if (_usernameDisplay == display)
            _usernameDisplay = null;
    }

    private void RefreshDisplay()
    {
        string currentName = PlayerPrefs.GetString("PlayerUsername", "");

        if (usernameInput != null)
            usernameInput.text = currentName;

        UpdateUsernameText(currentName);
    }

    private async void OnSaveClicked()
{
    Debug.Log("[UsernameUI] OnSaveClicked — START");
    try
    {
        Debug.Log("[UsernameUI] STEP 1 — reading usernameInput");
        string newName = usernameInput != null ? usernameInput.text : "";
        Debug.Log($"[UsernameUI] STEP 1 — newName='{newName}'");

        if (newName.Length < 3 || newName.Length > 20)
        {
            if (statusText != null) statusText.text = "Name must be 3-20 characters";
            return;
        }

        Debug.Log($"[UsernameUI] STEP 3 — DualLeaderboardManager is {(DualLeaderboardManager.Instance != null ? "READY" : "NULL")}");

        if (DualLeaderboardManager.Instance != null)
        {
            if (saveButton != null) saveButton.interactable = false;
            if (statusText != null) statusText.text = "Saving...";

            Debug.Log("[UsernameUI] STEP 5 — awaiting SetUsername...");
            await DualLeaderboardManager.Instance.SetUsername(newName);
            Debug.Log("[UsernameUI] STEP 5 — SetUsername returned");

            if (this == null || !gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[UsernameUI] Object destroyed after await");
                return;
            }

            if (statusText != null) statusText.text = "Saved!";
            if (saveButton != null) saveButton.interactable = true;
            UpdateUsernameText(newName);
        }
        else
        {
            Debug.Log("[UsernameUI] STEP 3B — saving locally");
            PlayerPrefs.SetString("PlayerUsername", newName);
            PlayerPrefs.Save();
            if (statusText != null) statusText.text = "Saved!";
            UpdateUsernameText(newName);
            Debug.LogWarning("[UsernameUI] DualLeaderboardManager not ready — saved locally only.");
        }

        Debug.Log("[UsernameUI] OnSaveClicked — COMPLETE");
    }
    catch (System.Exception ex)
    {
        string msg = $"CRASH: {ex.GetType().Name}\n{ex.Message}";
        Debug.LogError(msg);
        if (debugPanel != null) debugPanel.text = msg;  // shows on screen
        if (statusText != null) statusText.text = "Error. See screen.";
        if (saveButton != null) saveButton.interactable = true;
    }
}

    private void UpdateUsernameText(string username)
    {
        if (_usernameDisplay == null) return;

        if (string.IsNullOrEmpty(username))
        {
            _usernameDisplay.text = "Guest";
        }
        else
        {
            if (username.Contains("#"))
                username = username.Split('#')[0];

            _usernameDisplay.text = username;
        }
    }
}