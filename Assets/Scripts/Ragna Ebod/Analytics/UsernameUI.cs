using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UsernameUI : MonoBehaviour
{
    [Header("Input References")]
    public TMP_InputField usernameInput;
    public Button saveButton;
    public TMP_Text statusText;

    [Header("Display References")]
    public TMP_Text usernameDisplay; // Drag the Text object that should show "Ragna" here

    private bool _started = false;

    private void Start()
    {
        _started = true;

        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveClicked);

        RefreshDisplay();
    }

    // Fires every time this object/scene is re-enabled (e.g. returning from game scene).
    // Skip on the very first enable since Start() handles it directly.
    private void OnEnable()
    {
        if (!_started) return; // Start() hasn't run yet — it will call RefreshDisplay itself
        StartCoroutine(RefreshNextFrame());
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null; // wait one frame for scene objects to settle
        RefreshDisplay();
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
        try
        {
            string newName = usernameInput != null ? usernameInput.text : "";

            if (newName.Length < 3 || newName.Length > 20)
            {
                if (statusText != null) statusText.text = "Name must be 3-20 characters";
                return;
            }

            if (DualLeaderboardManager.Instance != null)
            {
                if (saveButton != null) saveButton.interactable = false;
                if (statusText != null) statusText.text = "Saving...";

                await DualLeaderboardManager.Instance.SetUsername(newName);

                // Guard: object may have been destroyed while awaiting
                if (this == null || !gameObject.activeInHierarchy) return;

                if (statusText != null) statusText.text = "Saved!";
                if (saveButton != null) saveButton.interactable = true;

                UpdateUsernameText(newName);
            }
            else
            {
                // Fallback: save locally if manager isn't ready yet
                PlayerPrefs.SetString("PlayerUsername", newName);
                PlayerPrefs.Save();
                if (statusText != null) statusText.text = "Saved!";
                UpdateUsernameText(newName);
                Debug.LogWarning("[UsernameUI] DualLeaderboardManager not ready — saved locally only.");
            }
        }
        catch (System.Exception ex)
        {
            // Catch-all: prevents async void from crashing the app
            Debug.LogError($"[UsernameUI] Unexpected error in OnSaveClicked: {ex.Message}");
            if (statusText != null) statusText.text = "Error saving. Try again.";
            if (saveButton != null) saveButton.interactable = true;
        }
    }

    private void UpdateUsernameText(string username)
    {
        if (usernameDisplay == null) return;

        if (string.IsNullOrEmpty(username))
        {
            usernameDisplay.text = "Guest"; // Fallback if no name saved
        }
        else
        {
            // Strip the #1234 tag if it exists, so it just shows "Ragna"
            if (username.Contains("#")) 
            {
                username = username.Split('#')[0];
            }
            
            // [FIX] Shows ONLY the username
            usernameDisplay.text = username;
        }
    }
}