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
public class SoarDiagnostic : MonoBehaviour
{
    [Header("Game Address")]
    public string gameAddress = "HYVQLiE2aGccFSFQ1ZjLxPSmtRQHzJbFYawQvhNU3oKS";

    [MenuItem("Magicblock/Diagnose PDA Issues")]
    public static async void DiagnosePDAIssues()
    {
        var instance = FindObjectOfType<SoarDiagnostic>();
        if (instance == null)
        {
            Debug.LogError("❌ Please add SoarDiagnostic component to a GameObject in your scene!");
            return;
        }

        var account = WalletConnector.PlayerAccount;
        if (account == null)
        {
            Debug.LogError("❌ No wallet connected!");
            return;
        }

        try
        {
            var gamePublicKey = new PublicKey(instance.gameAddress);
            var soarClient = new SoarClient(Web3.Rpc, Web3.WsRpc);
            
            Debug.Log($"<color=cyan>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
            Debug.Log($"<color=cyan>🔍 SOAR PDA DIAGNOSTIC</color>");
            Debug.Log($"<color=cyan>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
            
            // Fetch game account
            var gameAccount = (await soarClient.GetGameAsync(gamePublicKey)).ParsedResult;
            
            if (gameAccount == null)
            {
                Debug.LogError("❌ Failed to fetch game account!");
                return;
            }

            var leaderboardId = gameAccount.LeaderboardCount + 1;
            
            Debug.Log($"\n<b>📊 GAME STATE:</b>");
            Debug.Log($"Game: {gamePublicKey}");
            Debug.Log($"Current Leaderboard Count: {gameAccount.LeaderboardCount}");
            Debug.Log($"Next Leaderboard ID: {leaderboardId}");
            
            // Derive PDAs
            var leaderboard = SoarPda.LeaderboardPda(gamePublicKey, leaderboardId);
            var topEntries = SoarPda.LeaderboardTopEntriesPda(leaderboard);
            
            Debug.Log($"\n<b>🔑 DERIVED PDAs:</b>");
            Debug.Log($"Leaderboard PDA: {leaderboard}");
            Debug.Log($"TopEntries PDA: {topEntries}");
            
            // Check if leaderboard account already exists
            Debug.Log($"\n<b>🔍 CHECKING LEADERBOARD ACCOUNT:</b>");
            var leaderboardAccountInfo = await Web3.Rpc.GetAccountInfoAsync(leaderboard, Commitment.Confirmed);
            
            if (leaderboardAccountInfo.WasSuccessful && leaderboardAccountInfo.Result?.Value != null)
            {
                Debug.LogWarning($"⚠️ LEADERBOARD ACCOUNT ALREADY EXISTS!");
                Debug.LogWarning($"This might be causing the 0x7d3 error!");
                Debug.LogWarning($"Owner: {leaderboardAccountInfo.Result.Value.Owner}");
                Debug.LogWarning($"Data Length: {leaderboardAccountInfo.Result.Value.Data.Count} bytes");
                Debug.LogWarning($"\n<b>SOLUTION:</b> The leaderboard already exists. You may need to:");
                Debug.LogWarning($"1. Use a different leaderboard ID");
                Debug.LogWarning($"2. Close the existing leaderboard first");
                Debug.LogWarning($"3. This game already has this leaderboard");
            }
            else
            {
                Debug.Log($"✅ Leaderboard account does NOT exist (good - can be created)");
            }
            
            // Check TopEntries account
            Debug.Log($"\n<b>🔍 CHECKING TOPENTRIES ACCOUNT:</b>");
            var topEntriesAccountInfo = await Web3.Rpc.GetAccountInfoAsync(topEntries, Commitment.Confirmed);
            
            if (topEntriesAccountInfo.WasSuccessful && topEntriesAccountInfo.Result?.Value != null)
            {
                Debug.LogWarning($"⚠️ TOP ENTRIES ACCOUNT ALREADY EXISTS!");
                Debug.LogWarning($"Owner: {topEntriesAccountInfo.Result.Value.Owner}");
                Debug.LogWarning($"Data Length: {topEntriesAccountInfo.Result.Value.Data.Count} bytes");
            }
            else
            {
                Debug.Log($"✅ TopEntries account does NOT exist (good - can be created)");
            }
            
            // Verify manual PDA derivation matches SDK
            Debug.Log($"\n<b>🔍 VERIFYING PDA DERIVATION:</b>");
            
            // Manual derivation for leaderboard
            PublicKey.TryFindProgramAddress(
                new[]
                {
                    System.Text.Encoding.UTF8.GetBytes("leaderboard"),
                    gamePublicKey.KeyBytes,
                    BitConverter.GetBytes((ulong)leaderboardId)
                },
                SoarProgram.ProgramIdKey,
                out var manualLeaderboard,
                out var leaderboardBump
            );
            
            var leaderboardMatch = manualLeaderboard.Equals(leaderboard);
            Debug.Log($"Manual Leaderboard PDA: {manualLeaderboard} (Bump: {leaderboardBump})");
            Debug.Log($"SDK Leaderboard PDA:    {leaderboard}");
            Debug.Log($"PDAs Match: {(leaderboardMatch ? "✅ YES" : "❌ NO - PROBLEM!")}");
            
            if (!leaderboardMatch)
            {
                Debug.LogError($"<b>❌ CRITICAL:</b> SDK PDA derivation doesn't match manual derivation!");
                Debug.LogError($"This indicates a problem with the SDK's PDA helper methods.");
            }
            
            // Manual derivation for top entries
            PublicKey.TryFindProgramAddress(
                new[]
                {
                    System.Text.Encoding.UTF8.GetBytes("top-scores"),
                    leaderboard.KeyBytes
                },
                SoarProgram.ProgramIdKey,
                out var manualTopEntries,
                out var topEntriesBump
            );
            
            var topEntriesMatch = manualTopEntries.Equals(topEntries);
            Debug.Log($"\nManual TopEntries PDA: {manualTopEntries} (Bump: {topEntriesBump})");
            Debug.Log($"SDK TopEntries PDA:    {topEntries}");
            Debug.Log($"PDAs Match: {(topEntriesMatch ? "✅ YES" : "❌ NO - PROBLEM!")}");
            
            if (!topEntriesMatch)
            {
                Debug.LogError($"<b>❌ CRITICAL:</b> TopEntries PDA derivation doesn't match!");
            }
            
            // Check wallet SOL balance
            Debug.Log($"\n<b>💰 WALLET BALANCE:</b>");
            var balance = await Web3.Rpc.GetBalanceAsync(account.PublicKey, Commitment.Confirmed);
            if (balance.WasSuccessful)
            {
                var solBalance = balance.Result.Value / 1_000_000_000.0;
                Debug.Log($"SOL Balance: {solBalance:F4} SOL");
                
                if (solBalance < 0.01)
                {
                    Debug.LogWarning($"⚠️ Low SOL balance! You may not have enough for transaction fees.");
                    Debug.LogWarning($"Recommended: At least 0.01 SOL");
                }
                else
                {
                    Debug.Log($"✅ Sufficient SOL for transaction");
                }
            }
            
            Debug.Log($"\n<color=cyan>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
            Debug.Log($"<color=cyan>📋 DIAGNOSTIC COMPLETE</color>");
            Debug.Log($"<color=cyan>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>❌ Diagnostic Error:</color>");
            Debug.LogError($"Message: {e.Message}");
            Debug.LogError($"Type: {e.GetType().Name}");
            Debug.LogError($"\nStack Trace:\n{e.StackTrace}");
        }
    }
}
#endif