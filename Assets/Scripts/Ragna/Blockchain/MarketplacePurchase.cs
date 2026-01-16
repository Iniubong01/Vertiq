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
        // 1. SETUP
        if (!EnsureWeb3Initialized()) 
        {
            ShowPopup("System Error", "Connection lost.", Color.red);
            return;
        }

        bool isToken = !string.IsNullOrEmpty(mintAddress);
        string currencyName = isToken ? "$PLAY" : "SOL";
        PublicKey buyerKey = WalletConnector.UserPublicKey;
        
        // Android Fallback
        if (buyerKey == null && AppKit.IsInitialized && AppKit.Account != null)
        {
            buyerKey = new PublicKey(AppKit.Account.Address);
        }

        if (buyerKey == null) { ShowPopup("Wallet Error", "Connect wallet first.", Color.red); return; }

        PublicKey sellerKey = new PublicKey(sellerWallet);

        // 2. BUILD INSTRUCTIONS
        var instructions = new List<TransactionInstruction>();
        instructions.Add(ComputeBudgetProgram.SetComputeUnitLimit(300_000));
        instructions.Add(ComputeBudgetProgram.SetComputeUnitPrice(100_000)); 

        try 
        {
             if (isToken)
            {
                PublicKey tokenMint = new PublicKey(mintAddress);
                var source = await FindTokenAccountBroad(buyerKey, tokenMint.ToString());
                if (source.PublicKey == null) { ShowPopup("Error", "No token account", Color.red); return; }
                
                int decimals = await GetTokenDecimals(tokenMint);
                ulong amountRaw = (ulong)(amount * Math.Pow(10, decimals));
                
                var dest = await FindTokenAccountBroad(sellerKey, tokenMint.ToString());
                PublicKey destParams = dest.PublicKey;
                if(destParams == null)
                {
                    destParams = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(sellerKey, tokenMint);
                    instructions.Add(AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(buyerKey, sellerKey, tokenMint));
                }

                var tx = TokenProgram.Transfer(source.PublicKey, destParams, amountRaw, buyerKey);
                instructions.Add(new TransactionInstruction { ProgramId = source.Owner, Keys = tx.Keys, Data = tx.Data });
            }
            else
            {
                ulong lamports = (ulong)(amount * 1_000_000_000);
                instructions.Add(SystemProgram.Transfer(buyerKey, sellerKey, lamports));
            }
        }
        catch (Exception ex) { Debug.LogError(ex); return; }

        // 3. GET BLOCKHASH
        ShowPopup("Processing", "Please sign...", Color.yellow);
        var blockHash = await Web3.Rpc.GetLatestBlockHashAsync(Commitment.Finalized);
        if (!blockHash.WasSuccessful) { ShowPopup("Error", "Network error.", Color.red); return; }

        // 4. SIGN & BROADCAST
        string signedTxBase64 = null;

        try
        {
            // =================================================================
            // PATH A: EDITOR (Private Key)
            // =================================================================
            if (WalletConnector.PlayerAccount != null)
            {
                Debug.Log("[Marketplace] Signing with Local Editor Account...");
                
                var transaction = new Transaction
                {
                    RecentBlockHash = blockHash.Result.Value.Blockhash,
                    FeePayer = buyerKey,
                    Instructions = instructions
                    // NOTE: Do NOT add placeholder signatures here for Editor!
                    // It corrupts the transaction sanitation.
                };

                // Sign directly
                transaction.Sign(WalletConnector.PlayerAccount);
                signedTxBase64 = Convert.ToBase64String(transaction.Serialize());
            }
            // =================================================================
            // PATH B: ANDROID / APPKIT (External Wallet)
            // =================================================================
            else if (AppKit.IsInitialized)
            {
                Debug.Log("[Marketplace] Preparing AppKit Transaction...");

                var transaction = new Transaction
                {
                    RecentBlockHash = blockHash.Result.Value.Blockhash,
                    FeePayer = buyerKey,
                    Instructions = instructions,
                    // [ANDROID ONLY FIX] Add placeholder so Wallet sees 1 signer
                    Signatures = new List<SignaturePubKeyPair> 
                    { 
                        new SignaturePubKeyPair { PublicKey = buyerKey, Signature = new byte[64] } 
                    }
                };

                byte[] txBytes = transaction.Serialize();
                string unsigTxBase64 = Convert.ToBase64String(txBytes);

                var signResponse = await AppKit.Solana.SignTransactionAsync(unsigTxBase64);

                if (signResponse != null && !string.IsNullOrEmpty(signResponse.TransactionBase64))
                {
                    signedTxBase64 = signResponse.TransactionBase64;
                }
                else
                {
                    ShowPopup("Cancelled", "Wallet rejected transaction.", Color.red);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Marketplace] Signing Error: {ex.Message}");
            ShowPopup("Error", "Signing failed.", Color.red);
            return;
        }

        // 5. CONFIRMATION LOOP
        if (!string.IsNullOrEmpty(signedTxBase64))
        {
            ShowPopup("Confirming", "Waiting for network...", Color.yellow);
            
            // Fix for Editor: The signature extraction might differ slightly if not fully signed/encoded same way
            // But usually this works for both.
            byte[] txBytesForSig = Convert.FromBase64String(signedTxBase64);
            string signature = "";
            
            if (txBytesForSig.Length > 65) 
            {
                byte[] sigBytes = new byte[64];
                Array.Copy(txBytesForSig, 1, sigBytes, 0, 64);
                signature = Solana.Unity.Wallet.Utilities.Encoders.Base58.EncodeData(sigBytes);
            }

            Debug.Log($"[Marketplace] Tracking Signature: {signature}");

            // Immediate Send
            await Web3.Rpc.SendTransactionAsync(txBytesForSig, skipPreflight: true, commitment: Commitment.Processed);

            // Wait loop
            float timeout = 45f;
            float startTime = Time.time;
            bool confirmed = false;

            while (Time.time - startTime < timeout && !confirmed)
            {
                await Task.Delay(1000);
                
                // Re-broadcast (Spamming is safe)
                await Web3.Rpc.SendTransactionAsync(txBytesForSig, skipPreflight: true, commitment: Commitment.Processed);

                var status = await Web3.Rpc.GetSignatureStatusesAsync(new List<string> { signature }, true);
                if (status.WasSuccessful && status.Result.Value != null && status.Result.Value.Count > 0)
                {
                    var s = status.Result.Value[0];
                    // Check if status exists (it might be null if tx hasn't hit node yet)
                    if (s != null)
                    {
                        if (s.ConfirmationStatus == "confirmed" || s.ConfirmationStatus == "finalized")
                        {
                            if (s.Error == null) confirmed = true;
                            else 
                            {
                                Debug.LogError($"[Marketplace] On-chain Error: {s.Error}");
                                ShowPopup("Failed", "Transaction failed on-chain.", Color.red);
                                return; 
                            }
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
                ShowPopup("Timeout", "Network congested. Check wallet.", Color.red);
            }
        }
    }

    private async Task<int> GetTokenDecimals(PublicKey mint)
    {
        if (!EnsureWeb3Initialized()) return 9;
        var result = await Web3.Rpc.GetTokenSupplyAsync(mint.ToString());
        return result.WasSuccessful ? result.Result.Value.Decimals : 9;
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