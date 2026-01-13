using UnityEngine;
using Reown.AppKit.Unity;
using System.Threading.Tasks;
using System.Collections;
using TMPro;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using System;

public class WalletConnector : MonoBehaviour
{
    public static Account PlayerAccount;
    public static PublicKey UserPublicKey;

    [Header("UI References")]
    public GameObject loginPanel, connectedPanel, loginPanel2, connectedPanel2;
    public TMP_Text addressText, addressText2;
    public NotificationPopup notificationPopup;

    private string reownProjectId = "c7c0756cbca65514565202ed30f68613";
    private const string JUPITER_ID = "0ef262ca2a56b88d179c93a21383fee4e135bd7bc6680e5c2356ff8e38301037";
    private const string PREFS_KEY = "DevWalletMnemonic";

    private async void Start()
    {
        // Debug Log to confirm which RPC is actually being used
        Debug.Log($"[Wallet] Start(). Web3 RPC Configured to: {Web3.Instance.customRpc}");
        UpdateDisconnectedUI();

#if UNITY_EDITOR
        Debug.Log("[Wallet] EDITOR: Using local wallet.");
        LoginOrCreateEditorWallet(suppressFeedback: true); 
#elif UNITY_WEBGL
        Debug.Log("[Wallet] WEBGL: Ready.");
#else
        Debug.Log("[Wallet] MOBILE/ANDROID: Initializing AppKit...");
        if (!PlayerPrefs.HasKey("SessionReset_v12")) {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetInt("SessionReset_v12", 1);
            PlayerPrefs.Save();
        }
        
        // 🎯 FIX: Subscribe to events BEFORE initialization
        // This ensures events are captured even if initialization is delayed
        if (!AppKit.IsInitialized)
        {
            try
            {
                await InitializeAppKit();
                Debug.Log("[Wallet] AppKit initialization completed in Start()");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Wallet] AppKit initialization failed in Start(): {ex.Message}");
                // Don't throw - we'll retry on ConnectWallet()
            }
        }
        
        // Always subscribe to events (safe to call multiple times)
        SubscribeToAppKitEvents();
        
        await TryResumeSessionOnStart();
#endif
    }
    
    // 🎯 NEW: Separate method to subscribe to events
    private void SubscribeToAppKitEvents()
    {
        try
        {
            // Unsubscribe first to prevent duplicate subscriptions
            AppKit.AccountConnected -= OnAppKitAccountConnected;
            AppKit.AccountDisconnected -= OnAppKitAccountDisconnected;
            
            // Subscribe
            AppKit.AccountConnected += OnAppKitAccountConnected;
            AppKit.AccountDisconnected += OnAppKitAccountDisconnected;
            
            Debug.Log("[Wallet] AppKit events subscribed successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Wallet] Failed to subscribe to AppKit events: {ex.Message}");
        }
    }

    private async Task TryResumeSessionOnStart()
    {
        await Task.Delay(1000);
        try {
            bool resumed = await AppKit.ConnectorController.TryResumeSessionAsync();
            if (resumed && AppKit.Account != null) 
            {
                Debug.Log($"[Wallet] Session resumed: {AppKit.Account.Address}");
                HandleAccountConnectedImmediate(AppKit.Account.Address, true);
            }
            else
            {
                Debug.Log("[Wallet] No previous session to resume.");
            }
        } 
        catch (Exception ex) 
        { 
            Debug.LogWarning($"[Wallet] Resume session failed: {ex.Message}"); 
        }
    }

    private void OnAppKitAccountConnected(object sender, Reown.AppKit.Unity.Connector.AccountConnectedEventArgs e)
    {
        Debug.Log($"[Wallet] 🎉 AppKit AccountConnected event TRIGGERED!");
        Debug.Log($"[Wallet] Sender: {sender?.GetType().Name ?? "null"}");
        Debug.Log($"[Wallet] EventArgs: {e?.GetType().Name ?? "null"}");
        //Debug.Log($"[Wallet] Account Address: {e?.Account?.Address ?? "NULL"}");
        
        if (e?.Account != null) 
        {
            // Use immediate method instead of coroutine for more reliable UI updates
            HandleAccountConnectedImmediate(e.Account.Address, false);
        }
        else
        {
            Debug.LogError("[Wallet] AccountConnected event fired but Account is NULL!");
        }
    }

    private void OnAppKitAccountDisconnected(object sender, EventArgs e)
    {
        Debug.Log("[Wallet] AppKit AccountDisconnected event fired");
        PlayerAccount = null;
        UserPublicKey = null;
        UpdateDisconnectedUI();
    }

    // 🎯 NEW: Immediate handler without coroutine
    private void HandleAccountConnectedImmediate(string address, bool suppressFeedback)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[Wallet] Received null/empty address!");
            if (!suppressFeedback) notificationPopup?.Show("Error", "Invalid Address", Color.red);
            return;
        }

        try 
        {
            Debug.Log($"[Wallet] Processing connection for address: {address}");
            
            UserPublicKey = new PublicKey(address);
            PlayerAccount = null; // External wallet, no local account
            
            UpdateConnectedUI(address);
            
            if (!suppressFeedback) 
            {
                notificationPopup?.Show("Success!", "Wallet Connected", Color.green);
            }
            
            Debug.Log($"[Wallet] ✅ Successfully connected: {address}");
        } 
        catch (Exception ex) 
        { 
            Debug.LogError($"[Wallet] Failed to process address: {ex.Message}");
            if (!suppressFeedback) notificationPopup?.Show("Error", "Invalid Address", Color.red);
        }
    }

    // Keep old coroutine version for backward compatibility if needed elsewhere
    private IEnumerator HandleAccountConnected(string address, bool suppressFeedback)
    {
        yield return new WaitForEndOfFrame(); // Wait for frame to complete
        HandleAccountConnectedImmediate(address, suppressFeedback);
    }

    public async void ConnectWallet()
    {
#if UNITY_EDITOR
        LoginOrCreateEditorWallet(false);
#elif UNITY_WEBGL
        await LoginWebGLWallet();
#else
        Debug.Log("[Wallet] ConnectWallet called");
        
        // Ensure AppKit is initialized
        if (!AppKit.IsInitialized) 
        {
            Debug.Log("[Wallet] AppKit not initialized, initializing now...");
            try
            {
                await InitializeAppKit();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Wallet] Failed to initialize AppKit: {ex.Message}");
                notificationPopup?.Show("Error", "Failed to initialize wallet", Color.red);
                return;
            }
        }
        
        // Ensure events are subscribed (safe to call multiple times)
        SubscribeToAppKitEvents();
        
        Debug.Log("[Wallet] Opening AppKit modal...");
        try
        {
            AppKit.OpenModal();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Wallet] Failed to open modal: {ex.Message}");
            notificationPopup?.Show("Error", "Failed to open wallet", Color.red);
        }
#endif
    }

    private async Task InitializeAppKit()
    {
        var metadata = new Metadata("Vortiq", "Blockchain Arcade", "https://vertiq.game", "https://icon.png");
        var config = new AppKitConfig {
            projectId = reownProjectId.Trim(), 
            metadata = metadata,
            supportedChains = new[] { ChainConstants.Chains.Solana },
            includedWalletIds = new[] { JUPITER_ID }
        };
        await AppKit.InitializeAsync(config);
        Debug.Log("[Wallet] AppKit initialized successfully.");
    }

    private async Task LoginWebGLWallet()
    {
        try {
            Account account = await Web3.Instance.LoginWalletAdapter();
            if (account != null) OnLoginSuccess(account, false);
        } catch (Exception ex) { Debug.LogError($"[Wallet] WebGL Login Error: {ex.Message}"); }
    }

    private void LoginOrCreateEditorWallet(bool suppressFeedback)
    {
        string saved = PlayerPrefs.GetString(PREFS_KEY, "");
        Mnemonic mnemonic = string.IsNullOrEmpty(saved) ? new Mnemonic(WordList.English, WordCount.Twelve) : new Mnemonic(saved);
        if (string.IsNullOrEmpty(saved)) { PlayerPrefs.SetString(PREFS_KEY, mnemonic.ToString()); PlayerPrefs.Save(); }
        OnLoginSuccess(new Wallet(mnemonic).Account, suppressFeedback);
    }

    private void OnLoginSuccess(Account account, bool suppressFeedback)
    {
        if (account == null) return;
        PlayerAccount = account;
        UserPublicKey = account.PublicKey; 
        UpdateConnectedUI(account.PublicKey.ToString());
        if (!suppressFeedback) notificationPopup?.Show("Success!", "Wallet Connected", Color.green);
    }

    private void UpdateConnectedUI(string publicKey)
    {
        Debug.Log($"[Wallet] UpdateConnectedUI called with: {publicKey}");
        
        string shortAddr = publicKey.Length >= 8 
            ? $"{publicKey.Substring(0, 4)}...{publicKey.Substring(publicKey.Length - 4)}" 
            : publicKey;
        
        if (addressText != null) 
        {
            addressText.text = shortAddr;
            Debug.Log($"[Wallet] Set addressText to: {shortAddr}");
        }
        
        if (addressText2 != null) 
        {
            addressText2.text = shortAddr;
            Debug.Log($"[Wallet] Set addressText2 to: {shortAddr}");
        }
        
        // 🎯 CRITICAL: Null checks and logging for each UI element
        if(loginPanel != null) 
        {
            loginPanel.SetActive(false);
            Debug.Log("[Wallet] loginPanel disabled");
        }
        else Debug.LogWarning("[Wallet] loginPanel is NULL!");
        
        if(connectedPanel != null) 
        {
            connectedPanel.SetActive(true);
            Debug.Log("[Wallet] connectedPanel enabled");
        }
        else Debug.LogWarning("[Wallet] connectedPanel is NULL!");
        
        if(loginPanel2 != null) 
        {
            loginPanel2.SetActive(false);
            Debug.Log("[Wallet] loginPanel2 disabled");
        }
        else Debug.LogWarning("[Wallet] loginPanel2 is NULL!");
        
        if(connectedPanel2 != null) 
        {
            connectedPanel2.SetActive(true);
            Debug.Log("[Wallet] connectedPanel2 enabled");
        }
        else Debug.LogWarning("[Wallet] connectedPanel2 is NULL!");
    }

    private void UpdateDisconnectedUI()
    {
        Debug.Log("[Wallet] UpdateDisconnectedUI called");
        
        if (addressText != null) addressText.text = "";
        if (addressText2 != null) addressText2.text = "";
        
        if(loginPanel != null) loginPanel.SetActive(true);
        if(connectedPanel != null) connectedPanel.SetActive(false);
        if(loginPanel2 != null) loginPanel2.SetActive(true);
        if(connectedPanel2 != null) connectedPanel2.SetActive(false);
    }

    public async void DisconnectWallet()
    {
#if !UNITY_EDITOR && !UNITY_WEBGL
        Debug.Log("[Wallet] Disconnecting wallet...");
        if (AppKit.IsInitialized) 
        {
            try 
            {
                await AppKit.DisconnectAsync();
                Debug.Log("[Wallet] AppKit disconnected successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Wallet] Disconnect error: {ex.Message}");
            }
        }
#endif
        PlayerAccount = null; 
        UserPublicKey = null;
        UpdateDisconnectedUI();
    }
    
    public void CopyAddressToClipboard() 
    { 
        if (UserPublicKey != null) 
        {
            GUIUtility.systemCopyBuffer = UserPublicKey.ToString();
            Debug.Log($"[Wallet] Copied address to clipboard: {UserPublicKey}");
        }
    }
}