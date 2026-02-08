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
public class SoarAddLeaderboard_ManualPDA : MonoBehaviour
{
    [Header("Paste your existing Game Address here")]
    public string existingGameAddress = "HYVQLiE2aGccFSFQ1ZjLxPSmtRQHzJbFYawQvhNU3oKS";

    [MenuItem("Magicblock/Add Leaderboard (Manual PDA)")]
    public static async void AddLeaderboardManualPDA()
    {
        var instance = FindObjectOfType<SoarAddLeaderboard_ManualPDA>();
        if (instance == null)
        {
            Debug.LogError("❌ Please add SoarAddLeaderboard_ManualPDA component to a GameObject in your scene!");
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
            
            // Fetch game account
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
            
            // ✅ CRITICAL FIX: Manually derive PDAs with proper byte ordering
            // The issue might be in how the SDK constructs the PDA seeds
            
            // Convert leaderboard ID to little-endian bytes (Solana standard)
            byte[] idBytes = BitConverter.GetBytes((ulong)leaderboardId);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(idBytes);
            }
            
            Debug.Log($"[Setup] 🔍 Leaderboard ID bytes (LE): {BitConverter.ToString(idBytes)}");
            
            // Derive leaderboard PDA manually
            PublicKey.TryFindProgramAddress(
                new[]
                {
                    System.Text.Encoding.UTF8.GetBytes("leaderboard"),
                    gamePublicKey.KeyBytes,
                    idBytes
                },
                SoarProgram.ProgramIdKey,
                out var leaderboard,
                out var leaderboardBump
            );
            
            Debug.Log($"[Setup] 🏆 Leaderboard PDA: {leaderboard} (Bump: {leaderboardBump})");
            
            // Compare with SDK method
            var sdkLeaderboard = SoarPda.LeaderboardPda(gamePublicKey, leaderboardId);
            if (!sdkLeaderboard.Equals(leaderboard))
            {
                Debug.LogWarning($"⚠️ SDK PDA ({sdkLeaderboard}) differs from manual ({leaderboard})");
                Debug.LogWarning($"Using manual PDA derivation");
            }
            else
            {
                Debug.Log($"✅ SDK and manual PDAs match");
            }
            
            // Derive TopEntries PDA
            PublicKey.TryFindProgramAddress(
                new[]
                {
                    System.Text.Encoding.UTF8.GetBytes("top-scores"),
                    leaderboard.KeyBytes
                },
                SoarProgram.ProgramIdKey,
                out var topEntries,
                out var topEntriesBump
            );
            
            Debug.Log($"[Setup] 📊 TopEntries PDA: {topEntries} (Bump: {topEntriesBump})");

            var instructions = new List<TransactionInstruction>();
            
            // Add compute budget - increased for safety
            instructions.Add(ComputeBudgetProgram.SetComputeUnitLimit(400_000));
            instructions.Add(ComputeBudgetProgram.SetComputeUnitPrice(100_000));
            
            // Create AddLeaderboard instruction using SDK
            var addLbAccounts = new AddLeaderboardAccounts
            {
                Authority = account.PublicKey,
                Payer = account.PublicKey,
                Game = gamePublicKey,
                Leaderboard = leaderboard,  // Using our manually derived PDA
                TopEntries = topEntries,
                SystemProgram = SystemProgram.ProgramIdKey
            };
            
            var leaderboardInput = new RegisterLeaderBoardInput
            {
                Description = "High Scores - Season 1",
                NftMeta = PublicKey.DefaultPublicKey
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

            // Sign with only the payer account
            Debug.Log("[Setup] 🔐 Signing transaction...");
            transaction.Sign(account);
            
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
                
                Debug.LogError("\n<b>⚠️ Still failing with error 0x7d3?</b>");
                Debug.LogError("This suggests the issue may be:");
                Debug.LogError("1. A version mismatch between SDK and on-chain program");
                Debug.LogError("2. The game was created with a different/incompatible program version");
                Debug.LogError("3. There's a custom constraint in the program we're not aware of");
                Debug.LogError("\n<b>Next steps:</b>");
                Debug.LogError("- Run 'Magicblock/Diagnose PDA Issues' to check if accounts already exist");
                Debug.LogError("- Try creating a new game on devnet for testing");
                Debug.LogError("- Contact Soar/MagicBlock support with your game address");
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