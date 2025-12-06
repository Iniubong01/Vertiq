using UnityEngine;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using TMPro;

public class WalletConnector : MonoBehaviour
{
    public static Account PlayerAccount;

    [Header("UI References")]
    public GameObject loginPanel;
    public GameObject connectedPanel;

    public GameObject loginPanel2;
    public GameObject connectedPanel2;
    public TMP_Text addressText;
    public TMP_Text addressText2;

    private const string PREFS_KEY = "DevWalletMnemonic"; // Key to save our secret phrase

    public async void ConnectWallet()
    {
        if (Web3.Instance == null) return;

        // --- EDITOR LOGIC (The "Sticky" Fix) ---
#if UNITY_EDITOR
        // 1. Try to Login normally first
        var account = await Web3.Instance.LoginInGameWallet("devPassword");

        // 2. If normal login failed, use our "Sticky" Backup
        if (account == null)
        {
            // Check if we have a saved Mnemonic in PlayerPrefs
            string savedMnemonic = PlayerPrefs.GetString(PREFS_KEY, "");

            if (!string.IsNullOrEmpty(savedMnemonic))
            {
                Debug.Log("Restoring 'Sticky' Dev Wallet from PlayerPrefs...");
                // Restore the SAME wallet as last time
                Wallet keypair = new Wallet(new Mnemonic(savedMnemonic));
                account = keypair.Account;
            }
            else
            {
                Debug.Log("No Sticky Wallet found. Creating a NEW permanent dev wallet...");
                // Generate NEW Mnemonic
                var newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
                
                // SAVE IT so next time we load the SAME one
                PlayerPrefs.SetString(PREFS_KEY, newMnemonic.ToString());
                PlayerPrefs.Save();

                Wallet keypair = new Wallet(newMnemonic);
                account = keypair.Account;
            }
        }
        PlayerAccount = account;
#else
        // --- MOBILE/WEB LOGIC ---
        PlayerAccount = await Web3.Instance.LoginWalletAdapter();
#endif

        // --- SUCCESS LOGIC ---
        if (PlayerAccount != null)
        {
            Debug.Log($"Wallet connected: {PlayerAccount.PublicKey}");
            UpdateUI(PlayerAccount.PublicKey);
        }
        else
        {
            Debug.LogError("Wallet connection failed.");
        }
    }

    private void UpdateUI(string publicKey)
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (connectedPanel != null) connectedPanel.SetActive(true);

        if (loginPanel2 != null) loginPanel2.SetActive(false);
        if (connectedPanel2 != null) connectedPanel2.SetActive(true);

        if (addressText != null)
        {
            string shortAddress = publicKey.Substring(0, 4) + "..." + publicKey.Substring(publicKey.Length - 4);
            addressText.text = shortAddress;
        }

        if (addressText2 != null)
        {
            string shortAddress = publicKey.Substring(0, 4) + "..." + publicKey.Substring(publicKey.Length - 4);
            addressText2.text = shortAddress;
        }
    }

    public void CopyAddressToClipboard()
    {
        if (PlayerAccount != null)
        {
            GUIUtility.systemCopyBuffer = PlayerAccount.PublicKey;
        }
    }
    
    // Helper to reset if you actually WANT a new wallet
    [ContextMenu("Reset Dev Wallet")]
    public void ResetDevWallet()
    {
        PlayerPrefs.DeleteKey(PREFS_KEY);
        Debug.Log("Dev Wallet Reset! Next play will generate a new address.");
    }
}