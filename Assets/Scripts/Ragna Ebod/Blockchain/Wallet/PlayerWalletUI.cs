using UnityEngine;
using Solana.Unity.SDK;
using Solana.Unity.Rpc.Models;
using System;
using TMPro;

public class PlayerWalletUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI solBalanceText;
    public TextMeshProUGUI solBalanceText2;
    public TextMeshProUGUI tokenBalanceText;
    public TextMeshProUGUI tokenBalanceText2;

    [Header("Settings")]
    public string tokenMintAddress;
    public bool autoRefresh = true;
    public float refreshInterval = 15f;

    // MEMORY: Stores the last valid balance found
    private double _lastValidTokenBalance = -1;

    private void Start()
    {
        if (!string.IsNullOrEmpty(tokenMintAddress))
            tokenMintAddress = tokenMintAddress.Trim();

        // Initialize UI with empty/loading state
        UpdateTokenUI("--"); 

        if (autoRefresh)
        {
            InvokeRepeating(nameof(RefreshBalances), 1f, refreshInterval);
        }
    }

    public void RefreshBalances()
    {
        if (WalletConnector.UserPublicKey == null) return;
        ShowSolBalance();
        if (!string.IsNullOrEmpty(tokenMintAddress)) ShowTokenBalance(tokenMintAddress);
    }

    public async void ShowSolBalance()
    {
        if (WalletConnector.UserPublicKey == null || Web3.Rpc == null) return;

        try
        {
            var balanceResult = await Web3.Rpc.GetBalanceAsync(WalletConnector.UserPublicKey);
            if (balanceResult.WasSuccessful)
            {
                double solBalance = (double)balanceResult.Result.Value / 1_000_000_000;
                
                if (solBalanceText != null) solBalanceText.text = $"{solBalance:F4}";
                if (solBalanceText2 != null) solBalanceText2.text = $"{solBalance:F4}";
            }
        }
        catch (Exception e) { Debug.LogError($"[WalletUI] SOL Error: {e.Message}"); }
    }

    public async void ShowTokenBalance(string mintAddress)
    {
        if (WalletConnector.UserPublicKey == null) return;

        try
        {
            var tokenAccounts = await Web3.Rpc.GetTokenAccountsByOwnerAsync(
                WalletConnector.UserPublicKey,
                mintAddress,
                null
            );

            // CASE 1: SUCCESS - Found Tokens
            if (tokenAccounts.WasSuccessful && tokenAccounts.Result.Value.Count > 0)
            {
                var tokenAccount = tokenAccounts.Result.Value[0];
                double balance = double.Parse(tokenAccount.Account.Data.Parsed.Info.TokenAmount.UiAmountString);

                Debug.Log($"[WalletUI] ✅ FOUND {balance} $PLAY");
                
                // SAVE TO MEMORY
                _lastValidTokenBalance = balance;
                UpdateTokenUI($"{balance:F0}");
            }
            // CASE 2: SUCCESS - But RPC says 0 (Possible Glitch)
            else if (tokenAccounts.WasSuccessful && tokenAccounts.Result.Value.Count == 0)
            {
                // CRITICAL FIX:
                // If we previously saw money (e.g. 50), and now RPC says 0, IGNORE IT.
                // It is likely an RPC timeout returning an empty list.
                if (_lastValidTokenBalance > 0)
                {
                    Debug.LogWarning($"[WalletUI] ⚠️ RPC returned 0, but memory says {_lastValidTokenBalance}. Ignoring glitch.");
                    // Keep showing the old balance. Do NOT update UI to 0.
                }
                else
                {
                    // Truly 0 (New user)
                    UpdateTokenUI("0");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WalletUI] Token Error: {e.Message}");
        }
    }

    private void UpdateTokenUI(string balanceString)
    {
        if (tokenBalanceText != null) tokenBalanceText.text = $"{balanceString}";
        if (tokenBalanceText2 != null) tokenBalanceText2.text = $"{balanceString}";
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(RefreshBalances));
    }
}