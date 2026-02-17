using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Solana.Unity.Wallet;
using System.Collections.Generic; // Required for Metadata dictionary

public class DualLeaderboardManager : MonoBehaviour
{
    public static DualLeaderboardManager Instance { get; private set; }

    [Header("Web2 Setup")]
    public string unityLeaderboardId = "vortiq_leaderboard";
    
    [Header("UI Feedback")]
    public NotificationPopup notificationPopup; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        // Auto-find NotificationPopup if not assigned
        if (notificationPopup == null)
        {
            notificationPopup = FindFirstObjectByType<NotificationPopup>();
            if (notificationPopup == null)
            {
                Debug.LogWarning("[DualLeaderboardManager] NotificationPopup not found in scene. User feedback will be limited.");
            }
        }
        
        try 
        { 
            await UnityServices.InitializeAsync(); 
        }
        catch (System.Exception e) 
        { 
            Debug.LogWarning($"[DualLeaderboardManager] Unity Services Init Error: {e.Message}");
            ShowNotification("Service Error", "Failed to initialize services. Some features may be unavailable.", Color.red);
        }
    }

    public async Task SetUsername(string newUsername)
    {
        if (string.IsNullOrEmpty(newUsername)) return;
        PlayerPrefs.SetString("PlayerUsername", newUsername);
        PlayerPrefs.Save();

        try
        {
            if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(newUsername);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Leaderboard] Failed to update username: {e.Message}");
            ShowNotification("Warning", "Username saved locally but could not sync to cloud.", Color.yellow);
        }
    }

    public async Task LoginToUnity(string walletAddress)
    {
        if (AuthenticationService.Instance == null)
        {
            Debug.LogError("[Leaderboard] AuthenticationService is not available");
            ShowNotification("Service Error", "Authentication service unavailable.", Color.red);
            return;
        }
        
        if (AuthenticationService.Instance.IsSignedIn) return;

        try
        {
            // Login Logic
            string profileName = walletAddress.Length > 30 ? walletAddress.Substring(0, 30) : walletAddress;
            AuthenticationService.Instance.SwitchProfile(profileName);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            // Set Name Logic
            string displayName = PlayerPrefs.GetString("PlayerUsername", "");
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = walletAddress.Substring(0, 4) + "..." + walletAddress.Substring(walletAddress.Length - 4);
            }
            await AuthenticationService.Instance.UpdatePlayerNameAsync(displayName);
        }
        catch (System.Exception e) 
        { 
            Debug.LogWarning($"[Web2] Login Failed: {e.Message}");
            ShowNotification("Login Failed", "Could not authenticate with leaderboard service.", Color.red);
        }
    }

    public async void SubmitScoreHybrid(long score)
    {
        if (WalletConnector.UserPublicKey == null) 
        {
            Debug.LogWarning("[Leaderboard] Cannot submit score - wallet not connected");
            return;
        }
        
        string walletAddress = WalletConnector.UserPublicKey.ToString();

        // Check if authentication service is available
        if (AuthenticationService.Instance == null)
        {
            Debug.LogError("[Leaderboard] AuthenticationService not available");
            ShowNotification("Service Error", "Leaderboard service unavailable.", Color.red);
            return;
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await LoginToUnity(walletAddress);
            await Task.Delay(1000);
        }

        try 
        {
            // 1. Get Avatar Index
            int avatarIndex = 0;
            if (ProfilePictureManager.Instance != null)
            {
                avatarIndex = ProfilePictureManager.Instance.CurrentAvatarIndex;
                // If Custom (-1), fallback to 0 for global leaderboard
                if (avatarIndex < 0) avatarIndex = 0; 
            }
            else
            {
                Debug.LogWarning("[Leaderboard] ProfilePictureManager not found, using default avatar");
            }

            // 2. Prepare Metadata (Wallet + Avatar)
            var metadata = new Dictionary<string, string> { 
                { "wallet", walletAddress },
                { "avatar", avatarIndex.ToString() } 
            };
            
            // 3. Submit
            var options = new AddPlayerScoreOptions { Metadata = metadata };
            var response = await LeaderboardsService.Instance.AddPlayerScoreAsync(unityLeaderboardId, score, options);
            
            Debug.Log($"[Web2] Score Uploaded! Rank: {response.Rank} | Avatar: {avatarIndex}");
            //ShowNotification("Score Saved!", $"Rank: #{response.Rank}", Color.green);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Web2] Failed to submit score: {e.Message}");
            
            // Provide specific error messages based on exception type
            string userMessage = "Could not save score to leaderboard.";
            if (e.Message.Contains("network") || e.Message.Contains("timeout"))
            {
                userMessage = "Network error. Score saved locally.";
            }
            else if (e.Message.Contains("quota") || e.Message.Contains("limit"))
            {
                userMessage = "Leaderboard limit reached. Try again later.";
            }
            
            ShowNotification("Upload Failed", userMessage, Color.red);
        }

        // Safe analytics call with null check
        try
        {
            if (AnalyticsManager.Instance != null)
                AnalyticsManager.Instance.TrackWalletLogin(walletAddress);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Leaderboard] Analytics tracking failed: {e.Message}");
        }
    }
    
    private void ShowNotification(string title, string message, Color color)
    {
        try
        {
            if (notificationPopup != null)
            {
                notificationPopup.Show(title, message, color);
            }
            else
            {
                // Fallback if notification popup not available
                Debug.Log($"[Notification] {title}: {message}");
            }
        }
        catch (System.Exception e)
        {
            // Prevent notification system from crashing the game
            Debug.LogWarning($"[DualLeaderboardManager] Failed to show notification: {e.Message}");
        }
    }
}