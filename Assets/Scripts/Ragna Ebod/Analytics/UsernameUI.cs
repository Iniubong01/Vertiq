using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UsernameUI : MonoBehaviour
{
    [Header("Input References")]
    public TMP_InputField usernameInput;
    public Button saveButton;
    public TMP_Text statusText;

    [Header("Display References")]
    public TMP_Text usernameDisplay; // Drag the Text object that should show "Ragna" here

    private void Start()
    {
        // 1. Load existing name
        string currentName = PlayerPrefs.GetString("PlayerUsername", "");
        
        // 2. Set Input Field value
        if (usernameInput != null)
            usernameInput.text = currentName;

        // 3. Update the Display Text immediately
        UpdateUsernameText(currentName);

        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveClicked);
    }

    private async void OnSaveClicked()
    {
        string newName = usernameInput.text;
        
        if (newName.Length < 3 || newName.Length > 20)
        {
            if (statusText != null) statusText.text = "Name must be 3-20 characters";
            return;
        }

        if (DualLeaderboardManager.Instance != null)
        {
            if (saveButton != null) saveButton.interactable = false;
            if (statusText != null) statusText.text = "Saving...";
            
            // Save to Unity Cloud & Local Prefs
            await DualLeaderboardManager.Instance.SetUsername(newName);
            
            // Update UI
            if (statusText != null) statusText.text = "Saved!";
            if (saveButton != null) saveButton.interactable = true;

            // Update the Display Text immediately
            UpdateUsernameText(newName);
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