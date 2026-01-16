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
            // Auto-recover RPC if lost (common on Android)
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
        // 1. SETUP CHECKS
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
        
        // [CRITICAL] Priority Fee: Increase to 100k to ensure it lands on Mainnet
        instructions.Add(ComputeBudgetProgram.SetComputeUnitLimit(300_000));
        instructions.Add(ComputeBudgetProgram.SetComputeUnitPrice(100_000)); 

        try 
        {
            // Check Balance
            var balance = await Web3.Rpc.GetBalanceAsync(buyerKey);
            if (!balance.WasSuccessful) { ShowPopup("Error", "Check internet.", Color.red); return; }

            // Add Transfer Instruction
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
        ShowPopup("Processing", "Please sign in wallet...", Color.yellow);
        // Use Finalized to prevent "Blockhash not found" errors
        var blockHash = await Web3.Rpc.GetLatestBlockHashAsync(Commitment.Finalized);
        if (!blockHash.WasSuccessful) { ShowPopup("Error", "Network error.", Color.red); return; }

        // 4. CREATE TRANSACTION (SAFE MODE)
        // We use the Transaction class + Explicit Signature list to fix the wallet error
        var transaction = new Transaction();
        transaction.RecentBlockHash = blockHash.Result.Value.Blockhash;
        transaction.FeePayer = buyerKey;
        transaction.Instructions = instructions;
        
        // [CRITICAL FIX] Initialize signatures list manually to prevent "Object reference" crash
        transaction.Signatures = new List<SignaturePubKeyPair>();
        transaction.Signatures.Add(new SignaturePubKeyPair { PublicKey = buyerKey, Signature = new byte[64] });

        // 5. SIGN & BROADCAST LOOP
        string signedTxBase64 = null;

        try
        {
            // A: Editor
            if (WalletConnector.PlayerAccount != null)
            {
                transaction.Sign(WalletConnector.PlayerAccount);
                signedTxBase64 = Convert.ToBase64String(transaction.Serialize());
            }
            // B: Android (AppKit)
            else if (AppKit.IsInitialized)
            {
                byte[] txBytes = transaction.Serialize(); // This format works for Phantom/Jupiter
                string unsigTxBase64 = Convert.ToBase64String(txBytes);

                Debug.Log("[Marketplace] Requesting AppKit Signature...");
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

        // 6. CONFIRMATION LOOP (Fixes "Success but no deduction")
        if (!string.IsNullOrEmpty(signedTxBase64))
        {
            ShowPopup("Confirming", "Waiting for network...", Color.yellow);
            
            // Extract signature for tracking
            string signature = GetSignatureFromTx(signedTxBase64);
            Debug.Log($"[Marketplace] Tracking Signature: {signature}");

            // Loop for 30 seconds to ensure it lands
            float timeout = 30f;
            float startTime = Time.time;
            bool confirmed = false;

            while (Time.time - startTime < timeout && !confirmed)
            {
                // Spam the network (Safe on Solana)
                await Web3.Rpc.SendTransactionAsync(signedTxBase64, skipPreflight: true); // Skip simulation to avoid false failures
                
                await Task.Delay(1500); // Wait 1.5s

                // Check status
                var status = await Web3.Rpc.GetSignatureStatusesAsync(new List<string> { signature }, true);
                if (status.WasSuccessful && status.Result.Value[0] != null)
                {
                    var s = status.Result.Value[0];
                    if (s.ConfirmationStatus == "confirmed" || s.ConfirmationStatus == "finalized")
                    {
                        if (s.Error == null) confirmed = true;
                        else 
                        {
                            ShowPopup("Failed", "Transaction failed on-chain.", Color.red);
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
                ShowPopup("Timeout", "Network congested. Check wallet.", Color.red);
            }
        }
    }

    private string GetSignatureFromTx(string base64)
    {
        try {
            byte[] b = Convert.FromBase64String(base64);
            // First byte is count, next 64 are signature
            if(b.Length > 65) {
                byte[] sig = new byte[64];
                Array.Copy(b, 1, sig, 0, 64);
                return Solana.Unity.Wallet.Utilities.Encoders.Base58.EncodeData(sig);
            }
        } catch {}
        return null;
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