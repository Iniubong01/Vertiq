using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using System.Collections.Generic;
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
public class SoarSetupTool : MonoBehaviour
{
    [MenuItem("Magicblock/Create Leaderboard")]
    public static async void CreateLeaderboard()
    {
        var account = WalletConnector.PlayerAccount;

        if (account == null)
        {
            Debug.LogError("❌ No wallet connected! Please start Play Mode and connect wallet first.");
            return;
        }

        Debug.Log($"[Setup] ✅ Using Wallet: {account.PublicKey}");

        // Check Soar program
        Debug.Log("[Setup] 🔍 Checking if Soar program is deployed on this network...");
        var programCheck = await Web3.Rpc.GetAccountInfoAsync(SoarProgram.ProgramIdKey.ToString());
        
        if (!programCheck.WasSuccessful || programCheck.Result.Value == null)
        {
            Debug.LogError($"<color=red>❌ SOAR PROGRAM NOT FOUND!</color>");
            Debug.LogError($"Program ID: {SoarProgram.ProgramIdKey}");
            return;
        }
        
        Debug.Log($"[Setup] ✅ Soar program found: {SoarProgram.ProgramIdKey}");

        var gameKeypair = new Account();
        var leaderboardKeypair = new Account();

        Debug.Log($"[Setup] 🎮 Game Address: {gameKeypair.PublicKey}");
        Debug.Log($"[Setup] 🏆 Leaderboard Address: {leaderboardKeypair.PublicKey}");

        try
        {
            // Calculate TopEntries PDA
            PublicKey.TryFindProgramAddress(
                new[]
                {
                    System.Text.Encoding.UTF8.GetBytes("top-scores"),
                    leaderboardKeypair.PublicKey.KeyBytes
                },
                SoarProgram.ProgramIdKey,
                out var topEntriesPda,
                out var topEntriesBump
            );
            
            Debug.Log($"[Setup] 📊 TopEntries PDA: {topEntriesPda}");

            // STEP 1: Create Game (works fine)
            Debug.Log("\n[Setup] 📡 Creating game...");
            bool gameCreated = await CreateGame(account, gameKeypair);
            
            if (!gameCreated)
            {
                Debug.LogError("Game creation failed. Aborting.");
                return;
            }

            // Wait for confirmation
            await Task.Delay(3000);

            // STEP 2: Add Leaderboard using the working pattern
            Debug.Log("\n[Setup] 📡 Adding leaderboard...");
            await AddLeaderboardWithWalletPattern(
                account, 
                gameKeypair.PublicKey, 
                leaderboardKeypair, 
                topEntriesPda
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>❌ Error:</color>");
            Debug.LogError($"<b>Message:</b> {e.Message}");
            if (e.InnerException != null)
            {
                Debug.LogError($"<b>Inner Exception:</b> {e.InnerException.Message}");
            }
            Debug.LogError($"\n<b>Stack Trace:</b>\n{e.StackTrace}");
        }
    }

    private static async Task<bool> CreateGame(Account authority, Account gameKeypair)
    {
        try
        {
            var instructions = new List<TransactionInstruction>();
            
            instructions.Add(ComputeBudgetProgram.SetComputeUnitLimit(400_000));
            instructions.Add(ComputeBudgetProgram.SetComputeUnitPrice(100_000));
            
            var initGameAccounts = new InitializeGameAccounts
            {
                Creator = authority.PublicKey,
                Game = gameKeypair.PublicKey,
                SystemProgram = SystemProgram.ProgramIdKey
            };
            
            var gameAttributes = new GameAttributes
            {
                Title = "Vortiq",
                Description = "Arcade Shooter Game",
                Genre = 0,
                GameType = 0,
                NftMeta = PublicKey.DefaultPublicKey
            };
            
            var gameAuth = new PublicKey[] { authority.PublicKey };
            
            var initGameIx = SoarProgram.InitializeGame(
                initGameAccounts,
                gameAttributes,
                gameAuth,
                SoarProgram.ProgramIdKey
            );
            instructions.Add(initGameIx);
            
            var blockHash = await Web3.Rpc.GetLatestBlockHashAsync(Commitment.Finalized);
            if (!blockHash.WasSuccessful)
            {
                Debug.LogError($"❌ Failed to get blockhash: {blockHash.Reason}");
                return false;
            }
            
            var transaction = new Transaction
            {
                RecentBlockHash = blockHash.Result.Value.Blockhash,
                FeePayer = authority.PublicKey,
                Instructions = instructions
            };

            transaction.Sign(authority);
            transaction.Sign(gameKeypair);
            
            var res = await Web3.Rpc.SendTransactionAsync(
                transaction.Serialize(),
                skipPreflight: false,
                commitment: Commitment.Confirmed
            );
            
            if (res.WasSuccessful)
            {
                Debug.Log($"<color=green>✅ GAME CREATED!</color>");
                Debug.Log($"Game: {gameKeypair.PublicKey}");
                Debug.Log($"TX: {res.Result}");
                return true;
            }
            else
            {
                Debug.LogError($"<color=red>❌ Game Creation Failed: {res.Reason}</color>");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Game creation error: {e.Message}");
            return false;
        }
    }

    private static async Task AddLeaderboardWithWalletPattern(
        Account authority, 
        PublicKey gameAddress, 
        Account leaderboardKeypair,
        PublicKey topEntriesPda)
    {
        try
        {
            // Get the wallet instance (same pattern as MarketplacePurchase)
            var wallet = Web3.Wallet;
            if (wallet == null || wallet.Account == null)
            {
                Debug.LogError("❌ Wallet not available");
                return;
            }

            var instructions = new List<TransactionInstruction>();
            
            instructions.Add(ComputeBudgetProgram.SetComputeUnitLimit(400_000));
            instructions.Add(ComputeBudgetProgram.SetComputeUnitPrice(100_000));
            
            var addLbAccounts = new AddLeaderboardAccounts
            {
                Authority = authority.PublicKey,
                Payer = authority.PublicKey,
                Game = gameAddress,
                Leaderboard = leaderboardKeypair.PublicKey,
                TopEntries = topEntriesPda,
                SystemProgram = SystemProgram.ProgramIdKey
            };
            
            var leaderboardInput = new RegisterLeaderBoardInput
            {
                Description = "High Scores - Season 1",
                NftMeta = PublicKey.DefaultPublicKey
            };
            
            var addLbIx = SoarProgram.AddLeaderboard(
                addLbAccounts,
                leaderboardInput,
                SoarProgram.ProgramIdKey
            );
            
            instructions.Add(addLbIx);
            
            var blockHash = await Web3.Rpc.GetLatestBlockHashAsync(Commitment.Finalized);
            if (!blockHash.WasSuccessful)
            {
                Debug.LogError($"❌ Failed to get blockhash: {blockHash.Reason}");
                return;
            }
            
            // Create transaction using the SAME pattern as MarketplacePurchase
            var transaction = new Transaction
            {
                RecentBlockHash = blockHash.Result.Value.Blockhash,
                FeePayer = authority.PublicKey,
                Instructions = instructions
            };

            // CRITICAL: Sign with BOTH accounts before sending
            // The wallet will handle the final signing
            transaction.PartialSign(new[] { authority, leaderboardKeypair });
            
            Debug.Log("[Setup] 🔐 Signing and sending with wallet adapter...");
            
            // Use the same method as your working scripts
            var result = await wallet.SignAndSendTransaction(transaction);

            if (result.WasSuccessful)
            {
                Debug.Log($"<color=green>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.Log($"<color=green>✅ SUCCESS! Leaderboard Created!</color>");
                Debug.Log($"<color=green>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.Log($"\n<b>🎮 GAME ADDRESS:</b>");
                Debug.Log($"{gameAddress}");
                Debug.Log($"\n<b>🏆 LEADERBOARD ADDRESS:</b>");
                Debug.Log($"{leaderboardKeypair.PublicKey}");
                Debug.Log($"\n<b>📊 TOP ENTRIES PDA:</b>");
                Debug.Log($"{topEntriesPda}");
                Debug.Log($"\n<b>📝 TRANSACTION SIGNATURE:</b>");
                Debug.Log($"{result.Result}");
                Debug.Log($"\n<b>🔗 View on Solscan:</b>");
                Debug.Log($"https://solscan.io/tx/{result.Result}");
                Debug.Log($"\n<color=yellow>⬇️ COPY GAME & LEADERBOARD ADDRESSES TO YOUR INSPECTOR ⬇️</color>\n");
            }
            else
            {
                Debug.LogError($"<color=red>❌ Leaderboard Failed: {result.Reason}</color>");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>❌ Leaderboard Error:</color>");
            Debug.LogError($"Message: {e.Message}");
            if (e.InnerException != null)
            {
                Debug.LogError($"Inner: {e.InnerException.Message}");
            }
        }
    }
}
#endif