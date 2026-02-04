using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Solana.Unity.Wallet;

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
        try 
        {
            await UnityServices.InitializeAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Unity Services Init Error: {e.Message}");
        }
    }

    // [NEW] Call this as soon as the Wallet connects!
    public async void LoginToUnity(string walletAddress)
    {
        if (AuthenticationService.Instance.IsSignedIn) return;

        try
        {
            // Truncate Wallet Address to 30 chars for Profile Name
            string profileName = walletAddress.Length > 30 ? walletAddress.Substring(0, 30) : walletAddress;
            
            Debug.Log($"[Web2] Logging in with Profile: {profileName}");
            
            // Switch to the specific user profile for this wallet
            AuthenticationService.Instance.SwitchProfile(profileName);
            
            // Sign in
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            // Update Name
            string shortName = walletAddress.Substring(0, 4) + "..." + walletAddress.Substring(walletAddress.Length - 4);
            await AuthenticationService.Instance.UpdatePlayerNameAsync(shortName);

            Debug.Log("[Web2] Login Successful!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Web2] Login Failed: {e.Message}");
        }
    }

    public async void SubmitScoreHybrid(long score)
    {
        if (WalletConnector.UserPublicKey == null) return;
        string walletAddress = WalletConnector.UserPublicKey.ToString();

        // Ensure we are logged in before submitting
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            LoginToUnity(walletAddress);
            await Task.Delay(1000); // Small wait for login to finish
        }

        // --- PATH A: WEB2 ---
        try 
        {
            var response = await LeaderboardsService.Instance.AddPlayerScoreAsync(unityLeaderboardId, score);
            Debug.Log($"[Web2] Score Uploaded! Rank: {response.Rank}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Web2] Failed: {e.Message}");
        }

        // Analytics Tracking
        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.TrackWalletLogin(walletAddress);
        }
    }
}