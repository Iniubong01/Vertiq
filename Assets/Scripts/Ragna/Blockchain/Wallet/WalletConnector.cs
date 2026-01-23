using UnityEngine;
using System.Threading.Tasks;
using TMPro;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using System;

// No Reown namespace needed anymore
// The Native SDK handles the Jupiter Wallet connection on Android automatically

public class WalletConnector : MonoBehaviour
{
    public static Account PlayerAccount;
    public static PublicKey UserPublicKey;

    [Header("UI References")]
    public GameObject loginPanel;
    public GameObject loginPanel2;
    public GameObject connectedPanel;
    public GameObject connectedPanel2;

    public TMP_Text addressText;
    public TMP_Text addressText2;
    public NotificationPopup notificationPopup;

    // This key keeps your Editor wallet "Sticky" (Saved on disk)
    private const string PREFS_KEY = "DevWalletMnemonic";
    // This key remembers if the user was last connected
    private const string AUTO_CONNECT_KEY = "WalletAutoConnect";

    private void Start()
    {
        Debug.Log($"[Wallet] Start. RPC: {Web3.Instance.customRpc}");

        // 1. Check if user wants to be Auto-Connected (Sticky Session)
        int shouldAutoConnect = PlayerPrefs.GetInt(AUTO_CONNECT_KEY, 1);

        if (shouldAutoConnect == 1)
        {
            // Hide Login UI immediately to prevent flash
            ToggleUIState(false, false); 
            
            // Trigger connection
            ConnectWallet();
        }
        else
        {
            // User explicitly disconnected last time
            UpdateDisconnectedUI();
        }
    }

    public async void ConnectWallet()
    {
        Debug.Log("[Wallet] Connecting...");

#if UNITY_EDITOR
        // --- EDITOR LOGIC (STICKY DEV WALLET) ---
        // This ensures you keep using the same test wallet in Unity
        LoginOrCreateEditorWallet(false);
#else
        // --- ANDROID & WEBGL LOGIC (NATIVE) ---
        try 
        {
            // This function automatically scans for installed wallets (Jupiter, Phantom, Solflare)
            // on Android and opens them via Deep Link. No "ID" required.
            Account account = await Web3.Instance.LoginWalletAdapter();
            
            if (account != null)
            {
                OnLoginSuccess(account, false);
            }
            else
            {
                Debug.LogError("[Wallet] Login cancelled or failed.");
                UpdateDisconnectedUI();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Wallet] Adapter Error: {ex.Message}");
            UpdateDisconnectedUI();
            notificationPopup?.Show("Error", "Connection failed.", Color.red);
        }
#endif
    }

    public void DisconnectWallet()
    {
        Debug.Log("[Wallet] Disconnecting...");
        
        // Save Intent: User actively Disconnected
        PlayerPrefs.SetInt(AUTO_CONNECT_KEY, 0);
        PlayerPrefs.Save();

        PlayerAccount = null;
        UserPublicKey = null;
        Web3.Instance.Logout();

        UpdateDisconnectedUI();
        notificationPopup?.Show("Disconnected", "Wallet disconnected.", Color.white);
    }

    // =================================================================
    // STICKY EDITOR WALLET GENERATOR
    // =================================================================
    private void LoginOrCreateEditorWallet(bool suppressFeedback)
    {
        // 1. Try to load saved mnemonic
        string savedMnemonic = PlayerPrefs.GetString(PREFS_KEY, "");

        // 2. If none exists, create a new one and SAVE it (Sticky)
        if (string.IsNullOrEmpty(savedMnemonic))
        {
            Mnemonic newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
            savedMnemonic = newMnemonic.ToString();
            PlayerPrefs.SetString(PREFS_KEY, savedMnemonic);
            PlayerPrefs.Save();
            Debug.Log("[Wallet] Created New Sticky Editor Wallet");
        }
        else
        {
            Debug.Log("[Wallet] Loaded Sticky Editor Wallet");
        }

        // 3. Login with this wallet
        Wallet wallet = new Wallet(savedMnemonic);
        OnLoginSuccess(wallet.Account, suppressFeedback);
    }

    private void OnLoginSuccess(Account account, bool suppressFeedback)
    {
        PlayerAccount = account;
        UserPublicKey = account.PublicKey;
        
        // Remember that we are connected
        PlayerPrefs.SetInt(AUTO_CONNECT_KEY, 1);
        PlayerPrefs.Save();

        UpdateConnectedUI(account.PublicKey.ToString());
        
        if (!suppressFeedback) 
            notificationPopup?.Show("Success!", "Wallet Connected", Color.green);
    }

    // =================================================================
    // UI HELPERS
    // =================================================================
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
        if (connectedPanel != null) connectedPanel.SetActive(showConnected);
        if (connectedPanel2 != null) connectedPanel2.SetActive(showConnected);
    }

    public void CopyAddressToClipboard()
    {
        if (UserPublicKey != null) GUIUtility.systemCopyBuffer = UserPublicKey.ToString();
    }
}