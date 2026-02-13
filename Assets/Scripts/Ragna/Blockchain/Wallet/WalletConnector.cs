using UnityEngine;
using System.Threading.Tasks;
using TMPro;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using System;
using Unity.Services.Authentication;

public class WalletConnector : MonoBehaviour
{
    public static WalletConnector Instance; 
    public static Account PlayerAccount;
    public static PublicKey UserPublicKey;

    [Header("UI References")]
    public GameObject loginPanel;
    public GameObject loginPanel2;
    public GameObject loginPanel3;
    public GameObject connectedPanel;
    public GameObject connectedPanel2;
    public GameObject connectedPanel3;

    public TMP_Text addressText;
    public TMP_Text addressText2;
    public NotificationPopup notificationPopup;

    [Header("Username Handling")]
    public UsernameSetupPanel usernameSetupPanel;
    
    [Header("Username Panel Behavior")]
    [Tooltip("CHECKED: Panel opens every time. UNCHECKED: Panel only opens for users without a username.")]
    public bool alwaysShowUsernamePanel = false;
    
    [Tooltip("If enabled, the checkbox state will be saved to PlayerPrefs and persist across sessions")]
    public bool persistCheckboxState = true;

    private const string PREFS_KEY = "DevWalletMnemonic";
    private const string AUTO_CONNECT_KEY = "WalletAutoConnect";
    private const string USERNAME_PANEL_ALWAYS_SHOW_KEY = "AlwaysShowUsernamePanel";

    private bool _pendingUsernamePrompt = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
        // Load persisted checkbox state if enabled
        if (persistCheckboxState && PlayerPrefs.HasKey(USERNAME_PANEL_ALWAYS_SHOW_KEY))
        {
            alwaysShowUsernamePanel = PlayerPrefs.GetInt(USERNAME_PANEL_ALWAYS_SHOW_KEY, 0) == 1;
            Debug.Log($"[Wallet] Loaded checkbox state from PlayerPrefs: {alwaysShowUsernamePanel}");
        }
    }

    private void Start()
    {
        Debug.Log($"[Wallet] Start. RPC: {Web3.Instance.customRpc}");
        Debug.Log($"[Wallet] alwaysShowUsernamePanel = {alwaysShowUsernamePanel}");

        int shouldAutoConnect = PlayerPrefs.GetInt(AUTO_CONNECT_KEY, 1);

        if (shouldAutoConnect == 1)
        {
            ToggleUIState(false, false); 
            ConnectWallet(suppressNotification: true);
        }
        else
        {
            UpdateDisconnectedUI();
        }
    }

    // Called by UIManager when the Menu finishes loading
    public void AttemptShowUsernamePanel()
    {
        if (!_pendingUsernamePrompt) return;
        
        // Safety: If UIManager isn't ready, stop.
        if (!UIManager.IsMenuInteractable) 
        {
            return;
        }

        Debug.Log("[Wallet] Menu Ready. Showing Username Panel.");
        if (usernameSetupPanel != null) usernameSetupPanel.Show();
        
        _pendingUsernamePrompt = false;
    }

    /// <summary>
    /// Call this method to change the checkbox state at runtime (e.g., from a settings menu)
    /// </summary>
    public void SetAlwaysShowUsernamePanel(bool value)
    {
        alwaysShowUsernamePanel = value;
        
        if (persistCheckboxState)
        {
            PlayerPrefs.SetInt(USERNAME_PANEL_ALWAYS_SHOW_KEY, value ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[Wallet] Saved checkbox state to PlayerPrefs: {value}");
        }
    }

    public async void ConnectWallet(bool suppressNotification = false)
    {
#if UNITY_EDITOR
        LoginOrCreateEditorWallet(suppressNotification);
#else
        try 
        {
            Account account = await Web3.Instance.LoginWalletAdapter();
            if (account != null) OnLoginSuccess(account, suppressNotification);
            else UpdateDisconnectedUI();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Wallet] Adapter Error: {ex.Message}");
            UpdateDisconnectedUI();
            if (!suppressNotification) notificationPopup?.Show("Error", "Connection failed.", Color.red);
        }
#endif
    }

    public void DisconnectWallet()
    {
        PlayerPrefs.SetInt(AUTO_CONNECT_KEY, 0);
        PlayerPrefs.Save();
        PlayerAccount = null;
        UserPublicKey = null;
        Web3.Instance.Logout();
        UpdateDisconnectedUI();
        notificationPopup?.Show("Disconnected", "Wallet disconnected.", Color.white);
    }

    private void LoginOrCreateEditorWallet(bool suppressNotification)
    {
        string savedMnemonic = PlayerPrefs.GetString(PREFS_KEY, "");
        if (string.IsNullOrEmpty(savedMnemonic))
        {
            Mnemonic newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
            savedMnemonic = newMnemonic.ToString();
            PlayerPrefs.SetString(PREFS_KEY, savedMnemonic);
            PlayerPrefs.Save();
        }
        Wallet wallet = new Wallet(savedMnemonic);
        OnLoginSuccess(wallet.Account, suppressNotification);
    }

    private async void OnLoginSuccess(Account account, bool suppressNotification)
    {
        PlayerAccount = account;
        UserPublicKey = account.PublicKey;
        
        PlayerPrefs.SetInt(AUTO_CONNECT_KEY, 1);
        PlayerPrefs.Save();

        UpdateConnectedUI(account.PublicKey.ToString());
        
        if (!suppressNotification) 
            notificationPopup?.Show("Success!", "Wallet Connected", Color.green);

        if (DualLeaderboardManager.Instance != null)
        {
            // Wrap Login in try-catch so an error here doesn't kill the Username check logic
            try 
            {
                await DualLeaderboardManager.Instance.LoginToUnity(account.PublicKey.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Wallet] Unity Login Error: {ex.Message}");
            }

            // Check username logic immediately after login attempt
            CheckAndPromptUsername();
        }
    }

    private void CheckAndPromptUsername()
    {
        Debug.Log($"[Wallet] CheckAndPromptUsername() - alwaysShowUsernamePanel: {alwaysShowUsernamePanel}");
        
        // ===== CHECKBOX LOGIC =====
        // CHECKED: Always show panel
        if (alwaysShowUsernamePanel)
        {
            Debug.Log("[Wallet] Opening Panel: Checkbox is CHECKED (always show).");
            _pendingUsernamePrompt = true;
            AttemptShowUsernamePanel();
            return;
        }

        // UNCHECKED: Only show if no username exists
        // First check local PlayerPrefs
        string cachedUsername = PlayerPrefs.GetString("PlayerUsername", "");
        
        if (!string.IsNullOrEmpty(cachedUsername))
        {
            Debug.Log($"[Wallet] Panel Skipped: Username exists in PlayerPrefs: '{cachedUsername}'");
            return;
        }

        // If no local username, check Unity Authentication
        if (AuthenticationService.Instance.IsSignedIn)
        {
            string currentName = AuthenticationService.Instance.PlayerName;
            
            // Invalid if empty OR contains '#' (default names are like Player#1234)
            bool isInvalidName = string.IsNullOrEmpty(currentName) || currentName.Contains("#");

            if (isInvalidName)
            {
                Debug.Log($"[Wallet] Opening Panel: No valid username found (Unity name: '{currentName}').");
                _pendingUsernamePrompt = true;
                AttemptShowUsernamePanel();
            }
            else
            {
                Debug.Log($"[Wallet] Panel Skipped: Valid Unity username exists: '{currentName}'");
            }
        }
        else
        {
            Debug.LogWarning("[Wallet] Panel Skipped: User NOT Signed into Unity Services.");
        }
    }

    private void UpdateConnectedUI(string publicKey)
    {
        string shortAddr = publicKey.Length >= 8 ? $"{publicKey.Substring(0, 4)}...{publicKey.Substring(publicKey.Length - 4)}" : publicKey;
        if (addressText != null) addressText.text = shortAddr;
        if (addressText2 != null) addressText2.text = shortAddr;
        ToggleUIState(false, true);
    }

    private void UpdateDisconnectedUI()
    {
        if (addressText != null) addressText.text = "";
        if (addressText2 != null) addressText2.text = "";
        ToggleUIState(true, false);
    }

    private void ToggleUIState(bool showLogin, bool showConnected)
    {
        if (loginPanel != null) loginPanel.SetActive(showLogin);
        if (loginPanel2 != null) loginPanel2.SetActive(showLogin);
        if (loginPanel3 != null) loginPanel3.SetActive(showLogin);
        if (connectedPanel != null) connectedPanel.SetActive(showConnected);
        if (connectedPanel2 != null) connectedPanel2.SetActive(showConnected);
        if (connectedPanel3 != null) connectedPanel3.SetActive(showConnected);
    }

    public void CopyAddressToClipboard()
    {
        if (UserPublicKey != null) GUIUtility.systemCopyBuffer = UserPublicKey.ToString();
    }
}