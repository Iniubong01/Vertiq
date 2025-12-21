using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AnalyticsManager : MonoBehaviour
{
    // Singleton instance to access this from anywhere (e.g., WalletConnector)
    public static AnalyticsManager Instance;

    async void Awake()
    {
        // Singleton Pattern: Ensure only one AnalyticsManager exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep this object alive when changing scenes

        await InitializeAnalytics();
    }

    private async Task InitializeAnalytics()
    {
        try
        {
            // 1. Initialize connection to Unity Cloud
            await UnityServices.InitializeAsync();
            
            // 2. Start Data Collection 
            // Note: In a real release, you should show a GDPR/CCPA consent popup first.
            // For now, we auto-accept to get data flowing.
            AnalyticsService.Instance.StartDataCollection();

            Debug.Log("✅ Analytics Initialized! Tracking started.");
            
            // Optional: Track that the game app was opened
            TrackEvent("app_started");
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Analytics Init Failed: " + e.Message);
        }
    }

    /// <summary>
    /// Links the current player session to a specific Solana Wallet Address.
    /// Call this immediately after the user logs in.
    /// </summary>
    public void SetUserWallet(string walletAddress)
    {
        if (UnityServices.State == ServicesInitializationState.Initialized)
        {
            try
            {
                // This replaces the anonymous ID with the Wallet Address in your dashboard
                UnityServices.ExternalUserId = walletAddress;
                Debug.Log($"🔗 Analytics Linked to Wallet: {walletAddress}");
                
                // Track a specific login event so you know they are authenticated
                TrackEvent("wallet_login", new Dictionary<string, object> {
                    { "wallet_address", walletAddress }
                });
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to set external user ID: " + e.Message);
            }
        }
    }

    /// <summary>
    /// Helper to send custom events to the dashboard.
    /// Example: AnalyticsManager.Instance.TrackEvent("level_complete");
    /// </summary>
    public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (UnityServices.State == ServicesInitializationState.Initialized)
        {
            if (parameters == null)
            {
                AnalyticsService.Instance.RecordEvent(eventName);
            }
            else
            {
                var customEvent = new Dictionary<string, object> { { "eventName", eventName } };
                foreach (var param in parameters)
                {
                    customEvent[param.Key] = param.Value;
                }
                AnalyticsService.Instance.RecordEvent(eventName);
            }
            
            // Force upload immediately (useful during development/testing)
            AnalyticsService.Instance.Flush(); 
        }
    }
}