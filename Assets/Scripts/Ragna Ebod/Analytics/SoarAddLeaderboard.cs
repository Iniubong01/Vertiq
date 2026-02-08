using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Programs;
using Solana.Unity.Soar;
using Solana.Unity.Soar.Program;
using Solana.Unity.Soar.Types;
using System;

#if UNITY_EDITOR
public class SoarAddLeaderboard : MonoBehaviour
{
    [Header("Paste your existing Game Address here")]
    public string existingGameAddress = "HYVQLiE2aGccFSFQ1ZjLxPSmtRQHzJbFYawQvhNU3oKS";

    [MenuItem("Magicblock/Add Leaderboard to Existing Game")]
    public static async void AddLeaderboardToExistingGame()
    {
        var instance = FindObjectOfType<SoarAddLeaderboard>();
        if (instance == null)
        {
            Debug.LogError("❌ Please add SoarAddLeaderboard component to a GameObject in your scene!");
            return;
        }

        if (string.IsNullOrEmpty(instance.existingGameAddress))
        {
            Debug.LogError("❌ Please set the existing Game Address in the Inspector!");
            return;
        }

        var account = WalletConnector.PlayerAccount;
        if (account == null)
        {
            Debug.LogError("❌ No wallet connected! Please start Play Mode and connect wallet first.");
            return;
        }

        Debug.Log($"[Setup] ✅ Using Wallet: {account.PublicKey}");
        Debug.Log($"[Setup] 🎮 Using Existing Game: {instance.existingGameAddress}");

        try
        {
            var gamePublicKey = new PublicKey(instance.existingGameAddress);
            
            // ✅ CRITICAL FIX: Get the game account to determine the next leaderboard ID
            Debug.Log("[Setup] 📡 Fetching game account...");
            var soarClient = new SoarClient(Web3.Rpc, Web3.WsRpc);
            var gameAccount = (await soarClient.GetGameAsync(gamePublicKey)).ParsedResult;
            
            if (gameAccount == null)
            {
                Debug.LogError("❌ Failed to fetch game account. Make sure the game address is correct!");
                return;
            }
            
            // Calculate the next leaderboard ID
            var leaderboardId = gameAccount.LeaderboardCount + 1;
            Debug.Log($"[Setup] 🔢 Next Leaderboard ID: {leaderboardId}");
            
            // ✅ CRITICAL FIX: Derive the leaderboard PDA instead of creating a keypair
            var leaderboard = SoarPda.LeaderboardPda(gamePublicKey, leaderboardId);
            Debug.Log($"[Setup] 🏆 Leaderboard PDA: {leaderboard}");
            
            // Calculate TopEntries PDA
            var topEntries = SoarPda.LeaderboardTopEntriesPda(leaderboard);
            Debug.Log($"[Setup] 📊 TopEntries PDA: {topEntries}");

            var instructions = new List<TransactionInstruction>();
            
            // Add compute budget
            instructions.Add(ComputeBudgetProgram.SetComputeUnitLimit(400_000));
            instructions.Add(ComputeBudgetProgram.SetComputeUnitPrice(100_000));
            
            // Create AddLeaderboard instruction using SDK
            var addLbAccounts = new AddLeaderboardAccounts
            {
                Authority = account.PublicKey,
                Payer = account.PublicKey,
                Game = gamePublicKey,
                Leaderboard = leaderboard,  // ✅ This is a PDA, not a keypair
                TopEntries = topEntries,
                SystemProgram = SystemProgram.ProgramIdKey
            };
            
            var leaderboardInput = new RegisterLeaderBoardInput
            {
                Description = "High Scores - Season 1",
                NftMeta = PublicKey.DefaultPublicKey,
                // Optional: Add these fields if your RegisterLeaderBoardInput supports them
                // ScoresToRetain = 10,
                // IsAscending = false,
                // AllowMultipleScores = false
            };
            
            Debug.Log("[Setup] Creating AddLeaderboard instruction...");
            var addLbIx = SoarProgram.AddLeaderboard(
                addLbAccounts,
                leaderboardInput,
                SoarProgram.ProgramIdKey
            );
            
            // Log instruction details for debugging
            Debug.Log("[Setup] 🔍 Instruction account metadata:");
            for (int i = 0; i < addLbIx.Keys.Count; i++)
            {
                var key = addLbIx.Keys[i];
                Debug.Log($"  Key[{i}]: {key.PublicKey} (Writable: {key.IsWritable}, Signer: {key.IsSigner})");
            }
            
            instructions.Add(addLbIx);
            
            // Get blockhash
            Debug.Log("[Setup] 📡 Getting blockhash...");
            var blockHash = await Web3.Rpc.GetLatestBlockHashAsync(Commitment.Finalized);
            if (!blockHash.WasSuccessful)
            {
                Debug.LogError($"❌ Failed to get blockhash: {blockHash.Reason}");
                return;
            }
            
            // Build transaction
            var transaction = new Transaction
            {
                RecentBlockHash = blockHash.Result.Value.Blockhash,
                FeePayer = account.PublicKey,
                Instructions = instructions
            };

            // ✅ CRITICAL FIX: Only sign with the payer account, NOT the leaderboard
            Debug.Log("[Setup] 🔐 Signing transaction...");
            transaction.Sign(account);
            // ❌ DO NOT SIGN WITH LEADERBOARD - it's a PDA, not a keypair!
            // transaction.Sign(leaderboardKeypair); // REMOVED
            
            // Send transaction
            Debug.Log("[Setup] 📡 Sending transaction...");
            var serialized = transaction.Serialize();
            Debug.Log($"[Setup] Transaction size: {serialized.Length} bytes");
            
            var result = await Web3.Rpc.SendTransactionAsync(
                serialized,
                skipPreflight: false,
                commitment: Commitment.Confirmed
            );

            if (result.WasSuccessful)
            {
                Debug.Log($"<color=green>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.Log($"<color=green>✅ SUCCESS! Leaderboard Added!</color>");
                Debug.Log($"<color=green>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.Log($"\n<b>🎮 GAME ADDRESS:</b>");
                Debug.Log($"{instance.existingGameAddress}");
                Debug.Log($"\n<b>🔢 LEADERBOARD ID:</b>");
                Debug.Log($"{leaderboardId}");
                Debug.Log($"\n<b>🏆 LEADERBOARD ADDRESS (PDA):</b>");
                Debug.Log($"{leaderboard}");
                Debug.Log($"\n<b>📊 TOP ENTRIES PDA:</b>");
                Debug.Log($"{topEntries}");
                Debug.Log($"\n<b>📝 TRANSACTION SIGNATURE:</b>");
                Debug.Log($"{result.Result}");
                Debug.Log($"\n<b>🔗 View on Solscan:</b>");
                Debug.Log($"https://solscan.io/tx/{result.Result}");
                Debug.Log($"\n<color=yellow>⬇️ COPY THESE ADDRESSES TO YOUR INSPECTOR ⬇️</color>");
                Debug.Log($"<color=yellow>Game: {instance.existingGameAddress}</color>");
                Debug.Log($"<color=yellow>Leaderboard: {leaderboard}</color>");
                Debug.Log($"<color=yellow>Leaderboard ID: {leaderboardId}</color>\n");
            }
            else
            {
                Debug.LogError($"<color=red>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.LogError($"<color=red>❌ Transaction Failed!</color>");
                Debug.LogError($"<color=red>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.LogError($"Reason: {result.Reason}");
                
                Debug.LogError("\n<b>⚠️ Troubleshooting Tips:</b>");
                Debug.LogError("1. Verify your game address is correct");
                Debug.LogError("2. Ensure you have enough SOL for transaction fees");
                Debug.LogError("3. Check that you're the game authority");
                Debug.LogError("4. Try using devnet instead of mainnet for testing");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>❌ Error adding leaderboard:</color>");
            Debug.LogError($"<b>Message:</b> {e.Message}");
            Debug.LogError($"<b>Type:</b> {e.GetType().Name}");
            
            if (e.InnerException != null)
            {
                Debug.LogError($"<b>Inner Exception:</b> {e.InnerException.Message}");
            }
            
            Debug.LogError($"\n<b>Stack Trace:</b>\n{e.StackTrace}");
        }
    }
}
#endif