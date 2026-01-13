using UnityEngine;
using Solana.Unity.SDK;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Programs;
using Solana.Unity.Wallet;
using System.Collections.Generic;
using System;
using Solana.Unity.Rpc.Core.Http;
using System.Threading.Tasks;

public class MarketplacePurchase : MonoBehaviour
{
    [Header("Payment Setup")]
    public string sellerWallet; 

    [Header("UI Feedback")]
    public NotificationPopup notificationPopup;

    // Solana Rent Exemption limit (~0.00089 SOL)
    private const ulong MIN_RENT_EXEMPT_LAMPORTS = 890880; 

    public async void PurchaseWithSol(float priceInSol, Action onPurchaseSuccess)
    {
        await PerformPurchase(priceInSol, null, onPurchaseSuccess);
    }

    public async void PurchaseWithSplToken(float tokenAmount, string mintAddress, Action onPurchaseSuccess)
    {
        await PerformPurchase(tokenAmount, mintAddress, onPurchaseSuccess);
    }

    private async Task PerformPurchase(float amount, string mintAddress, Action onSuccess)
    {
        bool isToken = !string.IsNullOrEmpty(mintAddress);
        string currencyName = isToken ? "$PLAY" : "SOL";

        // --- 1. VALIDATION ---
        PublicKey buyerKey = WalletConnector.UserPublicKey;
        if (buyerKey == null) { ShowPopup("Wallet Error", "Connect wallet first.", Color.red); return; }
        
        if (string.IsNullOrEmpty(sellerWallet)) { ShowPopup("Config Error", "Seller address missing.", Color.red); return; }
        PublicKey sellerKey = new PublicKey(sellerWallet);

        var instructions = new List<TransactionInstruction>();

        try
        {
            // --- 2. FEE & BALANCE SAFETY CHECK ---
            ulong minSolForFees = 5000; // ~0.000005 SOL for network fee
            
            // Calculate total SOL required to be in the wallet
            ulong requiredSolTotal = minSolForFees; 
            if (!isToken) requiredSolTotal += (ulong)(amount * 1_000_000_000);

            // Fetch SOL Balance
            var balanceResult = await Web3.Rpc.GetBalanceAsync(buyerKey);
            
            if (!balanceResult.WasSuccessful)
            {
                ShowPopup("Network Error", "Could not fetch balance.", Color.red);
                return;
            }

            ulong currentSolBalance = balanceResult.Result.Value;

            // CHECK 1: Do we have enough SOL?
            if (currentSolBalance < requiredSolTotal)
            {
                Debug.LogWarning($"[Marketplace] Low SOL. Have: {currentSolBalance}, Need: {requiredSolTotal}");
                
                if (!isToken)
                {
                    ShowPopup("Insufficient SOL", $"You need {amount} SOL + Fees.", Color.red);
                }
                else
                {
                    ShowPopup("Insufficient SOL", "You need SOL for gas fees.", Color.red);
                }
                return; // Stop here gracefully
            }

            // --- 3. BUILD INSTRUCTIONS ---
            if (isToken)
            {
                PublicKey tokenMint = new PublicKey(mintAddress);

                // CHECK 2: Do we have enough Tokens?
                var sourceResult = await FindTokenAccountBroad(buyerKey, tokenMint.ToString());
                if (sourceResult.PublicKey == null) 
                {
                    ShowPopup("Insufficient Funds", $"You have 0 {currencyName}.", Color.red);
                    return;
                }

                // Check actual token balance inside the account
                var tokenBalanceResult = await Web3.Rpc.GetTokenAccountBalanceAsync(sourceResult.PublicKey.ToString());
                if (tokenBalanceResult.WasSuccessful)
                {
                    // [FIX] Use UiAmountString to fix CS0618 & CS0266 errors
                    double currentTokens = 0;
                    string amountString = tokenBalanceResult.Result.Value.UiAmountString;
                    
                    if (double.TryParse(amountString, out double parsedVal))
                    {
                        currentTokens = parsedVal;
                    }
                    
                    if (currentTokens < amount)
                    {
                        ShowPopup("Low Balance", $"You need {amount} {currencyName}.", Color.red);
                        return; // Stop here gracefully
                    }
                }

                // Calculate Raw Amount
                int decimals = await GetTokenDecimals(tokenMint);
                ulong amountRaw = (ulong)(amount * Math.Pow(10, decimals));

                // Check/Create Seller Token Account
                var destResult = await FindTokenAccountBroad(sellerKey, tokenMint.ToString());
                PublicKey destAccount = destResult.PublicKey;
                
                if (destAccount == null)
                {
                    destAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(sellerKey, tokenMint);
                    instructions.Add(AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(buyerKey, sellerKey, tokenMint));
                }

                var templateIx = TokenProgram.Transfer(sourceResult.PublicKey, destAccount, amountRaw, buyerKey);
                instructions.Add(new TransactionInstruction { ProgramId = sourceResult.Owner, Keys = templateIx.Keys, Data = templateIx.Data });
            }
            else
            {
                // SOL Logic
                ulong lamportsToSend = (ulong)(amount * 1_000_000_000);

                // --- 4. RENT EXEMPTION CHECK ---
                if (lamportsToSend < MIN_RENT_EXEMPT_LAMPORTS)
                {
                    var sellerBalance = await Web3.Rpc.GetBalanceAsync(sellerKey);
                    if (sellerBalance.WasSuccessful && sellerBalance.Result.Value == 0)
                    {
                        ShowPopup("Amount Error", "Transfer too small (Rent Limit).", Color.red);
                        return;
                    }
                }

                instructions.Add(SystemProgram.Transfer(buyerKey, sellerKey, lamportsToSend));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Marketplace] Setup Failed: {ex.Message}");
            ShowPopup("Error", "Setup failed. Check logs.", Color.red);
            return;
        }

        // --- 5. EXECUTE TRANSACTION ---
        ShowPopup("Processing", $"Approve {amount} {currencyName}...", Color.yellow);
        
        var blockHash = await Web3.Rpc.GetLatestBlockHashAsync();
        if (!blockHash.WasSuccessful) 
        { 
            ShowPopup("Connection Error", "Failed to get Blockhash.", Color.red); 
            return; 
        }

        var transaction = new Transaction
        {
            RecentBlockHash = blockHash.Result.Value.Blockhash,
            FeePayer = buyerKey,
            Instructions = instructions,
            Signatures = new List<SignaturePubKeyPair>()
        };

        try 
        {
            RequestResult<string> result;
            if (WalletConnector.PlayerAccount != null)
            {
                transaction.Sign(WalletConnector.PlayerAccount);
                result = await Web3.Rpc.SendTransactionAsync(Convert.ToBase64String(transaction.Serialize()));
            }
            else
            {
                result = await Web3.Wallet.SignAndSendTransaction(transaction);
            }

            if (result.WasSuccessful)
            {
                ShowPopup("Success!", "Purchase Complete!", Color.green);
                onSuccess?.Invoke();
            }
            else
            {
                string rawError = result.RawRpcResponse ?? result.Reason;
                Debug.LogError($"[Marketplace] Transaction Failed: {rawError}");

                if (rawError.Contains("InsufficientFundsForRent"))
                {
                    ShowPopup("Failed", "Transfer amount too small.", Color.red);
                }
                else if (rawError.Contains("Insufficient funds") || rawError.Contains("0x1"))
                {
                    ShowPopup("Failed", "Insufficient Funds.", Color.red);
                }
                else
                {
                    ShowPopup("Failed", "Transaction Failed.", Color.red);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Marketplace] RPC/JSON Error: {ex.Message}");
            ShowPopup("Network Error", "RPC failure. Check internet.", Color.red);
        }
    }

    private async Task<int> GetTokenDecimals(PublicKey mint)
    {
        var result = await Web3.Rpc.GetTokenSupplyAsync(mint.ToString());
        return result.WasSuccessful ? result.Result.Value.Decimals : 9;
    }

    private async Task<(PublicKey PublicKey, PublicKey Owner)> FindTokenAccountBroad(PublicKey owner, string mint)
    {
        var result = await Web3.Rpc.GetTokenAccountsByOwnerAsync(owner, mint, null);
        if (result.WasSuccessful && result.Result.Value.Count > 0)
        {
            var data = result.Result.Value[0];
            return (new PublicKey(data.PublicKey), new PublicKey(data.Account.Owner));
        }
        return (null, null);
    }

    private void ShowPopup(string title, string message, Color color)
    {
        if (notificationPopup != null) notificationPopup.Show(title, message, color);
    }
}