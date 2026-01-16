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
    public GameObject loginPanel, connectedPanel, loginPanel2, connectedPanel2;
    public TMP_Text addressText, addressText2;
    public NotificationPopup notificationPopup;

    private string reownProjectId = "c7c0756cbca65514565202ed30f68613";
    private const string JUPITER_ID = "0ef262ca2a56b88d179c93a21383fee4e135bd7bc6680e5c2356ff8e38301037";
    private const string PREFS_KEY = "DevWalletMnemonic";

    private bool appKitReady = false;

    private async void Start()
    {
        Debug.Log($"[Wallet] Start(). RPC: {Web3.Instance.customRpc}");
        UpdateDisconnectedUI();

#if UNITY_EDITOR
        Debug.Log("[Wallet] EDITOR: Using local wallet.");
        LoginOrCreateEditorWallet(true);
#elif UNITY_WEBGL
        Debug.Log("[Wallet] WEBGL: Ready.");
#else
        Debug.Log("[Wallet] MOBILE/ANDROID: Scheduling delayed AppKit initialization...");

        // Longer delay for native Android plugin to fully initialize
        await Task.Delay(3000);

        try
        {
            await InitializeAppKit();
            if (AppKit.IsInitialized)
            {
                appKitReady = true;
                Debug.Log("[Wallet] ✓ AppKit initialized successfully (single attempt)");
            }
            else
            {
                Debug.LogError("[Wallet] InitializeAsync completed but IsInitialized = false");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Wallet] AppKit initialization failed: {ex.GetType().Name} - {ex.Message}\nStack: {ex.StackTrace}");
        }

        if (appKitReady)
        {
            SubscribeToAppKitEvents();
            await TryResumeSessionOnStart();
        }
        else
        {
            Debug.LogError("[Wallet] AppKit failed initialization - wallet functionality limited.");
            notificationPopup?.Show("Critical Error", "Wallet system failed to initialize.\nRestart the app.", Color.red);
        }
#endif
    }

    private async Task InitializeAppKit()
    {
        var metadata = new Metadata(
            "Vortiq",
            "Blockchain Arcade",
            "https://vertiq.game",
            "https://icon.png"
        );

        var config = new AppKitConfig
        {
            projectId = reownProjectId.Trim(),
            metadata = metadata,
            supportedChains = new[] { ChainConstants.Chains.Solana },
            includedWalletIds = new[] { JUPITER_ID }
        };

        await AppKit.InitializeAsync(config);
        Debug.Log("[Wallet] InitializeAsync call completed");
    }

    private void SubscribeToAppKitEvents()
    {
        if (!AppKit.IsInitialized) return;

        AppKit.AccountConnected -= OnAppKitAccountConnected;
        AppKit.AccountDisconnected -= OnAppKitAccountDisconnected;

        AppKit.AccountConnected += OnAppKitAccountConnected;
        AppKit.AccountDisconnected += OnAppKitAccountDisconnected;

        Debug.Log("[Wallet] ✓ AppKit events subscribed");
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
            else
            {
                Debug.Log("[Wallet] No previous session to resume");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Wallet] Resume session failed: {ex.Message}");
        }
    }

    private void OnAppKitAccountConnected(object sender, Reown.AppKit.Unity.Connector.AccountConnectedEventArgs e)
    {
        Debug.Log($"[Wallet] 🎉 AppKit AccountConnected event! Sender: {sender?.GetType().Name ?? "null"}");
        if (e?.Account != null)
        {
            HandleAccountConnectedImmediate(e.Account.Address, false);
        }
    }

    private void OnAppKitAccountDisconnected(object sender, EventArgs e)
    {
        Debug.Log("[Wallet] AppKit AccountDisconnected");
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

            UpdateConnectedUI(address);

            if (!suppressFeedback)
            {
                notificationPopup?.Show("Success!", "Wallet Connected", Color.green);
            }

            Debug.Log($"[Wallet] ✅ Successfully connected: {address}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Wallet] Failed processing connection: {ex.Message}");
        }
    }

    public async void ConnectWallet()
    {
#if UNITY_EDITOR
        LoginOrCreateEditorWallet(false);
#elif UNITY_WEBGL
        await LoginWebGLWallet();
#else
        Debug.Log("[Wallet] ConnectWallet called");

        if (!appKitReady)
        {
            notificationPopup?.Show("Error", "Wallet system not ready. Restart app.", Color.red);
            return;
        }

        SubscribeToAppKitEvents();

        try
        {
            Debug.Log("[Wallet] Opening AppKit modal...");
            AppKit.OpenModal();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Wallet] Failed to open modal: {ex.Message}");
            notificationPopup?.Show("Error", "Failed to open wallet.", Color.red);
        }
#endif
    }

    private async Task LoginWebGLWallet()
    {
        try
        {
            Account account = await Web3.Instance.LoginWalletAdapter();
            if (account != null)
            {
                UserPublicKey = account.PublicKey;
                PlayerAccount = null;
                UpdateConnectedUI(account.PublicKey.ToString());
                notificationPopup?.Show("Success!", "Wallet Connected", Color.green);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Wallet] WebGL Login Error: {ex.Message}");
        }
    }

    private void LoginOrCreateEditorWallet(bool suppressFeedback)
    {
        string saved = PlayerPrefs.GetString(PREFS_KEY, "");
        Mnemonic mnemonic = string.IsNullOrEmpty(saved)
            ? new Mnemonic(WordList.English, WordCount.Twelve)
            : new Mnemonic(saved);

        if (string.IsNullOrEmpty(saved))
        {
            PlayerPrefs.SetString(PREFS_KEY, mnemonic.ToString());
            PlayerPrefs.Save();
        }

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
        string shortAddr = publicKey.Length >= 8 ? $"{publicKey.Substring(0, 4)}...{publicKey.Substring(publicKey.Length - 4)}" : publicKey;

        if (addressText != null) addressText.text = shortAddr;
        if (addressText2 != null) addressText2.text = shortAddr;

        if (loginPanel != null) loginPanel.SetActive(false);
        if (connectedPanel != null) connectedPanel.SetActive(true);
        if (loginPanel2 != null) loginPanel2.SetActive(false);
        if (connectedPanel2 != null) connectedPanel2.SetActive(true);
    }

    private void UpdateDisconnectedUI()
    {
        if (addressText != null) addressText.text = "";
        if (addressText2 != null) addressText2.text = "";

        if (loginPanel != null) loginPanel.SetActive(true);
        if (connectedPanel != null) connectedPanel.SetActive(false);
        if (loginPanel2 != null) loginPanel2.SetActive(true);
        if (connectedPanel2 != null) connectedPanel2.SetActive(false);
    }

    public async void DisconnectWallet()
    {
#if !UNITY_EDITOR && !UNITY_WEBGL
        if (AppKit.IsInitialized)
        {
            try
            {
                await AppKit.DisconnectAsync();
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
        }
    }
}