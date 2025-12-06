using UnityEngine;
using Solana.Unity.SDK;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Programs;
using System;
using TMPro;
using Solana.Unity.Wallet;

public class PlayerWalletUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI solBalanceText;
    public TextMeshProUGUI solBalanceText2;
    public TextMeshProUGUI tokenBalanceText;

    [Header("Settings")]
    public string tokenMintAddress;
    public bool autoRefresh = true;
    public float refreshInterval = 5f;

    private void Start()
    {
        if (autoRefresh)
        {
            // Start after 1 second to allow wallet to initialize
            InvokeRepeating(nameof(RefreshBalances), 1f, refreshInterval); 
        }
    }

    private Account GetActiveAccount()
    {
        // 1. Check the standard SDK account (Production/Mobile)
        if (Web3.Account != null) return Web3.Account;

        // 2. Fallback to our custom "Sticky" Dev Wallet (Editor)
        if (WalletConnector.PlayerAccount != null) return WalletConnector.PlayerAccount;

        return null;
    }

    public void RefreshBalances()
    {
        if (GetActiveAccount() == null) return;

        ShowSolBalance();

        if (!string.IsNullOrEmpty(tokenMintAddress))
        {
            ShowTokenBalance(tokenMintAddress);
        }
    }

    public async void ShowSolBalance()
    {
        Account activeAccount = GetActiveAccount();
        if (activeAccount == null) return;

        // REMOVED: This line caused the flicker
        // if (solBalanceText != null) solBalanceText.text = "Loading...";

        try
        {
            var balanceResult = await Web3.Rpc.GetBalanceAsync(activeAccount.PublicKey);

            if (balanceResult.WasSuccessful)
            {
                double solBalance = (double)balanceResult.Result.Value / 1_000_000_000;
                
                // Only update the text when we actually have the new number
                if (solBalanceText != null)
                    solBalanceText.text = $"{solBalance:F4} SOL";

                if (solBalanceText2 != null)
                    solBalanceText2.text = $"{solBalance:F4} SOL";
            }
        }
        catch (Exception e)
        {
            // Optional: You might want to remove this too if you want total silence on errors
            // if (solBalanceText != null) solBalanceText.text = "Error"; 
            Debug.LogError("Error getting SOL balance: " + e.Message);
        }
    }

    public async void ShowTokenBalance(string mintAddress)
    {
        Account activeAccount = GetActiveAccount();
        if (activeAccount == null) return;

        // REMOVED: This line caused the flicker
        // if (tokenBalanceText != null) tokenBalanceText.text = "Loading...";

        try
        {
            var tokenAccounts = await Web3.Rpc.GetTokenAccountsByOwnerAsync(
                activeAccount.PublicKey,
                mintAddress,
                TokenProgram.ProgramIdKey
            );

            if (tokenAccounts.WasSuccessful && tokenAccounts.Result.Value.Count > 0)
            {
                var tokenAccount = tokenAccounts.Result.Value[0];
                var accountInfo = tokenAccount.Account.Data.Parsed.Info;
                string balance = accountInfo.TokenAmount.UiAmountString;

                if (tokenBalanceText != null) 
                    tokenBalanceText.text = $"{balance} TOKENS";
            }
            else
            {
                if (tokenBalanceText != null) 
                    tokenBalanceText.text = "0 TOKENS";
            }
        }
        catch (Exception e)
        {
             // Optional: Remove if you want to keep old text on error
            // if (tokenBalanceText != null) tokenBalanceText.text = "Error";
            Debug.LogError("Error getting token balance: " + e.Message);
        }
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(RefreshBalances));
    }
}