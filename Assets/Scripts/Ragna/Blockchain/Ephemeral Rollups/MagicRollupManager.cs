using UnityEngine;
using Solana.Unity.SDK;
using Solana.Unity.Rpc;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Programs;
using System.Threading.Tasks;
using System.Reflection;

public class MagicRollupManager : MonoBehaviour
{
    // --- CONFIGURATION ---
    private const string L1_URL = "https://api.mainnet-beta.solana.com";
    private const string ER_URL = "https://mainnet.magicblock.app"; 

    public GameObject WalletObject; 

    private Vortiq.VortiqClient _programL1; 
    private Vortiq.VortiqClient _programER; 
    private PublicKey _randomnessAccount;

    private Account GetLiveAccount()
    {
        // 1. Priority: Static WalletConnector
        if (WalletConnector.PlayerAccount != null) return WalletConnector.PlayerAccount;

        // 2. Reflection
        if (WalletObject != null)
        {
            var scripts = WalletObject.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                var type = script.GetType();
                var field = type.GetField("PlayerAccount");
                if (field != null && field.FieldType == typeof(Account))
                {
                    var acc = field.GetValue(script) as Account;
                    if (acc != null) return acc;
                }
                var prop = type.GetProperty("Account");
                if (prop != null && prop.PropertyType == typeof(Account))
                {
                    var acc = prop.GetValue(script) as Account;
                    if (acc != null) return acc;
                }
            }
        }

        // 3. Fallbacks
        if (Web3.Account != null) return Web3.Account;
        if (Web3.Wallet != null) return Web3.Wallet.Account;

        return null;
    }

    void Start()
    {
        var account = GetLiveAccount();
        if (account != null) SetupClients(account);
        Web3.OnLogin += SetupClients;
    }

    private void SetupClients(Account account)
    {
        if (_programL1 != null) return;

        Debug.Log($"✅ [MAINNET] Login Detected! User: {account.PublicKey}");

        var clientL1 = ClientFactory.GetClient(L1_URL);
        _programL1 = new Vortiq.VortiqClient(clientL1, null); 

        var clientER = ClientFactory.GetClient(ER_URL);
        _programER = new Vortiq.VortiqClient(clientER, null);
        
        Debug.Log("✅ Clients Configured.");
    }

    public async void InitializeAccount()
    {
        var account = GetLiveAccount();
        if (account == null)
        {
            Debug.LogError("❌ ERROR: No Wallet found. Please log in.");
            return;
        }

        if (_programL1 == null) SetupClients(account);

        Debug.Log("Initializing Vortiq Account on MAINNET (Real SOL will be spent)...");
        
        var newAccount = new Account(); 
        _randomnessAccount = newAccount.PublicKey;

        try 
        {
            var result = await _programL1.InitializeAsync(
                accounts: new Vortiq.Program.InitializeAccounts {
                    RandomnessState = _randomnessAccount,
                    Payer = account.PublicKey,
                    SystemProgram = SystemProgram.ProgramIdKey
                },
                // FIX: Added 'account' here so YOU sign the transaction
                signingAccounts: new [] { newAccount, account }, 
                commitment: Commitment.Confirmed
            );

            if (result.WasSuccessful) 
                Debug.Log($"✅ Vortiq Account Created on Mainnet: {_randomnessAccount}");
            else 
                Debug.LogError($"❌ Init failed: {result.Reason}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Crash: {ex.Message}");
        }
    }

    public async void SendFastRequest()
    {
        if (_randomnessAccount == null) 
        {
            Debug.LogError("⚠️ Click 'Initialize' first!");
            return;
        }

        var account = GetLiveAccount();
        if (_programER == null && account != null) SetupClients(account);

        Debug.Log("Sending Fast Transaction to Mainnet Rollup...");

        var fakeOracleQueue = new Account().PublicKey; 

        try 
        {
            var result = await _programER.RequestRandomnessAsync(
                accounts: new Vortiq.Program.RequestRandomnessAccounts {
                    Payer = account.PublicKey, 
                    OracleQueue = fakeOracleQueue, 
                    SystemProgram = SystemProgram.ProgramIdKey,
                    SlotHashes = new PublicKey("SysvarS1otHashes111111111111111111111111111")
                },
                // FIX: Added 'account' here so YOU sign the transaction
                signingAccounts: new [] { account },
                kill_count: 0, 
                commitment: Commitment.Processed 
            );
            
            if (result.WasSuccessful) 
                Debug.Log("✅ Success! Transaction sent to MagicBlock Mainnet.");
            else 
                Debug.LogError($"❌ Fast transaction failed: {result.Reason}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Crash: {ex.Message}");
        }
    }
}