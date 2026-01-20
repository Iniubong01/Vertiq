using UnityEngine;
using Reown.AppKit.Unity;
using System.Threading.Tasks;
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
    [Tooltip("Panel showing the 'Connect Wallet' button")]
    public GameObject loginPanel;
    public GameObject loginPanel2;
    
    [Tooltip("Panel showing the 'Disconnect' button and Address")]
    public GameObject connectedPanel;
    public GameObject connectedPanel2;

    public TMP_Text addressText;
    public TMP_Text addressText2;
    
    public NotificationPopup notificationPopup;

    private string reownProjectId = "c7c0756cbca65514565202ed30f68613";
    private const string JUPITER_ID = "0ef262ca2a56b88d179c93a21383fee4e135bd7bc6680e5c2356ff8e38301037";
    private const string PREFS_KEY = "DevWalletMnemonic";
    
    // [NEW] Key to remember if the user explicitly disconnected
    private const string AUTO_CONNECT_KEY = "WalletAutoConnect";

    private bool appKitReady = false;

    private async void Start()
    {
        Debug.Log($"[Wallet] Start(). RPC: {Web3.Instance.customRpc}");
        
        // Ensure UI starts in disconnected state
        UpdateDisconnectedUI();

#if UNITY_EDITOR
        Debug.Log("[Wallet] EDITOR: Checking auto-login...");
        // Check intent for Editor too
        if (PlayerPrefs.GetInt(AUTO_CONNECT_KEY, 1) == 1)
        {
            LoginOrCreateEditorWallet(true);
        }
#elif UNITY_WEBGL
        Debug.Log("[Wallet] WEBGL: Ready.");
#else
        Debug.Log("[Wallet] MOBILE/ANDROID: Scheduling delayed AppKit initialization...");

        // Keep the 3000ms delay that works for your Android build
        await Task.Delay(3000);

        try
        {
            await InitializeAppKit();
            
            if (AppKit.IsInitialized)
            {
                appKitReady = true;
                Debug.Log("[Wallet] ✓ AppKit initialized successfully");
            }
            else
            {
                Debug.LogError("[Wallet] InitializeAsync completed but IsInitialized = false");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Wallet] AppKit init failed: {ex.Message}");
        }

        if (appKitReady)
        {
            SubscribeToAppKitEvents();

            // [FIXED LOGIC] 
            // Only try to resume session if the user DID NOT disconnect last time.
            int shouldAutoConnect = PlayerPrefs.GetInt(AUTO_CONNECT_KEY, 1); // Default to 1 (True)

            if (shouldAutoConnect == 1)
            {
                Debug.Log("[Wallet] Auto-connect allowed. Attempting resume...");
                await TryResumeSessionOnStart();
            }
            else
            {
                Debug.Log("[Wallet] User explicitly disconnected last time. Skipping auto-resume.");
                // Ensure the session is definitely cleared so the modal opens fresh next time
                if (AppKit.Account != null) await AppKit.DisconnectAsync();
            }
        }
        else
        {
            notificationPopup?.Show("Error", "Wallet Init Failed. Restart App.", Color.red);
        }
#endif
    }

    private async Task InitializeAppKit()
    {
        var metadata = new Metadata("Vortiq", "Blockchain Arcade", "https://github.com/Iniubong01/Vertiq", "https://cyan-elderly-lobster-29.mypinata.cloud/ipfs/bafkreige3cxf2jejnfcvybqbezqumri5laeaic5uty47cr3f7j63brww2a");
        var config = new AppKitConfig
        {
            projectId = reownProjectId.Trim(),
            metadata = metadata,
            supportedChains = new[] { ChainConstants.Chains.Solana },
            includedWalletIds = new[] { JUPITER_ID }
        };
        await AppKit.InitializeAsync(config);
    }

    private void SubscribeToAppKitEvents()
    {
        if (!AppKit.IsInitialized) return;
        AppKit.AccountConnected -= OnAppKitAccountConnected;
        AppKit.AccountDisconnected -= OnAppKitAccountDisconnected;
        AppKit.AccountConnected += OnAppKitAccountConnected;
        AppKit.AccountDisconnected += OnAppKitAccountDisconnected;
    }

    private async Task TryResumeSessionOnStart()
    {
        await Task.Delay(500);
        try
        {
            bool resumed = await AppKit.ConnectorController.TryResumeSessionAsync();
            if (resumed && AppKit.Account != null)
            {
                Debug.Log($"[Wallet] ✓ Session resumed: {AppKit.Account.Address}");
                HandleAccountConnectedImmediate(AppKit.Account.Address, true);
            }
        }
        catch (Exception ex) { Debug.LogWarning($"[Wallet] Resume failed: {ex.Message}"); }
    }

    private void OnAppKitAccountConnected(object sender, Reown.AppKit.Unity.Connector.AccountConnectedEventArgs e)
    {
        if (e?.Account != null) HandleAccountConnectedImmediate(e.Account.Address, false);
    }

    private void OnAppKitAccountDisconnected(object sender, EventArgs e)
    {
        Debug.Log("[Wallet] AppKit AccountDisconnected");
        
        // If disconnected externally (via wallet app), we treat it as a manual disconnect
        PlayerPrefs.SetInt(AUTO_CONNECT_KEY, 0);
        PlayerPrefs.Save();
        
        PlayerAccount = null;
        UserPublicKey = null;
        UpdateDisconnectedUI();
    }

    private void HandleAccountConnectedImmediate(string address, bool suppressFeedback)
    {
        if (string.IsNullOrEmpty(address)) return;

        try
        {
            UserPublicKey = new PublicKey(address);
            PlayerAccount = null;

            // [IMPORTANT] Mark user as "Connected" so auto-resume works next time
            PlayerPrefs.SetInt(AUTO_CONNECT_KEY, 1);
            PlayerPrefs.Save();

            UpdateConnectedUI(address);

            if (!suppressFeedback) notificationPopup?.Show("Success!", "Wallet Connected", Color.green);
        }
        catch (Exception ex) { Debug.LogError($"[Wallet] Connection logic error: {ex.Message}"); }
    }

    public async void ConnectWallet()
    {
#if UNITY_EDITOR
        LoginOrCreateEditorWallet(false);
#elif UNITY_WEBGL
        await LoginWebGLWallet();
#else
        if (!appKitReady) return;

        SubscribeToAppKitEvents();

        try
        {
            // [REVERTED] I removed the explicit DisconnectAsync here.
            // This restores the original behavior that worked on Android.
            Debug.Log("[Wallet] Opening AppKit modal...");
            AppKit.OpenModal(); 
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Wallet] Failed to open modal: {ex.Message}");
        }
#endif
    }

    // [NEW] Disconnect Button Function
    public async void DisconnectWallet()
    {
        Debug.Log("[Wallet] Disconnecting...");

        // 1. Set Flag: User does NOT want auto-connect next time
        PlayerPrefs.SetInt(AUTO_CONNECT_KEY, 0);
        PlayerPrefs.Save();

        // 2. Clear Local Data
        PlayerAccount = null;
        UserPublicKey = null;

#if !UNITY_EDITOR && !UNITY_WEBGL
        // 3. Clear AppKit Session
        if (AppKit.IsInitialized)
        {
            try { await AppKit.DisconnectAsync(); } catch { }
        }
#endif
        // 4. Update UI
        UpdateDisconnectedUI();
        notificationPopup?.Show("Disconnected", "Wallet disconnected.", Color.white);
    }

    private async Task LoginWebGLWallet()
    {
        try
        {
            Account account = await Web3.Instance.LoginWalletAdapter();
            if (account != null) OnLoginSuccess(account, false);
        }
        catch { }
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
        PlayerAccount = account;
        UserPublicKey = account.PublicKey;
        
        // Mark as connected for Editor logic too
        PlayerPrefs.SetInt(AUTO_CONNECT_KEY, 1);
        PlayerPrefs.Save();

        UpdateConnectedUI(account.PublicKey.ToString());
        if (!suppressFeedback) notificationPopup?.Show("Success!", "Wallet Connected", Color.green);
    }

    // --- UI HELPERS ---

    private void UpdateConnectedUI(string publicKey)
    {
        string shortAddr = publicKey.Length >= 8 ? $"{publicKey.Substring(0, 4)}...{publicKey.Substring(publicKey.Length - 4)}" : publicKey;

        if (addressText != null) addressText.text = shortAddr;
        if (addressText2 != null) addressText2.text = shortAddr;

        // Hide Login Panels
        if (loginPanel != null) loginPanel.SetActive(false);
        if (loginPanel2 != null) loginPanel2.SetActive(false);

        // Show Connected Panels (with Disconnect button)
        if (connectedPanel != null) connectedPanel.SetActive(true);
        if (connectedPanel2 != null) connectedPanel2.SetActive(true);
    }

    private void UpdateDisconnectedUI()
    {
        if (addressText != null) addressText.text = "";
        if (addressText2 != null) addressText2.text = "";

        // Show Login Panels
        if (loginPanel != null) loginPanel.SetActive(true);
        if (loginPanel2 != null) loginPanel2.SetActive(true);

        // Hide Connected Panels
        if (connectedPanel != null) connectedPanel.SetActive(false);
        if (connectedPanel2 != null) connectedPanel2.SetActive(false);
    }

    public void CopyAddressToClipboard()
    {
        if (UserPublicKey != null) GUIUtility.systemCopyBuffer = UserPublicKey.ToString();
    }
}