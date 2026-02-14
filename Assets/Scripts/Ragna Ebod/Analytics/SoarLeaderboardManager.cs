using UnityEngine;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Programs;
using Solana.Unity.Soar.Program; // Generated Code Namespace

public class SoarLeaderboardManager : MonoBehaviour
{
    [Header("Soar Configuration")]
    public string gameAddress;        // Public Key of your Game (from Magicblock/CLI)
    public string leaderboardAddress; // Public Key of the specific Leaderboard
    
    [Header("UI Feedback")]
    public NotificationPopup notificationPopup;

    // Fixed Priority Fee (0.00005 SOL) to ensure transactions land
    private const long PRIORITY_FEE_LAMPORTS = 50000;

    /// <summary>
    /// Submits a score. Handles Player Registration automatically if needed.
    /// </summary>
    public async void SubmitScore(long score)
    {
        if (WalletConnector.UserPublicKey == null)
        {
            ShowPopup("Error", "Connect wallet first!", Color.red);
            return;
        }

        ShowPopup("Leaderboard", "Preparing score...", Color.yellow);

        try
        {
            PublicKey userKey = WalletConnector.UserPublicKey;
            PublicKey gameKey = new PublicKey(gameAddress);
            PublicKey leaderboardKey = new PublicKey(leaderboardAddress);

            // 1. Check/Register Player Account (PDA)
            // Soar requires a specific PDA for the player to exist before they can submit scores.
            bool isRegistered = await CheckIfPlayerRegistered(userKey, gameKey);
            
            var instructions = new List<TransactionInstruction>();
            instructions.Add(ComputeBudgetProgram.SetComputeUnitLimit(300_000));
            instructions.Add(ComputeBudgetProgram.SetComputeUnitPrice(PRIORITY_FEE_LAMPORTS));

            if (!isRegistered)
            {
                Debug.Log("[Soar] Registering new player...");
                var registerIx = SoarProgram.RegisterPlayer(
                    new RegisterPlayerAccounts
                    {
                        Payer = userKey,
                        User = userKey,
                        PlayerAccount = FindPlayerPda(userKey),
                        Game = gameKey,
                        SystemProgram = SystemProgram.ProgramIdKey
                    },
                    SoarProgram.ProgramIdKey
                );
                instructions.Add(registerIx);
            }

            // 2. Submit Score Instruction
            // We must derive the PDAs required by Soar
            var submitAccounts = new SubmitScoreAccounts
            {
                Payer = userKey,
                Authority = userKey,
                PlayerAccount = FindPlayerPda(userKey),
                Game = gameKey,
                Leaderboard = leaderboardKey,
                PlayerScores = FindPlayerScoresPda(userKey, leaderboardKey),
                TopEntries = FindTopEntriesPda(leaderboardKey),
                SystemProgram = SystemProgram.ProgramIdKey
            };

            var submitIx = SoarProgram.SubmitScore(
                submitAccounts, 
                (ulong)score, 
                SoarProgram.ProgramIdKey
            );
            instructions.Add(submitIx);

            // 3. Build & Send
            await BuildAndSendTransaction(instructions);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Soar] Error: {ex.Message}");
            ShowPopup("Failed", "Could not submit score", Color.red);
        }
    }

    #region INTERNAL HELPERS

    private async Task<bool> CheckIfPlayerRegistered(PublicKey user, PublicKey game)
    {
        PublicKey playerPda = FindPlayerPda(user);
        var res = await Web3.Rpc.GetAccountInfoAsync(playerPda);
        return res.WasSuccessful && res.Result.Value != null;
    }

    // PDA Derivations (Specific to Soar Program)
    private PublicKey FindPlayerPda(PublicKey user)
    {
        // Seeds: [b"player", user_pubkey, soar_program_id]
        PublicKey.TryFindProgramAddress(
            new[] { Encoding.UTF8.GetBytes("player"), user.KeyBytes },
            SoarProgram.ProgramIdKey, out var pda, out var _
        );
        return pda;
    }

    private PublicKey FindPlayerScoresPda(PublicKey user, PublicKey leaderboard)
    {
        // Seeds: [b"player_scores", user_pubkey, leaderboard_pubkey, soar_program_id]
        PublicKey.TryFindProgramAddress(
            new[] { Encoding.UTF8.GetBytes("player_scores"), user.KeyBytes, leaderboard.KeyBytes },
            SoarProgram.ProgramIdKey, out var pda, out var _
        );
        return pda;
    }

    private PublicKey FindTopEntriesPda(PublicKey leaderboard)
    {
        // Seeds: [b"top_entries", leaderboard_pubkey, soar_program_id]
        PublicKey.TryFindProgramAddress(
            new[] { Encoding.UTF8.GetBytes("top_entries"), leaderboard.KeyBytes },
            SoarProgram.ProgramIdKey, out var pda, out var _
        );
        return pda;
    }

    #endregion

    #region ROBUST TRANSACTION HANDLING (Your Proven Logic)

    private async Task BuildAndSendTransaction(List<TransactionInstruction> instructions)
    {
        // Get Blockhash
        var blockHash = await Web3.Rpc.GetLatestBlockHashAsync(Commitment.Finalized);
        if (!blockHash.WasSuccessful)
        {
            ShowPopup("Error", "Network error", Color.red);
            return;
        }

        // Create Transaction
        var transaction = new Transaction
        {
            RecentBlockHash = blockHash.Result.Value.Blockhash,
            FeePayer = WalletConnector.UserPublicKey,
            Instructions = instructions
        };

        // SIGN AND SEND (The "Magicblock" Way)
        try
        {
            // PATH A: EDITOR (Use Sticky Wallet + Manual Binary Signing)
            if (WalletConnector.PlayerAccount != null)
            {
                byte[] msgBytes = transaction.CompileMessage();
                byte[] signature = WalletConnector.PlayerAccount.Sign(msgBytes);

                if (transaction.Signatures == null) transaction.Signatures = new List<SignaturePubKeyPair>();
                transaction.Signatures.Insert(0, new SignaturePubKeyPair { PublicKey = WalletConnector.UserPublicKey, Signature = signature });

                ShowPopup("Processing", "Submitting Score...", Color.yellow);
                var res = await Web3.Rpc.SendTransactionAsync(transaction.Serialize());
                
                if (res.WasSuccessful) ShowPopup("Success", "Score on-chain!", Color.green);
                else Debug.LogError(res.Reason);
            }
            // PATH B: MOBILE (MWA)
            else
            {
                ShowPopup("Wallet", "Sign score submission...", Color.yellow);
                var res = await Web3.Wallet.SignAndSendTransaction(transaction);
                
                if (res.WasSuccessful) ShowPopup("Success", "Score on-chain!", Color.green);
                else ShowPopup("Failed", "User cancelled", Color.red);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Soar] Transaction Failed: {ex.Message}");
            ShowPopup("Error", "Transaction Failed", Color.red);
        }
    }

    private void ShowPopup(string title, string message, Color color)
    {
        if (notificationPopup != null) notificationPopup.Show(title, message, color);
        else Debug.Log($"[{title}] {message}");
    }

    #endregion
}