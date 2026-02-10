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

    [Header("Web3 Setup")]
    public SoarLeaderboardManager soarManager; 

    [Header("Web2 Setup")]
    public string unityLeaderboardId = "vortiq_leaderboard"; 

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
        try { await UnityServices.InitializeAsync(); }
        catch (System.Exception e) { Debug.LogError($"Unity Services Init Error: {e.Message}"); }
    }

    public async Task SetUsername(string newUsername)
    {
        if (string.IsNullOrEmpty(newUsername)) return;
        PlayerPrefs.SetString("PlayerUsername", newUsername);
        PlayerPrefs.Save();

        if (AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newUsername);
        }
    }

    public async void LoginToUnity(string walletAddress)
    {
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
        catch (System.Exception e) { Debug.LogError($"[Web2] Login Failed: {e.Message}"); }
    }

    public async void SubmitScoreHybrid(long score)
    {
        if (WalletConnector.UserPublicKey == null) return;
        string walletAddress = WalletConnector.UserPublicKey.ToString();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            LoginToUnity(walletAddress);
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

            // 2. Prepare Metadata (Wallet + Avatar)
            var metadata = new Dictionary<string, string> { 
                { "wallet", walletAddress },
                { "avatar", avatarIndex.ToString() } 
            };
            
            // 3. Submit
            var options = new AddPlayerScoreOptions { Metadata = metadata };
            var response = await LeaderboardsService.Instance.AddPlayerScoreAsync(unityLeaderboardId, score, options);
            
            Debug.Log($"[Web2] Score Uploaded! Rank: {response.Rank} | Avatar: {avatarIndex}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Web2] Failed: {e.Message}");
        }

        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.TrackWalletLogin(walletAddress);
    }
}