using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance;

    async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeAnalytics();
    }

    private async Task InitializeAnalytics()
    {
        try
        {
            // Check if services are already initialized
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }
            
            // Start Collection (Required for UGS)
            AnalyticsService.Instance.StartDataCollection();

            Debug.Log("✅ Analytics Initialized! Tracking started.");
            TrackEvent("app_started");
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Analytics Init Failed: " + e.Message);
        }
    }

    /// <summary>
    /// Logs the login event with the Wallet Address.
    /// </summary>
    public void TrackWalletLogin(string walletAddress)
    {
        Debug.Log($"🔗 Tracking Login for Wallet: {walletAddress}");

        // Track the login event with the address as data
        TrackEvent("wallet_login", new Dictionary<string, object> {
            { "wallet_address", walletAddress }
        });
    }

    /// <summary>
    /// Sends a custom event to the Unity Dashboard.
    /// </summary>
    public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        // Only run if Unity Services are ready
        if (UnityServices.State == ServicesInitializationState.Initialized)
        {
            try 
            {
                if (parameters == null)
                {
                    // Send event with no parameters
                    AnalyticsService.Instance.RecordEvent(eventName);
                }
                else
                {
                    // The UGS Analytics SDK used here doesn't expose a RecordEvent overload that
                    // accepts a Dictionary<string, object>, so serialize the parameters into a
                    // compact string and append it to the event name.
                    var payloadSb = new System.Text.StringBuilder();
                    foreach (var kv in parameters)
                    {
                        if (kv.Value == null) continue;
                        payloadSb.Append(kv.Key).Append('=').Append(kv.Value.ToString()).Append(';');
                    }
                    var payload = payloadSb.ToString();
                    var composedEvent = string.IsNullOrEmpty(payload) ? eventName : $"{eventName}::{payload}";
                    AnalyticsService.Instance.RecordEvent(composedEvent);
                }
                
                // Force upload immediately (Great for testing, remove for Release)
                AnalyticsService.Instance.Flush(); 
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Analytics Error: {e.Message}");
            }
        }
    }
}