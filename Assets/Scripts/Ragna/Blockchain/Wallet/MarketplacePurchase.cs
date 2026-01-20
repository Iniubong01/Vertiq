using UnityEngine;
using Solana.Unity.SDK;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types; 
using Solana.Unity.Programs;
using Solana.Unity.Wallet;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Reown.AppKit.Unity;

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
        // 1. IMMEDIATE VISUAL FEEDBACK
        // Shows up instantly when button is clicked
        ShowPopup("Processing", "Checking wallet...", Color.yellow);

        // 2. SETUP CHECKS
        if (!EnsureWeb3Initialized()) 
        {
            ShowPopup("System Error", "Connection lost.", Color.red);
            return;
        }

        bool isToken = !string.IsNullOrEmpty(mintAddress);
        PublicKey buyerKey = WalletConnector.UserPublicKey;
        
        // Android Fallback
        if (buyerKey == null && AppKit.IsInitialized && AppKit.Account != null)
        {
            buyerKey = new PublicKey(AppKit.Account.Address);
        }

        if (buyerKey == null) { ShowPopup("Wallet", "Connect wallet first.", Color.red); return; }

        PublicKey sellerKey = new PublicKey(sellerWallet);

        PublicKey sourceTokenKey = null;
        PublicKey sourceTokenOwner = null;
        int tokenDecimals = 9;

        // =================================================================
        // 3. PRE-FLIGHT CHECK
        // =================================================================
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
                sourceTokenOwner = tokenAccountPair.Owner;

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
            return; 
        }

        // =================================================================
        // 4. BUILD INSTRUCTIONS
        // =================================================================
        var instructions = new List<TransactionInstruction>();
        instructions.Add(ComputeBudgetProgram.SetComputeUnitLimit(300_000));
        instructions.Add(ComputeBudgetProgram.SetComputeUnitPrice(100_000)); 

        try 
        {
             if (isToken)
            {
                PublicKey tokenMint = new PublicKey(mintAddress);
                ulong amountRaw = (ulong)(amount * Math.Pow(10, tokenDecimals));
                
                var dest = await FindTokenAccountBroad(sellerKey, tokenMint.ToString());
                PublicKey destParams = dest.PublicKey;
                
                if(destParams == null)
                {
                    destParams = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(sellerKey, tokenMint);
                    instructions.Add(AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(buyerKey, sellerKey, tokenMint));
                }

                var tx = TokenProgram.Transfer(sourceTokenKey, destParams, amountRaw, buyerKey);
                instructions.Add(new TransactionInstruction { ProgramId = sourceTokenOwner ?? TokenProgram.ProgramIdKey, Keys = tx.Keys, Data = tx.Data });
            }
            else
            {
                ulong lamports = (ulong)(amount * 1_000_000_000);
                instructions.Add(SystemProgram.Transfer(buyerKey, sellerKey, lamports));
            }
        }
        catch (Exception ex) { Debug.LogError(ex); return; }

        // 5. GET BLOCKHASH
        var blockHash = await Web3.Rpc.GetLatestBlockHashAsync(Commitment.Finalized);
        if (!blockHash.WasSuccessful) { ShowPopup("Error", "Network error.", Color.red); return; }

        // 6. SIGN & BROADCAST
        string signedTxBase64 = null;

        try
        {
            // PATH A: EDITOR
            if (WalletConnector.PlayerAccount != null)
            {
                var transaction = new Transaction
                {
                    RecentBlockHash = blockHash.Result.Value.Blockhash,
                    FeePayer = buyerKey,
                    Instructions = instructions
                };

                transaction.Sign(WalletConnector.PlayerAccount);
                ShowPopup("Processing", "Sending...", Color.yellow);
                
                var res = await Web3.Rpc.SendTransactionAsync(transaction.Serialize());
                
                if (res.WasSuccessful) 
                {
                    ShowPopup("Success!", "Purchase Complete!", Color.green);
                    onSuccess?.Invoke();
                }
                else 
                {
                    ShowPopup("Failed", "Transaction Failed", Color.red);
                }
                return;
            }
            
            // PATH B: ANDROID (AppKit)
            if (AppKit.IsInitialized)
            {
                var transaction = new Transaction
                {
                    RecentBlockHash = blockHash.Result.Value.Blockhash,
                    FeePayer = buyerKey,
                    Instructions = instructions,
                    Signatures = new List<SignaturePubKeyPair> { new SignaturePubKeyPair { PublicKey = buyerKey, Signature = new byte[64] } }
                };

                // Request Signature (Wallet opens here)
                ShowPopup("Wallet", "Please sign in wallet...", Color.yellow);
                var signResponse = await AppKit.Solana.SignTransactionAsync(Convert.ToBase64String(transaction.Serialize()));

                if (signResponse != null && !string.IsNullOrEmpty(signResponse.TransactionBase64))
                {
                    signedTxBase64 = signResponse.TransactionBase64;
                    // IMMEDIATE FEEDBACK AFTER RETURNING FROM WALLET
                    ShowPopup("Processing", "Finalizing transaction...", Color.yellow);
                }
                else
                {
                    ShowPopup("Cancelled", "Transaction cancelled.", Color.red);
                    return; 
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Sign Error: {ex.Message}");
            return;
        }

        // 7. CONFIRM LOOP
        if (!string.IsNullOrEmpty(signedTxBase64))
        {
            string signature = GetSignatureFromTx(signedTxBase64);
            byte[] signedBytes = Convert.FromBase64String(signedTxBase64);

            float timeout = 45f;
            float startTime = Time.time;
            bool confirmed = false;

            while (Time.time - startTime < timeout && !confirmed)
            {
                await Web3.Rpc.SendTransactionAsync(signedBytes, skipPreflight: true, commitment: Commitment.Processed);
                await Task.Delay(1000);

                if (string.IsNullOrEmpty(signature)) break;
                
                var status = await Web3.Rpc.GetSignatureStatusesAsync(new List<string> { signature }, true);
                if (status.WasSuccessful && status.Result.Value != null && status.Result.Value.Count > 0)
                {
                    var s = status.Result.Value[0];
                    if (s != null && (s.ConfirmationStatus == "confirmed" || s.ConfirmationStatus == "finalized"))
                    {
                        if (s.Error == null) confirmed = true;
                        else 
                        {
                            ShowPopup("Failed", "Transaction failed.", Color.red);
                            return; 
                        }
                    }
                }
            }

            if (confirmed)
            {
                ShowPopup("Success!", "Purchase Complete!", Color.green);
                onSuccess?.Invoke();
            }
            else
            {
                ShowPopup("Timeout", "Check wallet history.", Color.red);
            }
        }
    }

    private string GetSignatureFromTx(string base64)
    {
        try {
            byte[] b = Convert.FromBase64String(base64);
            if(b.Length > 65) {
                byte[] sig = new byte[64];
                Array.Copy(b, 1, sig, 0, 64);
                return Solana.Unity.Wallet.Utilities.Encoders.Base58.EncodeData(sig);
            }
        } catch {}
        return null;
    }

    private async Task<(PublicKey PublicKey, PublicKey Owner)> FindTokenAccountBroad(PublicKey owner, string mint)
    {
        if (!EnsureWeb3Initialized()) return (null, null);
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