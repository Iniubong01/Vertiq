using Reown.AppKit.Unity;
using UnityEngine;

public class AppKitInitializer : MonoBehaviour
{
    private async void Start()
    {
        string projectId = "c7c0756cbca65514565202ed30f68613";

        var config = new AppKitConfig
        {
            projectId = projectId,

            metadata = new Metadata(
                name: "Vortiq",
                description: "a fast-paced, blockchain-enhanced arcade shooter built in Unity",
                url: "https://github.com/Iniubong01/Vertiq",
                iconUrl: "https://cyan-elderly-lobster-29.mypinata.cloud/ipfs/bafkreige3cxf2jejnfcvybqbezqumri5laeaic5uty47cr3f7j63brww2a"
            ),

            // ✅ Solana chains
            supportedChains = new[]
            {
                ChainConstants.Chains.Solana,
                ChainConstants.Chains.SolanaDevNet
            },

            // ✅ Jupiter Wallet ID from WalletConnect
            // Found at: https://walletguide.walletconnect.network/?search=jupiter
            includedWalletIds = new[]
            {
                "0ef262ca2a56b88d179c93a21383fee4e135bd7bc6680e5c23566ffa383010372" // Jupiter Wallet ID
            }
        };

        await AppKit.InitializeAsync(config);
        Debug.Log("AppKit initialized with Solana support and Jupiter Wallet!");
    }
}