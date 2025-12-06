using UnityEngine;
using Solana.Unity.SDK;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Programs;
using Solana.Unity.Wallet;
using System.Collections.Generic;
using TMPro;
using System;

public class MarketplacePurchase : MonoBehaviour
{
    [Header("Payment Setup")]
    public string sellerWallet; 

    [Header("UI Feedback")]
    public NotificationPopup notificationPopup; // DRAG YOUR POPUP PREFAB HERE

    private Account GetActiveAccount()
    {
        if (Web3.Account != null) return Web3.Account;
        if (WalletConnector.PlayerAccount != null) return WalletConnector.PlayerAccount;
        return null;
    }

    public async void PurchaseWithSol(float priceInSol, Action onPurchaseSuccess)
    {
        Debug.Log($"[MarketplacePurchase] Request received for {priceInSol} SOL");

        Account activeAccount = GetActiveAccount();

        // 1. CHECK: Is Wallet Connected?
        if (activeAccount == null)
        {
            ShowPopup("Wallet Error", "Please connect your wallet first.", Color.red);
            return;
        }

        // 2. CHECK: Is Seller Wallet Set?
        if (string.IsNullOrEmpty(sellerWallet))
        {
            ShowPopup("Config Error", "Seller wallet address is missing.", Color.red);
            return;
        }

        // 3. PRE-CHECK: Check Balance
        try 
        {
            var balanceResult = await Web3.Rpc.GetBalanceAsync(activeAccount.PublicKey);
            if(balanceResult.WasSuccessful)
            {
                double currentBalance = (double)balanceResult.Result.Value / 1_000_000_000;
                if(currentBalance < priceInSol)
                {
                    ShowPopup("Not Enough SOL", "You don't have enough SOL to complete this purchase.", Color.magenta);
                    return;
                }
            }
        }
        catch(Exception ex) 
        { 
            Debug.LogWarning("Balance check skipped due to error: " + ex.Message);
        }

        // 4. PROCESS: Start Transaction
        ShowPopup("Processing", $"Please approve the transaction of {priceInSol} SOL...", Color.yellow);

        try
        {
            ulong lamports = (ulong)(priceInSol * 1_000_000_000);
            var fromPublicKey = activeAccount.PublicKey;
            var toPublicKey = new PublicKey(sellerWallet);

            var transferInstruction = SystemProgram.Transfer(
                fromPublicKey,
                toPublicKey,
                lamports
            );

            var blockHash = await Web3.Rpc.GetLatestBlockHashAsync();
            if (!blockHash.WasSuccessful)
            {
                ShowPopup("Connection Failed", "Could not reach Solana network.", Color.red);
                return;
            }

            var transaction = new Transaction
            {
                RecentBlockHash = blockHash.Result.Value.Blockhash,
                FeePayer = fromPublicKey,
                Instructions = new List<TransactionInstruction> { transferInstruction },
                Signatures = new List<SignaturePubKeyPair>()
            };

            RequestResult<string> result;

            if (Web3.Wallet != null)
            {
                result = await Web3.Wallet.SignAndSendTransaction(transaction);
            }
            else
            {
                // Editor Fallback
                transaction.Sign(activeAccount);
                byte[] txData = transaction.Serialize();
                result = await Web3.Rpc.SendTransactionAsync(Convert.ToBase64String(txData));
            }

            if (result.WasSuccessful)
            {
                ShowPopup("Purchase Complete", "Your game tokens are now added. Balance updated.", Color.green);
                Debug.Log($"Sent {priceInSol} SOL. TX: {result.Result}");

                // Update UI Immediately
                var ui = FindObjectOfType<PlayerWalletUI>();
                if (ui != null) ui.RefreshBalances();
                
                onPurchaseSuccess?.Invoke();
            }
            else
            {
                ShowPopup("Transaction Failed", "Something went wrong. Your tokens and SOL remain unchanged.", Color.red);
                Debug.LogError($"TX Failed: {result.Reason}");
            }
        }
        catch (Exception e)
        {
            ShowPopup("Transaction Failed", "Error: " + e.Message, Color.red);
            Debug.LogError(e);
        }
    }

    private void ShowPopup(string title, string message, Color color)
    {
        // 1. Always Log to Console (So you see it even if UI fails)
        Debug.Log($"[POPUP EVENT] {title}: {message}");

        // 2. Show UI
        if (notificationPopup != null)
        {
            notificationPopup.Show(title, message, color);
        }
        else
        {
            Debug.LogError("NOTIFICATION POPUP IS NOT ASSIGNED IN INSPECTOR!");
        }
    }
}