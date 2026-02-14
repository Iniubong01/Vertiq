using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Solana.Unity.Programs.Abstract;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using Solana.Unity.SDK; 
using Solana.Unity.Rpc.Core.Http;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

using Vortiq;
using Vortiq.Program;
using Vortiq.Errors;
using Vortiq.Accounts;

namespace Vortiq
{
    namespace Accounts
    {
        public partial class RandomnessState
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 3464876343636320930UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{162, 226, 24, 69, 173, 182, 21, 48};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "UFAuwWCgiXy";
            public byte[] Randomness { get; set; }
            public long Timestamp { get; set; }
            public bool Consumed { get; set; }

            public static RandomnessState Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR) return null;

                RandomnessState result = new RandomnessState();
                result.Randomness = _data.GetBytes(offset, 32);
                offset += 32;
                result.Timestamp = _data.GetS64(offset);
                offset += 8;
                result.Consumed = _data.GetBool(offset);
                offset += 1;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum VortiqErrorKind : uint
        {
            UnauthorizedCaller = 6000U
        }
    }

    namespace Types { }

    public partial class VortiqClient : TransactionalBaseClient<Errors.VortiqErrorKind>
    {
        public VortiqClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId = null) 
            : base(rpcClient, streamingRpcClient, programId ?? new PublicKey(Program.VortiqProgram.ID))
        {
        }

        // --- IMPROVED SMART TRANSACTION HANDLER ---
        private async Task<RequestResult<string>> SmartSignAndSend(
            Transaction tx, 
            IEnumerable<Account> auxiliarySigners, 
            Account explicitPayer,                 
            Commitment commitment)
        {
            // 1. Sign with auxiliary accounts first (if any)
            if (auxiliarySigners != null && auxiliarySigners.Any())
            {
                var signersList = auxiliarySigners.ToList();
                UnityEngine.Debug.Log($"[VortiqClient] Signing with {signersList.Count} auxiliary signer(s)");
                tx.PartialSign(signersList);
            }

#if UNITY_EDITOR
            // PATH A: EDITOR - Sign locally, send via raw HTTP (bypasses SDK JSON parser bug)
            if (explicitPayer == null)
            {
                throw new Exception("❌ No explicitPayer account provided for Editor signing.");
            }

            UnityEngine.Debug.Log("[VortiqClient] Using explicit payer account for signing (Editor mode)");
            
            var signature = explicitPayer.Sign(tx.CompileMessage());
            tx.AddSignature(explicitPayer.PublicKey, signature);
            
            byte[] txBytes = tx.Serialize();
            string txBase64 = Convert.ToBase64String(txBytes);
            UnityEngine.Debug.Log($"[VortiqClient] Transaction serialized, length: {txBytes.Length} bytes");
            
            // Bypass SDK's JsonRpcClient which throws "Unable to parse json"
            var rawResult = await SendTransactionRaw(txBase64);
            var editorResult = new RequestResult<string> { Result = rawResult.signature, Reason = rawResult.error };
            return editorResult;
#else
            // PATH B: MOBILE (Android/iOS) - Use wallet adapter for native signing
            if (Web3.Wallet != null)
            {
                UnityEngine.Debug.Log("[VortiqClient] Using Web3.Wallet for signing (Mobile/Phantom)");
                return await Web3.Wallet.SignAndSendTransaction(tx, skipPreflight: false, commitment);
            }

            // FALLBACK: If wallet adapter isn't available, try local signing
            if (explicitPayer != null)
            {
                UnityEngine.Debug.Log("[VortiqClient] Fallback: Using explicit payer on mobile");
                var sig = explicitPayer.Sign(tx.CompileMessage());
                tx.AddSignature(explicitPayer.PublicKey, sig);
                byte[] rawTx = tx.Serialize();
                string rawBase64 = Convert.ToBase64String(rawTx);
                var mobileRaw = await SendTransactionRaw(rawBase64);
                var mobileResult = new RequestResult<string> { Result = mobileRaw.signature, Reason = mobileRaw.error };
                return mobileResult;
            }

            throw new Exception(
                "❌ Cannot send transaction: No signing method available. " +
                "Web3.Wallet is null and no explicitPayer account was provided."
            );
#endif
        }

        /// <summary>
        /// Sends a signed transaction via raw HTTP POST, bypassing the SDK's 
        /// JsonRpcClient which has a known "Unable to parse json" bug.
        /// Returns (success, signature, error).
        /// </summary>
        private static async Task<(bool success, string signature, string error)> SendTransactionRaw(string txBase64)
        {
            string rpcUrl = Web3.Instance.customRpc;
            if (string.IsNullOrEmpty(rpcUrl))
                rpcUrl = "https://api.mainnet-beta.solana.com";

            // Build JSON-RPC request
            var requestBody = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = 1,
                ["method"] = "sendTransaction",
                ["params"] = new JArray
                {
                    txBase64,
                    new JObject
                    {
                        ["encoding"] = "base64",
                        ["skipPreflight"] = false,
                        ["preflightCommitment"] = "confirmed"
                    }
                }
            };

            string jsonPayload = requestBody.ToString(Newtonsoft.Json.Formatting.None);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonPayload);

            var request = new UnityWebRequest(rpcUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Wrap UnityWebRequest in TaskCompletionSource for async/await
            var tcs = new TaskCompletionSource<bool>();
            var op = request.SendWebRequest();
            op.completed += _ => tcs.TrySetResult(true);
            await tcs.Task;

            string responseText = request.downloadHandler.text;
            UnityEngine.Debug.Log($"[VortiqClient] Raw RPC response: {responseText}");

            if (request.result != UnityWebRequest.Result.Success)
            {
                string httpErr = $"HTTP Error: {request.error}";
                UnityEngine.Debug.LogError($"[VortiqClient] {httpErr}");
                request.Dispose();
                return (false, null, httpErr);
            }

            request.Dispose();

            try
            {
                var json = JObject.Parse(responseText);

                if (json["error"] != null)
                {
                    string errMsg = json["error"]["message"]?.ToString() ?? "Unknown RPC error";
                    UnityEngine.Debug.LogError($"[VortiqClient] RPC Error: {errMsg}");
                    return (false, null, errMsg);
                }

                string txSignature = json["result"]?.ToString();
                if (!string.IsNullOrEmpty(txSignature))
                {
                    UnityEngine.Debug.Log($"[VortiqClient] ✅ Transaction sent: {txSignature}");
                    return (true, txSignature, null);
                }
                else
                {
                    return (false, null, "No transaction signature in response");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[VortiqClient] Parse error: {ex.Message}\nResponse: {responseText}");
                return (false, null, $"Response parse error: {ex.Message}");
            }
        }

        public async Task<RequestResult<string>> InitializeAsync(
            Program.InitializeAccounts accounts, 
            IEnumerable<Account> signingAccounts = null, 
            Account payerAccount = null,                 
            PublicKey programId = null, 
            Commitment commitment = Commitment.Confirmed)
        {
            var instr = Program.VortiqProgram.Initialize(accounts, programId);
            var blockHash = await RpcClient.GetLatestBlockHashAsync(commitment);
            
            if (!blockHash.WasSuccessful)
            {
                UnityEngine.Debug.LogError($"❌ Failed to get blockhash: {blockHash.Reason}");
                return null; // Let the caller handle null result
            }
            
            var tx = new Transaction {
                RecentBlockHash = blockHash.Result.Value.Blockhash,
                FeePayer = accounts.Payer,
                Instructions = new List<TransactionInstruction> { instr }
            };

            return await SmartSignAndSend(tx, signingAccounts, payerAccount, commitment);
        }

        public async Task<RequestResult<string>> RequestRandomnessAsync(
            Program.RequestRandomnessAccounts accounts, 
            ulong kill_count, 
            IEnumerable<Account> signingAccounts = null, 
            Account payerAccount = null,
            PublicKey programId = null, 
            Commitment commitment = Commitment.Confirmed)
        {
            var instr = Program.VortiqProgram.RequestRandomness(accounts, kill_count, programId);
            var blockHash = await RpcClient.GetLatestBlockHashAsync(commitment);
            
            if (!blockHash.WasSuccessful)
            {
                UnityEngine.Debug.LogError($"❌ Failed to get blockhash: {blockHash.Reason}");
                return null; // Let the caller handle null result
            }
            
            var tx = new Transaction {
                RecentBlockHash = blockHash.Result.Value.Blockhash,
                FeePayer = accounts.Payer,
                Instructions = new List<TransactionInstruction> { instr }
            };

            return await SmartSignAndSend(tx, signingAccounts, payerAccount, commitment);
        }

        public async Task<RequestResult<string>> ConsumeRandomnessAsync(
            Program.ConsumeRandomnessAccounts accounts, 
            byte[] randomness, 
            IEnumerable<Account> signingAccounts = null, 
            Account payerAccount = null,
            PublicKey programId = null, 
            Commitment commitment = Commitment.Confirmed)
        {
            var instr = Program.VortiqProgram.ConsumeRandomness(accounts, randomness, programId);
            var blockHash = await RpcClient.GetLatestBlockHashAsync(commitment);
            
            if (!blockHash.WasSuccessful)
            {
                UnityEngine.Debug.LogError($"❌ Failed to get blockhash: {blockHash.Reason}");
                return null; // Let the caller handle null result
            }
            
            var tx = new Transaction {
                RecentBlockHash = blockHash.Result.Value.Blockhash,
                FeePayer = accounts.Payer,
                Instructions = new List<TransactionInstruction> { instr }
            };

            return await SmartSignAndSend(tx, signingAccounts, payerAccount, commitment);
        }

        // --- DATA METHODS (UNCHANGED) ---

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Accounts.RandomnessState>>> GetRandomnessStatesAsync(string programAddress = Program.VortiqProgram.ID, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Accounts.RandomnessState.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Accounts.RandomnessState>>(res);
            List<Accounts.RandomnessState> resultingAccounts = new List<Accounts.RandomnessState>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Accounts.RandomnessState.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Accounts.RandomnessState>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Accounts.RandomnessState>> GetRandomnessStateAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Accounts.RandomnessState>(res);
            var resultingAccount = Accounts.RandomnessState.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Accounts.RandomnessState>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Rpc.Core.Sockets.SubscriptionState> SubscribeRandomnessStateAsync(string accountAddress, Action<Solana.Unity.Rpc.Core.Sockets.SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Accounts.RandomnessState> callback, Commitment commitment = Commitment.Finalized)
        {
            return await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Accounts.RandomnessState parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Accounts.RandomnessState.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
        }

        protected override Dictionary<uint, ProgramError<Errors.VortiqErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<Errors.VortiqErrorKind>>{{6000U, new ProgramError<Errors.VortiqErrorKind>(Errors.VortiqErrorKind.UnauthorizedCaller, "Unauthorized caller")}, };
        }
    }

    namespace Program
    {
        public class ConsumeRandomnessAccounts
        {
            public PublicKey VrfProgramIdentity { get; set; }
            public PublicKey RandomnessState { get; set; }
            public PublicKey Payer { get; set; }
        }

        public class InitializeAccounts
        {
            public PublicKey RandomnessState { get; set; }
            public PublicKey Payer { get; set; }
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class MarkConsumedAccounts
        {
            public PublicKey Authority { get; set; }
            public PublicKey RandomnessState { get; set; }
        }

        public class RequestRandomnessAccounts
        {
            public PublicKey Payer { get; set; }
            public PublicKey OracleQueue { get; set; }
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey SlotHashes { get; set; } = new PublicKey("SysvarS1otHashes111111111111111111111111111");
        }

        public static class VortiqProgram
        {
            public const string ID = "8i88mEfKfssevViz93fxzxnSXWut28cKa8nysVDcAiTy";

            public static TransactionInstruction ConsumeRandomness(ConsumeRandomnessAccounts accounts, byte[] randomness, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<AccountMeta> keys = new()
                {AccountMeta.ReadOnly(accounts.VrfProgramIdentity, true), AccountMeta.Writable(accounts.RandomnessState, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(16882053693400275390UL, offset);
                offset += 8;
                _data.WriteSpan(randomness, offset);
                offset += randomness.Length;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static TransactionInstruction Initialize(InitializeAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<AccountMeta> keys = new()
                {AccountMeta.Writable(accounts.RandomnessState, false), AccountMeta.Writable(accounts.Payer, true), AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(17121445590508351407UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static TransactionInstruction MarkConsumed(MarkConsumedAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<AccountMeta> keys = new()
                {AccountMeta.Writable(accounts.Authority, true), AccountMeta.Writable(accounts.RandomnessState, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(4466570790232028923UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static TransactionInstruction RequestRandomness(RequestRandomnessAccounts accounts, ulong kill_count, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<AccountMeta> keys = new()
                {AccountMeta.Writable(accounts.Payer, true), AccountMeta.Writable(accounts.OracleQueue, false), AccountMeta.ReadOnly(accounts.SystemProgram, false), AccountMeta.ReadOnly(accounts.SlotHashes, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(1306022063415035349UL, offset);
                offset += 8;
                _data.WriteU64(kill_count, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}