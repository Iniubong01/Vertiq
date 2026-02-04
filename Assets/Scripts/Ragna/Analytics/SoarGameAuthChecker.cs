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
public class SoarGameAuthChecker : MonoBehaviour
{
    [Header("Paste your existing Game Address here")]
    public string existingGameAddress = "HYVQLiE2aGccFSFQ1ZjLxPSmtRQHzJbFYawQvhNU3oKS";

    [MenuItem("Magicblock/Check Game Authority")]
    public static async void CheckGameAuthority()
    {
        var instance = FindObjectOfType<SoarGameAuthChecker>();
        if (instance == null)
        {
            Debug.LogError("❌ Please add SoarGameAuthChecker component to a GameObject in your scene!");
            return;
        }

        var account = WalletConnector.PlayerAccount;
        if (account == null)
        {
            Debug.LogError("❌ No wallet connected! Please start Play Mode and connect wallet first.");
            return;
        }

        Debug.Log($"[Check] 🔍 Checking authority for wallet: {account.PublicKey}");
        Debug.Log($"[Check] 🎮 Game Address: {instance.existingGameAddress}");

        try
        {
            var gamePublicKey = new PublicKey(instance.existingGameAddress);
            var soarClient = new SoarClient(Web3.Rpc, Web3.WsRpc);
            
            Debug.Log("[Check] 📡 Fetching game account...");
            var gameAccount = (await soarClient.GetGameAsync(gamePublicKey)).ParsedResult;
            
            if (gameAccount == null)
            {
                Debug.LogError("❌ Failed to fetch game account. The game address may be incorrect!");
                return;
            }
            
            Debug.Log($"<color=cyan>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
            Debug.Log($"<color=cyan>📋 GAME INFORMATION</color>");
            Debug.Log($"<color=cyan>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
            Debug.Log($"Title: {gameAccount.Meta.Title}");
            Debug.Log($"Description: {gameAccount.Meta.Description}");
            Debug.Log($"Leaderboard Count: {gameAccount.LeaderboardCount}");
            Debug.Log($"Achievement Count: {gameAccount.AchievementCount}");
            
            Debug.Log($"\n<color=yellow>👥 AUTHORIZED ACCOUNTS:</color>");
            if (gameAccount.Auth != null && gameAccount.Auth.Length > 0)
            {
                foreach (var auth in gameAccount.Auth)
                {
                    var isYou = auth.Equals(account.PublicKey);
                    var marker = isYou ? " <color=green>← YOU!</color>" : "";
                    Debug.Log($"  • {auth}{marker}");
                }
            }
            else
            {
                Debug.Log("  No authorities found!");
            }
            
            // Check if current wallet is authorized
            var isAuthorized = gameAccount.Auth != null && gameAccount.Auth.Any(auth => auth.Equals(account.PublicKey));
            
            Debug.Log($"\n<color=cyan>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
            if (isAuthorized)
            {
                Debug.Log($"<color=green>✅ SUCCESS! You ARE authorized to add leaderboards!</color>");
                Debug.Log($"<color=green>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.Log($"\n<b>Next Steps:</b>");
                Debug.Log($"Use the 'Magicblock/Add Leaderboard to Existing Game' menu item.");
            }
            else
            {
                Debug.LogError($"<color=red>❌ AUTHORITY CHECK FAILED!</color>");
                Debug.LogError($"<color=red>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.LogError($"\n<b>Problem:</b>");
                Debug.LogError($"Your wallet ({account.PublicKey}) is NOT in the authorized accounts list.");
                Debug.LogError($"\n<b>This is why you're getting error 0x7d3 (ConstraintRaw)!</b>");
                Debug.LogError($"\n<b>Solutions:</b>");
                Debug.LogError($"1. Create a NEW game with your wallet (use 'Magicblock/Create New Game'");
                Debug.LogError($"2. Ask the game creator to add your wallet as an authority");
                Debug.LogError($"3. Use a different wallet that IS authorized");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>❌ Error checking game authority:</color>");
            Debug.LogError($"<b>Message:</b> {e.Message}");
            Debug.LogError($"<b>Type:</b> {e.GetType().Name}");
            
            if (e.InnerException != null)
            {
                Debug.LogError($"<b>Inner Exception:</b> {e.InnerException.Message}");
            }
            
            Debug.LogError($"\n<b>Stack Trace:</b>\n{e.StackTrace}");
        }
    }
    
    [MenuItem("Magicblock/Create New Game")]
    public static async void CreateNewGame()
    {
        var instance = FindObjectOfType<SoarGameAuthChecker>();
        if (instance == null)
        {
            Debug.LogError("❌ Please add SoarGameAuthChecker component to a GameObject in your scene!");
            return;
        }

        var account = WalletConnector.PlayerAccount;
        if (account == null)
        {
            Debug.LogError("❌ No wallet connected! Please start Play Mode and connect wallet first.");
            return;
        }

        Debug.Log($"[Create] 🎮 Creating new game with wallet: {account.PublicKey}");

        try
        {
            // Generate a new game keypair
            var game = new Account();
            Debug.Log($"[Create] 🆕 New Game Address: {game.PublicKey}");
            
            // Create game metadata
            var gameMeta = new GameAttributes
            {
                Title = "My Unity Game",
                Description = "Created from Unity Editor",
                Genre = 0, // Adjust as needed
                GameType = 0, // Adjust as needed
                NftMeta = PublicKey.DefaultPublicKey // or your NFT mint
            };
            
            // Create the InitializeGame instruction
            var initGameAccounts = new InitializeGameAccounts
            {
                Creator = account.PublicKey,
                Game = game.PublicKey,
                SystemProgram = SystemProgram.ProgramIdKey
            };
            
            Debug.Log("[Create] 📝 Building transaction...");
            
            var initGameIx = SoarProgram.InitializeGame(
                initGameAccounts,
                gameMeta,
                new[] { account.PublicKey }, // Your wallet as the only authority
                SoarProgram.ProgramIdKey
            );
            
            // Get blockhash
            var blockHash = await Web3.Rpc.GetLatestBlockHashAsync(Commitment.Finalized);
            if (!blockHash.WasSuccessful)
            {
                Debug.LogError($"❌ Failed to get blockhash: {blockHash.Reason}");
                return;
            }
            
            // Build and sign transaction
            var transaction = new Transaction
            {
                RecentBlockHash = blockHash.Result.Value.Blockhash,
                FeePayer = account.PublicKey,
                Instructions = new List<TransactionInstruction> { initGameIx }
            };
            
            Debug.Log("[Create] 🔐 Signing transaction...");
            transaction.Sign(account);
            transaction.Sign(game);
            
            Debug.Log("[Create] 📡 Sending transaction...");
            var result = await Web3.Rpc.SendTransactionAsync(
                transaction.Serialize(),
                skipPreflight: false,
                commitment: Commitment.Confirmed
            );

            if (result.WasSuccessful)
            {
                Debug.Log($"<color=green>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.Log($"<color=green>✅ SUCCESS! Game Created!</color>");
                Debug.Log($"<color=green>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.Log($"\n<b>🎮 GAME ADDRESS:</b>");
                Debug.Log($"<color=yellow>{game.PublicKey}</color>");
                Debug.Log($"\n<b>👤 AUTHORITY:</b>");
                Debug.Log($"{account.PublicKey} (YOU)");
                Debug.Log($"\n<b>📝 TRANSACTION:</b>");
                Debug.Log($"{result.Result}");
                Debug.Log($"\n<b>🔗 View on Solscan:</b>");
                Debug.Log($"https://solscan.io/tx/{result.Result}");
                Debug.Log($"\n<color=cyan>⬇️ COPY THIS GAME ADDRESS ⬇️</color>");
                Debug.Log($"<color=cyan>{game.PublicKey}</color>");
                Debug.Log($"\n<color=green>Now you can add leaderboards to this game!</color>");
            }
            else
            {
                Debug.LogError($"<color=red>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.LogError($"<color=red>❌ Transaction Failed!</color>");
                Debug.LogError($"<color=red>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.LogError($"Reason: {result.Reason}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>❌ Error creating game:</color>");
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