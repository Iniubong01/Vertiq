using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class JupiterSwapUI : MonoBehaviour
{
    /*[Header("References")]
    public JupiterSwapManager swapManager;
    
    [Header("Input Fields")]
    public TMP_InputField inputAmountField;
    public TMP_Dropdown inputTokenDropdown;
    public TMP_Dropdown outputTokenDropdown;
    
    [Header("Buttons")]
    public Button swapButton;
    public Button reverseButton;
    
    [Header("Info Display")]
    public TMP_Text estimatedOutputText;
    public TMP_Text priceImpactText;

    private void Start()
    {
        // Setup dropdowns with common devnet tokens
        SetupTokenDropdowns();
        
        // Add button listeners
        if (swapButton != null)
            swapButton.onClick.AddListener(ExecuteSwap);
            
        if (reverseButton != null)
            reverseButton.onClick.AddListener(ReverseTokens);
    }

    private void SetupTokenDropdowns()
    {
        var options = new System.Collections.Generic.List<string>
        {
            "SOL",
            "USDC (Devnet)",
            "$PLAY (Custom)"
        };
        
        if (inputTokenDropdown != null)
        {
            inputTokenDropdown.ClearOptions();
            inputTokenDropdown.AddOptions(options);
            inputTokenDropdown.value = 0; // Default to SOL
        }
        
        if (outputTokenDropdown != null)
        {
            outputTokenDropdown.ClearOptions();
            outputTokenDropdown.AddOptions(options);
            outputTokenDropdown.value = 1; // Default to USDC
        }
    }

    public void ExecuteSwap()
    {
        if (swapManager == null)
        {
            Debug.LogError("SwapManager not assigned!");
            return;
        }

        // Get input amount
        if (!float.TryParse(inputAmountField.text, out float amount) || amount <= 0)
        {
            Debug.LogError("Invalid amount entered");
            return;
        }

        // Get token addresses based on dropdown selection
        string inputMint = GetTokenMintFromDropdown(inputTokenDropdown);
        string outputMint = GetTokenMintFromDropdown(outputTokenDropdown);

        if (inputMint == outputMint)
        {
            Debug.LogError("Cannot swap same tokens!");
            return;
        }

        // Execute swap based on token types
        if (inputMint == swapManager.wrappedSolMint)
        {
            // SOL to Token
            swapManager.SwapSolToToken(outputMint, amount);
        }
        else if (outputMint == swapManager.wrappedSolMint)
        {
            // Token to SOL
            swapManager.SwapTokenToSol(inputMint, amount);
        }
        else
        {
            // Token to Token
            swapManager.SwapTokenToToken(inputMint, outputMint, amount);
        }
    }

    private string GetTokenMintFromDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return "";

        switch (dropdown.value)
        {
            case 0: // SOL
                return swapManager.wrappedSolMint;
            case 1: // USDC
                return swapManager.usdcMint;
            case 2: // $PLAY
                return swapManager.playTokenMint;
            default:
                return "";
        }
    }

    private void ReverseTokens()
    {
        if (inputTokenDropdown != null && outputTokenDropdown != null)
        {
            int temp = inputTokenDropdown.value;
            inputTokenDropdown.value = outputTokenDropdown.value;
            outputTokenDropdown.value = temp;
        }
    }

    // Optional: Add method to estimate output before swapping
    public async void EstimateOutput()
    {
        // You can call GetQuote directly and display the estimated output
        // This would require making GetQuote public in JupiterSwapManager
        // For now, this is just a placeholder
        if (estimatedOutputText != null)
        {
            estimatedOutputText.text = "Click Swap to see estimate";
        }
    }*/
}