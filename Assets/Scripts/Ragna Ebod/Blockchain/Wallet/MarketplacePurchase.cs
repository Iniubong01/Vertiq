using UnityEngine;
using Solana.Unity.SDK;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types; 
using Solana.Unity.Programs;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Core.Http; // [FIX] Added for RequestResult
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class MarketplacePurchase : MonoBehaviour
{
    [Header("Payment Setup")]
    public string sellerWallet;
    public NotificationPopup notificationPopup;

    public async void PurchaseWithSol(float priceInSol, Action onPurchaseSuccess)
    {
        await PerformPurchase(priceInSol, null, onPurchaseSuccess);
    }

    public async void PurchaseWithSplToken(float tokenAmount, string mintAddress, Action onPurchaseSuccess)
    {
        await PerformPurchase(tokenAmount, mintAddress, onPurchaseSuccess);
    }

    private bool EnsureWeb3Initialized()
    {
        if (Web3.Instance == null || Web3.Rpc == null)
        {
            if (Web3.Instance != null)
            {
                Web3.Instance.customRpc = "https://api.mainnet-beta.solana.com"; 
                return Web3.Rpc != null;
            }
            return false;
        }
        return true;
    }

    private async Task PerformPurchase(float amount, string mintAddress, Action onSuccess)
    {
        ShowPopup("Processing", "Checking wallet...", Color.yellow);

        if (!EnsureWeb3Initialized()) 
        {
            ShowPopup("System Error", "Connection lost.", Color.red);
            return;
        }

        bool isToken = !string.IsNullOrEmpty(mintAddress);
        
        PublicKey buyerKey = WalletConnector.UserPublicKey;

        if (buyerKey == null)
        {
            if (Web3.Wallet != null && Web3.Wallet.Account != null)
            {
                buyerKey = Web3.Wallet.Account.PublicKey;
            }
            else
            {
                ShowPopup("Wallet", "Connect wallet first.", Color.red);
                return;
            }
        }

        PublicKey sellerKey = new PublicKey(sellerWallet);
        PublicKey sourceTokenKey = null;
        int tokenDecimals = 9;

        // ---------------------------------------------------------
        // PRE-FLIGHT CHECK
        // ---------------------------------------------------------
        try 
        {
            var solBalanceReq = await Web3.Rpc.GetBalanceAsync(buyerKey);
            if (!solBalanceReq.WasSuccessful) 
            {
                ShowPopup("Network", "Check connection.", Color.red);
                return;
            }

            ulong currentLamports = solBalanceReq.Result.Value;
            ulong estFees = 50000; 

            if (isToken)
            {
                if (currentLamports < estFees)
                {
                    ShowPopup("Gas Error", "Need SOL for fees.", Color.red);
                    return;
                }

                var tokenAccountPair = await FindTokenAccountBroad(buyerKey, mintAddress);
                sourceTokenKey = tokenAccountPair.PublicKey;

                if (sourceTokenKey == null)
                {
                    ShowPopup("Balance", "0 $PLAY", Color.red);
                    return;
                }

                var tokenBalReq = await Web3.Rpc.GetTokenAccountBalanceAsync(sourceTokenKey.ToString());
                if (tokenBalReq.WasSuccessful)
                {
                    if (double.TryParse(tokenBalReq.Result.Value.UiAmountString, out double currentTokenBalance))
                    {
                        if (currentTokenBalance < amount)
                        {
                            ShowPopup("Balance", $"Need {amount} $PLAY.", Color.red);
                            return;
                        }
                        tokenDecimals = tokenBalReq.Result.Value.Decimals;
                    }
                }
                else 
                { 
                    ShowPopup("Error", "Balance check failed.", Color.red); 
                    return; 
                }
            }
            else
            {
                ulong requiredTotal = (ulong)(amount * 1_000_000_000) + estFees;
                if (currentLamports < requiredTotal)
                {
                     ShowPopup("Balance", $"Need {amount} SOL.", Color.red);
                     return;
                }
            }
        }
        catch (Exception ex) 
        { 
            Debug.LogError($"Check Error: {ex.Message}");
            ShowPopup("Error", "Balance check failed.", Color.red);
            return; 
        }

        // ---------------------------------------------------------
        // BUILD TRANSACTION USING SDK PATTERN
        // ---------------------------------------------------------
        try 
        {
            var instructions = new List<TransactionInstruction>();
            
            instructions.Add(ComputeBudgetProgram.SetComputeUnitLimit(300_000));
            instructions.Add(ComputeBudgetProgram.SetComputeUnitPrice(100_000));

            if (isToken)
            {
                PublicKey tokenMint = new PublicKey(mintAddress);
                ulong amountRaw = (ulong)(amount * Math.Pow(10, tokenDecimals));
                
                var dest = await FindTokenAccountBroad(sellerKey, tokenMint.ToString());
                PublicKey destTokenAccount = dest.PublicKey;
                
                if (destTokenAccount == null)
                {
                    destTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(sellerKey, tokenMint);
                    instructions.Add(
                        AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                            buyerKey, 
                            sellerKey, 
                            tokenMint
                        )
                    );
                }

                instructions.Add(
                    TokenProgram.Transfer(
                        sourceTokenKey,      
                        destTokenAccount,    
                        amountRaw,          
                        buyerKey            
                    )
                );
            }
            else
            {
                ulong lamports = (ulong)(amount * 1_000_000_000);
                instructions.Add(
                    SystemProgram.Transfer(
                        buyerKey,   
                        sellerKey,  
                        lamports    
                    )
                );
            }

            var blockHashResult = await Web3.Rpc.GetLatestBlockHashAsync(Commitment.Finalized);
            if (!blockHashResult.WasSuccessful) 
            { 
                ShowPopup("Error", "Network error.", Color.red); 
                return; 
            }

            // ---------------------------------------------------------
            // BUILD TRANSACTION AND SIGN
            // ---------------------------------------------------------
            var transaction = new Transaction
            {
                RecentBlockHash = blockHashResult.Result.Value.Blockhash,
                FeePayer = buyerKey,
                Instructions = instructions
            };

            RequestResult<string> result = null;

#if UNITY_EDITOR
            // PATH A: EDITOR — sign directly with the sticky wallet Account
            Account editorAccount = WalletConnector.PlayerAccount;
            if (editorAccount == null)
            {
                ShowPopup("Error", "No editor wallet found.", Color.red);
                return;
            }
            ShowPopup("Wallet", "Signing with Editor Wallet...", Color.yellow);
            var signature = editorAccount.Sign(transaction.CompileMessage());
            transaction.AddSignature(editorAccount.PublicKey, signature);
            byte[] txBytes = transaction.Serialize();
            string txBase64 = Convert.ToBase64String(txBytes);
            result = await Web3.Rpc.SendTransactionAsync(txBase64);
#else
            // PATH B: MOBILE (Android/iOS) — use wallet adapter for native signing
            if (Web3.Wallet == null)
            {
                ShowPopup("Error", "No wallet connected.", Color.red);
                return;
            }
            ShowPopup("Wallet", "Please approve on device...", Color.yellow);
            result = await Web3.Wallet.SignAndSendTransaction(transaction);
#endif

            HandleTransactionResult(result != null && result.WasSuccessful, result != null ? result.Reason : "Unknown error", onSuccess);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Transaction Error: {ex.Message}\n{ex.StackTrace}");
            ShowPopup("Error", "Transaction failed.", Color.red);
        }
    }

    private void HandleTransactionResult(bool success, string reason, Action onSuccess)
    {
        if (success)
        {
            ShowPopup("Success!", "Purchase Complete!", Color.green);
            onSuccess?.Invoke();
        }
        else
        {
            Debug.LogError($"Transaction failed: {reason}");
            ShowPopup("Failed", $"Transaction failed.", Color.red);
        }
    }

    private async Task<(PublicKey PublicKey, PublicKey Owner)> FindTokenAccountBroad(PublicKey owner, string mint)
    {
        if (!EnsureWeb3Initialized()) return (null, null);
        
        try
        {
            var result = await Web3.Rpc.GetTokenAccountsByOwnerAsync(owner, mint, null);
            if (result.WasSuccessful && result.Result.Value.Count > 0)
            {
                var data = result.Result.Value[0];
                return (new PublicKey(data.PublicKey), new PublicKey(data.Account.Owner));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Token account lookup error: {ex.Message}");
        }
        
        return (null, null);
    }

    private void ShowPopup(string title, string message, Color color)
    {
        if (notificationPopup != null) 
        {
            notificationPopup.Show(title, message, color);
        }
        else
        {
            Debug.Log($"[{title}] {message}");
        }
    }
}