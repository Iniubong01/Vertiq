using UnityEngine;
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
    public GameObject loginPanel;
    public GameObject loginPanel2;
    public GameObject loginPanel3;
    public GameObject connectedPanel;
    public GameObject connectedPanel2;
    public GameObject connectedPanel3;

    public TMP_Text addressText;
    public TMP_Text addressText2;
    public NotificationPopup notificationPopup;

    private const string PREFS_KEY = "DevWalletMnemonic";
    private const string AUTO_CONNECT_KEY = "WalletAutoConnect";

    private void Start()
    {
        Debug.Log($"[Wallet] Start. RPC: {Web3.Instance.customRpc}");

        int shouldAutoConnect = PlayerPrefs.GetInt(AUTO_CONNECT_KEY, 1);

        if (shouldAutoConnect == 1)
        {
            ToggleUIState(false, false); 
            
            // Pass true to suppress notification on auto-reconnect
            ConnectWallet(suppressNotification: true);
        }
        else
        {
            UpdateDisconnectedUI();
        }
    }

    // Add optional parameter to suppress notification
    public async void ConnectWallet(bool suppressNotification = false)
    {
        Debug.Log("[Wallet] Connecting...");

#if UNITY_EDITOR
        LoginOrCreateEditorWallet(suppressNotification);
#else
        try 
        {
            Account account = await Web3.Instance.LoginWalletAdapter();
            
            if (account != null)
            {
                OnLoginSuccess(account, suppressNotification);
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
            if (!suppressNotification)
                notificationPopup?.Show("Error", "Connection failed.", Color.red);
        }
#endif
    }

    public void DisconnectWallet()
    {
        Debug.Log("[Wallet] Disconnecting...");
        
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
            Debug.Log("[Wallet] Created New Sticky Editor Wallet");
        }
        else
        {
            Debug.Log("[Wallet] Loaded Sticky Editor Wallet");
        }

        Wallet wallet = new Wallet(savedMnemonic);
        OnLoginSuccess(wallet.Account, suppressNotification);
    }

    private void OnLoginSuccess(Account account, bool suppressNotification)
    {
        PlayerAccount = account;
        UserPublicKey = account.PublicKey;
        
        PlayerPrefs.SetInt(AUTO_CONNECT_KEY, 1);
        PlayerPrefs.Save();

        UpdateConnectedUI(account.PublicKey.ToString());
        
        if (!suppressNotification) 
            notificationPopup?.Show("Success!", "Wallet Connected", Color.green);

        // [FIX] IMMEDIATELY LOGIN TO UNITY LEADERBOARDS
        if (DualLeaderboardManager.Instance != null)
        {
            DualLeaderboardManager.Instance.LoginToUnity(account.PublicKey.ToString());
        }
        else
        {
            Debug.LogWarning("DualLeaderboardManager not found! Unity Login skipped.");
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